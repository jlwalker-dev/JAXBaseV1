/*
 * Testing shows the curvature is largely unneeded to create a
 * nice looking circle or ellipse if you put in 1000 or a higher
 * number for Points.  Speed does not seem to really suffer.
 * 
 * Like the line, you have to click directly onto the shape
 * to get the click events to fire.
 * 
 */
namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Shape : XBase_Class_Visual_ShapeBase
    {
        public XBase_Class_Visual_Shape(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "Shape", "shape", true, UserObject.urw);
        }
    }
}
