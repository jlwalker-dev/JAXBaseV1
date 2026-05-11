using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using JAXBase.Core;
using System.ComponentModel;

namespace JAXBase.UI
{
    public class FakeWindow
    {
        // Core identity & layout
        public string? Name { get; set; }
        public string Title { get; set; } = "Untitled";
        public int ShowWindow { get; set; } = 0;  // 0 = main workspace panel, 1 = nested, 2 = independent real window

        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public double Width { get; set; } = 400;
        public double Height { get; set; } = 300;

        // Icon support
        private WindowIcon? _icon;
        private Avalonia.Media.Imaging.Bitmap? _iconBitmap;

        public WindowIcon? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                _iconBitmap = null; // reset

                // Try to preserve the original bitmap if it was passed in
                if (value != null)
                {
                    // For now we rely on the caller also setting Bitmap, or load from stream later if needed
                }

                ApplyIconToRealWindow();
            }
        }

        // NEW: Public property for easy Bitmap access(recommended usage)
        public Avalonia.Media.Imaging.Bitmap? IconBitmap
        {
            get => _iconBitmap;
            set
            {
                _iconBitmap = value;
                _icon = value != null ? new WindowIcon(value) : null;
                ApplyIconToRealWindow();
            }
        }

        private void ApplyIconToRealWindow()
        {
            if (_realWindow != null)
            {
                _realWindow.Icon = _icon;
            }
        }

        // Helper to apply to floating panel (modes 0 and 1)
        private void ApplyIconToFloatingPanel()
        {
            if (_floatingPanel != null && _iconBitmap != null)
            {
                _floatingPanel.SetIcon(_iconBitmap);
            }
        }

        // Call this after creation in VFPShow(), CreateAsMainWorkspacePanel(), etc.
        private void ApplyAllVisualState()
        {
            ApplySizeConstraints();
            ApplyWindowState();
            ApplyIconToRealWindow();
            ApplyIconToFloatingPanel(); 
        }

        // Window state & buttons (VFP-like)
        public bool MinButton
        {
            get => _minButton;
            set
            {
                _minButton = value;
                _floatingPanel?.SetMinimizeButtonVisible(value);
            }
        }

        public bool MaxButton
        {
            get => _maxButton;
            set
            {
                _maxButton = value;
                _floatingPanel?.SetMaximizeButtonVisible(value);
            }
        }

        // NEW: Public read-only property for external checks
        public bool IsShown => _isShown;

        // Add private backing fields if not already present
        private bool _minButton = true;
        private bool _maxButton = true;

        public double MaxHeight { get; set; } = double.PositiveInfinity;
        public double MinHeight { get; set; } = 0;
        public double MaxWidth { get; set; } = double.PositiveInfinity;
        public double MinWidth { get; set; } = 0;

        public bool AutoCenter { get; set; } = false;

        public Avalonia.Controls.WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                ApplyWindowState();
            }
        }

        // Event handlers for when a fake window closes
        public event EventHandler<CancelEventArgs>? Closing;
        public event EventHandler? Closed;

        // Raise the events (call these before actually hiding/removing)
        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        protected virtual void OnClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        // Border style (0=no border, 1=fixed single, 2=fixed dialog, 3=sizable)
        private int _borderStyle = 3;
        public int BorderStyle
        {
            get => _borderStyle;
            set
            {
                _borderStyle = System.Math.Clamp(value, 0, 3);
                if (_realWindow != null) ApplyBorderStyleToRealWindow();
            }
        }

        private Avalonia.Controls.WindowState _windowState = Avalonia.Controls.WindowState.Normal;


        // Parent (only meaningful when ShowWindow=1)
        public FakeWindow? Parent { get; set; }

        // Internal controls
        private FloatingPanel? _floatingPanel;
        public Window? _realWindow { get; private set; } = null;
        private Canvas? _contentCanvas;
        private bool _isShown;

        public Canvas ContentCanvas
        {
            get
            {
                if (_contentCanvas != null)
                    return _contentCanvas;

                // Create a temporary canvas if accessed before real creation
                _contentCanvas = new Avalonia.Controls.Canvas
                {
                    Background = Avalonia.Media.Brushes.Transparent,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                };
                return _contentCanvas;
            }
        }

        public void VFPShow()
        {
            if (_isShown) return;

            switch (ShowWindow)
            {
                case 0:
                    CreateAsMainWorkspacePanel();
                    break;

                case 1:
                    CreateAsNestedPanel();
                    break;

                case 2:
                    CreateAsIndependentWindow();
                    break;

                default:
                    throw new InvalidOperationException($"Invalid ShowWindow value: {ShowWindow}");
            }

            _isShown = true;

            // Apply state after creation
            ApplySizeConstraints();
            ApplyWindowState();
            ApplyAllVisualState();

            if (ShowWindow == 2 && _realWindow != null)
            {
                _realWindow.WindowState = WindowState;
            }
        }

        public void VFPHide()
        {
            // TODO - I think this is where the JAXBase QueryUnload will occur for forms
            AppIO.DebugLog(">>>>>>>>>> QUERYUNLOAD? <<<<<<<<<<");

            if (!_isShown) return;
            // === Safe Closing logic for FakeWindow ===
            var cancelArgs = new System.ComponentModel.CancelEventArgs();

            OnClosing(cancelArgs);

            if (cancelArgs.Cancel)
            {
                AppIO.DebugLog("FakeWindow close canceled by OnClosing handler");
                return;   // Abort hide
            }

            if (_floatingPanel?.Parent is Canvas parentCanvas)
            {
                parentCanvas.Children.Remove(_floatingPanel);
            }
            _floatingPanel = null;

            _realWindow?.Close();
            _realWindow = null;

            _isShown = false;
            OnClosed();   // Fire Closed after everything is gone
        }

        private void CreateAsMainWorkspacePanel()
        {
            var main = JAXApp.MainWindowInstance
                ?? throw new InvalidOperationException("JAXApp.MainWindowInstance not set");

            var canvas = main._workspaceCanvas
                ?? throw new InvalidOperationException("Workspace canvas not available");

            _floatingPanel = new FloatingPanel(canvas, main.LayoutMinimizedPanels, Title)
            {
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                MaxWidth = MaxWidth == double.PositiveInfinity ? double.MaxValue : MaxWidth,
                MaxHeight = MaxHeight == double.PositiveInfinity ? double.MaxValue : MaxHeight
            };

            // Make inner canvas fill the panel (excluding title bar)
            _floatingPanel.InnerCanvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _floatingPanel.InnerCanvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            _floatingPanel.InnerCanvas.Margin = new Avalonia.Thickness(0);

            // Respect min/max buttons (FloatingPanel already has minimize/maximize logic)
            _floatingPanel.SetMinimizeButtonVisible(MinButton);
            _floatingPanel.SetMaximizeButtonVisible(MaxButton);

            Canvas.SetLeft(_floatingPanel, Left);
            Canvas.SetTop(_floatingPanel, Top);

            // Move any early-added children from placeholder to real InnerCanvas
            if (_contentCanvas != null && _contentCanvas.Children.Count > 0)
            {
                AppIO.DebugLog($"CreateAsMainWorkspacePanel: Moving {_contentCanvas.Children.Count} early children to main panel");
                var tempChildren = _contentCanvas.Children.ToList();
                foreach (var child in tempChildren)
                {
                    _contentCanvas.Children.Remove(child);
                    _floatingPanel.InnerCanvas.Children.Add(child);
                }
            }

            _contentCanvas = _floatingPanel.InnerCanvas;

            //jow.ParentAvaloniaWindow = _contentCanvas.Parent;

            _floatingPanel.InnerCanvas.ClipToBounds = true;
            _floatingPanel.InnerCanvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _floatingPanel.InnerCanvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            canvas.Children.Add(_floatingPanel);
        }

        private void CreateAsNestedPanel()
        {
            if (Parent == null)
                throw new InvalidOperationException("Parent FakeWindow required for ShowWindow=1");

            // Auto-show parent if not already visible
            if (!Parent._isShown)
                Parent.VFPShow();

            var parentInnerCanvas = Parent.ContentCanvas;

            _floatingPanel = new FloatingPanel(parentInnerCanvas, null, Title) // null layout callback = no minimize taskbar
            {
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                MaxWidth = MaxWidth == double.PositiveInfinity ? double.MaxValue : MaxWidth,
                MaxHeight = MaxHeight == double.PositiveInfinity ? double.MaxValue : MaxHeight,

                // Typical nested/child appearance: frameless, no title bar buttons
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(1),
                BorderBrush = Avalonia.Media.Brushes.Gray,
                Background = Avalonia.Media.Brushes.White,
                BoxShadow = new BoxShadows() // optional subtle shadow
            };

            // Fixed Dialog alterations (types 0,1,2)
            if (_borderStyle != 3)
            {
                if (_borderStyle != 1)  // Dialog or no borders
                {
                    if (_borderStyle == 0)
                    {
                        // No borders
                        _floatingPanel.BorderThickness = new Thickness(0);
                    }

                    // Hide title bar elements for dialog
                    _floatingPanel.SetMinimizeButtonVisible(false);
                    _floatingPanel.SetMaximizeButtonVisible(false);
                }

                // Hide grips if you want a dialog, fixed, or no borders
                _floatingPanel.SetGripsVisible(false);
            }

            Canvas.SetLeft(_floatingPanel, Left);
            Canvas.SetTop(_floatingPanel, Top);

            // CRITICAL: Move any early-added children to the real InnerCanvas
            if (_contentCanvas != null && _contentCanvas.Children.Count > 0)
            {
                var tempChildren = _contentCanvas.Children.ToList();
                AppIO.DebugLog($"CreateAsNestedPanel: Moving {tempChildren.Count} early children to child panel");

                foreach (var child in tempChildren)
                {
                    _contentCanvas.Children.Remove(child);
                    _floatingPanel.InnerCanvas.Children.Add(child);
                }
            }

            // Assign the real inner canvas
            _contentCanvas = _floatingPanel.InnerCanvas;
            _floatingPanel.InnerCanvas.ClipToBounds = true;
            _floatingPanel.InnerCanvas.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _floatingPanel.InnerCanvas.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            // Add to parent's canvas
            parentInnerCanvas.Children.Add(_floatingPanel);
        }

        private void CreateAsIndependentWindow()
        {
            _realWindow = new Avalonia.Controls.Window
            {
                Title = Title,
                Width = Width,
                Height = Height,
                MinWidth = MinWidth,
                MinHeight = MinHeight,
                MaxWidth = MaxWidth == double.PositiveInfinity ? double.MaxValue : MaxWidth,
                MaxHeight = MaxHeight == double.PositiveInfinity ? double.MaxValue : MaxHeight,
                WindowStartupLocation = AutoCenter ? WindowStartupLocation.CenterScreen : WindowStartupLocation.Manual,
                Position = new PixelPoint((int)Left, (int)Top),
                WindowState = WindowState,
                CanResize = BorderStyle == 3,
                CanMaximize = MaxButton,
                CanMinimize = MinButton,
                Background = Avalonia.Media.Brushes.White
            };

            // Apply icon right after creation
            if (_icon != null)
            {
                _realWindow.Icon = _icon;
            }

            var realContentCanvas = new Avalonia.Controls.Canvas
            {
                Background = Avalonia.Media.Brushes.Transparent
            };

            // Move early children if any
            if (_contentCanvas != null && _contentCanvas.Children.Count > 0)
            {
                AppIO.DebugLog($"CreateAsIndependentWindow: Moving {_contentCanvas.Children.Count} early children to real window canvas");

                var tempChildren = _contentCanvas.Children.ToList();

                foreach (var child in tempChildren)
                {
                    _contentCanvas.Children.Remove(child);
                    realContentCanvas.Children.Add(child);
                }
            }

            _contentCanvas = realContentCanvas;

            ApplyBorderStyleToRealWindow();

            var scrollViewer = new Avalonia.Controls.ScrollViewer
            {
                Content = ContentCanvas,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };

            _realWindow.Content = scrollViewer;
            _realWindow.Show();
        }

        private void ApplyBorderStyleToRealWindow()
        {
            if (_realWindow == null) return;

            switch (BorderStyle)
            {
                case 0:
                    _realWindow.SystemDecorations = SystemDecorations.None;
                    _realWindow.CanResize = false;
                    break;
                case 1:
                    _realWindow.SystemDecorations = SystemDecorations.Full;
                    _realWindow.CanResize = false;
                    break;
                case 2:
                    _realWindow.SystemDecorations = SystemDecorations.BorderOnly;
                    _realWindow.CanResize = false;
                    break;
                case 3:
                default:
                    _realWindow.SystemDecorations = SystemDecorations.Full;
                    _realWindow.CanResize = true;
                    break;
            }
        }

        public void ApplySizeConstraints()
        {
            if (_floatingPanel != null)
            {
                _floatingPanel.MinWidth = MinWidth;
                _floatingPanel.MinHeight = MinHeight;
                _floatingPanel.MaxWidth = MaxWidth == double.PositiveInfinity ? double.MaxValue : MaxWidth;
                _floatingPanel.MaxHeight = MaxHeight == double.PositiveInfinity ? double.MaxValue : MaxHeight;
            }

            if (_realWindow != null)
            {
                _realWindow.MinWidth = MinWidth;
                _realWindow.MinHeight = MinHeight;
                _realWindow.MaxWidth = MaxWidth == double.PositiveInfinity ? double.MaxValue : MaxWidth;
                _realWindow.MaxHeight = MaxHeight == double.PositiveInfinity ? double.MaxValue : MaxHeight;
            }
        }

        private void ApplyWindowState()
        {
            if (!_isShown) return;

            switch (ShowWindow)
            {
                case 0:
                case 1:
                    // FloatingPanel mode
                    if (_floatingPanel == null) return;

                    if (_windowState == Avalonia.Controls.WindowState.Minimized)
                    {
                        if (!_floatingPanel.IsMinimized)
                            _floatingPanel.ToggleMinimize();
                    }
                    else if (_windowState == Avalonia.Controls.WindowState.Maximized)
                    {
                        if (!_floatingPanel.IsMaximized)
                            _floatingPanel.ToggleMaximize();
                    }
                    else // Normal
                    {
                        if (_floatingPanel.IsMinimized)
                            _floatingPanel.ToggleMinimize();
                        if (_floatingPanel.IsMaximized)
                            _floatingPanel.ToggleMaximize();
                    }
                    break;

                case 2:
                    // Real window mode
                    if (_realWindow != null)
                    {
                        _realWindow.WindowState = _windowState;
                    }
                    break;
            }
        }

        public void Close() => VFPHide();
    }
}