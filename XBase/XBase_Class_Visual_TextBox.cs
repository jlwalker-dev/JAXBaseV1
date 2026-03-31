/*------------------------------------------------------------------------------------------*
 * Textbox Visual subclass of XBase_Class_Visual which is subclass of XBase_Class
 * 
 * 2025-11-14 - JLW
 *      Basic property and method support.
 *      
 *------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.UI.Controls;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_TextBox : XBase_Class_Avalonia
    {
        public int MaxLength = 0;

        public JAXMaskedTextBox txt => (JAXMaskedTextBox)me.avaloniaObject!;

        public XBase_Class_Visual_TextBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new JAXMaskedTextBox(), "TextBox", "text", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            if (InInit)
            {
                txt.TextChanged += Txt_TextChanged;
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        private async void Txt_TextChanged(object? sender, EventArgs e)
        {
            if (isProgrammaticChange)
                await _CallMethod("programmaticchange");
            else
                await _CallMethod("interactivechange");
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      >10 - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            int val;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    result = 0;

                    tk.Element.Value = objValue;

                    // Intercept property handling
                    switch (propertyName.ToLower())
                    {
                        case "bordercolor":
                            txt.BorderBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            break;

                        case "borderwidth":
                            txt.BorderThickness = new Avalonia.Thickness(tk.AsInt());
                            break;

                        case "fontbold":
                            if (tk.Element.Type.Equals("L"))
                            {
                                txt.FontWeight = tk.AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                            }
                            else
                                result = 11;
                            break;

                        case "fontitalic":
                            if (tk.Element.Type.Equals("L"))
                            {
                                txt.FontStyle = tk.AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                            }
                            else
                                result = 11;
                            break;

                        case "fontname":
                            if (tk.Element.Type.Equals("C"))
                            {
                                txt.FontFamily = new Avalonia.Media.FontFamily(tk.AsString());
                            }
                            else
                                result = 11;
                            break;

                        case "fontsize":
                            if (tk.Element.Type.Equals("N"))
                            {
                                txt.FontSize = tk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "format":
                            if (tk.Element.Type.Equals("C"))
                                txt.JAXFormat = tk.AsString();
                            else
                                result = 11;
                            break;

                        case "inputmask":
                            if (tk.Element.Type.Equals("C"))
                                txt.JAXMask = tk.AsString();
                            else
                                result = 11;
                            break;

                        case "maxlength":
                            if (tk.Element.Type.Equals("N") == false)
                                throw new Exception("11|");

                            val = tk.AsInt();
                            if (val < 0)
                                result = 41;
                            else
                            {
                                // Set the maxlength - 0 = no max
                                MaxLength = val;
                            }
                            break;

                        case "nulldisplay":
                            if (tk.Element.Type.Equals("C"))
                                txt.NullDisplay = tk.AsString();
                            else
                                result = 11;
                            break;

                        case "readonly":
                            if (tk.Element.Type.Equals("L") == false)
                                throw new Exception("11|");

                            txt.IsReadOnly = tk.AsBool();
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            txt.SetValue(tk.Element.Value);
                            isProgrammaticChange = false;
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
                // log the error
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }
            else
                result = 0; // No error

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                result = 0;

                // Post handling of getproperty
                switch (propertyName)
                {
                    case "maxlength":
                        returnToken.Element.Value = txt.MaxLength;
                        break;

                    case "readonly":
                        returnToken.Element.Value = txt.IsReadOnly;
                        break;

                    case "text":
                        returnToken.Element.Value = txt.Text ?? string.Empty;
                        break;

                    case "value":
                        returnToken= txt.GetValue();
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * Methods for class
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty", "move", "readexpression", "readmethod", "refresh", "resettodefault",
                "saveasclass", "settooriginalvalue", "setfocus", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * Events for class
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error","gotfocus","init","interactivechange","keypress","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rangehigh","rangelow","rightclick","valid","visiblechanged","when"
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
                "alignment,n,3","anchor,n,0",
                "backcolor,R,255|255|255","backstyle,n,1","bordercolor,R,0","borderwidth,n,1","baseclass,C!,textbox",
                "class,C!,textbox","classlibrary,C!,","Comment,C,","controlsource,c,",
                "enablehyperlinks,l,false","Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9",
                "format,c,","forecolor,R,0",
                "height,n,21",
                "inputmask,c,",
                "left,N,0",
                "margin,n,2","maxlength,n,0",
                "name,c,",
                "originalvalue,,",
                "parent,o!,","parentclass,C!,","passwordchar,c,",
                "readonly,l,false","righttoleft,L,false",
                "sellength,n,0","selstart,n,0","seltext,n,0","selectonentry,l,f","setoriginalwhen,n,0",
                "tabindex,n,1","tabstop,l,.T.","tag,C,","text,c,","top,N,0","tooltiptext,c,",
                "value,C,","visible,l,.T.",
                "width,N,100"
                ];
        }
    }
}
