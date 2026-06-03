using JAXBase.Core;
using JAXBase.Data;
using JAXBase.UI.Dialogs;
using JAXBase.Utilities;
using JAXBase.XBase;
using static JAXBase.Core.AppClass;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_C
    {
        /* 
         * 
         * CALCULATE eExpressionList [Scope] [FOR lExpression1] [WHILE lExpression2] [TO VarList | TO ARRAY ArrayName] [NOOPTIMIZE] [IN nWorkArea | cTableAlias]
         * 
         * expList/scopeExpr/forExp/whileExpr/V|A/varInfo/wrkareaExpr/flags
         * 
         *      Code    ExpList Functions                           Description
         *      A       AVG(expression)                             Average
         *      C       CNT() or COUNT()                            Count
         *      X       MAX(expression)                             Find Max value
         *      M       MIN(expression)                             Find Min value
         *      N       NPV(expression1,expression2[,expression3])  Net present value
         *      D       STD(expression)                             Standard deviation
         *      S       SUM(expression)                             Sums the values
         *      V       VAR(expression)                             Variance (STD ^ 2)
         *      
         *      ExprList Construction
         *      <literalStart>Code1<literalEnd><expStart>expressionList1<expEnd>[<exprDelimiter><literalStart>Code2<literalEnd><expStart>expressionList2<expEnd>]...
         *      
         */
        public static async Task<string> Calculate(JAXBase_Executor jbe, ExecutorCodes eCodes)
        {
            string editor = string.Empty;

            try
            {
                // Break out the calculation expression type
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    string[] exp = eCodes.Expressions[i].RNPExpr.Split(AppClass.expEnd);
                    eCodes.Expressions[i].Type = (await Program.CurrentApp.SolveFromRPNString(exp[0])).AsString(); // Get the calculation type
                    eCodes.Expressions[i].RNPExpr = exp[1];                                 // Get the calculation expression
                }

                // Go to the desired workarea
                int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (Program.CurrentApp.CurrentDS.CurrentWA is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", Program.CurrentApp.CurrentDS.CurrentWorkArea()));

                bool NoOptimize = eCodes.Flags.Length > 0 && Array.IndexOf(eCodes.Flags, "nooptimize") >= 0;

                // Now process the table as requested
                if (eCodes.Expressions.Count > eCodes.To.Count)
                    throw new Exception("1230|");                                               // Too many arguments

                if (eCodes.Expressions.Count < eCodes.To.Count)
                    throw new Exception("94|");                                                 // Must specify additional parameters

                // Create the sums list
                List<JAXObjects.Token> sums = [];
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    JAXObjects.Token t = new();
                    t.Element.Value = 0;
                    sums.Add(t);
                }

                // first record is already in the buffer and continue working on records until
                // we reach EOF.  We use goto top/bottom and skip because we want to play nice
                // with indexes
                JAXDirectDBF Table = Program.CurrentApp.CurrentDS.CurrentWA;
                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, Table, true);

                while (Table.DbfInfo.DBFEOF == false && Table.DbfInfo.RecCount > 0)
                {
                    JAXObjects.Token temp = await Program.CurrentApp.SolveFromRPNString(eCodes.ForExpr);
                    if (string.IsNullOrWhiteSpace(eCodes.ForExpr) || temp.Element.ValueAsBool)
                    {
                        temp = await Program.CurrentApp.SolveFromRPNString(eCodes.WhileExpr);
                        if (string.IsNullOrWhiteSpace(eCodes.WhileExpr) || temp.Element.ValueAsBool)
                        {
                            //recsRead++;

                            for (int j = 0; j < eCodes.Expressions.Count; j++)
                            {
                                JAXObjects.Token tk = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[j].RNPExpr);

                                if (tk.Element.Type.Equals("N"))
                                {
                                    switch (eCodes.Expressions[j].Type)
                                    {
                                        case "A":       // Average
                                            sums[j].Element.Value = sums[j].Element.ValueAsDouble + tk.Element.ValueAsDouble;
                                            break;

                                        case "C":       // Count
                                            sums[j].Element.Value = sums[j].Element.ValueAsDouble + 1;
                                            break;

                                        case "X":       // Max
                                            // If this the first record or the new value is larger
                                            if (jaxScope.RecordsRead == 0 || sums[j].Element.ValueAsDouble < tk.Element.ValueAsDouble)
                                                sums[j].Element.Value = tk.Element.ValueAsDouble;
                                            break;

                                        case "M":       // Min
                                            // If this the first record or the new value is smaller
                                            if (jaxScope.RecordsRead == 0 || sums[j].Element.ValueAsDouble > tk.Element.ValueAsDouble)
                                                sums[j].Element.Value = tk.Element.ValueAsDouble;
                                            break;

                                        case "N":       // Net Present Value - https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.financial.npv?view=net-9.0
                                            break;

                                        case "D":       // Standard Deviation - https://blog.danstockham.com/calculate-standard-deviation-with-c-cl2h05zjt02evz0nv609z8m5t
                                            break;

                                        case "S":       // Sum
                                            sums[j].Element.Value = sums[j].Element.ValueAsDouble + tk.Element.ValueAsDouble;
                                            break;

                                        case "V":       // Variance - https://www.coderslexicon.com/variance-and-standard-deviation-of-an-array-in-c/
                                            break;

                                        default:
                                            throw new Exception("10||Unknown expression type");
                                    }
                                }
                                else
                                    throw new Exception("Invalid expression type");
                            }

                            // Have we reached the end of the until flag scope?
                            if (jaxScope.IsDone()) break;
                        }
                        else
                        {
                            // break out of the loop because the
                            // while statement is false
                            break;
                        }

                    }

                    // Have we reached the end of the until flag scope?
                    //if (recsRead > 0 && (untilFlag == 0 || untilFlag == recsRead)) break;

                    // Otherwise try to read in the next record
                    await Table.DBFSkipRecord(1);
                }

                // Now finalize the results and place them into the requested vars
                for (int j = 0; j < eCodes.Expressions.Count; j++)
                {
                    switch (eCodes.Expressions[j].Type)
                    {
                        case "A":       // Average
                            sums[j].Element.Value = sums[j].Element.ValueAsDouble / jaxScope.RecordsRead;
                            break;

                        case "N":       // Net Present Value - https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.financial.npv?view=net-9.0
                            break;

                        case "D":       // Standard Deviation - https://blog.danstockham.com/calculate-standard-deviation-with-c-cl2h05zjt02evz0nv609z8m5t
                            break;

                        case "V":       // Variance - https://www.coderslexicon.com/variance-and-standard-deviation-of-an-array-in-c/
                            break;

                        default:
                            break;
                    }
                }

                // if going to an array
                if (eCodes.To[0].Type.Equals("A"))
                    AppVars.SetVarOrMakePrivate(eCodes.To[0].Name, 1, eCodes.Expressions.Count, false);

                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    // Get toVar name
                    string vName = eCodes.To[0].Type.Equals("A") ? eCodes.To[0].Name : eCodes.To[i].Name;

                    // Get the value
                    double dval = sums[i].Element.ValueAsDouble;

                    // Does the var exist?                    
                    //AppVars.GetVar(vName, out JAXObjects.Token v);
                    JAXObjects.Token v = await AppVars.GetVarFromExpression(vName, null);

                    if (v.TType.Equals("U"))
                    {
                        if (eCodes.To[0].Type.Equals("A"))         // If user wants an array, make sure you accomodate
                            AppVars.SetVarOrMakePrivate(vName, 1, eCodes.Expressions.Count, true);
                        else
                            AppVars.SetVarOrMakePrivate(vName, 1, 1, true);
                    }

                    // Put the value into the var
                    //AppVars.GetVar(vName, out v);
                    v = await AppVars.GetVarFromExpression(vName, null);

                    if (v.TType.Equals("A"))
                        AppVars.SetVar(vName, dval, 1, i);
                    else
                        AppVars.SetVar(vName, dval, 1, 1);
                }

                // Make sure we get back to starting workarea
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }


        /* 
         *  CANCEL
         */
        public static async Task<string> Cancel(JAXBase_Executor jbe, ExecutorCodes? eCodes)
        {
            // If in runtime mode, then quit the application
            if (Program.CurrentApp.RuntimeFlag) await JAXBase_Executor_Q.Quit(null);

            // We're in the IDE, so just stop execution
            for (int i = Program.CurrentApp.AppLevels.Count; i > 1; i--)
                Program.CurrentApp.AppLevels.RemoveAt(i - 1);

            Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count - 1;
            AppIO.Talk("Execution Canceled");

            return "!";
        }


        /* 
         * CASE lExpression
         * 
         * If we stumble onto this, we will look for an endcase because we should only be loading 
         * this command in a DO CASE statement. The DO CASE statement jumps through the related 
         * case statements until if finds a case expression that is true, otherwise, or endcase.
         * 
         * When an expression is true, it starts with the next command record and continues until
         * it finds another related case structure statement.
         * 
         */
        public static string Case(JAXBase_Executor jbe, ExecutorCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                string cEndCase = AppClass.cmdByte.ToString() + Program.CurrentApp.MiscInfo["endcasecmd"] + eCodes.SUBCMD;

                // Find the endcase
                int pos = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx].IndexOf(cEndCase);

                if (pos < 0)
                    throw new Exception("1211|");   // If/Else/Endif stmt is missing
                else
                {
                    Program.CurrentApp.utl.Conv64(pos, 3, out string pos2);
                    result = "Y" + pos2; // Return the position of the endcase
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
         * CATCH [TO VarName] [WHEN lExpression]
         */
        public static async Task<string> Catch(JAXBase_Executor jbe, ExecutorCodes eCodes)
        {
            string result = string.Empty;
            bool tryDone = false;

            try
            {
                // Get the expected tryCode string
                string tryCode = eCodes.SUBCMD;
                int prgPos = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;

                // If there is nothing in the LoopStack then something is wrong
                if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LoopStack.Count == 0)
                    throw new Exception("2058||No active loop");
                // What phase are we in?
                TryClass tryElement = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack[^1];

                if (tryElement.TryPhase == 1)
                {
                    tryDone = true;
                }
                else if (tryElement.TryPhase > 2)
                {
                    // A CATCH has executed already
                    tryDone = true;
                }
                else
                {
                    // Set the answer to TRUE.  If there isn't a WHEN expression
                    // then answer falls through and this CATCH is executed.
                    // The only way it be false is if the CATCH has a WHEN.
                    JAXObjects.Token answer = new(true);
                    if (string.IsNullOrWhiteSpace(eCodes.WhenExpr) == false)
                        answer = await Program.CurrentApp.SolveFromRPNString(eCodes.WhenExpr);

                    if (answer.Element.Type.Equals("L"))
                    {
                        if (answer.AsBool())
                        {
                            // We found a CATCH to execute.  Get the current error object
                            JAXErrors le = AppErrorHandling.GetCurrentError();
                            string toVar;

                            // Is there a TO entry?
                            if (eCodes.To.Count > 0)
                            {
                                // Create the array variable from the TO expression
                                toVar = (await Program.CurrentApp.SolveFromRPNString(eCodes.To[0].Name)).AsString();

                                if (toVar.Length > 0)
                                {
                                    if (AppHelper.IsLegalObjectName(toVar))
                                    {
                                        // Create an empty class and populate it with the error information
                                        AppVars.MakeLocalVar(toVar, 1, 1, false);
                                        JAXObjects.Token errtk = await AppVars.GetVarToken(toVar, false);

                                        JAXObjectWrapper errInfo = new(Program.CurrentApp, "empty", "", []);
                                        await errInfo.AddProperty("errorno", new(le.ErrorNo), 0, "");
                                        await errInfo.AddProperty("lineno", new(le.ErrorLine), 0, "");
                                        await errInfo.AddProperty("message", new(le.ErrorMessage), 0, "");
                                        await errInfo.AddProperty("procedure", new(le.ErrorProcedure), 0, "");

                                        errtk.Element.Value = errInfo;
                                    }
                                    else
                                        throw new Exception($"46|TO|CATCH {tryCode} TO value '{toVar.ToUpper()}' is not a legal name");
                                }

                                // Talk
                                AppIO.Talk("Catch" + (string.IsNullOrEmpty(toVar) ? "" : "to " + toVar.ToUpper()));

                                // We found a CATCH to execute
                                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack[^1].TryPhase = 3;
                            }
                        }
                        else
                        {
                            // This CATCH had a WHEN that resolved to .F.
                            // Look for the next catch and if not found raise an error
                            int nextCatch = -1;
                            int thisPos = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;

                            for (int i = 0; i < tryElement.CasePos.Count; i++)
                            {
                                if (tryElement.CasePos[i] > prgPos)
                                    nextCatch = tryElement.CasePos[i];
                            }

                            if (nextCatch < 0)
                            {
                                throw new Exception("9999||Missing catch");
                            }
                            else
                            {
                                // We have the next one
                                Program.CurrentApp.utl.Conv64(nextCatch, 3, out string lp2);
                                result = "X" + lp2;
                            }
                        }
                    }
                    else
                        throw new Exception("11|");

                }

                if (tryDone)
                {
                    // No error so CATCH is not needed. Is there a FINALLY?
                    if (tryElement.FinallyPos < 0)
                    {
                        if (tryElement.EndTry < 0)
                            throw new Exception("2058||Missing ENDTRY");
                        else
                        {
                            // Found the ENDTRY
                            Program.CurrentApp.utl.Conv64(tryElement.EndTry, 3, out string lp2);
                            result = "X" + lp2;
                        }
                    }
                    else
                    {
                        // Found the FINALLY
                        Program.CurrentApp.utl.Conv64(tryElement.FinallyPos, 3, out string lp2);
                        result = "X" + lp2;
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
         * CD Path
         */
        public static async Task<string> CD(JAXBase_Executor jbe, ExecutorCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (eCodes.FileExpr.Count > 0)
                {
                    JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.FileExpr[0].RNPExpr);

                    if (answer.Element.Type.Equals("C") == false)
                        throw new Exception("10|");

                    if (answer.Element.Value.Equals("?"))
                    {
                        // Bring up directory dialog
                        DialogHelper dialogHelper = new DialogHelper();
                        await dialogHelper.ShowFilePicker(Program.CurrentApp, "D");

                        string path = Program.CurrentApp.ReturnValue.Element.IsNull() ? "" : Program.CurrentApp.ReturnValue.AsString();
                        if (Directory.Exists(path))
                        {
                            Program.CurrentApp.CurrentDS.JaxSettings.Default = path;
                            result = "Default directory is " + path;
                        }
                    }
                    else
                    {
                        string path = JAXLib.Addbs(answer.AsString().Trim());

                        // Was something sent?
                        if (string.IsNullOrWhiteSpace(path) == false)
                        {
                            if (Program.CurrentApp.OS == OSType.Windows)
                            {
                                // If not an absolute path, then put the default path in first
                                if ((path.Length > 2 && (path[..2].Equals(@"\\") || path[1] == ':')) == false)
                                    path = Program.CurrentApp.CurrentDS.JaxSettings.Default + (path.Length > 1 && path[0] == '\\' ? path[1..] : path);
                            }
                            else
                            {
                                // Assuming Linux - add to default path if
                                // the provided path doesn't start with backslash
                                if (path[0] != '\\')
                                    path = Program.CurrentApp.CurrentDS.JaxSettings.Default + path;
                            }
                        }

                        if (string.IsNullOrWhiteSpace(path))
                        {
                            // Nothing to do, so just return the default
                            AppIO.Talk("Current directory is " + Program.CurrentApp.CurrentDS.JaxSettings.Default);
                        }
                        else if (Directory.Exists(path))
                        {
                            Program.CurrentApp.CurrentDS.JaxSettings.Default = path;
                            AppIO.Talk("Default directory is " + path);
                        }
                        else
                            throw new Exception("202|" + path);
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }


        /* TODO
         * 
         * EDIT
         * CHANGE [FIELDS FieldList] [Scope] 
         *      [FOR lExpression1] [WHILE lExpression2]
         *      [NAME ObjectName] [NOAPPEND] [NOCAPTION] [NOCLEAR] [NODELETE] 
         *      [NOEDIT | NOMODIFY] [NOLINK] [NOMENU] [NOOPTIMIZE] [NORMAL] [NOWAIT] 
         *      [REST] [SAVE] [TIMEOUT nSeconds] [TITLE cTitleText] 
         */
        public static string Change(AppClass app, string cmdRest)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }


        /*
         * 
         * CLEAR [ALL | CLASS ClassName | CLASSLIB ClassLibraryName | CONSOLE [ConsoleName]
         *      | DEBUG | EVENTS | ERROR |FIELDS | GETS | MACROS | MEMORY 
         *      | PROGRAM [Name]| PROMPT | RESOURCES [FileName] | TYPEAHEAD]
         *      
         */
        public static async Task<string> Clear(ExecutorCodes eCodes)
        {
            try
            {
                string clearCode = eCodes.SUBCMD;
                JAXObjects.Token clearName = new("");    // Set to empty string

                if (eCodes.Expressions.Count > 0)
                    clearName = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                string name = clearName.Element.Type.Equals("C") ? clearName.AsString() : throw new Exception("11|");

                if (clearCode.Equals("N") || string.IsNullOrEmpty(clearCode))
                {
                    // Clear named console
                    JAXApp.MainWindowInstance?.ClearMainOutput();
                }
                else if ("P".Contains(clearCode))   // * = ALL and that doesn't clear programs from cache
                {
                    // Clear program(s)
                    if (name.Length == 0)
                    {
                        // Clear all code from cache
                        Program.CurrentApp.CodeCache = [];
                        Program.CurrentApp.PRGCache = [];
                    }
                    else
                    {
                        // Look for any matches
                        string stem = JAXLib.JustStem(name);

                        for (int i = Program.CurrentApp.CodeCache.Count - 1; i >= 0; i--)
                        {
                            if (Program.CurrentApp.CodeCache[i].FileStem.Equals(stem, StringComparison.OrdinalIgnoreCase))
                                Program.CurrentApp.CodeCache.RemoveAt(i);
                        }
                    }
                }
                else if ("V*".Contains(clearCode))
                {
                    // Clear ClassLib
                    if (name.Length == 0)
                    {
                        // Clear all code from cache
                        Program.CurrentApp.ClassLibs = [];
                    }
                    else
                    {
                        // Find and clear this program
                        foreach (KeyValuePair<string, CCodeCache> c in Program.CurrentApp.ClassLibs)
                        {
                            if (c.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || c.Value.FQFN.Equals(name, StringComparison.OrdinalIgnoreCase))
                                Program.CurrentApp.ClassLibs.Remove(c.Key);
                        }
                    }
                }
                else if (clearCode.Equals("C"))
                {
                    // TODO - think this through
                }
                else if ("M*".Contains(clearCode))
                {
                    // Clear Memory
                    for (int i = 0; i < Program.CurrentApp.AppLevels.Count; i++)
                    {
                        Program.CurrentApp.AppLevels[i].PrivateVars = new();
                        Program.CurrentApp.AppLevels[i].LocalVars = new();
                    }
                }
                else if ("D*".Contains(clearCode))
                {
                    // Clear debug
                    //JAXSysObj.SetValue("debug", "OFF");

                    // Delete all debug files
                }
                else if ("R*".Contains(clearCode))
                {
                    // Clear errors
                    AppErrorHandling.ClearErrors();
                }
                else if ("L*".Contains(clearCode))
                {
                    // Clear DLLs
                }
                else if ("R*".Contains(clearCode))
                {
                    // Clear Read
                }
                else if ("S*".Contains(clearCode))
                {
                    // Clear Resources
                }
                else if ("F*".Contains(clearCode))
                {
                    // Clear Fields
                }
                else if ("T*".Contains(clearCode))
                {
                    // Clear typeahead
                }
                else if (clearCode.Equals("E"))
                {
                    // Look for all read events flag and kill them
                    for (int i = Program.CurrentApp.AppLevels.Count - 1; i > 0; i--)
                    {
                        if (Program.CurrentApp.AppLevels[i].InReadEvents)
                        {
                            Program.CurrentApp.AppLevels[i].InReadEvents = false;

                            // If no flag, just do the one, for now
                            if (eCodes.Flags.Length == 0)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }



        /* TODO 
         * 
         * CLOSE [ALL | ALTERNATE | DATABASES [ALL] | DEBUGGER | FORMAT | INDEXES | PROCEDURE | TABLES [ALL]]
         */
        public static async Task<string> Close(ExecutorCodes eCodes)
        {
            try
            {
                string clearType = eCodes.SUBCMD;
                JAXObjects.Token clearName = new(string.Empty, "C");    // Set to empty string

                switch (clearType)
                {
                    case "all":    //All
                        break;

                    case "A":   // Alternate
                        // SET ALTERNATE OFF
                        // SET ALTERNATE TO
                        break;

                    case "D":   // Databases <ALL>
                        break;

                    case "E":   // Debugger
                        break;

                    case "F":   // Format
                        break;

                    case "I":   // Indexes
                        await CloseIDX(eCodes);
                        break;

                    case "M":
                        break;

                    case "P":   // Procedure
                        break;

                    case "":
                    case "T":   // Tables <ALL>

                        // If all flag is there, then all datasessions are affected.
                        int closeDS = Array.IndexOf(eCodes.Flags, "all") < 0 ? 0 : Program.CurrentApp.CurrentDataSession;

                        foreach (KeyValuePair<int, JAXDataSession> ds in Program.CurrentApp.jaxDataSession)
                        {
                            if (ds.Key > 0) // Skip the system datasession
                            {
                                if (closeDS == 0 || ds.Key == closeDS)
                                {
                                    foreach (KeyValuePair<int, JAXDirectDBF> wa in ds.Value.WorkAreas)
                                    {
                                        if (wa.Value is not null)
                                            await wa.Value.DBFClose();
                                    }
                                }
                            }

                            // Now release all datasessions > 1  if closeDS is zero
                            if (closeDS == 0)
                            {
                                // Go to datasession 1
                                Program.CurrentApp.SetDataSession(1);

                                // Remove all datasessions > 1
                                foreach (KeyValuePair<int, JAXDataSession> cds in Program.CurrentApp.jaxDataSession)
                                    if (cds.Key > 1) Program.CurrentApp.jaxDataSession.Remove(cds.Key);
                            }
                        }
                        break;

                    default:
                        throw new Exception(string.Format("Unknown clear comand {0}", clearType));
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }


        // Close one or more indexes
        public static async Task CloseIDX(ExecutorCodes eCodes)
        {
            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    JAXObjects.Token iName = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (iName.Element.Type.Equals("C"))
                    {
                        string stem = JAXLib.JustStem(iName.AsString());

                        if (string.IsNullOrWhiteSpace(stem) == false)
                        {
                            // We have an idx name.  See if it's open.
                            for (int j = 0; j < Program.CurrentApp.CurrentDS.WorkAreas.Count; j++)
                            {
                                JAXDirectDBF.DBFInfo dbf = Program.CurrentApp.CurrentDS.WorkAreas[j].DbfInfo;
                                if (dbf is not null && dbf.DBFStream != null)
                                {
                                    for (int k = 0; k < dbf.IDX.Count; k++)
                                    {
                                        if (dbf.IDX[k].Name.Equals(stem, StringComparison.OrdinalIgnoreCase) && dbf.IDX[k].IsRegistered == false)
                                        {
                                            // Close it
                                            await Program.CurrentApp.CurrentDS.WorkAreas[j].IDXClose(k);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
        }



        /*
         * Compile one or more files
         * 
         * COMPILE [FORM | CLASSLIB | LABEL | REPORT] cFileName | cFileSkeleton | ? [ALL]
         * 
         */
        public static async Task<string> Compile(ExecutorCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            int errcount = 0;

            try
            {
                Program.CurrentApp.InCompile = true;

                string cType = string.IsNullOrWhiteSpace(eCodes.SUBCMD) ? "P" : eCodes.SUBCMD;

                JAXObjects.Token answer = eCodes.Expressions.Count > 0 ? await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr) : throw new Exception("1|");
                if (answer.Element.Type.Equals("C") == false) throw new Exception("11|");

                string fName = answer.AsString();
                string fStem = JAXLib.JustStem(fName);
                string fExt = JAXLib.JustExt(fName);
                string fPath = JAXLib.JustFullPath(fName);
                string cCode = string.Empty;
                bool doAll = Array.IndexOf(eCodes.Flags, "all") >= 0;

                if (string.IsNullOrWhiteSpace(fExt))
                {
                    fExt = cType switch
                    {
                        "F" => "scx",
                        "C" => "vcx",
                        "P" => "prg",
                        "L" => "lbx",
                        "R" => "frx",
                        _ => throw new Exception("1999|" + cType)
                    };
                }

                if (string.IsNullOrWhiteSpace(fPath))
                    fPath = Program.CurrentApp.CurrentDS.JaxSettings.Default;

                string FQFN = fPath + fStem + "." + fExt;

                // TODO NOW - handle wildcards
                FilerLib.GetDirectory(FQFN, out string[] fileArray);

                for (int i = 0; i < fileArray.Length; i++)
                {
                    AppErrorHandling.ClearErrors();

                    FilerLib.GetFileInfo(fileArray[i], out string[] fileInfo);

                    // Does the file exists?
                    if (File.Exists(fPath + fileInfo[0]))
                    {
                        // Compile and return compiled filename to cCode
                        cCode = AppHelper.CompileModule(fPath + fileInfo[0], "P");

                        //Program.CurrentApp.lists.Decompile(fileInfo[0].Replace(".", "_"), JAXLib.FileToStr(cCode));

                        if (AppErrorHandling.ErrorCount() == 0)
                            AppIO.Talk("Compiled " + fileInfo[0].ToUpper() + " with no errors");
                        else
                        {
                            errcount += AppErrorHandling.ErrorCount();
                            AppIO.Talk(fileInfo[0].ToUpper() + $" has {AppErrorHandling.ErrorCount()} errors");
                        }
                    }
                    else
                        throw new Exception("1|" + fName);
                }
            }
            catch (Exception ex)
            {
                Program.CurrentApp.InCompile = false;
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            AppErrorHandling.ClearErrors();
            Program.CurrentApp.InCompile = false;

            return "";
        }


        public static string CompileIt(string FQFN, string cType)
        {
            string result = string.Empty;

            try
            {
                if (File.Exists(FQFN))
                {
                    if ("FPMCDRL".Contains(cType))
                        AppHelper.CompileModule(FQFN, "P");
                    else
                        throw new Exception("1999|" + cType);

                    if (AppErrorHandling.ErrorCount() == 0)
                        result = "Compiled " + FQFN + " with no errors";
                    else
                        throw new Exception("9997|" + FQFN);
                }
                else
                    throw new Exception("1|" + FQFN);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            finally
            {
                Program.CurrentApp.InCompile = false;
            }

            return result;
        }


        /* 
         * CONTINUE 
         */
        public static string Continue(ExecutorCodes eCodes)
        {
            string result = string.Empty;
            string loopType = AppLoop.GetLoopStack();

            string cFindMe = loopType[0] switch
            {
                'W' => Program.CurrentApp.MiscInfo["enddocmd"] + loopType,
                'U' => Program.CurrentApp.MiscInfo["enduntilcmd"] + loopType,
                'C' => Program.CurrentApp.MiscInfo["endcasecmd"] + loopType,
                'F' => Program.CurrentApp.MiscInfo["endforcmd"] + loopType,
                _ => throw new Exception("9999|CONTINUE|Unsupported loop type " + loopType[0].ToString())
            };


            // Find the endcase
            int pos = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx].IndexOf(cFindMe);

            if (pos < 0)
            {
                // Missing the end statement
                throw new Exception(loopType[0] switch
                {
                    'W' => "1209|",
                    'U' => "1210|",
                    'C' => "1213|",
                    'F' => "1207|",
                    _ => throw new Exception("9999|CONTINUE|Unsupported loop type " + loopType[0].ToString())
                });
            }
            else
            {
                Program.CurrentApp.utl.Conv64(pos, 3, out string pos2);
                result = "Y" + pos2; // Return the position of the endcase
            }

            return result;
        }


        /* TODO 
         * 
         * COPY TO FileName [DATABASE DatabaseName]
         *      [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         *      [Scope] [FOR lExpression1] [WHILE lExpression2] 
         *      [ [TYPE] [ FOXPLUS | FOX2X | DIF | MOD | SDF | CSV | XLS | XLSX | 
         *      DELIMITED [ WITH Delimiter | WITH BLANK  | WITH TAB | WITH CHARACTER <delimiter> ] ] ] 
         *      [AS nCodePage]
         */
        public static string Copy(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* TODO 
         * 
         * COUNT   [Scope] [FOR lExpression1] [WHILE lExpression2] [TO VarName] [NOOPTIMIZE]
         * 
         */
        public static string Count(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * CREATE
         * 
         */
        public static async Task<string> Create(ExecutorCodes eCodes)
        {
            switch (eCodes.SUBCMD.ToLower())
            {
                case "t":   // Table
                    eCodes.SUBCMD = "T";
                    await CreateTable(eCodes);
                    break;

                case "c":   // Cursor
                    eCodes.SUBCMD = "C";
                    await CreateTable(eCodes);
                    break;

                default:
                    throw new Exception("1999|Create " + eCodes.SUBCMD.ToUpper());
            }

            return "";
        }

        /* TODO PARTIAL - FROM ARRAY
         *      
         * CREATE TABLE cFile (cField cType [(nWidth [,nPrecision])] [, cField cType [(nWidth [,nPrecision])] [, cField...]) 
         *      
         * The create table command will strictly set up the table and fields
         * with no special settings allowed during the creation.
         * 
         * Use the ALTER, INDEX, and other table related commands to set the table
         * up the way you would with the CREATE command.
         * 
         */
        public static async Task<string> CreateTable(ExecutorCodes eCodes)
        {
            try
            {
                JAXObjects.Token answer = new();

                if (eCodes.Expressions.Count != 1) throw new Exception("10");
                answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                string TableName = answer.Element.Type.Equals("C") ? answer.AsString() : throw new Exception("11|");

                bool isArray = string.IsNullOrWhiteSpace(eCodes.From.Name) == false;
                List<JAXTables.FieldInfo> FieldInfo = [];

                if (isArray)
                {
                    AppIO.DebugLog($"Creating table {TableName} from array");

                    // Go through the array and collect the information
                    string arrayName;
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.From.Name);
                    if (answer.Element.Type.Equals("C"))
                        arrayName = answer.AsString();
                    else
                        throw new Exception("10|");

                    JAXObjects.Token fa = await AppVars.GetVarToken(arrayName);

                    if ((fa.Col == 4 || fa.Col > 17) == false)
                        throw new Exception("CREATE FROM ARRAY requires a two dimensional array with 4 or 18 columns");

                    for (int r = 0; r < fa.Row; r++)
                    {
                        JAXTables.FieldInfo f = new();
                        fa.ElementNumber = r * fa.Col;
                        f.FieldName = fa.Element.ValueAsString;

                        fa.ElementNumber = r * fa.Col + 1;
                        f.FieldType = fa.Element.ValueAsString;

                        fa.ElementNumber = r * fa.Col + 2;
                        f.FieldLen = fa.Element.ValueAsInt;

                        fa.ElementNumber = r * fa.Col + 3;
                        f.FieldDec = fa.Element.ValueAsInt;

                        if (fa.Col > 17)
                        {
                            // TODO - fill in the rest of the information
                            fa.ElementNumber = r * fa.Col + 4;
                            f.NullOK = fa.Element.ValueAsBool;          // Null values allowed

                            fa.ElementNumber = r * fa.Col + 5;
                            f.BinaryData = fa.Element.ValueAsBool;      // Code page not allowed

                            fa.ElementNumber = r * fa.Col + 6;
                            f.Valid = fa.Element.ValueAsString;         // Validation clause

                            fa.ElementNumber = r * fa.Col + 7;
                            f.ValidMessage = fa.Element.ValueAsString;  // Validation message

                            fa.ElementNumber = r * fa.Col + 8;
                            f.DefaultValue = fa.Element.ValueAsString;  // Default value - TODO!!!

                            fa.ElementNumber = r * fa.Col + 9;
                            // Table validation expression

                            fa.ElementNumber = r * fa.Col + 10;
                            // Table validation message

                            fa.ElementNumber = r * fa.Col + 11;
                            f.TableName = fa.Element.ValueAsString;     // Long table name

                            fa.ElementNumber = r * fa.Col + 12;
                            // insert trigger

                            fa.ElementNumber = r * fa.Col + 13;
                            // Update trigger

                            fa.ElementNumber = r * fa.Col + 14;
                            // Delete trigger

                            fa.ElementNumber = r * fa.Col + 15;
                            f.Comment = fa.Element.ValueAsString;        // Comment

                            fa.ElementNumber = r * fa.Col + 16;
                            f.AutoIncNext = fa.Element.ValueAsInt;      // Next auto inc value

                            fa.ElementNumber = r * fa.Col + 17;
                            f.AutoIncStep = fa.Element.ValueAsInt;      // Next auto inc step
                        }

                        FieldInfo.Add(f);
                    }
                }
                else
                {
                    AppIO.DebugLog($"Creating table {TableName} from expression list", Program.CurrentApp.CurrentDS.JaxSettings.Talk == false);
                    string[] expr = eCodes.TABLE.Split(AppClass.expDelimiter);

                    for (int i = 0; i < expr.Length; i++)
                    {
                        AppIO.DebugLog($"Processing field {i + 1} expression {expr[i]}", Program.CurrentApp.CurrentDS.JaxSettings.Talk == false);

                        string[] fld = expr[i].Split(AppClass.expParam);

                        if (string.IsNullOrWhiteSpace(fld[0]) == false)
                        {
                            answer = await Program.CurrentApp.SolveFromRPNString(fld[0]);
                            if (answer.Element.Type.Equals("C")) fld[0] = answer.AsString(); else throw new Exception("11|");

                            answer = await Program.CurrentApp.SolveFromRPNString(fld[1]);
                            if (answer.Element.Type.Equals("C")) fld[1] = answer.AsString(); else throw new Exception("11|");

                            int width = 0;
                            int dec = 0;

                            if (fld.Length > 2 && string.IsNullOrWhiteSpace(fld[2]) == false)
                            {
                                answer = await Program.CurrentApp.SolveFromRPNString(fld[2]);
                                if (answer.Element.Type.Equals("N")) width = answer.AsInt(); else throw new Exception("11|");
                            }

                            if (fld.Length > 3 && string.IsNullOrWhiteSpace(fld[3]) == false)
                            {
                                answer = await Program.CurrentApp.SolveFromRPNString(fld[3]);
                                if (answer.Element.Type.Equals("N")) dec = answer.AsInt(); else throw new Exception("11|");
                            }

                            JAXTables.FieldInfo f = new()
                            {
                                FieldName = fld[0],
                                FieldType = fld[1].ToUpper().Trim(),
                                FieldLen = width,
                                FieldDec = dec
                            };

                            FieldInfo.Add(f);
                        }
                    }
                }

                string name = JAXLib.JustStem(TableName);
                string ext = JAXLib.JustExt(TableName);
                string path = JAXLib.JustFullPath(TableName);
                string fqfn = string.Empty;

                ext = string.IsNullOrWhiteSpace(ext) ? "dbf" : ext;

                fqfn = name + "." + ext;
                fqfn = AppHelper.FixFileCase(string.Empty, fqfn, Program.CurrentApp.CurrentDS.JaxSettings.Naming, Program.CurrentApp.CurrentDS.JaxSettings.NamingAll);

                // Ensure we have a path
                if (string.IsNullOrWhiteSpace(path))
                    path = AppHelper.FindPathForFile(name + "." + ext);

                path = string.IsNullOrWhiteSpace(path) ? Program.CurrentApp.CurrentDS.JaxSettings.Default : path;
                fqfn = AppHelper.FixFileCase(path, fqfn, Program.CurrentApp.CurrentDS.JaxSettings.Naming, Program.CurrentApp.CurrentDS.JaxSettings.NamingAll);

                // if no FQFN info, add the default
                if (path.Contains('\\') == false && path.Contains(':') == false)
                    fqfn = Program.CurrentApp.CurrentDS.JaxSettings.Default + fqfn + ".dbf";
                else
                {
                    if (fqfn.Length > 2)
                    {
                        if (fqfn[..2].Equals("\\\\") == false && fqfn[1] != ':' && fqfn[0] != '.')
                            fqfn = Program.CurrentApp.CurrentDS.JaxSettings.Default + fqfn + ".dbf";
                    }
                }

                // Default is to overwrite
                bool overwrite = true;

                // If safety is on, check for and ask if it exists
                if (File.Exists(fqfn) && Program.CurrentApp.CurrentDS.JaxSettings.Safety)
                {
                    // Ask if ok to overwrite
                    DialogResult dr = MessageBox.Show(string.Format("Overwrite table {0}", fqfn), "WARNING", MessageBoxButtons.YesNo);
                    overwrite = dr == DialogResult.Yes;
                }

                // Call the Creation routine
                JAXDirectDBF.DBFInfo dbfInfo = new()
                {
                    Fields = FieldInfo,
                    TableName = TableName,
                    FQFN = fqfn,
                    TableType = "CTV".Contains(eCodes.SUBCMD, StringComparison.CurrentCultureIgnoreCase) ? eCodes.SUBCMD.ToUpper() : "T"
                };

                if (await Program.CurrentApp.CurrentDS.CurrentWA.DBFCreateDBF(dbfInfo, overwrite))
                    AppIO.Talk(TableName.ToUpper() + " created");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }
    }
}
