/*
 * 2026.06.09 - JLW
 *      Finally getting round to finishing this class.
 *
 *      This emulates the VFP collection class.  Using the DataDictionary JAXObjects.Token type
 *      it will allow you to support a collection of objects.
 *
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Collection : XBase_Avalonia
    {

        public new string MyBaseClass = "Collection";
        public new string MyDefaultName = "collection";

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


        /*------------------------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here to get the value of the 
         * property from the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token? returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (JAXLib.Between(idx, 0, UserProperties[propertyName]._avalue.Count - 1))
                    returnToken = new(UserProperties[propertyName]._avalue[idx].Value);
                else
                    result = 3028;
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                returnToken.Element.IsNull();
            }

            return returnToken;
        }

        /*------------------------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here for basic storing of the 
         * property to the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;

            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;

            if (InInit == false)
                AppIO.DebugLog($"MyObj={me.JOWName} BASE.{propertyName}={objtk.AsString()}");

            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "item":
                            result = 1737;
                            break;

                        default:
                            if (string.IsNullOrWhiteSpace(UserProperties[propertyName].Element._setAsType) || UserProperties[propertyName].Element._setAsType.Equals(objtk.Element.Type))
                                UserProperties[propertyName].Element.Value = objValue;
                            else
                                result = 3023;
                            break;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods() =>
                [
                "add", "addproperty", "getkey", "item", "readexpression", "readmethod", "remove", "resettodefault",
                "saveasclass", "writeexpression", "writemethod"
                ];


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents() =>
                [
                "destroy","error","init"
                ];

        /*------------------------------------------------------------------------------------------*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - can't change after initialization
         *          * Array
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties() =>
                [
                "BaseClass,C!,collection",
                "Class,C!,collection","ClassLibrary,C,","count,n!,0","comment,C,",
                "item,o*,",
                "keysort,N,0",
                "name,c,collection",
                "parent,o!,","parentclass,C!,",
                "tag,C,"
                ];
    }
}
