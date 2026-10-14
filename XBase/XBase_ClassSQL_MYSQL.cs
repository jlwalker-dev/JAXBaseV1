/* 
 * MSSQL Engine support
 * 
 * 2026-10-10 - Initial version - Github is having problems..
 * 
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Language;
using JAXBase.Utilities;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Windows.Documents;
using System.Windows.Interop;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_MYSQL : SQLClass
    {
        XBase_Class_SQL SQLBase;

        private int ErrorCode = 0;
        private string ErrorMsg = string.Empty;
        int kindDetected = 0;

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
                    if (string.IsNullOrWhiteSpace(SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString()) == false)
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

                    AppIO.DebugLog($"XBase_ClassSQL_MYSQL.Connect() - ConnectionString: {ConnectionString}");

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
                AppIO.DebugLog($"XBase_ClassSQL_MYSQL.Connect() - result: {result} -  SQLCon: {SQLCon}");

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
        public async Task<int> CreateIndex(string indexName, string tableName, string indexExpression, string filter = "", string attribs = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                    result = ErrorCode = 6001;
                else
                {
                    attribs = attribs.ToUpper();

                    string lcSQL = "";

                    // Primary key
                    if (attribs.Contains("P"))
                    {
                        // Drop the primary if it exsits and create it
                        lcSQL += "DECLARE @PKName NVARCHAR(128);";
                        lcSQL += $"SELECT @PKName = name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('{tableName}') AND type = 'PK';";
                        lcSQL += $"IF @PKName IS NOT NULL EXEC('ALTER TABLE {tableName} DROP CONSTRAINT ' + @PKName);";
                        lcSQL += "alter table " + tableName + " add constraint " + indexName + $" primary key ({indexExpression})";
                    }
                    else
                    {
                        // Remove index name if it exists and create it
                        lcSQL += $"DROP INDEX IF EXISTS {indexName} ON {tableName};";
                        lcSQL += "create " + (attribs.Contains("C") ? "unique " : "") + (attribs.Contains("U") ? "unique " : "");
                        lcSQL += "index " + indexName + "on";
                        lcSQL += tableName + $"({indexExpression})";
                        lcSQL += string.IsNullOrWhiteSpace(filter) ? $"where {filter}" : "";
                        lcSQL += ";";
                    }

                    result = await Execute(lcSQL);
                }
            }
            catch (MySqlException ex)
            {
                ErrorCode = (int)ex.ErrorCode;
                msg = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.CreateIndex()");
                result = -1;
            }

            return result;
        }

        public async Task<int> CreateSP(string procName, string procCode)
        {
            int result = 0;

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
        public async Task<int> CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string schema = SQLBase.UserProperties[JAXLanguageLists.GetWord("schema", "REVPEMS")].AsString();

                    string sql = $"CREATE TABLE {tableName} (";
                    for (int i = 0; i < Fields.Count; i++)
                    {
                        JAXTables.FieldInfo field = Fields[i];
                        string fieldType = string.Empty;
                        string defaultValue = string.Empty;

                        switch (field.FieldType.ToUpper())
                        {
                            case "B":
                            case "F":
                                fieldType = "DOUBLE";
                                defaultValue = "0";
                                break;

                            case "C":
                                if (field.NoCPTrans || field.BinaryData)
                                    fieldType = $"VARBINARY({field.FieldLen})";
                                else
                                    fieldType = $"CHAR({field.FieldLen})";

                                defaultValue = "''";
                                break;

                            case "D":
                                fieldType = "DATE";
                                break;

                            case "G":
                            case "W":
                                fieldType = "BLOB";
                                break;

                            case "I":
                                fieldType = "INT";
                                defaultValue = "0";
                                break;

                            case "L":
                                fieldType = "TINYINT(1)";
                                defaultValue = "0";
                                break;

                            case "M":
                                fieldType = "BLOB";
                                break;

                            case "N":
                                fieldType = $"DECIMAL({field.FieldLen},{field.FieldDec})";
                                defaultValue = "0";
                                break;

                            case "Q":
                                fieldType = $"VARBINARY({field.FieldLen})";
                                defaultValue = "''";
                                break;

                            case "T":
                                fieldType = "DATETIME";
                                break;

                            case "V":
                                fieldType = $"VARBINARY({field.FieldLen})";

                                defaultValue = "''";
                                break;

                            case "Y":
                                fieldType = "DECIMAL(19,4)";
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
                    using var cmd = new MySqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.CreateTable()");
                result = -1;
            }

            return result;
        }

        public async Task<int> CreateView(string viewName, string viewCode)
        {
            int result = ErrorCode = 0;

            return result;
        }

        public async Task<int> DeleteDB(string name)
        {
            int result = 0;
            string msg = "";

            object? returnObject = ExecuteSelect($"DROP DATABASE {name};", out result);

            if (result < 0)
            {
                msg = "Error executing SQL to list databases";
                ErrorCode = 9999;
            }

            if (result > 0)
                ErrorCode = result;

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.ListDatabases()");
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


        public async Task<int> DeleteTable(string Name)
        {
            int result = ErrorCode = 0;
            string msg = "";
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            try
            {
                if (await IsConnected() == false)
                {
                    result = -1;
                    ErrorCode = 6001;
                }
                else
                {
                    string lcSQL = $@"DROP TABLE '{Name}';";
                    ExecuteSelect(lcSQL, out result);

                    if (result < 0)
                    {
                        msg = "Error executing SQL to drop table";
                        ErrorCode = 9999;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorCode = 9999;
                result = -1;
                msg = ex.Message;
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.DropTable()");
                result = -1;
            }

            return result;

        }
        public async Task<int> DeleteView(string Name)
        {
            string schema = SQLBase.UserProperties["schema"].AsString();
            string[] test = Name.Split('.');

            if (test.Length == 1)
                Name = string.IsNullOrWhiteSpace(schema) ? Name : schema + "." + Name;

            return 1999;
        }

        public async Task<int> DeleteIndex(string indexName, string tableName)
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    string lcSQL;

                    // Is it a primary key?
                    if (indexName.ToUpper() == "PRIMARY")
                    {
                        lcSQL = $"DROP INDEX IF EXISTS 'PRIMARY' ON {tableName};";
                    }
                    else
                    {
                        // Delete the index
                        lcSQL = $"DROP INDEX IF EXISTS {indexName} on {tableName};";
                    }

                    ExecuteSelect(lcSQL, out result);

                    if (result < 0)
                    {
                        msg = "Error executing SQL to drop index";
                        ErrorCode = 9999;
                        result = -1;
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.DeleteIndex()");
                result = -1;
            }

            return result;
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
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MYSQL.Disconnect()");
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
            DataTable dt;
            int nextValue = 0;
            int nextStep = 0;

            SQLBase.CurrentCursorName = string.IsNullOrEmpty(cursorName) ? SQLBase.CurrentCursorName : cursorName;

            try
            {
                if (await IsConnected() == false)
                    ErrorCode = 6001;
                else
                {
                    // Does this table have an auto increment field?
                    // If step is greater than 0, then we have an auto increment field and we need to get the next value for it.
                    string lcSQL = $@"SELECT @@auto_increment_increment AS step";
                    object? scalar = ExecuteSelect(lcSQL, out result);

                    if (result < 0)
                    {
                        SQLBase._AddError(1526, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                        AppIO.DebugLog($"Error {result} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetConnection()");
                        result = -1;
                    }
                    else
                    {
                        if (scalar is not null)
                        {
                            dt = (DataTable)scalar;
                            nextStep = dt.Rows[0]["step"] is not null ? Convert.ToInt32(dt.Rows[0]["step"]) : 1;

                            if (nextStep > 0)
                            {
                                // It has a step greater than 0, so get the next value for the auto increment field
                                lcSQL = $@"SELECT AUTO_INCREMENT AS nextValue FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString()}' AND TABLE_NAME = '{tableName}';";
                                scalar = ExecuteSelect(lcSQL, out result);

                                if (result < 0)
                                {
                                    SQLBase._AddError(1526, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                                    AppIO.DebugLog($"Error {result} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SetConnection()");
                                    result = -1;
                                }
                                else
                                {
                                    if (scalar is not null)
                                    {
                                        dt = (DataTable)scalar;
                                        nextValue = dt.Rows[0]["nextValue"] is not null ? Convert.ToInt32(dt.Rows[0]["nextValue"]) : 1;
                                    }
                                }
                            }

                        }

                        if (result >= 0)
                        {
                            lcSQL = $@"SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '{SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString()}' AND TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION;";
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
                                    dt = (DataTable)returnObject;

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
                                                string dataType = "";

                                                newRow.Rows[0]["fieldname"] = row["COLUMN_NAME"].ToString() ?? "";

                                                if ((row["COLUMN_TYPE"].ToString() ?? "").Equals("DECIMAL(19,4)", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    newRow.Rows[0]["fieldtype"] = dataType = "Y"; // Currency
                                                }
                                                else
                                                {
                                                    row["fieldtype"] = dataType = row["DATA_TYPE"]?.ToString() ?? "" switch
                                                    {
                                                        "int" => "I",
                                                        "bigint" => "O",
                                                        "smallint" => "I",
                                                        "tinyint" => "L",
                                                        "boolean" => "L",
                                                        "decimal" => "N",
                                                        "numeric" => "N",
                                                        "float" => "F",
                                                        "double" => "B",
                                                        "real" => "B",
                                                        "char" => "C",
                                                        "binary" => "Q",
                                                        "varchar" => row["CHARACTER_SET_NAME"] is not null ? "Q" : "V",
                                                        "text" => row["CHARACTER_SET_NAME"] is not null ? "M" : "W",
                                                        "tinyblob" => row["CHARACTER_SET_NAME"] is not null ? "M" : "W",
                                                        "blob" => row["CHARACTER_SET_NAME"] is not null ? "M" : "W",
                                                        "mediumblob" => row["CHARACTER_SET_NAME"] is not null ? "M" : "W",
                                                        "longblob" => row["CHARACTER_SET_NAME"] is not null ? "M" : "W",
                                                        _ => throw new Exception($"Unknown data type: {row["fieldtype"]}")
                                                    };
                                                }

                                                if (JAXLib.InListC(dataType, "C", "Q", "V"))
                                                {
                                                    newRow.Rows[0]["FieldLen"] = (int)(row["CHARACTER_MAXIMUM_LENGTH"] ?? 0);
                                                    newRow.Rows[0]["EmptyValue"] = "";
                                                }
                                                else if (JAXLib.InListC(dataType, "M", "W"))
                                                {
                                                    newRow.Rows[0]["FieldLen"] = 4;
                                                    newRow.Rows[0]["EmptyValue"] = "";
                                                }
                                                else if (dataType == "D")
                                                {
                                                    newRow.Rows[0]["FieldLen"] = 4;
                                                    newRow.Rows[0]["EmptyValue"] = DateOnly.MinValue;
                                                }
                                                else if (dataType == "T")
                                                {
                                                    newRow.Rows[0]["FieldLen"] = 4;
                                                    newRow.Rows[0]["EmptyValue"] = DateTime.MinValue;
                                                }
                                                else
                                                {
                                                    newRow.Rows[0]["EmptyValue"] = "0";
                                                    newRow.Rows[0]["FieldLen"] = (int)(row["NUMERIC_PRECISION"] ?? 0);
                                                }

                                                newRow.Rows[0]["FieldDec"] = (int)(row["NUMERIC_SCALE"] ?? 0);
                                                newRow.Rows[0]["NullOK"] = (row["IS_NULLABLE"].ToString() ?? "").Equals("yes", StringComparison.OrdinalIgnoreCase);

                                                //newRow.Rows[0]["NoCPTrans"] = row["NoCPTrans"];

                                                if (row["CHARACTER_SET_NAME"] is not null || JAXLib.InListC(dataType, "binary", "varbinary", "blob", "tinyblob", "mediumblob", "longblob"))
                                                    newRow.Rows[0]["BinaryData"] = true;
                                                else
                                                    newRow.Rows[0]["BinaryData"] = false;

                                                newRow.Rows[0]["DefaultValue"] = row["COLUMN_DEFAULT"];
                                                //tk._avalue[i * 18 + 9].Value = fld.;
                                                //tk._avalue[i * 18 + 10].Value = fld.;
                                                //tk._avalue[i * 18 + 11].Value = fld.;
                                                newRow.Rows[0]["TableName"] = row["TABLE_NAME"];
                                                //tk._avalue[i * 18 + 13].Value = fld.;
                                                newRow.Rows[0]["Caption"] = "";
                                                newRow.Rows[0]["Comment"] = row["COLUMN_COMMENT"];

                                                if (row["extra"] is not null && row["extra"].ToString()?.Contains("auto_increment") == true)
                                                {
                                                    newRow.Rows[0]["AutoIncNext"] = nextValue;
                                                    newRow.Rows[0]["AutoIncStep"] = nextStep;
                                                }
                                            }
                                        }
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
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public async Task<int> Execute(string sql, string cursorName = "")
        {
            int result = ErrorCode = 0;
            string msg = string.Empty;
            object? returnObject = null;
            DataTable dt = new();
            sql = sql.Trim();

            using var cmd = new MySqlCommand(sql, SQLCon) { CommandType = CommandType.Text };
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
                result = -1;
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

                            dt = (DataTable)returnObject;

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
                            result = -1;
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
            DataTable dt = new();
            result = 0;

            try
            {
                if (SQLCon is null || SQLCon.State != ConnectionState.Open)
                {
                    result = -1;
                    ErrorCode = 6001;
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
                result = -1;
                ErrorCode = 9999;
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

        public async Task<int> CreateDatabase(string name)
        {
            int result = 0;
            string msg = "";

            object? returnObject = ExecuteSelect($"CREATE DATABASE {name};", out result);

            if (result < 0)
            {
                msg = "Error executing SQL to list databases";
                ErrorCode = 9999;
            }

            if (result > 0)
                ErrorCode = result;


            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.ListDatabases()");
                result = -1;
            }

            return result;
        }

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


        public int ListIndexes(string tableName, out List<string> idxList)
        {
            int result = 0;
            idxList = [];

            string lcSQL = $"SHOW KEYS FROM {tableName};";

            DataTable? dt = ExecuteSelect(lcSQL, out result);

            if (dt is not null)
            {
                string lastIDX = "";
                foreach (DataRow row in dt.Rows)
                {
                    string thisIDX = row["Key_name"].ToString() ?? "";

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

        public async Task<int> GetDatabaseInfo(string dbName, string cursorName)
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
                    string lcSQL = $@"SELECT s.SCHEMA_NAME AS DatabaseName, s.DEFAULT_COLLATION_NAME AS Collation, " +
                        "ROUND(SUM(t.DATA_LENGTH + t.INDEX_LENGTH) / 1024, 0) AS DiskSizeKB, " +
                        "SUM(CASE WHEN t.TABLE_TYPE = 'BASE TABLE' THEN 1 ELSE 0 END) AS NumberOfTables, " +
                        "SUM(CASE WHEN t.TABLE_TYPE = 'VIEW' THEN 1 ELSE 0 END) AS NumberOfViews " +
                        "FROM INFORMATION_SCHEMA.SCHEMATA s" +
                        "LEFT JOIN INFORMATION_SCHEMA.TABLES t " +
                        "ON s.SCHEMA_NAME = t.TABLE_SCHEMA " +
                        $@"{(dbName.Length > 0 ? " WHERE s.SCHEMA_NAME =  '{dbName}';" : "")}" +
                        "GROUP BY s.SCHEMA_NAME, s.DEFAULT_COLLATION_NAME;";

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

        public async Task<int> GetView(string viewName, string cursorName)
        {
            int result = 0;
            return result;
        }



        // DO WE NEED THIS?
        public async Task<int> AlterField(string table, string field, string property, int parm4, int parm5)
        {
            return 0;
        }

        public async Task<int> AlterProperty(string table, string field, string property, JAXObjects.Token value)
        {
            int result = 0;
            string msg = "";
            string lcSQL = "";
            string schema = SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString();

            if (await IsConnected() == false)
            {
                ErrorCode = 6001;
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
            }
            else
            {
                try
                {
                    // Get the specified field information (MySQL)
                    lcSQL = $@"
                            SELECT 
                                TABLE_SCHEMA AS SchemaName, TABLE_NAME AS TableName,
                                COLUMN_NAME AS ColumnName, DATA_TYPE AS DataType,
                                CHARACTER_MAXIMUM_LENGTH AS MaxLengthChars,
                                CHARACTER_OCTET_LENGTH AS MaxLengthBytes,
                                NUMERIC_PRECISION AS Precision,
                                NUMERIC_SCALE AS Scale, IS_NULLABLE,
                                COLUMN_DEFAULT AS DefaultValue,
                                COLUMN_COMMENT AS ColumnDescription,
                                EXTRA, COLUMN_TYPE
                            FROM INFORMATION_SCHEMA.COLUMNS
                            WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{field}';";

                    lcSQL = lcSQL.Replace("\r", "").Replace("\n", "").Replace("\t", " ").Trim();

                    DataTable? fieldInfo = ExecuteSelect(lcSQL, out result);

                    DataRow row = fieldInfo!.Rows[0];

                    int iVal = 0;
                    string dataType = row["DataType"]?.ToString() ?? "";
                    string columnType = row["COLUMN_TYPE"]?.ToString() ?? "";
                    bool isNullable = (row["IS_NULLABLE"]?.ToString() ?? "").Equals("YES", StringComparison.OrdinalIgnoreCase);
                    int precision = Convert.ToInt32(row["Precision"] ?? 0);

                    bool noWidth = (JAXLib.InListC(dataType, "text", "blob", "date", "datetime", "mediumblob", "longblob", "tinyblob", "tinytext", "mediumtext", "longtext", "integer", "int"));
                    int scale = noWidth ? 0 : Convert.ToInt32(row["Scale"] ?? 0);

                    switch (property.ToLower())
                    {
                        case "fieldname":       // Rename column
                            lcSQL = $"ALTER TABLE `{schema}`.`{table}` RENAME COLUMN `{field}` TO `{value.AsString()}`;";
                            break;

                        case "null":            // Change nullability
                                                // Requires full column definition in MySQL
                                                // Simplified: rebuild type + nullability
                            string nullClause = value.AsBool() ? "NULL" : "NOT NULL";
                            lcSQL = $"ALTER TABLE `{schema}`.`{table}` MODIFY COLUMN `{field}` {columnType} {nullClause};";
                            break;

                        case "comment":         // Column comment
                            lcSQL = $"ALTER TABLE `{schema}`.`{table}` MODIFY COLUMN `{field}` {columnType} COMMENT '{value.AsString().Replace("'", "''")}';";
                            break;

                        case "fieldwidth":      // Change length of character/binary types
                            if (value.Element.Type.Equals("N"))
                            {
                                iVal = value.AsInt();

                                if (noWidth == false)
                                {
                                    if (JAXLib.InListC(dataType, "varchar", "char", "varbinary", "binary"))
                                    {
                                        if (JAXLib.Between(iVal, 1, 254) == false)
                                            ErrorCode = 9999;   // Invalid width for character/binary type
                                    }
                                    else
                                    {
                                        if (JAXLib.InListC(dataType, "double", "float", "int", "integer", "tinyint") && iVal < 1)
                                            ErrorCode = 9999;   // Invalid width for numeric type
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(dataType) == false && ErrorCode == 0)
                                {
                                    // Preserve nullability and rebuild type
                                    string nullStr = isNullable ? "NULL" : "NOT NULL";

                                    lcSQL = $"ALTER TABLE '{schema}'.'{table}' MODIFY COLUMN '{field}' {dataType}({iVal}) {nullStr};";

                                    if (JAXLib.InListC(dataType, "text", "blob", "date", "datetime", "mediumblob", "longblob", "tinyblob", "tinytext", "mediumtext", "longtext"))
                                        lcSQL = $"ALTER TABLE '{schema}'.'{table}' MODIFY COLUMN '{field}' {dataType} {nullStr};";
                                    else
                                        lcSQL = $"ALTER TABLE '{schema}'.'{table}' MODIFY COLUMN '{field}' {dataType}({iVal}) {nullStr};";
                                }
                                else
                                {
                                    ErrorCode = 9999;
                                    ErrorMsg = field;
                                }
                            }
                            else
                                ErrorCode = 11;
                            break;

                        case "fielddecimal":    // Change scale (decimal places)
                            int dec = value.AsInt();

                            if (dec < precision && dec >= 0)
                            {
                                string nullStr = isNullable ? "NULL" : "NOT NULL";
                                lcSQL = $"ALTER TABLE `{schema}`.`{table}` MODIFY COLUMN `{field}` {dataType}({precision}, {dec}) {nullStr};";
                            }
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

                            ErrorCode = 1999;
                            ErrorMsg = "FieldType is not yet implemented for MySQL";
                            break;

                        case "nextval":         // Set next AUTO_INCREMENT value
                            lcSQL = $"ALTER TABLE `{schema}`.`{table}` AUTO_INCREMENT = {value.AsInt()};";
                            break;

                        case "nextstep":        // MySQL does not support per-table increment step
                                                // auto_increment_increment is global/session only
                            break;

                        default:
                            ErrorCode = 9999;
                            ErrorMsg = property.ToUpper();
                            break;
                    }

                    if (ErrorCode == 0 && !string.IsNullOrWhiteSpace(lcSQL))
                    {
                        ExecuteSelect(lcSQL, out result);

                        if (result < 0)
                            SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                    }
                    else
                        result = -1;
                }
                catch (Exception ex)
                {
                    // Could not find the field
                    ErrorCode = 9999;
                    ErrorMsg = ex.Message;
                }
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SQLDatabaseInfo()");
                result = -1;
            }

            return result;
        }

        public async Task<int> AlterIndex(string parm1, string parm2, string parm3, string parm4)
        {
            return 0;
        }

        public async Task<int> DropField(string tableName, string fieldName)
        {
            int result = ErrorCode = 0;
            string msg = "";
            string lcSQL = "";
            string schema = SQLBase.UserProperties[JAXLanguageLists.GetWord("database", "REVPEMS")].AsString();

            if (await IsConnected() == false)
            {
                ErrorCode = 6001;
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
            }
            else
            {
                try
                {
                    lcSQL = $"ALTER TABLE {schema}.{tableName} DROP COLUMN {fieldName};";

                    ExecuteSelect(lcSQL, out result);

                    if (result < 0)
                    {
                        SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, ErrorMsg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                        ErrorCode = 9999;
                    }
                }
                catch (Exception ex)
                {
                    ErrorCode = 9999;
                    msg = ex.Message;
                }
            }

            if (ErrorCode > 0)
            {
                SQLBase._AddError(ErrorCode, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                AppIO.DebugLog($"Error {ErrorCode} - {JAXError.JAXErrMsg(ErrorCode, msg)} in XBase_ClassSQL_MSSS.SQLDatabaseInfo()");
                result = -1;
            }

            return result;
        }

        public async Task<int> DropIndex(string parm1, string parm2)
        {
            return 0;
        }
    }
}