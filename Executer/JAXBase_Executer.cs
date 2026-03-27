using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer
    {
        public AppClass App;
        readonly private Dictionary<string, string> Code = [];
        JAXObjectWrapper? CallingObject = null;
        bool ContainsSource = false;

        public Dictionary<string, int> CmdNum = [];

        public JAXBase_Executer(AppClass app)
        {
            App = app;

            // Load up the code dictionary
            for (int i = 0; i < App.lists.JAXCompilerDictionary.Length; i++)
            {
                string[] jcd = App.lists.JAXCompilerDictionary[i].Split('|');
                Code.Add(jcd[1], string.Empty);
            }

            for (int i = 0; i < App.lists.JAXCommands.Length; i++)
                CmdNum.Add(app.lists.JAXCommands[i].ToLower(), i);
        }

        /*
         * Load a program and execute it
         */
        public async Task<bool> LoadAndExecuteProgram(string type, string prgToLoad, string prgToRun, JAXObjectWrapper? parent, bool obeyReadEvents)
        {
            App.DebugLog($"LoadAndExecuteProgram: type={type}, prgToLoad={prgToLoad}, prgToRun={prgToRun}");
            App.RuntimeFlag = true;
            bool result = true;

            if (prgToRun.Contains("addpem", StringComparison.OrdinalIgnoreCase))
            {
                int iii = 0;
            }

            // If prgToLoad is empty then fill it with prgToRun value
            prgToLoad = string.IsNullOrWhiteSpace(prgToLoad) ? prgToRun : prgToLoad;

            // Is this program already loaded into the cache?
            int i = await AppHelper.LoadFileIntoCache(App, type, prgToLoad);

            // Look in APP levels to see if it's here
            // and get the index if it is.  This allows
            // us to make sure that the last loaded name
            // is the one that is called first
            //for (int jj = App.AppLevels.Count - 1; jj >= 0; jj--)
            //{
            //    // TODO - needs thought
            //}

            if (i < 0)
            {
                // It's not a program, so is it a procedure that's already loaded?
                for (int j = 0; j < App.CodeCache.Count; j++)
                {
                    if (App.CodeCache[i].Procedures.ContainsKey(prgToRun.ToLower()))
                    {
                        i = App.CodeCache[i].Procedures[prgToRun.ToLower()];
                        break;
                    }
                }
            }

            if (i >= 0)
            {
                string cCode = App.PRGCache[i];

                // TODO - check to make sure we have what we need
                // Create a new app level and execute the code
                AppLevel appLevel = new()
                {
                    PRGCacheIdx = i,
                    PrgType = type,
                    PrgName = prgToRun,
                    CodeCacheName = prgToRun.ToLower(),
                    ThisObject = parent,
                    ThisObjectMethod = parent is null ? string.Empty : prgToRun,
                    Instance = App.SystemCounter()
                };

                App.AppLevels.Add(appLevel);

                if (CallingObject is not null)
                {
                    App.DebugLog($"Program {prgToRun} found in cache at index {i} running under instance {appLevel.Instance}/{App.AppLevels.Count - 1} under object {CallingObject.JOWName} / {CallingObject.THIS.JOWName}");
                    // Set up for an object
                    App.SetLocalSystemVar("this", CallingObject.THIS, 1, 1, false);
                    App.SetLocalSystemVar("thisform", CallingObject.THISFORM, 1, 1, false);
                    App.SetLocalSystemVar("thisformset", CallingObject.THISFORMSET, 1, 1, false);
                }
                else
                {
                    App.DebugLog($"Program {prgToRun} found in cache at index {i} running under instance {appLevel.Instance}/{App.AppLevels.Count - 1}");
                    // Not an object so set to null
                    App.MakeLocalVar("this", 1, 1, false);
                    App.MakeLocalVar("thisform", 1, 1, false);
                    App.MakeLocalVar("thisformset", 1, 1, false);

                    JAXObjects.Token v = await App.GetVarToken("this");
                    v.Element.MakeNull();
                    v = await App.GetVarToken("thisform");
                    v.Element.MakeNull();
                    v = await App.GetVarToken("thisformset");
                    v.Element.MakeNull();
                }


                _ = ExecuteBlock(null, cCode);
            }

            return result;
        }

        /*
         * 
         * Create a new App.AppLevels and call ExecuteBlock
         * 
         * 
         */
        public async Task ExecuteCodeBlock(JAXObjectWrapper thisObject, string methodName, string ccBlock)
        {
            // TODO - check to make sure we have what we need
            // Create a new app level and execute the code
            AppLevel appLevel = new()
            {
                PRGCacheIdx = -1,
                PrgType = "m",
                PrgName = thisObject.JOWName,
                CodeCacheName = methodName.ToLower(),
                ThisObject = thisObject,
                ThisObjectMethod = methodName,
                PrgCode = ccBlock,
                Instance = App.SystemCounter()
            };

            App.AppLevels.Add(appLevel);

            //App.DebugLog($"Running {thisObject.nvObject}.{methodName} under instance {appLevel.Instance}/{App.AppLevels.Count - 1}", false);

            // Set up for an object
            App.SetLocalSystemVar("this", thisObject.THIS, 1, 1, false);

            // Do we have a parent form?
            if (thisObject.THISFORM is null)
            {
                App.MakeLocalVar("thisform", 1, 1, false);
                JAXObjects.Token v = await App.GetVarToken("thisform");
                v.Element.MakeNull();
            }
            else
                App.SetLocalSystemVar("thisform", thisObject.THISFORM, 1, 1, false);

            // Do we have a parent formset?
            if (thisObject.THISFORMSET is null)
            {
                App.MakeLocalVar("thisformset", 1, 1, false);
                JAXObjects.Token v = await App.GetVarToken("thisformset");
                v.Element.MakeNull();
            }
            else
                App.SetLocalSystemVar("thisformset", thisObject.THISFORMSET, 1, 1, false);

            _ = ExecuteBlock(thisObject, ccBlock);
        }

        /*
         * Execute the compiled code block 
         * Create a new App.AppLevels
         * 
         */
        public async Task ExecuteBlock(JAXObjectWrapper? thisObject, string ccBlock)
        {
            //App.DebugLog($"Execute code start ─ this: {this.GetHashCode()}  me: {thisObject?.GetHashCode() ?? -1}  me.Name: {thisObject?.Name ?? "?"}", false);

            App.ReturnValue.Element.Value = true;   // Set the default return value
            App.ClearErrors();
            CallingObject = thisObject;

            JAXObjects.Token tk = await App.GetVarToken("this");

            if (CallingObject is not null && tk.TType.Equals("U"))
            {
                // Set up for an object
                App.SetLocalSystemVar("this", CallingObject.THIS, 1, 1, false);
                App.SetLocalSystemVar("thisform", CallingObject.THISFORM, 1, 1, false);
                App.SetLocalSystemVar("thisformset", CallingObject.THISFORMSET, 1, 1, false);
            }

            if (ccBlock.Length > 0)
            {
                ContainsSource = App.utl.FindByteSequence(ccBlock, AppClass.cmdByte.ToString() + App.MiscInfo["sourcecode"], 0) >= 0;

                string PrgCode = ccBlock;
                App.AppLevels[^1].PrgPos = 0;

                while (true)
                {
                    int thisCmd = App.AppLevels[^1].PrgPos;
                    int nextCmd = PrgCode.IndexOf(AppClass.cmdByte, thisCmd + 1);
                    string prgCode = nextCmd > 0 ? PrgCode[thisCmd..nextCmd] : PrgCode[thisCmd..];

                    // Strip out the line number                
                    string lineNo = prgCode[^2..];

                    int ln = App.utl.Conv64ToInt(lineNo);
                    if (App.AppLevels[^1].CurrentLine < 0)
                        App.AppLevels[^1].StartLine = ln;

                    App.AppLevels[^1].FileLine = ln;
                    App.AppLevels[^1].CurrentLine = ln - App.AppLevels[^1].StartLine + 1;

                    // Clean up the line of code
                    prgCode = prgCode[..^2];

                    string cmdResponse = await ExecuteCommand(prgCode);

                    // ---------------------------------------------------
                    // READ EVENTS HOOK
                    // ---------------------------------------------------
                    while (App.AppLevels[^1].InReadEvents || App.OpenDialogCount > 0)
                    {
                        await Task.Delay(1);  // now properly awaited
                    }

                    // Not in Read Events, continue execution
                    if (App.ErrorCount() > 0)
                    {
                        bool tryCatch = false;
                        // Is there an active TRY/CATCH?
                        for (int i = App.AppLevels[^1].LoopStack.Count - 1; i >= 0; i--)
                        {
                            // if there is a TRY or CATCH active, look
                            // for the next CATCH and if not found
                            // stop executing this level
                            string lstack = App.AppLevels[^1].LoopStack[i];
                            if (lstack.Length > 1 && "TC".Contains(lstack[0]))
                            {
                                int f = App.utl.FindByteSequence(PrgCode, AppClass.cmdByte.ToString() + App.MiscInfo["catchcmd"], 0);

                                string code = PrgCode.Substring(f + 4, 3);
                                if (f < 0 || code.Equals(lstack) == false)
                                {
                                    // Unhandled exception error
                                    App.SetError(2305, string.Empty, string.Empty);
                                    nextCmd = -1;
                                    break;
                                }

                                tryCatch = true;
                                App.AppLevels[^1].PrgPos = f;
                                nextCmd = f;
                                continue;
                            }
                        }

                        if (tryCatch == false)
                        {
                            nextCmd = -1;
                            break;
                        }
                    }
                    else
                    {
                        char respCmd = cmdResponse.Length > 0 ? cmdResponse[0] : 'N';
                        string respRest = cmdResponse.Length > 1 ? cmdResponse[1..] : string.Empty;

                        switch (respCmd)
                        {
                            case 'N':   // Next command in this level
                                App.AppLevels[^1].PrgPos = nextCmd;
                                break;

                            case 'I':
                            case 'W':     // Locate this command
                            case 'C':
                            case 'F':
                            case 'U':
                                nextCmd = PrgCode.IndexOf(respRest);

                                if (nextCmd < 0)
                                {
                                    string cType = respRest[..1] switch
                                    {
                                        "I" => "1211|",     // IF
                                        "W" => "1209|",     // DO WHILE
                                        "C" => "1213|",     // DO CASE
                                        "F" => "1207|",     // FOR
                                        _ => "2010|"        // DO / UNTIL
                                    };

                                    throw new Exception(cType);
                                }

                                if (respCmd == 'U')
                                {
                                    // Did someone put the cart before the horse?
                                    if (nextCmd >= thisCmd)
                                        throw new Exception("2010|");

                                    // Get past the DO for the until and continue executing code
                                    nextCmd = PrgCode.IndexOf(AppClass.cmdByte.ToString(), nextCmd + 2);
                                }

                                break;

                            case 'X':   // Go to this command position
                                nextCmd = App.utl.Conv64ToInt(respRest);
                                break;

                            case 'Y':   // Go to the command after the indicated position
                                nextCmd = App.utl.Conv64ToInt(respRest);

                                if (nextCmd > 0)  // Find the command after this one (or end of file)
                                    nextCmd = PrgCode.IndexOf(AppClass.cmdByte, nextCmd + 1);
                                break;

                            case 'Z':           // Exit Immediately
                                nextCmd = -1;
                                break;

                            default:
                                break;
                        }

                        App.AppLevels[^1].PrgPos = nextCmd;
                    }

                    // if less than zero, we're done
                    // with this code block
                    if (nextCmd < 0)
                        break;
                }

                // Now remove the level we created to run this code
                if (App.AppLevels.Count > 1)
                {
                    App.AppLevels.RemoveAt(App.AppLevels.Count - 1);

                    if (App.AppLevels.Count == 1)
                    {
                        // We're done!
                        App.RuntimeFlag = false;
                        if (App.ErrorCount() > 0)
                        {
                            JAXErrors err = App.GetLastError();
                            MessageBox.Show(err.ErrorMessage, string.Format("Error {0}", err.ErrorNo), MessageBoxButtons.OK, MessageBoxIcon.Error);

                        }
                    }
                }
                else
                {
                    if (App.ErrorCount() > 0)
                    {
                        // end of execution and we have an outstanding error
                        // we we need to display it for the user
                        JAXErrors err = App.GetLastError();
                        MessageBox.Show(err.ErrorMessage, string.Format("Error {0}", err.ErrorNo), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /*
         * Execute a single command
         */
        public async Task<string> ExecuteCommand(string command)
        {
            string hold = command;
            string result = string.Empty;
            string cmd = command.Substring(1, 2);
            string cmdRest = command[3..].TrimEnd(AppClass.cmdEnd);
            int cmdCode = (int)App.utl.Conv64ToLong(cmd);
            ExecuterCodes eCodes = new();

            string cmdString;

            if (cmdCode < App.lists.JAXCommands.Length)
                cmdString = App.lists.JAXCommands[cmdCode];
            else
            {
                switch (cmdCode)
                {
                    case 250:   // Procedure Map
                        cmdString = "*procmap";
                        break;

                    default:
                        throw new Exception("9994|" + cmdCode.ToString());
                }
            }

            // Send out debug of what's executing
            string byteDisp = string.Empty;
            for (int i = 0; i < cmdRest.Length; i++)
                byteDisp += cmdRest[i] < 32 ? App.lists.PRGByteCodes[cmdRest[i]] : cmdRest[i];

            App.DebugLog(cmdString + " " + byteDisp);
            string task = string.Empty;

            try
            {
                if (ContainsSource == false)
                    App.AppLevels[^1].CurrentLineOfCode = cmdString + " ...";

                string[] mProc = cmdRest.Split(AppClass.stmtDelimiter);

                // Are we in a class definition
                if (App.InDefine.Length > 0 && App.InDefine[0] == 'C')
                {
                    task = "In class definition";

                    // --------------------------------------------------
                    // A class definition is a loading process and
                    // nothing is actually executed.  Load the methods
                    // and property code until we reach EndDefine.
                    // --------------------------------------------------
                    if (cmdString.ToLower().Equals("procedure"))
                    {
                        if (mProc.Length > 1)
                        {
                            GenericClass gc = await JAXBase_Executer_M.SolveFromRPNString(App, mProc[0]);
                            string mName = gc.Value.AsString().ToLower().Trim();
                            App.DebugLog($"Defining class method: {mName}");

                            gc = await JAXBase_Executer_M.SolveFromRPNString(App, mProc[1]);
                            bool mProtected = gc.Value.AsString().Length > 0 && gc.Value.AsString().ToUpper()[0].Equals('P');

                            if (App.ClassDefinitions[^1].methods.ContainsKey(mName) == false)
                            {
                                // Start the method
                                ClassMethod m = new() { Protected = mProtected };
                                App.ClassDefinitions[^1].methods.Add(mName, m);
                                App.CurrentClassMethod = mName;
                            }
                            else
                            {
                                // TODO - Already exists.  Perhaps throwing an error would be better?
                                App.ClassDefinitions[^1].methods[mName].ObjectCode = string.Empty;
                                App.ClassDefinitions[^1].methods[mName].Protected = mProtected;
                            }
                        }
                        else
                            throw new Exception("10|");
                    }
                    else if (cmdString.ToLower().Equals("endproc"))
                    {
                        task = "End of class procedure";
                        // End the current method
                        App.CurrentClassMethod = string.Empty;
                    }
                    else
                    {
                        task = "Loading class code";
                        // Load the command into the class definition
                        if (App.ClassDefinitions[^1].methods.Count > 0)
                            App.ClassDefinitions[^1].methods[App.CurrentClassMethod].ObjectCode += command;
                        else
                            App.ClassDefinitions[^1].PropertyCode += command;
                    }
                }
                else
                {
                    // Chop off the excess statement delimiters
                    cmdRest = cmdRest.Trim(AppClass.stmtDelimiter);

                    // --------------------------------------------------
                    // Load the eCodes class with the various components
                    // of the statement.  Usually under 4 but could be
                    // several for a few commands.
                    // --------------------------------------------------
                    task = "Parsing command codes";
                    for (int i = 0; i < mProc.Length; i++)
                    {
                        // Skip blank entries
                        if (string.IsNullOrWhiteSpace(mProc[i])) continue;

                        char k = mProc[i][0];       // Get the runtime key
                        string rpn = mProc[i][1..]; // Strip the key from the RPN expression
                        string codeName = App.RunTimeCodes[App.XRef4Runtime[k]];    // Get the code name

                        JAXObjects.Token rpnValue = new();
                        string[] rpns = [];
                        string[] rpnSplit = [];
                        JAXObjects.Token answer = new();

                        // Put the RPN expression into the code
                        // Some can be solved here, others have to wait for later
                        //App.DebugLog($"Processing line {App.AppLevels[^1].CurrentLine} in level {App.AppLevels.Count - 1} source {App.AppLevels[^1].CurrentLineOfCode} -> code: {codeName} with RPN: {rpn}", App.CurrentDS.JaxSettings.Talk == false);

                        switch (codeName)
                        {
                            case "as":
                                task = "breaking AS";
                                rpnSplit = rpn.Split(AppClass.expDelimiter);
                                for (int j = 0; j < rpnSplit.Length; j++)
                                    eCodes.As.Add(rpnSplit[j]);
                                break;

                            case "at":
                                task = "breaking AT";
                                rpns = rpn.Split(AppClass.expParam);
                                if (rpns.Length != 2) throw new Exception($"10||Invalid AT expression has {rpns.Length} parameters");
                                answer = await App.SolveFromRPNString(rpns[0]);
                                if (answer.Element.Type.Equals("N"))
                                    eCodes.At.row = answer.AsInt();
                                else
                                    throw new Exception("11|");

                                answer = await App.SolveFromRPNString(rpns[1]);
                                if (answer.Element.Type.Equals("N"))
                                    eCodes.At.col = answer.AsInt();
                                else
                                    throw new Exception("11|");

                                break;

                            case "command":
                                task = "breaking COMMAND";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.COMMAND = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "collate":
                                task = "breaking COLLATE";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.COLLATE = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "codepage":
                                task = "breaking CODEPAGE";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("N"))
                                    eCodes.CODEPAGE = rpnValue.AsInt();
                                else
                                    throw new Exception("11|");
                                break;

                            case "database":
                                task = "breaking DATABASE";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.DATABASE = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "expressions":
                                task = "breaking EXPRESSIONS";
                                rpnSplit = rpn.Split(AppClass.expDelimiter);
                                for (int j = 0; j < rpnSplit.Length; j++)
                                {
                                    if (string.IsNullOrWhiteSpace(rpnSplit[j]) == false)
                                    {
                                        // We only deal with non-blank expressions
                                        ExCodeRPN e = new()
                                        {
                                            // Is the RPN expression a Literal, eXpression, or plain Text?
                                            Type = rpnSplit[j][0] == AppClass.literalStart ? "L" : rpnSplit[j][0] == AppClass.expByte ? "X" : throw new Exception("11|"),
                                            RNPExpr = rpnSplit[j]
                                        };

                                        eCodes.Expressions.Add(e);
                                    }
                                }
                                break;

                            case "flags":
                                task = "breaking FLAGS";
                                eCodes.Flags = rpn.Split(AppClass.expParam);
                                for (int j = 0; j < eCodes.Flags.Length; j++)
                                    eCodes.Flags[j] = (await App.SolveFromRPNString(eCodes.Flags[j])).AsString();
                                break;

                            case "from":
                                task = "breaking FROM";
                                rpnSplit = rpn.Split(AppClass.expDelimiter);
                                if (rpnSplit.Length != 2) throw new Exception("11|");
                                eCodes.From.Type = rpnSplit[0];
                                eCodes.From.Name = (await App.SolveFromRPNString(rpnSplit[1])).AsString();
                                break;

                            case "fname":
                                task = "breaking FNAME";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.FNAME = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "for":
                                task = "breaking FOR";
                                eCodes.ForExpr = rpn;
                                break;

                            case "fields":
                                task = "breaking FIELDS";
                                rpns = rpn.Split(AppClass.expDelimiter);

                                for (int j = 0; j < rpns.Length; j += 2)
                                {
                                    if (rpns.Length > j + 1)
                                    {
                                        ExCodeName fld = new()
                                        {
                                            Type = rpns[j],
                                            Name = rpns[j + 1]
                                        };

                                        eCodes.Fields.Add(fld);
                                    }
                                }

                                break;

                            case "in":
                                task = "breaking IN";
                                eCodes.InExpr = rpn;
                                break;

                            case "index":
                                task = "breaking INDEX";
                                rpns = rpn.Split(AppClass.expDelimiter);

                                for (int j = 0; j < rpns.Length; j++)
                                {
                                    rpnSplit = rpns[j].Split(AppClass.expParam);

                                    ExCodeName fld = new()
                                    {
                                        Type = rpnSplit[0],
                                        Name = rpnSplit[1]
                                    };

                                    eCodes.Index.Add(fld);
                                }
                                break;

                            case "into":
                                task = "breaking INTO";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.INTO = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "like":
                                task = "breaking LIKE";
                                rpns = rpn.Split(AppClass.expDelimiter);

                                for (int j = 0; j < rpns.Length; j += 2)
                                {
                                    // Is it a valid literal/expression or empty?
                                    if (rpns[j].Length > 2)
                                        eCodes.Like.Add((await App.SolveFromRPNString(rpns[j])).AsString());
                                }

                                break;

                            case "of":
                                task = "breaking OF";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.OF = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "on":
                                task = "breaking ON";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.ON = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "order":
                                task = "breaking ORDER";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.ORDER = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "record":
                                task = "breaking RECORD";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("N"))
                                    eCodes.RECORD = rpnValue.AsInt();
                                else if (rpnValue.Element.Type.Equals("C"))
                                {
                                    if (rpnValue.AsString().Equals("top", StringComparison.OrdinalIgnoreCase))
                                        eCodes.RECORD = -1;
                                    else if (rpnValue.AsString().Equals("bottom", StringComparison.OrdinalIgnoreCase))
                                        eCodes.RECORD = -2;
                                    else
                                        throw new Exception("11|");
                                }
                                else
                                    throw new Exception("11|");

                                break;

                            case "sesssion":
                                task = "breaking SESSION";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("N"))
                                    eCodes.SESSION = rpnValue.AsInt();
                                else
                                    throw new Exception("11|");
                                break;

                            case "scope":
                                task = "breaking SCOPE";
                                rpnSplit = rpn.Split(AppClass.expDelimiter);
                                if (rpnSplit.Length != 2) throw new Exception("11|");
                                eCodes.Scope.Type = rpnSplit[0];
                                answer = await App.SolveFromRPNString(rpnSplit[1]);
                                if (answer.Element.Type.Equals("N"))
                                    eCodes.Scope.Count = answer.AsInt();
                                else
                                    throw new Exception("11|");
                                break;

                            case "sheet":
                                task = "breaking SHEET";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("C"))
                                    eCodes.SHEET = rpnValue.AsString();
                                else
                                    throw new Exception("11|");
                                break;

                            case "step":
                                task = "breaking STEP";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("N"))
                                    eCodes.RECORD = rpnValue.AsInt();
                                else
                                    throw new Exception("11|");
                                break;

                            case "subcmd":
                                task = "breaking SUBCMD";
                                if (string.IsNullOrWhiteSpace(rpn))
                                    throw new Exception("11|");
                                else
                                    eCodes.SUBCMD = rpn;
                                break;

                            case "table":
                                task = "breaking TABLE";
                                eCodes.TABLE = rpn;
                                break;

                            case "tag":
                                task = "breaking TAG";
                                rpns = rpn.Split(AppClass.expDelimiter);
                                for (int j = 0; j < rpns.Length; j++)
                                {
                                    ExCodeName fld = new()
                                    {
                                        Type = rpns[j][0] == AppClass.literalStart ? "L" : "X",
                                        Name = rpns[j]
                                    };

                                    eCodes.Tag.Add(fld);
                                }
                                break;

                            case "timeout":
                                task = "breaking TIMEOUT";
                                rpnValue = await App.SolveFromRPNString(rpn);
                                if (rpnValue.Element.Type.Equals("N"))
                                    eCodes.RECORD = rpnValue.AsInt();
                                else
                                    throw new Exception("11|");
                                break;

                            case "to":
                                task = "breaking TO";
                                // TO may be two parts or just one
                                // TO expr/lit
                                // TO expr/lit expr/lit
                                rpns = rpn.Split(AppClass.expDelimiter);

                                for (int j = 0; j < rpns.Length; j++)
                                {
                                    ExCodeName fld = new()
                                    {
                                        Type = rpns.Length == 1 ? (rpns[0][0] == AppClass.literalStart ? "L" : "X") : rpns[0],
                                        Name = rpns.Length == 1 ? rpns[0] : rpns[1]
                                    };

                                    eCodes.To.Add(fld);
                                }
                                break;

                            case "type":
                                task = "breaking TYPE";
                                rpns = rpn.Split(AppClass.expDelimiter);
                                for (int j = 0; j < rpns.Length; j++)
                                {
                                    ExCodeName fld = new()
                                    {
                                        Type = rpns[j][0] == AppClass.literalStart ? "L" : "X",
                                        Name = rpns[j]
                                    };

                                    eCodes.Type.Add(fld);
                                }
                                break;

                            case "values":
                                task = "breaking VALUES";
                                rpnSplit = rpn.Split(AppClass.expDelimiter);
                                for (int j = 0; j < rpnSplit.Length; j++)
                                {
                                    if (string.IsNullOrWhiteSpace(rpnSplit[j]) == false)
                                    {
                                        // We only deal with non-blank expressions
                                        ExCodeRPN e = new()
                                        {
                                            // Is the RPN expression a Literal, eXpression, or plain Text?
                                            Type = rpnSplit[j][0] == AppClass.literalStart ? "L" : rpnSplit[j][0] == AppClass.expByte ? "X" : throw new Exception("11|"),
                                            RNPExpr = rpnSplit[j]
                                        };

                                        eCodes.Values.Add(e);
                                    }
                                }
                                break;

                            case "when":
                                task = "breaking WHEN";
                                eCodes.WhenExpr = rpn;
                                break;

                            case "while":
                                task = "breaking WHILE";
                                eCodes.WhileExpr = rpn;
                                break;

                            case "with":
                                task = "breaking WITH";
                                rpns = rpn.Split(AppClass.expDelimiter);

                                for (int j = 0; j < rpns.Length; j += 2)
                                {
                                    if (rpns.Length > j + 1)
                                    {
                                        ExCodeRPN exCodeRPN = new()
                                        {
                                            Type = rpns[j],
                                            RNPExpr = rpns[j + 1]
                                        };

                                        eCodes.With.Add(exCodeRPN);
                                    }
                                }
                                break;
                        }
                    }

                    // --------------------------------------------------
                    // Hook for debugger form
                    // Only execute if we're in Stepping mode and the
                    // current command is not a source code update
                    // --------------------------------------------------
                    //if (App.CurrentDS.JaxSettings.Step && cmdString.Equals("*sc", StringComparison.OrdinalIgnoreCase) == false)
                    //{
                    //    // If debugger screen is not active, start it up
                    //    if (App.JaxDebugger is null)
                    //    {
                    //        App.JaxDebugger = new(App);
                    //        // At the very start of debugging (once)
                    //        App.JaxDebugger.BeginDebugging();
                    //    }

                    //    bool debugging = true;

                    //    while (debugging && App.JaxDebugger is not null)
                    //    {
                    //        DebugAction action = App.JaxDebugger.GetResponse();  // This now WORKS and is responsive

                    //        switch (action)
                    //        {
                    //            case JAXDebuggerForm.DebugAction.Step:
                    //                debugging = false;
                    //                break;

                    //            case JAXDebuggerForm.DebugAction.StepInto:
                    //                debugging = false;
                    //                break;

                    //            case JAXDebuggerForm.DebugAction.Cancel:
                    //                debugging = false;
                    //                App.CurrentDS.JaxSettings.Step = false;
                    //                App.JaxDebugger?.EndDebugging();
                    //                App.JaxDebugger = null;
                    //                JAXBase_Executer_C.Cancel(this, null);
                    //                return "Z";

                    //            case JAXDebuggerForm.DebugAction.Resume:
                    //                debugging = false;
                    //                App.CurrentDS.JaxSettings.Step = false;
                    //                App.JaxDebugger.EndDebugging();
                    //                App.JaxDebugger = null;
                    //                break;
                    //        }
                    //    }
                    //}


                    // --------------------------------------------------
                    // Process the command
                    // --------------------------------------------------
                    task = "Processing command " + cmdString.ToUpper();
                    switch (cmdString.ToLower())
                    {
                        case "average":
                        case "count":
                        case "sum":
                            await JAXBase_Executer_A.Average(this, eCodes);
                            break;

                        case "display":
                        case "list":
                            result = await JAXBase_Executer_D.Display(this, eCodes, cmdString.ToLower().Equals("display"));
                            break;

                        default:
                            result = cmdString.ToLower() switch
                            {
                                "activate" => JAXBase_Executer_A.Activate(this, eCodes),
                                "add" => await JAXBase_Executer_A.Add(this, eCodes),            // Version 0.6
                                "alter" => JAXBase_Executer_A.Alter(App, cmdRest),              // Version 0.6
                                "aparameters" => await JAXBase_Executer_A.AParameters(this, eCodes),
                                "append" => await JAXBase_Executer_A.Append(this, eCodes),
                                "assert" => await JAXBase_Executer_A.Assert(this, eCodes),
                                "begin" => JAXBase_Executer_B.Begin(App, cmdRest),              // Version 0.6
                                "blank" => JAXBase_Executer_B.Blank(App, cmdRest),              // Version 0.8
                                "browse" => await JAXBase_Executer_B.Browse(this, eCodes),            // Version 1
                                "build" => JAXBase_Executer_B.Build(App, cmdRest),              // Version 1
                                "cancel" => await JAXBase_Executer_C.Cancel(this, eCodes),
                                "calculate" => await JAXBase_Executer_C.Calculate(this, eCodes),
                                "case" => JAXBase_Executer_C.Case(this, eCodes),
                                "catch" => await JAXBase_Executer_C.Catch(this, eCodes),
                                "cd" => await JAXBase_Executer_C.CD(this, eCodes),
                                "clear" => await JAXBase_Executer_C.Clear(this, eCodes),
                                "close" => await JAXBase_Executer_C.Close(this, eCodes),
                                "compile" => await JAXBase_Executer_C.Compile(this, eCodes),
                                "continue" => JAXBase_Executer_C.Continue(this, eCodes),        //Version 0.6
                                "copy" => JAXBase_Executer_C.Copy(App, cmdRest),                // Version 1
                                "create" => await JAXBase_Executer_C.Create(this, eCodes),      // Version 1
                                "debug" => JAXBase_Executer_D.Debug(App, cmdRest),              // Version 1
                                "debugout" => JAXBase_Executer_D.DebugOut(App, cmdRest),        // Version 1
                                "define" => await JAXBase_Executer_D.Define(App, cmdRest),            // Version 1
                                "delete" => await JAXBase_Executer_D.Delete(this, eCodes),            // Version 1
                                "dimension" => await JAXBase_Executer_D.Dimension(this, eCodes),
                                "directory" => await JAXBase_Executer_D.Directory(this, eCodes),
                                "do" => await JAXBase_Executer_D.Do(this, eCodes),              // Version 1
                                "dodefault" => JAXBase_Executer_D.DoDefault(this, eCodes),
                                "doevents" => JAXBase_Executer_D.DoEvents(App, cmdRest),        // Version 1
                                "drop" => JAXBase_Executer_D.Drop(App, cmdRest),                // Version 1
                                "edit" => await JAXBase_Executer_E.Edit(this, eCodes),          // Version 1
                                "else" => JAXBase_Executer_E.Else(this, eCodes),
                                "elseif" => JAXBase_Executer_E.Else(this, eCodes),              // Same action as else
                                "end" => JAXBase_Executer_E.End(this, eCodes),
                                "endcase" => JAXBase_Executer_E.EndCase(this, eCodes),
                                "enddefine" => await JAXBase_Executer_E.EndDefine(App, cmdRest),      // Version 1
                                "enddo" => JAXBase_Executer_E.EndDo(this, eCodes),
                                "endfor" => JAXBase_Executer_E.EndFor(this, eCodes),
                                "endif" => JAXBase_Executer_E.EndIf(this, eCodes),
                                "endprocedure" => JAXBase_Executer_E.EndProcedure(this, eCodes),
                                "endscan" => JAXBase_Executer_E.EndScan(this, eCodes),
                                "endtext" => JAXBase_Executer_E.EndText(this, eCodes),          // Version 1
                                "endtry" => JAXBase_Executer_E.EndTry(this, eCodes),
                                "endwith" => JAXBase_Executer_E.EndWith(this, eCodes),
                                "error" => string.Empty,                                        // Version 1
                                "exit" => JAXBase_Executer_E.Exit(this, eCodes),
                                "external" => JAXBase_Executer_E.External(this, eCodes),        // Version 1
                                "finally" => string.Empty,
                                "for" => await JAXBase_Executer_F.For(this, eCodes),
                                "foreach" => JAXBase_Executer_F.ForEach(this, eCodes),          // Version 1
                                "gather" => JAXBase_Executer_G.Gather(App, cmdRest),            // Version 0.8
                                "getexp" => JAXBase_Executer_G.GetExpr(App, cmdRest),           // Version 1
                                "goto" => await JAXBase_Executer_G.Goto(this, eCodes),
                                "help" => JAXBase_Executer_H.Help(this, eCodes),
                                "if" => await JAXBase_Executer_I.If(this, eCodes),
                                "import" => JAXBase_Executer_I.Import(App, cmdRest),            // Version 2
                                "index" => await JAXBase_Executer_I.Index(this, eCodes),
                                "insert" => await JAXBase_Executer_I.Insert(this, eCodes),
                                "keyboard" => JAXBase_Executer_K.Keyboard(this, eCodes),        // Version 1
                                "local" => await JAXBase_Executer_L.Local(this, eCodes),
                                "locate" => await JAXBase_Executer_L.Locate(this, eCodes),
                                "loop" => JAXBase_Executer_L.Loop(this, eCodes),
                                "lparameters" => await JAXBase_Executer_L.LParameters(this, eCodes),
                                "lprocedure" => JAXBase_Executer_L.LProcedure(App, cmdRest),
                                "md" => await JAXBase_Executer_M.MD(this, eCodes),
                                "modify" => await JAXBase_Executer_M.Modify(this, eCodes),      // Version 1
                                "mouse" => JAXBase_Executer_M.Mouse(App, cmdRest),              // Version 1
                                "on" => JAXBase_Executer_O.On(App, cmdRest),                    // Version 1
                                "open" => JAXBase_Executer_O.Open(App, cmdRest),                // Version 0.6
                                "otherwise" => JAXBase_Executer_O.Otherwise(this, eCodes),
                                "pack" => await JAXBase_Executer_P.Pack(this, eCodes),
                                "parameters" => await JAXBase_Executer_P.Parameters(this, eCodes),
                                "play" => JAXBase_Executer_P.Play(App, cmdRest),                // Version 1
                                "private" => await JAXBase_Executer_P.Private(this, eCodes),
                                "procedure" => string.Empty,
                                "public" => await JAXBase_Executer_P.Public(this, eCodes),
                                "quit" => await JAXBase_Executer_Q.Quit(App, eCodes),
                                "rd" => await JAXBase_Executer_R.RD(this, eCodes),
                                "read" => JAXBase_Executer_R.Read(this, eCodes),                // Version 1
                                "recall" => await JAXBase_Executer_R.Recall(this, eCodes),
                                "register" => await JAXBase_Executer_R.Register(this, eCodes),
                                "reindex" => JAXBase_Executer_R.Reindex(App, cmdRest),          // Version 1
                                "release" => JAXBase_Executer_R.Release(App, cmdRest),          // Version 0.6/1
                                "remove" => JAXBase_Executer_R.Remove(App, cmdRest),            // Version 1
                                "rename" => await JAXBase_Executer_R.Rename(this, eCodes),      // Version 1
                                "replace" => await JAXBase_Executer_R.Replace(this, eCodes),    // Version 0.4/0.6
                                "restore" => JAXBase_Executer_R.Restore(App, cmdRest),          // Version 1
                                "resume" => JAXBase_Executer_R.Resume(App, cmdRest),            // Version 1
                                "retry" => JAXBase_Executer_R.Retry(App, cmdRest),              // Version 1
                                "return" => await JAXBase_Executer_R.Return(this, eCodes),
                                "save" => JAXBase_Executer_S.Save(App, cmdRest),                // Version 1
                                "scan" => await JAXBase_Executer_S.Scan(this, eCodes),
                                "scatter" => JAXBase_Executer_S.Scatter(App, cmdRest),          // Version 0.8
                                "seek" => await JAXBase_Executer_S.Seek(App, cmdRest),                // Version 0.6
                                "select" => await JAXBase_Executer_S.Select(this, eCodes),
                                "set" => await JAXBase_Executer_Settings.Settings(this, eCodes),
                                "skip" => await JAXBase_Executer_S.Skip(this, eCodes),
                                "sort" => JAXBase_Executer_S.Sort(App, cmdRest),                // Version 1
                                "store" => await JAXBase_Executer_S.Store(this, eCodes),
                                "suspend" => JAXBase_Executer_S.Suspend(App, cmdRest),          // Version 1
                                "text" => JAXBase_Executer_T.Text(App, cmdRest),                // Version 1
                                "throw" => await JAXBase_Executer_T.Throw(this, eCodes),              // Version 1
                                "try" => JAXBase_Executer_T.Try(this, eCodes),
                                "unlock" => JAXBase_Executer_U.Unlock(App, cmdRest),            // Version 0.8
                                "unpdate" => JAXBase_Executer_U.Update(App, cmdRest),           // Version 1
                                "until" => await JAXBase_Executer_U.Until(this, eCodes),
                                "use" => await JAXBase_Executer_U.Use(this, eCodes),                  // Version 0.6/0.8/1
                                "wait" => await JAXBase_Executer_W.Wait(this, eCodes),                // Version 1
                                "with" => await JAXBase_Executer_W.With(this, eCodes),                // Version 1
                                "zap" => await JAXBase_Executer_Z.Zap(this, eCodes),
                                "~~~" => await App.ObjectCall(eCodes, false) is null ? "N" : "N",
                                "?" => await JAXBase_Executer_Legacy.QPrint(this, eCodes),
                                "??" => await JAXBase_Executer_Legacy.QQPrint(this, eCodes),
                                "*sc" => JAXBase_Executer_Legacy.SourceCode(this, eCodes),
                                _ => throw new Exception(string.Format("Execute command {0} is not implemented", cmdString)),
                            };
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = ex.Message;
            }

            if (App.AppLevels.Count > 0 && cmdCode != 129)
                App.AppLevels[^1].LastCommand = cmdCode;

            return result;
        }



        public static string GetExpressionOrLiteral(AppClass app, string expression)
        {
            string result = string.Empty;
            char type = expression[0];

            if (type == AppClass.literalStart)
            {
                result = expression[1..].Trim(AppClass.literalEnd);
            }
            else if (type == AppClass.expByte)
            {
                expression = expression.Replace(AppClass.expByte.ToString(), "");

            }
            else
                throw new Exception(string.Format("Unknown expression {0}", expression));



            return result;
        }







        public static async Task<bool> SetVarExpression(AppClass app, string varExpr, object? value, bool createVar)
        {
            bool result = true;

            try
            {
                string[] varInfo = varExpr.Split(AppClass.expParam);               // Break the variable expression
                GenericClass gc=await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[0]);     // Get the variable name
                string varName = gc.Value.Element.ValueAsString;

                int r = 1;
                int c = 1;

                if (varInfo.Length > 1)
                {
                    gc=await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[1]);                  // Get the row value if it exists
                    r = gc.Value.AsInt() > 0 ? gc.Value.AsInt() : 1;
                }

                if (varInfo.Length > 2)
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[2]);                  // Get the col value if it exists
                    c = gc.Value.AsInt() > 0 ? gc.Value.AsInt() : 1;
                }

                // Make sure the varName exits
                JAXObjects.Token v = await app.GetVarToken(varName);

                if (v.TType.Equals("U"))
                {
                    // Variable is not defined
                    if (r < 2 && c < 2)
                    {
                        // It's a simple variable
                        if (createVar)
                            app.SetVarOrMakePrivate(varName, 1, 1, false);                          // It's ok to create the simple variable
                        else
                            throw new Exception(string.Format("12|{0}", varName));                  // Throw exception because you aren't allowed to create it
                    }
                    else
                        throw new Exception(string.Format("232|{0}", varName));                     // Array not defined
                }

                app.SetVar(varName, value, r, c);                                                   // Now set the variable element
                v = await app.GetVarToken(varName);

                if (v.TType.Equals("A"))
                    app.DebugLog(string.Format("Storing {0} ({1}) into {2}[{3},{4}]", v.Element.ValueAsString, v.Element.Type, varName, r, c), app.CurrentDS.JaxSettings.Talk == false);
                else
                    app.DebugLog(string.Format("Storing {0} ({1}) into {2}", v.Element.ValueAsString, v.Element.Type, varName), app.CurrentDS.JaxSettings.Talk == false);
            }
            catch (Exception ex)
            {
                // Something went wrong
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /*
         * 
         * Break out the RPN elements from the math string
         * 
         */
        public static List<string> GetRPN(string mathStr)
        {
            List<string> results = [];
            string test = AppClass.expEnd.ToString() + AppClass.expParam + AppClass.expDelimiter;
            while (test.Contains(mathStr[^1]))
                mathStr = mathStr[..^1];

            while (test.Contains(mathStr[0]))
                mathStr = mathStr[1..];

            if (mathStr[0].Equals(AppClass.expByte))
            {
                string[] r = mathStr.Split(AppClass.expParam);

                for (int i = 0; i < r.Length; i++)
                    results.Add(r[i].Replace(AppClass.expByte.ToString(), ""));
            }
            else
                throw new Exception("Not an expression");

            return results;
        }

        private readonly object _debugLock = new object();
    }
}
