/* 
 * NONE SQL Engine used during initialization of the SQL Class to prevent errors
 */
using JAXBase.Core;
using JAXBase.Data;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_NONE : SQLClass
    {
        public int ErrorCode { get; private set; } = 6004;
        public string ErrorMsg { get; private set; } = "";

        public XBase_ClassSQL_NONE(XBase_Class_SQL app)
        {
        }

        public async Task<bool> IsConnected()
        {
            return false;
        }

        public async Task<int> AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            return 6004;
        }

        public async Task<int> Connect(string connString)
        {
            return 6009;
        }

        public async Task<int> CreateIndex(string indexName, string tableName, string indexExpression, string filter = "", string attribs = "")
        {
            return 6004;
        }

        public async Task<int> CreateSP(string procName, string procCode)
        {
            return 6004;
        }


        public async Task<int> CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            return 6004;
        }

        public async Task<int> CreateView(string viewName, string viewCode)
        {
            return 6004;
        }


        public async Task<int> DeleteDB(string dbName)
        {
            return 6004;
        }

        public async Task<int> DeleteSP(string ProcName)
        {
            return 6004;
        }
        public async Task<int> DeleteTable(string tableName)
        {
            return 6004;
        }
        public async Task<int> DeleteView(string viewName)
        {
            return 6004;
        }

        public async Task<int> DeleteIndex(string indexName, string Name)
        {
            return 6004;
        }

        public async Task<int> Disconnect()
        {
            return 0;
        }

        /*
         * Drop a table from the database
         */
        public async Task<int> DropTable(string tableName)
        {
            return 6004;
        }

        /*
         * Return the table structure using JAXBase field codes
         */
        public async Task<int> GetTableStructure(string tableName, string varName) { return 6004; }


        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public async Task<int> Execute(string sql, string cursorName)
        {
            return 6004;
        }


        public async Task<int> ExecuteSP(string procName, List<xParameters> parameters)
        {
            return 6004;
        }

        public JAXErrors GetErrorMsg()
        {
            throw new Exception("6004||No SQL engine defined");
        }

        public async Task<int> GetSPCode(string procName, string cursorName)
        {
            int result = 6004;
            return result;
        }

        public async Task<int> Setup(List<xParameters> parameters)
        {
            return 0;
        }

        public async Task<int> SetParameterString(string Parameters)
        {
            return 0;
        }

        public async Task<int> SetParameter(string parameter, JAXObjects.Token value)
        {
            return 0;
        }

        public async Task<int> CreateDatabase(string name) { return 6004; }

        public async Task<int> GetIndex(string tableName, string indexName = "", string cursorName = "")
        {
            return 0;
        }

        public int ListDatabases(out List<string> dbList)
        {
            dbList = [];
            return 6004;
        }


        public int ListIndexes(string tableName, out List<string> idxList) { idxList = []; return 6004; }

        public int ListTables(out List<string> tblList)
        {
            tblList = [];
            return 6004;
        }

        public int GetState() { return 0; }

        public int GetKind() { return 0; }

        public string GetConnectionString() { return ""; }

        public async Task<int> SetConnectionString(string connString)
        {
            return 0;
        }

        public async Task<int> GetDatabaseInfo(string dbName, string cursorName)
        {
            int result = 0;
            return result;
        }

        public async Task<int> GetView(string viewName, string cursorName)
        {
            int result = 6004;
            return result;
        }



        public async Task<int> AlterField(string parm1, string parm2, string parm3, int parm4, int parm5)
        {
            return 6004;
        }

        public async Task<int> AlterProperty(string parm1, string parm2, string parm3, JAXObjects.Token parm4)
        {
            return 6004;
        }

        public async Task<int> AlterIndex(string parm1, string parm2, string parm3, string parm4)
        {
            return 6004;
        }

        public async Task<int> DropField(string parm1, string parm2)
        {
            return 6004;
        }

        public async Task<int> DropIndex(string parm1, string parm2)
        {
            return 6004;
        }

    }
}