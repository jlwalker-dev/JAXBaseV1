/* 
 * PostgreSQL Engine support
 * 
 * All public INT methods return 0 for success and a positive value for an error EXCEPT FOR the following:
 *      GetState() returns -1 for an error and positive value for indicating state
 *      Execute() which will return -1 for an error or 0+ for how manyu rows were returned or affected.
 *      ExecuteSP() which will return -1 for an error or 0+ for how many rows were returned or affected.
 * 
 * The most recent error is always logged and can be retrieved using the GetErrorMsg() method.
 * 
 */
using JAXBase.Core;
using JAXBase.Data;
using Npgsql;
using System.Data;
using JAXBase.Utilities;
using JAXBase.Language;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_POSTGRE : SQLClass
    {
        AppClass App;

        private int ErrorNo = 0;
        private string ErrorMsg = string.Empty;
        private string ErrorProc = string.Empty;
        private int ErrorLine = 0;

        private string _appName = string.Empty;
        NpgsqlConnection? SQLCon = null;
        private string ApplicationName
        {
            get { return _appName; }
            set { _appName = $"{value}:{App.MyInstance}"; }
        }

        //SqlAuthenticationMethod AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
        private int Port = 0;
        private string Database = string.Empty;
        private string DataSource = string.Empty;
        private bool Encryption = false;
        private bool IntegratedSecurity = false;
        private string ConnectionPassword = string.Empty;
        private int ConnectionTimeout = 30;
        private string ConnectionUserID = string.Empty;
        private bool TrustServerCertificate = false;
        private string WorkStation;

        private string ConnectionString = string.Empty;

        public XBase_ClassSQL_POSTGRE(AppClass app)
        {
            App = app;

            ApplicationName = "JAXBase";
            WorkStation = Environment.MachineName;

        }

        public int AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;
            string msg = string.Empty;
            SetError(0, msg, msg);

            try
            {
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
                SetError(result, msg, "AlterTable");

            return result;
        }

        public int Connect()
        {
            int result = 0;
            string msg = string.Empty;
            SetError(0, string.Empty, string.Empty);

            // Try to make the connection
            if (result == 0)
            {
                try
                {
                    ConnectionString = $"Host={DataSource};Database={Database};Username={ConnectionUserID};Password={ConnectionPassword}";

                    var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
                    {
                        SslMode = SslMode.Prefer,
                        ApplicationName = $"{ApplicationName}|WS:{WorkStation}|User:{ConnectionUserID}"
                    };

                    SQLCon = new(builder.ConnectionString);
                    SQLCon.Open();
                }
                catch (OutOfMemoryException ex) { result = 1; ErrorMsg = ex.Message; }
                catch (ArgumentNullException ex) { result = 3; ErrorMsg = ex.Message; }
                catch (ArgumentOutOfRangeException ex) { result = 4; ErrorMsg = ex.Message; }
                catch (ArgumentException ex) { result = 5; ErrorMsg = ex.Message; }
                catch (FormatException ex) { result = 6; ErrorMsg = ex.Message; }
                catch (Exception ex) { result = 9; ErrorMsg = ex.Message; }
            }

            if (result > 0)
            {
                SetError(result, ErrorMsg, "Connect");

                if (SQLCon is not null)
                {
                    // Make sure things get closed up on an error
                    try { SQLCon.Close(); } catch { }
                    SQLCon = null;
                }
            }

            return result;
        }

        public int CreateIndex(string tableName, string indexinfo)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "CreateIndex");
            }

            return result;
        }

        public int CreateSP(string procName, string procCode)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "CreateSP");
            }

            return result;
        }


        /*
         * Create a table using JAXBase field information
         */
        public int CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "CreateTable");
            }

            return result;
        }

        public int DeleteIndex(string tableName, string indexinfo)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "DeleteIndex");
            }

            return result;
        }

        public int Disconnect()
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {
                    if (SQLCon.State == ConnectionState.Open)
                        SQLCon.Close();
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "Disconnect");
            }

            return result;
        }

        /*
         * Drop a table from the database
         */
        public int DropTable(string tableName)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "DropTable");
            }

            return result;
        }

        /*
         * Return the table structure using JAXBase field codes
         */
        public int GetTableStructure(string tableName, out List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;
            Fields = [];
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(result, string.Empty, "GetTableStructure");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "GetTableStructure");
            }

            return result;
        }

        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public int Execute(string sql, out object? returnObject)
        {
            SetError(0, string.Empty, string.Empty);

            int result = 0;
            sql = sql.Trim();

            using var cmd = new NpgsqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
            var kind = XBase_Class_SQL.DetectCommandKind(sql);

            try
            {
                returnObject = kind switch
                {
                    XBase_Class_SQL.CommandKind.Select => ExecuteSelect(sql, out result),
                    XBase_Class_SQL.CommandKind.Scalar => cmd.ExecuteScalar(),
                    _ => cmd.ExecuteNonQuery()
                };

                if (result < 0) returnObject = null;
            }
            catch (Exception ex)
            {
                result = -1;
                SetError(9999, ex.Message, "Execute");
                returnObject = null;
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
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {
                    using (NpgsqlDataAdapter adapter = new(sql, SQLCon))
                    {
                        adapter.FillSchema(dt, SchemaType.Source);
                        adapter.Fill(dt);
                    }

                }
            }
            catch (Exception ex)
            {
                dt.Clear();
                result = -1;
                SetError(9999, ex.Message, "ExecuteSelect");
            }

            return dt;
        }

        public int ExecuteSP(string procName, List<xParameters> parameters)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = -1;
                    SetError(6001, string.Empty, "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = -1;
                SetError(9999, ex.Message, "ExecuteSP");
            }

            return result;
        }

        public JAXErrors GetErrorMsg()
        {
            JAXErrors result = new();
            result.ErrorMessage = ErrorMsg;
            result.ErrorNo = ErrorNo;
            result.ErrorProcedure = ErrorProc;
            return result;
        }

        public int GetSPCode(string procName)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = 6001;
                    SetError(6001, "SQL not connected", "ExecuteSelect");
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "GetSPCode");
            }

            return result;
        }

        public int Setup(List<xParameters> parameters)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                foreach (xParameters param in parameters)
                {
                    result = SetParameter(param.Name, param.Value);
                    if (result != 0) break;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "Setup");
            }

            return result;
        }

        public int SetParameterString(string Parameters)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            try
            {
                string[] ParamStrings = Parameters.Split(';');
                for (int i = 0; i < ParamStrings.Length; i++)
                {
                    JAXObjects.Token tk = new();

                    if (ParamStrings[i].Contains("="))
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
                        result = SetParameter(param[0], tk);
                    }
                    else
                    {
                        result = 1232;
                        SetError(1232, $"", "SetParameter");
                    }


                    // Break out on any error found
                    if (result != 0) break;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                SetError(9999, ex.Message, "SetParameterString");
            }

            return result;
        }

        public int SetParameter(string parameter, JAXObjects.Token value)
        {
            SetError(0, string.Empty, string.Empty);

            int result = 0;
            string type = value.Element.Type;

            try
            {
                switch (parameter.ToLower())
                {
                    case "applicationname":
                        ApplicationName = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "authtype":
                        int authType = type.Equals("N") ? value.AsInt() : throw new Exception($"11|");
                        break;

                    case "port":
                        Port = type.Equals("N") ? value.AsInt() : throw new Exception("11|");
                        if (JAXLib.Between(Port, 1, 65535) == false) throw new Exception($"3003|");
                        break;

                    case "database":
                        Database = type.Equals("C") ? value.AsString() : throw new Exception("11|");
                        break;

                    case "datasource":
                        DataSource = type.Equals("C") ? value.AsString() : throw new Exception("11|");
                        break;

                    case "integratedsecurity":
                        IntegratedSecurity = type.Equals("L") ? value.AsBool() : throw new Exception("11|");
                        break;

                    case "connectionpassword":
                        ConnectionPassword = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "connectiontimeout":
                        ConnectionTimeout = type.Equals("N") ? value.AsInt() : throw new Exception("11|");
                        if (ConnectionTimeout < 0) throw new Exception("3003|");
                        break;

                    case "connectionuserid":
                        ConnectionUserID = type.Equals("C") ? value.AsString() : throw new Exception("11|");
                        break;

                    case "trustservercertificate":
                        TrustServerCertificate = type.Equals("L") ? value.AsBool() : throw new Exception("11|");
                        break;

                    case "encryption":
                        Encryption = type.Equals("L") ? value.AsBool() : throw new Exception("11|");
                        break;

                    case "workstation":
                        WorkStation = type.Equals("C") ? value.AsString() : throw new Exception("11|");
                        break;

                    default:
                        result = 6003;
                        ErrorMsg = $"Invalid or unknown SQL connection property {parameter.ToUpper()}";
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.Equals("11|"))
                {
                    result = 11;
                    ErrorMsg = $"Function argument value, type, or count is invalid {parameter.ToUpper()}";
                    SetError(11, ErrorMsg, "SetParameter");
                }
                else if (ex.Equals("3003|"))
                {
                    result = 11;
                    ErrorMsg = "Value or index is out of range|" +
                        (parameter.Equals("port", StringComparison.OrdinalIgnoreCase) ? $"Port ={Port}"
                            : $"ConnectionTimeout={ConnectionTimeout}");
                    SetError(3003, ErrorMsg, "SetParameter");
                }
                else
                {
                    result = 11;
                    SetError(9999, ex.Message, "SetParameter");
                }
            }

            return result;
        }

        public int CreateDatabase(string name) { return 1999; }
        public int GetIndex(string name, out string idxInfo) { idxInfo = string.Empty; return 1999; }

        public int ListDatabases(out List<string> dbList)
        {
            SetError(0, string.Empty, string.Empty);

            dbList = [];
            int result = Execute("SELECT datname AS \"Database\" FROM pg_database WHERE datistemplate = false AND datname NOT IN ('postgres') ORDER BY datname;", out object? returnObject);

            if (result >= 0)
            {
                if (returnObject is not null)
                {
                    if (GetKind() == 1)
                    {
                        DataTable dt = (DataTable)returnObject;
                        foreach (DataRow row in dt.Rows)
                        {
                            string n = row["datname"].ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(n) == false)
                                dbList.Add(n);
                        }

                        // Get the final tally
                        result = dbList.Count;
                    }
                    else
                    {
                        // Error
                        result = 6007;
                        SetError(6007, string.Empty, "ListDatabase");
                    }
                }
                else
                {
                    // Error
                    result = 9999;
                    SetError(6006, string.Empty, "ListDatabase");
                }
            }

            return result;
        }

        public int ListIndexes(out List<string> idxList) { idxList = []; return 1999; }


        public int ListTables(out List<string> tblList)
        {
            tblList = [];
            SetError(0, string.Empty, string.Empty);

            int result = Execute("SELECT table_schema AS \"Schema\", table_name AS \"Table\" FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema') ORDER BY table_schema, table_name;", out object? returnObject);

            if (result >= 0)
            {
                if (returnObject is not null)
                {
                    if (GetKind() == 1)
                    {
                        DataTable dt = (DataTable)returnObject;
                        foreach (DataRow row in dt.Rows)
                        {
                            string n = row["table_name"].ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(n) == false)
                                tblList.Add(n);
                        }

                        // Get the final tally
                        result = tblList.Count;
                    }
                    else
                    {
                        // Error
                        result = 6007;
                        SetError(6007, string.Empty, "ListTables");
                    }
                }
                else
                {
                    // Error
                    result = 9999;
                    SetError(6006, string.Empty, "ListTables");
                }
            }

            return result;
        }


        public int GetState()
        {
            int result;
            SetError(0, string.Empty, string.Empty);

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

        private void SetError(int errno, string msg, string proc)
        {
            ErrorNo = errno;
            ErrorMsg = JAXErrorList.JAXErrMsg(errno, msg);
            ErrorProc = proc;
            ErrorLine = 0;
        }

        public int GetKind() { return ErrorLine; }


        public string GetConnectionString() { return SQLCon is null ? string.Empty : SQLCon.ConnectionString; }
        public int SetConnectionString(string connString)
        {
            int result = 0;
            SetError(0, string.Empty, string.Empty);

            if (SQLCon is null || SQLCon.State == ConnectionState.Closed)
                result = SetParameterString(connString);
            else
                result = 6004;

            if (result > 0 && ErrorNo == 0)
                SetError(result, string.Empty, "SetConnectionString");

            return result;
        }
    }
}