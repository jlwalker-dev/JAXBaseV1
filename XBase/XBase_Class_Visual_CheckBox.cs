/*
 * CHECKBOX class
 * 
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_CheckBox : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "CheckBox";
        public new string MyDefaultName { get; } = "checkbox";

        public Avalonia.Controls.CheckBox ChkBox => (Avalonia.Controls.CheckBox)me.avaloniaObject!;

        public XBase_Class_Visual_CheckBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.CheckBox(), "Checkbox", "checkbox", true, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // Process named parameters
            foreach (var param in parameterList)
            {
                if (UserProperties.ContainsKey(param.PName.ToLower()))
                {
                    object? propValue = AppHelper.GetParameterValue(param);

                    if (propValue is not null)
                        await SetProperty(param.PName, propValue, 0);
                }
            }

            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            if (InInit)
            {
                ChkBox.IsCheckedChanged += ChkBox_CheckedChanged;
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        /*------------------------------------------------------------------------------------------*
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
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            int h, w;
            bool a, b;

            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;    // Now we can type it easily!
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "autosize":
                            if (tk.Element.Type.Equals("L"))
                            {
                                a = tk.AsBool();
                                h = UserProperties["height"].AsInt();
                                b = UserProperties["wordwrap"].AsBool();
                                w = UserProperties["width"].AsInt();

                                if (b)
                                    SetWordWrap(a, b, h, w);
                                else
                                {
                                    ChkBox.ClearValue(Avalonia.Controls.CheckBox.WidthProperty);
                                    ChkBox.ClearValue(Avalonia.Controls.CheckBox.HeightProperty);
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objValue);
                            //ChkBox.BorderColor = XClass_AuxCode.IntToColor((int)objValue);
                            UserProperties["bordercolor"].Element.Value = objValue;
                            result = 9; // do nothing else
                            break;

                        case "borderwidth":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 15))
                                {
                                    //ChkBox.BorderWidth = tk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "enabled":
                            if (tk.Element.Type.Equals("L"))
                            {
                                ChkBox.IsEnabled = tk.AsBool();
                                //TODO ChkBox.Image = App.JaxImages.GetSDImage(UserProperties[ChkBox.Enabled ? "picture" : "disabledpicure"].AsString(), out _);
                            }
                            else
                                result = 11;
                            break;

                        case "height":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    a = UserProperties["autosize"].AsBool();
                                    h = tk.AsInt();
                                    b = UserProperties["wordwrap"].AsBool();
                                    w = UserProperties["width"].AsInt();

                                    if (b)
                                        SetWordWrap(a, b, h, w);
                                    else
                                        ChkBox.Height = h;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "picture":
                            result = 1999;
                            break;

                        case "pictureposition":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 14))
                                {
                                    objValue = tk.AsInt();

                                    ChkBox.HorizontalContentAlignment = tk.AsInt() switch
                                    {
                                        1 => Avalonia.Layout.HorizontalAlignment.Right,
                                        2 => Avalonia.Layout.HorizontalAlignment.Center,
                                        3 => Avalonia.Layout.HorizontalAlignment.Center,
                                        4 => Avalonia.Layout.HorizontalAlignment.Left,
                                        5 => Avalonia.Layout.HorizontalAlignment.Left,
                                        6 => Avalonia.Layout.HorizontalAlignment.Left,
                                        7 => Avalonia.Layout.HorizontalAlignment.Right,
                                        8 => Avalonia.Layout.HorizontalAlignment.Center,
                                        _ => Avalonia.Layout.HorizontalAlignment.Left
                                    };

                                    ChkBox.VerticalContentAlignment = tk.AsInt() switch
                                    {
                                        1 => Avalonia.Layout.VerticalAlignment.Center,
                                        2 => Avalonia.Layout.VerticalAlignment.Center,
                                        3 => Avalonia.Layout.VerticalAlignment.Top,
                                        4 => Avalonia.Layout.VerticalAlignment.Top,
                                        5 => Avalonia.Layout.VerticalAlignment.Top,
                                        6 => Avalonia.Layout.VerticalAlignment.Bottom,
                                        7 => Avalonia.Layout.VerticalAlignment.Bottom,
                                        8 => Avalonia.Layout.VerticalAlignment.Bottom,
                                        _ => Avalonia.Layout.VerticalAlignment.Center
                                    };

                                    // TODO - images are later (pictueposition property)
                                    //ChkBox.TextImageRelation = tk.AsInt() switch
                                    //{
                                    //    0 => System.Windows.Forms.TextImageRelation.ImageBeforeText,
                                    //    1 => System.Windows.Forms.TextImageRelation.ImageBeforeText,
                                    //    2 => System.Windows.Forms.TextImageRelation.ImageBeforeText,
                                    //    3 => System.Windows.Forms.TextImageRelation.TextBeforeImage,
                                    //    4 => System.Windows.Forms.TextImageRelation.TextBeforeImage,
                                    //    5 => System.Windows.Forms.TextImageRelation.TextBeforeImage,
                                    //    6 => System.Windows.Forms.TextImageRelation.ImageAboveText,
                                    //    7 => System.Windows.Forms.TextImageRelation.ImageAboveText,
                                    //    8 => System.Windows.Forms.TextImageRelation.ImageAboveText,
                                    //    9 => System.Windows.Forms.TextImageRelation.TextAboveImage,
                                    //    10 => System.Windows.Forms.TextImageRelation.TextAboveImage,
                                    //    11 => System.Windows.Forms.TextImageRelation.TextAboveImage,
                                    //    12 => System.Windows.Forms.TextImageRelation.Overlay,
                                    //    13 => System.Windows.Forms.TextImageRelation.ImageBeforeText,
                                    //    _ => System.Windows.Forms.TextImageRelation.Overlay,
                                    //};

                                    // Clear text for option 14
                                    if (tk.AsInt() == 14)
                                        ChkBox.Content = "";
                                    else
                                        ChkBox.Content = UserProperties["caption"].AsString();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;

                        case "width":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    a = UserProperties["autosize"].AsBool();
                                    b = UserProperties["wordwrap"].AsBool();
                                    h = UserProperties["height"].AsInt();
                                    w = tk.AsInt();

                                    if (b)
                                        SetWordWrap(a, b, h, w);
                                    else
                                        ChkBox.Width = w;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "wordwrap":
                            if (tk.Element.Type.Equals("L"))
                            {
                                a = UserProperties["autosize"].AsBool();
                                b = tk.AsBool();
                                h = UserProperties["height"].AsInt();
                                w = Convert.ToInt32(objValue);

                                SetWordWrap(a, b, h, w);
                            }
                            else
                                result = 11;
                            break;

                        case "value":
                            isProgrammaticChange = true;

                            if ("LN".Contains(tk.Element.Type))
                            {
                                ChkBox.IsChecked = tk.Element.Type.Equals("L") ? tk.AsBool() : tk.AsInt() != 0;

                                if ((bool)ChkBox.IsChecked)
                                {
                                    if (tk.Element.Type.Equals("N"))
                                        objValue = 1;
                                    else
                                        objValue = true;
                                }
                                else
                                {
                                    if (tk.Element.Type.Equals("N"))
                                        objValue = 0;
                                    else
                                        objValue = false;
                                }
                            }

                            isProgrammaticChange = false;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
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
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);
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

            // Get the property and fill in the value
            //resultToken.CopyFrom(UserProperties[propertyName]);
            if (UserProperties.ContainsKey(propertyName))
            {
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
                    // First, we double check to make sure that the property exists
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;

                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

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
                "addproperty", "move", "readexpression", "readmethod", "refresh", "resettodefault",
                "saveasclass", "setfocus", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error","gotfocus","init",
                "interactivechagnge","keypress","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rightclick","valid","visiblechanged","when"
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
                "alignment,n,3","anchor,n,0","autosize,L,",
                "backcolor,R,16777215","backstyle,n,1","BaseClass,C!,checkbox","bordercolor,R,0","borderwidth,n,0",
                "caption,c,","centered,L,.F.","Class,C!,checkboxbox","ClassLibrary,C,","Comment,C,","controlsource,c,","checkedpicture,c,",
                "disabledbackcolor,R,15790320","disabledforecolor,R,7171437","disabledpicture,c,","downpicture,c,",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","fontsize,N,9",
                "height,n,21",
                "left,N,0",
                "name,c,checkbox",
                "parent,o!,","parentclass,C!,","picture,c,","pictureposition,n,13",
                "readonly,l,false","righttoleft,L,false",
                "selectonentry,l,f","selectedbackcolor,R,14120960","selectedforecolor,R,16777215",
                "tabindex,n,1","tabstop,l,true","tag,C,","top,N,0","tooltiptext,c,",
                "uncheckedpicture,c,",
                "value,l,.F.","visible,l,true",
                "width,N,0","wordwrap,l,.F."
                ];
        }

        /*
         * Word wrap handler for .Net
         */
        public void SetWordWrap(bool autosize, bool wrap, int h, int w)
        {
            if (wrap)
            {
                // Wrap in the maximum area
                ChkBox.Height = h;
                ChkBox.Width = w;
                ChkBox.MaxWidth = w;
                UserProperties["autosize"].Element.Value = true;
            }
            else
            {
                if (autosize)
                {
                    ChkBox.Height = h;
                    ChkBox.MaxHeight = h;
                    UserProperties["autosize"].Element.Value = true;
                }
                else
                {
                    UserProperties["autosize"].Element.Value = false;
                    ChkBox.Height = h;
                    ChkBox.Width = w;
                }
            }
        }


        // ------------------------------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------------------------------
        private async void ChkBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (isProgrammaticChange)
                await _CallMethod("programmaticchange");
            else
                await _CallMethod("interactivechange");
        }
    }
}

