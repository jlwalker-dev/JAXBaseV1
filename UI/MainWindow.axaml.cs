/* --------------------------------------------------------------------------------------------------*
 * 2026-02-14 - JLW
 *      First attempt at replacing a WinForm control.
 *      With Grok's help, I created this form that acts as the
 *      command window for JAXBase, replacing the JAXForm object.
 *       
 * 2026-02-19 - JLW
 *      Using Avalonia is starting to make sense and Super Grok has been 
 *      a great help in understanding it, quickly.  
 *      
 *      Currently this is the MainWindow definition but is soon to become
 *      a "fake window".  I know I could also set it up to be in an actual
 *      window, separate from the main window, but think that I'll just 
 *      leave it in the JAXBase Desktop until Version 1.
 *      
 *      Ref: Super Grok Conversation - Avalonia Canvas as Dragable Floating Window
 * 
 *      Useful literals I leaned about.  You can find most of these via WinKey + period
 *      (this I did not know)
 * 
 *      "\u1F5A9"        // 🪟  (window)
 *      "\u25AD"         // ▭  (the one you were using)
 *      "\u274C"         // ❌  (another common close)
 *      "\u1F5D5"        // 🗕  (nice minimize symbol)
 *      "\u1F5D6"        // 🗖  (maximize)
 *      "\u1F5D7"        // 🗗  (restore)
 * 
 *      Holy Shnikee!  Grok just gave me a way to make a window like VFP
 *      where we can mix text and controls!!!  OMG, OMG, OMG!
 * 
 *          I can hear the screams now!  "MAKE IT A VFP CLONE!"
 * 
 *      Dude, that would push it back at least another year! And VFP
 *      is full of crap that shouldn't even be there for a modern
 *      language!
 *      
 *      Somebody recently said I need to make it very VFP compatible
 *      and then deprecate the MS-DOS and crappy stuff in future
 *      versions.
 *      
 *      Wait. What?  You want me to double the code size so I can
 *      throw it away in a couple of years?  
 *      
 *      Whatever you're smoking, I want a pound of it!
 *      
 *      ------------------------------ FACT ------------------------------
 *      I'm going to convert some of my VFP projects over to JAXBase
 *      and I'm going to pull try to the On Key Label, On Error, and 
 *      anything else that's from the last century.
 * 
 *
 * 2026-02-20 - JLW
 *      I have things pretty much fixed so moving on for now.
 *      TODO - Create IDEScreen class and tie it into MainWindow
 *
 * --------------------------------------------------------------------------------------------------*/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.UI;
using JAXBase.Utilities;
using System.ComponentModel;

namespace JAXBase
{
    public partial class MainWindow : Window
    {
        public readonly Canvas _workspaceCanvas;
        private TextBlock? _mainOutputText;
        private ScrollViewer? _mainOutputScroll;

        public readonly AppClass App;
        public JAXBase.FloatingPanel commandWindow;

        public MainWindow(AppClass app)
        {
            App = app;
            App.JaxImages = new(app);   // Init image library after Avalonia is initialized

            JAXApp.MainWindowInstance = this;
            Title = "JAXBase IDE";
            Width = 900;
            Height = 650;
            MinWidth = 400;
            MinHeight = 300;
            Background = Avalonia.Media.Brushes.White;

            _workspaceCanvas = new Canvas { Background = Avalonia.Media.Brushes.Transparent };
            Content = _workspaceCanvas;

            _workspaceCanvas.SizeChanged += (_, _) => LayoutMinimizedPanels();

            // ==================== FREE TEXT OUTPUT ON THE WORKSPACE CANVAS ====================
            _mainOutputScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,   // text wraps to window width
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Avalonia.Media.Brushes.Transparent
            };

            _mainOutputText = new TextBlock
            {
                FontFamily = new Avalonia.Media.FontFamily("Consolas, monospace"),
                FontSize = 13,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Foreground = Avalonia.Media.Brushes.DarkGreen,
                Padding = new Thickness(12, 12, 12, 40)
                // IMPORTANT: Do NOT set Text = "something" here if you're going to use Inlines
                // Initial text can be added via Inlines instead
            };

            _mainOutputText.Inlines!.Add(new Avalonia.Controls.Documents.Run(JAXLanguageLists.GetPhrase(42, Program.Version.ToString()) + "\n"));

            _mainOutputScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Background = Avalonia.Media.Brushes.Transparent,
                Content = _mainOutputText
            };

            _mainOutputScroll.Content = _mainOutputText;

            // Add FIRST so it sits behind all floating panels
            Canvas.SetLeft(_mainOutputScroll, 0);
            Canvas.SetTop(_mainOutputScroll, 0);
            _workspaceCanvas.Children.Add(_mainOutputScroll);

            // Resize to always fill the entire canvas
            _workspaceCanvas.SizeChanged += OnWorkspaceSizeChanged;

            // ==================== HOOK WINDOW CLOSING HERE ====================
            this.Closing += OnMainWindowClosing;

            // Bring up the command window (unchanged)
            commandWindow = CommandWindow.Create(app, JAXLanguageLists.GetPhrase(1));
        }


        /// <summary>
        /// Fires when the user tries to close the main IDE window
        /// </summary>
        private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            AppIO.DebugLog("MainWindow Closing event fired - Saving settings");

            // Call the save method from JAXApp
            AppIO.SaveWindowSettings().Wait();

            // Optional: If you ever want to cancel the close (e.g. unsaved changes later)
            // e.Cancel = true;
        }


        private void OnWorkspaceSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (_mainOutputScroll != null)
            {
                _mainOutputScroll.Width = _workspaceCanvas.Bounds.Width;
                _mainOutputScroll.Height = _workspaceCanvas.Bounds.Height;
            }
        }

        /// <summary>
        /// Returns reference to panel by index, null sent if index is out of bounds
        /// </summary>
        /// <param name="nameOrID"></param>
        /// <returns></returns>
        public FloatingPanel? GetPanelByIndex(int idx)
        {
            return JAXLib.Between(idx, 0, _workspaceCanvas.Children.Count - 1) ? (FloatingPanel)_workspaceCanvas.Children[idx] : null;
        }


        /// <summary>
        /// Returns reference to panel when sent the full title or id of the paenl with null sent if no match
        /// </summary>
        /// <param name="nameOrID"></param>
        /// <returns></returns>
        public FloatingPanel? GetPanelByNameOrID(string nameOrID)
        {
            foreach (FloatingPanel panel in _workspaceCanvas.Children.Cast<FloatingPanel>())
            {
                if (panel.Title.Equals(nameOrID, StringComparison.OrdinalIgnoreCase) || nameOrID.Equals(panel.Tag ?? " "))
                    return panel;
            }

            return null;
        }

        public void ClearMainOutput()
        {
            if (_mainOutputText is not null)
            {
                _mainOutputText.Inlines = [];
                //_mainOutputText.Inlines.Add("");
            }

        }

        /// <summary>
        /// Appends free text to the full-screen workspace output area.
        /// New lines appear at the bottom and the view scrolls up (old text exits the top).
        /// </summary>
        public void AppendMainOutput(string text)
        {
            if (_mainOutputText is not null && _mainOutputText.Inlines is not null && string.IsNullOrEmpty(text) == false)
            {
                // Split on \r\n, \n, or \r
                var lines = text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

                bool isFirst = _mainOutputText.Inlines.Count == 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    // Only add separator if not the very first content ever
                    if (isFirst || string.IsNullOrEmpty(lines[i]))
                    {
                        _mainOutputText.Inlines.Add(new Avalonia.Controls.Documents.LineBreak());
                    }

                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        _mainOutputText.Inlines.Add(new Avalonia.Controls.Documents.Run(lines[i]));
                    }
                }

                // Optional: crude line limiting (approximate)
                // Count runs + breaks roughly
                if (_mainOutputText.Inlines.Count > 4000)
                {
                    int removeCount = _mainOutputText.Inlines.Count - 3000;
                    for (int i = 0; i < removeCount && _mainOutputText.Inlines.Count > 0; i++)
                    {
                        _mainOutputText.Inlines.RemoveAt(0);
                    }
                }

                if (_mainOutputScroll != null)
                {
                    _mainOutputScroll.UpdateLayout();
                    _mainOutputScroll.ScrollToEnd();
                }
            }
        }


        /// <summary>
        /// Creates a new floating panel from anywhere in your app (CommandWindow, ViewModels, etc.)
        /// </summary>
        public FloatingPanel CreateFloatingPanel(string title = "Floating Panel")
        {
            var panel = new FloatingPanel(_workspaceCanvas, LayoutMinimizedPanels, title);
            panel.Tag = App.SystemCounter();

            var offset = _workspaceCanvas.Children.Count * 35;
            Canvas.SetLeft(panel, 100 + offset);
            Canvas.SetTop(panel, 90 + offset);

            _workspaceCanvas.Children.Add(panel);

            return panel;
        }

        public FloatingPanel CreateNewWindow(Canvas workspace, Action layoutMinimizedCallback)
        {
            var panel = new FloatingPanel(workspace, layoutMinimizedCallback);
            panel.Tag = App.SystemCounter();

            // Optional: stagger new panels
            var offset = _workspaceCanvas.Children.Count * 30;
            Canvas.SetLeft(panel, 80 + offset);
            Canvas.SetTop(panel, 80 + offset);

            _workspaceCanvas.Children.Add(panel);
            return panel;
        }

        public void LayoutMinimizedPanels()
        {
            if (_workspaceCanvas == null) return;

            const double gap = 8;
            const double leftMargin = 12;
            const double bottomMargin = 8;

            var minimizedPanels = _workspaceCanvas.Children
                .OfType<FloatingPanel>()
                .Where(p => p.IsMinimized)
                .OrderBy(p => _workspaceCanvas.Children.IndexOf(p))
                .ToList();

            double currentX = leftMargin;

            foreach (var panel in minimizedPanels)
            {
                panel.Height = 46;
                Canvas.SetLeft(panel, currentX);
                Canvas.SetTop(panel, _workspaceCanvas.Bounds.Height - 46 - bottomMargin);
                currentX += panel.Width + gap;
            }
        }
    }


    // ====================================================================================================
    // ====================================================================================================
    public class FloatingPanel : Border
    {
        public Avalonia.Controls.Button? MinimizeButton => _minimizeButton;
        public Avalonia.Controls.Button? MaximizeButton => _maximizeButton;

        public bool IsMinimized => _isMinimized;
        public bool IsMaximized => _isMaximized;

        // Optional: even better – methods to control visibility without exposing the Button directly
        public void SetMinimizeButtonVisible(bool visible)
        {
            if (_minimizeButton != null)
                _minimizeButton.IsVisible = visible;
        }

        public void SetMaximizeButtonVisible(bool visible)
        {
            if (_maximizeButton != null)
                _maximizeButton.IsVisible = visible;
        }

        private readonly Canvas _parentCanvas;
        private readonly Action? _layoutMinimized;
        private bool _isDragging;
        private Avalonia.Point _dragOffset;

        // Resize support
        private bool _isResizing;
        private ResizeDirection _resizeDir = ResizeDirection.None;
        private Avalonia.Point _resizeStartMousePos;
        private double _resizeStartLeft;
        private double _resizeStartTop;
        private double _resizeStartWidth;
        private double _resizeStartHeight;

        private enum ResizeDirection { None, Left, Right, Bottom, BottomLeft, BottomRight }

        // Minimum size for minimized panels
        private const double MinWinSize = 180.0;

        // Window states
        private bool _isMaximized;
        private bool _isMinimized;
        private double _restoreLeft, _restoreTop, _restoreWidth, _restoreHeight;

        private Avalonia.Controls.Button? _minimizeButton;
        private Avalonia.Controls.Button? _maximizeButton;
        private Canvas _innerCanvas;
        public Canvas InnerCanvas => _innerCanvas;

        // Resize grips
        private Border _leftGrip;
        private Border _rightGrip;
        private Border _bottomGrip;
        private Border _bottomLeftGrip;
        private Border _bottomRightGrip;

        private string _title = "Floating Panel";
        public string Title
        {
            get => _title;
            set
            {
                _title = value ?? "Floating Panel";
                // Update the title TextBlock if it already exists
                if (_titleTextBlock != null)
                    _titleTextBlock.Text = _title;
            }
        }

        private Avalonia.Controls.Image? _iconImage;
        private Avalonia.Media.Imaging.Bitmap? _iconBitmap;

        public Avalonia.Media.Imaging.Bitmap? Icon
        {
            get => _iconBitmap;
            set
            {
                _iconBitmap = value;
                if (_iconImage != null)
                {
                    _iconImage.Source = value;
                    _iconImage.IsVisible = value != null;
                }
            }
        }

        public void SetIcon(Avalonia.Media.Imaging.Bitmap? icon)
        {
            Icon = icon;
        }

        private TextBlock? _titleTextBlock;

        public void SetGripsVisible(bool visible)
        {
            _leftGrip.IsVisible = visible;
            _rightGrip.IsVisible = visible;
            _bottomGrip.IsVisible = visible;
            _bottomLeftGrip.IsVisible = visible;
            _bottomRightGrip.IsVisible = visible;
        }

        // NEW: Events for XBase event system
        public event EventHandler? WindowStateChanged;
        public event EventHandler? Activated;
        public event EventHandler? Deactivated;
        public event CancelEventHandler? QueryUnload;   // allow cancel
        public event EventHandler? Unload;

        // Raise when state changes (min/max/normal)
        protected virtual void OnWindowStateChanged()
        {
            WindowStateChanged?.Invoke(this, EventArgs.Empty);
        }

        // Raise when panel gets focus / activated
        protected virtual void OnActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }

        // Raise when loses focus / deactivated
        protected virtual void OnDeactivated()
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        // Raise before close (cancelable)
        protected virtual bool OnQueryUnload()
        {
            var args = new CancelEventArgs();
            QueryUnload?.Invoke(this, args);
            return args.Cancel;
        }

        private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Allow cancel via QueryUnload event (if wired)
            if (OnQueryUnload()) return;  // canceled by user code

            // Remove self from parent canvas
            if (_parentCanvas != null)
            {
                _parentCanvas.Children.Remove(this);
            }

            // Re-layout minimized panels if needed
            _layoutMinimized?.Invoke();

            // Fire Unload event for cleanup
            OnUnload();
        }

        // Raise after close
        protected virtual void OnUnload()
        {
            Unload?.Invoke(this, EventArgs.Empty);
        }

        public FloatingPanel(Canvas parentCanvas, Action? layoutMinimized, string title = "Floating Panel")
        {

            _parentCanvas = parentCanvas ?? throw new ArgumentNullException(nameof(parentCanvas));
            _layoutMinimized = layoutMinimized;
            Title = title;   // set the title

            Width = 320;
            Height = 240;
            MinWidth = 200;
            MinHeight = 150;

            Background = Avalonia.Media.Brushes.White;
            BorderBrush = Avalonia.Media.Brushes.DarkGray;
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(10);

            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 8,
                Blur = 20,
                Color = Avalonia.Media.Color.FromArgb(60, 0, 0, 0)
            });


            var rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var titleBar = CreateTitleBar();
            Grid.SetRow(titleBar, 0);
            Grid.SetColumnSpan(titleBar, 3);
            rootGrid.Children.Add(titleBar);

            _innerCanvas = new Canvas
            {
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(250, 250, 255)),
                Margin = new Thickness(0, 6, 0, 0)
            };
            Grid.SetRow(_innerCanvas, 1);
            Grid.SetColumn(_innerCanvas, 0);
            Grid.SetColumnSpan(_innerCanvas, 3);
            rootGrid.Children.Add(_innerCanvas);

            _leftGrip = CreateResizeGrip(StandardCursorType.SizeWestEast, ResizeDirection.Left);
            Grid.SetRow(_leftGrip, 1);
            Grid.SetColumn(_leftGrip, 0);
            rootGrid.Children.Add(_leftGrip);

            _rightGrip = CreateResizeGrip(StandardCursorType.SizeWestEast, ResizeDirection.Right);
            Grid.SetRow(_rightGrip, 1);
            Grid.SetColumn(_rightGrip, 2);
            rootGrid.Children.Add(_rightGrip);

            _bottomGrip = CreateResizeGrip(StandardCursorType.SizeNorthSouth, ResizeDirection.Bottom);
            Grid.SetRow(_bottomGrip, 1);
            Grid.SetColumn(_bottomGrip, 0);
            Grid.SetColumnSpan(_bottomGrip, 3);
            _bottomGrip.VerticalAlignment = VerticalAlignment.Bottom;
            _bottomGrip.Height = 8;
            rootGrid.Children.Add(_bottomGrip);

            _bottomLeftGrip = CreateResizeGrip(StandardCursorType.BottomLeftCorner, ResizeDirection.BottomLeft);
            Grid.SetRow(_bottomLeftGrip, 1);
            Grid.SetColumn(_bottomLeftGrip, 0);
            _bottomLeftGrip.VerticalAlignment = VerticalAlignment.Bottom;
            _bottomLeftGrip.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            _bottomLeftGrip.Width = 16;
            _bottomLeftGrip.Height = 16;
            rootGrid.Children.Add(_bottomLeftGrip);

            _bottomRightGrip = CreateResizeGrip(StandardCursorType.BottomRightCorner, ResizeDirection.BottomRight);
            Grid.SetRow(_bottomRightGrip, 1);
            Grid.SetColumn(_bottomRightGrip, 2);
            _bottomRightGrip.VerticalAlignment = VerticalAlignment.Bottom;
            _bottomRightGrip.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            _bottomRightGrip.Width = 16;
            _bottomRightGrip.Height = 16;
            rootGrid.Children.Add(_bottomRightGrip);

            Child = rootGrid;

            titleBar.PointerPressed += TitleBar_PointerPressed;
            this.PointerMoved += OnPointerMoved;
            this.PointerReleased += OnPointerReleased;
            this.PointerPressed += (_, _) => BringToFront();
        }

        private Border CreateTitleBar()
        {
            var titleBar = new Border
            {
                Background = new SolidColorBrush(Avalonia.Media.Colors.LightBlue),
                Height = 38,
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _iconImage = new Avalonia.Controls.Image
            {
                Width = 20,
                Height = 20,
                Margin = new Thickness(10, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                IsVisible = false  // hidden until an icon is set
            };

            Grid.SetColumn(_iconImage, 0);
            titleGrid.Children.Add(_iconImage);

            _titleTextBlock = new TextBlock
            {
                Text = Title,                     // ← uses the Title property
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(_titleTextBlock, 1);
            titleGrid.Children.Add(_titleTextBlock);

            _minimizeButton = new Avalonia.Controls.Button { Content = "−", Width = 28, Height = 28, Margin = new Thickness(0, 5, 4, 5), Background = Avalonia.Media.Brushes.Transparent, BorderThickness = new Thickness(0) };
            _minimizeButton.Click += (_, _) => ToggleMinimize();

            _maximizeButton = new Avalonia.Controls.Button { Content = "□", Width = 28, Height = 28, Margin = new Thickness(0, 5, 4, 5), Background = Avalonia.Media.Brushes.Transparent, BorderThickness = new Thickness(0) };
            _maximizeButton.Click += (_, _) => ToggleMaximize();

            var closeBtn = new Avalonia.Controls.Button { Content = "✕", Width = 28, Height = 28, Margin = new Thickness(0, 5, 8, 5), Background = Avalonia.Media.Brushes.Transparent, BorderThickness = new Thickness(0) };
            closeBtn.Click += CloseButton_Click;

            var buttonsStack = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            buttonsStack.Children.Add(_minimizeButton);
            buttonsStack.Children.Add(_maximizeButton);
            buttonsStack.Children.Add(closeBtn);
            Grid.SetColumn(buttonsStack, 3);
            titleGrid.Children.Add(buttonsStack);

            titleBar.Child = titleGrid;
            return titleBar;
        }

        // === The rest of the class (CreateResizeGrip, GripPointerPressed, BringToFront, 
        // OnPointerMoved, StopResizing, OnPointerReleased, TitleBar_PointerPressed, 
        // ToggleMinimize, ToggleMaximize) stays exactly the same as in the previous version ===

        private Border CreateResizeGrip(StandardCursorType cursorType, ResizeDirection direction)
        {
            var grip = new Border { Background = Avalonia.Media.Brushes.Transparent, Cursor = new Avalonia.Input.Cursor(cursorType) };
            grip.PointerPressed += (s, e) => GripPointerPressed(e, direction);
            return grip;
        }

        private void GripPointerPressed(PointerPressedEventArgs e, ResizeDirection dir)
        {
            if (_isMaximized || _isMinimized) return;

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isResizing = true;
                _resizeDir = dir;
                _resizeStartLeft = Canvas.GetLeft(this);
                _resizeStartTop = Canvas.GetTop(this);
                _resizeStartWidth = Width;
                _resizeStartHeight = Height;
                _resizeStartMousePos = e.GetPosition(_parentCanvas);
                e.Pointer.Capture(this);
                BringToFront();
                e.Handled = true;
            }
        }

        private void BringToFront()
        {
            if (_parentCanvas == null) return;

            int maxZ = 0;
            foreach (var child in _parentCanvas.Children)
            {
                if (child is Visual visual)
                    maxZ = System.Math.Max(maxZ, visual.ZIndex);
            }
            this.ZIndex = maxZ + 1;
            OnActivated();
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_parentCanvas == null) return;

            if (_isDragging)
            {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    _isDragging = false;
                    e.Pointer.Capture(null);
                    return;
                }

                var currentPos = e.GetPosition(_parentCanvas);
                Canvas.SetLeft(this, currentPos.X - _dragOffset.X);
                Canvas.SetTop(this, currentPos.Y - _dragOffset.Y);
            }
            else if (_isResizing)
            {
                if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    StopResizing(e);
                    return;
                }

                var currentPos = e.GetPosition(_parentCanvas);
                var deltaX = currentPos.X - _resizeStartMousePos.X;
                var deltaY = currentPos.Y - _resizeStartMousePos.Y;

                if (_resizeDir == ResizeDirection.Left || _resizeDir == ResizeDirection.BottomLeft)
                {
                    double newWidth = _resizeStartWidth - deltaX;
                    if (newWidth >= MinWidth)
                    {
                        Width = newWidth;
                        Canvas.SetLeft(this, _resizeStartLeft + deltaX);
                    }
                }
                else if (_resizeDir == ResizeDirection.Right || _resizeDir == ResizeDirection.BottomRight)
                {
                    double newWidth = _resizeStartWidth + deltaX;
                    if (newWidth >= MinWidth)
                        Width = newWidth;
                }

                if (_resizeDir == ResizeDirection.Bottom || _resizeDir == ResizeDirection.BottomLeft || _resizeDir == ResizeDirection.BottomRight)
                {
                    double newHeight = _resizeStartHeight + deltaY;
                    if (newHeight >= MinHeight)
                        Height = newHeight;
                }
            }
        }

        private void StopResizing(PointerEventArgs e)
        {
            _isResizing = false;
            _resizeDir = ResizeDirection.None;
            e.Pointer.Capture(null);
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDragging) _isDragging = false;
            if (_isResizing) StopResizing(e);
            else e.Pointer.Capture(null);
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_isMaximized || _isMinimized) return;

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _dragOffset = e.GetPosition(this);
                e.Pointer.Capture(this);
                BringToFront();
                e.Handled = true;
            }
        }

        public void ToggleMinimize()
        {
            if (_parentCanvas == null) return;
            if (_isMaximized) ToggleMaximize();

            if (!_isMinimized)
            {
                _restoreLeft = Canvas.GetLeft(this);
                _restoreTop = Canvas.GetTop(this);
                _restoreWidth = Width;
                _restoreHeight = Height;

                _isMinimized = true;
                Width = MinWinSize;
                Height = 46;
                _innerCanvas.IsVisible = false;
                _minimizeButton!.Content = "▭";

                _leftGrip.IsVisible = _rightGrip.IsVisible = _bottomGrip.IsVisible = _bottomLeftGrip.IsVisible = _bottomRightGrip.IsVisible = false;

                _layoutMinimized?.Invoke();
            }
            else
            {
                Canvas.SetLeft(this, _restoreLeft);
                Canvas.SetTop(this, _restoreTop);
                Width = _restoreWidth;
                Height = _restoreHeight;
                _innerCanvas.IsVisible = true;
                _isMinimized = false;
                _minimizeButton!.Content = "−";

                _leftGrip.IsVisible = _rightGrip.IsVisible = _bottomGrip.IsVisible = _bottomLeftGrip.IsVisible = _bottomRightGrip.IsVisible = true;

                if (_layoutMinimized is not null)
                    _layoutMinimized?.Invoke();
            }
            BringToFront();
            OnWindowStateChanged();
        }

        public void ToggleMaximize()
        {
            if (_parentCanvas == null) return;
            if (_isMinimized) ToggleMinimize();

            if (!_isMaximized)
            {
                _restoreLeft = Canvas.GetLeft(this);
                _restoreTop = Canvas.GetTop(this);
                _restoreWidth = Width;
                _restoreHeight = Height;

                Canvas.SetLeft(this, 0);
                Canvas.SetTop(this, 0);
                Width = _parentCanvas.Bounds.Width;
                Height = _parentCanvas.Bounds.Height;

                _isMaximized = true;
                _maximizeButton!.Content = "❐";

                _leftGrip.IsVisible = _rightGrip.IsVisible = _bottomGrip.IsVisible = _bottomLeftGrip.IsVisible = _bottomRightGrip.IsVisible = false;
            }
            else
            {
                Canvas.SetLeft(this, _restoreLeft);
                Canvas.SetTop(this, _restoreTop);
                Width = _restoreWidth;
                Height = _restoreHeight;

                _isMaximized = false;
                _maximizeButton!.Content = "□";

                _leftGrip.IsVisible = _rightGrip.IsVisible = _bottomGrip.IsVisible = _bottomLeftGrip.IsVisible = _bottomRightGrip.IsVisible = true;
            }

            if (_layoutMinimized is not null)
                _layoutMinimized?.Invoke();
            BringToFront();
            OnWindowStateChanged();
        }
    }
}