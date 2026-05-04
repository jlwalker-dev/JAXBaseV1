using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer
    {
        public AppClass App;
        readonly private Dictionary<string, string> Code = [];
        //JAXObjectWrapper? CallingObject = null;
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
            AppIO.DebugLog($"LoadAndExecuteProgram: type={type}, prgToLoad={prgToLoad}, prgToRun={prgToRun}");
            App.RuntimeFlag = true;
            bool result = true;

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

                Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count;
                Program.CurrentApp.AppLevels.Add(appLevel);

                JAXObjectWrapper? CallingObject = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObject;

                if (CallingObject is not null)
                {
                    AppIO.DebugLog($"Program {prgToRun} found in cache at index {i} running under instance {appLevel.Instance}/{Program.CurrentApp.CurrentAppLevel} under object {CallingObject.JOWName} / {CallingObject.THIS.JOWName}");

                    // Set up for an object
                    AppVars.SetLocalSystemVar("this", CallingObject.THIS, 1, 1, false);
                    AppVars.SetLocalSystemVar("thisform", CallingObject.THISFORM, 1, 1, false);
                    AppVars.SetLocalSystemVar("thisformset", CallingObject.THISFORMSET, 1, 1, false);
                }
                else
                {
                    AppIO.DebugLog($"Program {prgToRun} found in cache at index {i} running under instance {appLevel.Instance}/{App.AppLevels.Count - 1}");
                    // Not an object so set to null
                    AppVars.MakeLocalVar("this", 1, 1, false);
                    AppVars.MakeLocalVar("thisform", 1, 1, false);
                    AppVars.MakeLocalVar("thisformset", 1, 1, false);

                    JAXObjects.Token v = await AppVars.GetVarToken("this");
                    v.Element.MakeNull();
                    v = await AppVars.GetVarToken("thisform");
                    v.Element.MakeNull();
                    v = await AppVars.GetVarToken("thisformset");
                    v.Element.MakeNull();
                }


                _ = ExecuteBlock(cCode);
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
            // Set up for an object
            AppVars.SetLocalSystemVar("this", thisObject.THIS, 1, 1, false);

            // Do we have a parent form?
            if (thisObject.THISFORM is null)
            {
                AppVars.MakeLocalVar("thisform", 1, 1, false);
                JAXObjects.Token v = await AppVars.GetVarToken("thisform");
                v.Element.MakeNull();
            }
            else
                AppVars.SetLocalSystemVar("thisform", thisObject.THISFORM, 1, 1, false);

            // Do we have a parent formset?
            if (thisObject.THISFORMSET is null)
            {
                AppVars.MakeLocalVar("thisformset", 1, 1, false);
                JAXObjects.Token v = await AppVars.GetVarToken("thisformset");
                v.Element.MakeNull();
            }
            else
                AppVars.SetLocalSystemVar("thisformset", thisObject.THISFORMSET, 1, 1, false);

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

            Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count();
            App.AppLevels.Add(appLevel);


            _ = ExecuteBlock(ccBlock);
        }

        /*
         * Execute the compiled code block 
         * Create a new App.AppLevels
         * 
         */
        public async Task ExecuteBlock(string compCodeBlock)
        {
            string ccBlock = compCodeBlock;

            App.ReturnValue.Element.Value = true;   // Set the default return value
            AppErrorHandling.ClearErrors();
            JAXObjectWrapper? thisObject = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObject;

            JAXObjects.Token tk = await AppVars.GetVarToken("this");

            if (thisObject is not null && tk.TType.Equals("U"))
            {
                // Set up for an object
                AppVars.SetLocalSystemVar("this", thisObject.THIS, 1, 1, false);
                AppVars.SetLocalSystemVar("thisform", thisObject.THISFORM, 1, 1, false);
                AppVars.SetLocalSystemVar("thisformset", thisObject.THISFORMSET, 1, 1, false);
            }

            if (ccBlock.Length > 0)
            {
                ContainsSource = App.utl.FindByteSequence(ccBlock, AppClass.cmdByte.ToString() + App.MiscInfo["sourcecode"], 0) >= 0;

                string PrgCode = ccBlock;
                App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = 0;

                while (true)
                {
                    int thisCmd = App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;
                    int nextCmd = PrgCode.IndexOf(AppClass.cmdByte, thisCmd + 1);

                    // End of block
                    if (nextCmd < 0)
                        break;

                    string prgCode = nextCmd > 0 ? PrgCode[thisCmd..nextCmd] : PrgCode[thisCmd..];

                    // Strip out the line number                
                    string lineNo = prgCode[^2..];

                    int ln = App.utl.Conv64ToInt(lineNo);
                    if (App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine < 0)
                        App.AppLevels[Program.CurrentApp.CurrentAppLevel].StartLine = ln;

                    App.AppLevels[Program.CurrentApp.CurrentAppLevel].FileLine = ln;
                    App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine = ln - App.AppLevels[Program.CurrentApp.CurrentAppLevel].StartLine + 1;

                    // Clean up the line of code
                    prgCode = prgCode[..^2];

                    string cmdResponse = await ExecuteCommand(prgCode);

                    // ---------------------------------------------------
                    // READ EVENTS HOOK
                    // ---------------------------------------------------
                    while (App.AppLevels[Program.CurrentApp.CurrentAppLevel].InReadEvents || App.OpenDialogCount > 0)
                    {
                        await Task.Delay(1);  // now properly awaited
                    }

                    char respCmd = cmdResponse.Length > 0 ? cmdResponse[0] : 'N';
                    string respRest = cmdResponse.Length > 1 ? cmdResponse[1..] : string.Empty;

                    // We handle the final part an error trap here
                    if (Program.CurrentApp.InError > 0)
                    {
                        switch (Program.CurrentApp.InError)
                        {
                            case 1:     // Try/Catch
                                Program.CurrentApp.InError = 0;

                                // Move to the correct applevel and position for the catch
                                ccBlock = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgCode;
                                nextCmd = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;
                                break;

                            case 2:     // On Error
                                Program.CurrentApp.InError = 0;

                                // Compile the OnError code
                                prgCode = Program.CurrentApp.JaxCompiler.CompileLine(Program.CurrentApp.OnErrorCommand, false);

                                // Execute the OnError code
                                cmdResponse = await ExecuteCommand(prgCode);
                                break;

                            default:    // Unhandled error
                                // Quit, suspend, or ignore?
                                Program.CurrentApp.InError = 0;
                                break;
                        }

                    }
                    else
                    {

                        switch (respCmd)
                        {
                            case 'N':   // Next command in this level
                                App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = nextCmd;
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

                        App.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = nextCmd;


                        // Might be given end of block at this point
                        // because of U, Y or Z result code
                        if (nextCmd < 0)
                            break;
                    }
                }

                // Now remove the level we created to run this code
                if (App.AppLevels.Count > 1)
                {
                    App.AppLevels.RemoveAt(App.AppLevels.Count - 1);
                    Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count - 1;

                    if (App.AppLevels.Count == 1)
                    {
                        // We're done!
                        App.RuntimeFlag = false;
                        if (AppErrorHandling.ErrorCount() > 0)
                        {
                            JAXErrors err = AppErrorHandling.GetCurrentError();
                            MessageBox.Show(err.ErrorMessage, string.Format("Error {0}", err.ErrorNo), MessageBoxButtons.OK, MessageBoxIcon.Error);

                        }
                    }
                }
                else
                {
                    if (AppErrorHandling.ErrorCount() > 0)
                    {
                        // end of execution and we have an outstanding error
                        // we we need to display it for the user
                        JAXErrors err = AppErrorHandling.GetCurrentError();
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
            string result = string.Empty;
            string cmd = command.Substring(1, 2);
            string cmdRest = command[3..].TrimEnd(AppClass.cmdEnd);
            int cmdCode = (int)App.utl.Conv64ToLong(cmd);

            string cmdString;

            if (cmdCode < App.lists.JAXCommands.Length)
                cmdString = App.lists.JAXCommands[cmdCode];
            else
            {
                cmdString = cmdCode switch
                {
                    250 => "*procmap",  // Procedure Map
                    _ => throw new Exception("9994|" + cmdCode.ToString()),
                };
            }

            // Send out debug of what's executing
            string byteDisp = string.Empty;
            for (int i = 0; i < cmdRest.Length; i++)
                byteDisp += cmdRest[i] < 32 ? App.lists.PRGByteCodes[cmdRest[i]] : cmdRest[i];

            AppIO.DebugLog(cmdString + " " + byteDisp);
            ExecuterCodes eCodes;

            try
            {
                if (ContainsSource == false)
                    App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode = cmdString + " ...";

                // Chop off the excess statement delimiters
                cmdRest = cmdRest.Trim(AppClass.stmtDelimiter);
                string[] mProc = cmdRest.Split(AppClass.stmtDelimiter);

                // Are we in a class definition
                if (App.InDefine.Length > 0 && App.InDefine[0] == 'C')
                {
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
                            AppIO.DebugLog($"Defining class method: {mName}");

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
                        // End the current method
                        App.CurrentClassMethod = string.Empty;
                    }
                    else
                    {
                        // Load the command into the class definition
                        if (App.ClassDefinitions[^1].methods.Count > 0)
                            App.ClassDefinitions[^1].methods[App.CurrentClassMethod].ObjectCode += command;
                        else
                            App.ClassDefinitions[^1].PropertyCode += command;
                    }
                }
                else
                {
                    eCodes = await JAXBase_ECodes.Split(mProc);

                    // --------------------------------------------------
                    // Hook for debugger form
                    // Only execute if we're in Stepping mode and the
                    // current command is not a source code update
                    // --------------------------------------------------
                    JAXBase_Debugger.Stepper();

                    // --------------------------------------------------
                    // Process the command
                    // --------------------------------------------------
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
                                "endtry" => JAXBase_Executer_E.EndTry(eCodes),
                                "endwith" => JAXBase_Executer_E.EndWith(this, eCodes),
                                "error" => await JAXBase_Executer_E.ErrorCall(eCodes),                                        // Version 1
                                "exit" => JAXBase_Executer_E.Exit(this, eCodes),
                                "external" => JAXBase_Executer_E.External(this, eCodes),        // Version 1
                                "finally" => JAXBase_Executer_F.Finally(eCodes),
                                "for" => await JAXBase_Executer_F.For(eCodes),
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
                                "on" => JAXBase_Executer_O.On(eCodes),                          // Version 1
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
                                "try" => JAXBase_Executer_T.Try(eCodes),
                                "unlock" => JAXBase_Executer_U.Unlock(App, cmdRest),            // Version 0.8
                                "unpdate" => JAXBase_Executer_U.Update(App, cmdRest),           // Version 1
                                "until" => await JAXBase_Executer_U.Until(this, eCodes),
                                "use" => await JAXBase_Executer_U.Use(this, eCodes),                  // Version 0.6/0.8/1
                                "wait" => await JAXBase_Executer_W.Wait(this, eCodes),                // Version 1
                                "with" => await JAXBase_Executer_W.With(this, eCodes),                // Version 1
                                "zap" => await JAXBase_Executer_Z.Zap(this, eCodes),
                                "~~~" => await AppVars.ObjectCall(eCodes, false) is null ? "N" : "N",
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
                AppErrorHandling.HandleException(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure, ex.Message);
                AppIO.DebugLog($"Error executing command {cmdString}: {ex.Message} in ExecuteCommand");
                result = ex.Message;
            }

            if (Program.CurrentApp.CurrentAppLevel < App.AppLevels.Count && App.AppLevels.Count > 1 && cmdCode != 129)
                App.AppLevels[Program.CurrentApp.CurrentAppLevel].LastCommand = cmdCode;


            return result;
        }



        public static string GetExpressionOrLiteral(AppClass app, string expression)
        {
            string result;
            char type = expression[0];

            if (type == AppClass.literalStart)
            {
                result = expression[1..].Trim(AppClass.literalEnd);
            }
            else if (type == AppClass.expByte)
            {
                result = expression.Replace(AppClass.expByte.ToString(), "");
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
                GenericClass gc = await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[0]);     // Get the variable name
                string varName = gc.Value.Element.ValueAsString;

                int r = 1;
                int c = 1;

                if (varInfo.Length > 1)
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[1]);                  // Get the row value if it exists
                    r = gc.Value.AsInt() > 0 ? gc.Value.AsInt() : 1;
                }

                if (varInfo.Length > 2)
                {
                    gc = await JAXBase_Executer_M.SolveFromRPNString(app, varInfo[2]);                  // Get the col value if it exists
                    c = gc.Value.AsInt() > 0 ? gc.Value.AsInt() : 1;
                }

                // Make sure the varName exits
                JAXObjects.Token v = await AppVars.GetVarToken(varName);

                if (v.TType.Equals("U"))
                {
                    // Variable is not defined
                    if (r < 2 && c < 2)
                    {
                        // It's a simple variable
                        if (createVar)
                            AppVars.SetVarOrMakePrivate(varName, 1, 1, false);                          // It's ok to create the simple variable
                        else
                            throw new Exception(string.Format("12|{0}", varName));                  // Throw exception because you aren't allowed to create it
                    }
                    else
                        throw new Exception(string.Format("232|{0}", varName));                     // Array not defined
                }

                AppVars.SetVar(varName, value, r, c);                                                   // Now set the variable element
                v = await AppVars.GetVarToken(varName);

                if (v.TType.Equals("A"))
                    AppIO.DebugLog(string.Format("Storing {0} ({1}) into {2}[{3},{4}]", v.Element.ValueAsString, v.Element.Type, varName, r, c), app.CurrentDS.JaxSettings.Talk == false);
                else
                    AppIO.DebugLog(string.Format("Storing {0} ({1}) into {2}", v.Element.ValueAsString, v.Element.Type, varName), app.CurrentDS.JaxSettings.Talk == false);
            }
            catch (Exception ex)
            {
                // Something went wrong
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
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
    }
}
