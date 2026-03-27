/* --------------------------------------------------------------------------------------------------------*
 * Console Window Class for JAXBase
 *      Version 1.0 will only have the DEFAULT console window.  I hope that when I get into
 *      C++ and Qt, I will be able to create a more sophisticated console window class that
 *      will allow multiple named terminal windows to be created.
 *      
 *      
 * History
 *  2025-08-14 - JLW
 *      I got most of this code from the browser's AI (my first attempt at using AI for coding)
 *      At this point, I also set up copilot to help me with coding.  So far, copilot has been 
 *      farily impressive.  It has scanned my code and learned how I code, and for the most 
 *      part, it has been able to suggest code that I like.  It usually requires some minor 
 *      adjustments, but I am impressed enough with the results that I am going to continue 
 *      using it, for now.
 *      
 *  2025-09-01 - JLW
 *      Something happened to Copilot.  It's a new month and it's not being helpful at all.
 *      Too bad, I had some hopes for it.
 *      
 * --------------------------------------------------------------------------------------------------------*/
using JAXBase.XBase;
using System.Runtime.InteropServices;

namespace JAXBase
{
    public partial class JAXConsole
    {
        // Copilot & the web suggests DllImport even though VS wants LibraryImport
        // Since I don't know the difference, I will use DllImport

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();


        [DllImport("kernel32.dll", EntryPoint = "GetConsoleWindow", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();


        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
                                                int X, int Y, int cx, int cy, uint uFlags);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;


        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public const int SW_HIDE = 0; // Constant for hiding the window
        public const int SW_SHOWNORMAL = 1; // Constant for showing the window



        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_ERROR_HANDLE = -12;

        // Used to heal the console output redirection 
        // https://stackoverflow.com/questions/15578540/allocconsole-not-printing-when-in-visual-studio
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename,
                                               [MarshalAs(UnmanagedType.U4)] uint access,
                                               [MarshalAs(UnmanagedType.U4)] FileShare share,
                                                                                 IntPtr securityAttributes,
                                               [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                                               [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
                                                                                 IntPtr templateFile);

        public const uint GENERIC_WRITE = 0x40000000;
        public const uint GENERIC_READ = 0x80000000;


        // --------------------------------------------------------------------------------------------------
        // Properties for the console window class start here
        // --------------------------------------------------------------------------------------------------
        // Remember the console window handle
        private nint intPtr;

        public int Width { get; private set; } = 80;                                      // Width of the console
        public int Height { get; private set; } = 25;                                     // Height of the console
        public bool ShowCursor { get; private set; } = true;                              // Show cursor in the console
        public bool UseColors { get; private set; } = true;                                // Use colors in the console
        public ConsoleColor ForegroundColor { get; private set; } = ConsoleColor.White;   // Foreground color
        public ConsoleColor BackgroundColor { get; private set; } = ConsoleColor.Black;   // Background color
        public int Rows { get; private set; } = 25;
        public int Cols { get; private set; } = 80;

        // Public properties to know if visible or active
        public bool visible { get; private set; } = false;
        public bool active { get; private set; } = false;

        // Constructor to initialize the console
        public JAXConsole()
        {
            AllocConsole();
            intPtr = GetConsoleWindow();
            OverrideRedirection();
            SetPosition(0, 0);           // Set initial position to (0, 0)
            Clear();
            Visible(false); // Start with the console hidden
            Console.WriteLine("JAXBase Debugger Console Initialized");
        }

        public JAXConsoleSettings ReportSettings()
        {
            JAXConsoleSettings result = new()
            {
                InPtr = this.intPtr,
                Columns = this.Cols,
                Rows = this.Rows,
                ShowCursor = this.ShowCursor,
                UseColors = this.UseColors,
                ForegroundColor = this.ForegroundColor,
                BackgroundColor = this.BackgroundColor,
                IsActive = this.active,
                IsVisible = this.visible
            };

            return result;
        }

        // Clear the console window
        public void Clear() { Console.Clear(); }


        // Write text to the console
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }


        // Set the console window position
        public void SetPosition(int left, int top)
        {
            if (intPtr != IntPtr.Zero)
            {
                // Get current window dimensions to maintain size if not specified
                // (You might need GetWindowRect for this if you want to preserve size)
                // For simplicity, we'll just set position and show the window.
                SetWindowPos(intPtr, IntPtr.Zero, left, top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
            }
        }


        // Show or hide the console window
        public void Visible(bool visible)
        {
            if (visible)
                ShowWindow(intPtr, SW_SHOWNORMAL);
            else
                ShowWindow(intPtr, SW_HIDE);
        }

        // Signals that it's ok to write debug to the console
        public void Active(bool active)
        {
            this.active = active;
            if (active)
            {
                // Bring the console to the front
                SetWindowPos(intPtr, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
            }
        }

        public void Release()
        {
            FreeConsole();
        }

        // This fixes the redirection of the console output, otherwise the console output may
        // not appear in the output window.
        // https://stackoverflow.com/questions/15578540/allocconsole-not-printing-when-in-visual-studio
        private static void OverrideRedirection()
        {
            var hOut = GetStdHandle(STD_OUTPUT_HANDLE);
            var hRealOut = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE, FileShare.Write,
                                      IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero);

            if (hRealOut != hOut)
            {
                SetStdHandle(STD_OUTPUT_HANDLE, hRealOut);
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), Console.OutputEncoding)
                { AutoFlush = true });
            }
        }
    }
}
