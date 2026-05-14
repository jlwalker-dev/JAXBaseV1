using Avalonia.Input;
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Windows.Navigation;

namespace JAXBase.XBase
{
    /* 
     * This is a generic class used to pass information around
     * in this Dog-Foresaken async hell
     */
    public class GenericClass
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public bool Enabled = false;
        public int Error = 0;
        public string Header = string.Empty;
        public string cmdRest = string.Empty;
        public string Status = string.Empty;
        public string Tag = string.Empty;
        public string Type = string.Empty;
        public int Precision = 0;
        public JAXObjects.Token Value = new();
        public JAXObjects.Token Result = new();
        public bool Visible = false;
        public int Width = 0;
    }


    // Image loads as IImage, AV loads as memorystream to Media while
    // both can place Var/URL/URI strings into the Media object.
    // SubTypes: F=Loaded IImage/memorystream, N=Local File Name, L=URL, I=URI, T=Token Var name
    public class MediaEntry
    {
        public char Type = 'I';
        public string SubType = "";
        public string FileName = "";
        public object? Media = null;
    }


    public class KeyClass
    {
        public int iKey = 0;
        public Key aKey = Key.None;
        public string keyLabel = "";
        public bool CTRL = false;
        public bool ALT = false;
        public bool SHIFT = false;
    }

    /*
     * The editor returns this class so the calling program
     * knows what the user wants/did
     */
    public class JAXEditorResult
    {
        public string Command = string.Empty;
        public int ErrorNo = 0;
        public string ErrorMsg = string.Empty;
        public string Name = string.Empty;
        public string Text = string.Empty;
        public string Type = string.Empty;
    }


    public class WebHistory
    {
        public string Content = "";
        public readonly DateTime DateVisited = DateTime.Now;
        public string URL = "";
        public int Status = 0;
    }


    public class JAXErrors
    {
        public int ErrorNo = 0;
        public int ErrorLine = 0;
        public string ErrorMessage = string.Empty;
        public string ErrorSource = string.Empty;
        public string ErrorProcedure = string.Empty;
    }


    /*-----------------------------------------------------------*
     * File Handling
     *-----------------------------------------------------------*/
    public class FileHandle
    {
        public int Attributes = 0;
        public string FilePath = string.Empty;
        public FileStream? Stream = null;
        public BinaryWriter? SWriter = null;
        public BinaryReader? SReader = null;
    }

    /*-----------------------------------------------------------*
         * Compiled code cache - files are loaded into here first 
         * when read from a file.  They stay in memory until CLEARed 
         * or the program terminates.  If a procedure or function is 
         * found during the load, it is broken out into it's own 
         * codecache entry. FileStem is given the file name stem 
         * for each entry
         *-----------------------------------------------------------*/
    public class CCodeCache
    {
        public string Type = string.Empty;              // code type: Prg, Scx, etc
        public string FQFN = string.Empty;              // Fully Qualified File Name
        public string Name = string.Empty;              // File stem or Procedure Name
        public string FileStem = string.Empty;          // Name of parent file
        public float Version = 0.0F;                    // Compiler Version
        public string MD5 = string.Empty;               // Checksum
        public string SourceFile = string.Empty;        // Source file name
        public DateTime CompileDT = DateTime.MinValue;  // Date compiled
        public string StartProc = string.Empty;         // Starting Procedure
        public Dictionary<string, int> Procedures = []; // Procedure pointers
        public Dictionary<string, int> Classes = [];    // Class definition pointers
    }

    /*-----------------------------------------------------------*
     * Class Definition Container
     *-----------------------------------------------------------*/
    public class ClassDef
    {
        public string Name = string.Empty;
        public string ParentClass = string.Empty;
        public string ClassLibrary = string.Empty;
        public string PropertyCode = string.Empty;
        public Dictionary<string, ClassMethod> methods = [];
    }

    public class ClassMethod
    {
        public string SourceCode = string.Empty;
        public string ObjectCode = string.Empty; // Compiled code
        public bool Protected = false;
    }

    // FOR and SCAN loop information because once the loop
    // is started, the parameters aren't allowed to change.
    // Also used by the TryLoop stack
    public class LoopClass
    {
        public string VarName = string.Empty;   // Try code
        public double EndValue = 0.0D;
        public double StepValue = 0.0D;
        public int DataSession = 0;
        public int WorkArea = 0;
        public int RecordCounter = 0;           // Try phase
        public JAXScope? Scope = null;
    }


    /// <summary>
    /// Loop class: TryPhase 1=In Try, 2=Looking for Catch, 3=In Catch, 4=In Finally
    /// </summary>
    public class TryClass
    {
        public string Code = string.Empty;
        public int TryPhase = 0;
        public int Level = -1;
        public int PrgPos = -1;
        public List<int> CasePos = [];
        public int FinallyPos = -1;
        public int EndTry = -1;
    }


    // Used for data binding
    public class ListObjectCollection
    {
        public int ItemDataID = 0;
        public JAXObjects.Token ItemRow;

        public ListObjectCollection(int col, int id)
        {
            // Create the array row
            ItemRow = new()
            {
                Col = col,
                TType = "A"
            };

            // Populate the array row
            for (int i = 1; i < col; i++)
                ItemRow._avalue.Add(new());

            // Set the itemID value
            ItemDataID = id;
        }
    }

    /*
     * This class is used STRICTLY for passing parameters by reference or value
     * such as DO prg WITH var
     * 
     * Type:    P - PrivateVars (Public if Level=0) - ByRef
     *          L - LocalVars - By Reference
     *          M - Math string (such as "(4+3)*7")
     *          N - Named parameter (name in PName) with Token value
     *          R - RPN Expression - By Value
     *          V - Var Expression to search - By Value
     *          T - Token value
     *          X - .NULL. value
     *
     * Level:   0 = These PrivateVars are the Public vars
     *         >0 = AppLevels[] it was found
     *          Ignore if Type R, T, or V
     * 
     * PName:   Name of parameter
     * 
     * RefVal:  RPN Expression must start with ExprByte
     *          Var Expression is a string like J or b[a+7,3]
     *          and is resolved before adding to the list
     * 
     * VName:   Variable Name
     *         
     * Parameter class allows passing parameters by value or
     * by reference.  If Type="V" then level is ignored and
     * the RevVal holds the value, array, or object.
     * 
     * If Type="R" then it's an RPN Expression in RefVal
     * 
     * If Type="P" or "L" then it points to the variable
     * that is being passed by reference and the vairable
     * name is stored in RefVal as a character type.
     */
    public class ParameterClass
    {
        public string Type = "T";
        public int Level = 0;
        public string RefVal = string.Empty;
        public string PName = string.Empty;
        public string VName = string.Empty;
        public JAXObjects.Token token = new();

        public ParameterClass() { }

        public ParameterClass(object? obj)
        {
            if (obj is JAXObjects.Token)
                token.CopyFrom((JAXObjects.Token)obj);
            else if (obj is null)
                token.Element.MakeNull();
            else
                token.Element.Value = obj;
        }
    }

    /*-----------------------------------------------------------*
     * Each PRG is assigned a level that holds information for 
     * that instance including private variables that are 
     * instantiated here and any local variables created in 
     * that app.  Further, unlike many xBase systems, the 
     * only limit in JAX is memory.
     /*-----------------------------------------------------------*/
    public class AppLevel
    {
        public string PrgName = string.Empty;       // Program Name
        public string Path = string.Empty;          // Path of program
        public string PrgType = string.Empty;       // Prg, Form
        public string Instance = string.Empty;

        public int CurrentLine = -1;                // Current executing line
        public int FileLine = -1;
        public int StartLine = -1;
        public int CallingLevel = -1;               // Used to return back to the correct level

        public int LastCommand = -1;
        public string CurrentLineOfCode = string.Empty;

        public JAXObjects LocalVars = new();        // Local Vars
        public JAXObjects PrivateVars = new();      // Private Vars

        // User defined objects created via DEFINE.  Other objects
        // are attached to variables and don't show up here
        public Dictionary<string, JAXObjectWrapper> UserObjects = [];

        public JAXObjectWrapper? ThisObject = null;
        public string ThisObjectMethod = string.Empty;
        public bool DoDefault = true;               // DoDefault flag

        public int LoopCounter = 0;
        public List<string> LoopStack = [];         // Loop stacks
        public Dictionary<string, LoopClass> ForLoops = [];
        public Dictionary<string, LoopClass> ScanLoops = [];
        public List<TryClass> TryStack = [];
        public List<string> WithStack = [];         // With statements

        public int CodeCacheIDX = -1;
        public string CodeCacheName = string.Empty; // Dictionary key
        public string Procedure = string.Empty;     // Procedure name
        public int PRGCacheIdx = 0;                 // Array index
        public int PrgPos = 0;                      // Byte position in code
        public string PrgCode = string.Empty;       // Method code

        //-------------------------------------------------------------
        // Read Events hook
        //-------------------------------------------------------------
        // Needed for read events
        public bool InReadEvents = false;


        // Keeps track of current line and command being executed
        public void DebugUpdate(int line, string command)
        {
            CurrentLine = line;
            CurrentLineOfCode = command;
        }

        public void Clear(bool all)
        {
            PrgName = string.Empty;
            Path = string.Empty;
            PrgType = string.Empty;
            CurrentLine = 0;
            CurrentLineOfCode = string.Empty;
            ThisObject = null;
            ThisObjectMethod = string.Empty;
            DoDefault = true;
            LoopCounter = 0;
            LoopStack = [];
            ForLoops = [];
            WithStack = [];
            CodeCacheIDX = -1;
            CodeCacheName = string.Empty;
            Procedure = string.Empty;
            PRGCacheIdx = 0;
            PrgPos = 0;

            if (all)
            {
                LocalVars = new();
                PrivateVars = new();
                UserObjects = [];
            }
        }
    }

    /* ------------------------------------------------------------*
     * Code handling routines
     * ------------------------------------------------------------*/

    public class ExtensionTypes
    {
        public string SourceTable = string.Empty;
        public string MemoFile = string.Empty;
        public string SourceCode = string.Empty;
        public string CompiledCode = string.Empty;
        public string VFPTable = string.Empty;

        public bool IsJAXCodeExtension(string ext)
        {
            return JAXLib.InListC(ext, SourceTable, SourceCode, CompiledCode);
        }
    }


    /* ------------------------------------------------------------*
     * Type (P=prg, F=form, C=Class Library, etc)
     * JAXLib.JustStem(fName)
     * CurrentMajorVersion.CurrentMinorVersion
     * fName
     * MD5
     * DateTime Compiled
     * startingProc
     * ------------------------------------------------------------*/
    public class FileHeader
    {
        public string Type = string.Empty;
        public string Stem = string.Empty;
        public float CompilerVersion = 0.0F;
        public string SourceFQFN = string.Empty;
        public string MD5 = string.Empty;
        public DateTime CompiledAt = DateTime.MinValue;
        public string StartingProc = string.Empty;
    }

    public class JAXConsoleSettings
    {
        public nint InPtr = 0;             // Windows pointer
        public string Name = string.Empty; // Name of the console
        public int Columns = 80;           // Columns in console
        public int Rows = 25;              // Rows in console
        public int Width = 0;              // Width of the window
        public int Height = 0;             // Height of the window
        public bool ShowCursor = true;     // Show cursor in console
        public bool UseColors = true;      // Use colors in console
        public ConsoleColor ForegroundColor = ConsoleColor.White;
        public ConsoleColor BackgroundColor = ConsoleColor.Black;
        public bool IsVisible = true;      // Is visible?
        public bool IsActive = false;      // Active receives output
    }

    /*----------------------------------------------------------------------------------------------*
     * Executer Code Class section
     * 
     * The ExecuterCodes class contains the information extracted from the pcode for a command.
     * Properties in ALLCAPS are already processed and are just a value.  The rest typically
     * require you to resolve their values when you need them as they may rely on data from
     * a table in a different datasession/work area.
     *----------------------------------------------------------------------------------------------*/
    public class ExCodeAt { public int row = -1; public int col = -1; };
    public class ExCodeName { public string Type = string.Empty; public string Name = string.Empty; }
    public class ExCodeRPN { public string Type = string.Empty; public string RNPExpr = string.Empty; }
    public class ExCodeScope { public string Type = string.Empty; public int Count = 0; }


    public class ExecuterCodes
    {
        public string ALIAS = string.Empty;
        public List<string> As = [];
        public ExCodeAt At = new();
        public string BLANK = string.Empty;
        public string CLASS = string.Empty;
        public string COLLATE = string.Empty;
        public int CODEPAGE = 0;
        public string COMMAND = string.Empty;
        public string DATABASE = string.Empty;
        public List<ExCodeRPN> Expressions = [];
        public List<ExCodeName> Fields = [];
        public List<ExCodeRPN> FileExpr = [];
        public string[] Flags = [];
        public string FNAME = string.Empty;
        public string ForExpr = string.Empty;
        public ExCodeName From = new();
        public string InExpr = string.Empty;
        public List<ExCodeName> Index = [];
        public string INTO = string.Empty;
        public List<string> Like = [];
        public string MESSAGE = string.Empty;
        public string NAME = string.Empty;
        public string OF = string.Empty;
        public string ON = string.Empty;
        public string ORDER = string.Empty;
        public int RECORD = 0;
        public ExCodeScope Scope = new();
        public int SESSION = 0;
        public string SHEET = string.Empty;
        public ExCodeAt Size = new();
        public double STEP = 0;
        public string SUBCMD = string.Empty;
        public string TABLE = string.Empty;
        public List<ExCodeName> Tag = [];
        public int TIME = 0;
        public string TITLE = string.Empty;
        public List<ExCodeName> To = [];
        public List<ExCodeName> Type = [];
        //public JAXObjects.Token[] ValTokens = [];
        public List<ExCodeRPN> Values = [];
        public string WhenExpr = string.Empty;
        public string WhileExpr = string.Empty;
        public List<ExCodeRPN> With = [];
    }


    /*----------------------------------------------------------------------------------------------*
     * Take a ExCodeScope class, set the flags for handling the scope reference, and if a table 
     * reference has been provided, go to the correct next record and then return True if is all
     * is well.
     * 
     * When a record is processed, the IsDone() method is called which updates the number of
     * records read and returns true if you are out of scope.
     *----------------------------------------------------------------------------------------------*/
    public class JAXScope
    {
        public int Count = 0;
        public string GotoPosCmd = string.Empty;
        public int Record = 0;
        public string Type = string.Empty;
        public int SkipVal = 0;
        public int UntilFlag = 0;       // How many records do process. 0 means just current record, -1 means all.
        public double RecordsRead { get; private set; } = 0D;
        public string ErrorMessage = string.Empty;

        // You only call IsDone if a record was looked at so
        // so update the counter and dedide if the end of
        // scope was reached.
        public bool IsDone()
        {
            RecordsRead++;
            return UntilFlag == (int)RecordsRead;
        }

        public async Task Setup(ExCodeScope scope, JAXDirectDBF? Table, bool FixRec)
        {
            if (string.IsNullOrWhiteSpace(scope.Type) == false)
            {
                Type = scope.Type;      // Remember the scope Type
                Count = scope.Count;    // Remember the original count

                // Scope Type
                // A - All
                // N - Next nRecords
                // R - Record nRecord
                // S - Rest
                // T - Top nRecords
                Type = scope.Type;

                switch (Type)
                {
                    case "A":       // All
                        UntilFlag = -1;
                        if (Table is not null) await Table.DBFGotoRecord("top");
                        break;

                    case "N":       // Next x records
                        GotoPosCmd = "X";
                        UntilFlag = scope.Count;
                        break;

                    case "R":       // Record x
                        GotoPosCmd = "R";
                        SkipVal = scope.Count;

                        if (Table is not null)
                        {
                            if (Table.DbfInfo.RecCount >= SkipVal)
                            {
                                // Go to the absolute record position
                                await Table.DBFGotoRecord(SkipVal);
                            }
                            else
                            {
                                // go past the end of the table
                                await Table.DBFGotoRecord("bottom");
                                await Table.DBFSkipRecord(1);
                            }
                        }
                        break;

                    case "S":       // Rest
                        GotoPosCmd = "X";
                        UntilFlag = -1;
                        break;

                    case "T":       // Top
                        UntilFlag += scope.Count;
                        GotoPosCmd = "T";
                        if (Table is not null) await Table.DBFGotoRecord("top");
                        break;

                    default:
                        // No Scope passed, so processing just one record
                        break;
                }
            }
            else
            {
                // Default to ALL
                UntilFlag = -1;
                if (Table is not null && FixRec) await Table.DBFGotoRecord("top");
            }
        }
    }

    /**************************************************************
     * See App.ControlSourceAssignment() for following code
    **************************************************************/
    // -----------------------------------------------------------
    public class VarRef
    {
        public string varName = string.Empty;
        public int row = -1;
        public int col = -1;
    }



    /*----------------------------------------------------------------------------------------------*
     * Holds the system settings.  First time I tried to do this, I used a Token dictionary
     * which was just overkill.  This class is kept in the APP class and is very fast and easy
     * for getting the value of a setting.
     *----------------------------------------------------------------------------------------------*/
    public class JAXSettings
    {
        public bool AIAgent = false;

        public bool Alternate = false;
        public string Alternate_Name = string.Empty;

        public bool ANSI = false;
        public bool Asserts = true;
        public bool AutoIncError = true;
        public bool AutoSave = true;

        public bool Bell = true;
        public string Bell_Name = string.Empty;

        public int BlockSize = 0;

        public bool Carry = true;
        public string Carry_Name = string.Empty;

        public bool Century = true;
        public int Century_Current = DateTime.Now.Year / 100;

        public string ClassLib = string.Empty;
        public bool Collate = true;

        public bool Clock = true;
        public string Clock_Name = string.Empty;

        public bool Confirm = true;

        public bool Console = true;
        public string Console_Name = string.Empty;

        public bool Coverage = false;
        public string Coverage_Name = string.Empty;

        public bool CP_Dialog = true;

        public string Currency = "LEFT";
        public string Currency_Symbol = "$";

        public bool Cursor = false;
        public string Database = string.Empty;
        public int DataSession = 0;

        public string Date_Format = "DMY";
        public int Date_Ordering = 0;

        public bool Debug = true;
        public string DebugOut = string.Empty;
        public int Decimals = 2;
        public string Default = string.Empty;
        public bool Deleted = false;

        public bool Delimiters = false;
        public string Delimiters_Char = ",";

        public bool Development = false;

        public string Device = "SCREEN";
        public string Device_Name = "DEFAULT";

        public bool Echo = false;
        public bool ErrorClassReporting = true;
        public bool Escape = false;

        public string EventList = string.Empty;
        public bool EventTracking = false;
        public string EventTracking_Name = string.Empty;

        public bool Exact = false;
        public bool Exclusive = true;
        public int FDOW = 0;

        public bool Fields = false;
        public string Fields_Name = string.Empty;
        public string Fields_Value = "GLOBAL";

        public string Filter = string.Empty;
        public bool Fixed = false;
        public bool FullPath = false;
        public int FWeek = 0;
        public bool Headings = false;

        public bool Help = false;
        public string Help_Name = string.Empty;
        public string Help_URL = string.Empty;
        public string Help_Location = "SYSTEM";

        public int Hours = 12;
        public bool IncludeSource = false;
        public string Index = string.Empty;

        public float JAXVersion = 1.0F;
        public string JAXEnvironment = "I"; // (I)DE, (A)pp, (E)xe

        public string KeyComp = "WINDOWS";
        public string Library = string.Empty;
        public bool Lock = false;
        public bool LogErrors = true;
        public string MacKey = string.Empty;
        public long MaxMediaSize = 16777215L;
        public uint MemoWidth = 4294967295;

        public int Message_Row = -1;
        public string Message = string.Empty;

        public bool Mouse = true;
        public int Mouse_Sensitivity = 1;

        public bool MultiLocks = false;
        public bool Near = false;
        public string NoCPTrans = string.Empty;

        public int Naming = 0;          // 0 = No change, 1=Upper, 2=Lower, 3=Proper
        public bool NamingAll = false;   // Do we fix the path?

        public bool Notify = false;
        public bool Notify_DataRelated = false;

        public bool Null = false;
        public string Null_Display = ".NULL.";

        public int Odometer = 100;
        public bool Optimize = false;
        public string Order = string.Empty;
        public string Path = string.Empty;
        public string Point = ".";

        public int Printer = -1;
        public int Printer_Default = -1;

        public string Procedure = string.Empty;
        public bool Readborder = false;

        public int Refresh_Display = 10;
        public int Refresh_Buffers = 0;

        public bool Reprocess = false;
        public int Reprocess_Attempts = 0;
        public int Reprocess_CurrentSession = 0;    // 0=attempts, 1=Seconds
        public int Reprocess_SystemSession = 0;     // 0=attempts, 1=Seconds

        public bool Resource = false;
        public string Resource_Name = string.Empty;

        public int RollOver = 75;
        public bool Safety = true;
        public bool Seconds = true;
        public string Separator = ",";
        public bool Space = true;

        public string Skip = string.Empty;
        public string SkipOf = string.Empty;

        public bool SQLBuffering = false;
        public bool Status = false;
        public bool Step = false;
        public int StrictDate = 0;
        public JAXObjects.Token? SQLConnection = null;
        public bool SysFormats = false;
        public string SysMenu = "AUTOMATIC";
        public bool TablePrompt = false;
        public int TableValidate = 0;
        public int TypeAhead = 15;

        public bool Talk = true;
        public string Talk_Console = string.Empty;

        public bool TextMerge = false;
        public string TextMerge_Delimiters = "<,>";
        public string TextMerge_Name = string.Empty;
        public bool TextMerge_show = false;

        public string Topic_Name = string.Empty;
        public int Topic_ID = 0;
        public bool TRBetween = true;
        public bool TypeConvert = false;

        public bool UDFParms_ByValue = true;
        public bool Unique = false;
        public bool VarCharMapping = false;
    }


    /*----------------------------------------------------------------------------------------------*
     * Environment variables used to describe or control the JAXBase environment.
     *----------------------------------------------------------------------------------------------*/
    public class JAXVariables
    {
        // parameter counts
        public int PCount = 0;
        public int Parameters = 0;

        // Editors and Wizards
        public string _AppendPRG = string.Empty;
        public string _EditPRG = string.Empty;
        public string _Browser = string.Empty;

        public string _CodeCleaner = string.Empty;
        public string _Builder = string.Empty;
        public string _ClassEditor = string.Empty;
        public string _FormEditor = string.Empty;
        public string _GraphicEditor = string.Empty;
        public string _HexEditor = string.Empty;
        public string _LabelEditor = string.Empty;
        public string _MenuEditor = string.Empty;
        public string _ObjectBrowser = string.Empty;
        public string _PrgEditor = string.Empty;
        public string _ProjectEditor = string.Empty;
        public string _QueryEditor = string.Empty;
        public string _RepEditor = string.Empty;
        public string _StartupApp = string.Empty;
        public string _TableEditor = string.Empty;
        public string _Wizard = string.Empty;

        // JAX Paths
        private string _appPath = string.Empty;
        public string _AppPath
        {
            get { return _appPath; }
            set { _appPath = JAXLib.Addbs(value); }
        }

        private string _baseFolder = string.Empty;
        public string _BaseFolder
        {
            get { return _baseFolder; }
            set { _baseFolder = JAXLib.Addbs(value); }
        }

        private string _exePath = string.Empty;
        public string _ExePath
        {
            get { return _exePath; }
            set { _exePath = JAXLib.Addbs(value); }
        }

        private string _homePath = string.Empty;
        public string _HomePath
        {
            get { return _homePath; }
            set { _homePath = JAXLib.Addbs(value); }
        }

        private string _logPath = string.Empty;
        public string _LogPath
        {
            get { return _logPath; }
            set { _logPath = JAXLib.Addbs(value); }
        }


        public string _LogName = string.Empty;

        private string _samplesPath = string.Empty;
        public string _SamplesPath
        {
            get { return _samplesPath; }
            set { _samplesPath = JAXLib.Addbs(value); }
        }


        private string _tempPath = string.Empty;
        public string _TempPath
        {
            get { return _tempPath; }
            set { _tempPath = JAXLib.Addbs(value); }
        }

        private string _toolsPath = string.Empty;
        public string _ToolsPath
        {
            get { return _toolsPath; }
            set { _toolsPath = JAXLib.Addbs(value); }
        }

        private string _workPath = string.Empty;
        public string _WorkPath
        {
            get { return _workPath; }
            set { _workPath = JAXLib.Addbs(value); }
        }

        // Defaults for various class properties
        public int _ConsoleColums = 80;
        public int _ConsoleRows = 26;
        public double _DblClick = 0.50;
        public double _Throttle = 0;
        public double _ToolTipTimeout = 0;

        // JAX Environmental Classes - TODO - NEEDS WORK!
        public JAXObjectWrapper? _JAX = null;
        public JAXObjectWrapper? _CPU = null;
        public JAXObjectWrapper[] _DRIVES = [];
        public JAXObjectWrapper? _HEADER = null;
        public JAXObjectWrapper? _KEYBOARD = null;
        public JAXObjectWrapper[] _MONITORS = [];
        public JAXObjectWrapper? _MOUSE = null;
        public JAXObjectWrapper[] _PRINTERS = [];
        public JAXObjectWrapper? _REGEX = null;
        public JAXObjectWrapper? _TABLE = null;
    }

    public class xParameters
    {
        public string Name = string.Empty;
        public JAXObjects.Token Value = new();

        public xParameters() { }
        public xParameters(object val) { Value.Element.Value = val; }
        public xParameters(string name, object val)
        {
            Name = name;
            Value.Element.Value = val;
        }
    }

    // TCP and web based classes
    public class Cookie
    {
        public string Name { get; }
        public string Value { get; }
        public string Path { get; }
        public string Domain { get; }

        public Cookie(string name, string value, string path, string domain)
        {
            Name = name;
            Value = value;
            Path = path;
            Domain = domain;
        }
    }

    class F1Swallower : IMessageFilter
    {
        AppClass App;
        public F1Swallower(AppClass app)
        {
            App = app;
        }
        public bool PreFilterMessage(ref Message m)
        {
            // ONLY look at real keyboard messages — ignore mouse, paint, timers, etc.
            if (m.Msg != 0x100 && m.Msg != 0x101 &&   // WM_KEYDOWN & WM_KEYUP
                m.Msg != 0x104 && m.Msg != 0x105)     // WM_SYSKEYDOWN & WM_SYSKEYUP
                return false;

            Keys key = (Keys)(int)m.WParam;

            // Optional: also filter by currently focused control if you want
            // if (Control.FromHandle(m.HWnd) is not TextBox) return false;
            bool isDown = (m.Msg == 0x100 || m.Msg == 0x104);

            bool alt = (System.Windows.Forms.Control.ModifierKeys & Keys.Alt) == Keys.Alt;
            bool shift = (System.Windows.Forms.Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool control = (System.Windows.Forms.Control.ModifierKeys & Keys.Control) == Keys.Control;

            string onKey = alt ? "A" : string.Empty;
            onKey += control ? "C" : string.Empty;
            onKey += shift ? "S" : string.Empty;
            onKey += onKey.Length > 0 ? "+" : string.Empty;
            onKey += $"{key.ToString()}";

            if (App.OnKeyLabel.ContainsKey(onKey))
            {
                if (isDown == false)
                {
                    Console.WriteLine($"{key.ToString()}");

                    // run your JAX code exactly once per press
                    //
                }
                return true;   // swallow both down and up so no help window
            }

            return false;   // let all other keys through
        }
    }

    /// <summary>
    /// SortedDictionary with change notifications (CollectionChanged and PropertyChanged).
    /// Useful for UI binding in Avalonia and for runtime monitoring in JAXBaseV1.
    /// </summary>
    public class ObservableSortedDictionary<TKey, TValue> :
        IDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
        private readonly SortedDictionary<TKey, TValue> _innerDictionary;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableSortedDictionary()
        {
            _innerDictionary = new SortedDictionary<TKey, TValue>();
        }

        public ObservableSortedDictionary(IComparer<TKey>? comparer)
        {
            _innerDictionary = new SortedDictionary<TKey, TValue>(comparer);
        }

        // IDictionary members
        public TValue this[TKey key]
        {
            get => _innerDictionary[key];
            set
            {
                bool exists = _innerDictionary.ContainsKey(key);
                TValue oldValue = exists ? _innerDictionary[key] : default!;

                _innerDictionary[key] = value;

                if (exists)
                {
                    // Value replaced
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, oldValue)));
                }
                else
                {
                    // New key added
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        new KeyValuePair<TKey, TValue>(key, value)));
                }

                OnPropertyChanged(nameof(Count));
            }
        }

        public ICollection<TKey> Keys => _innerDictionary.Keys;
        public ICollection<TValue> Values => _innerDictionary.Values;

        public int Count => _innerDictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _innerDictionary.Add(key, value);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                new KeyValuePair<TKey, TValue>(key, value)));
            OnPropertyChanged(nameof(Count));
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public bool Remove(TKey key)
        {
            if (_innerDictionary.TryGetValue(key, out TValue? value))
            {
                _innerDictionary.Remove(key);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    new KeyValuePair<TKey, TValue>(key, value)));
                OnPropertyChanged(nameof(Count));
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

        public void Clear()
        {
            _innerDictionary.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        public bool ContainsKey(TKey key) => _innerDictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _innerDictionary.TryGetValue(key, out value!);
        public bool Contains(KeyValuePair<TKey, TValue> item) => _innerDictionary.Contains(item);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_innerDictionary).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            => _innerDictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Helper methods to raise events
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EmptyFactory
    {
        /// <summary>
        /// Creates a new empty object (VFP Empty equivalent).
        /// </summary>
        public dynamic Create()
        {
            return new ExpandoObject();
        }
    }
}
