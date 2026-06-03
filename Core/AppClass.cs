/*
 * This class is the key to the system as it holds everything
 * that is system related, such as variables, settings, ect.
 * 
 * The only way to pass information in a global/public manner 
 * is through this class.
 *          
 */
using DeftSharp.Windows.Input.Mouse;
using JAXBase.Compiler;
using JAXBase.Data;
using JAXBase.Executor;
using JAXBase.Language;
using JAXBase.Math;
using JAXBase.Utilities;
using JAXBase.XBase;
using NodaTime;
using NodaTime.TimeZones;
using System.Net;
using System.Runtime.InteropServices;
using static JAXBase.Core.JAXObjects;

namespace JAXBase.Core
{
    public class AppClass
    {
        //-------------------------------------------------------------
        // COMMAND BYTE DECLARATIONS
        //-------------------------------------------------------------

        // Literals - a literal expression is typically a string
        // like a file name or constant
        public static readonly char literalStart = (char)2;
        public static readonly char literalEnd = (char)3;

        // Header
        public static readonly char headerStartByte = (char)6;
        public static readonly char headerEndByte = (char)7;
        public static readonly char headerMapStartByte = (char)8;
        public static readonly char headerMapEndByte = (char)9;

        // Expressions
        public static readonly char expByte = (char)14;         // Exp start
        public static readonly char expParam = (char)15;        // Delimits RPN parts
        public static readonly char expEnd = (char)16;          // End of expression
        public static readonly char expDelimiter = (char)17;    // Delimits RPN expressions
        public static readonly char parameterEnd = (char)18;    // Delimits parameters

        public static readonly char stmtDelimiter = (char)20;// Statement delimiter

        // Command start/stop bytes
        public static readonly char appByte = (char)27;      // App module declaration
        public static readonly char appEnd = (char)28;       // End module declaration
        public static readonly char cmdByte = (char)29;      // Start of command
        public static readonly char cmdEnd = (char)30;       // End of a command

        public static readonly int CurrentMajorVersion = 0;  // Version Info
        public static readonly int CurrentMinorVersion = 2;

        public readonly UtilitiesLib utl;
        public readonly JAXLanguageLists lists = new();

        // Used for var assignments
        public readonly Token NullToken;

        public Dictionary<string, string> OnKeyLabel = [];
        public Dictionary<string, string> MiscInfo = [];
        public Dictionary<string, string> SysObjects = [];
        //public Dictionary<string, TryClass> TryStack = [];
        public List<ParameterClass> ParameterClassList = [];
        public List<string> CmdList = [];

        public JAXObjects.Token ReturnValue = new();
        public string RTFileName = string.Empty;

        // ON SHUTDOWN and ON ERROR
        public string OnShutDownCommand { get; set; } = string.Empty;
        public string OnErrorCommand { get; set; } = string.Empty;
        public int CurrentError = -1;
        public int InError = 0;
        public bool InErrorTrap = false;

        // DEFINE
        public string InDefine = string.Empty;
        public JAXObjectWrapper? InDefineObject = null;

        // Environment setup
        public readonly string MyInstance;

        public readonly JAXBase_Executor JaxExecutor;
        public readonly JAXBase_Compiler JaxCompiler;

        public readonly JAXMath JaxMath;
        private readonly JAXMath PrivateJaxMath;

        //public string ActiveConsole { get; private set; } = "default";
        //public Dictionary<string, JAXConsole> JAXConsoles = []; // Console windows

        public Dictionary<char, string> XRef4Runtime = []; // Convert compler byte to runtime codes
        public Dictionary<string, string> RunTimeCodes = []; // Runtime codes - Human readable runtime statement elements
        public Dictionary<string, char> CompilerXRef = [];         // Convert compiler code to byte

        //public readonly JAXSettings JaxSettings = new();
        public readonly JAXVariables JaxVariables = new();

        public JAXMediaLibrary? JaxImages = null;

        //public JAXDebugger? JaxDebugger = null;
        //public JAXDebuggerForm.DebugAction DebugAction = JAXDebuggerForm.DebugAction.None;
        public bool DebugActionConsumed = false;

        public DateTimeZone TimeZone { get { return BclDateTimeZone.FromTimeZoneInfo(TimeZoneInfo.Local); } }

        public double DefaultScaling = 1.0;
        public double SystemDPI = 96.0;

        public bool EventsAreActive = true;

        public Avalonia.Input.Key? LastKeyPressed = null;

        // ------------------------------------------------------------
        // Move direction tracking for visual objects
        // ------------------------------------------------------------
        public int LastTabIndex = 0;

        //-------------------------------------------------------------
        // DIALOG SECTION - WARNING! TOXIC ZONE
        //-------------------------------------------------------------
        public int _openDialogCount = 0;
        public int OpenDialogCount => _openDialogCount;


        // If you have a non-generic version



        public UI.Dialogs.FilePickerDialog? fileDialog = null;
        public UI.Dialogs.FolderPickerDialog? folderDialog = null;

        public JAXObjectWrapper? _jax = null;
        public XBase_Class_JAX? _jaxClass = null;

        public JAXObjectWrapper? _screen=null;
        public XBase_Class_Screen? _screenClass = null;

        //-------------------------------------------------------------
        // GLOBALS
        //-------------------------------------------------------------

        /*-----------------------------------------------------------*
         * System variables
         *-----------------------------------------------------------*/
        public enum OSType { Windows, Linux, Mac, FreeBSD, Unknown };
        public readonly bool UsesTables = true;
        public readonly string UserName = string.Empty;
        public readonly string MachineName = Environment.MachineName;
        public readonly string HostName = Dns.GetHostName();
        public readonly string ComputerName = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? Environment.MachineName;
        public readonly OSType OS = OSType.Unknown;
        public readonly string ExeFolder = string.Empty;

        public Avalonia.Controls.Window? WaitWindow = null;

        /*-----------------------------------------------------------*
         * Language settings - set commands
         * Hidden System and object settings
         * Printer and Tool settings - system _variables
         *-----------------------------------------------------------*/
        //public readonly JAXObjects JAXSettings = new(); // Settings
        public JAXObjects JAXSysObj = new();            // System vars
        public JAXObjects JAXPrtObj = new();            // Printer/tools

        // Useful path information
        public string UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\";
        public string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\";
        public string DeskTop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\";



        /*-----------------------------------------------------------*
         * Logging
         *-----------------------------------------------------------*/
        public string AppLogFile = string.Empty;
        public string AppWorkFolder = string.Empty;
        public string AppBaseFolder = string.Empty;
        public string AppTempFolder = string.Empty;

        //public int ConsoleWidth = -1;       // No console requested
        public bool Overwrite = false;      // Insert/Overwrite status
        public bool InRead = true;          // false=no, true=in read

        private int LogLife = 0; // 0=keep, -1=kill all, 1+=age in days


        public List<FileHandle> FileHandles = [];

        public List<CCodeCache> CodeCache = [];
        public Dictionary<string, CCodeCache> ClassLibs = [];
        public List<string> PRGCache = [];

        public List<ClassDef> ClassDefinitions = [];
        public string CurrentClassMethod = string.Empty;


        public List<AppLevel> AppLevels = [];
        public int CurrentAppLevel = 0;

        public bool SuspendFlag = false;      // True= suspend execution
        public bool CancelFlag = false;       // True= cancel execution
        public bool RuntimeFlag = false;      // True= running a prg
        public bool InCompile = false;        // True= in compile cmd

        // Used by the Continue command
        public LoopClass LastLocate = new();

        public List<string> WithHold = [];


        public bool CreateDebugLog = true;
        public bool ClearActiveWindow = false;


        /*-------------------------------------------------------------*
         * ERROR RECORDING/REPORTING
         *-------------------------------------------------------------*/
        public readonly List<JAXErrors> Errors = [];
        public readonly string Name = string.Empty;



        // ------------------------------------------------------------
        // Mouse and Keyboard handling
        // ------------------------------------------------------------
        public MouseListener mouseListener = new();
        //public KeyboardListener keyboardListener = new();




        /*-----------------------------------------------------------*
         * DataSession handling
         *-----------------------------------------------------------*/
        public Dictionary<int, JAXDataSession> jaxDataSession = [];
        public int CurrentDataSession { get; private set; } = 0;
        public JAXDataSession CurrentDS { get { return jaxDataSession[CurrentDataSession]; } set { } }

        public int DestroyDataSession(int datasession)
        {
            return jaxDataSession.Remove(datasession) ? 0 : 1;
        }

        public int CreateNewDataSession(string name)
        {
            int ds = 2;

            // Look for the lowest open data session number
            for (int i = 0; i < jaxDataSession.Count; i++)
            {
                if (jaxDataSession.ContainsKey(ds))
                    ds++;
                else
                    break;
            }

            // Set up the new session name
            if (string.IsNullOrEmpty(name))
                name = "*Session" + ds.ToString("000");

            // Set up the new session
            JAXDataSession dsession = new(this, name);

            // Copy the JAXSettings info from session
            // 1 and add it to the dictionary
            if (jaxDataSession.TryGetValue(1, out JAXDataSession? value))
                dsession.JaxSettings = JAXUtilities.CloneJson(value.JaxSettings) ?? new();

            jaxDataSession.Add(ds, dsession);

            return ds;
        }


        public void SetDataSession(int ds)
        {
            if (jaxDataSession.ContainsKey(ds))
                CurrentDataSession = ds;
            else
                throw new Exception(string.Format("4014|{0}", ds));
        }

        // ------------------------------------------------------------
        // Create a unique 10-character session key representing
        // the time since Jan 1, 0001 in 1/10,000 seconds
        // ------------------------------------------------------------
        private long _systemCounter = 0;

        /// <summary>
        /// Creates a unique 12 character session key for the few centuries.
        /// </summary>
        /// <param name="MaxChars"></param>
        /// <returns></returns>
        public string SystemCounter()
        {
            long t = DateTime.Now.Ticks;

            // Make sure you don't repeat because 
            // of a fast processor or short time
            // between calls for a counter value
            while (t == _systemCounter)
                t = DateTime.Now.Ticks;

            _systemCounter = t;

            utl.Conv36(_systemCounter, 12, out string p1);
            return p1;
        }


        //-------------------------------------------------------------
        // INITIALIZATION
        //-------------------------------------------------------------
        public AppClass()
        {
            utl = new(this);

            // ----------------------------------------------------------------
            // Perform some platform specific startup chores
            // ----------------------------------------------------------------
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OS = OSType.Windows;
                string[] uName = System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split("\\");

                if (uName.Length > 1)
                    UserName = uName[1].ToUpper();
                else
                    UserName = uName[0].ToUpper();
            }

            switch (OS)
            {
                case OSType.Windows:
                    break;

                case OSType.Linux:
                    break;

                case OSType.Mac:
                    break;

                case OSType.FreeBSD:
                    break;

                default: // UNKNOWN
                    break;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) OS = OSType.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) OS = OSType.Mac;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) OS = OSType.FreeBSD;

            // Set up the null token
            NullToken = new();
            NullToken.Element.MakeNull();

            // Set up the system and default data session
            jaxDataSession.Add(0, new(this, "*system"));
            jaxDataSession.Add(1, new(this, "*default"));
            CurrentDataSession = 1;

            // Set up runtime vars
            AppLevels.Add(new AppLevel());
            JaxExecutor = new();
            JaxCompiler = new(this);
            JaxMath = new();
            PrivateJaxMath = new();
            MyInstance = SystemCounter();

            // Perform the App Startup
            JAXStartup.AppStartup(this);

            AppWorkFolder = JaxVariables._WorkPath;


            // Make sure the app work folder is there
            if (string.IsNullOrWhiteSpace(AppWorkFolder) == false && Directory.Exists(AppWorkFolder) == false)
                Directory.CreateDirectory(AppWorkFolder);

            CurrentDS.JaxSettings.Default = JAXLib.Addbs(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }


        public async Task SetEnvironment()
        {
            // Set up the default log file
            try { if (Directory.Exists(JaxVariables._LogPath) == false) Directory.CreateDirectory(JaxVariables._LogPath); } catch { JaxVariables._LogPath = ""; }
            string logfile = $"System_{DateTime.Now.ToString("yyyyMMddHHmmssff")}.log";
            AppLogFile = string.Format(JaxVariables._LogPath + logfile);

            // Set up system JAXObjectWrappers
            _screen = new(this, "screen", "_screen", []);
            _screenClass = (XBase_Class_Screen)_screen.thisObject!;

            _jax = new(this, "jax", "_jax", []);
            _jaxClass = (XBase_Class_JAX)_jax.thisObject!;

            // Create system variables
            AppVars.CreateSystemVars();

            // Read the ini
            if (File.Exists(ExeFolder + "jaxbase.ini"))
            {
                bool inError = false;
                string iData = JAXLib.FileToStr(ExeFolder + "jaxbase.ini").Replace("\n", "");
                string[] iniData = iData.Split('\r');

                for (int i = 0; i < iniData.Length; i++)
                {
                    string iniLine = iniData[i].Trim();
                    if (iniLine.Length > 0)
                    {
                        if (iniLine[0].Equals(';') == false)
                        {
                            if (iniLine.Contains('='))
                            {
                                string[] iLine = iniLine.Split('=');
                                switch (iLine[0])
                                {
                                    case "logfolder":
                                        iLine[1] = JAXLib.Addbs(AppIO.FixDirectory(iLine[1].Trim()));
                                        bool logErr = false;

                                        if (string.IsNullOrEmpty(iLine[1]) == false)
                                        {
                                            if (Directory.Exists(iLine[1]))
                                                JaxVariables._LogPath = JAXLib.Addbs(iLine[1].Trim());
                                            else
                                            {
                                                try
                                                {
                                                    Directory.CreateDirectory(iLine[1]);
                                                    JaxVariables._LogPath = JAXLib.Addbs(iLine[1].Trim());
                                                }
                                                catch (Exception e)
                                                {
                                                    AppIO.DebugLog(string.Format("INI Error with {0} - {1}", iniLine, e.Message));
                                                    inError = true;
                                                    logErr = true;
                                                }
                                            }

                                            if (logErr == false)
                                            {
                                                if (File.Exists(JaxVariables._LogPath + logfile) == false)
                                                {
                                                    // Move the log file
                                                    FilerLib.MoveFile(AppLogFile, JaxVariables._LogPath + AppLogFile);
                                                    AppLogFile = JaxVariables._LogPath + logfile;
                                                }
                                            }
                                        }
                                        break;

                                    case "loglife":
                                        if (int.TryParse(iLine[1], out LogLife) == false)
                                        {
                                            LogLife = 0;
                                            AppIO.DebugLog(string.Format("INI Error with {0} - could not parse int value", iniLine));
                                            inError = true;
                                        }
                                        break;

                                    case "workfolder":
                                        iLine[1] = JAXLib.Addbs(AppIO.FixDirectory(iLine[1].Trim()));

                                        if (string.IsNullOrEmpty(iLine[1]) == false)
                                        {
                                            if (Directory.Exists(iLine[1]) == false)
                                                JaxVariables._WorkPath = JAXLib.Addbs(iLine[1].Trim());
                                            else
                                            {
                                                try
                                                {
                                                    Directory.CreateDirectory(iLine[1]);
                                                    AppWorkFolder = JAXLib.Addbs(iLine[1].Trim());
                                                }
                                                catch (Exception e)
                                                {
                                                    AppIO.DebugLog(string.Format("INI Error with {0} - {1}", iniLine, e.Message));
                                                    inError = true;
                                                }
                                            }
                                        }
                                        break;

                                    case "tempfolder":
                                        iLine[1] = JAXLib.Addbs(AppIO.FixDirectory(iLine[1].Trim()));
                                        if (string.IsNullOrEmpty(iLine[1]) == false)
                                        {
                                            if (Directory.Exists(iLine[1]) == false)
                                            {
                                                try
                                                {
                                                    Directory.CreateDirectory(iLine[1]);
                                                    AppTempFolder = JAXLib.Addbs(iLine[1].Trim());
                                                }
                                                catch (Exception e)
                                                {
                                                    AppIO.DebugLog(string.Format("INI Error with {0} - {1}", iniLine, e.Message));
                                                    inError = true;
                                                }
                                            }
                                        }
                                        break;

                                    //case "console":         // console window flag
                                    //    break;

                                    case "default":         // Set the default directory
                                        iLine[1] = JAXLib.Addbs(AppIO.FixDirectory(iLine[1].Trim()));
                                        if (string.IsNullOrEmpty(iLine[1]) == false)
                                        {
                                            if (Directory.Exists(iLine[1]))
                                            {
                                                // Set the default path
                                                CurrentDS.JaxSettings.Default = JAXLib.Addbs(iLine[1].Trim());
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    Directory.CreateDirectory(iLine[1]);
                                                    CurrentDS.JaxSettings.Default = JAXLib.Addbs(iLine[1].Trim());
                                                }
                                                catch (Exception e)
                                                {
                                                    AppIO.DebugLog(string.Format("INI Error with {0} - {1}", iniLine, e.Message));
                                                    inError = true;
                                                }
                                            }
                                        }
                                        break;

                                    default:
                                        AppIO.DebugLog(string.Format("Unknown INI command - {0}", iniLine));
                                        inError = true;
                                        break;
                                }
                            }
                        }
                    }
                }

                if (inError)
                    MessageBox.Show("Errors were detected in the jaxbase.ini file - check the log for details", "JAXBase INI Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                // Create the INI
                string newINI = "tempfolder=";
                JAXLib.StrToFile(newINI, ExeFolder + "jaxbase.ini", 0);
            }

            try { if (Directory.Exists(JaxVariables._WorkPath) == false) Directory.CreateDirectory(JaxVariables._WorkPath); } catch { JaxVariables._WorkPath = ""; }
            try { if (Directory.Exists(JaxVariables._TempPath) == false) Directory.CreateDirectory(JaxVariables._TempPath); } catch { JaxVariables._TempPath = ""; }

            // Delete log files older than x days
            try
            {
                string logPath = JAXLib.JustFullPath(AppLogFile);


                FilerLib.GetFiles(logPath, out string[] logFiles);
                for (int i = 0; i < logFiles.Length; i++)
                {
                    FilerLib.GetFileInfo(logFiles[i], out string[] fileInfo);
                    if (fileInfo.Length > 3)
                    {
                        if (DateTime.TryParse(fileInfo[2], out DateTime dt))
                        {
                            if ((LogLife > 0 && (DateTime.Now - dt).TotalDays > LogLife) || LogLife == -1)
                                FilerLib.DeleteFile(logPath + logFiles[i]);
                        }
                        else
                            AppIO.DebugLog(string.Format("Could not parse file date of {0} for {1}", fileInfo[2], logPath + logFiles[i]));
                    }
                    else
                        AppIO.DebugLog(string.Format("Could not get file info for {0}", fileInfo[2]));

                }
            }
            catch (Exception ex)
            {
                AppIO.DebugLog(string.Format("Error in Log clean up - {0}", ex.Message));
            }
        }












        /*-------------------------------------------------------------------------------------------*
         * PURPOSE:
         *      This routine is used to grab an expression from the command string and return the 
         *      remaining command string along with the expression value as a token.  This command 
         *      is expected to be used in cases where a literal is expected but may be replaced by 
         *      an expression in parenthisis.
         * 
         *      Source examples:
         * 
         *          USE (tablename)
         *      
         *          AVERAGE (exprString) ALL TO ARRAY (arrayName)
         *      
         *      This allows us to extend the XBase language by putting in (experession) instead of
         *      having to perform marco substituion all the time, which will be faster since we you
         *      need to compile macro supstitution results during execution.
         * 
         * 
         * 
         * PROCESS DESCRIPTION:
         *      Get the next expression value from the command and send out the
         *      value found as an object token and return the rest of the string
         * 
         *      Literals are in the form of:
         *          <literalStart>literalstring<literalEnd>
         *      
         *      Expressions are in the form:
         *          <expByte>expstring1<expParam>exprstring2<exprParam>exprstring3...<expEnd>
         * 
         *      Grab the string between the start and end then process accordingly.  A literal
         *      is passed back as a string, while an expression is broken into a list by <expParam> 
         *      byte and returned, typically, as a string.
         * 
         * Note:
         *      There is no error handling because we want the error to go back to the calling
         *      routine.  If we catch it here, everything will continue on with the error
         *      in play.
         *-------------------------------------------------------------------------------------------*/
        public async Task<JAXObjects.Token> SolveFromRPNString(string Command)
        {
            JAXObjects.Token answer = new();

            if (Command[0] == AppClass.literalStart)
            {
                // Process a literal, returning as a string
                if (Command[^1] != AppClass.literalEnd)
                    throw new Exception("10|SyntaxError|Mismatched literal expression");

                answer.Element.Value = Command.TrimStart(AppClass.literalStart).TrimEnd(AppClass.literalEnd);
            }
            else if (Command[0] == AppClass.expByte)
            {
                // Process an expression
                if (Command[^1] != AppClass.expEnd)
                    throw new Exception("10|Invalid expression string");

                // Break out the expressions to a list
                List<string> rpnList = [.. Command.TrimStart(AppClass.expByte).TrimEnd(AppClass.expEnd).Split(AppClass.expParam)];

                // Process the RPNList
                answer = await PrivateJaxMath.MathSolve(rpnList);
            }
            else
                throw new Exception(string.Format("10|Unknown command byte {0}", Command[0]));

            // Return the token
            return answer;
        }
    }
}
