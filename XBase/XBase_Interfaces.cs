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
    //public interface IJAXClass
    //{
    //    public Dictionary<string, JAXObjects.Token> UserProperties { get; }
    //    public Dictionary<string, MethodClass> Methods { get; }
    //    public bool PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList);
    //    public int GetProperty(string propertyName, out JAXObjects.Token returnToken);
    //    public int GetProperty(string propertyName, int idx, out JAXObjects.Token returnToken);
    //    public List<GenericClass> GetPEMList();
    //    public JAXObjectWrapper? GetObject(string propertyName, out int idx);
    //    public JAXObjectWrapper GetObject(int idx);
    //    public int GetObjectIDX();
    //    public int GetObjectProperty(int idx, string propertyName, out JAXObjects.Token returnToken);
    //    public int AddObject(JAXObjectWrapper token);
    //    public bool HasProperty(string propertyName);
    //    public string IsMember(string name);
    //    public int InsertObjectAt(JAXObjectWrapper obj, int moveIDX);
    //    public int ResetPropertyToDefault(string name);
    //    public int RemoveObject(int idx);
    //    public int SetDefault(string cmd);
    //    public int SetObjectIDX(int idx);
    //    public int SetObjectProperty(int idx, string propertyName, JAXObjects.Token value);
    //    //public int SetProperty(string propertyName, object value);
    //    public int SetProperty(string propertyName, object value, int objIdx);
    //    public int AddProperty(string propertyName);
    //    public int AddProperty(string propertyName, string lockType, string lockValue);
    //    public int AddProperty(string propertyName, JAXObjects.Token token);
    //    public int AddProperty(string propertyName, JAXObjectWrapper token);
    //    public string DefaultName();
    //    public int _CallMethod(string methodName);
    //    public int DoDefault(string methodName);
    //    public int _SetMethod(string methodName, string sourceCode, string CompiledCode, string Type);
    //    public void _AddError(int errorNo, int lineNo, string message, string procedure);
    //    public void SetAllOfClass(string className, string property, JAXObjects.Token objtk);
    //    public void SetAllOfBaseClass(string baseClassName, string property, JAXObjects.Token objtk);
    //    public string[] JAXMethods();
    //    public string[] JAXEvents();
    //    public string[] JAXProperties();
    //}

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

    //public interface IJAXClasses
    //{
    //    public Dictionary<string, JAXObjects.Token> UserProperties { get; }
    //    public Dictionary<string, MethodClass> Methods { get; }
    //    public bool PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList);
    //    public int GetProperty(string propertyName, out JAXObjects.Token returnToken);
    //    public int GetProperty(string propertyName, int idx, out JAXObjects.Token returnToken);
    //    public List<GenericClass> GetPEMList();
    //    public JAXObjectWrapper? GetObject(string propertyName, out int idx);
    //    public JAXObjectWrapper GetObject(int idx);
    //    public int GetObjectIDX();
    //    public int GetObjectProperty(int idx, string propertyName, out JAXObjects.Token returnToken);
    //    public int AddObject(JAXObjectWrapper token);
    //    public bool HasProperty(string propertyName);
    //    public string IsMember(string name);
    //    public int InsertObjectAt(JAXObjectWrapper obj, int moveIDX);
    //    public int ResetPropertyToDefault(string name);
    //    public int RemoveObject(int idx);
    //    public int SetDefault(string cmd);
    //    public int SetObjectIDX(int idx);
    //    public int SetObjectProperty(int idx, string propertyName, JAXObjects.Token value);
    //    //public int SetProperty(string propertyName, object value);
    //    public int SetProperty(string propertyName, object value, int objIdx);
    //    public int AddProperty(string propertyName);
    //    public int AddProperty(string propertyName, string lockType, string lockValue);
    //    public int AddProperty(string propertyName, JAXObjects.Token token);
    //    public int AddProperty(string propertyName, JAXObjectWrapper token);
    //    public string DefaultName();
    //    public int _CallMethod(string methodName);
    //    public int DoDefault(string methodName);
    //    public int _SetMethod(string methodName, string sourceCode, string CompiledCode, string Type);
    //    public void _AddError(int errorNo, int lineNo, string message, string procedure);
    //    public void SetAllOfClass(string className, string property, JAXObjects.Token objtk);
    //    public void SetAllOfBaseClass(string baseClassName, string property, JAXObjects.Token objtk);
    //    public int GetPrivateProperty(string propertyName, out JAXObjects.Token value);
    //    public int SetPrivateProperty(string propertyName, object? value);
    //    public string[] JAXMethods();
    //    public string[] JAXEvents();
    //    public string[] JAXProperties();
    //}



    // SQL Classes are wrapped by XBase_Class_SQL so that JAXObjectWrapper can use them
    public interface SQLClass
    {
        public int Execute(string sql, out object? returnObject);
        public int Connect();
        public int Disconnect();
        public int DropTable(string tableName);
        public int CreateTable(string tableName, List<JAXTables.FieldInfo> Fields);
        public int AlterTable(string tableName, List<JAXTables.FieldInfo> Fields);
        public int GetTableStructure(string tableName, out List<JAXTables.FieldInfo> Fields);
        public int CreateIndex(string tableName, string indexinfo);
        public int DeleteIndex(string tableName, string indexinfo);
        public int CreateSP(string procName, string procCode);
        public int ExecuteSP(string procName, List<xParameters> parameters);
        public int GetSPCode(string procName);
        public int Setup(List<xParameters> parameters);
        public int SetParameterString(string Parameters);
        public int SetParameter(string parameter, JAXObjects.Token value);
        public JAXErrors GetErrorMsg();
        public int GetKind();
        public int CreateDatabase(string name);
        public int GetIndex(string name, out string idxInfo);
        public int ListDatabases(out List<string> dbList);
        public int ListIndexes(out List<string> idxList);
        public int ListTables(out List<string> tblList);
        public int GetState();
        public string GetConnectionString();
        public int SetConnectionString(string connString);

    }
}
