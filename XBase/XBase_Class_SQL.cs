/*
 * 2025.11.10 - JLW
 *      This is the JAXBase SQL class.  It's a front-end that uses SQLClass objects
 *      for MSSQL, MySQL, and PostgreSQL
 * 
 *      The first thing I need to do is figure out sub-classing so that the specific
 *      diffences for each engine are in separate code bases.
 * 
 *      Engine property will determine what type of backend.
 * 
 *      Value   Description
 *          0   Not initialized
 *          1   MS SQL Server
 *          2   MySQL
 *          3   PostgreSQL
 *          
 *     Version 2 plans
 *          4   Oracle
 *          
 * 2025.11.15 - JLW
 *      Now that I'm into it and have the code written for MS SQL, MySQL, and PostgreSQL, 
 *      I realize I could have made a class and subclassed the different engines.  
 *      Not a biggie and, once again, something to remember for Version 2.
 * 
 *      I am stopping with the three engines for Version 1 and will make plans for more 
 *      engines in Version 2.  I have had some thoughts about adding in NOSQL capabilities 
 *      to support Mongo, NOSQL, and Apache Cassandra.  However, I don't think that's
 *      worth the effort for Version 2 unless some really talented people were to
 *      sign on to help.
 *      
 * 2025.11.16 - JLW
 *      I got my Merlin SQL Server back up and will start testing soon.
 *      
 *      
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities.Utilities;
using System.Data;
using System.Text.RegularExpressions;
using ZXing;

namespace JAXBase.XBase
{
    public class XBase_Class_SQL : XBase_Class_Avalonia
    {
        public SQLClass? MyConnection = null;

        public XBase_Class_SQL(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "sql" : name;
            SetVisualObject(null, "SQL", name, false, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here to get the value of the 
         * property from the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName.ToLower())
                {
                    case "connectionstring":
                        if (MyConnection is not null)
                            returnToken.Element.Value = MyConnection.GetConnectionString();
                        else
                            returnToken.Element.Value = string.Empty;
                        break;
                    case "isconnected":
                        if (MyConnection is not null && MyConnection.GetState() == 1)
                            returnToken.Element.Value = true;
                        else
                            returnToken.Element.Value = false;
                        break;

                    case "state":
                        if (MyConnection is not null)
                            returnToken.Element.Value = MyConnection.GetState();
                        else
                            returnToken.Element.Value = 0;
                        break;

                    // Intercept special handling of properties
                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }
        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *     -1   - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName)
                    {
                        case "baseclass":
                        case "class":
                        case "classlibrary":
                        case "parent":
                        case "parentclass":
                        case "isconnected":
                        case "state":
                            // These are protected properties
                            result = 3024;
                            break;

                        // Intercept special handling of properties
                        case "database":
                        case "name":
                        case "password":
                        case "server":
                        case "userid":
                            if (MyConnection is null || MyConnection.GetState() == 0)
                            {
                                if (tk.Element.Type.Equals("C"))
                                    result = await base.SetProperty(propertyName, objValue, objIdx);
                                else
                                    result = 11;
                            }
                            else
                                result = 3100;
                            break;

                        case "comment":
                        case "tag":
                            result = await base.SetProperty(propertyName, tk.AsString(), objIdx);
                            break;

                        case "authentication":
                        case "engine":
                        case "port":
                            if (MyConnection is null || MyConnection.GetState() == 0)
                            {
                                if (tk.Element.Type.Equals("N"))
                                    result = await base.SetProperty(propertyName, objValue, objIdx);
                                else
                                    result = 11;
                            }
                            else
                                result = 3100;
                            break;

                        case "encryption":
                        case "security":
                            if (MyConnection is null || MyConnection.GetState() == 0)
                            {
                                if (tk.Element.Type.Equals("L"))
                                    result = await base.SetProperty(propertyName, objValue, objIdx);
                                else if (tk.Element.Type.Equals("N"))
                                {
                                    objValue = tk.AsInt() == 0 ? 0 : 1;
                                    result = await base.SetProperty(propertyName, objValue, objIdx);
                                }
                                else
                                    result = 11;
                            }
                            else
                                result = 3100;
                            break;

                        default:
                            // Just update the standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 1, 10))
                    {
                        // First, we check to make sure that the property exists
                        if (UserProperties.ContainsKey(propertyName))
                        {
                            // Visual object common property handler
                            switch (propertyName)
                            {
                                default:
                                    // We processed it or just need to save the property (perhaps again)
                                    // Ignore the CA1854 as it won't put the value into the property
                                    if (UserProperties.ContainsKey(propertyName))
                                        UserProperties[propertyName].Element.Value = objValue;
                                    else
                                        result = 1559;

                                    break;
                            }
                        }
                        else
                            result = 1559;
                    }
                }
                else
                    result = 1559;

                // Deal with errors
                if (result > 10)
                {
                    _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                    if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                        App.SetError(result, $"{result}|", string.Empty);

                    result = -1;
                }
                else
                    result = 0;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Call the JAXCode for a method
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> _CallMethod(string methodName)
        {
            int result = 0;
            string msg = "";

            try
            {
                if (Methods.ContainsKey(methodName.ToLower()))
                {
                    string cCode = Methods[methodName.ToLower()].CompiledCode;

                    // Execute the code
                    if (cCode.Length > 0)
                    {
                        // Call the routine to compile and execute a block of code
                        _ = App.JaxExecuter.ExecuteCodeBlock(me, methodName, cCode);
                    }
                    else
                        await DoDefault(methodName);
                }
                else
                {
                    msg = methodName;
                    result = 6501;
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = 9999;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{msg}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }

        /*
         * This is where the default actions for the methods occur
         */
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            string errMsg = string.Empty;

            switch (methodName.ToLower())
            {
                case "connect":
                    result = SQLConnect();
                    break;

                case "createtable":
                    result = SQLCreateTable();
                    break;

                case "createdatabase":
                    result = SQLCreateDatabase();
                    break;

                case "disconnect":
                    result = SQLDisconnect();
                    break;

                case "exec":
                    result = await SQLExec();
                    break;

                case "getindex":
                    result = SQLGetIndex();
                    break;

                case "gettable":
                    result = await SQLGetTable();
                    break;

                case "listindexes":
                    result = SQLListIndexes();
                    break;

                case "listtables":
                    result = SQLListTables();
                    break;

                case "listdatabases":
                    result = SQLListDatabases();
                    break;

                default:
                    // Try base code
                    await base.DoDefault(methodName);
                    break;
            }

            string info = "";

            // Process any errors
            if (result > 0)
            {
                info = result switch
                {
                    11 => string.Empty,
                    333 => JAXLib.JustPath(UserProperties["filename"].AsString()),
                    401 => string.Empty,
                    1705 => string.Empty,
                    1737 => methodName.ToUpper(),
                    _ => string.Empty,
                };
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{info}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }

        public override string[] JAXMethods()
        {
            return [
                "addproperty",
                "createtable", "createdatabase","createindex","createview","connect",
                "drop","disconnect",
                "exec",
                "getindex","gettable","getview",
                "list",
                "truncate",
                "update",
                "writeexpression", "writemethod", "zorder"];
        }

        public override string[] JAXEvents()
        {
            return ["connected", "disconnected", "destroy", "error", "init", "load"];
        }

        /*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - can't change after initialization
         *          $ Special Handling - do not auto process
         */
        public override string[] JAXProperties()
        {
            return [
                "authentication,n,0",
                $"appname,C,{App.AppLevels[0].PrgName}",
                "baseclass,C!,SQL",
                "class,C!,SQL","classlibrary,C$,","comment,C,","connectionstring,c,",
                "database,c,",
                "encryption,L,.F.",
                "engine,N,0",
                "integratedsecurity,L,.F.",
                "isconnected,L!,.F.",
                "name,C,SQL",
                "parent,o$,","parentclass,C$,",
                "password,c,",
                "port,n,0",
                "security,L,.F.",
                "server,c,",
                "state,n!,0",
                "trustservercertificate,L,.T.",
                "trustservercertificate,L,.T.",
                "tag,C,",
                "userid,c,"
                ];
        }

        // --------------------------------------------------------------
        // COMMAND-KIND DETECTION
        // --------------------------------------------------------------
        public enum CommandKind { Select, Scalar, NonQuery }

        private static readonly Regex _selectRegex = new Regex(
            @"^\s*(SELECT)\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _scalarRegex = new Regex(
            @"^\s*(SELECT)\s+COUNT\s*\(|TOP\s*1\s|TOP\s*\(\s*\d+\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static CommandKind DetectCommandKind(string sql)
        {
            // Trim comments
            sql = Regex.Replace(sql, @"--.*?$|/\*.*?\*/", string.Empty,
                                 RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var match = _selectRegex.Match(sql);
            if (!match.Success) return CommandKind.NonQuery;

            // If it starts with SELECT but looks like a scalar (COUNT(*) or TOP 1)
            if (_scalarRegex.IsMatch(sql))
                return CommandKind.Scalar;

            return CommandKind.Select;
        }

        // --------------------------------------------------------------
        // Connect to the SQL Engine
        // --------------------------------------------------------------
        private int SQLConnect()
        {
            int result = 0;
            string errMsg = string.Empty;

            if (MyConnection is null)
            {
                try
                {
                    // Select the SQL Engine class and set it up
                    switch (UserProperties["engine"].AsInt())
                    {
                        case 0: // Not chosen
                            result = 6005;
                            break;

                        case 1: // SQL Server
                            MyConnection = new XBase_ClassSQL_MSSS(App);
                            break;

                        case 2: // MySQL
                            MyConnection = new XBase_ClassSQL_MYSQL(App);
                            break;

                        case 3: // PostGreSQL
                            MyConnection = new XBase_ClassSQL_POSTGRE(App);
                            break;

                        default:    // Not chosen
                            result = 1999;
                            errMsg = $"Not implemented: Engine={UserProperties["engine"].AsInt()}";
                            break;

                    }
                }
                catch (Exception ex)
                {
                    result = 9999;
                    errMsg = ex.Message;
                }

                if (result == 0)
                {
                    string par = $"authentication={UserProperties["authentication"].AsInt()};";
                    par += $"port={UserProperties["port"].AsInt()};";
                    par += $"database={UserProperties["database"].AsString()};";
                    par += $"datasource={UserProperties["server"].AsString()};";
                    par += $"password={UserProperties["password"].AsString()};";
                    par += $"userid={UserProperties["userid"].AsString()};";
                    par += $"encryption={(UserProperties["encryption"].AsBool() ? ".T." : ".F.")};";
                    par += $"integratedsecurity={(UserProperties["security"].AsBool() ? ".T." : ".F.")};";
                    par += $"applicationname={UserProperties["appname"]};";
                    par += $"trustservercertificate={(UserProperties["trustservercertificate"].AsBool() ? ".T." : ".F.")};";
                    MyConnection!.SetParameterString(par);
                    result = MyConnection!.Connect();
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Execute a SQL command
        // --------------------------------------------------------------
        private async Task<int> SQLExec()
        {
            int result = 0;
            string msg = string.Empty;

            // Returns -1 if error otherwise returns
            // number of rows returned or affected by
            // the sql statement.
            if (MyConnection is null)
            {
                result = 6004;
            }
            else
            {
                string sqlStmt = string.Empty;
                string sqlCursor = string.Empty;
                object? returnObject = null;

                if (App.ParameterClassList.Count == 0)
                {
                    // ERROR!
                    result = 1498;
                }
                else
                {
                    if (App.ParameterClassList[0].token.Element.Type.Equals("C"))
                    {
                        sqlStmt = App.ParameterClassList[0].token.AsString();
                    }
                    else
                    {
                        // ERROR!
                        result = 11;
                    }

                    if (App.ParameterClassList.Count == 2)
                    {
                        if (App.ParameterClassList[1].token.Element.Type.Equals("C"))
                            sqlCursor = App.ParameterClassList[1].token.AsString();
                        else
                        {
                            result = 11;
                        }
                    }
                    else if (App.ParameterClassList.Count > 2)
                        result = 1230;
                }


                if (result > 0)
                {
                    // Deal with errors before trying to execute
                    _AddError(result, 0, msg, App.AppLevels[^1].Procedure);

                    if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                        App.SetError(result, $"{result}|{msg}", string.Empty);

                    App.ReturnValue.Element.Value = -1;
                }
                else
                {
                    sqlCursor = string.IsNullOrWhiteSpace(sqlCursor) ? "sqlresult" : sqlCursor;

                    // Execute the SQL Statement
                    result = MyConnection.Execute(sqlStmt, out returnObject);

                    if (result < 0)
                    {
                        // Retrieve the error and log it
                        JAXErrors err = MyConnection.GetErrorMsg();
                        _AddError(err.ErrorNo, 0, err.ErrorMessage, App.AppLevels[^1].Procedure);

                        if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                            App.SetError(result, $"{result}|", string.Empty);

                        App.ReturnValue.Element.Value = -1;
                    }
                    else
                    {
                        if (returnObject is not null)
                        {
                            string type = returnObject.GetType().Name;

                            // Only valid right after Execute
                            switch (MyConnection.GetKind())
                            {
                                // Data Table
                                case 1:
                                    DataTable dt = (DataTable)returnObject;
                                    App.ReturnValue.Element.Value = dt.Rows.Count;

                                    // Create a cursor for the datatable
                                    await TableHelper.MakeCursorForDataTable(App, dt, sqlCursor);
                                    break;

                                // Scalar
                                case 2:
                                    App.ReturnValue.Element.Value = 1;

                                    // Create a cursor
                                    break;

                                // NonQuery
                                case 3:
                                    App.ReturnValue.Element.Value = result;
                                    break;

                                default:
                                    _AddError(1400, 0, "Invalid or unknown sql statement type", App.AppLevels[^1].Procedure);
                                    App.ReturnValue.Element.Value = -1;
                                    break;
                            }
                        }
                    }
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        // --------------------------------------------------------------
        // Create a table
        // --------------------------------------------------------------
        private int SQLCreateTable()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
            }
            else
                result = MyConnection.CreateTable("", []);

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Create a database
        // --------------------------------------------------------------
        private int SQLCreateDatabase()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
            }
            else
                result = MyConnection.CreateDatabase("");

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Disconnect engine connection
        // --------------------------------------------------------------
        private int SQLDisconnect()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
            }
            else
                result = MyConnection.Disconnect();

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Get an index and return the creation string
        // --------------------------------------------------------------
        private int SQLGetIndex()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
                App.ReturnValue.Element.Value = string.Empty;
            }
            else
            {
                result = MyConnection.GetIndex("", out string idxInfo);
                App.ReturnValue.Element.Value = idxInfo;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Get a table structure an place it into an array
        // --------------------------------------------------------------
        private async Task<int> SQLGetTable()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
            }
            else
            {
                if (App.ParameterClassList.Count == 1)
                {
                    if (App.ParameterClassList[0].token.Element.Type.Equals("C"))
                    {
                        string varName = App.ParameterClassList[0].token.AsString();

                        // Is it a variable?

                        result = MyConnection.GetTableStructure("", out List<JAXTables.FieldInfo> fldList);

                        if (fldList.Count > 0)
                        {
                            // Update the variable as an array
                            App.SetVarOrMakePrivate(varName, fldList.Count, 18, true);
                            JAXObjects.Token tk = await App.GetVarToken(varName);

                            int i = 0;
                            foreach (JAXTables.FieldInfo fld in fldList)
                            {
                                tk._avalue[i * 18 + 0].Value = fld.FieldName;
                                tk._avalue[i * 18 + 1].Value = fld.FieldType;
                                tk._avalue[i * 18 + 2].Value = fld.FieldLen;
                                tk._avalue[i * 18 + 3].Value = fld.FieldDec;
                                tk._avalue[i * 18 + 4].Value = fld.NullOK;
                                tk._avalue[i * 18 + 5].Value = fld.NoCPTrans;
                                tk._avalue[i * 18 + 6].Value = fld.BinaryData;
                                tk._avalue[i * 18 + 7].Value = fld.EmptyValue;
                                tk._avalue[i * 18 + 8].Value = fld.DefaultValue;
                                //tk._avalue[i * 18 + 9].Value = fld.;
                                //tk._avalue[i * 18 + 10].Value = fld.;
                                //tk._avalue[i * 18 + 11].Value = fld.;
                                tk._avalue[i * 18 + 12].Value = fld.TableName;
                                //tk._avalue[i * 18 + 13].Value = fld.;
                                tk._avalue[i * 18 + 14].Value = fld.Caption;
                                tk._avalue[i * 18 + 15].Value = fld.Comment;
                                tk._avalue[i * 18 + 16].Value = fld.AutoIncNext;
                                tk._avalue[i * 18 + 17].Value = fld.AutoIncStep;
                            }
                        }
                    }
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Return list of indexes
        // --------------------------------------------------------------
        private int SQLListIndexes()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
                App.ReturnValue.Element.Value = string.Empty;
            }
            else
            {
                result = MyConnection.ListIndexes(out List<string> idxList);
                string idxlist = string.Empty;
                foreach (string idx in idxList)
                    idxlist += idx + ";";

                App.ReturnValue.Element.Value = idxlist;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Return list of tables
        // --------------------------------------------------------------
        private int SQLListTables()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
                App.ReturnValue.Element.Value = string.Empty;
            }
            else
            {
                result = MyConnection.ListTables(out List<string> tblList);
                string tbllist = string.Empty;
                foreach (string tbl in tblList)
                    tbllist += tbl + ";";

                App.ReturnValue.Element.Value = tbllist;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }

        // --------------------------------------------------------------
        // Return list of databases
        // --------------------------------------------------------------
        private int SQLListDatabases()
        {
            int result = 0;
            if (MyConnection is null)
            {
                result = 6004;
                App.ReturnValue.Element.Value = string.Empty;
            }
            else
            {
                result = MyConnection.ListDatabases(out List<string> dbList);
                string dblist = string.Empty;
                foreach (string db in dbList)
                    dblist += db + ";";

                App.ReturnValue.Element.Value = dbList;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }
    }
}
