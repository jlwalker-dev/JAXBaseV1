using JAXBase.Core;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_U
    {

        /* TODO
         * 
         * UNLOCK [RECORD nRecordNumber] [IN nWorkArea | cTableAlias] [ALL]
         * 
         */
        public static string Unlock(AppClass app, string cmdLine)
        {
            string result = string.Empty;
            AppErrorHandling.ClearErrors();
            try
            {
                throw new Exception("UNLOCK is not implemented in version 1.0");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }
            return result;
        }

        /*
         * 
         * UNTIL lExpression
         * 
         */
        public static async Task<string> Until(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            AppErrorHandling.ClearErrors();
            try
            {
                string lp = AppLoop.GetLoopStack();

                if (lp.Equals(eCodes.SUBCMD))
                {
                    // Get the until expression
                    JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                    if (answer.Element.Type.Equals("L"))
                    {
                        // Get back to the correct datasession and workarea
                        LoopClass loop = jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].ScanLoops[eCodes.SUBCMD];
                        jbe.App.SetDataSession(loop.DataSession);
                        jbe.App.CurrentDS.SelectWorkArea(loop.WorkArea);

                        if (answer.Element.ValueAsBool)
                            result = "U" + lp;  // Go to the DO
                        else
                            AppLoop.PopLoopStack();
                    }
                    else
                        throw new Exception("11|");
                }
                else
                    throw new Exception("1210|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }
            return result;
        }

        /* TODO
         *
         * UPDATE
         * The SQL update must have a where clause or join and is not supported in Version 1
         * 
         * (SQL) UPDATE Target SET Column_Name1 = eExpression1 [, Column_Name2 = eExpression2 ...] [FROM [FORCE] Table_List_Item [[, ...] | [JOIN [ Table_List_Item]]] WHERE FilterCondition1 [AND | OR FilterCondition2 ...]
         * 
         * UPDATE with no where sends to REPLACE
         * 
         */
        public static string Update(AppClass app, string cmdLine)
        {
            string result = string.Empty;
            AppErrorHandling.ClearErrors();
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }
            return result;
        }



        /* TODO -   AGAIN for Version 0.8
         * 
         * USE [[DatabaseName!] TableName ] [IN nWorkArea | cAlias] 
         *      [ALIAS cTableAlias] 
         *      [INDEX Index1 [ASCENDING | DESCENDING], Index2...]
         *      [EXCLUSIVE|SHARED] [NOUPDATE] [AGAIN] 
         * 
         */
        public static async Task<string> Use(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            AppErrorHandling.ClearErrors();

            try
            {
                int ds = jbe.App.CurrentDataSession;
                int wa = jbe.App.CurrentDS.CurrentWorkArea();

                string dbc = string.Empty;
                string dbf = string.Empty;
                string alias = eCodes.ALIAS;

                JAXObjects.Token answer = new();

                // Is there a table name?
                if (eCodes.Expressions.Count > 0)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                    if (answer.Element.Type.Equals("C"))
                        dbf = answer.AsString();
                    else
                        throw new Exception("11|");

                    // Is there a database name to break out?
                    int f = dbf.IndexOf('!');
                    if (f < dbf.Length - 1 & f > 0)
                    {
                        dbc = dbf[..f++];
                        dbf = dbf[f..];
                    }
                }

                if (string.IsNullOrWhiteSpace(dbc) == false && string.IsNullOrWhiteSpace(dbf))
                    throw new Exception("1||Cannot open a blank table name with a database specification");

                bool again = Array.IndexOf(eCodes.Flags, "again") >= 0;
                bool exclusive = Array.IndexOf(eCodes.Flags, "exclusive") >= 0 || jbe.App.CurrentDS.JaxSettings.Exclusive;
                bool noupdate = Array.IndexOf(eCodes.Flags, "noupdate") >= 0;

                // Are we using a different datasession?
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

                // We don't worry about VFP DBCs because it will be accessed by
                // the OPEN routine to get the long field names for the table
                // and then closed again.  JAXBase doesn't support any other
                // legacy features of the Visual FoxPro DBC.

                if (dbc.Length > 0)
                {
                    // Open a table in a SQL connection
                    throw new Exception("1999|database!table not supported");
                }
                else if (dbf.Length > 0)
                {
                    // Open the DBF
                    // Break out the dbf FQFN
                    string path = JAXLib.JustFullPath(dbf);
                    string stem = JAXLib.JustStem(dbf);
                    string ext = JAXLib.JustExt(dbf);
                    ext = ext.Length > 0 ? ext : "dbf";

                    // Try to locate it in the path list
                    if (path.Length == 0)
                        path = AppHelper.FindPathForFile(jbe.App, stem + "." + ext);

                    // If not found in the path list, toss a file not found error
                    if (path.Length == 0)
                        throw new Exception("1|" + dbf);

                    // Got this far, so we must be good to go
                    dbf = path + stem + "." + ext;

                    // TODO - add AGAIN flag to call for Version 0.8
                    if (await jbe.App.CurrentDS.CurrentWA.DBFUse(dbf, alias, exclusive, noupdate, string.Empty) == 0)
                    {
                        // successful open - are there any index?
                        for (int i = 0; i < eCodes.Index.Count(); i++)
                        {
                            // Open each index found
                            bool desc = "DESCENDING".StartsWith(eCodes.Index[i].Type, StringComparison.OrdinalIgnoreCase);
                            string name = eCodes.Index[i].Name;

                            // Break up the name and search for the index
                            string extIdx = JAXLib.JustExt(name);

                            string pathIdx = JAXLib.JustFullPath(name);
                            name = JAXLib.JustStem(name);

                            extIdx = string.IsNullOrWhiteSpace(extIdx) ? "idx" : extIdx;

                            if (string.IsNullOrWhiteSpace(pathIdx))
                                pathIdx = AppHelper.FindPathForFile(jbe.App, name + "." + extIdx);

                            // Put together the full index name and open it
                            name = pathIdx + name + "." + extIdx;
                            await jbe.App.CurrentDS.CurrentWA.IDXOpen(name, false);

                            // Set the ascending/descending flag
                            jbe.App.CurrentDS.CurrentWA.DbfInfo.IDX[jbe.App.CurrentDS.CurrentWA.DbfInfo.ControllingIDX].Descending = desc;
                        }
                    }
                }
                else
                {
                    // No dbf name means CLOSE IT ALL UP!
                    await jbe.App.CurrentDS.CurrentWA.DBFClose();
                }

            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }
    }
}
