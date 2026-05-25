/*
 * Load an image to an array that will be passed to a form
 * 
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Image : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Image";
        public new string MyDefaultName { get; } = "image";

        public Avalonia.Controls.Image img => (Avalonia.Controls.Image)me.avaloniaObject!;

        public XBase_Class_Visual_Image(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Image(), "Image", "image", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            if (InInit)
            {
            }

            bool result = await base.PostInit(callBack, parameterList);
            await SetProperty("picture", "*nog*",0);
            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Was not found - not yet processed
         *      2   - Requires special handling, did not process
         *      3   - Not a class property
         *      9   - Processed and saved, do not do anything else
         *      10  - Processed and saved
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    JAXObjects.Token tk = new();
                    tk.Element.Value = objValue;

                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objValue);
                            //img.BorderColor = XClass_AuxCode.IntToColor((int)objValue);
                            UserProperties["bordercolor"].Element.Value = objValue;
                            result = 9; // do nothing else
                            break;

                        case "borderwidth":
                            if (tk.Element.Type.Equals("N"))
                            {
                                //if (JAXLib.Between(tk.AsInt(), 0, 15))
                                //    img.BorderWidth = tk.AsInt();
                                //else
                                //    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "picture":
                            if (tk.Element.Type.Equals("C"))
                                img.Source = Program.CurrentApp.JaxImages!.GetImage(tk.AsString(), out _);
                            else
                                result = 11;
                            break;

                        case "stretch":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 2))
                                {
                                    switch (tk.AsInt())
                                    {
                                        case 1:     // Isometric
                                            img.Stretch = Avalonia.Media.Stretch.Uniform;
                                            break;

                                        case 2:     // Stretch
                                            img.Stretch = Avalonia.Media.Stretch.Fill;
                                            break;

                                        default:    // Clip
                                            img.Stretch = Avalonia.Media.Stretch.None;
                                            break;
                                    }

                                    objValue = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;  // 0 -> 9 = successfully processed.  Don't do anything else!
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                        {
                            // We processed it or just need to save the property (perhaps again)
                            // Ignore the CA1854 as it won't put the value into the property
                            UserProperties[propertyName].Element.Value = objValue;
                        }

                        result = 0;
                    }
                }
                else
                    result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                // Get the property and fill in the value
                returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]);
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","move","readexpression","readmethod","refresh",
                "saveasclass","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","visiblechanged","when"
                ];
        }

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
         *          $ Special Handling - do not auto process
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
                [
                "anchor,n,0","autosize,l,false",
                "BaseClass,C,image","bordercolor,R,0","borderwidth,N,0",
                "Class,C,Grid","ClassLibrary,C,","Comment,C,",
                "Height,N,0",
                "left,N,0",
                "name,c,command",
                "parent,o,","parentclass,C,","picture,c,","picturemargin,n,0","pictureposition,n,13","picturespacing,n,0",
                "stretch,n,0",
                "tabstop,L!,false","tag,C,","top,N,0","tooltiptext,c,",
                "visible,l,true",
                "width,N,10"
                ];
        }
    }
}
