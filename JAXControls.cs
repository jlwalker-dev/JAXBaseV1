using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase
{
    public class JAXWindow : Window
    {
        public AppClass app;
        public JAXObjectWrapper jowForm;

        // Needed for hover control
        private Avalonia.Threading.DispatcherTimer? _hoverTimer = null;
        private Avalonia.Controls.Control? _currentHoverControl = null;
        private const int HoverDelayMs = 400;   // Change this if you want faster/slower

        public JAXWindow(JAXObjectWrapper FormCanvas)
        {
            jowForm = FormCanvas;
            app = jowForm.App;

            // Basic idea, but needs more coding that just this
            this.Closing += FormClosing;
            this.SizeChanged += FormResize;
        }

        // This is called after the form is rendered
        public void AfterRender()
        {
            _hoverTimer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(HoverDelayMs)
            };

            _hoverTimer.Tick += (s, e) =>
            {
                _hoverTimer.Stop();
                if (_currentHoverControl != null)
                {
                    OnControlHover(_currentHoverControl);   // Your code runs here
                }
            };
        }

        private async void OnControlHover(Avalonia.Controls.Control control)
        {
            await jowForm.MethodCall("mousehover");
        }

        private async void FormResize(object? sender, SizeChangedEventArgs e)
        {
            await jowForm.MethodCall("resize"); // (e.NewSize.Width, e.NewSize.Height);
        }

        private async void FormClosing(object? sender, WindowClosingEventArgs e)
        {
            await jowForm.MethodCall("queryunload");
            e.Cancel = app.ReturnValue.AsBool() == false;
        }
    }


    /* -----------------------------------------------------------------------------------------*
     * -----------------------------------------------------------------------------------------*/
    /// <summary>
    /// A Panel-based label that automatically resizes to fit its text content
    /// with configurable inner padding / border-like spacing.
    /// </summary>
    /// 
    public class JAXLabel : TemplatedControl
    {
        // Text content
        public static readonly StyledProperty<string?> TextProperty =
            AvaloniaProperty.Register<JAXLabel, string?>(nameof(Text));

        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            AvaloniaProperty.Register<JAXLabel, TextWrapping>(
                nameof(TextWrapping), TextWrapping.NoWrap);

        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
            AvaloniaProperty.Register<JAXLabel, TextTrimming>(
                nameof(TextTrimming), TextTrimming.None);

        public TextTrimming TextTrimming
        {
            get => GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            AvaloniaProperty.Register<JAXLabel, TextAlignment>(
                nameof(TextAlignment), TextAlignment.Left);

        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        // Border coloring 
        public new static readonly StyledProperty<IBrush?> BorderBrushProperty =
            AvaloniaProperty.Register<JAXLabel, IBrush?>(
                nameof(BorderBrush), Avalonia.Media.Brushes.Gray);

        public new IBrush? BorderBrush
        {
            get => GetValue(BorderBrushProperty);
            set => SetValue(BorderBrushProperty, value);
        }

        public new static readonly StyledProperty<Thickness> BorderThicknessProperty =
            AvaloniaProperty.Register<JAXLabel, Thickness>(
                nameof(BorderThickness), new Thickness(1));

        public new Thickness BorderThickness
        {
            get => GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }

        // CornerRadius - now correct struct type
        public new static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<JAXLabel, CornerRadius>(
                nameof(CornerRadius), new CornerRadius(0));

        public new CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        static JAXLabel()
        {
            // Invalidate arrange when border/corner properties change
            AffectsArrange<JAXLabel>(
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty);
        }
    }



    /*
     * Border class that will allow any control to have a border
     * meaning that all classes can have the exact same border
     * that acts the exact same way.
     *
     * The interior of the border is shrunk by the thickness of the border
     * A thickness of 2 takes 4 pixels in both directions from the interior space
     * 
     * _containerBorder = new CustomBorder
     *      {
     *          BorderThickness = new Avalonia.Thickness(2),
     *          BorderBrush = Avalonia.Media.Brushes.Black,
     *          Width = 300,
     *          Height = 150
     *      };
     *
     *      var containerCanvas = new Avalonia.Controls.Canvas { Name = "ContainerCanvas" };
     *      _containerBorder.Child = containerCanvas;
     *
     *      innerCanvas.Children.Add(_containerBorder);
     *      Avalonia.Controls.Canvas.SetLeft(_containerBorder, 10);
     *      Avalonia.Controls.Canvas.SetTop(_containerBorder, 10);
     */
    public class CustomBorder : Avalonia.Controls.Decorator
    {
        private int _borderStyle = 0;

        public Avalonia.Media.IBrush BorderBrush { get; set; } = Avalonia.Media.Brushes.Black;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(2);
        public double cornerRadius { get; set; } = 0.0;
        public Avalonia.Media.IBrush Background { get; set; } = Avalonia.Media.Brushes.White;

        public CustomBorder()
        {
            ClipToBounds = true;
        }

        // Setter for border style
        public void SetBorderStyle(int bStyle)
        {
            _borderStyle = bStyle;
            InvalidateVisual();
        }

        // Create the border styles, set thickness, and color
        public override void Render(Avalonia.Media.DrawingContext context)
        {
            var background = Background;
            if (background != null)
            {
                context.FillRectangle(background, new Avalonia.Rect(Bounds.Size), 0.0F);
            }

            var borderBrush = BorderBrush;
            var borderThickness = BorderThickness;
            var thicknessAvg = (borderThickness.Left + borderThickness.Top + borderThickness.Right + borderThickness.Bottom) / 4;

            if (_borderStyle == 0 || borderBrush == null || thicknessAvg <= 0)
            {
                return;
            }

            Avalonia.Media.DashStyle? dashStyle = null;
            Avalonia.Media.PenLineCap lineCap = Avalonia.Media.PenLineCap.Flat;

            switch (_borderStyle)
            {
                case 1: // solid
                    dashStyle = null;
                    break;
                case 2: // dash
                    dashStyle = new Avalonia.Media.DashStyle(new double[] { 4, 4 }, 0);
                    break;
                case 3: // dotted 1
                    dashStyle = new Avalonia.Media.DashStyle(new double[] { 0, 2 }, 0);
                    lineCap = Avalonia.Media.PenLineCap.Round;
                    break;
                case 4: // dash dot
                    dashStyle = new Avalonia.Media.DashStyle(new double[] { 4, 4, 0, 2 }, 0);
                    lineCap = Avalonia.Media.PenLineCap.Round;
                    break;
                case 5: // dash dot dot
                    dashStyle = new Avalonia.Media.DashStyle(new double[] { 4, 4, 0, 2, 0, 2 }, 0);
                    lineCap = Avalonia.Media.PenLineCap.Round;
                    break;
                case 6: // dotted 2
                    dashStyle = new Avalonia.Media.DashStyle(new double[] { 2, 0 }, 0);
                    lineCap = Avalonia.Media.PenLineCap.Round;
                    break;
            }

            using (context.PushClip(new Avalonia.Rect(Bounds.Size)))
            {
                var halfThickness = new Avalonia.Thickness(
                    borderThickness.Left / 2,
                    borderThickness.Top / 2,
                    borderThickness.Right / 2,
                    borderThickness.Bottom / 2);
                var rect = new Avalonia.Rect(Bounds.Size).Deflate(halfThickness);
                var pen = new Avalonia.Media.Pen(borderBrush, thicknessAvg, dashStyle, lineCap, Avalonia.Media.PenLineJoin.Miter, 10);

                context.DrawRectangle(null, pen, rect, cornerRadius, cornerRadius);
            }
        }

        // Provides the new measurements of the child
        protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
        {
            var child = Child;
            if (child != null)
            {
                var padding = BorderThickness;
                child.Measure(availableSize.Deflate(padding));
                return child.DesiredSize.Inflate(padding);
            }
            return new Avalonia.Size();
        }

        // The child is arranged within a rectangle deflated by the full BorderThickness
        protected override Avalonia.Size ArrangeOverride(Avalonia.Size finalSize)
        {
            var child = Child;
            if (child != null)
            {
                var padding = BorderThickness;
                child.Arrange(new Rect(finalSize).Deflate(padding));
            }
            return finalSize;
        }
    }
}
