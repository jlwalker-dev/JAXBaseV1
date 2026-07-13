/*
 * Screen class for _Screens environment object
 * 
 * 2026.06.09 - JLW
 *      Add DoDefault()
 *      Clean up properties
 *      
 */
using Avalonia;
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Screen : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "Screen";
        public new string MyDefaultName = "screen";
        public new bool Register = false;

        // This list holds the row source array followed by important related values
        public ObservableSortedDictionary<int, JAXObjects.Token> Screens = [];

        public XBase_Class_Screen(JAXObjectWrapper jow, string name) : base(jow, "Screen")
        {
            SetVisualObject(null, MyBaseClass, string.IsNullOrWhiteSpace(name) ? "_screen" : name, false, UserObject.URW);
            me.nvObject = new EmptyFactory();   // JAXApp.MainWindowInstance;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            string forms = JAXLanguageLists.GetWord("forms", "REVPEMS");
            UserProperties.Add(forms, new(""));
            UserProperties[forms] = new(Screens, "M") // Mapped List of Dictionary
            {
                Protected = true
            };

            return result;
        }

        /* ------------------------------------------------------------------------------------------*
         * GetProperty
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();

            propertyName = propertyName.ToLower();
            string EnglishPropertyName = Program.CurrentApp.ActiveLanguagePack.PEMs.TryGetValue(propertyName, out string? p) ? p : propertyName;

            if (UserProperties.ContainsKey(propertyName))
            {

                switch (EnglishPropertyName)
                {
                    case "activecontrol":
                        returnToken = UserProperties["activecontrol"];
                        break;

                    case "activeform":
                        returnToken.Element = UserProperties["forms"]._avalue[UserProperties[JAXLanguageLists.GetWord("activeform", "REVPEMS")].AsInt()];
                        break;

                    case "commandwindowheight":
                        if (JAXApp.MainWindowInstance is null)
                            returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = JAXApp.MainWindowInstance.commandWindow.Height;
                        break;

                    case "commandwindowleft":
                        if (JAXApp.MainWindowInstance is null)
                            returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = JAXApp.MainWindowInstance.commandWindow.Bounds.Left;
                        break;

                    case "commandwindowtop":
                        if (JAXApp.MainWindowInstance is null)
                            returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = JAXApp.MainWindowInstance.commandWindow.Bounds.Top;
                        break;

                    case "commandwindowwidth":
                        if (JAXApp.MainWindowInstance is null)
                            returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = JAXApp.MainWindowInstance.commandWindow.Width;
                        break;

                    case "forms":
                        if (JAXLib.Between(idx, 1, UserProperties["forms"]._avalue.Count))
                        {
                            returnToken.Element = UserProperties["forms"]._avalue[idx - 1];
                        }
                        else
                            result = 41;
                        break;

                    case "height":
                        returnToken.Element.Value = JAXApp.MainWindowInstance is null ? 0 : JAXApp.MainWindowInstance.Height; // TODO - delta or no?
                        break;

                    case "left":
                        returnToken.Element.Value = JAXApp.MainWindowInstance is null ? 0 : JAXApp.MainWindowInstance.Bounds.Left;
                        break;

                    case "monitor":
                        returnToken.Element.Value = 1;
                        break;

                    case "monitortop":
                        returnToken.Element.Value = MonitorLib.GetScreenInfo(JAXApp.MainWindowInstance!, JAXLanguageLists.GetWord("top","REVPEMS"));
                        break;

                    case "monitorleft":
                        returnToken.Element.Value = MonitorLib.GetScreenInfo(JAXApp.MainWindowInstance!, me.cPropLeft);
                        break;

                    case "top":
                        if (JAXApp.MainWindowInstance is not null)
                            returnToken.Element.Value = JAXApp.MainWindowInstance is null ? 0 : JAXApp.MainWindowInstance.Bounds.Top;
                        else
                            returnToken.Element.Value = 0;
                        break;

                    case "width":
                        if (JAXApp.MainWindowInstance is not null)
                            returnToken.Element.Value = JAXApp.MainWindowInstance is null ? 0 : JAXApp.MainWindowInstance.Width;
                        else
                            returnToken.Element.Value = 0;
                        break;

                    default:
                        result = 1;
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /* ------------------------------------------------------------------------------------------*
         * SetProperty
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Success, do no further processing
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
            string EnglishPropertyName = Program.CurrentApp.ActiveLanguagePack.PEMs.TryGetValue(propertyName, out string? p) ? p : propertyName;
            
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                {
                    result = 3026;
                }
                else
                {
                    switch (EnglishPropertyName)
                    {
                        case "alwaysontop":
                            if (tk.Element.Type.Equals("L"))
                            {

                            }
                            else
                                result = 11;
                            break;

                        case "autocenter":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("L"))
                                {
                                    // Do it manually to prevent cross-platform issues
                                    var currentScreen = JAXApp.MainWindowInstance!.Screens.ScreenFromWindow(JAXApp.MainWindowInstance);
                                    JAXApp.MainWindowInstance.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;

                                    if (tk.AsBool() && currentScreen != null)
                                    {
                                        // Example: Center the window on its current screen

                                        int centerX = currentScreen.WorkingArea.X
                                            + (currentScreen.WorkingArea.Width - (int)JAXApp.MainWindowInstance.Width) / 2;

                                        int centerY = currentScreen.WorkingArea.Y
                                            + (currentScreen.WorkingArea.Height - (int)JAXApp.MainWindowInstance.Height) / 2;

                                        JAXApp.MainWindowInstance.Position = new PixelPoint(centerX, centerY);

                                        UserProperties[me.cPropLeft].Element.Value = JAXApp.MainWindowInstance.Bounds.Left;
                                        UserProperties[me.cPropTop].Element.Value = JAXApp.MainWindowInstance.Bounds.Top;
                                    }
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "backcolor":
                            if (JAXApp.MainWindowInstance is not null)
                                JAXApp.MainWindowInstance!.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            break;

                        case "borderstyle":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                    {

                                    }
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "commandwindowheight":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                        JAXApp.MainWindowInstance.commandWindow.Height = tk.AsInt();
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "commandwindowleft":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                        Avalonia.Controls.Canvas.SetLeft(JAXApp.MainWindowInstance.commandWindow, tk.AsInt());
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "commandwindowtop":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                        Avalonia.Controls.Canvas.SetTop(JAXApp.MainWindowInstance.commandWindow, tk.AsInt());
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "commandwindowwidth":
                            if (JAXApp.MainWindowInstance is not null)
                            {
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (tk.AsInt() < 0)
                                        result = 41;
                                    else
                                        JAXApp.MainWindowInstance.commandWindow.Width = tk.AsInt();
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "caption":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                    JAXApp.MainWindowInstance!.Title = tk.AsString();
                            }
                            else
                                result = 11;
                            break;

                        case "closable":
                            if (tk.Element.Type.Equals("L"))
                            {
                            }
                            else
                                result = 11;
                            break;

                        case "comment":
                            UserProperties["comment"].Element.Value = tk.AsString();
                            result = 9;
                            break;

                        case "controlbox":
                            if (tk.Element.Type.Equals("L"))
                            {
                            }
                            else
                                result = 11;
                            break;

                        case "fontname":
                            if (JAXApp.MainWindowInstance! is not null)
                            {
                                JAXApp.MainWindowInstance!.FontFamily = tk.AsString();
                                JAXApp.MainWindowInstance!.FontFamily ??= "Segoe UI";
                                JAXApp.MainWindowInstance!.FontFamily ??= "Arial";
                                JAXApp.MainWindowInstance!.FontFamily ??= "Hevelica";
                            }
                            break;

                        case "fontsize":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                        JAXApp.MainWindowInstance!.FontSize = tk.AsInt() / 72 * 96;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "fontbold":
                            if (tk.Element.Type.Equals("L"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                    JAXApp.MainWindowInstance!.FontWeight = tk.AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                            }
                            else
                                result = 11;
                            break;

                        case "fontitalic":
                            if (tk.Element.Type.Equals("L"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                    JAXApp.MainWindowInstance!.FontStyle = tk.AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                            }
                            else
                                result = 11;
                            break;

                        case "forecolor":
                            if (JAXApp.MainWindowInstance is not null)
                                JAXApp.MainWindowInstance!.Foreground = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            break;

                        case "height":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                        JAXApp.MainWindowInstance!.Height = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "icon":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    // set up the image and apply it
                                    var icon = string.IsNullOrEmpty(tk.AsString()) ? null : Program.CurrentApp.JaxImages!.GetImage(tk.AsString(), out _);
                                    icon ??= Program.CurrentApp.JaxImages!.GetImage("*jax*", out _);

                                    JAXApp.MainWindowInstance!.Icon = new Avalonia.Controls.WindowIcon(Program.CurrentApp.JaxImages!.Resize(icon, 32, 32));
                                }
                            }
                            else
                                result = 11;

                            break;

                        case "left":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    Avalonia.PixelPoint pp = new(tk.AsInt(), UserProperties[JAXLanguageLists.GetWord("top","REVPEMS")].AsInt());
                                    JAXApp.MainWindowInstance!.Position = pp;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "locked":
                            if (tk.Element.Type.Equals("L"))
                            {
                            }
                            else
                                result = 11;
                            break;

                        case "maxbutton":
                            if (tk.Element.Type.Equals("L"))
                            {

                            }
                            else
                                result = 11;
                            break;

                        case "maxheight":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    if (tk.AsInt() < 0)
                                        JAXApp.MainWindowInstance!.MaxHeight = double.NaN;
                                    else
                                        JAXApp.MainWindowInstance!.MaxHeight = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "maxwidth":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    if (tk.AsInt() < 0)
                                        JAXApp.MainWindowInstance!.MaxWidth = double.NaN;
                                    else
                                        JAXApp.MainWindowInstance!.MaxWidth = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "minheight":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    if (tk.AsInt() < 0)
                                        JAXApp.MainWindowInstance!.MinHeight = double.NaN;
                                    else
                                        JAXApp.MainWindowInstance!.MinHeight = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "minwidth":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    if (tk.AsInt() < 0)
                                        JAXApp.MainWindowInstance!.MinWidth = double.NaN;
                                    else
                                        JAXApp.MainWindowInstance!.MinWidth = tk.AsInt();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "minbutton":
                            if (tk.Element.Type.Equals("L"))
                            {
                            }
                            else
                                result = 11;
                            break;

                        case "monitor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                    {
                                        // Get all connected screens
                                        var screens = JAXApp.MainWindowInstance!.Screens.All;

                                        // Monitor 1 = index 1 (0 = primary / Monitor 0)
                                        if (JAXLib.Between(tk.AsInt(), 1, screens.Count))
                                        {
                                            var monitor1 = screens[tk.AsInt()];

                                            // Set manual startup location
                                            JAXApp.MainWindowInstance.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;

                                            // Position relative to Monitor 1's top-left corner
                                            int targetX = monitor1.Bounds.X;
                                            int targetY = monitor1.Bounds.Y;

                                            JAXApp.MainWindowInstance.Position = new PixelPoint(targetX, targetY);

                                            // Move to the center or topleft of the specified monitor
                                            if (UserProperties[JAXLanguageLists.GetWord("autocenter", "REVPEMS")].AsBool())
                                            {
                                                // Center
                                                await SetProperty(JAXLanguageLists.GetWord("autocenter", "REVPEMS"), true, 0);
                                            }
                                            else
                                            {
                                                UserProperties[JAXLanguageLists.GetWord("left","REVPEMS")].Element.Value = JAXApp.MainWindowInstance.Bounds.Left;
                                                UserProperties[JAXLanguageLists.GetWord("top", "REVPEMS")].Element.Value = JAXApp.MainWindowInstance.Bounds.Top;
                                            }
                                        }
                                        else
                                            result = 41;
                                    }
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "monitortop":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                    {
                                        var currentScreen = JAXApp.MainWindowInstance!.Screens.ScreenFromWindow(JAXApp.MainWindowInstance);
                                        JAXApp.MainWindowInstance.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;

                                        if (currentScreen != null)
                                        {
                                            if (JAXLib.Between(tk.AsInt(), 0, currentScreen.WorkingArea.Height))
                                            {
                                                int X = currentScreen.WorkingArea.X;
                                                int newY = tk.AsInt();

                                                JAXApp.MainWindowInstance.Position = new PixelPoint(X, newY);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "monitorleft":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                    {
                                        var currentScreen = JAXApp.MainWindowInstance!.Screens.ScreenFromWindow(JAXApp.MainWindowInstance);
                                        JAXApp.MainWindowInstance.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;

                                        if (currentScreen != null)
                                        {
                                            if (JAXLib.Between(tk.AsInt(), 0, currentScreen.WorkingArea.Height))
                                            {
                                                int Y = currentScreen.WorkingArea.Y;
                                                int newX = tk.AsInt();

                                                JAXApp.MainWindowInstance.Position = new PixelPoint(newX, Y);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "mousepointer":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    //JAXApp.MainWindowInstance!
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "moveable":
                            if (tk.Element.Type.Equals("L"))
                            {
                                //JAXApp.MainWindowInstance!
                            }
                            else
                                result = 11;
                            break;

                        case "name":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (JAXUtilities.IsValidName(tk.AsString()))
                                    UserProperties[me.cPropName].Element.Value = tk.AsString();
                                else
                                    result = 1575;
                            }
                            else
                                result = 11;
                            break;

                        case "forms":
                        case "objects":
                            result = 3040;
                            break;

                        case "picture":
                            break;

                        case "releasetype":
                            break;

                        case "righttoleft":
                            if (tk.Element.Type.Equals("L"))
                            {

                            }
                            else
                                result = 11;
                            break;

                        case "scalefactor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {

                                }
                            }
                            else
                                result = 11;
                            break;

                        case "showintaskbar":
                            if (tk.Element.Type.Equals("L"))
                            {
                            }
                            else
                                result = 11;
                            break;

                        case "tag":
                            UserProperties["tag"].Element.Value = tk.AsString();
                            result = 9;
                            break;

                        case "top":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    Avalonia.PixelPoint pp = new(Convert.ToInt32(UserProperties[me.cPropLeft].AsInt()), tk.AsInt());
                                    JAXApp.MainWindowInstance!.Position = pp;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "visible":
                            if (tk.Element.Type.Equals("L"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                    JAXApp.MainWindowInstance!.IsVisible = tk.AsBool(); ;
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
                                    if (JAXApp.MainWindowInstance is not null)
                                        JAXApp.MainWindowInstance!.Width = tk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "windowstate":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 2))
                                {
                                    if (JAXApp.MainWindowInstance is not null)
                                    {
                                        switch (tk.AsInt())
                                        {
                                            case 0:
                                                JAXApp.MainWindowInstance!.WindowState = Avalonia.Controls.WindowState.Normal;
                                                break;

                                            case 1:
                                                JAXApp.MainWindowInstance!.WindowState = Avalonia.Controls.WindowState.Minimized;
                                                break;

                                            case 2:
                                                JAXApp.MainWindowInstance!.WindowState = Avalonia.Controls.WindowState.Maximized;
                                                break;
                                        }
                                    }

                                    objValue = tk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        default:
                            result = 1;
                            break;
                    }

                    // Was the property retrieved?
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * Add a form to the Screens array
         * ------------------------------------------------------------------------------------------*/
        public int AddForm(JAXObjectWrapper jow)
        {
            int result = 0;

            try
            {
                Screens.Add(Screens.Count, new(jow));
            }
            catch (Exception ex)
            {
                result = 9999;
                AppIO.DebugLog($"Error {result} ({ex.Message}) in Screen.AddForm");
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * Remove a from from the Screens array using classID
         * ------------------------------------------------------------------------------------------*/
        public int RemoveForm(string ClassID)
        {
            int result = 9999;
            string msg = $"Did not find form with ClassID {ClassID}";

            try
            {
                for (int i = 0; i < Screens.Count; i++)
                {
                    JAXObjectWrapper jow = (JAXObjectWrapper)Screens[i].Element.Value;
                    if (jow.ClassID == ClassID)
                    {
                        Screens.Remove(i);
                        result = 0;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            if (result != 0)
                AppIO.DebugLog($"Error {result} ({msg}) in Screen.RemoveForm");

            return result;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;

            methodName = methodName.ToLower();
            string EnglishMethodName = Program.CurrentApp.ActiveLanguagePack.PEMs.TryGetValue(methodName, out string? p) ? p : methodName;

            switch (methodName)
            {
                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }

        public override string[] JAXMethods() => ["addproperty", "readexpression", "readmethod", "writeexpression", "writemethod"];


        public override string[] JAXEvents() => ["destroy", "error", "init", "load"];

        public override string[] JAXProperties() =>
            [
                "activecontrol,o!,","activeform,n!,","alwaysontop,L,false", "autocenter,L,false",
                "backcolor,R,255|255|255","baseclass,C!,form","borderstyle,N!,3",
                "caption,C,Form","class,C!,screen","classlibrary,C!,","closable,L,true","comment,C,",
                "commandwindowleft,N,100","commandwindowheight,N,200","commandwindowtop,N,100","commandwindowwidth,N,200",
                "controlbox,L,true","controlcount,N!,0",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","FontSize,N,12","forecolor,R,0","formcount,N!,0",
                "Height,N,300",
                "icon,C,",
                "left,N,0",
                "maxbutton,L,true","maxheight,N,-1","maxwidth,N,-1","minbutton,L,true","minheight,N,-1","minwidth,N,-1","mousepointer,n,0","moveable,L,true",
                "name,C,_screen",
                "objects,*,",
                "parent,o!$,","parentclass,C!$,","picture,C,",
                "releasetype,n,0","righttoleft,L,false",
                "scalefactor,N,0","showintaskbar,L,.T.",
                "tag,C,","top,N,0",
                "visible,L,true",
                "width,N,300","windowstate,N,0"
            ];
    }
}


