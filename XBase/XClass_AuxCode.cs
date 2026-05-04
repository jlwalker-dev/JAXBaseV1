/*--------------------------------------------------------------------------------------------------*
 * 2025-05-06 - JLW
 * 
 * Holds common conversion of xBase to .Net, and back, routines for 
 * the XClass properties and methods.
 * 
 *--------------------------------------------------------------------------------------------------*/
using Avalonia.Controls;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.Executer;
using JAXBase.Utilities;

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
                if (tk.Element.IsNull())
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

        /*
         * Create an array object using the rowsource and rowsource type information
         * 
         * Row Source Type
         * nValue   Description  
         * 0        None. (Default) 
         * 1        "Value1,Value2,..."
         * 2        Table alias
         * 3        SQL statement - Version 1.0
         * 4        Query (.qpr) file - Version 1.0
         * 5        Array
         * 6        Fields
         * 7        Files
         * 8        Field structure of a table
         * 9        JSON string - After Version 1.0
         * 10       Collection object - After Version 1.0
         */
        public static async Task<JAXObjects.Token> GetRowSource(AppClass app, string rowsource, int rowsourcetype)
        {
            JAXObjects.Token rowInfo = new();

            switch (rowsourcetype)
            {
                case 0:
                    // No row source
                    rowInfo.Element.Value = "";
                    break;

                case 1:
                    // Value row source
                    string[] values = rowsource.Split(",");

                    if (values.Length < 2)
                        rowInfo.Element.Value = values[0];
                    else
                    {
                        rowInfo.SetDimension(1, values.Length, true);
                        for (int i = 0; i < values.Length; i++)
                            rowInfo._avalue[i].Value = values[i];  // Field names in subsequent columns
                    }
                    break;

                case 2:
                    // Turn "TableAlias.FieldName,..." into "TableAlias"
                    string[] rsource = rowsource.Split(",");
                    string[] rstable = rsource[0].Split(".");
                    rowInfo.Element.Value = rstable;
                    break;

                case 3: break;
                case 4: break;

                case 5:
                    // Row source is an array name to be moved into rowinfo
                    rowInfo = await AppVars.GetVarToken(rowsource);
                    if (rowInfo.TType.Equals("A") == false)
                        rowInfo.Element.Value = 9999;
                    break;

                case 6:
                    // Break down "TableAlias.FieldName,FieldName2,..." into "TableAlias", "FieldName", "FieldName2", ...
                    string[] fsource = rowsource.Split(",");
                    if (fsource[0].Contains("."))
                    {
                        string[] fstable = fsource[0].Split(".");
                        rowInfo.SetDimension(1, fsource.Length + 1, true);
                        rowInfo._avalue[0].Value = fstable[0];  // Table name in the first column
                        rowInfo._avalue[1].Value = fstable[1];  // First field name in the second column

                        for (int i = 1; i < fsource.Length; i++)
                            rowInfo._avalue[i + 1].Value = fsource[i];  // Field names in subsequent columns
                    }
                    else
                        rowInfo.Element.Value = 9999;
                    break;

                case 7:
                    // Get list of files in the directory specified by rowsource and put into rowinfo
                    string dir = string.IsNullOrWhiteSpace(rowsource) ? app.CurrentDS.JaxSettings.Default : rowsource;
                    if (FilerLib.GetDirectory(dir, out string[] files) > 0)
                    {
                        int fileCount = 0;
                        for(int i = 0; i<files.Length; i++)
                        {
                            if ((files[i].Equals('.') || files[i].Equals("..")) == false)
                            {
                                fileCount++;
                                rowInfo.SetDimension(1, fileCount, true);
                                rowInfo._avalue[fileCount - 1].Value = files[i];
                            }
                        }
                    }
                    else
                    {
                        // Send back an empty array
                        rowInfo.SetDimension(1, 1, true);
                        rowInfo.TType = "A";
                        rowInfo.Element.Value = "";
                    }
                    break;

                case 8:
                    // Get field structure of the table specified by rowsource and put into rowinfo
                    break;

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

    }
}
