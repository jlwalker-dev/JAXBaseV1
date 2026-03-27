using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Avalonia : XBase_Avalonia
    {
        public XBase_Class_Avalonia(JAXObjectWrapper jow, string name) : base(jow, name) { }

        /* -----------------------------------------------------------------------------------
         * VFP Anchor values
         *
         * VFP ANCHOR values
         * Position         Bit Value   Description
         * Top Absolute     1           Anchors control to top border of container and does not change the distance between the top border.
         * Left Absolute    2           Anchors control to left border of container and does not change the distance between the left border.
         * Bottom Absolute  4           Anchors control to bottom border of container and does not change the distance between the bottom border.
         * Right Absolute   8           Anchors control to right border of container and does not change the distance between the right border.
         *
         * Top Relative     16          Anchors control to top border of container and maintains relative distance between the top border.
         * Left Relative    32          Anchors control to left border of container and maintains relative distance between the left border.
         * Bottom Relative  64          Anchors control to bottom border of container and maintains relative distance between the bottom border.
         * Right Relative   128         Anchors control to right border of container and maintains relative distance between the right border.
         * Horizontal Fixed 256         Anchors center of control relative to left and right borders but remains fixed in size.
         * Vertical Fixed   512         Anchors center of control relative to top and bottom borders but remains fixed in size.
         *
         *-----------------------------------------------------------------------------------*/

        /*
         * This handles the most common properties for visual controls.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Was not found - not yet processed
         *      2   - Requires special handling, did not process
         *      9   - Processed and saved, do not do anything else
         *      10  - 
         *      >10 - Error code being passed back
         *      
         */
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            Avalonia.Controls.Control? MyObj = me.avaloniaObject;
            Avalonia.Controls.Primitives.TemplatedControl? MyObjTC = MyObj as TemplatedControl;

            JAXObjects.Token objtk = new(objValue);

            //App.DebugLog($"XBase_Class_Avalonia: {me.JOWName.ToUpper()}.{propertyName}={objtk.AsString()}");

            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                // First, we check to make sure that the property exists
                if (UserProperties.ContainsKey(propertyName))
                {
                    if (UserProperties[propertyName].Protected)
                    {
                        // It's protected - leave it alone
                        result = 1533;
                    }
                    else if (UserProperties[propertyName].SpecialHandling)
                    {
                        // Special handling - pass it back
                        result = 2;
                    }
                    else if (MyObj is null)
                    {
                        // MyObj has to have something in it to
                        // process as a visual object.  This is
                        // most likely an error
                        result = 1901;
                    }
                    else
                    {
                        result = 0;

                        // Visual object common property handler.  Only the common properties
                        // that are registered to the current class will be executed.
                        switch (propertyName)
                        {
                            case "anchor":
                                if (objtk.Element.Type == "N")
                                {
                                    int anchorValue = objtk.AsInt();
                                    if (JAXLib.Between(anchorValue, 0, 1023)) // safe max for now
                                    {
                                        //await ApplyVFPAnchor(MyObj, anchorValue);
                                        objValue = anchorValue;
                                        result = 0;
                                    }
                                    else
                                        result = 41; // invalid property value
                                }
                                else
                                    result = 11; // type mismatch
                                break;

                            case "backcolor":
                                if (MyObjTC is not null)
                                    MyObjTC.Background = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                                else if (MyObj is Avalonia.Controls.Canvas)
                                {
                                    var c = MyObj as Avalonia.Controls.Canvas;
                                    c!.Background = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                                }
                                else if (MyObj is Avalonia.Controls.StackPanel)
                                {
                                    var c = MyObj as Avalonia.Controls.StackPanel;
                                    c!.Background = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                                }

                                objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                                break;

                            case "backstyle":
                                if (MyObjTC is not null)
                                    MyObjTC.Background = objtk.AsInt() == 0 ? Avalonia.Media.Brushes.Transparent : new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(UserProperties["backcolor"].AsInt())));
                                else if (MyObj is Avalonia.Controls.Canvas)
                                {
                                    var c = MyObj as Avalonia.Controls.Canvas;
                                    c!.Background = objtk.AsInt() == 0 ? Avalonia.Media.Brushes.Transparent : new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(UserProperties["backcolor"].AsInt())));
                                }
                                else if (MyObj is Avalonia.Controls.StackPanel)
                                {
                                    var c = MyObj as Avalonia.Controls.StackPanel;
                                    c!.Background = objtk.AsInt() == 0 ? Avalonia.Media.Brushes.Transparent : new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(UserProperties["backcolor"].AsInt())));
                                }

                                break;

                            case "borderwidth":
                                if (MyObjTC is not null)
                                    MyObjTC.BorderThickness = new(objtk.AsDouble());
                                break;

                            case "bordercolor":
                                if (MyObjTC is not null)
                                    MyObjTC.BorderBrush = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));

                                objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                                break;

                            case "borderstyle":
                                // TODO
                                break;

                            case "enabled":
                                MyObj.IsEnabled = objtk.AsBool();
                                break;

                            case "fontname":
                                if (MyObjTC is not null)
                                {
                                    MyObjTC.FontFamily = objtk.AsString();
                                    if (MyObjTC.FontFamily is null) MyObjTC.FontFamily = "Segoe UI";
                                    if (MyObjTC.FontFamily is null) MyObjTC.FontFamily = "Arial";
                                    if (MyObjTC.FontFamily is null) MyObjTC.FontFamily = "Hevelica";
                                }
                                break;

                            case "fontsize":
                                if (MyObjTC is not null)
                                    MyObjTC.FontSize = objtk.AsDouble() / 72 * 96;
                                break;

                            case "forecolor":
                                if (MyObjTC is not null)
                                    MyObjTC.Foreground = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                                objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                                break;

                            case "fontbold":
                                if (MyObjTC is not null)
                                    MyObjTC.FontWeight = objtk.AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                                break;

                            case "fontitalic":
                                if (MyObjTC is not null)
                                    MyObjTC.FontStyle = objtk.AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                                break;

                            case "fontunderline":

                                break;

                            case "fontstrikethrough":
                                break;

                            case "height":
                                if (objtk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    MyObj.Height = objtk.AsInt();
                                    App.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
                                }
                                break;

                            case "left":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    Avalonia.Controls.Canvas.SetLeft(MyObj, objtk.AsInt());
                                    App.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
                                }
                                else
                                    result = 11;
                                break;

                            case "maxheight":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    if (objtk.AsInt() >= 0)
                                        MyObj.MaxHeight = objtk.AsInt();
                                    else
                                        MyObj.MaxHeight = double.PositiveInfinity;
                                }
                                else
                                    result = 11;
                                break;

                            case "maxwidth":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    if (objtk.AsInt() >= 0)
                                        MyObj.MaxWidth = objtk.AsInt();
                                    else
                                        MyObj.MaxWidth = double.PositiveInfinity;
                                }
                                else
                                    result = 11;
                                break;

                            case "minheight":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    if (objtk.AsInt() >= 0)
                                        MyObj.MinHeight = objtk.AsInt();
                                    else
                                        MyObj.MinHeight = 0;
                                }
                                else
                                    result = 11;
                                break;

                            case "minwidth":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    if (objtk.AsInt() >= 0)
                                        MyObj.MaxWidth = objtk.AsInt();
                                    else
                                        MyObj.MaxWidth = 0;
                                }
                                else
                                    result = 11;
                                break;

                            case "name":
                                if (objtk.Element.Type.Equals("C"))
                                {
                                    string nm = objtk.AsString();
                                    nm = string.IsNullOrWhiteSpace(nm) ? me.ClassID : nm;
                                    if (JAXUtilities.IsValidName(nm))
                                    {
                                        // We set the control name to the ClassID.  This
                                        // is a unique name, without question, and it provides
                                        // the ability to double check to see if this is the
                                        // ClassID and object name are equal for sanity checks
                                        if (MyObj.IsInitialized == false)
                                            MyObj.Name = me.ClassID;

                                        // This is where we find the name along
                                        // with in the UserProperties
                                        me.SetName(nm);
                                        objValue = nm;
                                    }
                                    else
                                        result = 9105;
                                }
                                else
                                    result = 11;
                                break;

                            case "righttoleft":
                                MyObj.FlowDirection = objtk.AsBool() ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
                                break;

                            case "tag":
                                MyObj.Tag = objtk.AsString();
                                break;

                            case "tabstop":
                                MyObj.IsTabStop = objtk.AsBool();
                                break;

                            case "tabindex":
                                MyObj.TabIndex = objtk.AsInt();
                                break;

                            case "top":
                                Avalonia.Controls.Canvas.SetTop(MyObj, objtk.AsInt());
                                App.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
                                break;

                            case "tooltiptext":
                                if (string.IsNullOrWhiteSpace(objtk.AsString()))
                                    Avalonia.Controls.ToolTip.SetTip(MyObj, null);
                                else
                                    Avalonia.Controls.ToolTip.SetTip(MyObj, objtk.AsString());
                                break;

                            case "visible":
                                MyObj.IsVisible = objtk.AsBool();
                                break;

                            case "width":
                                if (objtk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    MyObj.Width = objtk.AsInt();
                                    App.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
                                }
                                break;

                            default:
                                result = 1;
                                break;
                        }
                    }

                    // We don't save what we don't process
                    if (result == 0)
                    {
                        // We processed it, so save the property to the dictionary
                        UserProperties[propertyName].Element.Value = objValue;
                    }
                }
                else
                    result = 1559;
            }

            return result;
        }

        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token resultToken = new();
            int result = 0;
            Avalonia.Controls.Control? MyObj = me.avaloniaObject;
            propertyName = propertyName.ToLower();

            // First, we check to make sure that the property exists
            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].SpecialHandling)
                {
                    // Special handling - pass it back
                    result = 2;
                }
                else if (MyObj is null)
                {
                    if (me.nvObject is null)
                    {
                        // if no object defined, raise the error
                        result = 1901;
                    }
                    else
                    {
                        // If we're here with a non visual object
                        // then just return the value from UserProperties
                        // TODO - drop to base for user final property processing?

                        UserProperties[propertyName].ElementNumber = idx;
                        resultToken.Element.Value = UserProperties[propertyName].Element.Value;
                    }
                }
                else
                {
                    // Get the property and fill in the value
                    //resultToken.CopyFrom(UserProperties[propertyName]);

                    switch (propertyName.ToLower())
                    {
                        case "enabled":
                            resultToken.Element.Value = MyObj.IsEnabled;
                            break;


                        case "height":
                            resultToken.Element.Value = MyObj.Height;
                            break;

                        case "left":
                            Rect boundX = MyObj.Bounds;
                            resultToken.Element.Value = boundX.X;
                            break;

                        case "maxheight":
                            resultToken.Element.Value = MyObj.MaxHeight;
                            break;

                        case "maxwidth":
                            resultToken.Element.Value = MyObj.MaxWidth;
                            break;

                        case "minheight":
                            resultToken.Element.Value = MyObj.MinHeight;
                            break;

                        case "minwidth":
                            resultToken.Element.Value = MyObj.MinWidth;
                            break;

                        case "name":
                            resultToken.Element.Value = me.JOWName;
                            break;

                        case "objects":
                            UserProperties["objects"].ElementNumber = idx;
                            resultToken.Element.Value = UserProperties["objects"].Element.Value;
                            break;

                        case "parent":
                            if (Parent is null)
                                resultToken.Element.MakeNull();
                            else
                            {
                                resultToken.Element.Value = Parent;
                            }
                            break;

                        case "parentclass":
                            if (Parent is null)
                                resultToken.Element.Value = string.Empty;
                            else
                            {
                                JAXObjects.Token tk = await Parent.GetProperty("class");
                                if (tk.Element.Type.Equals("C"))
                                    resultToken.Element.Value = tk.AsString();
                                else
                                    resultToken.Element.Value = string.Empty;
                            }
                            break;

                        case "tabindex":
                            resultToken.Element.Value = MyObj.TabIndex;
                            break;

                        case "tabstop":
                            resultToken.Element.Value = MyObj.IsTabStop;
                            break;

                        case "top":
                            Rect boundY = MyObj.Bounds;
                            resultToken.Element.Value = boundY.Y;
                            break;

                        case "width":
                            resultToken.Element.Value = MyObj.Width;
                            break;

                        case "visible":
                            resultToken.Element.Value = MyObj.IsVisible;
                            break;

                        default:
                            // Not processed - TODO - drop to base for user final property processing?
                            result = 1;
                            resultToken.Element.MakeNull();
                            break;

                    }
                }
            }

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                resultToken.Element.MakeNull();
            }

            return resultToken;
        }
    }
}
