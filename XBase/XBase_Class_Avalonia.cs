/*
 * This class is the base class for all visual controls in Avalonia.  It provides
 * the common properties and methods that are shared across all visual controls.
 * 
 * It also provides the implementation for the VFP Anchor property, which allows
 * controls to be anchored to the edges of their container and adjust their position
 * and size accordingly when the container is resized.
 * 
 * The SetProperty and GetProperty methods are overridden in the class definition
 * for visual controls, while the common properties are handled here.  The base
 * class really doesn't get called by visual controls.
 * 
 * 2026-04-06 - JLW
 *      The VFP Anchor property is fully supported for absolute anchoring.  
 *      Relative anchoring and fixed centering are not yet supported, but the 
 *      structure is in place to add support in the future.
 *      
 */
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Avalonia(JAXObjectWrapper jow, string name) : XBase_Avalonia(jow, name)
    {
        private bool topAbs = false;
        private bool leftAbs = false;
        private bool bottomAbs = false;
        private bool rightAbs = false;

        // Future expansion
        private bool topRel = false;
        private bool leftRel = false;
        private bool bottomRel = false;
        private bool rightRel = false;

        private bool horzFixed = false;
        private bool vertFixed = false;

        // Current position and size for use in ApplyVFPAnchor to track changes
        // over time and prevent over-adjustment when resizing multiple times
        private double currentTop = 0;
        private double currentLeft = 0;
        private double currentWidth = 0;
        private double currentHeight = 0;

        /* -----------------------------------------------------------------------------------
         * VFP Anchor property support
         *
         * VFP ANCHOR values
         * Position         Bit Value   Description
         * Top Absolute     1           Anchors control to top border of container and does not change the distance between the top border.
         * Left Absolute    2           Anchors control to left border of container and does not change the distance between the left border.
         * Bottom Absolute  4           Anchors control to bottom border of container and does not change the distance between the bottom border.
         * Right Absolute   8           Anchors control to right border of container and does not change the distance between the right border.
         *
         * DeltaX and DeltaY are the changes in the container's size since the last 
         * time this method was called.  This allows us to adjust the position and 
         * size of the control based on the anchor settings.
         *
         * -----------------------------------------------------------------------------------
         * FUTURE SUPPORT
         * -----------------------------------------------------------------------------------
         * Top Relative     16          Anchors control to top border of container and maintains relative distance between the top border.
         * Left Relative    32          Anchors control to left border of container and maintains relative distance between the left border.
         * Bottom Relative  64          Anchors control to bottom border of container and maintains relative distance between the bottom border.
         * Right Relative   128         Anchors control to right border of container and maintains relative distance between the right border.
         * Horizontal Fixed 256         Anchors center of control relative to left and right borders but remains fixed in size.
         * Vertical Fixed   512         Anchors center of control relative to top and bottom borders but remains fixed in size.
         *-----------------------------------------------------------------------------------*/
        public override void ApplyVFPAnchor(double DeltaX, double DeltaY)
        {
            if (me.AnchorValue == 0) return;

            AppIO.DebugLog($">>>>>Applying VFP Anchor for {me.JOWName} with AnchorValue={me.AnchorValue}, DeltaX={DeltaX}, DeltaY={DeltaY}");

            Avalonia.Controls.Control MyObj = me.avaloniaObject!;

            if (topAbs && bottomAbs)
            {
                // Top and bottom absolute - adjust height by deltaY
                //SetProperty("Height", System.Math.Max(0, MyObj.Height + DeltaY),0).Wait();
                double height = System.Math.Max(0, MyObj.Height + DeltaY);
                currentHeight += DeltaY;

                if (DeltaY < 0 || currentHeight > 0)
                    MyObj.Height = height;
                //AppIO.DebugLog($">>>>>    MyObj.Height={height}");
            }
            else if (bottomAbs)
            {
                // Bottom absolute - adjust top by deltaY
                double top = System.Math.Max(me.originalTop, Avalonia.Controls.Canvas.GetTop(MyObj) + DeltaY);
                currentTop += DeltaY;

                if (DeltaY < 0 || currentTop > me.originalTop)
                    Avalonia.Controls.Canvas.SetTop(MyObj, top);

                //AppIO.DebugLog($">>>>>    MyObj.Top={top}");
            }

            if (leftAbs && rightAbs)
            {
                // Left and right absolute - adjust width by deltaX
                double width = System.Math.Max(0, MyObj.Width + DeltaX);
                currentWidth += DeltaX;

                if (DeltaX < 0 || currentWidth >= 0)
                    MyObj.Width = width;

                //AppIO.DebugLog($">>>>>    MyObj.Width={width}");
            }
            else if (rightAbs)
            {
                // Right absolute - adjust left by deltaX
                double left = System.Math.Max(0, Avalonia.Controls.Canvas.GetLeft(MyObj) + DeltaX);
                double deltaX = 0;

                if (DeltaX < 0)
                {
                    if (currentLeft > 0 && currentLeft + DeltaX < 0)
                        deltaX = DeltaX - currentLeft;
                    else if (currentLeft <= 0)
                        deltaX = DeltaX;
                }

                currentLeft += DeltaX;

                if (DeltaX < 0)
                {
                    if (currentLeft > 0)
                        MyObj.SetValue(Avalonia.Controls.Canvas.LeftProperty, left);
                    else
                        MyObj.SetValue(Avalonia.Controls.Canvas.LeftProperty, 0);

                    if (currentLeft < 0)
                    {
                        MyObj.Width = System.Math.Max(0, MyObj.Width + deltaX);
                        //AppIO.DebugLog($">>>>>    Adjusting width by deltaX={deltaX} due to left boundary. New width={MyObj.Width} MinWidth={MyObj.MinWidth}");
                    }
                }
                else
                {
                    if (MyObj.Width < me.originalWidth)
                        MyObj.Width = System.Math.Min(me.originalWidth, MyObj.Width + DeltaX);
                    else
                        MyObj.SetValue(Avalonia.Controls.Canvas.LeftProperty, left);
                }

                //AppIO.DebugLog($">>>>>    MyObj.Left={left}");
            }

            //Rect boundX = MyObj.Bounds;
            //AppIO.DebugLog($">>>>>    MyObj.Bounds after applying anchor: ({boundX.X}x, {boundX.Y}y, {boundX.Width}w, {boundX.Height}h)");
            //AppIO.DebugLog("---------------------------------------------------------------------\r\n\r\n");
        }


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
            int result;
            Avalonia.Controls.Control? MyObj = me.avaloniaObject;
            Avalonia.Controls.Primitives.TemplatedControl? MyObjTC = MyObj as TemplatedControl;

            JAXObjects.Token objtk = new(objValue);

            //AppIO.DebugLog($"XBase_Class_Avalonia: {me.JOWName.ToUpper()}.{propertyName}={objtk.AsString()}");

            propertyName = propertyName.ToLower();

            if (UserProperties.TryGetValue(propertyName, out JAXObjects.Token? value) && value.Protected)
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
                                    if (JAXLib.Between(objtk.AsInt(), 0, 1023)) // safe max for now
                                    {
                                        // Decode the anchor value into its components for use in ApplyVFPAnchor
                                        me.AnchorValue = objtk.AsInt();
                                        objValue = me.AnchorValue;

                                        // Fully Supported
                                        topAbs = (me.AnchorValue & 1) != 0;
                                        leftAbs = (me.AnchorValue & 2) != 0;
                                        bottomAbs = (me.AnchorValue & 4) != 0;
                                        rightAbs = (me.AnchorValue & 8) != 0;

                                        // 2026-04-05
                                        // Not yet supported
                                        // If absolute flag is set, the relative flag is ignored
                                        topRel = (me.AnchorValue & 16) != 0 && topAbs == false;
                                        leftRel = (me.AnchorValue & 32) != 0 && leftAbs == false;
                                        bottomRel = (me.AnchorValue & 64) != 0 && bottomAbs == false;
                                        rightRel = (me.AnchorValue & 128) != 0 && rightAbs == false;

                                        if ((me.AnchorValue & 256) != 0)
                                        {
                                            // Horizontally fixed
                                            horzFixed = true;
                                            leftAbs = false;
                                            leftRel = false;
                                            rightAbs = false;
                                            rightRel = false;
                                        }

                                        if ((me.AnchorValue & 512) != 0)
                                        {
                                            // Vertically fixed
                                            vertFixed = true;
                                            topAbs = false;
                                            topRel = false;
                                            bottomAbs = false;
                                            bottomRel = false;
                                        }
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
                                if (MyObjTC is not null)
                                {
                                    if (objtk.Element.Type.Equals("N"))
                                    {
                                        if (objtk.AsInt() == 0)
                                            MyObjTC.BorderThickness = new Avalonia.Thickness(0);
                                        else
                                            MyObjTC.BorderThickness = new Avalonia.Thickness(UserProperties["borderwidth"].AsInt());
                                    }
                                }
                                break;


                            case "enabled":
                                MyObj.IsEnabled = objtk.AsBool();
                                break;

                            case "fontname":
                                // TODO - Need to rethink this
                                if (MyObjTC is not null)
                                {
                                    MyObjTC.FontFamily = objtk.AsString();
                                    MyObjTC.FontFamily ??= "Segoe UI";
                                    MyObjTC.FontFamily ??= "Arial";
                                    MyObjTC.FontFamily ??= "Hevelica";
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

                            case "height":
                                if (objtk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    currentHeight = objtk.AsDouble();
                                    MyObj.Height = objtk.AsDouble();
                                    AppIO.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
                                }
                                break;

                            case "left":
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    currentLeft = objtk.AsDouble();
                                    Avalonia.Controls.Canvas.SetLeft(MyObj, objtk.AsDouble());
                                    AppIO.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
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
                                currentTop = objtk.AsDouble();
                                me.originalTop = objtk.AsDouble();
                                Avalonia.Controls.Canvas.SetTop(MyObj, objtk.AsDouble());
                                AppIO.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsDouble()}");
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
                                    me.originalWidth = objtk.AsDouble();
                                    currentWidth = objtk.AsDouble();
                                    MyObj.Width = objtk.AsDouble();
                                    AppIO.DebugLog($">>>>>Setting {me.JOWName}.{propertyName} to {objtk.AsInt()}");
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
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                resultToken.Element.MakeNull();
            }

            return resultToken;
        }

        /// <summary>
        /// Re-applies Left and Top attached properties to the Avalonia control after it has been
        /// added to a Canvas (or after child-moving in FakeWindow). 
        /// Call this from AddObject (and any child transfer logic) to make inline 
        /// CREATEOBJECT([left=...; top=...]) work reliably.
        /// </summary>
        public async Task ReapplyPosition(JAXObjectWrapper jow)
        {
            if (jow.thisObject is null || jow.avaloniaObject is null)
                return;

            AppIO.DebugLog($">>>>>Reapplying {jow.JOWName} left and top values");

            // Use the same pattern as SetProperty for consistency
            if (jow.thisObject.UserProperties.TryGetValue("left", out JAXObjects.Token? valueLeft))
                Avalonia.Controls.Canvas.SetLeft(jow.avaloniaObject, valueLeft.AsDouble());

            if (jow.thisObject.UserProperties.TryGetValue("top", out JAXObjects.Token? valueTop))
                Avalonia.Controls.Canvas.SetTop(jow.avaloniaObject, valueTop.AsDouble());

            if (jow.thisObject.UserProperties.TryGetValue("minwidth", out JAXObjects.Token? valueMinWidth))
                jow.avaloniaObject.MinWidth = System.Math.Max(0, valueMinWidth.AsDouble());

            if (jow.thisObject.UserProperties.TryGetValue("minheight", out JAXObjects.Token? valueMinHeight))
                jow.avaloniaObject.MinHeight = System.Math.Max(0, valueMinHeight.AsDouble());

            if (jow.thisObject.UserProperties.TryGetValue("maxwidth", out JAXObjects.Token? valueMaxWidth))
                jow.avaloniaObject.MaxWidth = valueMaxWidth.AsDouble() >= 0 ? valueMaxWidth.AsDouble() : double.PositiveInfinity;

            if (jow.thisObject.UserProperties.TryGetValue("maxheight", out JAXObjects.Token? valueMaxHeight))
                jow.avaloniaObject.MaxHeight = valueMaxHeight.AsDouble() >= 0 ? valueMaxHeight.AsDouble() : double.PositiveInfinity;

            if (jow.thisObject.UserProperties.TryGetValue("width", out JAXObjects.Token? valueWidth))
                jow.avaloniaObject.Width = valueWidth.AsDouble();

            if (jow.thisObject.UserProperties.TryGetValue("height", out JAXObjects.Token? valueHeight))
                jow.avaloniaObject.Height = valueHeight.AsDouble();
        }


        public override void CanvasResized(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            AppIO.DebugLog($">>>>>CanvasResized: {me.JOWName} - NewSize=({e.NewSize.Width}, {e.NewSize.Height}), PreviousSize=({e.PreviousSize.Width}, {e.PreviousSize.Height})");

            // Get the NEW size (after resize)
            double newWidth = e.NewSize.Width;
            double newHeight = e.NewSize.Height;

            // Get the PREVIOUS size (before this resize) - useful for comparison
            double previousWidth = e.PreviousSize.Width;
            double previousHeight = e.PreviousSize.Height;

            // Optional: Safe fallback if size is NaN or invalid
            if (double.IsNaN(newWidth)) newWidth = previousWidth;
            if (double.IsNaN(newHeight)) newHeight = previousHeight;

            double widthChange = newWidth - previousWidth;
            double heightChange = newHeight - previousHeight;

            if (previousHeight > 0D && previousWidth > 0D)
            {
                // Canvas has captured initial size, so we can calculate
                // the change and apply anchors to child controls
                JAXObjects.Token objs = UserProperties["objects"];
                AppIO.DebugLog($">>>>>CanvasResized: Processing child objects in {me.JOWName} for VFP Anchor. Child count: {objs.Col}");

                for (int i = 0; i < objs.Col; i++)
                {
                    if (objs._avalue[i].IsNull())
                    {
                        AppIO.DebugLog($">>>>>CanvasResized: Skipping null at index {i}");
                    }
                    else
                    {
                        JAXObjectWrapper jow = (JAXObjectWrapper)objs._avalue[i].Value;
                        AppIO.DebugLog($">>>>>CanvasResized: {jow.JOWName} - AnchorValue={jow.AnchorValue}");

                        if (jow.AnchorValue > 0)
                            jow.ApplyVFPAnchor(widthChange, heightChange);
                    }
                }
            }
            else
            {
                AppIO.DebugLog($">>>>>CanvasResized: Previous canvas size is invalid (width={previousWidth}, height={previousHeight}). Skipping anchor processing.");
            }

            // TODO - Call the resize event
        }


        public override void CleanUp(bool disposing)
        {
            base.CleanUp(disposing);
        }
    }
}
