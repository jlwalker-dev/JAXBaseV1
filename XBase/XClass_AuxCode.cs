/*--------------------------------------------------------------------------------------------------*
 * 2025-05-06 - JLW
 * 
 * Holds common conversion of xBase to .Net, and back, routines for the XClass 
 * properties such as Color, Font related, and Anchor
 * 
 *--------------------------------------------------------------------------------------------------*/
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;                // for Brush, Geometry, etc.
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using JAXBase.Core;
using JAXBase.Executer;

namespace JAXBase.XBase
{
    public class XClass_AuxCode
    {
        public static async Task<List<xParameters>> ParameterListFromString(AppClass app, string parameterStr)
        {
            List<xParameters> pList = [];
            parameterStr = parameterStr.Replace("\r", "");
            string[] parameterString = parameterStr.Split("\r");

            foreach (string p in parameterString)
            {
                if (p.Length > 1 && p.Contains('='))
                {
                    int f = p.IndexOf('=');
                    xParameters parm = new()
                    {
                        Name = p[..f],
                        Value = await JAXBase_Executer_M.RawMath(app, p[(f + 1)..])
                    };

                    pList.Add(parm);
                }
            }

            return pList;
        }

        /* -----------------------------------------------------------------------------------
         *  Font Controls
         * -----------------------------------------------------------------------------------*/
        public static Font SetFont(IJAXAvaClass myObj)
        {

            /* FontCharSet values (Unsupported in Ver 1)
             *   0 Western
             *   1 Default
             *   2 Symbol
             * 128 Japanese
             * 161 Greek
             * 162 Turkish
             * 163 Vietnamese
             * 177 Hebrew
             * 178 Arabic
             * 186 Baltic
             * 204 Cyrillic
             * 238 Central European
             * 
             */

            /* Supported Font Styles
             * 0 Regular
             * 1 Bold
             * 2 Italics
             * 4 Underline
             * 8 Strikethrough
             */


            int style = (myObj.UserProperties["fontbold"].AsBool() ? 1 : 0) + (myObj.UserProperties["FontItalic"].AsBool() ? 2 : 0) + (myObj.UserProperties["FontUnderline"].AsBool() ? 4 : 0) + (myObj.UserProperties["FontStrikeThrough"].AsBool() ? 8 : 0);
            return new(myObj.UserProperties["FontName"].AsString(), myObj.UserProperties["FontSize"].AsFloat(), (System.Drawing.FontStyle)style);
        }



        


        /* -----------------------------------------------------------------------------------
         * Color Interface
         * -----------------------------------------------------------------------------------*/

        // Alpha Red Green Blue from a property to an int
        public static string RGBIntToHex(int rgb)
        {
            return "#FF" + rgb.ToString("X6");
        }

        public static string ARGBIntToHex(int alpha, int rgb)
        {
            return "#" + alpha.ToString("X2") + rgb.ToString("X6");
        }

        public static Avalonia.Media.Color IntToAvColor(int color)
        {
            byte r = Convert.ToByte(color % 256);
            byte g = Convert.ToByte((color % 65536) / 256);
            byte b = Convert.ToByte(color / 65536);
            return Avalonia.Media.Color.FromArgb(255, r, g, b);
        }

        /* -----------------------------------------------------------------------------------
         * Used for ResetToDefault process which will reset the Value of
         * the control to the registered default value or an empty value
         * if none is registered.
         * 
         * cmd  Action
         * ---  -------------------------------------------
         * C    Clear the default value to an empty value
         * R    Reset value from the defaultvalue
         * S    Set the defaultvalue from current value
         * 
         * -----------------------------------------------------------------------------------*/
        public static async Task SetDefault(JAXObjectWrapper me, string cmd)
        {
            JAXObjects.Token tk;

            // Doublecheck that this control has set default capabilities
            if ((await me.IsMember("setdefault")).Equals("P"))
            {
                switch (cmd.ToLower())
                {
                    case "CLEAR":
                    case "C":
                        tk = await me.GetProperty("value");
                        if (tk.Element.IsNull() == false)
                            await me.SetProperty("defaultvalue", tk.Element.ValueAsEmpty()!);
                        break;

                    case "RESET":
                    case "R":
                        tk = await me.GetProperty("defaultvalue");
                        if (tk.Element.IsNull() == false)
                            await me.SetProperty("value", tk.Element.Value);
                        break;

                    case "SET":
                    case "S":
                        tk = await me.GetProperty("value");
                        if (tk.Element.IsNull() == false)
                            await me.SetProperty("defaultvalue", tk.Element.Value);
                        break;
                }
            }
        }


        /* -----------------------------------------------------------------------------------*
         * Reset a property to it's default value or empty value.
         * 
         * Properties set up at init may have default values and that will be used.
         * Protected properties won't change and won't be affected.
         * -----------------------------------------------------------------------------------*/
        public static async Task ResetPropertyToDefault(JAXObjectWrapper me, string property)
        {
            property = property.ToLower().Trim();

            if ((await me.IsMember(property)).Equals("P"))
            {
                JAXObjects.Token tk = await me.GetProperty(property);
                if ( tk.Element.IsNull())
                    throw new Exception($"9999|ResetToDefault|Failed to reset property {property}");

                if (tk.Element.DefaultValue is not null)
                {
                    // If it has a default value, always drop to here
                    // so that protected properties don't get called 
                    // and blow things up with an error
                    if (tk.Element.HasChanged)
                        tk.Element.SetToDefault();
                }
                else
                {
                    // Arrays and objects are ignored
                    if (tk.TType.Equals("A") == false && tk.Element.Type.Equals("O") == false)
                    {
                        // Set user defined properties to an empty value
                        object v = tk.Element.Type switch
                        {
                            "N" => 0,
                            "C" => string.Empty,
                            "D" => DateOnly.MinValue,
                            "T" => DateTime.MinValue,
                            _ => false
                        };

                        await me.SetProperty(property, v);
                    }
                }
            }
            else
                throw new Exception("1559|" + property.ToUpper());
        }

        public static void AddLockedProperty(JAXObjectWrapper me, string propertyName, string lockType, string lockValue)
        {
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk;
            if (string.IsNullOrWhiteSpace(lockType))
            {
                // No locktype indicates it can be anything
                // and therefore we start with an empty string
                tk = new();
                tk.Element.Value = string.Empty;
            }
            else
            {
                // Set the default value of the type-locked property
                tk = new(lockValue, lockType);
                tk.Element.SetDefaultValue(tk.Element.Value, true); // When a property type is locked, the default value is automatically set
            }

            // Add the property without calling the ADDPROPERTY method
            me.AddPropertyDirect(propertyName, tk);
        }

        public static async Task Method_Addobject(AppClass App, JAXObjectWrapper me)
        {
            // TODO - Only certain classes can have objects added to them
            if (App.ParameterClassList.Count == 1 && App.ParameterClassList[0].token.Element.Type.Equals("O"))
            {
                // JAXBase can accept an object in ADDOBJECT()
                await me.AddObject((JAXObjectWrapper)App.ParameterClassList[0].token.Element.Value);
            }
            else
            {
                // we're expecting cName, cClass [,aInit1, aInit2...]
                if (App.ParameterClassList.Count > 1)
                {
                    List<JAXObjects.Token> tkList = [];
                    foreach (ParameterClass p in App.ParameterClassList)
                    {
                        JAXObjects.Token tk = new();
                        tk.CopyFrom(p.token);
                        tkList.Add(tk);
                    }
                    await me.AddObjectUsingParameters(tkList);
                }
            }
        }

        /*
         * Create an array object using the rowsource and rowsource type information
         * 
         * Row Source Type
         * nValue   Description  
         * 0        None. (Default) 
         * 1        "Value1,Value2,..."
         * 2        Table alias - Version 0.8
         * 3        SQL statement - Version 1.0
         * 4        Query (.qpr) file - Version 1.0
         * 5        Array
         * 6        Fields - After Version 1.0
         * 7        Files - After Version 1.0
         * 8        Field structure of a table - After Version 1.0
         * 9        JSON string - After Version 1.0
         * 10       Collection object - After Version 1.0
         */
        public static async Task<JAXObjects.Token> GetRowSource(AppClass app, string rowsource, int rowsourcetype)
        {
            JAXObjects.Token rowInfo = new();

            switch (rowsourcetype)
            {
                case 0: break;      // No row source
                case 1:             // Row source is a a comma delimited string
                    string[] sArray = rowsource.Split(',');
                    AppHelper.ASetDimension(rowInfo, sArray.Length, 1);
                    for (int i = 0; i < sArray.Length; i++)
                        rowInfo._avalue[i].Value = sArray[i];
                    break;

                case 2: break;
                case 3: break;
                case 4: break;

                case 5:             // Row source is an array
                    JAXObjects.Token mArray = await app.GetVarToken(rowsource);
                    AppHelper.ASetDimension(rowInfo, mArray.Row, mArray.Col);

                    for (int i = 0; i < mArray._avalue.Count; i++)
                        rowInfo._avalue[i].Value = mArray._avalue[i];
                    break;

                case 6: break;
                case 7: break;
                case 8: break;
                case 9: break;
                case 10: break;

                default:
                    throw new Exception("11|");
            }

            return rowInfo;
        }


        /* ------------------------------------------------------------------------------------------------------
         * Clone a canvas
         * ------------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Creates a fully independent copy of a Canvas (including children, positions, brushes, etc.)
        /// The result has no reference to the original visual/logical tree.
        /// </summary>
        public static Canvas DeepClone(Canvas source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var clone = new Canvas
            {
                Width = source.Width,
                Height = source.Height,
                MinWidth = source.MinWidth,
                MinHeight = source.MinHeight,
                MaxWidth = source.MaxWidth,
                MaxHeight = source.MaxHeight,
                Background = SafeCloneBrush(source.Background),
                ClipToBounds = source.ClipToBounds,
                IsVisible = source.IsVisible,
                Opacity = source.Opacity,
                // Add more Canvas/Control properties you actually need (RenderTransform, etc.)
            };

            foreach (var child in source.Children)
            {
                if (child is not Avalonia.Controls.Control originalChild) continue;

                var clonedChild = CloneChildControl(originalChild);
                if (clonedChild != null)
                {
                    clone.Children.Add(clonedChild);

                    // Preserve Canvas attached properties
                    Canvas.SetLeft(clonedChild, Canvas.GetLeft(originalChild));
                    Canvas.SetTop(clonedChild, Canvas.GetTop(originalChild));
                    Canvas.SetRight(clonedChild, Canvas.GetRight(originalChild));
                    Canvas.SetBottom(clonedChild, Canvas.GetBottom(originalChild));
                    child.ZIndex = originalChild.ZIndex;
                }
            }

            return clone;
        }

        private static Avalonia.Controls.Control? CloneChildControl(Avalonia.Controls.Control source)
        {
            return source switch
            {
                // Shapes
                Avalonia.Controls.Shapes.Rectangle r => new Avalonia.Controls.Shapes.Rectangle
                {
                    Width = r.Width,
                    Height = r.Height,
                    Fill = SafeCloneBrush(r.Fill),
                    Stroke = SafeCloneBrush(r.Stroke),
                    StrokeThickness = r.StrokeThickness,
                    StrokeDashArray = r.StrokeDashArray,  // usually safe to share (doubles)
                    RadiusX = r.RadiusX,
                    RadiusY = r.RadiusY,
                },

                Avalonia.Controls.Shapes.Ellipse e => new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = e.Width,
                    Height = e.Height,
                    Fill = SafeCloneBrush(e.Fill),
                    Stroke = SafeCloneBrush(e.Stroke),
                    StrokeThickness = e.StrokeThickness,
                    StrokeDashArray = e.StrokeDashArray,
                },

                Avalonia.Controls.Shapes.Line l => new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = l.StartPoint,
                    EndPoint = l.EndPoint,
                    Stroke = SafeCloneBrush(l.Stroke),
                    StrokeThickness = l.StrokeThickness,
                    StrokeDashArray = l.StrokeDashArray,
                },

                Avalonia.Controls.Shapes.Path p => new Avalonia.Controls.Shapes.Path
                {
                    Data = p.Data?.Clone(),  // Geometry usually has Clone()
                    Fill = SafeCloneBrush(p.Fill),
                    Stroke = SafeCloneBrush(p.Stroke),
                    StrokeThickness = p.StrokeThickness,
                },

                // Text
                TextBlock tb => new TextBlock
                {
                    Text = tb.Text,
                    FontSize = tb.FontSize,
                    FontFamily = tb.FontFamily,
                    FontWeight = tb.FontWeight,
                    FontStyle = tb.FontStyle,
                    Foreground = SafeCloneBrush(tb.Foreground),
                    HorizontalAlignment = tb.HorizontalAlignment,
                    VerticalAlignment = tb.VerticalAlignment,
                    TextAlignment = tb.TextAlignment,
                    // Add Padding, LineHeight, etc. if used
                },

                // Images (source usually immutable/shareable)
                Avalonia.Controls.Image img => new Avalonia.Controls.Image
                {
                    Source = img.Source,  // IBitmap / IImage typically safe to share
                    Width = img.Width,
                    Height = img.Height,
                    Stretch = img.Stretch,
                    StretchDirection = img.StretchDirection,
                },

                // Containers
                Border b => new Border
                {
                    Background = SafeCloneBrush(b.Background),
                    BorderBrush = SafeCloneBrush(b.BorderBrush),
                    BorderThickness = b.BorderThickness,
                    CornerRadius = b.CornerRadius,
                    Padding = b.Padding,
                    Child = CloneChildControl(b.Child! as Avalonia.Controls.Control),  // recursive
                },

                Canvas nested => DeepClone(nested),  // recursive for nested Canvases

                // Fallback: skip unknown types or log warning
                _ => null  // or throw new NotSupportedException($"Cannot clone {source.GetType().Name}")
            };
        }

        /// <summary>
        /// Creates an independent copy of a brush by reconstructing it.
        /// </summary>
        private static IBrush? SafeCloneBrush(IBrush? original)
        {
            if (original == null) return null;

            switch (original)
            {
                case SolidColorBrush sb:
                    return new SolidColorBrush(sb.Color)
                    {
                        Opacity = sb.Opacity,
                        // Transform, etc. if used
                    };

                case LinearGradientBrush lgb:
                    var cloneL = new LinearGradientBrush
                    {
                        StartPoint = lgb.StartPoint,
                        EndPoint = lgb.EndPoint,
                        SpreadMethod = lgb.SpreadMethod,
                        Opacity = lgb.Opacity,
                        // MappingMode  = lgb.MappingMode,
                    };
                    foreach (var stop in lgb.GradientStops)
                    {
                        cloneL.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
                    }
                    return cloneL;

                case RadialGradientBrush rgb:
                    var cloneR = new RadialGradientBrush
                    {
                        Center = rgb.Center,
                        GradientOrigin = rgb.GradientOrigin,
                        RadiusX = rgb.RadiusX,
                        RadiusY = rgb.RadiusY,
                        SpreadMethod = rgb.SpreadMethod,
                        Opacity = rgb.Opacity,
                    };
                    foreach (var stop in rgb.GradientStops)
                    {
                        cloneR.GradientStops.Add(new GradientStop(stop.Color, stop.Offset));
                    }
                    return cloneR;

                case ImageBrush ib:
                    return new ImageBrush
                    {
                        Source = ib.Source,  // usually safe to share
                        Stretch = ib.Stretch,
                        AlignmentX = ib.AlignmentX,
                        AlignmentY = ib.AlignmentY,
                        SourceRect = ib.SourceRect,
                        Opacity = ib.Opacity,
                    };

                // Add ConicGradientBrush, DrawingBrush, etc. as needed

                default:
                    // Fallback: share reference (safe if brush is immutable & not modified later)
                    return original;
            }
        }

        /*
         * Used to render a screen out of sight so that we can
         * get the controls to size correctly
         */
        public static RenderTargetBitmap? RenderFormOffscreen(Avalonia.Controls.Control content, double? width = null, double? height = null, double dpi = 96.0)
        {
            // 1. Create invisible helper window
            var helperWindow = new Window
            {
                ShowInTaskbar = false,
                WindowState = WindowState.Minimized,  // or Normal + Opacity=0 + Position offscreen
                SizeToContent = SizeToContent.WidthAndHeight,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.None },
                Background = null,                    // optional
                Content = content
            };

            try
            {
                // 2. Critical: Show() briefly to initialize rendering / theme / layout
                //    Then hide immediately — this is the key trick
                helperWindow.Show();
                helperWindow.IsVisible = false;

                // 3. Force full layout pass (very important!)
                content.Measure(new Avalonia.Size(double.PositiveInfinity, double.PositiveInfinity));
                content.Arrange(new Avalonia.Rect(content.DesiredSize));
                content.UpdateLayout();

                // TODO - Introduce a tiny delay for complex controls


                // 4. Determine final pixel size (respect DPI/scaling)
                var scale = dpi / 96.0;
                int pixelWidth = (int)System.Math.Ceiling((width ?? content.Bounds.Width) * scale);
                int pixelHeight = (int)System.Math.Ceiling((height ?? content.Bounds.Height) * scale);

                var renderSize = new PixelSize(pixelWidth, pixelHeight);
                var bitmap = new RenderTargetBitmap(renderSize, new Vector(dpi, dpi));

                // 5. Render the content (not the window!)
                bitmap.Render(content);

                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Offscreen render failed: {ex.Message}");
                return null;
            }
            finally
            {
                helperWindow.Content = null;   // detach to avoid memory leaks
                helperWindow.Close();          // clean up
            }
        }

        /// <summary>
        /// Applies a Visual FoxPro-compatible Anchor value to any Avalonia Control on a Canvas.
        /// Supports the common absolute anchoring values (0-15):
        ///   0  = Top + Left (default VFP behavior)
        ///   1  = Top
        ///   2  = Left
        ///   3  = Top + Left
        ///   4  = Bottom
        ///   5  = Top + Bottom
        ///   6  = Left + Bottom
        ///   7  = Top + Left + Bottom
        ///   8  = Right
        ///   9  = Top + Right
        ///  10  = Left + Right
        ///  11  = Top + Left + Right
        ///  12  = Bottom + Right
        ///  13  = Top + Bottom + Right
        ///  14  = Left + Bottom + Right
        ///  15  = All four sides (control stretches with canvas)
        /// 
        /// Relative anchoring bits (16/32/64/128) are ignored in this version (they require
        /// additional SizeChanged ratio logic - let me know if you need that too).
        /// 
        /// Call this AFTER the control is added to the canvas and has its initial position/size.
        /// The layout system will then automatically keep the anchoring when the canvas resizes.
        /// </summary>
        public static void ApplyVFPAnchor(Avalonia.Controls.Control control, Canvas canvas, int vfpAnchorValue)
        {
            if (control == null || canvas == null)
                return;

            // This is wrong!  Return if 0
            if (vfpAnchorValue == 0) return;

            // Get current position and size (use attached properties first, then Bounds as fallback)
            double curLeft = Canvas.GetLeft(control);
            double curTop = Canvas.GetTop(control);
            double curRight = Canvas.GetRight(control);
            double curBottom = Canvas.GetBottom(control);

            if (double.IsNaN(curLeft)) curLeft = control.Bounds.Left;
            if (double.IsNaN(curTop)) curTop = control.Bounds.Top;
            if (double.IsNaN(curRight)) curRight = 0;
            if (double.IsNaN(curBottom)) curBottom = 0;

            double width = double.IsNaN(control.Width) || control.Width == 0
                ? control.Bounds.Width
                : control.Width;

            double height = double.IsNaN(control.Height) || control.Height == 0
                ? control.Bounds.Height
                : control.Height;

            double canvasW = double.IsNaN(canvas.Bounds.Width) || canvas.Bounds.Width == 0
                ? canvas.Width
                : canvas.Bounds.Width;

            double canvasH = double.IsNaN(canvas.Bounds.Height) || canvas.Bounds.Height == 0
                ? canvas.Height
                : canvas.Bounds.Height;

            // VFP special case: Anchor = 0 means Top + Left
            bool hasTop = ((vfpAnchorValue & 1) != 0) || (vfpAnchorValue == 0);
            bool hasLeft = ((vfpAnchorValue & 2) != 0) || (vfpAnchorValue == 0);
            bool hasBottom = (vfpAnchorValue & 4) != 0;
            bool hasRight = (vfpAnchorValue & 8) != 0;

            // Clear any attached properties we are NOT anchoring to
            if (!hasLeft) Canvas.SetLeft(control, double.NaN);
            if (!hasRight) Canvas.SetRight(control, double.NaN);
            if (!hasTop) Canvas.SetTop(control, double.NaN);
            if (!hasBottom) Canvas.SetBottom(control, double.NaN);

            // Set the anchored distances (preserving current distance to each anchored edge)
            if (hasLeft)
                Canvas.SetLeft(control, curLeft);

            if (hasRight)
                Canvas.SetRight(control, canvasW - (curLeft + width));

            if (hasTop)
                Canvas.SetTop(control, curTop);

            if (hasBottom)
                Canvas.SetBottom(control, canvasH - (curTop + height));

            // Enable automatic stretching when anchored to both opposite sides
            if (hasLeft && hasRight)
                control.Width = double.NaN;

            if (hasTop && hasBottom)
                control.Height = double.NaN;

            // If neither horizontal side is anchored, default to Left (matches VFP behavior)
            if (!hasLeft && !hasRight)
                Canvas.SetLeft(control, curLeft);

            // If neither vertical side is anchored, default to Top
            if (!hasTop && !hasBottom)
                Canvas.SetTop(control, curTop);

        }
    }
}
