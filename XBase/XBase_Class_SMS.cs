namespace JAXBase.XBase
{
    public class XBase_Class_SMS : XBase_Avalonia
    {
        public new string MyBaseClass = "SMS";
        public new string MyDefaultName = "sms";

        // === SMS SETTINGS ===

        public XBase_Class_SMS(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "sms" : name;
            SetVisualObject(null, "SMS", name, false, UserObject.URW);
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
