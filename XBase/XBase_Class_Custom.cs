namespace JAXBase.XBase
{
    public class XBase_Class_Custom : XBase_Class_Avalonia
    {
        public XBase_Class_Custom(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "custom" : name;
            SetVisualObject(null, "Custom", name, false, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public override string[] JAXMethods()
        {
            return ["addobject", "addproperty", "readexpression","readmethod","removeobject","resettodefault","saveasclass","writeexpression", "writemethod"];
        }

        public override string[] JAXEvents()
        {
            return ["destroy", "error", "init", "load"];
        }

        /*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - can't change after initialization
         *          $ Special Handling - do not auto process
         */
        public override string[] JAXProperties()
        {
            return [
                "application,c,",
                "baseclass,C!,Custom",
                "class,C!,Custom","classlibrary,C$,","comment,C,","controlcount,N,0",
                "Height,N,0",
                "left,N,0",
                "name,C,custom",
                "objects,*,",
                "parent,o$,","parentclass,C$,","picture,c,",
                "tag,C,","top,N,0",
                "width,N,0"
                ];
        }
    }
}
