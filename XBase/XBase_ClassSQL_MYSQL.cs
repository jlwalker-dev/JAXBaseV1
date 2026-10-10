/* 
 * MSSQL Engine support
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Language;
using JAXBase.Utilities;
using MySqlConnector;
using System.Data;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_MYSQL : SQLClass
    {
        XBase_Class_SQL SQLBase;

        private int ErrorCode = 0;
        private string ErrorMsg = string.Empty;

        private string _appName = string.Empty;
        MySqlConnection? SQLCon = null;
        private string ApplicationName
        {
            get { return _appName; }
            set { _appName = $"{value}:{Program.CurrentApp.MyInstance}"; }
        }

        //SqlAuthenticationMethod AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
        private string WorkStation;

        private string ConnectionString = string.Empty;

        public XBase_ClassSQL_MYSQL(XBase_Class_SQL app)
        {
            SQLBase = app;

            ApplicationName = "JAXBase";
            WorkStation = Environment.MachineName;

        }

        public async Task<int> AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;

            try
            {
            }
            catch (Exception ex) { result = 9999; ErrorMsg = ex.Message; }

            return result;
        }

        public async Task<int> Connect(string connString)
        {
            int result = 0;
            string msg = string.Empty;

            // If there's a connection string, set it up
            if (string.IsNullOrWhiteSpace(connString) == false)
                await SetConnectionString(connString);

            // Try to make the connection
            if (result == 0)
            {
                try
                {
                    ConnectionString = $"Server={SQLBase.UserProperties[JAXLanguageLists.GetWord("server", "REVPEMS")].AsString()};";

                    // Do we have a database connection request?
                    if (string.IsNullOrWhiteSpace(SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString())==false)
                        ConnectionString += $"Database={SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString()};";

                    ConnectionString += $"Uid={SQLBase.UserProperties[JAXLanguageLists.GetWord("userid", "REVPEMS")].AsString()};";
                    ConnectionString += $"Pwd={SQLBase.UserProperties[JAXLanguageLists.GetWord("password", "REVPEMS")].AsString()};";
                    ConnectionString += $"Port={SQLBase.UserProperties[JAXLanguageLists.GetWord("port", "REVPEMS")].AsString()};";

                    switch (SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].AsInt())
                    {
                        case 0:
                            ConnectionString += $"SslMode = Disabled;";
                            break;
                        case 1:
                            ConnectionString += $"SslMode = Preferred;";
                            break;
                        case 2:
                            ConnectionString += $"SslMode = Required;";
                            break;
                        case 3:
                            ConnectionString += $"SslMode = VerifyCA;";
                            break;
                        case 4:
                            ConnectionString += $"SslMode = VerifyFull;";
                            break;
                        default:
                            ConnectionString += $"SslMode = Disabled;";
                            break;
                    }
                    ConnectionString += $"AllowPublicKeyRetrieval=True";

                    var builder = new MySqlConnectionStringBuilder(ConnectionString)
                    {
                        SslMode = MySqlSslMode.Preferred,
                        ApplicationName = $"{ApplicationName}|WS:{WorkStation}|User:{""}"
                    };

                    SQLCon = new(ConnectionString);
                    SQLCon.Open();
                }
                catch (OutOfMemoryException ex) { result = 1; ErrorMsg = ex.Message; }
                catch (ArgumentNullException ex) { result = 3; ErrorMsg = ex.Message; }
                catch (ArgumentOutOfRangeException ex) { result = 4; ErrorMsg = ex.Message; }
                catch (ArgumentException ex) { result = 5; ErrorMsg = ex.Message; }
                catch (FormatException ex) { result = 6; ErrorMsg = ex.Message; }
                catch (Exception ex) { result = 9; ErrorMsg = ex.Message; }
            }

            if (result > 0 && SQLCon is not null)
            {
                // Make sure things get closed up on an error
                try { SQLCon.Close(); } catch { }
                SQLCon = null;
            }

            return result;
        }

        public async Task<bool> IsConnected()
        {
            return SQLCon is not null && JAXLib.InList(SQLCon.State, ConnectionState.Open, ConnectionState.Executing, ConnectionState.Fetching);
        }


        public async Task<int> CreateIndex(string indexName, string tableName, string indexExpression, string filter = "", string attribs = "")
        {
            int result = 0;


            return result;
        }

        public async Task<int> CreateSP(string procName, string procCode)
        {
            int result = 0;

            return result;
        }


        /*
         * Create a table using JAXBase field information
         */
        public async Task<int> CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;

            return result;
        }

        public async Task<int> CreateView(string viewName, string viewCode)
        {
            int result = ErrorCode = 0;

            return result;
        }

        public async Task<int> DeleteDB(string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return 1999;
        }

        public async Task<int> DeleteSP(string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return 1999;
        }
        public async Task<int> DeleteTable(string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return 1999;
        }
        public async Task<int> DeleteView(string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return 1999;
        }
        public async Task<int> DeleteIndex(string indexName, string Name)
        {
            int result = 0;

            return result;
        }

        public async Task<int> Disconnect()
        {
            int result = 0;

            return result;
        }

        /*
         * Drop a table from the database
         */
        public async Task<int> DropTable(string tableName)
        {
            int result = 0;

            return result;
        }

        /*
         * Return the table structure using JAXBase field codes
         */
        public async Task<int> GetTableStructure(string tableName, string varName)
        {
            int result = 0;


            return result;
        }

        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public async Task<int> Execute(string sql, string cursorName)
        {
            int result = 0;
            sql = sql.Trim();

            using var cmd = new MySqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
            var kind = XBase_Class_SQL.DetectCommandKind(sql);

            try
            {
            }
            catch (Exception ex)
            {
                result = 9999;
                ErrorMsg = ex.Message;
            }

            return result;
        }

        private DataTable? ExecuteSelect(string sql, out int result)
        {
            DataTable dt = new();
            result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = -1;
                    //                    SetError(6001, "SQL not connected", "ExecuteSelect");
                }
                else
                {
                    using (MySqlDataAdapter adapter = new(sql, SQLCon))
                    {
                        adapter.FillSchema(dt, SchemaType.Source);
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                dt.Clear();
                result = 9999;
                ErrorMsg = ex.Message;
            }

            return dt;
        }

        public async Task<int> ExecuteSP(string procName, List<xParameters> parameters)
        {
            int result = 0;


            return result;
        }


        public async Task<int> GetSPCode(string procName, string cursorName)
        {
            int result = 0;
            return result;
        }

        public async Task<int> Setup(List<xParameters> parameters)
        {
            int result = 0;

            try
            {
                foreach (xParameters param in parameters)
                {
                    result = await SetParameter(param.Name, param.Value);
                    if (result < 0) break;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                ErrorMsg = ex.Message;
            }

            return result;
        }

        public async Task<int> SetParameterString(string Parameters)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                string[] ParamStrings = Parameters.Split(';');
                for (int i = 0; i < ParamStrings.Length; i++)
                {
                    JAXObjects.Token tk = new();

                    if (ParamStrings[i].Contains('='))
                    {
                        string[] param = ParamStrings[i].Split("=");
                        param[0] = param[0].Trim();
                        param[1] = param[1].Trim();

                        if (param[1].Length > 0)
                        {
                            if ("0123456789".Contains(param[1][0]))
                            {
                                // It's a numeric value
                                if (int.TryParse(param[1], out int iVal) == false) iVal = 0;
                                tk.Element.Value = iVal;
                            }
                            else if (JAXLib.InListC(param[1], ".t.", ".f."))
                            {
                                // It's a boolean
                                tk.Element.Value = param[1].ToLower().Equals(".t.");
                            }
                            else
                            {
                                // Assuming it's a character value
                                tk.Element.Value = param[1];
                            }
                        }
                        else
                        {
                            // Received an empty string
                            tk.Element.Value = string.Empty;
                        }

                        // Now parse it
                        result = await SetParameter(param[0], tk);
                    }
                    else
                        ErrorCode = 1232;

                    // Break out on any error found
                    if (result != 0) break;
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                ErrorCode = 9999;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetParameterString()");
                result = -1;
            }

            return result;
        }

        public async Task<int> SetParameter(string parameter, JAXObjects.Token value)
        {

            int result = ErrorCode = 0;
            string type = value.Element.Type;
            string msg = "";

            parameter = parameter.ToLower();
            string EnglishParameter = JAXLanguageLists.GetWord(parameter, "REVPEMS");

            try
            {
                switch (EnglishParameter.ToLower())
                {
                    case "applicationname":
                        ApplicationName = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "port":
                        if (type.Equals("N"))
                        {
                            int Port = value.AsInt() < 1 ? 3306 : value.AsInt();
                            if (JAXLib.Between(Port, 1, 65535) == false) result = ErrorCode = 3003;
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("port", "REVPEMS")].Element.Value = Port;
                        }
                        else
                            ErrorCode = 11;

                        break;

                    case "database":
                    case "initialcatalog":
                        if (type.Equals("C"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;

                        break;

                    case "server":
                    case "datasource":
                    case "address":
                    case "addr":
                    case "host":
                    case "networkaddress":
                        if (type.Equals("C"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("server", "REVPEMS")].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "pwd":
                    case "pwd1":
                    case "password":
                        if (type.Equals("C"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("password", "REVPEMS")].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "pwd2":
                        break;

                    case "pwd3":
                        break;

                    case "timeout":
                        if (type.Equals("N"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("connecttimeout", "REVPEMS")].Element.Value = value.AsInt();
                        else
                            ErrorCode = 11;

                        if (SQLBase.UserProperties[JAXLanguageLists.GetWord("connecttimeout", "REVPEMS")].AsInt() < 0) result = 3003;
                        break;

                    case "uid":
                    case "usrid":
                    case "userid":
                    case "username":
                    case "user":
                        if (type.Equals("C"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("userid", "REVPEMS")].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "trust":
                        if (type.Equals("L"))
                            SQLBase.UserProperties[JAXLanguageLists.GetWord("trust", "REVPEMS")].Element.Value = value.AsBool();
                        else
                            ErrorCode = 11;
                        break;

                    // TODO - SSL Mode is a MySQL specific parameter and numeric
                    case "sslmode":
                        if (type.Equals("C"))
                            switch (value.AsString().ToLower())
                            {
                                case "none":
                                    SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].Element.Value = 0;
                                    break;
                                case "preferred":
                                    SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].Element.Value = 1;
                                    break;
                                case "required":
                                    SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].Element.Value = 2;
                                    break;
                                case "verifyca":
                                    SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].Element.Value = 3;
                                    break;
                                case "verifyfull":
                                    SQLBase.UserProperties[JAXLanguageLists.GetWord("sslmode", "REVPEMS")].Element.Value = 4;
                                    break;
                                default:
                                    ErrorCode = 6003;
                                    break;
                            }

                        else
                            ErrorCode = 11;
                        break;


                    case "loadbalance":
                        break;

                    case "connectionprotocol":
                        break;

                    case "integratedsecurity":
                        break;

                    case "replication":
                        break;

                    case "pipname":
                        break;

                    case "workstation":
                        if (type.Equals("C"))
                            WorkStation = value.AsString();
                        else
                            ErrorCode = 11;
                        break;


                    // SSL/TLS related parameters
                    case "certificatefile":
                        break;

                    case "certificatepassword":
                        break;

                    case "certificatestorelocation":
                        break;

                    case "certificatethumbprint":
                        break;

                    case "sslcert":
                        break;

                    case "sslkey":
                        break;

                    case "sslca":
                    case "cacertificatefile":
                        break;

                    case "tlsversion":
                        break;

                    case "skipcertificaterevocationcheck":
                        break;

                    case "tlsciphersuites":
                        break;

                    // Pooling related parameters
                    case "ConnectionLifeTime":
                        break;


                    case "ConnectionReset":
                        break;

                    case "ConnectionIdleTimeout":
                        break;

                    case "MinimumPoolSize":
                        break;

                    case "MaximumPoolSize":
                        break;

                    case "DnsCheckInterval":
                        break;


                    // Other parameters
                    case "AllowLoadLocalInfile":
                        break;

                    case "AllowPublicKeyRetrieval":
                        break;

                    case "AllowUserVariables":
                        break;

                    case "AllowZeroDateTime":
                        break;

                    case "ApplicationName":
                        break;

                    case "AutoEnlist":
                        break;

                    case "CancellationTimeout":
                        break;

                    case "CharSet":
                    case "CharacterSet":
                        break;

                    case "ConnectionTimeout":
                        break;

                    case "ConvertZeroDateTime":
                        break;

                    case "DateTimeKind":
                        break;

                    case "DefaultCommandTimeout":
                    case "CommandTimeout":
                        break;

                    case "GuidFormat":
                        break;

                    case "IgnoreCommandTransaction":
                        break;

                    case "IgnorePrepare":
                        break;

                    case "Interactive":
                    case "InteractiveSession":
                        break;

                    case "Keepalive":
                        break;

                    case "NoBackslashEscapes":
                        break;

                    case "OldGuids":
                        break;

                    case "PersistSecurityInfo":
                        break;

                    case "ServerRedirectionMode":
                        break;

                    case "ServerRsaPublicKeyFile":
                        break;

                    case "ServerSPN":
                        break;

                    case "TreatTinyAsBoolean":
                        break;

                    case "UseAffectedRows":
                        break;

                    case "Compress":
                    case "UseCompression":
                        break;

                    case "UseXaTransactions":
                        break;





                    default:
                        ErrorCode = 6003;
                        msg = parameter.ToUpper();
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetParameter()");
            }

            return result;
        }

        public async Task<int> CreateDatabase(string name) { return 1999; }

        public async Task<int> GetIndex(string tableName, string indexName = "", string cursorName = "")
        {
            return 0;
        }

        public int ListDatabases(out List<string> dbList)
        {
            string msg = "";
            int result = 0;
            dbList = [];
            object? returnObject = ExecuteSelect("SELECT SCHEMA_NAME FROM information_schema.SCHEMATA;", out result);


            if (result >= 0)
            {
                if (returnObject is not null)
                {
                    if (GetKind() == 1)
                    {
                        DataTable dt = (DataTable)returnObject;
                        foreach (DataRow row in dt.Rows)
                        {
                            string n = row["SCHEMA_NAME"].ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(n) == false)
                                dbList.Add(n);
                        }

                        // Get the final tally
                        result = dbList.Count;
                    }
                    else
                    {
                        // Error
                        ErrorCode = 6007;
                    }
                }
                else
                {
                    // Error
                    ErrorCode = 6006;
                }
            }
            else
            {
                msg = "Error executing SQL to list databases";
                ErrorCode = 9999;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.ListDatabases()");
                result = -1;
            }

            return result;
        }


        public int ListIndexes(string tableName, out List<string> idxList) { idxList = []; return 1999; }

        public int ListTables(out List<string> tblList)
        {
            int result = 0;
            tblList = [];

            //int result = Execute("SELECT TABLE_NAME AS 'Table' FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME;", out object? returnObject);

            return result;
        }

        public int GetState()
        {
            int result;

            if (SQLCon is null)
                result = -1;
            else
            {
                result = SQLCon.State switch
                {
                    ConnectionState.Closed => 0,
                    ConnectionState.Open => 1,
                    ConnectionState.Connecting => 2,
                    ConnectionState.Executing => 3,
                    ConnectionState.Fetching => 4,
                    ConnectionState.Broken => 5,
                    _ => 6
                };
            }

            return result;
        }

        public int GetKind() { return 0; }

        public string GetConnectionString() { return SQLCon is null ? string.Empty : SQLCon.ConnectionString; }

        public async Task<int> SetConnectionString(string connString)
        {
            int result = ErrorCode = 0;
            string msg = "";

            if (SQLCon is null || SQLCon.State == ConnectionState.Closed)
                result = await SetParameterString(connString);
            else
                ErrorCode = 6004;

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetConnection()");
                result = -1;
            }

            return result;

        }

        public async Task<int> GetDatabaseInfo(string dbName, string cursorName)
        {
            int result = 0;
            return result;
        }

        public async Task<int> GetView(string viewName, string cursorName)
        {
            int result = 0;
            return result;
        }



        public async Task<int> AlterField(string parm1, string parm2, string parm3, int parm4, int parm5)
        {
            return 0;
        }

        public async Task<int> AlterProperty(string parm1, string parm2, string parm3, JAXObjects.Token parm4)
        {
            return 0;
        }

        public async Task<int> AlterIndex(string parm1, string parm2, string parm3, string parm4)
        {
            return 0;
        }

        public async Task<int> DropField(string parm1, string parm2)
        {
            return 0;
        }

        public async Task<int> DropIndex(string parm1, string parm2)
        {
            return 0;
        }
    }
}