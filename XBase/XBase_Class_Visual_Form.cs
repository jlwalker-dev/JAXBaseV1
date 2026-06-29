using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.UI;
using JAXBase.Utilities;
using System.ComponentModel;
using System.Windows.Controls;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Form : XBase_Class_Avalonia
    {
        public FakeWindow fakeWindow => (FakeWindow)me.nvObject!;
        public Avalonia.Controls.Canvas InnerCanvas => fakeWindow.ContentCanvas;

        private XBase_Class_Visual_Form? parentForm;  // only used when ShowWindow=1

        public bool windowLocked = false;
        public const double WidthDelta = 4;
        public const double HeightDelta = 50;

        public new string MyBaseClass = "Form";
        public new string MyDefaultName = "form";

        public XBase_Class_Visual_Form(JAXObjectWrapper jow, string defaultname) : base(jow, defaultname)
        {
            // There are several form subclasses in JAXBase and we need to capture
            // the default name and set up the base class accordingly
            string formBase = defaultname.ToLower() switch
            {
                "editform" => "EditForm",
                "browseform" => "BrowseForm",
                "robrowser" => "ROBrowser",
                _ => MyBaseClass
            };

            SetVisualObject(null, formBase, string.IsNullOrEmpty(defaultname) ? MyDefaultName : defaultname, false, UserObject.URW);
            me.nvObject = new FakeWindow();
            me.THISFORM = me;

            // Default canvas setup (will be overridden by FakeWindow later)
            InnerCanvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            InnerCanvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            InnerCanvas.Margin = new Thickness(0);
            InnerCanvas.Background = Avalonia.Media.Brushes.LightCoral;
            InnerCanvas.Name = Program.CurrentApp.SystemCounter();
        }


        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);

            // Deal with datasession handling
            if (UserProperties["datasession"].AsInt() > 1 && UserProperties["datasessionid"].AsInt() < 2)
                UserProperties["datasessionid"].Element.Value = Program.CurrentApp.CreateNewDataSession(Program.CurrentApp.SystemCounter());

            // Add to the _Screen object
            if (result)
            {

            }
            return result;
        }


        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            string msg = string.Empty;

            AppIO.DebugLog($"Adding object '{value.JOWName}' of class '{value.Class}' to form '{me.JOWName}'");

            try
            {
                // Add valid controls to the canvas
                if (value.avaloniaObject is not null)
                    InnerCanvas.Children.Add(value.avaloniaObject!);
                else if (value.nvObject is Avalonia.Controls.Shapes.Path)
                    InnerCanvas.Children.Add((Avalonia.Controls.Shapes.Path)value.nvObject!);
                else if (value.nvObject is not null)
                {
                    // It's something else then add it to the form's objects collection
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                }

            }
            catch
            {
                err = 1980; // generic error code for unexpected exceptions
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
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|{value.JOWName}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token objtk = new() { Element = { Value = objValue } };

            if (InInit == false)
                AppIO.DebugLog($"FORM.{propertyName}={objtk.AsString()}");

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
                                me.originalHeight = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "icon":
                            if (objtk.Element.Type.Equals("C"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    // set up the image and apply it
                                    var icon = string.IsNullOrEmpty(objtk.AsString()) ? null : Program.CurrentApp.JaxImages!.GetImage(objtk.AsString(), out _);
                                    icon ??= Program.CurrentApp.JaxImages!.GetImage("*jax*", out _);

                                    //JAXApp.MainWindowInstance!.Icon = new Avalonia.Controls.WindowIcon(App.JaxImages!.Resize(icon, 32, 32));
                                    fakeWindow.Icon = new Avalonia.Controls.WindowIcon(Program.CurrentApp.JaxImages!.Resize(icon, 32, 32));
                                    fakeWindow.IconBitmap = Program.CurrentApp.JaxImages!.Resize(icon, 32, 32);
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "left":
                            fakeWindow.Left = objtk.AsDouble();
                            me.originalLeft = objtk.AsDouble();
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

                        case "name":
                            if (objtk.Element.Type == "C")
                                me.SetName(objtk.AsString());
                            else
                                result = 41;
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
                            me.originalTop = objtk.AsDouble();
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
                                me.originalWidth = objtk.AsDouble();
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}||{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
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
                            AppIO.DebugLog($"ShowWindow=1 but no parent set — cannot show nested form {UserProperties["name"].AsString()}");
                            return 9701;
                        }

                        if (!parentForm.fakeWindow.IsShown)
                        {
                            AppIO.DebugLog("Parent not yet shown — showing parent first");
                            await parentForm.DoDefault("show"); // recursive, but safe
                        }
                    }

                    AppIO.DebugLog($"'{UserProperties["objects"].Col}' objects in {me.JOWName}");
                    fakeWindow.VFPShow();

                    switch (fakeWindow.ShowWindow)
                    {
                        case 0:  // Main workspace / FloatingPanel
                        case 1:  // Nested panel
                            me.ParentAvaloniaWindow = fakeWindow.ContentCanvas;           // or the FloatingPanel itself
                            break;

                        case 2:  // Independent real Window
                            me.ParentAvaloniaWindow = fakeWindow._realWindow;             // private, but you can expose it
                                                                                          // or better:
                                                                                          // visualForDialogs = (Avalonia.Visual)fakeWindow._realWindow;
                            break;
                    }

                    me.avaloniaObject = fakeWindow.ContentCanvas;
                    SetEvents();

                    FixObjects(me);

                    InnerCanvas.IsVisible = true;
                    AppIO.DebugLog($"Form {UserProperties["name"].AsString()} shown via FakeWindow");
                    break;

                case "hide":
                    fakeWindow.VFPHide();
                    InnerCanvas.IsVisible = false;
                    break;

                case "queryunload":
                    break;

                case "destroy":
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }

        private void EditFormCanvas_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (sender is not null)
                UpdateGridSizeToCanvas((Avalonia.Controls.Canvas)sender!);
        }

        private void UpdateGridSizeToCanvas(Avalonia.Controls.Canvas _canvas)
        {
            if (_canvas.Children.Count > 0 && _canvas.Children[0] is Avalonia.Controls.Grid _grid)
            {
                _grid.Width = _canvas.Bounds.Width;
                _grid.Height = _canvas.Bounds.Height;

                // Optional: force immediate re-layout
                _grid.InvalidateMeasure();
                _grid.InvalidateArrange();
            }
        }

        // Recursive method to walk through all child objects and re-apply them to ensure they
        // are correctly parented after the form's visual tree has been moved to the FakeWindow
        // Also reapplies MenuItem hotkeys to ensure they are registered correctly after the move
        private void FixObjects(JAXObjectWrapper thisJOW)
        {
            // Re-apply on the form's own children (the label and any other visual objects)
            JAXObjects.Token objects = new();
            objects = thisJOW.thisObject!.UserProperties["objects"];

            AppIO.DebugLog($">>> FixObjects for {thisJOW.JOWName} - baseclass {thisJOW.BaseClass}");

            if (JAXLib.InListC(thisJOW.BaseClass, "editform", "robrowser"))
            {
                // EditForm has one child which is a grid.  This routine ties the grid
                // to the form and sets a resize event to keep it sized correctly.
                XBase_Class_Visual_Form form = (XBase_Class_Visual_Form)thisJOW.thisObject;

                Avalonia.Controls.Control _grid = (Avalonia.Controls.Grid)form.InnerCanvas.Children[0];

                if (thisJOW.BaseClass.Equals("robrowser", StringComparison.OrdinalIgnoreCase))
                {
                    if (_grid is Avalonia.Controls.Grid layoutGrid)
                    {
                        if (layoutGrid.Children.Count > 0)
                        {
                            Avalonia.Controls.DataGrid? _dataGrid = layoutGrid.Children[0] as Avalonia.Controls.DataGrid;

                            if (_dataGrid is not null)
                            {
                                // Force column regeneration and refresh
                                _dataGrid.AutoGenerateColumns = false;
                                _dataGrid.Columns.Clear();
                                _dataGrid.AutoGenerateColumns = true;

                                _dataGrid.InvalidateVisual();
                                _dataGrid.InvalidateMeasure();
                                _dataGrid.InvalidateArrange();

                                // Additional layout refresh for Canvas scenario
                                if (InnerCanvas != null)
                                {
                                    InnerCanvas.InvalidateVisual();
                                    InnerCanvas.InvalidateMeasure();
                                    InnerCanvas.InvalidateArrange();
                                }
                            }
                            else
                                throw new Exception("9999|");
                        }
                    }
                }

                Avalonia.Controls.Canvas.SetLeft(_grid, 0);
                Avalonia.Controls.Canvas.SetTop(_grid, 0);

                UpdateGridSizeToCanvas(form.InnerCanvas);

                // Subscribe to resize events
                form.InnerCanvas.SizeChanged += EditFormCanvas_SizeChanged;
            }
            else
            {
                for (int i = 0; i < objects._avalue.Count; i++)
                {
                    if (objects._avalue[i].IsNull() == false)
                    {
                        JAXObjectWrapper childWrapper = (JAXObjectWrapper)objects._avalue[i].Value;
                        AppIO.DebugLog($"   Reapply Position for {childWrapper.JOWName} - baseclass {childWrapper.BaseClass} is {(childWrapper.thisObject is XBase_Class_Avalonia ? "" : "NOT ")}an AvaloniaObject ");

                        // This ensures inline CREATEOBJECT left/top values survive the final move to InnerCanvas
                        if (childWrapper.thisObject is XBase_Class_Avalonia childVisual)
                            childVisual.ReapplyPosition(childWrapper).Wait();

                        if (childWrapper.BaseClass.Equals("menu", StringComparison.OrdinalIgnoreCase))
                        {
                            // Walk through all MenuItems and re-apply HotKeys to ensure they are registered
                            // after the dynamic canvas move
                            Avalonia.Controls.Menu _menu = (Avalonia.Controls.Menu)childWrapper.thisObject!;

                            foreach (var menuItem in GetAllMenuItems(_menu))
                            {
                                if (menuItem.HotKey != null)
                                {
                                    var gesture = menuItem.HotKey;           // capture current gesture
                                    menuItem.HotKey = null;                  // clear first (forces re-register in some cases)
                                    HotKeyManager.SetHotKey(menuItem, gesture);
                                }
                            }
                        }
                        else if (JAXLib.InListC(childWrapper.BaseClass, "form", "container", "pageframe", "page")) //, "commandgroup", "optiongroup"))
                            FixObjects((JAXObjectWrapper)objects._avalue[i].Value);  // recursive call for nested containers);
                    }
                }
            }
        }


        private IEnumerable<Avalonia.Controls.MenuItem> GetAllMenuItems(Avalonia.Controls.Menu menu)
        {
            var items = new List<Avalonia.Controls.MenuItem>();
            foreach (var item in menu.Items.OfType<Avalonia.Controls.MenuItem>())
            {
                items.Add(item);
                // Recurse into submenus if needed
                if (item.Items != null)
                {
                    foreach (var subItem in item.Items.OfType<Avalonia.Controls.MenuItem>())
                    {
                        items.Add(subItem);
                    }
                }
            }
            return items;
        }

        // ------------------------------------------------------------------------
        // New public method — call this before Show() when using ShowWindow=1
        // ------------------------------------------------------------------------
        public void SetParentForm(XBase_Class_Visual_Form parent)
        {
            if (fakeWindow.ShowWindow != 1)
            {
                AppIO.DebugLog("SetParentForm called but ShowWindow != 1 — ignored");
                return;
            }

            parentForm = parent;
            fakeWindow.Parent = parent.fakeWindow;
            AppIO.DebugLog($"Form {me.thisObject?.UserProperties["name"].AsString()} nested inside parent '{parent.me.thisObject?.UserProperties["name"].AsString()}'");
        }

        // FAKE WINDOW Events
        public override void SetEvents()
        {
            base.SetEvents();

            if (fakeWindow._realWindow != null)
            {
                fakeWindow._realWindow.Closing += MyObj_Closing;
                fakeWindow._realWindow.Closed += MyObj_Closed;
            }
            else
            {
                // Wire FakeWindow events
                fakeWindow.Closing += FakeWindow_Closing;
                fakeWindow.Closed += FakeWindow_Closed;
            }
        }

        public override void SuspendEvents()
        {
            fakeWindow.Closing -= FakeWindow_Closing;
            fakeWindow.Closed -= FakeWindow_Closed;

            base.SuspendEvents();
        }

        private void FakeWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("queryunload"))
            {
                // JAX code can set ReturnValue = .F. to cancel
                _CallMethod("queryunload").Wait();

                if (Program.CurrentApp.ReturnValue.Element.Type.Equals("L") && Program.CurrentApp.ReturnValue.AsBool() == false)
                {
                    e.Cancel = true;
                }
            }
        }

        private void FakeWindow_Closed(object? sender, EventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("destroy"))
                _CallMethod("destroy").Wait();

            // Optional: auto-cleanup
            Dispose();
        }

        public override string[] JAXMethods()
        {
            return ["addobject", "addproperty", "move", "readexpression", "readmethod", "refresh", "release",
                    "removeobject", "resettodefault", "saveas", "saveasclass", "setall", "setfocus", "setmousepointer",
                    "show", "writeexpression", "writemethod", "zorder"];
        }

        public override string[] JAXEvents()
        {
            return
            [
                "activate","click","dblclick","deactivate","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "moved","queryunload","resize","rightclick","scrolled","unload","visiblechanged"
            ];
        }

        public override string[] JAXProperties()
        {
            return
            [
                "activecontrol,N!,0","alwaysontop,L,false", "autocenter,L,false",
                "backcolor,R,255|255|255","baseclass,C!,form","bindcontrols,L,true","borderstyle,N,3",
                "caption,C,Form","class,C!,Form","classlibrary,C!,","closable,L,true","comment,C,","controlbox,L,true","controlcount,N!,0",
                "datasession,N,1","datasessionid,N!,1",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","FontSize,N,12","forecolor,R,0",
                "Height,N,300",
                "icon,C,*jax*",
                "keypreview,L,false",
                "left,N,0","lockscreen,L,false",
                "maxbutton,L,true","maxheight,N,-1","maxwidth,N,-1","minbutton,L,true","minheight,N,-1","minwidth,N,-1","mousepointer,n,0","moveable,L,true",
                "name,C,form",
                "objects,*,",
                "parent,o!$,","parentclass,C!$,","picture,C,",
                "righttoleft,L,false",
                "scalefactor,N,0","scrollbars,n,0","showintaskbar,L,.T.","showwindow,N,0",
                "tabindex,N,1","tabstop,L,true","tag,C,","top,N,0","tooltiptext,c,",
                "visible,L,true",
                "width,N,300","windowstate,N,0","windowtype,N,0"
            ];
        }
    }
}