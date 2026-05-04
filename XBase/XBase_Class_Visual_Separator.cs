/*------------------------------------------------------------------------------------------*
 * Separator class
 * 
 * 2025.11.17 - JLW
 *      Created this class to be a component of the menu class that creats a
 *      visual separator.  Not actually a visual object, but a component of a
 *      menu which is a visual object.
 *      
 *      At this time it's just a visual separator for menus, but could be expanded
 *      to also support properties, methods, and events in the future.
 *      
 *------------------------------------------------------------------------------------------*/
namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Separator : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Separator";
        public new string MyDefaultName { get; } = "separator";


        public Avalonia.Controls.Separator separator => (Avalonia.Controls.Separator)me.avaloniaObject!;

        public XBase_Class_Visual_Separator(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Separator(), "Separator", "separtor", true, UserObject.urw);
            me.nvObject= new ToolStripSeparator();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public override string[] JAXProperties()
        {
            return
                [
                "baseclass,C!,separator",
                "class,C!,separator","classlibrary,C!,","comment,c,",
                "name,c,",
                "parent,o!,","parentclass,c!,",
                "tag,c,",
                "visible,l,.T.",
                ];
        }

    }
}
