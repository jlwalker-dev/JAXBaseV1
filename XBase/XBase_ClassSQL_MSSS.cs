/* ================================================================================================= * 
 * MSSQL Engine support
 *  
 * All public INT methods return 0 for success and a -1 for an error, while some will also
 * provide 0+ for number of rows affected EXCEPT:
 * 
 *      GetState() returns -1 for an error and 0+ indicating state
 * 
 * Errors are logged through the parent XBase_Class_SQL.AddError() method.
 * 
 * ================================================================================================= */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Language;
using JAXBase.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Windows.Interop;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_MSSS : SQLClass
    {
        XBase_Class_SQL SQLBase;

        public int ErrorCode { get; private set; } = 0;
        public string ErrorMsg { get; private set; } = "";

        private string _appName = "";

        int kindDetected = 0;
        Microsoft.Data.SqlClient.SqlConnection? SQLCon = null;
        private string ApplicationName
        {
            get { return _appName; }
            set { _appName = $"{value}:{Program.CurrentApp.MyInstance}"; }
        }

        SqlAuthenticationMethod AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
        private string WorkStation;

        public XBase_ClassSQL_MSSS(XBase_Class_SQL app)
        {
            SQLBase = app;

            ApplicationName = "JAXBase";
            WorkStation = Environment.MachineName;

        }

        public async Task<int> AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = ErrorCode = 0;
            string msg = "";

            try
            {
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                msg= ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                ErrorMsg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.AlterTable()");
                result = -1;
            }
            return result;
        }

        public async Task<int> Connect(string connString)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;
            SqlConnectionStringBuilder builder = new();

            // If there's a connection string, set it up
            if (string.IsNullOrWhiteSpace(connString) == false)
                await SetConnectionString(connString);

            // Make the connection object
            try
            {
                builder.ConnectTimeout = SQLBase.UserProperties["connecttimeout"].AsInt();
                builder.ApplicationName = ApplicationName;
                builder.Authentication = AuthenticationMethod;                  // Default for Linux
                builder.DataSource = SQLBase.UserProperties["server"].AsString();                                // IP or host name
                builder.Encrypt = SQLBase.UserProperties["encryption"].AsBool();
                builder.InitialCatalog = SQLBase.UserProperties["database"].AsString();
                builder.TrustServerCertificate = SQLBase.UserProperties["trust"].AsBool();

                // Not using OS authentication
                switch (SQLBase.UserProperties["authtype"].AsInt())
                {
                    case 1:
                        // Windows
                        builder.IntegratedSecurity = true;

                        // MySQL PAM - requires server side changes
                        //builder.IntegratedSecurity = false;
                        //builder.Authentication = MySqlAuthenticationMode.PAM;
                        //builder.IntegratedSecurity = false;
                        //builder["Authentication PAM Service Name"] = "mysql";
                        break;

                    case 2:
                        builder.UserID = SQLBase.UserProperties["userid"].AsString();
                        builder.Password = SQLBase.UserProperties["password"].AsString();
                        break;

                    case 3:
                        builder.Encrypt = true;                     // REQUIRED
                        builder.TrustServerCertificate = false;     // REQUIRED
                        builder.IntegratedSecurity = false;         // Usually disable

                        // Thumbprint support
                        if (string.IsNullOrWhiteSpace(SQLBase.UserProperties["thumbprint"].AsString()) == false)
                            builder["Certificate"] = SQLBase.UserProperties["thumbprint"];

                        // Remove username/password when using client certificate
                        builder.UserID = null;
                        builder.Password = null;

                        // Optional: Force TLS 1.2 or higher
                        builder.Encrypt = true;
                        builder["Encrypt"] = "true";   // sometimes needed for clarity
                        break;
                }

                builder.WorkstationID = WorkStation;
                builder.CommandTimeout = 30;
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                msg= ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            // Try to make the connection
            if (ErrorCode == 0)
            {
                try
                {
                    string cstring = builder.ConnectionString;
                    SQLCon = new(cstring);
                    SQLCon.Open();
                }
                catch (Exception ex)
                {
                    ErrorCode = 9999;
                    msg = ex.Message;
                }
            }

            if (ErrorCode > 0 && SQLCon is not null)
            {
                // Make sure things get closed up on an error
                try { SQLCon.Close(); } catch { }
                SQLCon = null;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.Connect()");
                result = -1;
            }

            return result;
        }


        public async Task<bool> IsConnected()
        {
            return SQLCon is not null && JAXLib.InList(SQLCon.State, ConnectionState.Open, ConnectionState.Executing, ConnectionState.Fetching);
        }


        /*
         * Create a new database container
         */
        public async Task<int> CreateDatabase(string name)
        {
            int result = 0;
            ExecuteSelect($"create database {name};", out result);
            return result;
        }

        /*
         * Used to create indexes with simplicity.
         * 
         * Warning: Indexes in SQL are huge deals that take some specific knowledge on how to
         * make them useful as the table grows bigger.  This routine is meant to make it
         * easy to build sql indexes, but it is meant more for indexes that don't fragment
         * (that means the data is placed into the table in the natural sorted order, for
         * instance a primary key index where the next primary key is always larger than
         * the last) or static tables of pretty much any size.
         * 
         * In my experience, a table that has an index that takes up 30 bytes per record
         * and is under 250,000 records in size is unlikely to become a problem.
         * 
         */
        public async Task<int> CreateIndex(string indexName, string Name, string indexExpression, string filter = "", string attribs = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected()==false)
                    result = ErrorCode = 6001;
                else
                {
                    attribs = attribs.ToUpper();

                    string schema = SQLBase.UserProperties["schema"].AsString();
                    string[] test = Name.Split('.');

                    if (test.Length == 1)
                        Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

                    string lcSQL = "";

                    // Primary key
                    if (attribs.Contains("P"))
                    {
                        // Drop the primary if it exsits and create it
                        lcSQL += "DECLARE @PKName NVARCHAR(128);";
                        lcSQL += $"SELECT @PKName = name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('{schema + Name}') AND type = 'PK';";
                        lcSQL += $"IF @PKName IS NOT NULL EXEC('ALTER TABLE {schema + Name} DROP CONSTRAINT ' + @PKName);";
                        lcSQL += "alter table " + schema + Name + " add constraint " + indexName + $" primary key ({indexExpression})";
                    }
                    else
                    {
                        // Remove index name if it exists and create it
                        lcSQL += $"DROP INDEX IF EXISTS {indexName} ON {schema + Name};";
                        lcSQL += "create " + (attribs.Contains("C") ? "unique " : "") + (attribs.Contains("U") ? "unique " : "");
                        lcSQL += "index " + indexName + "on";
                        lcSQL += schema + Name + $"({indexExpression})";
                        lcSQL += string.IsNullOrWhiteSpace(filter) ? $"where {filter}" : "";
                        lcSQL += ";";
                    }

                    result = await Execute(lcSQL);
                }
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                msg= ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.CreateIndex()");
                result = -1;
            }

            return result;
        }

        public async Task<int> CreateSP(string Name, string procCode)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;
            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    // TODO - Write the Stored Procedure
                    string schema = SQLBase.UserProperties["schema"].AsString();
                    string[] test = Name.Split('.');

                    if (test.Length == 1)
                        Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.CreateSP()");
                result = -1;
            }

            return result;
        }


        public async Task<int> CreateView(string Name, string viewCode)
        {
            int result = ErrorCode = 0;

            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return result;
        }



        /*
         * Create a table using JAXBase field information
         *
         * Current data types           Future data types
         * ------------------------     ------------------------
         *                              A 
         * B Double                     
         * C Character                  
         * D Date                       E 
         * F Float                      
         * G General                    H 
         * I Integer                    J Block Memo
         *                              K 
         * L Logical
         * M Memo
         * N Numeric                    O Precise Long Integer
         *                              P Precise Double
         * Q VarChar (binary)           R 
         *                              S Timestamp
         * T Datetime                   U 
         * V Varchar                    
         * W Blob                       X 
         * Y Currency                   Z DateTime with timezone
         * 
         * Precise data types will allow you to duplicate COBOL's precision 
         * math capabilities as long as you do not mix them up with other
         * numeric types.
         *
         */
        public async Task<int> CreateTable(string Name, List<JAXTables.FieldInfo> Fields)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string schema = SQLBase.UserProperties["schema"].AsString();
                    string[] test = Name.Split('.');

                    if (test.Length == 1)
                        Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

                    string sql = $"CREATE TABLE {Name} (";
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
                                ErrorCode = 6100;
                                msg = fieldType;
                                break;
                        }

                        if (JAXLib.Between(field.FieldName.Length, 1, 128) == false)
                        {
                            ErrorCode = 6105;
                            msg = "128";
                        }

                        // Was there an error?
                        if (ErrorCode > 0) break;

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
                    using var cmd = new SqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
                    result = cmd.ExecuteNonQuery();
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.CreateTable()");
                result = -1;
            }

            return result;
        }

        public async Task<int> DeleteDB(string Name)
        {
            return 1999;
        }


        public async Task<int> DeleteIndex(string indexName, string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    // Delete the index
                    string lcSQL = "drop index if exists " + indexName + "on" + Name;
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.DeleteIndex()");
                result = -1;
            }

            return result;
        }

        /*
         * Drop a table from the database
         */
        public async Task<int> DeleteTable(string Name)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string lcSQL = $@"DROP TABLE '{Name}';";
                    ExecuteSelect(lcSQL, out result);
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.DropTable()");
                result = -1;
            }

            return result;
        }

        public async Task<int> DeleteSP(string Name)
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



        public async Task<int> Disconnect()
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (SQLCon is null)
                    ErrorCode = 6001;
                else
                {
                    if (SQLCon.State == ConnectionState.Open)
                        SQLCon.Close();
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.Disconnect()");
                result = -1;
            }

            return result;
        }

        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public async Task<int> Execute(string sql, string cursorName = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;
            sql = sql.Trim();
            object? returnObject = null;

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            using var cmd = new SqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
            var kind = XBase_Class_SQL.DetectCommandKind(sql);

            try
            {
                switch (kind)
                {
                    case XBase_Class_SQL.CommandKind.Select:
                        returnObject = ExecuteSelect(sql, out result);
                        kindDetected = 1;
                        break;

                    case XBase_Class_SQL.CommandKind.Scalar:
                        returnObject = ExecuteSelect(sql, out result);
                        kindDetected = 2;
                        break;

                    case XBase_Class_SQL.CommandKind.NonQuery:
                        result = cmd.ExecuteNonQuery();
                        kindDetected = 3;
                        break;
                }

                if (result > 1) returnObject = null;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
                returnObject = null;
            }

            if (ErrorCode == 0)
            {
                if (returnObject is not null)
                {
                    AppIO.DebugLog($"Processing SQL Command type {kindDetected}");
                    string type = returnObject.GetType().Name;

                    // Only valid right after Execute
                    switch (kindDetected)
                    {
                        // Data Table
                        case 1:
                            if (Program.CurrentApp.CurrentDS.IsWorkArea(SQLBase.CurrentCursorName))
                                Program.CurrentApp.CurrentDS.SelectWorkArea(SQLBase.CurrentCursorName); // Overwrite existing alias
                            else
                                Program.CurrentApp.CurrentDS.SelectWorkArea(0);         // Select an unused work area

                            DataTable dt = (DataTable)returnObject;

                            // Create a cursor for the datatable
                            Program.CurrentApp.ReturnValue.Element.Value = dt.Rows.Count;
                            TableHelper.MakeCursorForDataTable(dt, SQLBase.CurrentCursorName).Wait();
                            break;

                        // Scalar
                        case 2:
                            if (Program.CurrentApp.CurrentDS.IsWorkArea(SQLBase.CurrentCursorName))
                                Program.CurrentApp.CurrentDS.SelectWorkArea(SQLBase.CurrentCursorName); // Overwrite existing alias
                            else
                                Program.CurrentApp.CurrentDS.SelectWorkArea(0);         // Select an unused work area

                            dt = (DataTable)returnObject;

                            // Create a cursor for the datatable
                            Program.CurrentApp.ReturnValue.Element.Value = dt.Rows.Count;
                            TableHelper.MakeCursorForDataTable(dt, SQLBase.CurrentCursorName).Wait();
                            break;

                        // NonQuery
                        case 3:
                            // Nothing to do except return the result
                            Program.CurrentApp.ReturnValue.Element.Value = 0;
                            break;

                        default:
                            ErrorCode = 1400;
                            break;
                    }
                }
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.Execute()");
                result = -1;
            }

            return result;
        }


        private DataTable? ExecuteSelect(string sql, out int result)
        {
            DataTable? dt = new();
            result = ErrorCode = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                    ErrorCode = 6001;
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
            catch (SqlException ex)
            {
                dt = null;
                result = -1;
                ErrorCode = ex.ErrorCode;
                ErrorMsg = ex.Message;
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {result} - {JAXError.JAXErrMsg(ErrorCode, ErrorMsg)} in XBase_ClassSQL_MSSS.ExecuteSelect()");
                result = -1;
            }
            catch (Exception ex)
            {
                dt = null;
                result = -1;
                ErrorCode = 9999;
                ErrorMsg = ex.Message;
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {result} - {JAXError.JAXErrMsg(ErrorCode, ErrorMsg)} in XBase_ClassSQL_MSSS.ExecuteSelect()");
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


        public async Task<int> ExecuteSP(string procName, List<xParameters> parameters)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                {
                    ErrorCode = 6001;
                }
                else
                {
                    SqlCommand cmd = new SqlCommand("Command String", SQLCon);
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (xParameters p in parameters)
                        {
                            if (p.Value is not null)
                            {
                                string paramName = p.Name.StartsWith("@") ? p.Name : "@" + p.Name;

                                switch (p.Value.Element.Type)
                                {
                                    case "N":
                                        cmd.Parameters.Add(paramName, SqlDbType.Decimal).Value = p.Value.AsDecimal();
                                        break;

                                    case "C":
                                        cmd.Parameters.Add(paramName, SqlDbType.VarChar).Value = p.Value.AsString();
                                        break;

                                    case "D":
                                        cmd.Parameters.Add(paramName, SqlDbType.Date).Value = p.Value.AsDate();
                                        break;

                                    case "T":
                                        cmd.Parameters.Add(paramName, SqlDbType.DateTime).Value = p.Value.AsDateTime();
                                        break;

                                    case "L":
                                        cmd.Parameters.Add(paramName, SqlDbType.Bit).Value = p.Value.AsBool() ? 1 : 0;
                                        break;
                                }
                            }
                        }
                    }

                    SQLCon!.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    System.Data.DataTable resultTable = new System.Data.DataTable();

                    // Add RETURN value parameter
                    SqlParameter returnParam = cmd.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                    returnParam.Direction = System.Data.ParameterDirection.ReturnValue;

                    // Open connection
                    if (SQLCon.State != System.Data.ConnectionState.Open)
                        await SQLCon.OpenAsync();

                    // Fill result table + get rows affected
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        rowsAffected = await Task.Run(() => adapter.Fill(resultTable));

                        if (rowsAffected > 0)
                        {
                            // Set up the cursor
                            await TableHelper.MakeCursorForDataTable(resultTable, SQLBase.CurrentCursorName);
                        }
                    }

                    // Get the procedure's RETURN value
                    if (returnParam.Value != null && returnParam.Value != DBNull.Value)
                    {
                        Program.CurrentApp.ReturnValue.Element.Value = returnParam.Value;
                    }
                }
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                ErrorMsg = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.ExecuteSP()");
                result = -1;
            }

            return result;
        }


        public async Task<int> GetSPCode(string procName, string cursorName = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            JAXDataSession ds = Program.CurrentApp.CurrentDS;
            int cwa = ds.CurrentWorkArea();

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string schema = SQLBase.UserProperties["schema"].AsString();
                    schema += string.IsNullOrWhiteSpace(schema) ? "" : ".";

                    // Get all matching stored procedures
                    string lcSQL = "SELECT SCHEMA_NAME(p.schema_id) AS SchemaName, p.name AS ProcedureName, " +
                        "p.type_desc AS Type, p.create_date AS Created, p.modify_date AS LastModified " +
                        $"WHERE p.name LIKE '{procName}' and p.schema_id='{schema}' ORDER BY SchemaName, ProcedureName;";

                    DataTable? dt = ExecuteSelect(lcSQL, out result);

                    if (dt is not null)
                    {
                        // Create a cursor
                        ds.SelectWorkArea(0);

                        JAXDirectDBF.DBFInfo dbfInfo = new()
                        {
                            Fields = TableHelper.MakeFieldsFromString("name c(128), schema c(128), sql m"),
                            Alias = SQLBase.CurrentCursorName,
                            TableType = "C"
                        };

                        await ds.CurrentWA.DBFCreateDBF(dbfInfo, true);

                        DataTable? newRow = null;

                        // Look at each matching row
                        foreach (DataRow row in dt.Rows)
                        {
                            newRow = ds.CurrentWA.DbfInfo.EmptyRow.Copy();

                            // Get the formatted text of the stored procedure
                            using (SqlCommand command = new SqlCommand("sp_helptext", SQLCon))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                // Add parameters
                                command.Parameters.AddWithValue("@objname", schema + dt.Rows[0]["name"]);

                                // Get the formatted stored procedure
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    StringBuilder sb = new StringBuilder();

                                    while (reader.Read())
                                    {
                                        // Each row contains one line of the procedure text
                                        sb.AppendLine(reader["Text"].ToString());
                                    }

                                    // Write the record to the cursor
                                    newRow.Rows[0]["name"] = dt.Rows[0]["name"];
                                    newRow.Rows[0]["schema"] = dt.Rows[0]["schema_name"];
                                    newRow.Rows[0]["sql"] = sb.ToString();
                                    await ds.CurrentWA.DBFAppendRecord(newRow);
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                ErrorMsg = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.GetSPCode()");
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


        /*
         * This is done at the engine level because some engines may set the property differently, while
         * others may just not support a property.
         */
        public async Task<int> SetParameter(string parameter, JAXObjects.Token value)
        {

            int result = ErrorCode = 0;
            string type = value.Element.Type;
            string msg = "";

            try
            {
                switch (parameter.ToLower())
                {
                    case "applicationname":
                        ApplicationName = type.Equals("C") ? value.AsString() : throw new Exception($"11|");
                        break;

                    case "authtype":
                        int authType = type.Equals("N") ? value.AsInt() : throw new Exception($"11|");
                        switch (authType)
                        {
                            case 0:
                                // High-Trust
                                AuthenticationMethod = SqlAuthenticationMethod.NotSpecified;
                                break;

                            case 1:
                                // Windows Authentication
                                AuthenticationMethod = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                                break;

                            case 2:
                                // UserID/PW
                                AuthenticationMethod = SqlAuthenticationMethod.SqlPassword;
                                break;

                            default:
                                // Certificate
                                AuthenticationMethod = SqlAuthenticationMethod.NotSpecified;
                                break;
                        }
                        break;

                    case "port":
                        if (type.Equals("N"))
                        {
                            int Port = value.AsInt() < 1 ? 1433 : value.AsInt();
                            if (JAXLib.Between(Port, 1, 65535) == false) result = ErrorCode = 3003;
                            SQLBase.UserProperties["port"].Element.Value = Port;
                        }
                        else
                            ErrorCode = 11;

                        break;

                    case "database":
                        if (type.Equals("C"))
                            SQLBase.UserProperties["database"].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;

                        break;

                    case "server":
                    case "datasource":
                        if (type.Equals("C"))
                            SQLBase.UserProperties["server"].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "trusted_connection":
                    case "trusted_security":
                        string yes = value.AsString();
                        if (yes.Length < 1)
                            ErrorCode = 41;
                        else
                            SQLBase.UserProperties["integratedsecurity"].Element.Value = yes[..1].Equals("Y", StringComparison.OrdinalIgnoreCase);
                        break;

                    case "pwd":
                    case "password":
                        if (type.Equals("C"))
                            SQLBase.UserProperties["password"].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "timeout":
                        if (type.Equals("N"))
                            SQLBase.UserProperties["connecttimeout"].Element.Value = value.AsInt();
                        else
                            ErrorCode = 11;

                        if (SQLBase.UserProperties["connecttimeout"].AsInt() < 0) result = 3003;
                        break;

                    case "uid":
                    case "usrid":
                    case "user id":
                    case "userid":
                        if (type.Equals("C"))
                            SQLBase.UserProperties["userid"].Element.Value = value.AsString();
                        else
                            ErrorCode = 11;
                        break;

                    case "trust":
                        if (type.Equals("L"))
                            SQLBase.UserProperties["trust"].Element.Value = value.AsBool();
                        else
                            ErrorCode = 11;
                        break;

                    case "encryption":
                        if (type.Equals("L"))
                            SQLBase.UserProperties["encryption"].Element.Value = value.AsBool();
                        else
                            ErrorCode = 11;
                        break;

                    case "workstation":
                        if (type.Equals("C"))
                            WorkStation = value.AsString();
                        else
                            ErrorCode = 11;
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






        /*
         * Return index information
         */
        public async Task<int> GetIndex(string tableName, string indexName = "", string cursorName = "")
        {
            int result = 0;
            ErrorCode = 0;

            JAXDataSession DS = Program.CurrentApp.CurrentDS;
            int cwaSelect = DS.CurrentWorkArea();

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            string lcSQL =
                "SELECT t.name AS TableName, i.name AS IndexName, i.type_desc AS IndexType, c.name AS ColumnName, " +
                "ic.key_ordinal AS ColumnPosition, ic.is_included_column AS IsIncluded,ic.is_descending_key as Descending, " +
                "i.is_unique AS IsUnique, i.is_primary_key AS IsPrimaryKey, " +
                "FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id " +
                "INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id " +
                "INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id ";


            if (string.IsNullOrWhiteSpace(indexName))
                lcSQL += $"WHERE t.name='{tableName} ORDER BY t.name, i.name, ic.key_ordinal;";
            else
                lcSQL += $"WHERE t.name='{tableName}' and i.name='{indexName}' ORDER BY t.name, i.name, ic.key_ordinal;";

            // data columns are TableName, IndexName, IndexType, ColumnName, ColumnPosition, IsIncluded, Descending
            DataTable? dt = ExecuteSelect(lcSQL, out result);

            if (dt is not null)
            {
                // Create the cursor in a new work area
                DS.SelectWorkArea(0);
                int thiswa = DS.CurrentWorkArea();

                JAXDirectDBF cwa = DS.CurrentWA;
                JAXDirectDBF.DBFInfo dbfInfo = new();

                if (await TableHelper.CreateCursor("table C(128), index C(128), type(1), columns M", SQLBase.CurrentCursorName) == 0)
                {
                    string currentIndex = "";
                    string c = "";
                    string idx = "";
                    DataTable? newRow = null;
                    JAXObjects.Token v = new();

                    // Found at least one match so create a row for each one
                    // Now save the data to the cursor
                    newRow = cwa.DbfInfo.EmptyRow.Copy();

                    foreach (DataRow dr in dt.Rows)
                    {
                        // Is it a new index or just the first one?
                        if (currentIndex.Equals(idx) == false)
                        {
                            if (string.IsNullOrWhiteSpace(c) == false)
                            {
                                // New index, save the current record and clean up the columns var
                                cwa.DbfInfo.CurrentRow.Rows[0]["columns"] = c.Trim(',');
                                await cwa.DBFWriteRecord(newRow.Rows[0], true);
                                newRow = cwa.DbfInfo.EmptyRow.Copy();

                                // Clear the column variable
                                c = "";
                            }

                            // Process this record by starting with index type
                            v.Element.Value = dr["is_primary"];
                            string idxType = "";

                            if (v.Element.Type.Equals("N") && v.AsInt() > 0)
                                idxType = "P";      // Primary index
                            else
                            {
                                v.Element.Value = dr["is_unique"];
                                if (v.Element.Type.Equals("N") && v.AsInt() > 0)
                                    idxType = "U";  // Unique index
                                else
                                    idxType = "I";  // Regular index
                            }

                            // Set up the new row
                            cwa.DbfInfo.CurrentRow.Rows[0]["table"] = dr["table"].ToString();
                            cwa.DbfInfo.CurrentRow.Rows[0]["index"] = dr["index"].ToString() ?? "";
                            cwa.DbfInfo.CurrentRow.Rows[0]["type"] = idxType;
                        }


                        // get the descending key value
                        v.Element.Value = dr["is_descending_key"];

                        // Add this column to the memo field var
                        c += dr["column_name"].ToString() + (v.AsInt() == 1 ? " (DESC)" : "") + ",";
                    }

                    // Take care of the last index
                    if (string.IsNullOrWhiteSpace(idx) == false && string.IsNullOrWhiteSpace(c) == false)
                    {
                        cwa.DbfInfo.CurrentRow.Rows[0]["columns"] = c.Trim(',');
                        await cwa.DBFWriteRecord(newRow.Rows[0], true);
                    }
                }
                else
                {
                    // Error
                    DS.SelectWorkArea(cwaSelect);
                    ErrorCode=1554;
                    result = -1;
                    SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                    AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, ErrorMsg)} in XBase_ClassSQL_MSSS.SetParameter()");
                }
            }

            return result;
        }


        public int ListDatabases(out List<string> dbList)
        {
            string msg = "";
            dbList = [];
            object? returnObject = ExecuteSelect("SELECT name FROM sys.databases ORDER BY name", out int result);

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
                ErrorCode = 9999;

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.ListDatabases()");
                result = -1;
            }

            return result;
        }


        public int ListIndexes(string tableName, out List<string> idxList)
        {
            int result = 0;
            idxList = [];

            string lcSQL =
                "SELECT t.name AS TableName, i.name AS IndexName, i.type_desc AS IndexType, c.name AS ColumnName, " +
                "ic.key_ordinal AS ColumnPosition, ic.is_included_column AS IsIncluded,ic.is_descending_key as Descending " +
                "FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id " +
                "INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id " +
                "INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id " +
                $"WHERE t.name='{tableName} ORDER BY t.name, i.name, ic.key_ordinal;";

            DataTable? dt = ExecuteSelect(lcSQL, out result);

            if (dt is not null)
            {
                string lastIDX = "";
                foreach (DataRow row in dt.Rows)
                {
                    string thisIDX = row["IndexName"].ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(thisIDX) == false)
                    {
                        if (thisIDX.Equals(lastIDX, StringComparison.Ordinal) == false)
                            idxList.Add(thisIDX);

                        thisIDX = lastIDX;
                    }
                }

                result = idxList.Count;
            }
            else
                result = -1;

            return result;
        }


        public int ListTables(out List<string> tblList)
        {
            string msg = "";
            tblList = [];

            object? returnObject = ExecuteSelect("SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_SCHEMA, TABLE_NAME;", out int result);

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
                ErrorCode = 9999;

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.ListTables()");
                result = -1;
            }
            return result;
        }

        public int GetState()
        {
            int result;
            string msg = "";

            if (SQLCon is null)
                result = ErrorCode = 6001;
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

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.GetState()");
                result = -1;
            }

            return result;
        }

        public int GetKind() { return kindDetected; }

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

        /*
         * Return the table structure using JAXBase field codes
         */
        public async Task<int> GetTableStructure(string tableName, string cursorName = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string lcSQL = $@"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}';";
                    object? returnObject = ExecuteSelect(lcSQL, out result);

                    if (result < 0)
                    {
                        SQLBase._AddError(1526, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                        AppIO.DebugLog($"Error {result} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetConnection()");
                        result = -1;
                    }
                    else
                    {
                        if (returnObject is not null)
                        {
                            DataTable dt = (DataTable)returnObject;

                            lcSQL = "fieldname C(128), fieldtype C(1), fieldlen N(3), fielddec N(2), nullok L, " +
                                "nocptrans L, binary L, emptyval c(254), defaultval c(254), " +
                                "f10,f11,f12,tablename c(128), f14, caption c(254), comment c(254), " +
                                "autonext n(10), autostep n(10)";

                            if (await TableHelper.CreateCursor(lcSQL, SQLBase.CurrentCursorName) == 0)
                            {
                                // Found at least one match so create a row for each one
                                foreach (DataRow row in dt.Rows)
                                {
                                    string db = row[0]?.ToString() ?? "";
                                    DataTable newRow = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.EmptyRow.Copy();

                                    if (string.IsNullOrWhiteSpace(db) == false)
                                    {
                                        newRow.Rows[0]["fieldname"] = row["fieldname"];
                                        newRow.Rows[0]["fieldtype"] = row["fieldtype"];
                                        newRow.Rows[0]["FieldLen"] = row["FieldLen"];
                                        newRow.Rows[0]["FieldDec"] = row["FieldDec"];
                                        newRow.Rows[0]["NullOK"] = row["NullOK"];
                                        newRow.Rows[0]["NoCPTrans"] = row["NoCPTrans"];
                                        newRow.Rows[0]["BinaryData"] = row["BinaryData"];
                                        newRow.Rows[0]["EmptyValue"] = row["EmptyValue"];
                                        newRow.Rows[0]["DefaultValue"] = row["DefaultValue"];
                                        //tk._avalue[i * 18 + 9].Value = fld.;
                                        //tk._avalue[i * 18 + 10].Value = fld.;
                                        //tk._avalue[i * 18 + 11].Value = fld.;
                                        newRow.Rows[0]["TableName"] = row["TableName"];
                                        //tk._avalue[i * 18 + 13].Value = fld.;
                                        newRow.Rows[0]["Caption"] = row["Caption"];
                                        newRow.Rows[0]["Comment"] = row["Comment"];
                                        newRow.Rows[0]["AutoIncNext"] = row["AutoIncNext"];
                                        newRow.Rows[0]["AutoIncStep"] = row["AutoIncStep"];
                                    }
                                }
                            }
                        }
                    }
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.GetTableStructure()");
                result = -1;
            }

            return result;
        }


        /*
         * Get basic database info in an array, returns the number of elements returned
         * 
         * Element  Description
         *      1   Database name
         *      2   Collation
         *      3   Disk size in kilobytes
         *      4   Number of tables
         *      5   Number of views
         *      
         */
        public async Task<int> GetDatabaseInfo(string dbName, string cursorName = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;
            int thiswa = 0;

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            JAXDataSession ds = Program.CurrentApp.CurrentDS;
            int cwa = ds.CurrentWorkArea();

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    // Get all matching databases
                    string lcSQL = $@"SELECT * FROM sys.databases" + (dbName.Length > 0 ? " WHERE name like '{dbName}';" : ";");
                    DataTable? qry1 = ExecuteSelect(lcSQL, out result);

                    // Create the cursor in a new work area
                    ds.SelectWorkArea(0);
                    thiswa = ds.CurrentWorkArea();

                    if (await TableHelper.CreateCursor("name C(128), tables N(10), views(10), size(10)", SQLBase.CurrentCursorName) == 0)
                    {
                        if (qry1 is not null && qry1.Rows.Count > 0)
                        {
                            // Found at least one match so create a row for each one
                            foreach (DataRow row in qry1.Rows)
                            {
                                string db = row[0]?.ToString() ?? "";

                                if (string.IsNullOrWhiteSpace(db) == false)
                                {
                                    // Loop through the list and get info on database
                                    lcSQL = $@"USE [{db}]; EXEC sp_spaceused;";
                                    DataTable? qry2 = ExecuteSelect(lcSQL, out result);

                                    lcSQL = $@"USE [{db}]; SELECT DB_NAME() AS database_name, COUNT(CASE WHEN type = 'U' AND is_ms_shipped = 0 THEN 1 END) AS table_count, COUNT(CASE WHEN type = 'V' THEN 1 END) AS view_count FROM sys.objects;";
                                    DataTable? qry3 = ExecuteSelect(lcSQL, out result);

                                    if (qry1 is not null && qry2 is not null && qry3 is not null)
                                    {
                                        DataTable newRow = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.EmptyRow.Copy();
                                        newRow.Rows[0]["name"] = qry1.Rows[0]["name"].ToString() ?? "unknown";                                  // Name
                                        newRow.Rows[0]["size"] = qry2.Rows[0]["reserved"] is null ? 0 : (long)qry2.Rows[0]["reserved"];         // Disk Space used
                                        newRow.Rows[0]["tables"] = qry3.Rows[0]["table_count"] is null ? 0 : (int)qry2.Rows[0]["table_count"];  // Table Count
                                        newRow.Rows[0]["views"] = qry3.Rows[0]["view_count"] is null ? 0 : (int)qry2.Rows[0]["view_count"];     // View Count
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Close the new workarea and select the old one
                if (thiswa > 0)
                {
                    ds.SelectWorkArea(thiswa);
                    await ds.CloseDBF(SQLBase.CurrentCursorName);
                }

                ds.SelectWorkArea(cwa);

                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SQLDatabaseInfo()");
                result = -1;
            }

            return result;

        }

        /*
         *  Get the code to create the view
         */
        public async Task<int> GetView(string viewName, string cursorName = "")
        {
            string msg = "";
            int result = ErrorCode = 0;
            string lcSQL = "";

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            // Get current work area
            JAXDataSession ds = Program.CurrentApp.CurrentDS;
            int cwa = ds.CurrentWorkArea();

            JAXDirectDBF? dbf = null;

            try
            {

                // Go to an empty workarea
                ds.SelectWorkArea(0);
                dbf = Program.CurrentApp.CurrentDS.CurrentWA;

                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    // if a while card, look for a list
                    lcSQL = "SELECT SCHEMA_NAME(schema_id) AS schema_name, name AS procedure_name, " +
                        "create_date, modify_date, is_auto_executed FROM sys.procedures " +
                        "WHERE is_ms_shipped = 0 ";

                    if (string.IsNullOrWhiteSpace(SQLBase.UserProperties["schema"].AsString()) == false)
                    {
                        // Select only the current schema
                        lcSQL += $"and schema_name='{SQLBase.UserProperties["schema"].AsString()}' ";
                    }

                    if (string.IsNullOrWhiteSpace(viewName) == false)
                    {
                        // search for this name
                        lcSQL += $" and name like {viewName.Replace("*", "%").Replace("?", "_")} ";
                    }

                    lcSQL += "ORDER BY schema_name, procedure_name;";
                    object? returnObject = ExecuteSelect(lcSQL, out result);

                    DataTable? vdt = (DataTable?)returnObject;

                    // Create the cursor
                    List<JAXTables.FieldInfo> flds = TableHelper.MakeFieldsFromString("name C(128), schema C(128), sql M");
                    JAXDirectDBF.DBFInfo dbfInfo = new()
                    {
                        Alias = SQLBase.CurrentCursorName,
                        TableType = "C",
                        Fields = flds
                    };

                    if (await dbf!.DBFCreateDBF(dbfInfo, true))
                    {

                        if (vdt is not null)
                        {
                            foreach (DataRow row in vdt.Rows)
                            {
                                string schema = row["schema_name"].ToString() ?? "";
                                string name = (string.IsNullOrWhiteSpace(schema) ? "" : schema + ".") + row["name"].ToString() ?? "";

                                lcSQL = $@"SELECT OBJECT_DEFINITION(OBJECT_ID('{name}')) AS CreateViewStatement;);";
                                returnObject = ExecuteSelect(lcSQL, out result);


                                if (result < 1)
                                {
                                    DataTable? dt = (DataTable?)returnObject;

                                    if (dt is not null)
                                    {
                                        DataTable newRow = dbf.DbfInfo.EmptyRow.Copy();
                                        DataRow r = dt.Rows[0];

                                        newRow.Rows[0]["name"] = name;
                                        newRow.Rows[0]["schema"] = schema;
                                        newRow.Rows[0]["sql"] = r["CreateViewStatement"];

                                        if (await dbf.DBFAppendRecord(newRow) == false)
                                        {
                                            // An error was encountered
                                            ErrorCode = 9999;
                                            ErrorMsg = "OOPS";
                                            break;
                                        }
                                    }
                                }
                                else
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Failed to create cursor
                        ErrorCode = 9999;
                        msg = "";
                    }
                }
            }
            catch (SqlException ex)
            {
                ErrorCode = ex.ErrorCode;
                msg = ex.Message;
                AppIO.DebugLog($"SQL Error {ErrorCode} in XBase_ClasSSQL_MSSS.GetView() - {msg} - lcSQL={lcSQL}");
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
                AppIO.DebugLog($"General Error {ErrorCode} in XBase_ClasSSQL_MSSS.GetView() - {msg} - lcSQL={lcSQL}");
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                result = -1;
            }

            return result;
        }


        public async Task<int> AlterField(string parm1, string parm2, string parm3, int parm4, int parm5)
        {
            return 0;
        }

        public async Task<int> AlterProperty(string table, string field, string property, JAXObjects.Token value)
        {
            int result = 0;

            string lcSQL = "";
            string schema = SQLBase.UserProperties["schema"].AsString();
            schema = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;

            if (await IsConnected() == false)
            {
                ErrorCode= 6001;
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
            }
            else
            {
                // If the schema is part of original field name
                if (table.Contains('.'))
                {
                    string[] s = table.Split('.');
                    schema = s[0];
                    table = string.IsNullOrWhiteSpace(s[1]) ? s[1] : schema;
                }

                schema += ".";

                // Get the specified field information
                lcSQL = "SELECT " +
                "SCHEMA_NAME(t.schema_id) AS SchemaName, " +
                "OBJECT_NAME(c.object_id) AS TableName, " +
                "c.name AS ColumnName, " +
                "t.name AS DataType, " +
                "c.max_length AS MaxLengthBytes, " +
                "CASE " +
                "WHEN t.name IN('varchar', 'char', 'varbinary') THEN c.max_length " +
                "WHEN t.name IN('nvarchar', 'nchar') THEN c.max_length / 2 " +
                "ELSE NULL " +
                "END AS MaxLengthChars, " +
                "c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed, " +
                "OBJECT_DEFINITION(c.default_object_id) AS DefaultValue, " +
                "ep.value AS ColumnDescription " +
                "FROM sys.columns c " +
                "INNER JOIN sys.tables t ON c.object_id = t.object_id " +
                "INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id " +
                "LEFT JOIN sys.extended_properties ep " +
                "ON ep.major_id = c.object_id " +
                "AND ep.minor_id = c.column_id " +
                "AND ep.name = 'MS_Description' " +
                $"WHERE c.name = '{field}';" +
                $"AND t.name = '{table}'" +
                $"AND t.schema_id='{schema}'";

                DataTable? fieldInfo = ExecuteSelect(lcSQL, out result);

                if (result == 1)
                {
                    DataRow row = fieldInfo!.Rows[0];

                    string info = "";
                    int iVal = 0;

                    switch (property.ToLower())
                    {
                        case "fieldname":       // Alter the name of a field
                            lcSQL = $"EXEC sp_rename '{schema}.{table}.{field}', '{value.AsString()}', 'COLUMN';";
                            break;

                        case "null":            // Null ok
                                                // TODO - Get default value if there is one
                                                // Create one if not
                                                // Are we adding or removing null?
                                                // If removing, update all nulls
                                                // Update the property
                            break;

                        case "comment":         // Add a comment to a field
                            lcSQL = $"EXEC sp_updateextendedproperty @name = N'MS_Description', " +
                                $"@value = N'{value.AsString()}', @level0type = N'SCHEMA', " +
                                $"@level0name = N'{schema}', @level1type = N'TABLE', " +
                                $"@level1name = N'{table}', @level2type = N'COLUMN', @level2name = N'{field}';";
                            break;


                        case "fieldwidth":      // Change the width of a field
                            info = row["DataType"]?.ToString() ?? "";   // Get the data type
                            int iOrig = (int)row["MaxLengthBytes"];

                            // Is there a numeric value?
                            if (value.Element.Type.Equals("N"))
                            {
                                iVal = value.AsInt(); // Get length

                                if (JAXLib.InListC(info, "varchar", "char", "nvarchar", "nchar"))
                                {
                                    if (JAXLib.Between(iVal, 1, 254) == false)
                                        ErrorCode= 9999;
                                }

                                if (string.IsNullOrWhiteSpace(info) == false)
                                {
                                    // Found a data type (info)
                                    lcSQL = $"ALTER TABLE {schema}.{table} ALTER COLUMN {field} {info}({value.AsInt()});";
                                }
                                else
                                {
                                    // Failed to find the field
                                    ErrorCode = 9999;
                                    ErrorMsg = field;
                                }
                            }
                            else
                                ErrorCode=11;

                            break;

                        case "fielddecimal":    // Change the width of the decimals
                            info = row["DataType"]?.ToString() ?? "";   // Get the data type
                            iVal = (int)row["precision"];               // Get the current length
                            int dec = value.AsInt();                   // Get the decimal width

                            if (dec < iVal && dec >= 0)
                                lcSQL = $"ALTER TABLE schema.table_name ALTER COLUMN column_name {info}({iVal}, {dec})";
                            else
                                ErrorCode = 41;
                            break;

                        case "fieldtype":
                            // TODO - lots of checks to be run before peforming or just let it fail?

                            // Add a new column with the desired type
                            //lcSQL = $"ALTER TABLE {schema}.{table} ADD {field}_New {value.AsString()}(18, 4) NULL;";

                            // 2.Copy / convert data
                            //lcSQL = $"UPDATE {schema}.{table} {field}_New = TRY_CONVERT(DECIMAL(18, 4), {field});";

                            // 3.Drop old column and rename new one(or use sp_rename)
                            //lcSQL = $"ALTER TABLE {schema}.{field} DROP COLUMN {field};";

                            //lcSQL = $"EXEC sp_rename '{schema}.{table}.{field}_New', '{field}', 'COLUMN';";

                            break;

                        case "nextval":         // Alter next inc value
                                                //TODO - have to support auto inc in the create code using sequences
                            lcSQL = $"ALTER SEQUENCE {table}-{field}-Seq RESTART WITH {value.AsInt()};";
                            break;

                        case "nextstep":        // Alter increment step
                                                //TODO - have to support auto inc in the create code using sequences
                            lcSQL = $"ALTER SEQUENCE {table}-{field}-Seq INCREMENT BY {value.AsInt()};";
                            break;

                        default:                // Error
                            ErrorCode= 9999;
                            ErrorMsg = property.ToUpper();
                            break;
                    }

                    if (ErrorCode == 0)
                    {
                        ExecuteSelect(lcSQL, out result);

                        if (result < 0)
                            SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                    }
                    else
                        result = -1;
                }
                else
                {
                    // Could not find the field
                    ErrorCode = 9999;
                    ErrorMsg = "";
                    SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                    result = -1;
                }
            }

            return result;
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