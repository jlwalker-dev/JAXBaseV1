using Avalonia;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.UI;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Form : XBase_Class_Avalonia
    {
        public FakeWindow fakeWindow => (FakeWindow)me.nvObject!;
        public Avalonia.Controls.Canvas InnerCanvas => fakeWindow.ContentCanvas;

        private XBase_Class_Visual_Form? parentForm;  // only used when ShowWindow=1

        private bool windowLocked = false;
        private string MainMenuName = string.Empty;
        private Avalonia.Controls.Menu mainMenu = new();

        private const double WidthDelta = 4;
        private const double HeightDelta = 50;

        public XBase_Class_Visual_Form(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "Form", "form", false, UserObject.URW);
            me.nvObject = new FakeWindow();
            me.THISFORM = me;

            // Default canvas setup (will be overridden by FakeWindow later)
            InnerCanvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            InnerCanvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            InnerCanvas.Margin = new Thickness(0);
            InnerCanvas.Background = Avalonia.Media.Brushes.White;
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

            // Final setup — most moved to SetProperty now
            // Datasession handling remains here
            if (UserProperties["datasession"].AsInt() > 1 && UserProperties["datasessionid"].AsInt() < 2)
                UserProperties["datasessionid"].Element.Value = App.CreateNewDataSession(App.SystemCounter());

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }


        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;

            // Use the real/live canvas from FakeWindow
            //var targetCanvas = InnerCanvas;

            // Optional diagnostic: warn if adding before shown (content may not appear until show)
            //if (!fakeWindow.IsShown)  // ← assumes you add public bool IsShown => _isShown; to FakeWindow
            //{
            //    App.DebugLog($"Warning: AddObject called before form is shown. Controls added to canvas but may not be visible until VFPShow().");
            //}

            if (value.avaloniaObject is not null)
            {
                InnerCanvas.Children.Add(value.avaloniaObject!);

                if ((await value.IsMember("anchor")).Equals("P"))
                {
                    JAXObjects.Token answer = await value.GetProperty("anchor");
                    XClass_AuxCode.ApplyVFPAnchor(value.avaloniaObject!, InnerCanvas, answer.AsInt());
                }
            }
            else if (value.nvObject is Avalonia.Controls.Shapes.Path path)
            {
                InnerCanvas.Children.Add(path);

                if ((await value.IsMember("anchor")).Equals("P"))
                {
                    JAXObjects.Token answer = await value.GetProperty("anchor");
                    XClass_AuxCode.ApplyVFPAnchor(path, InnerCanvas, answer.AsInt());
                }
            }

            if (err == 0)
            {
                UserProperties["objects"].Add(value);
                UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;

                if (value.thisObject is not null)
                    value.SetParent(me);
            }
            else
            {
                _AddError(err, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(err, $"{err}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            //App.DebugLog($"========> Added {value.thisObject?.UserProperties["name"].AsString()} to InnerCanvas with children count: {fakeWindow.ContentCanvas.Children.Count}");

            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token objtk = new() { Element = { Value = objValue } };

            App.DebugLog($"FORM.{propertyName}={objtk.AsString()}");

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "autocenter":
                            if (objtk.Element.Type == "L")
                                fakeWindow.AutoCenter = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "backcolor":
                            int colorInt = JAXUtilities.ReturnColorInt(objtk.AsString());
                            InnerCanvas.Background = new SolidColorBrush(XClass_AuxCode.IntToAvColor(colorInt));
                            objValue = colorInt;
                            break;

                        case "borderstyle":
                            if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 3))
                                fakeWindow.BorderStyle = objtk.AsInt();
                            else
                                result = objtk.Element.Type != "N" ? 11 : 41;
                            break;

                        case "caption":
                            fakeWindow.Title = objtk.AsString();
                            break;

                        case "height":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Height = objtk.AsDouble() + HeightDelta;
                                objValue = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "left":
                            fakeWindow.Left = objtk.AsDouble();
                            break;

                        case "maxbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MaxButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "minbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MinButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "maxheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxHeight = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinHeight = 0;
                                else
                                    fakeWindow.MinHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "maxwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxWidth = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinWidth = 0;
                                else
                                    fakeWindow.MinWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "showwindow":
                            if (windowLocked)
                                result = 9702;
                            else if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 2))
                                fakeWindow.ShowWindow = objtk.AsInt();
                            else
                                result = 41;
                            break;

                        case "top":
                            fakeWindow.Top = objtk.AsDouble();
                            break;

                        case "visible":
                            if (objtk.Element.Type == "L")
                            {
                                // Visibility handled in Show/Hide
                                if (objtk.AsBool() && !InInit)
                                    windowLocked = true;
                            }
                            else
                                result = 11;
                            break;

                        case "width":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Width = objtk.AsDouble() + WidthDelta;
                                objValue = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "windowstate":
                            if (objtk.Element.Type == "N")
                            {
                                int vfpState = objtk.AsInt();
                                fakeWindow.WindowState = vfpState switch
                                {
                                    1 => Avalonia.Controls.WindowState.Minimized,
                                    2 => Avalonia.Controls.WindowState.Maximized,
                                    _ => Avalonia.Controls.WindowState.Normal   // 0 or invalid → Normal
                                };
                                objValue = vfpState;
                            }
                            else
                                result = 11;  // type mismatch
                            break;
                    }

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
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }

            return result;
        }

        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            propertyName = propertyName.ToLower();
            JAXObjects.Token returnToken = new();
            int result = 0;

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "height":
                        returnToken.Element.Value = fakeWindow.Height - HeightDelta;
                        break;

                    case "width":
                        returnToken.Element.Value = fakeWindow.Width - WidthDelta;
                        break;

                    case "left":
                        returnToken.Element.Value = fakeWindow.Left;
                        break;

                    case "top":
                        returnToken.Element.Value = fakeWindow.Top;
                        break;

                    case "showwindow":
                        returnToken.Element.Value = fakeWindow.ShowWindow;
                        break;

                    case "windowstate":
                        int vfpState = fakeWindow.WindowState switch
                        {
                            Avalonia.Controls.WindowState.Minimized => 1,
                            Avalonia.Controls.WindowState.Maximized => 2,
                            _ => 0
                        };
                        returnToken.Element.Value = vfpState;
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]);
                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}||{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                returnToken.Element.MakeNull();
            }

            return returnToken;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower();

            switch (methodName)
            {
                case "show":
                    if (fakeWindow.ShowWindow == 1)
                    {
                        if (parentForm == null || fakeWindow.Parent == null)
                        {
                            // TODO - add error reporting here to capture name for dialog
                            App.DebugLog($"ShowWindow=1 but no parent set — cannot show nested form {UserProperties["name"].AsString()}");
                            return 9701;
                        }

                        if (!parentForm.fakeWindow.IsShown)
                        {
                            App.DebugLog("Parent not yet shown — showing parent first");
                            await parentForm.DoDefault("show"); // recursive, but safe
                        }
                    }

                    fakeWindow.VFPShow();

                    InnerCanvas.IsVisible = true;
                    App.DebugLog($"Form {UserProperties["name"].AsString()} shown via FakeWindow");
                    break;

                case "hide":
                    fakeWindow.VFPHide();
                    InnerCanvas.IsVisible = false;
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }


        // ------------------------------------------------------------------------
        // New public method — call this before Show() when using ShowWindow=1
        // ------------------------------------------------------------------------
        public void SetParentForm(XBase_Class_Visual_Form parent)
        {
            if (fakeWindow.ShowWindow != 1)
            {
                App.DebugLog("SetParentForm called but ShowWindow != 1 — ignored");
                return;
            }

            parentForm = parent;
            fakeWindow.Parent = parent.fakeWindow;
            App.DebugLog($"Form {me.thisObject?.UserProperties["name"].AsString()} nested inside parent '{parent.me.thisObject?.UserProperties["name"].AsString()}'");
        }


        public override string[] JAXMethods()
        {
            return ["addobject", "addproperty", "box", "circle", "cls", "dock", "draw", "getdockstate", "hide", "line",
                    "move", "newobject", "pset", "point", "print", "readexpression", "readmethod", "refresh", "release",
                    "removeobject", "resettodefault", "saveas", "saveasclass", "setall", "setfocus", "setmousepointer",
                    "setviewport", "show", "showwhatsthis", "textheight", "textwidth", "whatsthismode", "writeexpression",
                    "writemethod", "zorder"];
        }

        public override string[] JAXEvents()
        {
            return
            [
                "activate","afterdock","beforedock","click","dblclick","dblrightclick","deactivate","destroy","dragdrop","dragover","error",
                "gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "moved","paint","queryunload","resize","rightclick","scrolled","undock","unload","visiblechanged"
            ];
        }

        public override string[] JAXProperties()
        {
            return
            [
                "activecontrol,N!,0","alwaysontop,L,false",
                "autocenter,L,false",
                "backcolor,R,255|255|255","bindcontrols,L,true","bordercolor,R,0","borderstyle,N,3","borderwidth,n,0","baseclass,C!,form",
                "class,C!,Form","caption,C,Form","classlibrary,C!$,","closable,L,true","comment,C,","controlbox,L,true","controlcount,N!,0",
                "datasession,N,1","datasessionid,N!,1",
                "Enabled,L,true",
                "FontBold,L,false",
                "FontItalic,L,false","FontName,C,Arial","FontSize,N,9",
                "FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0",
                "Height,N,300",
                "icon,C,",
                "keypreview,L,false",
                "left,N,0","lockscreen,L,false",
                "maxbutton,L,true","maxheight,N,-1","maxwidth,N,-1","minbutton,L,true",
                "minheight,N,-1","minwidth,N,-1","mousepointer,n,0","moveable,L,true",
                "name,C,form",
                "objects,*,",
                "parent,o!$,","parentclass,C!$,","picture,C,",
                "righttoleft,L,false",
                "shownintaskbar,L,true","showwindow,N,0",
                "tag,C,","tabindex,N,1","tabstop,L,true","top,N,0","tooltiptext,c,",
                "visible,L,true",
                "width,N,300","windowstate,N,0","windowtype,N,0"
            ];
        }
    }
}