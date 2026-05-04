/*
 * The Empty Class.  It's a thing of beauty!
 */
namespace JAXBase.XBase
{
    public class XBase_Class_Empty : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "Empty";
        public new string MyDefaultName = "empty";

        public XBase_Class_Empty(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "Empty", string.Empty, false, UserObject.urw);
            me.BaseClass = "EMPTY";
            me.Class = string.IsNullOrWhiteSpace(name) ? "EMPTY" : name;
            me.ClassID = jow.App.SystemCounter();
            me.nvObject = new EmptyFactory();
        }
    }
}
