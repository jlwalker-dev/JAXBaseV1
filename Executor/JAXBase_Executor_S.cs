using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

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
        public static string Save(ExecutorCodes eCodes)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Save", ex.Message);
            }

            return "";
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
        public static async Task<string> Scatter(ExecutorCodes eCodes)
        {
            try
            {
                if (Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception("52||Scatter");

                if (eCodes.To.Count != 1) throw new Exception("10||Scatter - must have exactly one TO target");

                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.To[0].Type);
                if (answer.Element.Type.Equals("C") == false) throw new Exception("11||Scatter");
                string toType = answer.AsString();
                string toName = eCodes.From.Name;


                JAXObjectWrapper toObject = new(Program.CurrentApp, "empty", "", []);
                Dictionary<string, object>? toJson = [];
                JAXObjects.Token toArray = new()
                {
                    Col = 1,
                    Row = 0,
                    TType = "A"
                };

                JAXObjects.Token toVar = await AppVars.GetVarToken(toName);

                // If the toVar does not exist, create it
                if (toVar.TType.Equals("U"))
                {
                    AppVars.SetVarOrMakePrivate(toName, new());
                    toVar = await AppVars.GetVarToken(toName);
                }


                if (eCodes.Flags.Contains("additive"))
                {
                    if (toType.Equals("A"))
                    {
                        // Is this an array with something in it?
                        if (toVar.TType.Equals("A") == false)
                        {
                            toVar.TType = "A";
                            toVar.Col = 1;
                            toVar.Row = 0;
                        }

                        toArray = toVar;
                    }
                    else if (toType.Equals("N"))
                    {
                        toObject = answer.Element.Value as JAXObjectWrapper ?? new(Program.CurrentApp, "empty", "", []);
                    }
                    else if (toType.Equals("J"))
                    {
                        // Is this a JSON var with something in it?
                        if (answer.Element.Type.Equals("C"))
                        {

                            JObject jObj = JObject.Parse(answer.AsString());

                            if (jObj is not null)
                            {
                                Dictionary<string, object>? testJson = [];
                                testJson = jObj.ToObject<Dictionary<string, object>>();

                                if (testJson is not null)
                                    toJson = jObj.ToObject<Dictionary<string, object>>();

                                testJson = [];
                            }
                        }
                    }
                }

                string isLike = "";
                string isNotLike = "";
                string[] fields = [];
                bool memoOK = eCodes.Flags.Contains("memo");
                bool blank = eCodes.Flags.Contains("blank");

                // Copy the current row
                DataRow thisRow = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.CurrentRow.NewRow();
                thisRow.ItemArray = (object[])Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.CurrentRow.Rows[0].ItemArray.Clone();

                // Now move the fields to the target object
                int arrayPtr = 0;
                int fieldCount = 0;
                bool copyField = false;

                for (int i = 0; i < Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields.Count; i++)
                {
                    JAXTables.FieldInfo fieldInfo = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields[i];
                    string fieldName = fieldInfo.FieldName;
                    string fieldType = fieldInfo.FieldType;
                    object? fieldValue = thisRow[fieldName];

                    // Is it a system column?
                    if (fieldInfo.SystemColumn == false)
                    {
                        fieldCount++;

                        // No, so we may be updating it
                        if (string.IsNullOrWhiteSpace(isLike) == false)
                        {
                            // We have a like condition, so we need to check it
                            if (fieldName.Equals(isLike))
                            {
                            }
                        }
                        else if (string.IsNullOrWhiteSpace(isNotLike) == false)
                        {
                            // We have a not like condition, so we need to check it
                            if (fieldName.Equals(isNotLike))
                            {
                            }
                        }
                        else if (fields.Contains(fieldName))
                        {
                        }
                        else
                        {
                            // No conditions, so we update it
                            copyField = true;
                        }

                        answer = new();


                        if (copyField)
                        {
                            if (eCodes.Flags.Contains("blank"))
                            {
                                // Check the type and if it passes, save it, otherwise throw an error
                                if ("BNFYI".Contains(fieldInfo.FieldType))
                                {
                                    // Numeric field
                                    if (blank)
                                        fieldValue = 0.00;
                                }
                                else if ("CVQMGW".Contains(fieldInfo.FieldType))
                                {
                                    // Character field
                                    if (blank)
                                        fieldValue = "";
                                }
                                else if ("D".Contains(fieldInfo.FieldType))
                                {
                                    // Date field
                                    if (blank)
                                        fieldValue = DateOnly.MinValue;
                                }
                                else if ("T".Contains(fieldInfo.FieldType))
                                {
                                    // DateTime field
                                    if (blank)
                                        fieldValue = DateTime.MinValue;
                                }
                                else if ("L".Contains(fieldInfo.FieldType))
                                {
                                    // Logical field
                                    if (blank)
                                        fieldValue = false;
                                }
                                else
                                    throw new Exception($"11||Gather");
                            }

                            // Update the field
                            if (toType.Equals("A"))
                            {
                                if ("MGW".Contains(fieldType) == false)
                                {
                                    // ----- TO array ----- 
                                    toArray.SetDimension(0, ++arrayPtr, true);
                                    toArray._avalue[arrayPtr - 1].Value = fieldValue;
                                }
                            }
                            else if (toType.Equals("N"))
                            {
                                if ("MGW".Contains(fieldType) == false || eCodes.Flags.Contains("memo"))
                                {
                                    // ----- NAME object ----- 
                                    if (toObject.thisObject!.UserProperties.ContainsKey(fieldName) == false)
                                        await toObject.AddProperty(fieldName, new(fieldValue), 1, "");
                                    else
                                        await toObject.AddProperty(fieldName, new(fieldValue), 1, "");
                                }
                            }
                            else if (toType.Equals("J"))
                            {
                                if ("MGW".Contains(fieldType) == false || eCodes.Flags.Contains("memo"))
                                {
                                    // ----- JSON var ----- 
                                    if (toJson!.ContainsKey(fieldName) == false)
                                        toJson.Add(fieldName, fieldValue);
                                    else
                                        toJson[fieldName] = fieldValue;
                                }
                            }
                            else if (toType.Equals("M"))
                            {
                                if ("MGW".Contains(fieldType) == false || eCodes.Flags.Contains("memo"))
                                {
                                    // ----- MEMVAR ----- 
                                    if (toJson!.ContainsKey(fieldName) == false)
                                        toJson.Add(fieldName, fieldValue);
                                    else
                                        toJson[fieldName] = fieldValue;
                                }
                            }
                        }
                    }
                }

                if (fieldCount > 0)
                {
                    // We did some work so finish off by saving the target object
                    if (toType.Equals("A"))
                    {
                        // ----- TO array ----- 
                        toVar.CopyFrom(toArray);

                    }
                    else if (toType.Equals("N"))
                    {
                        // ----- NAME object ----- 
                        toVar.Element.Value = toObject;
                    }
                    else if (toType.Equals("J"))
                    {
                        // ----- JSON var ----- 
                        toVar.Element.Value = JsonConvert.SerializeObject(toJson);
                    }
                    else
                    {
                        // ----- MEMVAR -----
                        foreach (KeyValuePair<string, object> pair in toJson!)
                            await AppVars.SetVarFromExpression("m." + pair.Key, pair.Value, true);
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }



        /*
         * 
         * SEEK eExpression [ORDER nIndexNumber | cIndexName] [ASCENDING | DESCENDING] [IN nWorkArea | cAlias] [SESSION nSession]
         * 
         */
        public static async Task<string> Seek(string cmdRest)
        {
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
            Program.CurrentApp.SetDataSession(ds);
            Program.CurrentApp.CurrentDS.SelectWorkArea(wa);

            return "";
        }


        /*
         * 
         * SELECT nExpr|cExpr SESSION nExpr
         * 
         */
        public static async Task<string> Select(ExecutorCodes eCodes)
        {
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

            return "";
        }



        /* TODO
         * 
         * SELECT (SQL)
         * 
         */
        public static string SelectSQL(ExecutorCodes eCodes)
        {
            try
            {

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.SelectSQL", ex.Message);
            }

            return "";
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

            return "";
        }


        /*
         * 
         * Sleep nMilliseconds
         * 
         * Used to pause the current thread for the specified number of milliseconds.
         * 
         */
        public static string Sleep(ExecutorCodes eCodes)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Sleep", ex.Message);
            }
            return "";
        }


        /*
         * 
         * SORT
         * 
         */
        public static string Sort(ExecutorCodes eCodes)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Sort", ex.Message);
            }

            return "";
        }


        /*
         * 
         * STORE eExpression1 [,eExpression2...] TO cVar1 [, cVar2...]
         * 
         */
        public static async Task<string> Store(ExecutorCodes eCodes)
        {
            try
            {
                // basic sanity checks
                if (eCodes.Expressions.Count < 1) throw new Exception("10|");

                // get the expression to store to the var list
                JAXObjects.Token ExprValue = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                if (eCodes.Expressions[0].RNPExpr.Contains("exec(",StringComparison.OrdinalIgnoreCase))
                {
                    int iii = 0;
                }

                if ("EM".Contains(ExprValue.TType))
                    ExprValue = new("");

                for (int i = 0; i < eCodes.To.Count; i++)
                {
                    // Get the VarName literal or (expression)
                    JAXObjects.Token varName = await Program.CurrentApp.SolveFromRPNString(eCodes.To[i].Name);

                    AppIO.DebugLog($"store {ExprValue.AsString()} to {varName.AsString()}");

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

            return "";
        }


        /* TODO
         * 
         * SUSPEND
         * 
         */
        public static string Suspend(string cmdRest)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException($"{thisFile}.Suspend", ex.Message);
            }

            return "";
        }
    }
}
