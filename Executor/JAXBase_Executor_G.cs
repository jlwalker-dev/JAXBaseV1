using JAXBase.Core;
using JAXBase.Data;
using JAXBase.XBase;
using Newtonsoft.Json.Linq;
using System.Data;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_G
    {

        /*
         * 
         *  GATHER FROM cArray | MEMVAR | NAME cObject | JSON cExpression
         *      [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         *      [MEMO][BLANK]
         * 
         * 2026-06-02 - TODO: Need to work on Like, Except, and Fields support
         *              Need to test JSON array support
         */
        public static async Task<string> Gather(ExecutorCodes eCodes)
        {
            try
            {
                if (Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception("52||Gather");

                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.From.Type);
                if (answer.Element.Type.Equals("C") == false) throw new Exception("11||Gather");
                string fromType = answer.AsString();
                string fromName = eCodes.From.Name;

                JAXObjects.Token fromArray = new();
                JAXObjectWrapper fromObject = new(Program.CurrentApp, "empty", "", []);
                Dictionary<string, object>? fromJson = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                if (fromType.Equals("A"))
                {
                    // FROM ARRAY
                    fromArray = await AppVars.GetVarToken(fromName);

                    // Is it an array?
                    if (fromArray.TType.Equals("A") == false)
                        throw new Exception($"232|{fromName.ToUpper()}|Gather - not an array");
                }
                else if (fromType.Equals("N"))
                {
                    // NAME
                    answer = await AppVars.GetVarToken(fromName);

                    // Is it an object?
                    if (answer.Element.Type.Equals("O"))
                        fromObject = answer.Element.Value as JAXObjectWrapper ?? throw new Exception("11||Gather");
                    else
                        throw new Exception($"1924|{fromName.ToUpper()}|Gather - NAME var is not an object");
                }
                else if (fromType.Equals("J"))
                {
                    // FROM JSON
                    answer = await AppVars.GetVarToken(fromName);

                    // is it a string?
                    if (answer.Element.Type.Equals("C") == false)
                        throw new Exception("11||Gather - JSON var is not a string");

                    JObject jObj = JObject.Parse(answer.AsString());
                    if (jObj is null)
                        throw new Exception($"2500|{fromName.ToUpper()}|Gather - invalid JSON object");
                    else
                        fromJson = jObj.ToObject<Dictionary<string, object>>();

                    if (fromJson is null)
                        throw new Exception($"2502|{fromName.ToUpper()}|Gather - Could not convert JSON object");
                }

                string isLike = "";
                string isNotLike = "";
                string[] fields = [];
                bool memoOK = eCodes.Flags.Contains("memo");
                bool blank = eCodes.Flags.Contains("blank");

                // Copy the current row
                DataRow newRow = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.CurrentRow.NewRow();
                newRow.ItemArray = (object[])Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.CurrentRow.Rows[0].ItemArray.Clone();

                int fieldCount = 0;
                bool fieldsUpdated = false;
                int arrayPtr = 0;

                for (int i = 0; i < Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields.Count; i++)
                {
                    JAXTables.FieldInfo fieldInfo = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields[i];
                    string fieldName = fieldInfo.FieldName;
                    bool doUpdate = false;

                    // Is it a system column?
                    if (fieldInfo.SystemColumn == false)
                    {
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
                            doUpdate = true;
                        }

                        answer = new();


                        if (doUpdate)
                        {
                            fieldsUpdated = true;

                            // Update the field
                            if (fromType.Equals("A"))
                            {
                                // ----- FROM array ----- 
                                if (answer._avalue.Count > fieldCount)
                                    answer.Element.Value = fromArray._avalue[arrayPtr++];
                                else
                                    doUpdate = false;
                            }
                            else if (fromType.Equals("N"))
                            {
                                // ----- NAME object ----- 
                                if (fromObject.thisObject!.UserProperties.ContainsKey(fieldName.ToLower()))
                                    answer.Element.Value = fromObject.GetProperty(fieldName);
                                else
                                    doUpdate = false;
                            }
                            else if (fromType.Equals("J"))
                            {
                                // ----- JSON var ----- 
                                if (fromJson.ContainsKey(fieldName))
                                    answer.Element.Value = fromJson[fieldName];
                                else
                                    doUpdate = false;
                            }
                            else if (fromType.Equals("M"))
                            {
                                // ----- MEMVAR ----- 
                                answer = await AppVars.GetVarToken("m." + fieldName);
                                if (answer.Element.Type.Equals("U"))
                                    doUpdate = false;
                            }

                            if (doUpdate)
                            {
                                // Check the type and if it passes, save it, otherwise throw an error
                                if (answer.Element.Type.Equals("N") && "BNFY".Contains(fieldInfo.FieldType))
                                {
                                    // Numeric field
                                    if (blank)
                                        newRow[fieldName] = 0;
                                    else
                                        newRow[fieldName] = answer.AsDouble();
                                }
                                if (answer.Element.Type.Equals("N") && fieldInfo.FieldType.Equals("I"))
                                {
                                    // Integer Field
                                    if (blank)
                                        newRow[fieldName] = 0;
                                    else
                                        newRow[fieldName] = answer.AsInt();
                                }
                                else if (answer.Element.Type.Equals("C") && "CVQ".Contains(fieldInfo.FieldType))
                                {
                                    // Character field
                                    if (blank)
                                        newRow[fieldName] = "";
                                    else
                                        newRow[fieldName] = answer.AsString();
                                }
                                else if (answer.Element.Type.Equals("C") && "MGW".Contains(fieldInfo.FieldType))
                                {
                                    // Never update a memo, general, or binary field from an array
                                    if (fromType.Equals("A") == false && memoOK)
                                    {
                                        // Character field
                                        if (blank)
                                            newRow[fieldName] = "";
                                        else
                                            newRow[fieldName] = answer.AsString();
                                    }

                                    // We don't update these fields from an array so leave the array pointer where it is
                                    if (fromType.Equals("A"))
                                        arrayPtr--;

                                }
                                else if (answer.Element.Type.Equals("D") && "D".Contains(fieldInfo.FieldType))
                                {
                                    // Date field
                                    if (blank)
                                        newRow[fieldName] = DateOnly.MinValue;
                                    else
                                        newRow[fieldName] = answer.AsDate();
                                }
                                else if (answer.Element.Type.Equals("T") && "T".Contains(fieldInfo.FieldType))
                                {
                                    // DateTime field
                                    if (blank)
                                        newRow[fieldName] = DateTime.MinValue;
                                    else
                                        newRow[fieldName] = answer.AsDateTime();
                                }
                                else if (answer.Element.Type.Equals("L") && "L".Contains(fieldInfo.FieldType))
                                {
                                    // Logical field
                                    if (blank)
                                        newRow[fieldName] = false;
                                    else
                                        newRow[fieldName] = answer.AsBool();
                                }
                                else
                                    throw new Exception($"11||Gather");
                            }


                            // Update the visisble field count
                            fieldCount++;
                        }
                    }
                }

                if (fieldsUpdated)
                {
                    // We updated at least one field, so we can add the row
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFWriteRecord(newRow, false);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "";
        }


        /* TODO NOW
         * 
         * GETEXPR()
         * 
         */
        public static string GetExpr(string cmdRest)
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
         * GOTO
         * 
         * Move Record Pointer to record potision
         * 
         */
        public static async Task<string> Goto(ExecutorCodes eCodes)
        {
            string result = string.Empty;

            // Where are we?
            int ds = Program.CurrentApp.CurrentDataSession;
            int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

            try
            {

                if (eCodes.SESSION > 0)
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);

                int cwa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (eCodes.Expressions.Count != 1) throw new Exception("10|");

                JAXObjects.Token tk = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (tk.Element.Type.Equals("C"))
                {
                    if (tk.AsString().Equals("TOP"))
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("TOP");
                    else if (tk.AsString().Equals("BOTTOM"))
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("BOTTOM");
                    else
                        throw new Exception($"12|{tk.AsString()}");
                }
                else if (tk.Element.Type.Equals("N"))
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord(tk.AsInt());
                else
                    throw new Exception("11|");

                // Back to where we were
                Program.CurrentApp.CurrentDS.SelectWorkArea(cwa);

                Program.CurrentApp.SetDataSession(ds);
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa); // Restore workarea
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
