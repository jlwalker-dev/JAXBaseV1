using JAXBase.Core;
using JAXBase.XBase;
using System.Text;
using System.Text.RegularExpressions;
using JAXBase.Utilities;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_D
    {
        const char literalEnd = (char)3;
        const char paramEnd = (char)4;

        public static async Task<string> Define(AppClass app, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                // Are we already in a define?
                if (app.InDefine.Length > 0) throw new Exception("1926|");

                // Break up the command line
                string[] cmd = cmdLine.Split(AppClass.stmtDelimiter);

                // Must be at least three components
                if (cmd.Length < 3) throw new Exception("10|");

                GenericClass gc = await JAXBase_Executer_M.SolveFromRPNString(app, cmd[0]);
                string var = gc.Value.AsString().Trim();

                gc = await JAXBase_Executer_M.SolveFromRPNString(app, cmd[1]);
                string className = gc.Value.AsString().Trim();

                gc = await JAXBase_Executer_M.SolveFromRPNString(app, cmd[2]);
                string classType = gc.Value.AsString().Trim();

                // Now handle the define
                switch (var.ToString())
                {
                    case "B":
                        throw new Exception("1999|DEFINE BAR");

                    case "C":
                        app.InDefine = "C" + className;

                        ClassDef cd = new()
                        {
                            Name = className.ToLower(),
                        };

                        app.ClassDefinitions.Add(cd);
                        app.InDefineObject = new(app, classType, className, []);
                        break;

                    case "M":
                        throw new Exception("1999|DEFINE MENU");

                    case "P":
                        throw new Exception("1999|DEFINE PAD");

                    case "U":
                        throw new Exception("1999|DEFINE POPUP");

                    case "W":
                        throw new Exception("1999|DEFINE WINDOW");

                    default:
                        throw new Exception("1999|DEFINE " + var);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return AppErrorHandling.ErrorCount() > 0 ? string.Empty : result;
        }

        /* TODO NOW
         * 
         * DEBUG
         * 
         */
        public static string Debug(AppClass app, string cmdLine)
        {
            AppErrorHandling.ClearErrors();
            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return AppErrorHandling.ErrorCount() > 0 ? string.Empty : "$";

        }


        /* TODO NOW
         * 
         * DEBUGOUT
         * 
         */
        public static string DebugOut(AppClass app, string cmdLine)
        {
            AppErrorHandling.ClearErrors();
            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return AppErrorHandling.ErrorCount() > 0 ? string.Empty : "$";

        }


        /*
         * 
         * DELETE [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cAlias]
         * 
         * DELETE FILE cFileName
         * 
         */
        public static async Task<string> Delete(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            try
            {
                switch (eCodes.SUBCMD)
                {
                    case "file":    // DELETE FILE
                        // TODO - get all matching files
                        // and delete them
                        break;

                    default:        // DELETE records
                        await DeleteFor(jbe, eCodes, true);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;

        }


        /*
         * 
         * DELETE [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cAlias]
         * 
         */
        public static async Task<string> DeleteFor(JAXBase_Executer jbe, ExecuterCodes eCodes, bool delete)
        {
            string result = string.Empty;

            try
            {
                // Delete or recall?
                string action = delete ? "deleted" : "recalled";


                // Starting workarea
                int wa = jbe.App.CurrentDS.CurrentWorkArea();

                if (string.IsNullOrWhiteSpace(eCodes.InExpr) == false)
                {
                    JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.InExpr);

                    if (answer.Element.Type.Equals("C"))
                        jbe.App.CurrentDS.SelectWorkArea(answer.AsString());
                    else if (answer.Element.Type.Equals("N"))
                        jbe.App.CurrentDS.SelectWorkArea(answer.AsInt());
                    else
                        throw new Exception("11|");
                }

                int MaxCount = jbe.App.CurrentDS.CurrentWA.DbfInfo.RecCount;

                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, jbe.App.CurrentDS.CurrentWA, true);

                while (jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFEOF == false)
                {
                    // Break out if WHILE is false
                    if (string.IsNullOrWhiteSpace(eCodes.WhileExpr) == false && (await jbe.App.SolveFromRPNString(eCodes.WhileExpr)).AsBool() == false)
                        break;

                    // If no for expression or the for expression evaluates to true then process this record.
                    // The for expression is used to scan all records and selectively delete/recall those that match.
                    if (string.IsNullOrWhiteSpace(eCodes.ForExpr) || (await jbe.App.SolveFromRPNString(eCodes.ForExpr)).AsBool())
                    {
                        // Delete this record
                        await jbe.App.CurrentDS.CurrentWA.DBFDeleteRecord(delete);      // true = delete, false=recall
                        //counter++;
                        if (jaxScope.IsDone()) break;
                        await jbe.App.CurrentDS.CurrentWA.DBFSkipRecord(1);

                        // If while and for expressions are empty then we exit after one record
                        if (string.IsNullOrEmpty(eCodes.WhileExpr + eCodes.ForExpr))
                            break;
                    }
                }

                jbe.App.CurrentDS.SelectWorkArea(wa);

                result = string.Format("{0} records {1}", jaxScope.RecordsRead, action);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         *
         * DIMENSION ArrayName1(nRows1 [, nColumns1]) [AS cType] [, ArrayName2(nRows2 [, nColumns2])] ...
         * 
         *      literalVar1<exprDelimiter>expr1[<exprDelimiter>expr2][<stmtDelimiter>literalVar2...]
         *      
         */
        public static async Task<string> Dimension(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        if (answer.AsString().Contains("gaprops", StringComparison.OrdinalIgnoreCase))
                        {
                            int iii = 0;
                        }
                        VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                        AppVars.SetVarOrMakePrivate(var.varName, var.row, var.col, true);

                        // Set the var as this type
                        string typ = (await jbe.App.SolveFromRPNString(eCodes.As[i])).AsString();
                        if (string.IsNullOrWhiteSpace(typ) == false)
                            await AppVars.SetAsType(var.varName, eCodes.As[i]);
                    }
                    else
                        throw new Exception("11|");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /*
         * 
         * Directory [cSkeleton] [TO PRINTER [PROMPT] | TO FILE cFileName [ADDITIVE]]
         * 
         */
        public static async Task<string> Directory(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            StringBuilder dInfo = new();     // String to return to calling routine

            try
            {
                StringBuilder result = new();

                JAXObjects.Token answer = eCodes.Expressions.Count > 0 ? await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr) : new("*.dbf");

                if (eCodes.To.Count > 0)
                {
                    // To device
                }

                // Peform the directory listing to a stringbuilder var
                string drivePath = JAXLib.JustFullPath(answer.AsString());
                string fileSkeleton = JAXLib.JustFName(answer.AsString());

                if (string.IsNullOrWhiteSpace(drivePath))
                    drivePath = jbe.App.CurrentDS.JaxSettings.Default;

                if (FilerLib.GetFiles(drivePath, fileSkeleton, out string[] fileArray) == 0)
                {
                    int r = 0;

                    foreach (string file in fileArray)
                    {
                        int err = FilerLib.GetFileInfo(file, out string[] fileEntry);
                        if (err == 0)
                        {
                            r++;
                            if (r == 1)
                                dInfo.AppendLine("File                                              Length   Last Modified  Attributes");

                            if (int.TryParse(fileEntry[1], out int fsize) == false) fsize = 0;

                            dInfo.AppendLine(string.Format("{0} {1} {2} {3}", fileEntry[0].Length > 40 ? fileEntry[0][..36] + "..." : fileEntry[0].PadRight(40), string.Format("{0,15:N0}", fsize), fileEntry[2].ToUpper().Replace("T", " ")[..16], fileEntry[3]));
                        }
                        else
                            dInfo.AppendLine(string.Format("Error {0} reading file {1}", err, file));
                    }

                    dInfo.AppendLine(string.Format("{0} files found", r > 0 ? r : "No"));
                }

                if (eCodes.To.Count > 0)
                {
                    int additive = Array.IndexOf(eCodes.Flags, "additive") >= 0 ? 1 : 0;

                    // Send it out
                    switch (eCodes.To[0].Type)
                    {
                        case "F":           // Send to file
                            JAXLib.StrToFile(dInfo.ToString(), eCodes.To[0].Name, additive);
                            break;

                        case "P":           // Send to printer
                            throw new Exception("1999||Directory to printer is not yet supported");
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                dInfo = new();
            }

            AppIO.SendToIDE(dInfo.ToString());
            return "";
        }


        /*
         * 
         * DISPLAY [[FIELDS] FieldList] [Scope] [FOR lExpression1] [WHILE lExpression2] [OFF] [NOCONSOLE] [NOOPTIMIZE] [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]]
         * 
         * 
         * DISPLAY CONNECTIONS  [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY DATABASE     [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY DLLS         [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY PROCEDURES   [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         *
         * DISPLAY STATUS       [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY TABLES       [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY VIEWS        [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * 
         * DISPLAY STRUCTURE [IN nWorkArea | cTableAlias] [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * 
         * DISPLAY FILES [ON Drive] [LIKE FileSkeleton] [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]]
         *
         * DISPLAY MEMORY   [LIKE FileSkeleton]     [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY OBJECTS  [LIKE cObjectSkeleton]  [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * specific
         *      Structure   xU/tableName/toType/toDevName/flags (P-prompt, A-additive, N-noconsole)
         *      Files       xI/onDrive/onType/toDevName/flags
         *      Memory      xM/likeExpr/toType/toDevName/flags
         *      Objects     xO/likeExpr/toType/toDevName/flags
         *
         * default 
         *                  dType//toType/toDevName/flags
         */
        public static async Task<string> Display(JAXBase_Executer jbe, ExecuterCodes eCodes, bool display)
        {
            StringBuilder sb = new(Environment.NewLine);

            try
            {
                string dType = eCodes.SUBCMD;
                //string actOn = info[1];
                string toType = eCodes.To.Count > 0 ? eCodes.To[0].Type : string.Empty;
                string toDevName = eCodes.To.Count > 0 ? eCodes.To[0].Name : string.Empty;

                bool prompt = Array.IndexOf(eCodes.Flags, "P") >= 0;
                bool additive = Array.IndexOf(eCodes.Flags, "A") >= 0;
                bool noconsole = Array.IndexOf(eCodes.Flags, "N") >= 0;

                switch (dType.ToLower())
                {
                    case "memory":          // Memory
                    case "objects":         // Objects
                        List<string> vars = [];

                        // Load all private and local variables for each level
                        // Level 0 private vars are program globals
                        for (int i = jbe.App.AppLevels.Count - 1; i >= 0; i--)
                        {
                            List<string> varsX = [];

                            if (i == jbe.App.AppLevels.Count - 1)
                            {
                                varsX = jbe.App.AppLevels[i].LocalVars.GetVarNames();

                                for (int j = 0; j < varsX.Count; j++)
                                    vars.Add(varsX[j].PadRight(21) + (i == 0 ? "000" : "Loc"));
                            }

                            varsX = jbe.App.AppLevels[i].PrivateVars.GetVarNames();
                            for (int j = 0; j < varsX.Count; j++)
                                vars.Add(varsX[j].PadRight(21) + (i == 0 ? "Pub" : "Pri"));
                        }

                        // Sort the list of vars
                        vars.Sort();

                        // Remove anything that doesn't match like templates
                        if (eCodes.Like.Count > 0)
                        {
                            // First, create the regex strings
                            for (int i = 0; i < eCodes.Like.Count; i++)
                            {
                                string rx = eCodes.Like[i];
                                rx = rx.Replace("*", ".*");
                                rx = rx.Replace("?", ".?");
                                eCodes.Like[i] = "^" + rx + "$";
                            }

                            // Now compare them
                            for (int j = vars.Count - 1; j >= 0; j--)
                            {
                                bool deleteMe = true;
                                for (int i = eCodes.Like.Count - 1; i >= 0; i--)
                                {
                                    if (Regex.IsMatch(vars[j][..20].Trim(), eCodes.Like[i], RegexOptions.IgnoreCase))
                                    {
                                        // If a match, remember that
                                        // and drop out of the loop
                                        deleteMe = false;
                                        break;
                                    }
                                }

                                // If no match, kill it
                                if (deleteMe)
                                    vars.RemoveAt(j);
                            }
                        }

                        // Display the var, scope, type, and value
                        for (int j = 0; j < vars.Count; j++)
                        {
                            JAXObjects.Token t = await AppVars.GetVarToken(vars[j][..20].Trim());

                            if (t.TType.Equals("A"))
                            {
                                // Handle the array display
                                sb.AppendLine(string.Format("{0} {1}", vars[j], t.TType));
                                if (t.Row == 0)
                                {
                                    // 1D array
                                    for (int c = 1; c <= t.Col; c++)
                                    {
                                        t.SetElement(0, c);
                                        if (t.Element.Type.Equals("O"))
                                        {
                                            JAXObjectWrapper o = (JAXObjectWrapper)t.Element.Value;

                                            JAXObjects.Token tk = await o.GetProperty("baseclass");
                                            string typ = tk.AsString();
                                            string nam = (await o.GetProperty("name")).AsString();
                                            sb.AppendLine(string.Format("    [{0}]={1}", c, t.Element.Value));
                                        }
                                        else
                                            sb.AppendLine(string.Format("    [{0}]={1}", c, t.Element.Value));
                                    }
                                }
                                else
                                {
                                    // 2D array
                                    for (int r = 1; r <= t.Row; r++)
                                    {
                                        for (int c = 1; c <= t.Col; c++)
                                        {
                                            t.SetElement(r, c);
                                            if (t.Element.Type.Equals("O"))
                                            {
                                                JAXObjectWrapper o = (JAXObjectWrapper)t.Element.Value;

                                                JAXObjects.Token tk = await o.GetProperty("baseclass");
                                                string typ = tk.AsString();
                                                string nam = (await o.GetProperty("name")).AsString();
                                                sb.AppendLine(string.Format("    [{0}]={1}", c, t.Element.Value));
                                            }
                                            else
                                                sb.AppendLine(string.Format("    [{0},{1}]={2}", r, c, t.Element.Value));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (t.Element.Type.Equals("O"))
                                {
                                    // Display the object
                                    JAXObjectWrapper o = (JAXObjectWrapper)t.Element.Value;

                                    JAXObjects.Token tk = await o.GetProperty("baseclass");
                                    string typ = tk.AsString();
                                    string nam;

                                    if (typ.Equals("empty", StringComparison.OrdinalIgnoreCase))
                                        nam = "<none>";
                                    else
                                        nam = (await o.GetProperty("name")).AsString();

                                    // Program variable
                                    sb.AppendLine(string.Format("{0} {1}  {2} (Name: {3})", vars[j], t.Element.Type, typ, nam));
                                }
                                else
                                {
                                    sb.AppendLine(string.Format("{0} {1}  {2}", vars[j], t.Element.Type, t.Element.Value));
                                }
                            }

                        }
                        break;

                    default:
                        throw new Exception("1999|" + dType);
                }

                AppIO.DebugLog(sb.ToString(), false);

                if (toType.Length > 0)
                {
                    if (toType[..1].Equals("F") && toDevName.Length > 0)
                        JAXLib.StrToFile(sb.ToString(), toDevName, 0);
                    else if (toType[..1].Equals("P"))
                    {
                        // send to printer
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            AppIO.SendToIDE(sb.ToString());

            return "";
        }

        /*
         * 
         *  DO ProgramName1 | ProcedureName [IN ProgramName2] [WITH ParameterList]
         *      "P" + AppClass.stmtDelimiter + prg + AppClass.stmtDelimiter + inprg + AppClass.stmtDelimiter + plist;
         *  
         *  DO FORM FormName | ? [NAME VarName [LINKED]] [WITH cParameterList] [TO VarName] [NOREAD] [NOSHOW]
         *
         *  DO WHILE lExpression 
         *  
         *  DO CASE
         *  
         *  DO 
         *  UNTIL lExpression
         *  
         *  P - program
         *  F - Form
         *  W - While
         *  C - Case
         *  U - Until
         * 
         *  P|F / filename / inFileName / parameterList / toVar / flags
         *  
         *  Following have a loop code following command code
         *  W##|U##|C## / expression
         *  
         *  RETURNS
         *      X000 - Move command pointer to location encoded in 3 char simple base 64 string
         */
        public static async Task<string> Do(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            AppIO.DebugLog("Entering DO");
            string result = string.Empty;
            string PrgCode = jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx < 0 ? jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgCode : jbe.App.PRGCache.Count > 0 ? jbe.App.PRGCache[jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx] : string.Empty;
            JAXObjects.Token answer = new();

            try
            {
                switch (eCodes.SUBCMD[..1])
                {
                    case "U":       // Do Until
                        AppLoop.PushLoop(eCodes.SUBCMD);
                        break;

                    case "P":       // Do Program
                        result = await DoPrg(jbe, eCodes);
                        break;

                    case "F":       // Do Form
                        result = await DoSCX(jbe, eCodes);
                        break;

                    case "C":       // Do case
                        if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                        string ccase = AppClass.cmdByte.ToString() + jbe.App.MiscInfo["casecmd"] + eCodes.SUBCMD;
                        string cOtherwise = AppClass.cmdByte.ToString() + jbe.App.MiscInfo["otherwisecmd"] + eCodes.SUBCMD;
                        string cEndCase = AppClass.cmdByte.ToString() + jbe.App.MiscInfo["endcasecmd"] + eCodes.SUBCMD;

                        while (true)
                        {
                            int pos = PrgCode.IndexOf(ccase, jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos + 1);

                            if (pos < 0)
                            {
                                // Look for otherwise
                                pos = PrgCode.IndexOf(cOtherwise, jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos + 1);

                                // if not found, look for endcase
                                if (pos < 0)
                                {
                                    // Look for endcase
                                    pos = PrgCode.IndexOf(cEndCase, jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos + 1);
                                    if (pos < 0)
                                        throw new Exception("Mismatched DO CASE/ ENDCASE");
                                    else
                                    {
                                        // Found the encase
                                        result = "Y" + result;
                                        break;
                                    }
                                }
                                else
                                {
                                    // Go to the next command after the otherwise
                                    result = "Y" + result;
                                    break;
                                }
                            }
                            else
                            {
                                // Get case statement
                                int endcmd = PrgCode.IndexOf(AppClass.cmdEnd, pos);
                                if (endcmd < 0)
                                    throw new Exception("Unexpected end of file");

                                jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = pos;
                                string caseExpr = PrgCode[pos..endcmd];
                                caseExpr = caseExpr[8..]; // Remove the case command 

                                // process the expression
                                GenericClass gc = await JAXBase_Executer_M.SolveFromRPNString(jbe.App, caseExpr);

                                // if true, go to the next line
                                if (gc.Value.Element.Type.Equals("L") && gc.Value.AsBool())
                                {
                                    jbe.App.utl.Conv64(++endcmd, 3, out result);
                                    result = "Y" + result;
                                    break;
                                }

                                // if not, then loop
                                pos = endcmd + 1; // Look for next case command
                            }
                        }
                        break;

                    case "W":       // Do While
                        if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                        AppLoop.PushLoop(eCodes.SUBCMD);

                        if (eCodes.Expressions.Count != 1) throw new Exception($"10||DO WHILE has {eCodes.Expressions.Count} expressions and requires just 1");

                        answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                        if ("UX".Contains(answer.TType) || answer.Element.Type.Equals("L") == false)
                            throw new Exception("11|");
                        else
                        {
                            if (answer.Element.ValueAsBool == false)
                            {
                                // find endwhile and then go past to next command
                                string wend = AppClass.cmdByte + jbe.App.MiscInfo["enddocmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                                int pos = PrgCode.IndexOf(wend);
                                pos = PrgCode.IndexOf(AppClass.cmdEnd, pos);

                                if (pos < 0)
                                    throw new Exception("Mismatched DO WHILE/ ENDDO");
                                else
                                {
                                    jbe.App.utl.Conv64(++pos, 3, out result);
                                    result = "Y" + result;
                                }
                            }
                        }
                        break;

                    default:
                        throw new Exception(string.Format("1999|DO {0}|", eCodes.SUBCMD));
                }

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * 
         * DOEVENTS
         * 
         */
        public static string DoEvents(AppClass app, string cmdLine)
        {
            AppErrorHandling.ClearErrors();
            try
            {
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }





        /*
         * TODO - Fix this up once we have SCX forms defined
         */
        public static async Task<string> DoSCX(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                string prgToLoad = eCodes.NAME;

                // Break up the file name
                string prgPath = JAXLib.JustFullPath(prgToLoad);
                string prgFile = JAXLib.JustStem(prgPath);
                string prgExt = JAXLib.JustExt(prgPath);

                List<string> parameters = [];
                for (int i = 0; i < eCodes.With.Count; i++) parameters.Add(eCodes.With[i].RNPExpr);
                await AppHelper.LoadRPNListToParameters(jbe.App, parameters, true);

                // if prgPath is empty, look around for the file
                if (prgPath.Length == 0)
                {
                    // Look for the file
                    if (await jbe.App.JaxExecuter.LoadAndExecuteProgram("F", prgToLoad, prgToLoad, null, true))
                    {
                        // TODO - Anything?
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /*
         * This is how we call all PRGs
         * 
         * pCode[0]=P for Program, S for Form
         * pCode[1]=prg/procedure name
         * pCode[2]=procedure parent if in a procedure file
         * pCode[3]=string of RPN expressions for parameters
         */
        public static async Task<string> DoPrg(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                JAXObjects.Token answer = new();

                string prgToLoad = string.Empty;
                string prgToRun;

                // DO what program?
                if (eCodes.Expressions.Count < 1) throw new Exception("10|");
                answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (answer.Element.Type.Equals("C") && string.IsNullOrWhiteSpace(answer.AsString()) == false)
                    prgToRun = answer.AsString().Trim();
                else
                    throw new Exception("11|");

                // IN what program?
                if (string.IsNullOrWhiteSpace(eCodes.InExpr) == false)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.InExpr);
                    if (answer.Element.Type.Equals("C"))
                        prgToLoad = answer.AsString().Trim();
                    else
                        throw new Exception("11|");
                }

                // WITH always uses the ParameterClasslist so that
                // we can pass by ref
                if (eCodes.With.Count > 0)
                {
                    jbe.App.ParameterClassList.Clear();

                    List<string> parameters = [];
                    for (int i = 0; i < eCodes.With.Count; i++)
                        parameters.Add(eCodes.With[i].RNPExpr);

                    await AppHelper.LoadRPNListToParameters(jbe.App, parameters, true);
                }

                // Look for the file
                if (await jbe.App.JaxExecuter.LoadAndExecuteProgram("P", prgToLoad, prgToRun, null, true))
                {
                    // TODO - Anything?
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * DO (application)
         * 
         */
        public static string DoApp(AppClass app, string appFile, string[] RPNParams)
        {

            return string.Empty;
        }

        /* TODO NOW
        * 
        * DODEFAULT()
        * 
        */
        public static string DoDefault(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string thisProcedure = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure;
            JAXObjectWrapper? thisObject = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObject;
            string thisMethod = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObjectMethod;

            if (thisObject is null)
            {
                throw new Exception("|");
            }
            else
            {
                thisObject.MethodCall(thisMethod).Wait();
            }
            return string.Empty;
        }

        /* TODO
         * 
         *  DROP TABLE
         * 
         */
        public static string Drop(AppClass app, string cmdLine)
        {
            AppErrorHandling.ClearErrors();
            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return AppErrorHandling.ErrorCount() > 0 ? string.Empty : "$";

        }
    }
}