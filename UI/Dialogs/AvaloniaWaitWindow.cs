using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace JAXBase.UI.Dialogs
{
    public class AvaloniaWaitWindow : Window
    {
        private readonly TextBlock _messageTextBlock;
        private readonly DispatcherTimer? _timeoutTimer;
        private readonly TaskCompletionSource<string> _tcs = new();
        private string _returnValue = string.Empty;

        public AvaloniaWaitWindow(string msgText, int row, int col, int timeoutSeconds)
        {
            this.SystemDecorations = SystemDecorations.None;
            this.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 192));
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.MinWidth = 180;
            this.MinHeight = 60;
            this.Padding = new Thickness(12);
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.BorderBrush = Avalonia.Media.Brushes.Gray;
            this.BorderThickness = new Thickness(1);
            this.Focusable = true;

            _messageTextBlock = new TextBlock
            {
                Text = msgText,
                TextAlignment = Avalonia.Media.TextAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 14,
                Foreground = Avalonia.Media.Brushes.Black
            };

            this.Content = _messageTextBlock;

            this.Width = System.Math.Max(200, msgText.Length * 8 + 60);
            this.Height = 60;

            var primary = Screens.Primary;
            if (primary != null)
            {
                double screenWidth = primary.WorkingArea.Width;
                this.Position = new PixelPoint((int)(screenWidth - this.Width - 30), 20);

                if (row >= 0 && col >= 0)
                {
                    this.Position = new PixelPoint((int)(40 + col * 8), (int)(40 + row * 24));
                }
            }

            this.KeyDown += OnKeyDownHandler;
            this.PointerPressed += (_, _) => { _returnValue = string.Empty; CloseWindow(); };
            _messageTextBlock.PointerPressed += (_, _) => { _returnValue = string.Empty; CloseWindow(); };

            this.Loaded += (_, _) => this.Focus();

            if (timeoutSeconds > 0)
            {
                _timeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(timeoutSeconds) };
                _timeoutTimer.Tick += (_, _) => { _returnValue = string.Empty; CloseWindow(); };
                _timeoutTimer.Start();
            }
        }

        private void OnKeyDownHandler(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or
                         Key.LeftAlt or Key.RightAlt or Key.CapsLock or Key.NumLock)
                return;

            char ch = GetCharFromKey(e.Key, e.KeyModifiers);
            _returnValue = ch != '\0' ? ch.ToString() : string.Empty;
            CloseWindow();
        }

        private static char GetCharFromKey(Key key, KeyModifiers modifiers)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                char c = (char)('a' + (key - Key.A));
                if ((modifiers & KeyModifiers.Shift) != 0) c = char.ToUpper(c);
                return c;
            }
            if (key >= Key.D0 && key <= Key.D9) return (char)('0' + (key - Key.D0));

            return key switch
            {
                Key.Enter => '\r',
                Key.Space => ' ',
                Key.Escape => '\x1B',
                _ => '\0'
            };
        }

        private void CloseWindow()
        {
            _timeoutTimer?.Stop();
            _tcs.TrySetResult(_returnValue);
            Close();
        }

        public void ShowNonBlocking() => Show();

        public Task<string> ShowAndWaitAsync()
        {
            this.Show();
            return _tcs.Task;
        }
    }
}

//using System;
//using System.Threading.Tasks;
//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Media;
//using Avalonia.Threading;

//namespace JAXBase.UI.Dialogs
//{
//    public class AvaloniaWaitWindow : Window
//    {
//        private readonly TextBlock _messageTextBlock;
//        private readonly DispatcherTimer? _timeoutTimer;
//        private string _returnValue = string.Empty;

//        public AvaloniaWaitWindow(string msgText, int row, int col, int timeoutSeconds)
//        {
//            this.SystemDecorations = SystemDecorations.None;
//            this.Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 192));
//            this.Topmost = true;
//            this.ShowInTaskbar = false;
//            this.MinWidth = 180;
//            this.MinHeight = 60;
//            this.Padding = new Thickness(12);
//            this.WindowStartupLocation = WindowStartupLocation.Manual;
//            this.BorderBrush = Avalonia.Media.Brushes.Gray;
//            this.BorderThickness = new Thickness(1);
//            this.Focusable = true;

//            _messageTextBlock = new TextBlock
//            {
//                Text = msgText,
//                TextAlignment = Avalonia.Media.TextAlignment.Center,
//                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
//                FontSize = 14,
//                Foreground = Avalonia.Media.Brushes.Black
//            };

//            this.Content = _messageTextBlock;

//            this.Width = System.Math.Max(200, msgText.Length * 8 + 60);
//            this.Height = 60;

//            // Default top-right + AT support
//            var primary = Screens.Primary;
//            if (primary != null)
//            {
//                double screenWidth = primary.WorkingArea.Width;
//                this.Position = new PixelPoint((int)(screenWidth - this.Width - 30), 20);

//                if (row >= 0 && col >= 0)
//                {
//                    this.Position = new PixelPoint((int)(40 + col * 8), (int)(40 + row * 24));
//                }
//            }

//            this.KeyDown += OnKeyDownHandler;
//            this.PointerPressed += (_, _) => { _returnValue = string.Empty; Close(); };
//            _messageTextBlock.PointerPressed += (_, _) => { _returnValue = string.Empty; Close(); };

//            this.Loaded += (_, _) => this.Focus();

//            if (timeoutSeconds > 0)
//            {
//                _timeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(timeoutSeconds) };
//                _timeoutTimer.Tick += (_, _) => { _returnValue = string.Empty; Close(); };
//                _timeoutTimer.Start();
//            }
//        }

//        private void OnKeyDownHandler(object? sender, Avalonia.Input.KeyEventArgs e)
//        {
//            if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or
//                         Key.LeftAlt or Key.RightAlt or Key.CapsLock or Key.NumLock)
//                return;

//            char ch = GetCharFromKey(e.Key, e.KeyModifiers);
//            _returnValue = ch != '\0' ? ch.ToString() : string.Empty;
//            Close();
//        }

//        private static char GetCharFromKey(Key key, KeyModifiers modifiers)
//        {
//            if (key >= Key.A && key <= Key.Z)
//            {
//                char c = (char)('a' + (key - Key.A));
//                if ((modifiers & KeyModifiers.Shift) != 0) c = char.ToUpper(c);
//                return c;
//            }
//            if (key >= Key.D0 && key <= Key.D9) return (char)('0' + (key - Key.D0));

//            return key switch
//            {
//                Key.Enter => '\r',
//                Key.Space => ' ',
//                Key.Escape => '\x1B',
//                _ => '\0'
//            };
//        }

//        public void ShowNonBlocking() => Show();

//        // Blocking version that works on UI thread
//        public string ShowAndWait()
//        {
//            this.Show();
//            // The caller will use Dispatcher.UIThread.Invoke to block cleanly
//            return _returnValue; // value is set before Close()
//        }
//    }
//}