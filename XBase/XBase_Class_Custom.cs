/*
 * 2026.06.09 - JLW
 * 
 *      Finally getting around to finishing this class.  The Custom class is
 *      basically like the empty class execpt it has a few dedicated properties
 *      and methods and can handle objects. 
 *      
 *      
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Custom : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "Custom";
        public new string MyDefaultName = "custom";

        public XBase_Class_Custom(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "custom" : name;
            SetVisualObject(null, "Custom", name, false, UserObject.URW);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Get the property (no call to base)
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName.ToLower())
                {
                    case "objects":
                        if (JAXLib.Between(idx, 0, UserProperties["objects"].Count))
                        {
                            UserProperties["objects"].ElementNumber = idx;
                            returnToken.Element.Value = UserProperties["objects"].Element.Value;
                        }
                        else
                            result = 3028;
                        break;
                }

                if (JAXLib.Between(result, 0, 10))
                {
                    if (result < 9)
                        returnToken.Element.Value = UserProperties[propertyName].Element.Value;

                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * Set the property (no call to base)
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName.ToLower())
                    {
                        case "application":
                        case "comment":
                        case "name":
                        case "tag":
                            if (tk.Element.Type.Equals("C") == false)
                                result = 11;
                            break;

                        case "height":
                        case "left":
                        case "top":
                        case "width":
                            if (tk.Element.Type.Equals("N") == false)
                                result = 11;
                            break;
                    }
                }
                else
                    result = 1559;

                if (result == 0)
                    UserProperties[propertyName].Element.Value = tk.Element.Value;
            }

            // Deal with errors
            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        public override string[] JAXMethods() =>
            [
            "addobject", "addproperty", "readexpression","readmethod","removeobject","resettodefault",
            "saveasclass","writeexpression", "writemethod"
            ];


        public override string[] JAXEvents() => ["destroy", "error", "init", "load"];


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
                "parent,o$,","parentclass,C$,",
                "tag,C,","top,N,0",
                "width,N,0"
                ];
        }
    }
}
