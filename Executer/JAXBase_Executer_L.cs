using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_L
    {
        /*
         * 
         * LPARAMETERS
         * 
         */
        public static async Task<string> LParameters(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            try
            {
                // Is this the first executed command of the program?
                if (Program.CurrentApp.AppLevels.Count == 0) throw new Exception("2|");
                if (JAXLib.InList(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LastCommand, -1, jbe.CmdNum["procedure"], jbe.CmdNum["*sc"]) == false) throw new Exception("8|");

                // Break out the var expressions
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                    AppVars.MakeLocalVar(var.varName, var.row, var.col, true);
                    AppVars.SetVarOrMakePrivate(var.varName, var.row, var.col, false);

                    string type = eCodes.As[i];

                    // Set the var as this type
                    if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                        await AppVars.SetAsType(var.varName, type);

                    if (Program.CurrentApp.ParameterClassList.Count > 0)
                    {
                        JAXObjects.Token tk = await AppHelper.GetParameterToken(null);
                        if (string.IsNullOrWhiteSpace(type) || tk.Element.Type.Equals(type))
                        {
                            AppIO.DebugLog($"LPARAMETER created {var.varName} = {tk.AsString()} via type {type}");
                            AppVars.SetVar(var.varName, tk);
                        }
                        else
                            throw new Exception("1732|");

                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            Program.CurrentApp.ParameterClassList.Clear();
            return string.Empty;
        }


        /*
         * 
         * LOCAL var1 [AS Type1][, var2 AS Type...]
         * 
         */
        public static async Task<string> Local(ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                        AppVars.MakeLocalVar(var.varName, var.row, var.col, true);

                        string type = eCodes.As[i];

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                            await AppVars.SetAsType(var.varName, type);
                    }
                    else
                        throw new Exception("11|");
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
         * LOCATE [FOR lExpression1] [Scope] [WHILE lExpression2] [IN nExpr | cAlias [SESSION nDataSession]] [NOOPTIMIZE]
         * 
         */
        public static async Task<string> Locate(ExecuterCodes eCodes)
        {
            string result = string.Empty;
            int cwa = Program.CurrentApp.CurrentDS.CurrentWorkArea();   // Starting workarea
            int cds = Program.CurrentApp.CurrentDataSession;
            JAXObjects.Token answer = new();

            try
            {
                if (eCodes.SESSION > 0)
                {
                    // Set the new data session
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);
                }

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? cwa : (await Program.CurrentApp.SolveFromRPNString(eCodes.InExpr)).Element.Value;
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (Program.CurrentApp.CurrentDS.CurrentWA is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", Program.CurrentApp.CurrentDS.CurrentWorkArea()));

                JAXDirectDBF Table = Program.CurrentApp.CurrentDS.CurrentWA;

                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, Table, true);

                int scopeCount = jaxScope.UntilFlag;
                int recNo = 0;

                Table.DbfInfo.Found = false;

                while (Table.DbfInfo.DBFEOF == false && scopeCount != 0)
                {
                    // Solve the WHILE expresion and exit if false
                    answer.Element.Value = string.IsNullOrWhiteSpace(eCodes.WhileExpr) ? true : (await Program.CurrentApp.SolveFromRPNString(eCodes.WhileExpr)).Element.Value;
                    if (answer.Element.Type.Equals("L") == false) throw new Exception("11|");
                    if (answer.AsBool() == false)
                        break;

                    // Solve the FOR expresion & skip if false, break if true (found)
                    answer.Element.Value = string.IsNullOrWhiteSpace(eCodes.ForExpr) ? true : (await Program.CurrentApp.SolveFromRPNString(eCodes.ForExpr)).Element.Value;
                    if (answer.Element.Type.Equals("L") == false) throw new Exception("11|");
                    if (answer.AsBool())
                    {
                        recNo = Table.DbfInfo.RecNo;
                        Table.DbfInfo.Found = true;
                        break;
                    }

                    // Are we out of scope?
                    if (jaxScope.IsDone())
                        break;

                    await Table.DBFSkipRecord(1, true);
                }

                // set up for success and failure
                if (Table.DbfInfo.Found == false)
                {
                    // If not found - set the environment correctly
                    await Table.DBFGotoRecord("bottom");
                    await Table.DBFSkipRecord(1);
                    Table.DbfInfo.LastLocate = null;
                }
                else
                    Table.DbfInfo.LastLocate = eCodes;  // Remember this locate in case they CONTINUE
            }
            catch (Exception ex)
            {
                result = string.Empty;
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // Return to the starting workarea
            Program.CurrentApp.CurrentDS.SelectWorkArea(cwa);
            return result;
        }

        /*
         * 
         * LOOP
         * 
         */
        public static string Loop( ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                string PrgCode = Program.CurrentApp.PRGCache.Count > 0 ? Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx] : string.Empty;

                // What loop are we currently in?
                string loopType = AppLoop.PopLoopStack();
                string loop = string.Empty;

                switch (loopType[0])
                {
                    case 'S':   // SCAN
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endscancmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                        break;

                    case 'W':   // WHILE
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["enddocmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                        break;

                    case 'F':   // FOR
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endforcmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                        break;

                    case 'U':   // UNTIL
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["untilcmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                        break;

                    default:    // ERROR
                        throw new Exception("Unsupported loop type " + loopType[0]);
                }

                int pos = PrgCode.IndexOf(loop);

                if (pos < 0)
                    switch (loopType[0])
                    {
                        case 'S':   // SCAN
                            throw new Exception("1203|");

                        case 'W':   // WHILE
                            throw new Exception("1209|");

                        case 'F':   // FOR
                            throw new Exception("1207|");

                        case 'U':   // UNTIL
                            throw new Exception("1210|");
                    }
                else
                {
                    Program.CurrentApp.utl.Conv64(++pos, 3, out result);
                    result = "X" + result;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }


            return result;
        }


        /* TODO NOW
         * 
         * LPROCEDURE
         * 
         */
        public static string LProcedure(string cmdRest)
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
    }
}
