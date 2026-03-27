namespace JAXBase.XBase
{
    public class XBase_Class_Collection :XBase_Avalonia
    {
        public XBase_Class_Collection(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "collection" : name;
            SetVisualObject(null, "Collection", name, false, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {

            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }
    }
}
