using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_S
    {
        const string thisFile = "JAXBase_Executor_S";

        /* TODO
         * 
         * SAVE
         * 
         */
        public static string Save(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Save", ex.Message);
            }

            return result;
        }


        /*
         * 
         * SCAN [NOOPTIMIZE] [Scope] [FOR lExpression1] [WHILE lExpression2]
         * 
         */
        public static async Task<string> Scan(ExecutorCodes eCodes)
        {
            string result = string.Empty;
            JAXObjects.Token answer = new();
            bool ForOK = true;
            bool WhileOK = true;
            bool EOF = false;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                LoopClass loop;
                bool firstTime = false;

                if (AppLoop.GetLoopStack().Equals(eCodes.SUBCMD))
                {
                    // Already in this loop, so grab the loop info
                    loop = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ScanLoops[eCodes.SUBCMD];
                    Program.CurrentApp.SetDataSession(loop.DataSession);
                    Program.CurrentApp.CurrentDS.SelectWorkArea(loop.WorkArea);
                }
                else
                {
                    // First time through, so set the loop stack
                    firstTime = true;
                    AppLoop.PushLoop(eCodes.SUBCMD);
                    loop = new()
                    {
                        DataSession = Program.CurrentApp.CurrentDataSession,
                        WorkArea = Program.CurrentApp.CurrentDS.CurrentWorkArea(),
                        RecordCounter = 0,
                        Scope = new()
                    };

                    // Set up the scope of the loop
                    await loop.Scope.Setup(eCodes.Scope, Program.CurrentApp.CurrentDS.CurrentWA, true);



                    Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ScanLoops.Add(eCodes.SUBCMD, loop);
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("top");
                }

                // Process the FOR expression
                if (string.IsNullOrEmpty(eCodes.ForExpr) == false)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.ForExpr);
                    if (answer.Element.Type.Equals("L") == false)
                        throw new Exception("11||FOR expression must be logical");

                    ForOK = answer.AsBool();
                }

                // Process the WHILE expression
                if (answer.AsBool() && string.IsNullOrEmpty(eCodes.WhileExpr) == false)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.WhileExpr);

                    if (answer.Element.Type.Equals("L") == false)
                        throw new Exception("11||WHILE expression must be logical");

                    WhileOK = answer.AsBool();
                }

                if (WhileOK)
                {
                    if (ForOK)
                    {
                        if (Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFEOF == false && Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.RecCount > 0)
                        {

                            if (firstTime == false)
                            {
                                await Program.CurrentApp.CurrentDS.CurrentWA.DBFSkipRecord(1);

                                // Did we go past the end of file?
                                EOF = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFEOF;
                            }
                        }
                    }
                }

                if (WhileOK && ForOK && EOF && loop.Scope!.IsDone() == false)
                {
                    // We're good to go to the next record
                }
                else
                {
                    // Done with SCAN.  Find the ENDSCAN and instruct
                    // JAXBase to proceed to next command
                    string PrgCode = Program.CurrentApp.PRGCache.Count > 0 ? Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx] : string.Empty;
                    string endscan = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endscancmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                    int pos = PrgCode.IndexOf(endscan);
                    pos = PrgCode.IndexOf(AppClass.cmdEnd, pos);

                    if (pos < 0)
                        throw new Exception("Mismatched SCAN/ENDSCAN");
                    else
                    {
                        Program.CurrentApp.utl.Conv64(++pos, 3, out result);
                        result = "Y" + result;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Scan", ex.Message);
            }

            return result;
        }


        /* TODO NOW
         * 
         * SCATTER [FIELDS FieldNameList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton] [MEMO] [BLANK] TO ArrayName | TO ArrayName | MEMVAR | NAME ObjectName [ADDITIVE]
         * 
         */
        public static string Scatter(string cmdRest)
        {
            string result = string.Empty;

            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Scatter", ex.Message);
            }

            return result;
        }



        /*
         * 
         * SEEK eExpression [ORDER nIndexNumber | cIndexName] [ASCENDING | DESCENDING] [IN nWorkArea | cAlias] [SESSION nSession]
         * 
         */
        public static async Task<string> Seek(string cmdRest)
        {
            string result = string.Empty;

            // where are we?
            int ds = Program.CurrentApp.CurrentDataSession;
            int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

            try
            {
                string[] stmt = cmdRest.Split(AppClass.stmtDelimiter);
                if (stmt.Length != 4)
                    throw new Exception("10||Expect 4 statement parts");

                // Break the statement into it's pars
                JAXObjects.Token orderExpr = new();
                JAXObjects.Token sortExpr = new("A", "C");
                JAXObjects.Token waExpr = new();

                JAXObjects.Token gc = await Program.CurrentApp.SolveFromRPNString(stmt[0]);
                JAXObjects.Token SeekExpr = new();
                SeekExpr.CopyFrom(gc);

                if (string.IsNullOrEmpty(stmt[1]))
                {
                    gc = await Program.CurrentApp.SolveFromRPNString(stmt[1]);
                    orderExpr.CopyFrom(gc);
                    if ("NC".Contains(orderExpr.Element.Type) == false)
                        throw new Exception("11||Invalid order value");
                }

                if (string.IsNullOrEmpty(stmt[2]))
                {
                    gc = await Program.CurrentApp.SolveFromRPNString(stmt[2]);
                    sortExpr.CopyFrom(gc);

                    if ("descending".StartsWith(orderExpr.AsString(), StringComparison.OrdinalIgnoreCase) == false
                        && "ascending".StartsWith(orderExpr.AsString(), StringComparison.OrdinalIgnoreCase) == false)
                        throw new Exception("10||Unknown sort order " + orderExpr.AsString());
                }

                if (string.IsNullOrEmpty(stmt[3]))
                {
                    gc = await Program.CurrentApp.SolveFromRPNString(stmt[3]);
                    waExpr.CopyFrom(gc);

                    if ("NC".Contains(waExpr.Element.Type) == false)
                        throw new Exception("11||Invalid workarea value");
                }

                // Select the workarea for the search
                if (waExpr.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(waExpr.AsInt());
                else
                    Program.CurrentApp.CurrentDS.SelectWorkArea(waExpr.AsString());

                JAXDirectDBF.IDXCommand cmd = new();
                int idx = 0;

                if (orderExpr.Element.Type.Equals("C"))
                {
                    List<JAXDirectDBF.IDXInfo> list = await Program.CurrentApp.CurrentDS.CurrentWA.IDXGetInfoList(orderExpr.AsString(), string.Empty);
                    if (list.Count == 0) throw new Exception("1683||Cannot find index " + orderExpr.AsString());
                    idx = list[0].IDXListPos;
                }

                cmd = await Program.CurrentApp.CurrentDS.CurrentWA.IDXSearch(idx, SeekExpr.Element.Value, 0, sortExpr.AsString()[0] != 'D', true);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Seek", ex.Message);
            }

            // Go back to current workarea
            Program.CurrentApp.CurrentDS.SelectWorkArea(wa);

            return result;
        }


        /*
         * 
         * SELECT nExpr|cExpr SESSION nExpr
         * 
         */
        public static async Task<string> Select(ExecutorCodes eCodes)
        {
            string result = string.Empty;
            AppErrorHandling.ClearErrors();

            try
            {
                if (eCodes.SESSION > 0)
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);

                JAXObjects.Token tk = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                switch (tk.Element.Type)
                {
                    case "N":
                        Program.CurrentApp.CurrentDS.SelectWorkArea(tk.AsInt());
                        break;

                    case "C":
                        Program.CurrentApp.CurrentDS.SelectWorkArea(tk.AsString());
                        break;

                    default:
                        throw new Exception("11|");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Select", ex.Message);
            }

            return result;
        }



        /* TODO
         * 
         * SELECT (SQL)
         * 
         */
        public static string SelectSQL(ExecutorCodes eCodes)
        {
            string result = string.Empty;

            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.SelectSQL", ex.Message);
            }

            return result;
        }



        /*
         * 
         * SKIP [nExpr | TOP | BOTTOM] [IN nWorkArea | cAlias [SESSION nSessionID]]
         * 
         */
        public static async Task<string> Skip(ExecutorCodes eCodes)
        {
            // Get the current workarea
            int ds = Program.CurrentApp.CurrentDataSession;
            int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

            try
            {
                // Got to the desired data session
                if (eCodes.SESSION > 0)
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                // Get the nExpr | TOP | BOTTOM
                JAXObjects.Token answer = new();
                if (eCodes.Expressions.Count < 1)
                    answer.Element.Value = 1;   // Default is forward 1 record
                else
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr); // Get requested value

                if (answer.Element.Type.Equals("N"))
                {
                    // Skip the desired number of records forward or backward
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFSkipRecord(answer.AsInt());
                }
                else if (answer.Element.Type.Equals("C") && JAXLib.InListC(answer.AsString(), "top", "bottom"))
                {
                    if (answer.AsString().Equals("top", StringComparison.OrdinalIgnoreCase))
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("top");
                    else
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("bottom");
                }
                else
                    throw new Exception("11|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Skip", ex.Message);
            }
            finally
            {
                Program.CurrentApp.SetDataSession(ds); // Restore data session
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa); // Restore workarea
            }

            return string.Empty;
        }


        /*
         * 
         * SORT
         * 
         */
        public static string Sort(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Sort", ex.Message);
            }

            return result;
        }


        /*
         * 
         * STORE eExpression1 [,eExpression2...] TO cVar1 [, cVar2...]
         * 
         */
        public static async Task<string> Store(ExecutorCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                // basic sanity checks
                if (eCodes.Expressions.Count < 1) throw new Exception("10|");

                // get the expression to store to the var list
                JAXObjects.Token ExprValue = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                if ("EM".Contains(ExprValue.TType))
                    ExprValue = new("");

                for (int i = 0; i < eCodes.To.Count; i++)
                {
                    // Get the VarName literal or (expression)
                    JAXObjects.Token varName = await Program.CurrentApp.SolveFromRPNString(eCodes.To[i].Name);

                    // Make sure we have a character expression and there's something in it
                    if (varName.Element.Type.Equals("C") && string.IsNullOrWhiteSpace(varName.AsString()) == false)
                    {
                        // Get the var name to which we're storing the value
                        JAXObjects.Token tk = await AppVars.GetVarFromExpression(varName.AsString(), null);

                        // Are we trying to save to an unknown object.property?
                        if (tk.TType.Equals("X") == false)
                        {
                            // Nope, we're good.  So is it a table reference?
                            if (string.IsNullOrWhiteSpace(tk.Alias) == false)
                            {
                                // Illegal assignment to table
                                throw new Exception("1778|");
                            }

                            // How about an array
                            if (ExprValue.TType.Equals("A"))
                            {
                                // Yes, an array
                                tk.SetDimension(ExprValue.Row, ExprValue.Col, true);
                                for (int j = 0; j < tk._avalue.Count; j++)
                                    tk._avalue[j].Value = ExprValue._avalue[j].Value;
                            }
                            else
                            {
                                // Set the varName to the Expression Value with CreatePrivateVar set to true
                                // so it will create the variable if it's not found.
                                await AppVars.SetVarFromExpression(varName.AsString(), ExprValue.Element.Value, true);
                            }
                        }
                    }
                    else
                        throw new Exception("10|");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Store", ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * SUSPEND
         * 
         */
        public static string Suspend(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Suspend", ex.Message);
            }

            return result;
        }
    }
}
