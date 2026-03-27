using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_CommandButton : XBase_Class_Avalonia
    {
        public Avalonia.Controls.Button btn => (Avalonia.Controls.Button)me.avaloniaObject!;
        Avalonia.Media.Imaging.Bitmap? bitMap = null;

        public XBase_Class_Visual_CommandButton(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            string objType = name.Equals("toolbutton", StringComparison.OrdinalIgnoreCase) ? name : "commandbutton";
            SetVisualObject(new Avalonia.Controls.Button(), objType, "command", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // Process named parameters
            foreach (var param in parameterList)
            {
                if (UserProperties.ContainsKey(param.PName.ToLower()))
                {
                    object? propValue = App.GetParameterValue(param);

                    if (propValue is not null)
                        await SetProperty(param.PName, propValue, 0);
                }
            }


            // ----------------------------------------
            // Final setup
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        // Cleanup for IDispose
        public override void CleanUp(bool disposing)
        {
            base.CleanUp(disposing);

            if (disposing)
            {
                // Clean up managed resources
                // Break reference to bitmap before disposing.
                if (btn.Content is Avalonia.Controls.Image img)
                {
                    img.Source = null;
                }

                bitMap?.Dispose();
                bitMap = null;
            }
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

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "alignment":
                            if (JAXLib.Between(tk.AsInt(), 0, 14))
                            {
                                objValue = tk.AsInt();

                                btn.HorizontalContentAlignment = tk.AsInt() switch
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

                                btn.VerticalContentAlignment = tk.AsInt() switch
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
                            }
                            break;

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
                                    btn.ClearValue(Avalonia.Controls.Button.WidthProperty);
                                    btn.ClearValue(Avalonia.Controls.Button.HeightProperty);
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "caption":
                            if (tk.Element.Type.Equals("C"))
                                btn.Content = SetHotKey(tk.AsString());
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
                                        btn.Height = h;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "picture":
                            if (string.IsNullOrWhiteSpace(tk.AsString()) == false)
                            {
                                string imageName = "";
                                App.JaxImages?.RegisterImage(tk.AsString(), "", out imageName);

                                if (string.IsNullOrWhiteSpace(imageName) == false)
                                {
                                    PrivateProperties["picname"].Element.Value = imageName;

                                    w = UserProperties["width"].AsInt() - 2;
                                    w = w < 16 ? 16 : w;
                                    h = UserProperties["height"].AsInt() - 2;
                                    h = h < 16 ? 16 : h;
                                    bitMap = App.JaxImages!.Resize(App.JaxImages.GetImage(imageName, out _), w, h);
                                    Avalonia.Controls.Image img = new() { Source = bitMap, Stretch = Avalonia.Media.Stretch.Uniform };
                                    btn.Content = img;
                                }
                            }
                            else
                            {
                                btn.Content = null;
                            }
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
                                        btn.Width = w;
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
                            UserProperties[propertyName].Element.Value = objValue;
                        }

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{propertyName}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }


        /*
         * Word wrap handler for .Net
         */
        public void SetWordWrap(bool autosize, bool wrap, int h, int w)
        {
            if (wrap)
            {
                if (autosize)
                {
                    btn.ClearValue(Avalonia.Controls.Button.WidthProperty);
                    btn.ClearValue(Avalonia.Controls.Button.HeightProperty);
                }
                else
                {
                    // TODO
                    // Text that auto-wraps
                    //var wrappingText = new TextBlock
                    //{
                    //    Text = "This is a long button label that needs to wrap automatically to multiple lines when it doesn't fit on one line.",
                    //    TextWrapping = TextWrapping.Wrap,              // ← This enables auto-wrap
                    //    TextAlignment = TextAlignment.Center,
                    //    VerticalAlignment = VerticalAlignment.Center
                    //};

                    //// Assign the TextBlock as content
                    //button.Content = wrappingText;
                }
            }
            else
            {
                if (autosize)
                {
                    btn.ClearValue(Avalonia.Controls.Button.WidthProperty);
                    btn.ClearValue(Avalonia.Controls.Button.HeightProperty);
                    UserProperties["autosize"].Element.Value = true;
                }
                else
                {
                    UserProperties["autosize"].Element.Value = false;
                    btn.Height = h;
                    btn.Width = w;
                }
            }
        }



        // Set the caption and hotkey for the button
        private string SetHotKey(string caption)
        {
            string result = caption;
            char underlineLetter = '\0';

            // get the xbase signal for a hot key
            int index = caption.IndexOf("\\<");
            if (index >= 0)
            {
                // pull the hotkey flag and mark the position
                caption = caption.Replace("\\<", "");
                underlineLetter = caption[index];
            }

            // We use a TextBlock to set up the content for the button
            TextBlock textBlock = new TextBlock()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = UserProperties["fontsize"].AsInt()
            };

            if (index >= 0)
            {
                // Text before underline
                if (index > 0)
                    textBlock.Inlines!.Add(new Avalonia.Controls.Documents.Run(caption.Substring(0, index)));

                // Underlined letter
                var underlinedRun = new Avalonia.Controls.Documents.Run(caption.Substring(index, 1))
                {
                    TextDecorations = Avalonia.Media.TextDecorations.Underline
                };
                textBlock.Inlines!.Add(underlinedRun);

                // Text after
                if (index + 1 < caption.Length)
                    textBlock.Inlines.Add(new Avalonia.Controls.Documents.Run(caption.Substring(index + 1)));
            }
            else
            {
                // No match → just plain text
                textBlock.Text = caption;
            }

            btn.Content = textBlock;

            // Set the Alt + letter hotkey
            if (index >= 0)
            {
                var key = Char.ToUpperInvariant(underlineLetter);
                var gesture = new KeyGesture((Key)key, KeyModifiers.Alt);
                HotKeyManager.SetHotKey(btn, gesture);
            }

            return result;
        }

        /*
         * If this is a child of a command group:
         * Record which button was clicked to the toolbar Value property; this
         * happens before anything else.  Then call the click method and if
         * you get a true (.T.) value return, call the toolbar Valid method.
         */
        public override async void MyObj_Click(object? sender, RoutedEventArgs e)
        {
            if (me.parent is not null && me.parent.thisObject is not null && me.parent.BaseClass.Equals("commandgroup", StringComparison.OrdinalIgnoreCase))
            {
                // Record the button press to the toolbar
                me.parent.thisObject.UserProperties["value"].Element.Value = UserProperties["value"].AsInt();

                // Call the toolbutton click
                await me.MethodCall("click");

                // If a return value of true, then call the commandgroup valid
                if (App.ReturnValue.AsBool())
                    await me.parent.MethodCall("valid");
            }
            else
                base.MyObj_Click(sender, e);
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","move","readexpression","readmethod","refresh","resettodefault",
                "saveasclass","settooriginalvalue","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","destroy","error","gotfocus",
                "init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","valid","visiblechanged","when"
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
                "alignment,n,2","anchor,n,0","autosize,l,false",
                "backcolor,R,16777215","BaseClass,C!,commandbutton","bordercolor,R,0","borderwidth,n,2",
                "caption,c,Option1","Class,C!,commandbutton","ClassLibrary,C!,",
                "Comment,C,",
                "disabledpicture,c,","downpicture,c,",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forcolor,R,0",
                "Height,N,40",
                "left,N,0",
                "name,c,command",
                "originalvalue,N,",
                "parent,o!,","parentclass,C!,","picture,c,","picturemargin,n,0","pictureposition,n,13","picturespacing,n,0",
                "righttoleft,L,false",
                "setoriginalwhen,n,0",
                "tabindex,n,1","tabstop,l,true","tag,C,","tooltiptext,c,",
                "top,N,0",
                "value,N,1","visible,l,true",
                "width,N,100","wordwrap,l,false"
                ];
        }
    }
}
