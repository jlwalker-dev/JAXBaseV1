/* 
 * NONE SQL Engine used during initialization of the SQL Class to prevent errors
 */
using JAXBase.Core;
using JAXBase.Data;

namespace JAXBase.XBase
{
    public class XBase_ClassSQL_NONE : SQLClass
    {
        public XBase_ClassSQL_NONE(AppClass app)
        {
        }

        public int AlterTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            return 1999;
        }

        public int Connect()
        {
            return 1999;
        }

        public int CreateIndex(string tableName, string indexinfo)
        {
            return 1999;
        }

        public int CreateSP(string procName, string procCode)
        {
            return 1999;
        }


        public int CreateTable(string tableName, List<JAXTables.FieldInfo> Fields)
        {
            return 1999;
        }

        public int DeleteIndex(string tableName, string indexinfo)
        {
            return 1999;
        }

        public int Disconnect()
        {
            return 1999;
        }

        /*
         * Drop a table from the database
         */
        public int DropTable(string tableName)
        {
            return 1999;
        }

        /*
         * Return the table structure using JAXBase field codes
         */
        public int GetTableStructure(string tableName, out List<JAXTables.FieldInfo> Fields)
        {
            Fields = [];
            return 1999;
        }


        /*
         * Execute a SQL statement and return a datatable, scalar result,
         * or the number of affected rows.
         */
        public int Execute(string sql, out object? returnObject)
        {
            returnObject= null;
            return 1999;
        }


        public int ExecuteSP(string procName, List<xParameters> parameters)
        {
            return 1999;
        }

        public JAXErrors GetErrorMsg()
        {
            throw new Exception("1999||No SQL engine defined");
        }

        public int GetSPCode(string procName)
        {
            return 1999;
        }

        public int Setup(List<xParameters> parameters)
        {
            return 1999;
        }

        public int SetParameterString(string Parameters)
        {
            return 1999;
        }

        public int SetParameter(string parameter, JAXObjects.Token value)
        {
            return 1999;
        }

        public int CreateDatabase(string name) { return 1999; }
        public int GetIndex(string name, out string idxInfo) { idxInfo = string.Empty; return 1999; }
        public int ListDatabases(out List<string> dbList)
        {
            dbList = [];
            return 1999;
        }


        public int ListIndexes(out List<string> idxList) { idxList = []; return 1999; }

        public int ListTables(out List<string> tblList)
        {
            tblList = [];
            return 1999;
        }

        public int GetState()
        {
            return 1999;
        }

        public int GetKind() { return 1999; }

        public string GetConnectionString() { return string.Empty; }

        public int SetConnectionString(string connString)
        {
            return 1999;
        }
    }
}