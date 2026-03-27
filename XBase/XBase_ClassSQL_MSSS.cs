/* 
 * MSSQL Engine support
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
using Microsoft.Data.SqlClient;
using System.Data;
using JAXBase.Utilities.Utilities;
using JAXBase.Language;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_MSSS : SQLClass
    {
        AppClass App;

        private int ErrorNo = 0;
        private string ErrorMsg = string.Empty;
        private string ErrorProc = string.Empty;
        private int ErrorLine = 0;

        private string _appName = string.Empty;
        Microsoft.Data.SqlClient.SqlConnection? SQLCon = null;
        private string ApplicationName
        {
            get { return _appName; }
            set { _appName = $"{value}:{App.MyInstance}"; }
        }

        SqlAuthenticationMethod AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
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

        public XBase_ClassSQL_MSSS(AppClass app)
        {
            App = app;

            ApplicationName = "JAXBase";
            WorkStation = Environment.MachineName;

        }

        public int AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;

            try
            {
            }
            catch (Exception ex) { result = 9999; ErrorMsg = ex.Message; }

            return result;
        }

        public int Connect()
        {
            int result = 0;
            string msg = string.Empty;
            SqlConnectionStringBuilder builder = new();

            try
            {
                builder.ConnectTimeout = ConnectionTimeout;
                builder.ApplicationName = ApplicationName;
                builder.Authentication = AuthenticationMethod;   // Default for Linux
                builder.DataSource = DataSource;                                // IP or host name
                builder.Encrypt = Encryption;
                builder.InitialCatalog = Database;
                builder.IntegratedSecurity = IntegratedSecurity;
                builder.Password = ConnectionPassword;
                builder.TrustServerCertificate = TrustServerCertificate;
                builder.UserID = ConnectionUserID;
                builder.WorkstationID = WorkStation;

                //builder.CurrentLanguage = string.Empty;
                //builder.IPAddressPreference = SqlConnectionIPAddressPreference.IPv4First;
                //builder.PersistSecurityInfo = false;
                //builder.ServerCertificate = string.Empty;
                //builder.ServerSPN = string.Empty;
            }
            catch (Exception ex) { result = 9999; msg = ex.Message; }

            // Try to make the connection
            if (result == 0)
            {
                try
                {
                    string connString = builder.ConnectionString;
                    SQLCon = new(connString);
                    SQLCon.Open();
                }
                catch (Exception ex) { result = 9999; msg = ex.Message; }
            }

            if (result > 0 && SQLCon is not null)
            {
                // Make sure things get closed up on an error
                try { SQLCon.Close(); } catch { }
                SQLCon = null;
            }

            if (result > 0)
            {
                result = -1;
                SetError(result, msg, "Connect");
            }

            return result;
        }

        public int CreateIndex(string tableName, string indexinfo)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        public int CreateSP(string procName, string procCode)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(9999, string.Empty, "CreateSP");
                result = -1;
            }

            return result;
        }


        /*
         * Create a table using JAXBase field information
         *
         * Current data types           Future data types
         *                              A 
         * B Double                     
         * C Character                  
         * D Date                       E 
         * F Float                      
         * G General                    H 
         * I Integer                    J JSON
         *                              K 
         * L Logical
         * M Memo
         * N Numeric                    O 
         *                              P 
         * Q binary data                R 
         *                              S Timestamp
         * T Datetime                   U 
         * V Varchar                    
         * W blob                       X XML
         * Y Currency                   Z DateTime with timezone
         *
         */
        public int CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {
                    string sql = $"CREATE TABLE {tableName} (";
                    for (int i = 0; i < Fields.Count; i++)
                    {
                        JAXTables.FieldInfo field = Fields[i];
                        string fieldType = string.Empty;
                        string defaultValue = string.Empty;

                        switch (field.FieldType.ToUpper())
                        {
                            case "B":
                                fieldType = "FLOAT";
                                defaultValue = "0";
                                break;

                            case "C":
                                if (field.NoCPTrans || field.BinaryData)
                                    fieldType = $"NCHAR({field.FieldLen})";
                                else
                                    fieldType = $"CHAR({field.FieldLen})";

                                defaultValue = "''";
                                break;

                            case "D":
                                fieldType = "DATE";
                                break;

                            case "G":
                            case "W":
                                fieldType = "VARBINARY(MAX)";
                                break;

                            case "I":
                                fieldType = "INT";
                                defaultValue = "0";
                                break;

                            case "L":
                                fieldType = "BIT";
                                defaultValue = "0";
                                break;

                            case "M":
                                if (field.NoCPTrans)
                                    fieldType = "NVARCHAR(MAX)";
                                else
                                    fieldType = "VARCHAR(MAX)";
                                break;

                            case "F":
                            case "N":
                                fieldType = $"DECIMAL({field.FieldLen},{field.FieldDec})";
                                defaultValue = "0";
                                break;

                            case "Q":
                                if (field.NoCPTrans || field.BinaryData)
                                    fieldType = $"VARBINARY({field.FieldLen})";
                                else
                                    fieldType = $"VARCHAR({field.FieldLen})";

                                defaultValue = "''";
                                break;

                            case "T":
                                fieldType = "DATETIME2";
                                break;

                            case "V":
                                if (field.NoCPTrans || field.BinaryData)
                                    fieldType = $"VARBINARY({field.FieldLen})";
                                else
                                    fieldType = $"VARCHAR({field.FieldLen})";

                                defaultValue = "''";
                                break;

                            case "Y":
                                fieldType = "MONEY";
                                defaultValue = "0";
                                break;

                            default:
                                result = 6100;
                                SetError(result, field.FieldType, "CreateTable");
                                break;
                        }

                        // Was there an error?
                        if (result > 0) break;

                        // Default values and accepting nulls
                        if (field.NullOK)
                            fieldType += " NULL";
                        else
                        {
                            if (defaultValue.Length > 0)
                                fieldType += $" default {defaultValue} NOT NULL";
                            else
                                fieldType += " NOT NULL";
                        }

                        // Now put it together
                        sql += $"{field.FieldName} {fieldType}";

                        if (i < Fields.Count - 1)
                            sql += ", ";
                    }

                    // Close the sql string up
                    sql += ");";

                    // Create the table now
                    Execute(sql, out object? returnObject);

                    if (returnObject is string s)
                    {
                        // creation failed
                        result = 6200;
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        public int DeleteIndex(string tableName, string indexinfo)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        public int Disconnect()
        {
            int result = 0;

            try
            {
                if (SQLCon is null)
                    result = 6001;
                else
                {
                    if (SQLCon.State == ConnectionState.Open)
                        SQLCon.Close();
                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        /*
         * Drop a table from the database
         */
        public int DropTable(string tableName)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
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

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public int Execute(string sql, out object? returnObject)
        {
            int result = 0;
            sql = sql.Trim();

            using var cmd = new SqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
            var kind = XBase_Class_SQL.DetectCommandKind(sql);

            try
            {
                returnObject = kind switch
                {
                    XBase_Class_SQL.CommandKind.Select => ExecuteSelect(sql, out result),
                    XBase_Class_SQL.CommandKind.Scalar => cmd.ExecuteScalar(),
                    _ => cmd.ExecuteNonQuery()
                };

                ErrorLine = kind switch
                {
                    XBase_Class_SQL.CommandKind.Select => 1,
                    XBase_Class_SQL.CommandKind.Scalar => 2,
                    _ => 3
                };

                if (result < 0) returnObject = null;
            }
            catch (Exception ex)
            {
                result = 9999;

                ErrorLine = kind switch
                {
                    XBase_Class_SQL.CommandKind.Select => 1,
                    XBase_Class_SQL.CommandKind.Scalar => 2,
                    _ => 3
                };

                returnObject = null;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
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
                    result = 6001;
                else
                {
                    using (SqlDataAdapter adapter = new(sql, SQLCon))
                    {
                        adapter.FillSchema(dt, SchemaType.Source);
                        adapter.Fill(dt);
                        DataTable schemaTable = GetNativeSchemaTable(SQLCon, sql);

                        // --------------------------------------------------------------------
                        // Inside LoadQueryWithFullMetadata – ENRICH COLUMNS (null-safe!)
                        foreach (DataColumn col in dt.Columns)
                        {
                            DataRow[] matches = schemaTable.Select($"ColumnName = '{col.ColumnName}'");
                            if (matches.Length == 0)
                            {
                                // Fallback: use .NET type only if no schema info
                                col.ExtendedProperties["Info"] = new ColumnInfo
                                {
                                    SqlType = "<unknown>",
                                    DotNetType = col.DataType.Name,
                                    MaxLength = col.MaxLength,
                                    AllowDBNull = col.AllowDBNull,
                                    SampleValue = dt.Rows.Count > 0 ? dt.Rows[0][col] : DBNull.Value
                                };
                                continue;
                            }

                            DataRow meta = matches[0];

                            // === NULL-SAFE: DataTypeName ===
                            string baseType = meta["DataTypeName"]?.ToString() ?? "unknown";

                            // === NULL-SAFE: ColumnSize, Precision, Scale ===
                            int maxLen = meta["ColumnSize"] is int size ? size : -1;
                            short precision = meta["NumericPrecision"] is short p ? p : (short)0;
                            short scale = meta["NumericScale"] is short s ? s : (short)0;

                            // === Build SQL type string safely ===
                            string sqlType = baseType;

                            if (baseType.IndexOf("char", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                baseType.IndexOf("binary", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                sqlType = $"{baseType}({(maxLen == -1 ? "MAX" : maxLen.ToString())})";
                            }
                            else if (precision > 0)
                            {
                                sqlType = $"{baseType}({precision}{(scale > 0 ? $",{scale}" : "")})";
                            }

                            // === Sample value ===
                            object sample = dt.Rows.Count > 0 ? dt.Rows[0][col] : DBNull.Value;

                            // === Attach enriched info ===
                            col.ExtendedProperties["Info"] = new ColumnInfo
                            {
                                SqlType = sqlType,
                                DotNetType = col.DataType.Name,
                                MaxLength = col.MaxLength,
                                AllowDBNull = col.AllowDBNull,
                                SampleValue = sample
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dt.Clear();
                result =9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return dt;
        }

        // --------------------------------------------------------------------
        // Gets native SQL Server schema for ANY query
        static DataTable GetNativeSchemaTable(SqlConnection conn, string sql)
        {
            // Use FMTONLY to get metadata without executing the query
            using var cmd = new SqlCommand("SET FMTONLY ON; " + sql + "; SET FMTONLY OFF;", conn);
            using var reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly);
            return reader.GetSchemaTable() ?? new DataTable();
        }

        public int ExecuteSP(string procName, List<xParameters> parameters)
        {
            int result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result =  6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
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

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    result = 6001;
                else
                {

                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        public int Setup(List<xParameters> parameters)
        {
            int result = 0;

            try
            {
                foreach (xParameters param in parameters)
                {
                    result = SetParameter(param.Name, param.Value);
                    if (result > 0) break;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        public int SetParameterString(string Parameters)
        {
            int result = 0;
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
                        result = SetParameter(param[0], tk);
                    }
                    else
                        result = 1232;

                    // Break out on any error found
                    if (result != 0) break;
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = 9999;
            }

            if (result > 0)
            {
                SetError(result, msg, "SetParameterString");
                result = -1;
            }

            return result;
        }

        public int SetParameter(string parameter, JAXObjects.Token value)
        {

            int result = 0;
            string type = value.Element.Type;

            try
            {
                switch (parameter.ToLower())
                {
                    case "applicationname":
                        ApplicationName = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "authentication":
                        int authType = type.Equals("N") ? value.AsInt() : throw new Exception($"11|");
                        switch (authType)
                        {
                            case 1:
                                // Windows Authentication
                                AuthenticationMethod = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                                break;

                            case 2:
                                // ActoveDomain Authentication
                                AuthenticationMethod = SqlAuthenticationMethod.ActiveDirectoryDefault;
                                break;

                            case 3:
                                // Active directory password
                                AuthenticationMethod = SqlAuthenticationMethod.ActiveDirectoryPassword;
                                break;

                            default:
                                // UserID/PW
                                AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
                                break;
                        }
                        break;

                    case "port":
                        Port = type.Equals("N") ? value.AsInt() : throw new Exception("11|");
                        Port = Port == 0 ? 1433 : Port;
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

                    case "security":
                        IntegratedSecurity = type.Equals("L") ? value.AsBool() : throw new Exception("11|");
                        break;

                    case "password":
                        ConnectionPassword = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "timeout":
                        ConnectionTimeout = type.Equals("N") ? value.AsInt() : throw new Exception("11|");
                        if (ConnectionTimeout < 0) throw new Exception("3003|");
                        break;

                    case "userid":
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
                        result = -1;
                        SetError(6003, $"Invalid or unknown SQL connection property {parameter.ToUpper()}", "SetParameter");
                        break;
                }
            }
            catch (Exception ex)
            {
                if (ex.Equals("11|"))
                {
                    result = 11;
                    ErrorMsg = $"Function argument value, type, or count is invalid {parameter.ToUpper()}";
                }
                else if (ex.Equals("3003|"))
                {
                    result = 3003;
                    ErrorMsg = "Value or index is out of range|" +
                        (parameter.Equals("port", StringComparison.OrdinalIgnoreCase) ? $"Port ={Port}"
                            : $"ConnectionTimeout={ConnectionTimeout}");
                }
                else
                {
                    result = 9999;
                    ErrorMsg = ex.Message;
                }
            }

            return result;
        }

        public int CreateDatabase(string name) { return 1999; }
        public int GetIndex(string name, out string idxInfo) { idxInfo = string.Empty; return 1999; }
        public int ListDatabases(out List<string> dbList)
        {
            dbList = [];
            int result = Execute("SELECT name FROM sys.databases ORDER BY name", out object? returnObject);

            if (result >= 0)
            {
                if (returnObject is not null)
                {
                    if (GetKind() == 1)
                    {
                        DataTable dt = (DataTable)returnObject;
                        foreach (DataRow row in dt.Rows)
                        {
                            string n = row["name"].ToString() ?? string.Empty;
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
                    }
                }
                else
                {
                    // Error
                    result = 6006;
                }
            }

            if (result > 0)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }


        public int ListIndexes(out List<string> idxList) { idxList = []; return 1999; }

        public int ListTables(out List<string> tblList)
        {
            tblList = [];

            int result = Execute("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME;", out object? returnObject);

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
                    }
                }
                else
                {
                    // Error
                    result = 6006;
                }
            }

            return result;
        }

        public int GetState()
        {
            int result;

            if (SQLCon is null)
                result = 6001;
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

            if (result > 10)
            {
                SetError(result, string.Empty, "ExecuteSelect");
                result = -1;
            }

            return result;
        }

        private void SetError(int errno, string msg, string proc)
        {
            ErrorNo = errno;
            ErrorMsg = msg;
            ErrorProc = JAXErrorList.JAXErrMsg(errno, msg);
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
            {
                result = -1;
                SetError(result, string.Empty, "SetConnectionString");
            }

            return result;
        }

    }
}