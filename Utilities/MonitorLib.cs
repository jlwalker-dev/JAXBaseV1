/*
 */
using Avalonia;
using Avalonia.Controls;

namespace JAXBase.Utilities
{
    public static class MonitorLib
    {

        /// <summary>
        /// Simple DTO for monitor layout information.
        /// </summary>
        public class MonitorInfo(Avalonia.Platform.Screen screen)
        {
            public string DisplayName { get; } = screen.DisplayName ?? "Unknown";
            public bool IsPrimary { get; } = screen.IsPrimary;
            public PixelRect Bounds { get; } = screen.Bounds;
            public PixelRect WorkingArea { get; } = screen.WorkingArea;
            public double Scaling { get; } = screen.Scaling;

            public override string ToString()
            {
                return $"{DisplayName} | Primary: {IsPrimary} | " +
                       $"Position: ({Bounds.X}, {Bounds.Y}) | " +
                       $"Size: {Bounds.Width}x{Bounds.Height} | " +
                       $"Scaling: {Scaling:F2}";
            }
        }


        /// <summary>
        /// Returns the number of monitors available to the system.
        /// </summary>
        /// <param name="topLevel">A TopLevel (Window, etc.) to get Screens from. Required for reliable access.</param>
        /// <returns>Number of available monitors (0 if none detected).</returns>
        public static int GetAvailableMonitorCount(TopLevel? topLevel = null)
        {
            var screens = GetScreens(topLevel);
            return screens?.ScreenCount ?? 0;
        }


        /// <summary>
        /// Returns all available Screen objects.
        /// </summary>
        public static Avalonia.Platform.Screen[] GetAllAvailableScreens(TopLevel? topLevel = null)
        {
            var screens = GetScreens(topLevel);
            if (screens?.All == null)
            {
                return [];
            } 

            return [.. screens.All];
        }


        /// <summary>
        /// Returns detailed information about each monitor including its position
        /// relative to the virtual desktop (and thus to other monitors).
        /// </summary>
        public static List<MonitorInfo> GetMonitorLayout(TopLevel? topLevel = null)
        {
            var screens = GetScreens(topLevel);
            if (screens?.All == null || !screens.All.Any())
            {
                return [];
            }

            return [.. screens.All
                .Select(s => new MonitorInfo(s))
                .OrderBy(m => m.Bounds.X)
                .ThenBy(m => m.Bounds.Y)];
        }

        /// <summary>
        /// Internal helper to safely get the Screens object.
        /// </summary>
        private static Screens? GetScreens(TopLevel? topLevel)
        {
            if (topLevel?.Screens != null)
            {
                return topLevel.Screens;
            }

            // Fallback: try to get from any known window via TopLevel.GetTopLevel if needed
            return null;
        }


        /// <summary>
        /// Returns the Screen that contains the top-left corner of the given window.
        /// </summary>
        /// <param name="window">The window to check (typically 'this' from a Window-derived class).</param>
        /// <returns>The Screen containing the window's Position, or the primary screen as fallback.</returns>
        public static Avalonia.Platform.Screen? GetScreenForWindow(Window window)
        {
            if (window == null) return null;

            // Preferred method: directly ask for the screen containing the window
            var screen = window.Screens.ScreenFromWindow(window);
            if (screen != null)
                return screen;

            // Fallback: use the exact top-left position (PixelPoint)
            var topLeft = window.Position;
            screen = window.Screens.ScreenFromPoint(topLeft);
            if (screen != null)
                return screen;

            // Final fallback: primary screen
            return window.Screens.Primary ?? null;
        }



        /// <summary>
        /// Returns whether the window's top-left corner is on the primary monitor.
        /// </summary>
        public static bool IsOnPrimaryMonitor(Window window)
        {
            var currentScreen = GetScreenForWindow(window);
            return currentScreen!.IsPrimary;
        }


        /// <summary>
        /// Gets detailed info about the monitor containing the window's top-left corner.
        /// </summary>
        public static double GetScreenInfo(Window window, string propertyName)
        {
            var screen = GetScreenForWindow(window);
            return propertyName.ToLower() switch
            {
                "left" => screen!.Bounds.TopLeft.X,
                "top" => screen!.Bounds.TopLeft.Y,
                "width" => screen!.WorkingArea.BottomRight.X - screen.WorkingArea.TopLeft.X,
                "height" => screen!.WorkingArea.BottomRight.Y - screen.WorkingArea.TopLeft.Y,
                "scaling" => screen!.Scaling*100D,
                _ => -1
            };
        }

        public static string Name(Window window) 
        {
            var screen = GetScreenForWindow(window);
            return screen!.DisplayName ?? "unknown";
        }
    }
}
