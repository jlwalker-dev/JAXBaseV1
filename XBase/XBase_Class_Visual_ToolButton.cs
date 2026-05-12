/*
 * The toolbutton is basically a commandbutton with fewer
 * properties, methods, and events.
 * 
 * Events tie into the toolbar.
 * 
 */
using Avalonia.Input;
using Avalonia.Interactivity;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ToolButton : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "ToolButton";
        public new string MyDefaultName { get; } = "toolbutton";


        public Avalonia.Controls.Button btn => (Avalonia.Controls.Button)me.avaloniaObject!;
        Avalonia.Media.Imaging.Bitmap? bitMap = null;

        public XBase_Class_Visual_ToolButton(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Button(), "Toolbutton", string.IsNullOrWhiteSpace(name) ? MyDefaultName : name, true, UserObject.urw);
            SetPrivateProperty("picname", "");
            SetPrivateProperty("picdiabled", "");
            SetPrivateProperty("picdown", "");

            // Final default setup of button properties
            btn.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            btn.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
            btn.Padding = new Avalonia.Thickness(2);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
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

                        case "caption":
                            if (tk.Element.Type.Equals("C"))
                                btn.Content = tk.AsString();
                            else
                                result = 11;
                            break;

                        case "disabledpicture":
                            if (InInit == false && string.IsNullOrWhiteSpace(tk.AsString()) == false)
                            {
                                App.JaxImages!.RegisterImage(tk.AsString(), "", out string imageName);

                                if (string.IsNullOrWhiteSpace(imageName) == false)
                                    PrivateProperties["picdisabled"].Element.Value = imageName;
                            }
                            break;

                        case "downpicture":
                            if (InInit == false && string.IsNullOrWhiteSpace(tk.AsString()) == false)
                            {
                                App.JaxImages!.RegisterImage(tk.AsString(), "", out string imageName);

                                if (string.IsNullOrWhiteSpace(imageName) == false)
                                    PrivateProperties["picdown"].Element.Value = imageName;
                            }
                            break;

                        case "picture":
                            if (string.IsNullOrWhiteSpace(tk.AsString()) == false)
                            {
                                if (InInit == false)
                                {
                                    string imageName;

                                    if (App.JaxImages!.HasImage(tk.AsString()))
                                        imageName = tk.AsString();
                                    else
                                        App.JaxImages!.RegisterImage(tk.AsString(), "", out imageName);

                                    if (string.IsNullOrWhiteSpace(imageName) == false)
                                    {
                                        PrivateProperties["picname"].Element.Value = imageName;

                                        w = UserProperties["width"].AsInt() - 2;
                                        w = w < 16 ? 16 : w;
                                        h = UserProperties["height"].AsInt() - 2;
                                        h = h < 16 ? 16 : h;
                                        bitMap = App.JaxImages.Resize(App.JaxImages.GetImage(imageName, out _), w, h);
                                        Avalonia.Controls.Image img = new() { Source = bitMap, Stretch = Avalonia.Media.Stretch.Uniform };
                                        btn.Content = img;
                                    }

                                    btn.BorderThickness = new Avalonia.Thickness(UserProperties["borderwidth"].AsInt());
                                    btn.IsVisible = true;
                                }
                            }
                            else
                            {
                                btn.Content = null;
                                btn.BorderThickness = new Avalonia.Thickness(0);
                                btn.IsVisible = false;
                            }
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
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }


        /*
         * If this is a child of a tool bar:
         * Record which button was clicked to the toolbar Value property; this
         * happens before anything else.  Then call the click method and if
         * you get a true (.T.) value return, call the toolbar Valid method.
         */
        //public override async void MyObj_Click(object? sender, RoutedEventArgs e)
        public override async void HandleTapped(object? sender, TappedEventArgs e)
        {
            if (me.parent is not null && me.parent.thisObject is not null && me.parent.BaseClass.Equals("toolbar", StringComparison.OrdinalIgnoreCase))
            {
                // Record the button press to the toolbar
                me.parent.thisObject.UserProperties["value"].Element.Value = UserProperties["value"].AsInt();

                // Call the toolbutton click
                await me.MethodCall("click");

                // If a return value of true, then call the toolbar valid
                if (App.ReturnValue.AsBool())
                    await me.parent.MethodCall("valid");
            }
            else 
                base.HandleTapped(sender, e);
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
                "click","destroy","error","gotfocus","init","lostfocus","visiblechanged","when"
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
                "alignment,n,2",
                "backcolor,R,16777215","BaseClass,C!,toolbutton","bordercolor,R,0","borderwidth,n,2",
                "caption,c,","Class,C!,toolbutton","ClassLibrary,C!,",
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
                "width,N,40",
                ];
        }
    }
}
