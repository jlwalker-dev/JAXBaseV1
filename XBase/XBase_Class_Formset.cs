using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Formset : XBase_Avalonia
    {
        public new string MyBaseClass = "FormSet";
        public new string MyDefaultName = "formset";

        public object formset => (object)me.nvObject!;
        private bool windowLocked = false;

        public XBase_Class_Formset(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "formset" : name;
            SetVisualObject(null, "Formset", name, false, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        /*
         * Handle any cases that need special processing when
         * adding a new object to the form
         */
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;

            // Add valid controls to the canvas
            // -------------------------------------------------------------
            // Always add the control to the canvas unless it's
            // a top level menu which must be on the form
            if (value.avaloniaObject is not null)
            {
                if (value.BaseClass.Equals("form", StringComparison.OrdinalIgnoreCase))
                {
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                }
                else
                    err = 1903;
            }
            else
                err = 1901;

            if (err > 0)
            {
                // Something went wrong
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }


        /*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Processed and saved, do not do anything else
         *      10  - Processed and saved
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *     -1   - Error Code
         * 
         */
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();

            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;

            if (InInit == false)
                AppIO.DebugLog($"FORM.{propertyName}={objtk.AsString()}");

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    // Visual object common property handler
                    switch (propertyName)
                    {
                        case "datasession":
                            // Read only at runtime
                            if (Program.CurrentApp.RuntimeFlag == false)
                            {
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    int v = objtk.AsInt();
                                    objValue = v > 1 ? 2 : 1;
                                }
                            }
                            else
                                result = 11;
                            break;


                        case "windowtype":
                            if (windowLocked)
                            {
                                result = 9702;
                            }
                            else if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 2) == false)
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;


                        case "visible":
                            if (objtk.Element.Type.Equals("L"))
                            {
                                // find out what to do
                            }
                            break;

                        case "windowstate":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 2) == false)
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // Did we process it?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }
            else
                result = 0;

            return result;
        }



        public override string[] JAXMethods()
        {
            return
                [
                "addobject","addproperty","readexpression","readmethod","refresh","release",
                    "removeobject","saveas","saveasclass","setall","setfocus","show","writeexpression", "writemethod"
                ];
        }

        public override string[] JAXEvents()
        {
            return
                [
                "activate","deactivate","destroy","error","init","load","unload"
                ];
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
                "activeform,o!,","autorelease,l,.F.",
                    "baseclass,C!,formset","buffermode,n,0",
                    "class,C!,Form","classlibrary,C!,","comment,C,","controlcount,N!,0",
                    "datasession,n,1","datasessionid,n!,1",
                    "name,C,form",
                    "objects,*,",
                    "parent,o!,","parentclass,C!,",
                    "tag,C,",
                    "visible,l,.T.",
                    "windowtype,n,0"
                ];
        }
    }
}
