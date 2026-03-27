using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities.Utilities;
using JAXBase.XBase;
using ZXing.Aztec.Internal;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_S
    {


        /* TODO
         * 
         * SAVE
         * 
         */
        public static string Save(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * 
         * SCAN [NOOPTIMIZE] [Scope] [FOR lExpression1] [WHILE lExpression2]
         * 
         */
        public static async Task<string> Scan(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            JAXObjects.Token answer = new();
            bool ForOK = true;
            bool WhileOK = true;
            bool EOF = false;

            try
            {
                if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");

                LoopClass loop;
                bool firstTime = false;

                if (jbe.App.GetLoopStack().Equals(eCodes.SUBCMD))
                {
                    // Already in this loop, so grab the loop info
                    loop = jbe.App.AppLevels[^1].ScanLoops[eCodes.SUBCMD];
                    jbe.App.SetDataSession(loop.DataSession);
                    jbe.App.CurrentDS.SelectWorkArea(loop.WorkArea);
                }
                else
                {
                    // First time through, so set the loop stack
                    firstTime = true;
                    jbe.App.PushLoop(eCodes.SUBCMD);
                    loop = new()
                    {
                        DataSession = jbe.App.CurrentDataSession,
                        WorkArea = jbe.App.CurrentDS.CurrentWorkArea(),
                        RecordCounter = 0,
                        Scope = new()
                    };

                    // Set up the scope of the loop
                    await loop.Scope.Setup(eCodes.Scope, jbe.App.CurrentDS.CurrentWA, true);



                    jbe.App.AppLevels[^1].ScanLoops.Add(eCodes.SUBCMD, loop);
                    await jbe.App.CurrentDS.CurrentWA.DBFGotoRecord("top");
                }

                // Process the FOR expression
                if (string.IsNullOrEmpty(eCodes.ForExpr) == false)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.ForExpr);
                    if (answer.Element.Type.Equals("L") == false)
                        throw new Exception("11||FOR expression must be logical");

                    ForOK = answer.AsBool();
                }

                // Process the WHILE expression
                if (answer.AsBool() && string.IsNullOrEmpty(eCodes.WhileExpr) == false)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.WhileExpr);

                    if (answer.Element.Type.Equals("L") == false)
                        throw new Exception("11||WHILE expression must be logical");

                    WhileOK = answer.AsBool();
                }

                if (WhileOK)
                {
                    if (ForOK)
                    {
                        if (jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFEOF == false && jbe.App.CurrentDS.CurrentWA.DbfInfo.RecCount > 0)
                        {

                            if (firstTime == false)
                            {
                                await jbe.App.CurrentDS.CurrentWA.DBFSkipRecord(1);

                                // Did we go past the end of file?
                                EOF = jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFEOF;
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
                    string PrgCode = jbe.App.PRGCache.Count > 0 ? jbe.App.PRGCache[jbe.App.AppLevels[^1].PRGCacheIdx] : string.Empty;
                    string endscan = AppClass.cmdByte + jbe.App.MiscInfo["endscancmd"] + eCodes.SUBCMD + AppClass.cmdEnd;
                    int pos = PrgCode.IndexOf(endscan);
                    pos = PrgCode.IndexOf(AppClass.cmdEnd, pos);

                    if (pos < 0)
                        throw new Exception("Mismatched SCAN/ENDSCAN");
                    else
                    {
                        jbe.App.utl.Conv64(++pos, 3, out result);
                        result = "Y" + result;
                    }
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO NOW
         * 
         * SCATTER [FIELDS FieldNameList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton] [MEMO] [BLANK] TO ArrayName | TO ArrayName | MEMVAR | NAME ObjectName [ADDITIVE]
         * 
         */
        public static string Scatter(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {

            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }



        /*
         * 
         * SEEK eExpression [ORDER nIndexNumber | cIndexName] [ASCENDING | DESCENDING] [IN nWorkArea | cAlias] [SESSION nSession]
         * 
         */
        public static async Task<string> Seek(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            // where are we?
            int ds = app.CurrentDataSession;
            int wa = app.CurrentDS.CurrentWorkArea();

            try
            {
                string[] stmt = cmdRest.Split(AppClass.stmtDelimiter);
                if (stmt.Length != 4)
                    throw new Exception("10||Expect 4 statement parts");

                // Break the statement into it's pars
                JAXObjects.Token orderExpr = new();
                JAXObjects.Token sortExpr = new("A", "C");
                JAXObjects.Token waExpr = new();

                GenericClass gc = await JAXBase_Executer_M.SolveFromRPNString(app, stmt[0]);
                JAXObjects.Token SeekExpr = new();
                SeekExpr.CopyFrom(gc.Value);

                if (string.IsNullOrEmpty(stmt[1]))
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, stmt[1]);
                    orderExpr.CopyFrom(gc.Value);
                    if ("NC".Contains(orderExpr.Element.Type) == false)
                        throw new Exception("11||Invalid order value");
                }

                if (string.IsNullOrEmpty(stmt[2]))
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, stmt[2]);
                    sortExpr.CopyFrom(gc.Value);

                    if ("descending".StartsWith(orderExpr.AsString(), StringComparison.OrdinalIgnoreCase) == false
                        && "ascending".StartsWith(orderExpr.AsString(), StringComparison.OrdinalIgnoreCase) == false)
                        throw new Exception("10||Unknown sort order " + orderExpr.AsString());
                }

                if (string.IsNullOrEmpty(stmt[3]))
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, stmt[3]);
                    waExpr.CopyFrom(gc.Value);

                    if ("NC".Contains(waExpr.Element.Type) == false)
                        throw new Exception("11||Invalid workarea value");
                }

                // Select the workarea for the search
                if (waExpr.Element.Type.Equals("N"))
                    app.CurrentDS.SelectWorkArea(waExpr.AsInt());
                else
                    app.CurrentDS.SelectWorkArea(waExpr.AsString());

                JAXDirectDBF.IDXCommand cmd = new();
                int idx = 0;

                if (orderExpr.Element.Type.Equals("C"))
                {
                    List<JAXDirectDBF.IDXInfo> list = await app.CurrentDS.CurrentWA.IDXGetInfoList(orderExpr.AsString(), string.Empty);
                    if (list.Count == 0) throw new Exception("1683||Cannot find index " + orderExpr.AsString());
                    idx = list[0].IDXListPos;
                }

                cmd = await app.CurrentDS.CurrentWA.IDXSearch(idx, SeekExpr.Element.Value, 0, sortExpr.AsString()[0] != 'D', true);
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // Go back to current workarea
            app.CurrentDS.SelectWorkArea(wa);

            return result;
        }


        /*
         * 
         * SELECT nExpr|cExpr SESSION nExpr
         * 
         */
        public static async Task<string> Select(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            jbe.App.ClearErrors();

            try
            {
                if (eCodes.SESSION > 0)
                    jbe.App.SetDataSession(eCodes.SESSION);

                JAXObjects.Token tk = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                switch (tk.Element.Type)
                {
                    case "N":
                        jbe.App.CurrentDS.SelectWorkArea(tk.AsInt());
                        break;

                    case "C":
                        jbe.App.CurrentDS.SelectWorkArea(tk.AsString());
                        break;

                    default:
                        throw new Exception("11|");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }



        /* TODO
         * 
         * SELECT (SQL)
         * 
         */
        public static string SelectSQL(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {

            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }



        /*
         * 
         * SKIP [nExpr | TOP | BOTTOM] [IN nWorkArea | cAlias [SESSION nSessionID]]
         * 
         */
        public static async Task<string> Skip(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            // Get the current workarea
            int ds = jbe.App.CurrentDataSession;
            int wa = jbe.App.CurrentDS.CurrentWorkArea();

            try
            {
                // Got to the desired data session
                if (eCodes.SESSION > 0)
                    jbe.App.SetDataSession(eCodes.SESSION);

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : jbe.App.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    jbe.App.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    jbe.App.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                // Get the nExpr | TOP | BOTTOM
                JAXObjects.Token answer = new();
                if (eCodes.Expressions.Count < 1)
                    answer.Element.Value = 1;   // Default is forward 1 record
                else
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr); // Get requested value

                if (answer.Element.Type.Equals("N"))
                {
                    // Skip the desired number of records forward or backward
                    await jbe.App.CurrentDS.CurrentWA.DBFSkipRecord(answer.AsInt());
                }
                else if (answer.Element.Type.Equals("C") && JAXLib.InListC(answer.AsString(), "top", "bottom"))
                {
                    if (answer.AsString().Equals("top", StringComparison.OrdinalIgnoreCase))
                        await jbe.App.CurrentDS.CurrentWA.DBFGotoRecord("top");
                    else
                        await jbe.App.CurrentDS.CurrentWA.DBFGotoRecord("bottom");
                }
                else
                    throw new Exception("11|");
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            finally
            {
                jbe.App.SetDataSession(ds); // Restore data session
                jbe.App.CurrentDS.SelectWorkArea(wa); // Restore workarea
            }

            return string.Empty;
        }


        /*
         * 
         * SORT
         * 
         */
        public static string Sort(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * 
         * STORE eExpression1 [,eExpression2...] TO cVar1 [, cVar2...]
         * 
         */
        public static async Task<string> Store(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                // basic sanity checks
                if (eCodes.Expressions.Count < 1) throw new Exception("10|");

                if (eCodes.To.Count > 0 && eCodes.To[0].Name.Contains("btndel.cap", StringComparison.OrdinalIgnoreCase))
                {
                    int iii = 0;
                }

                // get the expression to store to the var list
                JAXObjects.Token ExprValue = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                for (int i = 0; i < eCodes.To.Count; i++)
                {
                    // Get the VarName literal or (expression)
                    JAXObjects.Token varName = await jbe.App.SolveFromRPNString(eCodes.To[i].Name);

                    // Make sure we have a character expression and there's something in it
                    if (varName.Element.Type.Equals("C") && string.IsNullOrWhiteSpace(varName.AsString()) == false)
                    {
                        // Get the var name to which we're storing the value
                        JAXObjects.Token tk = await jbe.App.GetVarFromExpression(varName.AsString(), null);

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
                                await jbe.App.SetVarFromExpression(varName.AsString(), ExprValue.Element.Value, true);
                            }
                        }
                    }
                    else
                        throw new Exception("10|");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * SUSPEND
         * 
         */
        public static string Suspend(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
