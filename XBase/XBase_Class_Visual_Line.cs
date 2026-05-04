/*
 * Dedicated line class - will only hold two (2) in the points property.
 * Ignores fillstyle, fillcolor, and polypoints properties.
 * 
 * Line class uses almost no realestate compared to VFP because
 * the height and width set the boundaries, but is not the area
 * that the line occupies.  You have to click on the line to
 * get it to fire the click events.
 * 
 */
namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Line : XBase_Class_Visual_ShapeBase
    {
        public new string MyBaseClass { get; } = "Line";
        public new string MyDefaultName { get; } = "line";


        public XBase_Class_Visual_Line(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "Line", "line", false, UserObject.urw);
        }

        public override Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            UserProperties.Remove("curvature");
            UserProperties.Remove("fillcolor");
            UserProperties.Remove("polypoints");

            return base.PostInit(callBack, parameterList);
        }
    }
}
