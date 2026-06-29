/* 
 * MSSQL Engine support
 */
using JAXBase.Core;
using JAXBase.Data;
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

        public async Task<int> Connect(string ConnString)
        {
            int result = 0;
            string msg = string.Empty;

            // Try to make the connection
            if (result == 0)
            {
                try
                {
//                    ConnectionString = $"Server={DataSource};Database={Database};Uid={ConnectionUserID};Pwd={ConnectionPassword}";

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
            int result = 0;


            return result;
        }

        public async Task<int> SetParameter(string parameter, JAXObjects.Token value)
        {

            int result = 0;
            string type = value.Element.Type;


            return result;
        }

        public async Task<int> CreateDatabase(string name) { return 1999; }

        public async Task<int> GetIndex(string tableName, string indexName = "", string cursorName = "")
        {
            return 0; 
        }

        public int ListDatabases(out List<string> dbList)
        {
            int result = 0;
            dbList = [];
            string cursorName = "LT" + Program.CurrentApp.SystemCounter();
            //int result = Execute("SELECT SCHEMA_NAME AS 'Database' FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME IS NOT NULL AND SCHEMA_NAME NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys') ORDER BY SCHEMA_NAME;", cursorName);


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
            int result = 0;
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