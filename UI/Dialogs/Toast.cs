/* --------------------------------------------------------------------------------------------------*
 * 2026-07-29 - JLW
 * 
 * Grok put together this nice little Toast popup, so thanks to whomever first published it on
 * StackOverflow, GitHub, or their blog.
 * 
 * Produces a lightweight popup similar to:
 *   ┌───────────────┐
 *   │ Invalid entry │
 *   └───────────────┘
 *   
 * --------------------------------------------------------------------------------------------------*
 * 
 * USAGE EXAMPLES:
 * 
 * Near the current pointer, 3.5-second timeout
 *      XBase_Toast.Show("Invalid Entry");
 *
 * Explicit client coordinates (relative to the main window)
 *      XBase_Toast.Show("Record saved", new Avalonia.Point(120, 80));
 *
 * Custom duration (3 seconds)
 *      XBase_Toast.Show("Processing…", durationMs: 5000);
 * 
 * 
 * 
 * --------------------------------------------------------------------------------------------------*
 * I'm dropping these characters here in case I need them in the future.
 * 
 *      ─  │  ┌  ┐  └  ┘  ├  ┤  ┬  ┴  ┼ ╭ ╮ ╯ ╰
 * 
 *      ━  ┃  ┏  ┓  ┗  ┛  ┣  ┫  ┳  ┻  ╋
 * 
 *      ═  ║  ╔  ╗  ╚  ╝  ╠  ╣  ╦  ╩  ╬
 * 
 *      ╒  ╓  ╕  ╖  ╘  ╙  ╛  ╜  ╞  ╟  ╡  ╢  ╤  ╥  ╧  ╨  ╪  ╫
 * 
 *      ▀ ▁ ▂ ▃ ▄ ▅ ▆ ▇ █ ▉ ▊ ▋ ▌ ▍ ▎ ▏ ▐ ░ ▒ ▓ ▔ ▕ ▖ ▗ ▘ ▙ ▚ ▛ ▜ ▝ ▞ ▟
 * 
 * --------------------------------------------------------------------------------------------------*/
using Avalonia.Threading;
using JAXBase.Core;

namespace JAXBase.UI.Dialogs
{
    /// <summary>
    /// Lightweight, non-interactive popup that shows a short message near a given
    /// screen point (or the current pointer) and automatically disappears after
    /// 5 seconds or on the first mouse movement.
    /// </summary>
    public static class Toast
    {
        private static Avalonia.Controls.Primitives.Popup? _currentPopup;
        private static CancellationTokenSource? _cts;
        private static Avalonia.Controls.TopLevel? _topLevel;
        private static Avalonia.Point _lastPointerPos;

        /// <summary>
        /// Shows a toast message.
        /// </summary>
        /// <param name="message">Text to display.</param>
        /// <param name="position">
        /// Optional screen coordinates (relative to the TopLevel).  
        /// If null the popup appears at the current pointer position.
        /// </param>
        /// <param name="durationMs">Auto-close timeout in milliseconds (default 3500).</param>
        public static void Show(string message, Avalonia.Point? position = null, int durationMs = 3500)
        {
            // Always run on the UI thread
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => Show(message, position, durationMs));
                return;
            }

            Close();   // only one toast at a time

            _topLevel = Avalonia.Controls.TopLevel.GetTopLevel(
                JAXApp.MainWindowInstance as Avalonia.Controls.Control)
                ?? Avalonia.Controls.TopLevel.GetTopLevel(
                    Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.Control);

            if (_topLevel is null)
            {
                AppIO.DebugLog("XBase_Toast: no TopLevel available");
                return;
            }

            // Build the visual content entirely in code
            var textBlock = new Avalonia.Controls.TextBlock
            {
                Text = message ?? string.Empty,
                FontSize = 13,
                Foreground = Avalonia.Media.Brushes.Black,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 280
            };

            var border = new Avalonia.Controls.Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 255, 225)), // light yellow
                BorderBrush = Avalonia.Media.Brushes.Gray,
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 6),
                Child = textBlock,
                BoxShadow = new Avalonia.Media.BoxShadows(
                    new Avalonia.Media.BoxShadow
                    {
                        OffsetX = 2,
                        OffsetY = 2,
                        Blur = 6,
                        Color = Avalonia.Media.Color.FromArgb(80, 0, 0, 0)
                    })
            };

            _currentPopup = new Avalonia.Controls.Primitives.Popup
            {
                Child = border,
                IsLightDismissEnabled = false,   // we manage closing ourselves
                Placement = Avalonia.Controls.PlacementMode.Pointer, // default near pointer
                PlacementTarget = _topLevel as Avalonia.Controls.Control,
                HorizontalOffset = 12,
                VerticalOffset = 12,
                WindowManagerAddShadowHint = false
            };

            // If an explicit position was supplied, switch to absolute placement
            if (position.HasValue)
            {
                _currentPopup.Placement = Avalonia.Controls.PlacementMode.Pointer;
                _currentPopup.HorizontalOffset = position.Value.X + 12;
                _currentPopup.VerticalOffset = position.Value.Y + 12;
            }
            else
            {
                _currentPopup.Placement = Avalonia.Controls.PlacementMode.Pointer;
                _currentPopup.HorizontalOffset = 12;
                _currentPopup.VerticalOffset = 12;
            }

            // Remember starting pointer location so we can detect movement
            _lastPointerPos = position ?? GetCurrentPointerPosition(_topLevel);

            // Open
            _currentPopup.IsOpen = true;
            AppIO.DebugLog($"XBase_Toast shown: \"{message}\"");

            // Listen for any pointer movement on the TopLevel
            _topLevel.PointerMoved += OnPointerMoved;

            // Auto-close timer
            _cts = new CancellationTokenSource();
            _ = Task.Delay(durationMs, _cts.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    Dispatcher.UIThread.Post(Close);
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Force-close the current toast (if any).
        /// </summary>
        public static void Close()
        {
            if (_currentPopup is null) return;

            try
            {
                if (_topLevel is not null)
                    _topLevel.PointerMoved -= OnPointerMoved;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                _currentPopup.IsOpen = false;
                _currentPopup = null;
                _topLevel = null;

                AppIO.DebugLog("XBase_Toast closed");
            }
            catch (Exception ex)
            {
                AppIO.DebugLog($"XBase_Toast.Close error: {ex.Message}");
            }
        }

        private static void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_topLevel is null || _currentPopup is null) return;

            var current = e.GetPosition(_topLevel);

            // Close only when the pointer has actually moved a few pixels
            if (System.Math.Abs(current.X - _lastPointerPos.X) > 4 || System.Math.Abs(current.Y - _lastPointerPos.Y) > 4)
            {
                Close();
            }
        }

        private static Avalonia.Point GetCurrentPointerPosition(Avalonia.Controls.TopLevel topLevel)
        {
            // Fallback when no explicit position is given
            try
            {
                // Avalonia does not expose a global “current mouse position” API
                // in every version; returning (0,0) is safe – PlacementMode.Pointer
                // will still place near the real cursor when possible.
                return new Avalonia.Point(0, 0);
            }
            catch
            {
                return new Avalonia.Point(0, 0);
            }
        }
    }
}
