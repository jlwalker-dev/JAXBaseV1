using JAXBase.Core;
using JAXBase.Data;
using static JAXBase.XBase.JAXObjectsAux;

namespace JAXBase.XBase
{
    public class XBase_Interfaces
    {
    }

    /*-------------------------------------------------------------------------------------------------*
     * This interface defines how all of the classes are constructed, specifying
     * the required methods so that they all act in the same manner
     * 
     * For the most part, you cannot access any property without going through
     * one of these calls
     *-------------------------------------------------------------------------------------------------*/
    public interface IJAXAvaClass
    {
        public Dictionary<string, JAXObjects.Token> UserProperties { get; }
        public Dictionary<string, MethodClass> Methods { get; }
        public Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList);
        public Task<bool> PostClassInit();
        public Task<JAXObjects.Token> GetProperty(string propertyName);
        public Task<JAXObjects.Token> GetProperty(string propertyName, int idx);
        public List<GenericClass> GetPEMList();
        public Task<JAXObjectWrapper?> GetObject(string propertyName);
        public Task<JAXObjectWrapper?> GetObject(int idx);
        public int GetObjectIDX();
        public Task<JAXObjects.Token> GetObjectProperty(int idx, string propertyName);
        public Task<int> AddObject(JAXObjectWrapper token);
        public bool HasProperty(string propertyName);
        public Task<string> IsMember(string name);
        public int InsertObjectAt(JAXObjectWrapper obj, int moveIDX);
        public Task<int> ResetPropertyToDefault(string name);
        public int RemoveObject(int idx);
        public Task<int> SetDefault(string cmd);
        public int SetObjectIDX(int idx);
        public int SetObjectProperty(int idx, string propertyName, JAXObjects.Token value);
        //public int SetProperty(string propertyName, object value);
        public Task<int> SetProperty(string propertyName, object value, int objIdx);
        public int AddProperty(string propertyName);
        public int AddProperty(string propertyName, string lockType, string lockValue);
        public int AddProperty(string propertyName, JAXObjects.Token token);
        public int AddProperty(string propertyName, JAXObjectWrapper token);
        public string DefaultName();
        public Task<int> _CallMethod(string methodName);
        public Task<int> DoDefault(string methodName);
        public int _SetMethod(string methodName, string sourceCode, bool createOK, string Type);
        public void _AddError(int errorNo, int lineNo, string message, string procedure);
        public void SetAllOfClass(string className, string property, JAXObjects.Token objtk);
        public void SetAllOfBaseClass(string baseClassName, string property, JAXObjects.Token objtk);
        public int GetPrivateProperty(string propertyName, out JAXObjects.Token value);
        public int SetPrivateProperty(string propertyName, object? value);
        public void ApplyVFPAnchor(double DeltaX, double DeltaY);
        public string[] JAXMethods();
        public string[] JAXEvents();
        public string[] JAXProperties();
    }




    /*
     * This interface is the SQL engines that are wrapped by the XBase_Class_SQL 
     * so that the SQL engines are then compatible with the JAXBase system.
     * 
     * All core JAXBase classes have to use the IJAXAvaClass interface in order 
     * to be treated as objects that can be stored in a variable.
     * 
     */
    public interface SQLClass
    {
        public Task<int> AlterTable(string tableName, List<JAXTables.FieldInfo> Fields);
        public Task<int> AlterField(string parm1, string parm2, string parm3, int parm4, int parm5);
        public Task<int> AlterProperty(string parm1, string parm2, string parm3, JAXObjects.Token parm4);
        public Task<int> AlterIndex(string parm1, string parm2, string parm3, string parm4);
        public Task<int> DropField(string parm1, string parm2);
        public Task<int> DropIndex(string parm1, string parm2);
        public Task<int> Connect(string connString);
        public Task<int> CreateDatabase(string name);
        public Task<int> CreateIndex(string indexName, string tableName, string indexExpression, string filter, string attribs);
        public Task<int> CreateSP(string procName, string procCode);
        public Task<int> CreateTable(string tableName, List<JAXTables.FieldInfo> Fields);
        public Task<int> CreateView(string viewName, string viewCode);
        public Task<int> DeleteDB(string dbName);
        public Task<int> DeleteIndex(string indexName, string tableName);
        public Task<int> DeleteSP(string procName);
        public Task<int> DeleteTable(string tableName);
        public Task<int> DeleteView(string viewName);
        public Task<int> Disconnect();
        public Task<int> Execute(string sql, string sqlCursor);
        public Task<int> ExecuteSP(string procName, List<xParameters> parameters);
        public Task<int> GetDatabaseInfo(string dbName, string cursorName);
        public Task<int> GetIndex(string tableName, string indexName, string cursorName);
        public int GetKind();
        public Task<int> GetTableStructure(string tableName, string cursorName);
        public Task<int> GetView(string viewName, string cursorName);
        public int GetState();
        public string GetConnectionString();
        public Task<int> GetSPCode(string procName, string cursorName);
        public Task<bool> IsConnected();
        public int ListDatabases(out List<string> dbList);
        public int ListIndexes(string tableName, out List<string> idxList);
        public int ListTables(out List<string> tblList);
        public Task<int> SetParameterString(string Parameters);
        public Task<int> SetParameter(string parameter, JAXObjects.Token value);
        public Task<int> SetConnectionString(string connString);
    }
}
