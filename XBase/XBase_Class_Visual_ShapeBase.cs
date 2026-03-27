/*------------------------------------------------------------------------------------------*
 * Create an array that holds a png image of a shape.
 * 
 * The shape can be a line, any poly of 3 or more sides, any elipse, or
 * a complex poly created using the polypoints array.
 * 
 * Include: SixLabors.ImageSharp
 *          SixLabors.Drawing
 *          
 *------------------------------------------------------------------------------------------*/
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.Utilities.Utilities;
using ZXing;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ShapeBase : XBase_Class_Avalonia
    {
        private Avalonia.Controls.Shapes.Path shape = new();
        private List<Avalonia.Point> points = [];
        private Avalonia.Media.RotateTransform rotateTransform = new Avalonia.Media.RotateTransform(0.0);
        private Avalonia.Media.ScaleTransform scaleTransform = new Avalonia.Media.ScaleTransform(1.0, 1.0);
        private Avalonia.RelativePoint centerPoint = new Avalonia.RelativePoint(0.5, 0.5, Avalonia.RelativeUnit.Relative);

        public XBase_Class_Visual_ShapeBase(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, name, name, false, UserObject.urw);
            me.nvObject = shape;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            shape.RenderTransformOrigin = centerPoint;
            shape.RenderTransform = rotateTransform;

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
         *      9   - Success, do nothing more
         *      10  - <same as 9 for now>
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
            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;
            string objType = objtk.Element.Type;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                {

                    result = 3026;
                }
                else
                {
                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "polypoints":
                            if (me.BaseClass.Equals("line", StringComparison.OrdinalIgnoreCase))
                                result = 9;
                            else if (objtk.Element.Type.Equals("C"))
                            {
                                if (string.IsNullOrWhiteSpace(objtk.AsString()) == false)
                                {
                                    result = 2;

                                    if (objtk.AsString().Contains(" "))
                                        CreateFromMarkupString(objtk.AsString());
                                    else
                                    {
                                        objtk = await App.GetVarFromExpression(objtk.AsString(), null);

                                        // TODO - Convert the array
                                        if (objtk.TType.Equals("A") && objtk.Row > 0 && objtk.Col > 1)
                                        {
                                            // TODO
                                            points = [];
                                            for (int i = 0; i < objtk.Row; i++)
                                            {
                                                objtk.SetElement(i, 1);
                                                double x = 0;
                                                if (objtk.Element.Type.Equals("N"))
                                                {
                                                    x = objtk.AsDouble();

                                                    objtk.SetElement(i, 2);
                                                    if (objtk.Element.Type.Equals("N"))
                                                        points.Add(new(x, objtk.AsDouble()));
                                                    else
                                                        result = 11;
                                                }
                                                else
                                                    result = 11;
                                            }
                                        }
                                        else
                                            result = 1920;
                                    }
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "points":
                            if (me.BaseClass.Equals("line", StringComparison.OrdinalIgnoreCase))
                            {
                                result = 2;
                                objValue = 2;
                            }
                            else if (objtk.Element.Type.Equals("N"))
                            {
                                int x = objtk.AsInt();

                                if (x < 2)
                                    result = 41;
                                else
                                {
                                    result = 2;
                                    objValue = x;
                                }
                            }
                            break;

                        case "anchor":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 0, 15) == false) result = 41;
                            break;

                        case "fillcolor":
                            objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                            if (shape is not null)
                                shape.Fill = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor((int)objValue));
                            break;

                        case "backcolor":
                            break;

                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                            if (shape is not null)
                                shape.Stroke = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor((int)objValue));
                            break;

                        case "backstyle":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 0, 1) == false) result = 41;
                            break;

                        case "borderstyle":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 0, 6))
                            {
                                if (shape is not null)
                                {
                                    var dashArray = GetVfpDashArray(UserProperties["borderstyle"].AsInt());
                                    shape.StrokeDashArray = dashArray is null ? null : new Avalonia.Collections.AvaloniaList<double>(dashArray);
                                }
                            }
                            else
                                result = 41;
                            break;

                        case "borderwidth":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 0, 64) == false)
                                result = 41;
                            else
                            {
                                if (shape is not null)
                                    shape.StrokeThickness = UserProperties["borderwidth"].AsInt();
                            }
                            break;

                        case "curvature":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 0, 100))
                                result = 2;
                            else
                                result = 41;

                            break;

                        case "drawmode":
                            if (objtk.Element.Type.Equals("N") == false) result = 11;
                            if (JAXLib.Between(objtk.AsInt(), 1, 16) == false) result = 41;
                            break;

                        case "enabled":
                            if (shape is not null)
                            {
                                if (objtk.Element.Type.Equals("L"))
                                    shape.IsEnabled = objtk.AsBool();
                                else
                                    result = 11;
                            }
                            break;

                        case "fillstyle":
                            if (me.BaseClass.Equals("line", StringComparison.OrdinalIgnoreCase))
                                result = 9;
                            else if (objtk.Element.Type.Equals("N") == false)
                                result = 11;
                            else if (JAXLib.Between(objtk.AsInt(), 0, 7))
                            {
                                int borderThickness = UserProperties["borderwidth"].AsInt();
                                Avalonia.Media.Color fillColor = XClass_AuxCode.IntToAvColor(UserProperties["fillcolor"].AsInt());
                                Avalonia.Media.Color borderColor = XClass_AuxCode.IntToAvColor(UserProperties["bordercolor"].AsInt());

                                // === FILLSTYLE (VFP compatible) ===
                                shape.Fill = CreateFillBrush(objtk.AsInt(), fillColor);

                                // Border
                                shape.Stroke = new Avalonia.Media.SolidColorBrush(borderColor);
                                shape.StrokeThickness = borderThickness;

                                // === BORDERSTYLE (VFP compatible) ===
                                ApplyBorderStyle(shape, objtk.AsInt(), borderColor, borderThickness);
                            }
                            break;

                        case "name":
                        case "tag":
                        case "tooltiptext":
                            if (objtk.Element.Type.Equals("C") == false) result = 11;
                            break;


                        case "visible":
                            if (objtk.Element.Type.Equals("L"))
                                shape.IsVisible = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "left":
                            if (objtk.Element.Type.Equals("N"))
                                Avalonia.Controls.Canvas.SetLeft(shape, objtk.AsInt());
                            else
                                result = 11;
                            break;

                        case "top":
                            if (objtk.Element.Type.Equals("N"))
                                Avalonia.Controls.Canvas.SetTop(shape, objtk.AsInt());
                            else
                                result = 11;
                            break;

                        case "height":
                        case "width":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (objtk.AsInt() < 0)
                                    result = 41;
                                else
                                    result = 2;
                            }
                            else
                                result = 11;
                            break;

                        case "rotation":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                double x = objtk.AsDouble();
                                if (x < 0)
                                {
                                    x = System.Math.Abs(x);
                                    x = x % 360.00D;
                                    x = -x;
                                }
                                else
                                    x = x % 360.00D;

                                rotateTransform.Angle = x;
                                objValue = x;
                            }

                            break;

                        case "scale":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                double x = objtk.AsDouble();
                                if (JAXLib.Between(x, 0D, 100D))
                                {
                                    scaleTransform.ScaleX = UserProperties["scale"].AsDouble();
                                    scaleTransform.ScaleY = UserProperties["scale"].AsDouble();
                                }
                                else
                                    result = 41;
                            }
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        // Special processsing means something for the path has changed
                        if (result == 2 && InInit == false)
                        {
                            if (points.Count > 0)
                            {
                                // Polypoint drawing
                                CreatePolyPoints();
                            }
                            else
                            {
                                // Line
                                if (me.BaseClass.Equals("line", StringComparison.OrdinalIgnoreCase))
                                {
                                    UserProperties["points"].Element.Value = 2;
                                    CreateLine();
                                }
                                else
                                    CreatePolygon();
                            }
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
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * LINES
         * 
         * Calculate the line position and create the geometry string.  Then uppdate the geometry
         * along with color, thickness, and other properties.
         * 
         * ------------------------------------------------------------------------------------------*/
        public void CreateLine()
        {
            int w = UserProperties["width"].AsInt();
            int h = UserProperties["height"].AsInt();

            string pathStr;

            // This line will be placed onto the canvas at top/left coordinates
            if (UserProperties["slant"].AsString().Equals("/"))
                pathStr = $"M {0} {h} L {w} {0}";     // Slant = "/"
            else
            {
                pathStr = $"M {0} {0} L {w} {h}"; // Slant = "\"
            }

            CreateFromMarkupString(pathStr);
        }


        /* ------------------------------------------------------------------------------------------*
         * SVG Path Data syntax (also called "Path Markup Language")
         * Avalonia (and WPF, Skia, etc.) uses this exact same mini-language inside Geometry.Parse().
         * Full Breakdown (command by command)
         * Command  Meaning                 Coordinates         What it does
         * M        MoveTo (absolute)       M 450 500           Move the pen to starting point (450, 500) 
         *                                                      without drawing anything
         *                                              
         * Q        Quadratic Bézier curve  Q 550 400 650 500   Draw a curved line from current position (450,500) 
         *                                                      to (650,500) using control point (550,400)
         *                                                      
         * T        Smooth Quadratic        T 750 500           Continue the curve smoothly to point (750,500). 
         *                                                      Avalonia automatically calculates the control point 
         *                                                      by reflecting the previous one
         *                                                      
         * L        LineTo (absolute)       L 750 600           Draw a straight line down to (750,600)
         * 
         * Z        Close Path              Z                   Draw a line back to the very first 
         *                                                      point (450,500) and close the shape
         * ------------------------------------------------------------------------------------------*/
        public void CreateFromMarkupString(string svg)
        {
            try
            {
                var newdata = Avalonia.Media.Geometry.Parse(svg);
                shape.Data = newdata;
            }
            catch (Exception ex)
            {
                throw new Exception($"9104||Invalid parse data {svg}");
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * Create shape using polypoint List<Point>
         * ------------------------------------------------------------------------------------------*/
        public void CreatePolyPoints()
        {
            var figure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = true,
                Segments = new PathSegments()
            };

            for (int i = 1; i < points.Count; i++)
            {
                figure.Segments.Add(new LineSegment { Point = points[i] });
            }

            shape.Data = new PathGeometry { Figures = new PathFigures { figure } };
        }


        /* ------------------------------------------------------------------------------------------*
         * POLYGON
         * ------------------------------------------------------------------------------------------*/
        // 2026-02-21 - JLW
        //      Fairly straight forward Grok sessions.  I think Super Grok and I are starting
        //      to figure each other out.  
        // 
        /// <summary>
        /// Creates an image with a lie, polygon, rounded polygon (or circle/ellipse when curvature = 99)
        /// </summary>
        /// <param name="width">Final image width in pixels</param>
        /// <param name="height">Final image height in pixels</param>
        /// <param name="points">Number of polygon sides (3 = triangle, 4 = rect, 5+, etc.)</param>
        /// <param name="rotationDeg">Rotation in degrees (0 = one vertex at top by convention)</param>
        /// <param name="curvature">0 = sharp corners, 1–98 = increasing roundness, 99 = full circle/ellipse</param>
        /// <param name="borderColor">Color of the outline</param>
        /// <param name="borderWidth">Thickness of the border (in pixels)</param>
        /// <param name="fillColor">Interior fill color (use Color.Transparent for no fill)</param>
        /// <param name="fillStyle>">How the fill color is to be laid out</param>
        /// <returns>Avalonia.Controls.Shapes.Path you can save / assign to Canvas</returns>
        /// 
        /*
         * FillStyle                                    Border Style
         *      0 - Solid                                   0 - Transparent
         *      1 - Transparent                             1 - Solid
         *      2 - Horizontal line                         2 - Dash
         *      3 - Vertical line                           3 - Dot
         *      4 - Upward diagonal                         4 - Dash Dot
         *      5 - Downward diagonal                       5 - Dash Dot Dot
         *      6 - Cross <not supported>                   6 - Inside solid <not supported>
         *      7 - Diagonal Cross <not supported>
         */

        // ================================================
        // FULLY QUALIFIED - CURVATURE SUPPORTED (corner radius in pixels)
        // curvature = 0     → sharp straight sides (like before)
        // curvature > 0     → rounded corners using QuadraticBezierSegment
        // All code uses full namespaces - no "using" statements required
        // ================================================

        public void CreatePolygon()
        {
            double width = UserProperties["width"].AsDouble();
            double height = UserProperties["height"].AsDouble();
            int numberOfPoints = UserProperties["points"].AsInt();
            double rotationDegrees = UserProperties["rotation"].AsDouble();
            double curvature = UserProperties.ContainsKey("curvature")?UserProperties["curvature"].AsDouble():0.00D;

            shape.Width = width;
            shape.Height = height;

            double centerX = width / 2;
            double centerY = height / 2;
            double radius = System.Math.Min(width, height) / 2 * 0.92;

            double rotationRad = rotationDegrees * System.Math.PI / 180.0;

            var vertices = new System.Collections.Generic.List<Avalonia.Point>(numberOfPoints);

            for (int i = 0; i < numberOfPoints; i++)
            {
                double angle = (2 * System.Math.PI * i / numberOfPoints) + rotationRad;
                double x = centerX + radius * System.Math.Cos(angle);
                double y = centerY + radius * System.Math.Sin(angle);
                vertices.Add(new Avalonia.Point(x, y));
            }

            if (numberOfPoints < 3) numberOfPoints = 3;

            // ================================================
            // CURVATURE LOGIC
            // ================================================
            double cornerRadius = System.Math.Max(0, curvature);
            cornerRadius = System.Math.Min(cornerRadius, radius * 0.45);   // safety clamp

            var geometry = new Avalonia.Media.PathGeometry();
            var figure = new Avalonia.Media.PathFigure { IsClosed = true };
            geometry.Figures = new Avalonia.Media.PathFigures { figure };

            if (cornerRadius <= 0.5)   // sharp polygon
            {
                figure.StartPoint = vertices[0];

                for (int i = 1; i < numberOfPoints; i++)
                {
                    figure.Segments!.Add(new Avalonia.Media.LineSegment { Point = vertices[i] });
                }
            }
            else   // rounded polygon with Quadratic Bezier corners
            {
                var aPoints = new System.Collections.Generic.List<Avalonia.Point>(numberOfPoints);
                var bPoints = new System.Collections.Generic.List<Avalonia.Point>(numberOfPoints);

                for (int i = 0; i < numberOfPoints; i++)
                {
                    int prevIdx = (i + numberOfPoints - 1) % numberOfPoints;
                    int nextIdx = (i + 1) % numberOfPoints;

                    Avalonia.Point pPrev = vertices[prevIdx];
                    Avalonia.Point p = vertices[i];
                    Avalonia.Point pNext = vertices[nextIdx];

                    // Incoming direction (normalized)
                    Avalonia.Point dirIn = new Avalonia.Point(p.X - pPrev.X, p.Y - pPrev.Y);
                    double lenIn = System.Math.Sqrt(dirIn.X * dirIn.X + dirIn.Y * dirIn.Y);
                    if (lenIn > 0) dirIn = new Avalonia.Point(dirIn.X / lenIn, dirIn.Y / lenIn);

                    // Outgoing direction (normalized)
                    Avalonia.Point dirOut = new Avalonia.Point(pNext.X - p.X, pNext.Y - p.Y);
                    double lenOut = System.Math.Sqrt(dirOut.X * dirOut.X + dirOut.Y * dirOut.Y);
                    if (lenOut > 0) dirOut = new Avalonia.Point(dirOut.X / lenOut, dirOut.Y / lenOut);

                    double shorten = cornerRadius;
                    shorten = System.Math.Min(shorten, lenIn * 0.45);
                    shorten = System.Math.Min(shorten, lenOut * 0.45);

                    Avalonia.Point A = new Avalonia.Point(p.X - dirIn.X * shorten, p.Y - dirIn.Y * shorten);
                    Avalonia.Point B = new Avalonia.Point(p.X + dirOut.X * shorten, p.Y + dirOut.Y * shorten);

                    aPoints.Add(A);
                    bPoints.Add(B);
                }

                // Start at the outgoing point of the LAST corner (closes the loop)
                figure.StartPoint = bPoints[numberOfPoints - 1];

                for (int i = 0; i < numberOfPoints; i++)
                {
                    // Straight part of the side
                    figure.Segments!.Add(new Avalonia.Media.LineSegment { Point = aPoints[i] });

                    // Rounded corner (Quadratic Bezier)
                    var bezier = new Avalonia.Media.QuadraticBezierSegment
                    {
                        Point1 = vertices[i],   // control point = original vertex
                        Point2 = bPoints[i]     // end of rounding
                    };
                    figure.Segments.Add(bezier);
                }
            }

            shape.Data = geometry;
        }

        // ===================================================================
        // VFP BorderStyle handler
        // ===================================================================
        private static void ApplyBorderStyle(
            Avalonia.Controls.Shapes.Path path,
            int borderStyle,
            Avalonia.Media.Color color,
            double thickness)
        {
            if (borderStyle == 0)
            {
                path.Stroke = Avalonia.Media.Brushes.Transparent;
                return;
            }

            path.Stroke = new Avalonia.Media.SolidColorBrush(color);
            path.StrokeThickness = thickness;

            var dashArray = GetVfpDashArray(borderStyle);
            if (dashArray != null)
            {
                path.StrokeDashArray = new Avalonia.Collections.AvaloniaList<double>(dashArray);
                path.StrokeLineCap = Avalonia.Media.PenLineCap.Round;   // nicer dashes/dots
            }
            else
            {
                path.StrokeDashArray = null;   // solid
            }
        }

        private static double[]? GetVfpDashArray(int borderStyle)
        {
            switch (borderStyle)
            {
                case 2: return new double[] { 5, 3 };           // Dash
                case 3: return new double[] { 1, 3 };           // Dot
                case 4: return new double[] { 5, 2, 1, 2 };     // Dash-Dot
                case 5: return new double[] { 5, 2, 1, 2, 1, 2 }; // Dash-Dot-Dot
                default: return null;                            // Solid or unknown
            }
        }

        private Avalonia.Media.IBrush CreateFillBrush(int fillStyle, Avalonia.Media.Color color)
        {
            if (fillStyle == 1)
                return Avalonia.Media.Brushes.Transparent;

            if (fillStyle == 0 || fillStyle > 7)
                return new Avalonia.Media.SolidColorBrush(color);

            // Create small 16x16 pattern bitmap
            var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(
                new Avalonia.PixelSize(16, 16),
                new Avalonia.Vector(96, 96));

            using (var ctx = rtb.CreateDrawingContext(true))
            {
                var brush = new Avalonia.Media.SolidColorBrush(color);
                var pen = new Avalonia.Media.Pen(brush, 2);

                switch (fillStyle)
                {
                    case 2: // Horizontal
                        ctx.DrawRectangle(brush, null, new Avalonia.Rect(0, 6, 16, 4));
                        break;

                    case 3: // Vertical
                        ctx.DrawRectangle(brush, null, new Avalonia.Rect(6, 0, 4, 16));
                        break;

                    case 4: // Diagonal \
                        ctx.DrawLine(pen, new Avalonia.Point(0, 0), new Avalonia.Point(16, 16));
                        break;

                    case 5: // Diagonal /
                        ctx.DrawLine(pen, new Avalonia.Point(0, 16), new Avalonia.Point(16, 0));
                        break;

                    case 6: // Cross
                        ctx.DrawRectangle(brush, null, new Avalonia.Rect(0, 6, 16, 4));
                        ctx.DrawRectangle(brush, null, new Avalonia.Rect(6, 0, 4, 16));
                        break;

                    case 7: // Diagonal cross (X)
                        ctx.DrawLine(pen, new Avalonia.Point(0, 0), new Avalonia.Point(16, 16));
                        ctx.DrawLine(pen, new Avalonia.Point(0, 16), new Avalonia.Point(16, 0));
                        break;
                }
            }

            return new Avalonia.Media.ImageBrush
            {
                Source = rtb,
                TileMode = Avalonia.Media.TileMode.Tile,
                Stretch = Avalonia.Media.Stretch.None
            };
        }

        /*
         * Capture click event
         *
            // Position them on the canvas
            Avalonia.Controls.Canvas.SetLeft(poly1, 50);
            Avalonia.Controls.Canvas.SetTop(poly1, 50);

            Avalonia.Controls.Canvas.SetLeft(poly2, 320);
            Avalonia.Controls.Canvas.SetTop(poly2, 80);

            Avalonia.Controls.Canvas.SetLeft(poly3, 580);
            Avalonia.Controls.Canvas.SetTop(poly3, 120);

            // === CLICK HANDLERS ===
            AttachClickHandlers(poly1, "Gold Pentagon");
            AttachClickHandlers(poly2, "Green Hexagon");
            AttachClickHandlers(poly3, "Red Square (rotated)");
         
            // Add to canvas
            canvas.Children.Add(poly1);
            canvas.Children.Add(poly2);
            canvas.Children.Add(poly3);

            this.Content = canvas;

            // ===================================================================
            // Reusable click handler (works with any number of polygons)
            // ===================================================================
            private void AttachClickHandlers(Path polygon, string name)
            {
                _lastClick[polygon] = DateTime.MinValue;

                polygon.Tapped += (s, e) =>
                {
                    var now = DateTime.UtcNow;
                    var last = _lastClick[polygon];

                    if ((now - last).TotalMilliseconds < 380)
                        HandleDoubleClick((Path)s, name);
                    else
                        HandleSingleClick((Path)s, name);

                    _lastClick[polygon] = now;
                };
            }

            private void HandleSingleClick(Path p, string name)
            {
                Console.WriteLine($"[Single Click] {name}");
                // invert color example
                if (p.Fill is SolidColorBrush b)
                {
                    var c = b.Color;
                    p.Fill = new SolidColorBrush(Color.FromArgb(255, (byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B)));
                }
            }

            private void HandleDoubleClick(Path p, string name)
            {
                Console.WriteLine($"[DOUBLE CLICK] {name}");
                p.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                p.RenderTransform = new RotateTransform(45);
            }
         *
         *
         */


        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

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
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

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
                "addproperty","move","readexpression","readmethod","refresh","resettodefault",
                "saveasclass","settooriginalvalue","setfocus","saveaudio","savevideo","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error",
                "init","keypress","load",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","visiblechanged","when"
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
                "anchor,n,0",
                "backcolor,R,240|240|240","backstyle,n,1","BaseClass,C!,shape","bordercolor,R,100|100|100","borderstyle,n,0","borderwidth,n,1",
                "Class,C!,shape","ClassLibrary,C!,","Comment,C,","curvature,n,0",
                "drawmode,n,13",
                "Enabled,L,.T.",
                "fillcolor,R,0|0|0","fillstyle,n,1",
                "Height,N,50",
                "left,N,0",
                "name,c,command",
                "parent,o!,","parentclass,C!,","points,n,3","polypoints,,",
                "rotation,n,0",
                "scale,n,100","slant,C,/",
                "tag,C,","top,N,0","tooltiptext,c,",
                "visible,l,true",
                "width,N,50"
                ];

        }
    }
}
