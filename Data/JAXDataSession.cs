/*
 * This is the Datasession control system
 * 
 * All databases, tables, cursors, views, and datasessions
 * are controlled through this class.
 * 
 * WARNING: The database system is not meant to be used with very large
 * tables.  You an use large tables if you use the select statement to
 * get a range of records. Otherwise, you will be looking at massive
 * overhead and long waits.
 * 
 * Some helpful links
 * https://stackoverflow.com/questions/5005658/how-do-you-sort-a-datatable-given-column-and-direction
 * https://www.codeproject.com/Questions/1263311/How-to-go-back-and-forth-in-datatable-in-Csharp
 * https://www.codeproject.com/Questions/1109458/How-can-I-move-forward-and-backward-through-rows-o
 * https://community.jmp.com/t5/Discussions/Should-you-Loop-through-a-data-table-or-use-Recode-or-use-Get/td-p/381360/page/2
 * 
 * 2024-01-17 - JLW
 *      I'm planning to use VFP OLE for a first wack at supporting local DBF tables.  The way the
 *      code is set up, I think it would meld pretty easily.
 * 
 *      However, I remembered that I created a QB4 program that would read & write directly to DBF tables
 *      and handle IDX files.  I do not have any clue, nor can I really find clear information on how
 *      CDX files work.  I could fudge something up for CDX which would be an extension to how IDX files 
 *      work, and the size of the files would be huge.  It would also lack Rushmore, but it is what it is.
 * 
 *      If I can find that code, I'm going to attempt to include direct DBF/IDX access so that Linux will
 *      eventually have the ability to read/write local dbf files.
 * 
 * 2025-01-21 - JLW
 *      The concept of using SQL instead of local tables is pretty much completed with just some testing
 *      and clean up left.  I'm going to dig out an old QuickBASIC program that I wrote, last century, that
 *      will read and write DBF and IDX files. CDX files will simply error out until I get around to
 *      supporting them.  If I'm right, it will provide faster access to large tables.
 * 
 * 2025-02-06 - JLW
 *      I've decided to go for direct table manipulation and leaving SQL handling to another class.
 *      I dusted off my QuickBASIC code and upgraded it to handle VFP 9 tables.  Still don't have
 *      the logic for CDX, but I've cracked a lot of the IDX structure.  Other issues also loom in
 *      the Database (DBC) container where I'm going to have to limit any code written to function
 *      calls to code or bool expressions.
 * 
 *      I've rewritten DataSessions to DataSession and each session is created, as needed, and
 *      only the default data session lives throughout the running of the application.  I've also
 *      added some capabilities to the classes to make it easier to deal with the DBCs, which
 *      are addressed by name.
 * 
 *      My next major step will be in making it possible to open the tables in shared mode.
 * 
 * 2025-08-17 - JLW
 *      Will be setting work area limits per datasession to unlimited positive integer.  
 *      This means the work areas will be stored in a dictionary<int,dbfinfo> instead 
 *      of an array of dbfinfo.
 * 
 * 2025-08-21 - JLW
 *      Will be testing SEEK and Locate once Gather, Scatter, & Replace code logic is ready
 *      to test.  I expect I'll start writing some of the simpler utilities, such as the 
 *      VFP Screen Conversion and Screen Table compiler.  Nothing too taxing, but definitely
 *      good tests for the dbf logic.
 *      
 *      I'm starting to think about the SQL table logic so that a SQL table is capable
 *      of doing things like a normal local table, but the data will need to be pulled 
 *      in, whether automatically or manually.  That's the biggest issue to consider as I
 *      don't want to be causing issues with response.  Local tables are what I like to
 *      call "hot data", being available almost instantly with direct access to the data.
 *      SQL tables need to be queried and only what is pulled in is available.  I might
 *      try to cook something up so that a GOTO, SKIP, LOCATE, etc pulls in what's asked 
 *      and along with few records before and after.  Table order will be an issue, and 
 *      I've got some ideas, but I'm not sure they'd be pretty or practical.  I may settle 
 *      for "JAX Tables in SQL", which appear to act exactly like local tables, and then 
 *      leave standard SQL table handling to the more traditional methods.
 *      
 */
using JAXBase.Core;
using JAXBase.Math;
using System.Text;
using JAXBase.Utilities.Utilities;
using JAXBase.XBase;

namespace JAXBase.Data
{
    public class JAXDataSession
    {
        const string DEFAULT = "*default";

        readonly AppClass App;
        public JAXSettings JaxSettings = new();

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public JAXDataSession(AppClass app, string formID)
        {
            App = app;
            FormID = formID;

            JAXDirectDBF db = new(app);
            //db.DbfInfo.FQFN = app.JaxSettings.Default;
            //Databases.Add(DEFAULT, db);     // Set up the default empty SQL database for free tables
            WorkAreas.Add(0, new(App));
            WorkAreas.Add(1, new(App));

            // Set up the default user workarea
            db = new(app);
        }

        public JAXDirectDBF CurrentWA { get { return WorkAreas[currentWorkArea]; } private set { } }

        public string CurrentDatabase { get; private set; } = DEFAULT;

        private int currentWorkArea = 1;

        private readonly string FormID = string.Empty;


        // TODO - VFP 9 allows for 32,767 work areas.  We'll give unlimited
        // and then use a sorteddictionary to handle everything
        public Dictionary<int, JAXDirectDBF> WorkAreas = [];
        public readonly Dictionary<string, JAXDirectDBF> Databases = [];

        public int CurrentWorkArea()
        {
            return currentWorkArea;
        }



        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public int SetDB(string name)
        {
            int Err = 0;

            if (name.Length > 0)
            {
                if (Databases.ContainsKey(name.ToLower()) == false)
                    Err = 1;
                else
                    CurrentDatabase = name.ToLower();
            }
            else
                CurrentDatabase = DEFAULT;

            return Err;
        }


        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public bool IsDBUsed(string filename)
        {
            string dbName = JAXLib.JustStem(filename).ToLower();
            return Databases.ContainsKey(dbName);
        }

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public JAXDirectDBF GetTable(int workarea)
        {
            if (workarea < 0) throw new Exception("3021|");
            return WorkAreas.ContainsKey(workarea) ? WorkAreas[workarea] : new(App);
        }

        public async Task<bool> OpenDB(string filename) { return await OpenDB(filename, string.Empty); }

        public async Task<bool> OpenDB(string filename, string alias)
        {
            JAXDirectDBF db = new(App);

            if (IsDBUsed(filename) == false)
            {
                bool exclusive = App.JAXSysObj.GetValue("excluisve").Equals("ON", StringComparison.OrdinalIgnoreCase);
                db = new(App);
                await db.DBFUse(filename, alias, exclusive, false, string.Empty);

                if (App.ErrorCount() == 0)
                {
                    if (db.DbfInfo.IsDBC == false)
                    {
                        App.SetError(1552, $"1552|{filename}", "OpenDB");
                        await db.DBFClose();
                    }
                    else
                        Databases.Add(db.DbfInfo.Alias.ToLower(), db);  // Successful open
                }
            }

            return App.ErrorCount() == 0;
        }

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public string GetCurrentDBName() { return CurrentDatabase; }


        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public void CloseAll(string type)
        {
            if (type.Equals("table", StringComparison.OrdinalIgnoreCase))
            {
                // Close all DBFs
            }
            else if (type.Equals("database", StringComparison.OrdinalIgnoreCase))
            {
                // Close all DBs
            }
            else if (type.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                // Close all DBFs
                // Close all Databases
            }
        }

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public async Task CloseDBF(string alias)
        {
            foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
            {
                if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                {
                    await WorkAreas[dbf.Key].DBFClose();
                    break;
                }
            }
        }

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public async Task CloseDB(string dbname)
        {
            if (dbname.ToLower().Equals(DEFAULT) == false && Databases.ContainsKey(dbname.ToLower()))
                await Databases[dbname].DBFClose();
            else
                throw new Exception("Database is not open");
        }

        /*---------------------------------------------------------------------------------*
         * Open a DBF, and if appropriate, the Memo and structural CDX files
         * 
         * Parameters:
         *      String  Fully Qualified File Name
         *      String  Alias
         *      
         * Returns: 
         *      Int     Error Count
         *---------------------------------------------------------------------------------*/
        public async Task<int> OpenTable(string filename, string Alias)
        {
            string FQFN = string.Empty;
            string[] pathList = App.CurrentDS.JaxSettings.Path.Split(";");

            if (filename.Contains('\\') || filename.Contains(':'))
                FQFN = filename;
            else
                FQFN = JAXLib.FindFileInPathList(pathList, filename);

            JAXDirectDBF? table;
            bool exclusive = App.JAXSysObj.GetValue("excluisve").Equals("ON", StringComparison.OrdinalIgnoreCase);

            if (File.Exists(FQFN))
            {
                table = new(App);
                await table.DBFUse(FQFN, Alias, exclusive, false, string.Empty);
            }
            else
                throw new Exception("Table does not exist");

            if (WorkAreas[currentWorkArea].DbfInfo.TableName.Length > 0)
                await WorkAreas[currentWorkArea].DBFClose();

            //if (Workareas![currentWorkArea].DbfInfo.TableName.Length > 0) Workareas[currentWorkArea].DBCClose();

            WorkAreas[currentWorkArea] = table;

            // Is there a DBC link and is it currently open?
            if (table.DbfInfo.DBCLink.Trim().Length > 0)
            {
                // If not, then open it up
                if (IsDBUsed(table.DbfInfo.DBCLink) == false)
                    await OpenDB(table.DbfInfo.DBCLink);
            }

            return App.ErrorCount();
        }


        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public string[] ListOfOpenDBs()
        {
            string[] dbList = new string[Databases.Count];

            for (int i = 0; i < Databases.Count; i++)
                dbList[i] = Databases.ElementAt(i).Value.DbfInfo.TableName;

            return dbList;
        }

        /*---------------------------------------------------------------------------------*
         * Returns a list of open tables and cursors for the current data session
         *---------------------------------------------------------------------------------*/
        public string[] ListOfOpenTables()
        {
            string dbList = string.Empty;

            foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
            {
                if (dbf.Value.DbfInfo.TableName.Length > 0)
                    dbList += dbf.Value.DbfInfo.TableName + ",";
            }
            return dbList.Split(',');
        }

        /*---------------------------------------------------------------------------------*
         * Check all workareas in the current datasession, looking for an alias that 
         * equals the tablename Return work area if found
         *---------------------------------------------------------------------------------*/
        public int TableUsed(string tablename)
        {
            int isUsed = -1;

            foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
            {
                if (dbf.Value.DbfInfo.Alias.Equals(tablename, StringComparison.OrdinalIgnoreCase))
                {
                    isUsed = dbf.Key;
                    break;
                }
            }

            return isUsed;
        }


        /*---------------------------------------------------------------------------------*
         * Return the work area number which holds the specified alias in the current
         * data session.  If the alias is not found, an error is raise.
         *---------------------------------------------------------------------------------*/
        public int GetWorkArea(string alias)
        {
            int wa = -1;

            if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                // Work areas 1 - 10
                wa = "abcdefghij".IndexOf(alias.ToLower());
            }
            else if (alias.Length > 0)
            {
                foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                {
                    if (dbf.Key>0 && dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        wa = dbf.Key;
                        break;
                    }
                }
            }

            if (wa < 0) throw new Exception("Alias not found");
            return wa;
        }

        /*---------------------------------------------------------------------------------*
         * Simple boolean check if alias is a valid work area in this data session
         *---------------------------------------------------------------------------------*/
        public bool IsWorkArea(string alias)
        {
            int wa = -1;

            if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                // Work areas 1 - 10
                wa = "abcdefghij".IndexOf(alias, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (alias.Length > 0)
            {
                foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                {
                    if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        wa = dbf.Key;
                        break;
                    }
                }
            }

            return wa >= 0;
        }


        /*---------------------------------------------------------------------------------*
         * Get's the DbfInfo object for a work area in the current datasession specified 
         * by the provided alias name.  If the alias does not exist, an error is raised.
         *---------------------------------------------------------------------------------*/
        public JAXDirectDBF GetWorkAreaObject(string alias)
        {
            int wa = -1;

            if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                // Work areas 1 - 10
                wa = "abcdefghij".IndexOf(alias, StringComparison.OrdinalIgnoreCase);
            }
            else if (alias.Length > 0)
            {
                foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                {
                    if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        wa = dbf.Key;
                        break;
                    }
                }
            }

            if (wa < 0) throw new Exception("Alias not found");
            return WorkAreas[wa];
        }

        /*---------------------------------------------------------------------------------*
         * Returns the DBInfo object for a specified work area in the current data session.
         * If it does not exist then it is initialized so that an empty object is returned.
         *---------------------------------------------------------------------------------*/
        public JAXDirectDBF GetWorkAreaObject(int workarea)
        {
            if (workarea < 0) throw new Exception("3021|");

            if (WorkAreas.ContainsKey(workarea) == false)
                WorkAreas.Add(workarea, new(App));

            return WorkAreas[workarea];
        }


        /*---------------------------------------------------------------------------------*
         * This is essencially the SELECT command in xBase.  If the work area in the
         * current data session is not already initialized then it will be set with an 
         * empty JAXDirectDBF object.
         * 
         * If workarea provided is 0 then it goes to the lowest numberd open work area
         *---------------------------------------------------------------------------------*/
        public void SelectWorkArea(int workarea)
        {
            if (workarea < 0) throw new Exception("3021|");

            // If workarea = 0, find the lowest open one
            if (workarea == 0)
            {
                int wa = 1;
                while (true)
                {
                    if (WorkAreas.ContainsKey(wa) == false)
                    {
                        // This is the lowest open work area
                        workarea = wa;
                        break;
                    }

                    wa++;
                }
            }

            // Now set the current work area
            currentWorkArea = workarea;

            // Initialize it if it's not alreay initialized
            if (WorkAreas.ContainsKey(currentWorkArea) == false)
                WorkAreas.Add(currentWorkArea, new(App));
        }

        /*---------------------------------------------------------------------------------*
         * Select the workarea by alias in the current data session.  If the alias is no
         * found, then an error is raised.
         *---------------------------------------------------------------------------------*/
        public void SelectWorkArea(string alias)
        {
            int wa = -1;

            if (alias.Length > 0)
            {
                // Is this a number cast as a string?
                if (JAXLib.ChrTran(alias, "0123456789", "").Length == 0)
                    if (int.TryParse(alias, out wa) == false) wa = -1;
            }

            if (wa > 0)
                SelectWorkArea(wa);    // Found a number cast as a string
            else
            {
                // It's a string, so try to figure it out
                if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
                {
                    // It's a work area alias (1 - 10)
                    wa = "abcdefghij".IndexOf(alias, StringComparison.OrdinalIgnoreCase);
                }
                else if (alias.Length > 0)
                {
                    foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                    {
                        if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                        {
                            wa = dbf.Key;
                            break;
                        }
                    }
                }

                if (wa < 1) throw new Exception("13|" + alias);
                SelectWorkArea(wa);
            }
        }


        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public async Task<JAXObjects.Token> GetFieldToken(string alias, string field) { return await GetFieldToken(alias, field, false); }

        public async Task<JAXObjects.Token> GetFieldToken(string alias, string field, bool getMemoData)
        {
            JAXObjects.Token token;
            int wa = -1;

            foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
            {
                if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                {
                    wa = dbf.Key;
                    break;
                }
            }

            if (wa < 0)
                throw new Exception("Alias not found");
            else
                token = await GetFieldToken(wa, field, getMemoData);   // found it, get the field

            return token;
        }

        /*---------------------------------------------------------------------------------*
         *---------------------------------------------------------------------------------*/
        public bool FieldExists(string field)
        {
            return FieldExists(field, currentWorkArea);
        }

        public bool FieldExists(string field, int wa)
        {
            bool result = false;

            // Is there a table open in the current work area?
            if (WorkAreas.ContainsKey(wa) && WorkAreas[wa].DbfInfo.DBFStream is not null)
            {
                JAXDirectDBF.DBFInfo DbfInfo = WorkAreas[wa].DbfInfo;
                int f = DbfInfo.Fields.FindIndex(x => x.FieldName.Equals(field, StringComparison.OrdinalIgnoreCase));
                if (f >= 0)
                    result = true;
            }

            return result;
        }

        public async Task<JAXObjects.Token> GetFieldToken(int workarea, string field) { return await GetFieldToken(workarea, field, false); }

        public async Task<JAXObjects.Token> GetFieldToken(int workarea, string field, bool getMemoData)
        {
            JAXObjects.Token token = new();
            JAXTables.JAXMemo mInfo;
            if (workarea > 0)
            {
                if (WorkAreas.ContainsKey(workarea) == false || WorkAreas[workarea].DbfInfo.Alias.Length == 0) throw new Exception("3022|" + workarea.ToString());
            }
            else
                workarea = currentWorkArea;

            JAXDirectDBF.DBFInfo DbfInfo = WorkAreas[workarea].DbfInfo;

            try
            {
                if ("TC".Contains(DbfInfo.TableType))
                {
                    if (JAXLib.Between(DbfInfo.CurrentRecNo, 1, DbfInfo.RecCount))
                    {
                        // Tables and cursors only have 1 row loaded in memory at a time
                        string a = DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field].GetType().Name;
                        if (a.Equals("JAXMEMO", StringComparison.OrdinalIgnoreCase))
                        {
                            mInfo = (JAXTables.JAXMemo)DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field];

                            // If blob/general/memo info requested, see if we need to load it
                            if (getMemoData && mInfo.Pointer > 0 && mInfo.Changed < 1)
                            {
                                // Retrieve the missing blob/general/memo field value
                                byte[] buffer = await CurrentWA.FPTRead(mInfo.Pointer);

                                // Load the table with the value returned
                                mInfo.Value = Encoding.UTF8.GetString(buffer);
                                mInfo.Changed = 1;
                                DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field] = mInfo;
                            }

                            // Return the table information
                            token.Element.Value = mInfo;
                        }
                        else
                            token.Element.Value = DbfInfo.CurrentRow.Rows[0][field];

                        // if we're creating an idx expression, make sure we get the correct
                        // empty value, and if text data of the correct space filled length 
                        if (DbfInfo.CreatingIDXExpression)
                        {
                            int f = DbfInfo.Fields.FindIndex(x => x.FieldName.Equals(field, StringComparison.OrdinalIgnoreCase));
                            if (f < 0)
                                throw new Exception("Invalid field expression");
                            else
                            {
                                token.Element.Value = DbfInfo.Fields[f].FieldType switch
                                {
                                    "C" => new string(' ', DbfInfo.Fields[f].FieldLen),
                                    "V" => new string(' ', DbfInfo.Fields[f].FieldLen),
                                    "Q" => new string(' ', DbfInfo.Fields[f].FieldLen),
                                    "T" => DateTime.MinValue,
                                    "D" => DateOnly.MinValue,
                                    "L" => "F",
                                    "I" => 0,
                                    "N" => 0.0D,
                                    "B" => 0.0D,
                                    "Y" => 0.0D,
                                    _ => throw new Exception($"4002|{DbfInfo.Fields[f].FieldName}")
                                };
                            }
                        }
                    }
                    else
                    {
                        // No records or out of range
                        if (DbfInfo.RecCount==0)
                        {
                            // No records, so return empty value
                            token.Element.Value = DbfInfo.CurrentRow.Rows[0][field];
                        }
                        else
                        {
                            // Out of range, throw an error
                            throw new Exception("5|");
                        }
                    }
                }
                else
                {
                    // We're dealing with a multi-row table

                    string a = DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field].GetType().Name;
                    if (a.Equals("JAXMEMO", StringComparison.OrdinalIgnoreCase))
                    {
                        mInfo = (JAXTables.JAXMemo)DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field];

                        if (getMemoData && mInfo.Pointer > 0 && mInfo.Changed == 0)
                        {
                            // Retrieve the field information
                            byte[] buffer = await CurrentWA.FPTRead(mInfo.Pointer);
                            mInfo.Value = Encoding.UTF8.GetString(buffer);
                        }

                        token.Element.Value = mInfo;
                    }
                    else
                        token.Element.Value = DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][field];
                }
                token.Alias = DbfInfo.Alias;
            }
            catch
            {
                token.TType = "U";  // Return unknown token "U"
            }

            return token;
        }

        /*---------------------------------------------------------------------------------*
         * TODO - add $changes support
         *---------------------------------------------------------------------------------*/
        public bool SaveFieldToken(JAXMath jaxMath, string alias, string fldRef, string exprString)
        {
            bool result = true;
            //int wa = -1;

            /*
            // Need to get work area number
            wa = GetWorkArea(alias) - 1;

            if (wa < 0)
                throw new Exception("Unknown alias");
            else
            {
                jaxMath.SolveMath(exprString, out JAXObjects.Token token);

                // Get the correct row number based on the type
                // Tables and cursors only have the current row in memory
                int RecNo = "TC".Contains(Workareas[wa].DbfInfo.TableType) ? 0 : Workareas[wa].DbfInfo.CurrentRecNo - 1;

                int f = Workareas[wa].DbfInfo.Fields.FindIndex(f => f.FieldName.Equals(fldRef, StringComparison.OrdinalIgnoreCase));

                if (token.TType.Equals("U") == false)
                {
                    if (f < 0)
                        throw new Exception(string.Format("Field {0} does not exist", fldRef));

                    // If there's a valid $changes field, update it
                    int c = Workareas[wa].DbfInfo.Fields.FindIndex(f => f.FieldName.Equals("$changes", StringComparison.OrdinalIgnoreCase));
                    if (c >= 0)
                    {
                        char[] change = (Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo]["$changes"].ToString() ?? string.Empty).ToCharArray();
                        if (change.Length >= f)
                        {
                            change[f] = '1';
                            Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo]["$changes"] = new string(change);
                        }
                    }

                    try
                    {
                        switch (token.Element.Type)
                        {
                            case "C":   // character
                            case "V":   // varchar
                            case "Q":   // varchar binary
                            case "M":   // memo
                            case "G":   // general
                            case "W":   // blob
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.Value.ToString();
                                break;

                            case "D":   // dateonly
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.ValueAsDateOnly;
                                break;

                            case "T":   // datetime
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.ValueAsDateTime;
                                break;

                            case "F":   // float
                            case "N":   // numeric
                            case "Y":   // currency
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = (float)token.Element.ValueAsDouble;
                                break;

                            case "B":   // double
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.ValueAsDouble;
                                break;

                            case "I":   // integer
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.ValueAsInt;
                                break;

                            case "L":   // logical
                                Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo][f] = token.Element.ValueAsBool;
                                break;
                        }

                        // TODO - will need to eventually deal with buffering here
                        Workareas[wa].DBFWriteRecord(Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo], false);

                        if (c >= 0)
                            Workareas[wa].DbfInfo.CurrentRow.Rows[RecNo]["$changes"] = new string('0', Workareas[wa].DbfInfo.VisibleFields + 1);
                    }
                    catch
                    {
                        throw new Exception(string.Format("Failed to write field {0} in record {1}", fldRef, RecNo));
                    }
                }
                else
                {
                    throw new Exception("Invalid or unknown expression");
                }
            }
            */

            return result;
        }


        /*---------------------------------------------------------------------------------*
         * Return the zero based work area index by alias string 
         * with a scope:
         *      0 = compare name & alias
         *      1 = compare only Table
         *      2 = compare only Alias
         *---------------------------------------------------------------------------------*/
        public int ReturnWorkArea(string alias, int scope)
        {
            int iWrk = -1;

            if (alias.Length == 1 && alias.ToUpper().CompareTo("K") < 0)
            {
                // We're looking at a work area, not an alias
                iWrk = "ABCDEFGHIJ".IndexOf(alias.ToUpper());
            }
            else
            {
                if (alias.Length > 0)
                {
                    foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                    {
                        if (dbf.Key > 0)
                        {
                            if ((scope != 2 && dbf.Value.DbfInfo.TableName.Equals(alias, StringComparison.OrdinalIgnoreCase)) || (scope != 1 && dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase)))
                            {
                                iWrk = dbf.Key;
                                break;
                            }
                        }
                    }
                }
            }

            return iWrk;
        }


        /*---------------------------------------------------------------------------------*
         * Get the lowest zero based open workarea for datasession
         *---------------------------------------------------------------------------------*/
        public int GetLowestOpenWorkArea()
        {
            int nResult = -1;

            int i = 1;
            while (i<int.MaxValue)
            {
                if (WorkAreas.ContainsKey(i) == false)
                {
                    nResult = i;
                    break;
                }
            }

            if (nResult < 0) throw new Exception("8090|");
            return nResult;
        }

        /*---------------------------------------------------------------------------------*
         * Get the highest open workarea for current datasession
         * Returns highest currently used +1 if highest > 32767
         * otherwise returns 32767
         *---------------------------------------------------------------------------------*/
        public int GetHighestOpenWorkArea()
        {
            int nResult = -1;

            // Go through each work area and compare the key
            foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
            {
                if (dbf.Key>0 && dbf.Key > nResult)
                {
                    nResult = dbf.Key;
                    break;
                }
            }

            nResult = nResult < 32767 ? 32767 : nResult + 1;
            return nResult;
        }

        /*-----------------------------------------------------------------------------------*
         * TODO
         * Add/Alter/Delete a field
         *      If the field exists and field len>0 it's an alter
         *      If the field exists and field len=0 it's a delete
         *      If the field doesn't exist, it's a field add
         *      
         *      FieldInfo class holds the column information for the alter
         *-----------------------------------------------------------------------------------*/
        public bool AlterTable(string tableName, JAXTables.FieldInfo fieldInfo)
        {
            bool result = true;

            try
            {
                // Is the table open in more than one work area
                int wa = -1;
                string table = tableName.Trim().ToUpper();

                if (tableName.Length == 0)
                    throw new Exception("Table name expected");
                else
                {
                    foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                    {
                        if (dbf.Value.DbfInfo.Alias.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                            dbf.Value.DbfInfo.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        {
                            wa = dbf.Key;
                            break;
                        }
                    }

                    /*
                    // Look for the table alias
                    for (int i = 0; i < Workareas.Length; i++)
                    {
                        if (Workareas[i].DbfInfo.Alias.Equals(tableName, StringComparison.OrdinalIgnoreCase) ||
                            Workareas[i].DbfInfo.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase))
                        {
                            wa = i;
                            break;
                        }
                    }
                    */
                }

                if (wa < 0)
                    wa = GetHighestOpenWorkArea();

                if (wa > 0)
                {
                    // Select the work aea

                    // Set up everything

                    // Is it open?  If not, open it

                    // Get the field list and close the table

                    // Update the field list

                    // Create the new table

                    // First time through, do it one record at a time

                    // Clean up
                }


            }
            catch (Exception ex)
            {
                result = false;
                App.SetError(8030, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return result;
        }

        /*-----------------------------------------------------------------------------------*
         * Append from data source
         *      dataType
         *          0       Array
         *          1       DBF
         *          2       CSV
         *          3       TAB
         *          4       SDF
         *          5       XLS - not implemented
         *-----------------------------------------------------------------------------------*/
        public bool AppendFrom(string alias, string dataSource, int dataType)
        {
            int wa = -1;

            if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                // Work areas 1 - 10
                wa = "abcdefghij".IndexOf(alias.ToLower());
            }
            else if (alias.Length > 0)
            {
                foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                {
                    if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        wa = dbf.Key;
                        break;
                    }
                }

                /*
                // Look for the table alias
                for (int i = 0; i < workArea.Length; i++)
                {
                    if (Workareas[i].DbfInfo.Alias.ToLower() == workArea.ToLower().Trim())
                    {
                        wa = i;
                        break;
                    }
                }
                */
            }

            if (wa < 0) throw new Exception("Alias not found");
            return AppendFrom(wa, dataSource, dataType);
        }

        public bool AppendFrom(int workArea, string dataSource, int dataType)
        {
            bool result = true;
            try

            {

            }
            catch (Exception ex)
            {
                result = false;
                App.SetError(8031, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return result;
        }

        /*-----------------------------------------------------------------------------------*
         * Copy to table/file from specified alias/workarea
         * 
         * Scope - 0 = all, -115=record 115, 100=next 100, recCount=rest
         *          0       Array
         *          1       DBF
         *          2       CSV
         *          3       TAB
         *          4       SDF
         *          5       XLS - not implemented
         *-----------------------------------------------------------------------------------*/
        public bool CopyTo(string alias, string dataTarget, int dataType, string fieldList, int scope, string forExpr)
        {
            int wa = -1;

            if (alias.Length == 1 && "abcdefghij".Contains(alias, StringComparison.OrdinalIgnoreCase))
            {
                // Work areas 1 - 10
                wa = "abcdefghij".IndexOf(alias, StringComparison.OrdinalIgnoreCase);
            }
            else if (alias.Length > 0)
            {
                foreach (KeyValuePair<int, JAXDirectDBF> dbf in WorkAreas)
                {
                    if (dbf.Value.DbfInfo.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    {
                        wa = dbf.Key;
                        break;
                    }
                }

                /*
                // Look for the table alias
                for (int i = 0; i < workArea.Length; i++)
                {
                    if (Workareas[i].DbfInfo.Alias.ToLower() == workArea.ToLower().Trim())
                    {
                        wa = i;
                        break;
                    }
                }
                */
            }

            if (wa < 0) throw new Exception("Alias not found");
            return CopyTo(wa, dataTarget, dataType, fieldList, scope, forExpr);
        }

        public bool CopyTo(int workArea, string dataTarget, int dataType, string fieldList, int scope, string forExpr)
        {
            bool result = true;

            try
            {

            }
            catch (Exception ex)
            {
                result = false;
                App.SetError(8032, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            return result;
        }

        public bool CopyStructureExtended(string targetTable, string DBCName, string longFileName, string fieldList)
        {
            return false;
        }
    }
}
