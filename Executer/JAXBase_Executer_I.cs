using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_I
    {

        /* 
         * IF lexpr
         * 
         */
        public static async Task<string> If(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                string PrgCode = jbe.App.PRGCache[jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];

                if (eCodes.Expressions.Count < 1) throw new Exception("10|");
                if (string.IsNullOrWhiteSpace(eCodes.SUBCMD)) 
                    throw new Exception("9999|Missing IF identifier");

                JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                if (answer.TType.Equals("U") || answer.Element.Type.Equals("L") == false)
                    throw new Exception("11|");
                else
                {
                    if (answer.AsBool() == false)
                    {
                        string elseCmd = jbe.App.MiscInfo["elsecmd"];
                        string endCmd = jbe.App.MiscInfo["endifcmd"];
                        string elseifCmd = jbe.App.MiscInfo["elseifcmd"];

                        // Look for an else
                        int elsePos = PrgCode.IndexOf(elseCmd + eCodes.SUBCMD);           // look for else
                        int elseifPos = PrgCode.IndexOf(elseifCmd + eCodes.SUBCMD);         // look for elseif

                        while (elseifPos > 0)
                        {
                            // Is there an elseif after the else?
                            if (elsePos > 0 && elseifPos > elsePos) throw new Exception("1211");

                            // Loop through the ElseIf statements until
                            // we get to one that evaluates to true

                            int p = PrgCode.IndexOf((char)0xF8, elseifPos + 1);
                            if (p < 0) throw new Exception("10||No expression in ElseIf statement");

                            int e = PrgCode.IndexOf(AppClass.cmdEnd, p);
                            if (e < 0) throw new Exception("10||No command end in ElseIf statement");
                            string rpn = PrgCode.Substring(p + 1, e - p - 2);
                            answer = await jbe.App.SolveFromRPNString(rpn);

                        }

                        // Is there an else?
                        int f = elsePos < 0 ? PrgCode.IndexOf(endCmd + eCodes.SUBCMD) : elsePos;

                        if (f > 0)
                        {
                            jbe.App.utl.Conv64(f, 3, out elseCmd);
                            result = "Y" + elseCmd;
                        }
                        else
                            throw new Exception("1211|");   // If/Else/ElseIf/Endif stmt is missing
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * IMPORT
         * 
         */
        public static string Import(AppClass app, string cmdRest)
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
         * INDEX ON eExpression TO cFile [COLLATE cCollateSequence] [FOR lExpression] [ASCENDING | DESCENDING] [UNIQUE | CANDIDATE] [NOCASE]
         * 
         */
        public static async Task<string> Index(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                JAXDirectDBF Table = jbe.App.CurrentDS.CurrentWA;
                if (Table.DbfInfo is null || Table.DbfInfo.DBFStream is null) throw new Exception("52|");

                // Get the index name
                JAXObjects.Token answer = new();
                answer = eCodes.To.Count > 0 ? await jbe.App.SolveFromRPNString(eCodes.To[0].Name) : throw new Exception("10");
                if (answer.Element.Type.Equals("C") == false) throw new Exception("11|");

                string FQFN = answer.AsString();
                string path = JAXLib.JustPath(FQFN);
                string fName = JAXLib.JustStem(FQFN);
                string fExt = JAXLib.JustExt(FQFN);

                path = string.IsNullOrWhiteSpace(path) ? JAXLib.JustFullPath(Table.DbfInfo.FQFN) : path;
                fExt = string.IsNullOrWhiteSpace(fExt) ? "idx" : fExt;
                FQFN = path + fName + "." + fExt;

                // Get the collation sequence - TODO
                string Collation = eCodes.COLLATE;

                // Get the key expression
                string keyExpr = string.IsNullOrWhiteSpace(eCodes.ON) == false ? eCodes.ON : throw new Exception("1232|");

                // Get the for expression
                string forExpr = string.Empty;
                answer.Element.Value = string.IsNullOrWhiteSpace(eCodes.ForExpr) ? string.Empty : (await jbe.App.SolveFromRPNString(eCodes.ForExpr)).Element.Value;
                if (answer.Element.Type.Equals("C"))
                    forExpr = answer.AsString();
                else
                    throw new Exception("11||For expression is type " + answer.Element.Type);

                // Get the flags
                bool isDesc = eCodes.Flags.Length > 0 && Array.IndexOf(eCodes.Flags, "descending") >= 0;
                bool isUnique = eCodes.Flags.Length > 0 && Array.IndexOf(eCodes.Flags, "unique") >= 0;

                // TODO
                bool isCandidate = eCodes.Flags.Length > 0 && Array.IndexOf(eCodes.Flags, "candidate") >= 0;
                bool isNoCase = eCodes.Flags.Length > 0 && Array.IndexOf(eCodes.Flags, "nocase") >= 0;

                await Table.IDXCreate(FQFN, keyExpr, isDesc, isUnique, eCodes.ForExpr);
                Table.DbfInfo.ControllingIDX = Table.DbfInfo.IDX.Count - 1; // This is now the controlling index
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * INSERT INTO dbf_name [(FieldName1 | (cExpression1) [, FieldName2 | (cExpression2), ...])] VALUES (eExpression1 [, eExpression2, ...])
         * 
         * INSERT INTO dbf_name FROM ARRAY ArrayName | FROM MEMVAR | FROM NAME ObjectName
         * 
         * NOT IMPLEMENTED IN VERSION 1
         *      INSERT INTO dbf_name [(FieldName1 [, FieldName2, ...])] SELECT SELECTClauses [UNION UnionClause SELECT SELECTClauses ...]
         * 
         * 
         * TODO - ADD THE FOLLOWING CALLS
         *      result = JAXDataHandler.AppendJSON(jbe, eCodes)
         *      result = JAXDataHandler.AppendArray(jbe, eCodes)
         *      result = JAXDataHandler.AppendValues(jbe, eCodes)
         */
        public static async Task<string> Insert(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                string dbfName = string.IsNullOrWhiteSpace(eCodes.INTO) == false ? eCodes.INTO : throw new Exception("10|");

                List<string> fieldList = [];
                List<List<JAXObjects.Token>> valueList = [];
                JAXDirectDBF.DBFInfo dbfInfo = jbe.App.CurrentDS.CurrentWA.DbfInfo;

                // Get the fields
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(eCodes.Expressions[i].RNPExpr) == false)
                    {
                        JAXObjects.Token val = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);
                        fieldList.Add(val.AsString());
                    }
                }

                // Did we get a field list?
                if (fieldList.Count == 0)
                {
                    // No field list, so get all of the fields for this table
                    for (int i = 0; i < dbfInfo.Fields.Count; i++)
                    {
                        if (dbfInfo.Fields[i].SystemColumn == false)
                            fieldList.Add(dbfInfo.Fields[i].FieldName.Trim());
                    }
                }


                // VALUES or FROM?
                if (string.IsNullOrWhiteSpace(eCodes.From.Type))
                {
                    // VALUES ()
                    if (eCodes.Expressions.Count < 1) throw new Exception("10|");
                    valueList.Add([]);

                    for (int i = 0; i < eCodes.Fields.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(eCodes.Values[i].RNPExpr) == false)
                        {
                            JAXObjects.Token val = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);
                            valueList[0].Add(val);
                        }
                    }
                }
                else
                {
                    // FROM ARRAY var | MEMVAR | ObjectName | JSON expr
                    if (eCodes.From.Type.Equals("A"))
                    {
                        // FROM ARRAY
                        string arrayName = eCodes.From.Name;
                        JAXObjects.Token av = await AppVars.GetVarToken(arrayName);

                        if (av.TType.Equals("A"))
                        {
                            // Chop off the excess fields
                            while (av.Col < fieldList.Count)
                                fieldList.RemoveAt(av.Col);
                        }
                        else
                            throw new Exception("232|" + arrayName);

                        // There may be multiple rows to process
                        for (int i = 0; i < av.Row; i++)
                        {
                            valueList.Add([]);
                            for (int j = 0; j < av.Col; j++)
                            {
                                av.ElementNumber = i * av.Col + j;
                                JAXObjects.Token tk = new();
                                tk.Element.Value = av.Element.Value;
                                valueList[0].Add(tk);
                            }
                        }
                    }
                    else if (eCodes.From.Type.Equals("M"))
                    {
                        // FROM MEMVAR
                        int k = 0;
                        while (k < fieldList.Count)
                        {
                            // Get the field name
                            string cField = "m." + fieldList[k];

                            // Look for the matching memory variable
                            JAXObjects.Token mv = await AppVars.GetVarToken(cField);

                            if (mv.TType.Equals("U"))
                            {
                                // No matching memory variable, so remove
                                // the current field from the list
                                fieldList.RemoveAt(k);
                            }
                            else
                            {
                                // Add the valueList row on the first
                                // successful value match
                                if (k == 0)
                                    valueList.Add([]);

                                // We have a value!
                                valueList[0].Add(mv);
                                k++;
                            }
                        }
                    }
                    else if (eCodes.From.Type.Equals("J"))
                    {
                        // FROM JSON - TODO
                        string jsonString = eCodes.From.Name;    // JSON Expression
                    }
                }

                // We're ready to start loading the table
                // FROM ARRAY and FROM JSON will set the field count to
                // be less than or equal to the number of values so
                // we just check if an INSERT/VALUES statement
                if (eCodes.Expressions.Count > 0)
                {
                    if (fieldList.Count > valueList[0].Count)
                        throw new Exception("94|");                                                 // Must specify additional parameters

                    if (fieldList.Count < valueList[0].Count)
                        throw new Exception("1230|");                                               // Too many arguments
                }

                // Add a record and update the table/cursor
                if (dbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", jbe.App.CurrentDS.CurrentWorkArea()));

                if (dbfInfo.NoUpdate)
                    throw new Exception(string.Format("2088|{0}", dbfInfo.TableName));           // File is read only

                // Is this a buffered table?
                bool buffered = jbe.App.CurrentDS.CurrentWA.DbfInfo.Buffered;

                // For each valueList row, add a new record to the table
                for (int v = 0; v < valueList.Count; v++)
                {
                    // Add a blank record
                    await jbe.App.CurrentDS.CurrentWA.DBFAppendRecord(null);

                    // TODO - if there is an autoincrement field, it can't be in the
                    // list of fields unless SET AUTOINCERROR is OFF
                    for (int i = 0; i < fieldList.Count; i++)
                        await jbe.App.CurrentDS.CurrentWA.DBFReplaceField(fieldList[i], valueList[v][i], !buffered);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
