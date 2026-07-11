/*
 * This helper class primarily deals with loading files for execution
 * into the APP and handling AppLevel creation
 */
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.XBase;

namespace JAXBase.Executor
{
    public class JAXBase_Executor
    {
        readonly private Dictionary<string, string> Code = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        //JAXObjectWrapper? CallingObject = null;
        bool ContainsSource = false;

        public Dictionary<string, int> CmdNum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public JAXBase_Executor()
        {
            // Load up the code dictionary
            for (int i = 0; i < JAXLanguageLists.JAXCompilerDictionary.Length; i++)
            {
                string[] jcd = JAXLanguageLists.JAXCompilerDictionary[i].Split('|');
                Code.Add(jcd[1], string.Empty);
            }

            for (int i = 0; i < JAXLanguageLists.JAXCommands.Length; i++)
                CmdNum.Add(JAXLanguageLists.JAXCommands[i].ToLower(), i);
        }

        /*
         * Load a program and execute it
         */
        public async Task<bool> LoadAndExecuteProgram(string type, string prgToLoad, string prgToRun, JAXObjectWrapper? parent, bool obeyReadEvents)
        {
            AppIO.DebugLog($"LoadAndExecuteProgram: type={type}, prgToLoad={prgToLoad}, prgToRun={prgToRun}");
            Program.CurrentApp.RuntimeFlag = true;
            bool result = true;

            // If prgToLoad is empty then fill it with prgToRun value
            prgToLoad = string.IsNullOrWhiteSpace(prgToLoad) ? prgToRun : prgToLoad;

            // Is this program already loaded into the cache?
            int i = await AppHelper.LoadFileIntoCache(type, prgToLoad);

            // Look in APP levels to see if it's here
            // and get the index if it is.  This allows
            // us to make sure that the last loaded name
            // is the one that is called first
            //for (int jj =Program.CurrentApp.AppLevels.Count - 1; jj >= 0; jj--)
            //{
            //    // TODO - needs thought
            //}

            if (i < 0)
            {
                // It's not a program, so is it a procedure that's already loaded?
                for (int j = 0; j < Program.CurrentApp.CodeCache.Count; j++)
                {
                    if (Program.CurrentApp.CodeCache[i].Procedures.ContainsKey(prgToRun.ToLower()))
                    {
                        i = Program.CurrentApp.CodeCache[i].Procedures[prgToRun.ToLower()];
                        break;
                    }
                }
            }

            if (i >= 0)
            {
                string cCode = Program.CurrentApp.PRGCache[i];

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
                    Instance = Program.CurrentApp.SystemCounter()
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
                    AppIO.DebugLog($"Program {prgToRun} found in cache at index {i} running under instance {appLevel.Instance}/{Program.CurrentApp.AppLevels.Count - 1}");
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
         * Create a newProgram.CurrentApp.AppLevels and call ExecuteBlock
         * 
         * 
         */
        public async Task ExecuteCodeBlock(JAXObjectWrapper thisObject, string methodName, string ccBlock)
        {
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
                Instance = Program.CurrentApp.SystemCounter()
            };

            Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count();
            Program.CurrentApp.AppLevels.Add(appLevel);

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

            _ = ExecuteBlock(ccBlock);
        }

        /*
         * Execute the compiled code block 
         * Create a newProgram.CurrentApp.AppLevels
         * 
         */
        public async Task ExecuteBlock(string compCodeBlock)
        {
            string ccBlock = compCodeBlock;

            Program.CurrentApp.ReturnValue.Element.Value = true;   // Set the default return value
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
                //ContainsSource = Program.CurrentApp.utl.FindByteSequence(ccBlock, AppClass.cmdByte.ToString() + Program.CurrentApp.MiscInfo["sourcecode"], 0) >= 0;

                string PrgCode = ccBlock;
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = 0;

                while (true)
                {
                    int thisCmd = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;
                    int nextCmd = PrgCode.IndexOf(AppClass.cmdByte, thisCmd + 1);

                    string prgCode = nextCmd > 0 ? PrgCode[thisCmd..nextCmd] : PrgCode[thisCmd..];
                    //AppIO.DebugLog($" Code length is {prgCode.Length}, nextCmd is {nextCmd}");

                    // End of block
                    if (prgCode.Length < 1)
                        break;

                    // Strip out the line number                
                    string lineNo = prgCode[^2..];

                    int ln = Program.CurrentApp.utl.Conv64ToInt(lineNo);
                    int lv = Program.CurrentApp.CurrentAppLevel;
                    string pr = Program.CurrentApp.AppLevels[lv].Procedure;

                    if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine < 0)
                        Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].StartLine = ln;

                    Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].FileLine = ln;
                    Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine = ln - Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].StartLine + 1;

                    // Clean up the line of code
                    prgCode = prgCode[..^2];

                    string cmdResponse = await ExecuteCommand(prgCode);

                    // ---------------------------------------------------
                    // READ EVENTS HOOK
                    // ---------------------------------------------------
                    while (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].InReadEvents || Program.CurrentApp.OpenDialogCount > 0)
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
                                AppIO.DebugLog($">>> TRY/CATCH");
                                Program.CurrentApp.InError = 0;

                                // Move to the correct applevel and position for the catch
                                ccBlock = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgCode;
                                nextCmd = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;
                                break;

                            case 2:     // On Error
                                AppIO.DebugLog($">>> ON ERROR");
                                Program.CurrentApp.InError = 0;

                                // Compile the OnError code
                                prgCode = Program.CurrentApp.JaxCompiler.CompileLine(Program.CurrentApp.OnErrorCommand, false);

                                // Execute the OnError code
                                cmdResponse = await ExecuteCommand(prgCode);
                                break;

                            default:    // Unhandled error
                                // Quit, suspend, or ignore?
                                Program.CurrentApp.InError = 0;
                                AppIO.DebugLog($">>> UNHANDLED ERROR DROPOUT");
                                break;
                        }

                    }
                    else
                    {
                        switch (respCmd)
                        {
                            case 'N':   // Next command in this level
                                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = nextCmd;
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
                                nextCmd = Program.CurrentApp.utl.Conv64ToInt(respRest);
                                break;

                            case 'Y':   // Go to the command after the indicated position
                                nextCmd = Program.CurrentApp.utl.Conv64ToInt(respRest);

                                if (nextCmd > 0)  // Find the command after this one (or end of file)
                                    nextCmd = PrgCode.IndexOf(AppClass.cmdByte, nextCmd + 1);
                                break;

                            case 'Z':           // Exit Immediately
                                nextCmd = -1;
                                break;

                            default:
                                break;
                        }

                        AppIO.DebugLog($"Processed line {ln} level {lv} procedure {pr} - Received {respCmd} nextCmd = {nextCmd}");
                        Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos = nextCmd;


                        // Might be given end of block at this point
                        // because of U, Y or Z result code
                        if (nextCmd < 0)
                            break;
                    }
                }

                // Now remove the level we created to run this code
                if (Program.CurrentApp.AppLevels.Count > 1)
                {
                    Program.CurrentApp.AppLevels.RemoveAt(Program.CurrentApp.AppLevels.Count - 1);
                    Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count - 1;

                    if (Program.CurrentApp.AppLevels.Count == 1)
                    {
                        // We're done!
                        Program.CurrentApp.RuntimeFlag = false;
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
            int cmdCode = (int)Program.CurrentApp.utl.Conv64ToLong(cmd);

            string cmdString;

            if (cmdCode < JAXLanguageLists.JAXCommands.Length)
                cmdString = JAXLanguageLists.JAXCommands[cmdCode];
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
                byteDisp += cmdRest[i] < 32 ? JAXLanguageLists.PRGByteCodes[cmdRest[i]] : cmdRest[i];

            AppIO.DebugLog(cmdString + " " + byteDisp);
            ExecutorCodes eCodes;

            try
            {
                if (ContainsSource == false)
                    Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode = cmdString + " ...";

                // Chop off the excess statement delimiters
                cmdRest = cmdRest.Trim(AppClass.stmtDelimiter);
                string[] mProc = cmdRest.Split(AppClass.stmtDelimiter);

                // Are we in a class definition
                if (Program.CurrentApp.InDefine.Length > 0 && Program.CurrentApp.InDefine[0] == 'C')
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
                            JAXObjects.Token gc = await Program.CurrentApp.SolveFromRPNString(mProc[0]);
                            string mName = gc.AsString().ToLower().Trim();
                            AppIO.DebugLog($"Defining class method: {mName}");

                            gc = await Program.CurrentApp.SolveFromRPNString(mProc[1]);
                            bool mProtected = gc.AsString().Length > 0 && gc.AsString().ToUpper()[0].Equals('P');

                            if (Program.CurrentApp.ClassDefinitions[^1].methods.ContainsKey(mName) == false)
                            {
                                // Start the method
                                ClassMethod m = new() { Protected = mProtected };
                                Program.CurrentApp.ClassDefinitions[^1].methods.Add(mName, m);
                                Program.CurrentApp.CurrentClassMethod = mName;
                            }
                            else
                            {
                                // TODO - Already exists.  Perhaps throwing an error would be better?
                                Program.CurrentApp.ClassDefinitions[^1].methods[mName].ObjectCode = string.Empty;
                                Program.CurrentApp.ClassDefinitions[^1].methods[mName].Protected = mProtected;
                            }
                        }
                        else
                            throw new Exception("10|");
                    }
                    else if (cmdString.ToLower().Equals("endproc"))
                    {
                        // End the current method
                        Program.CurrentApp.CurrentClassMethod = string.Empty;
                    }
                    else
                    {
                        // Load the command into the class definition
                        if (Program.CurrentApp.ClassDefinitions[^1].methods.Count > 0)
                            Program.CurrentApp.ClassDefinitions[^1].methods[Program.CurrentApp.CurrentClassMethod].ObjectCode += command;
                        else
                            Program.CurrentApp.ClassDefinitions[^1].PropertyCode += command;
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
                            await JAXBase_Executor_A.Average(this, eCodes);
                            break;

                        case "display":
                        case "list":
                            result = await JAXBase_Executor_D.Display(eCodes, cmdString.ToLower().Equals("display"));
                            break;

                        default:
                            result = cmdString.ToLower() switch
                            {
                                "activate" => JAXBase_Executor_A.Activate(this, eCodes),
                                "add" => await JAXBase_Executor_A.Add(this, eCodes),            // Version 0.6
                                "alter" => JAXBase_Executor_A.Alter(cmdRest),              // Version 0.6
                                "aparameters" => await JAXBase_Executor_A.AParameters(this, eCodes),
                                "append" => await JAXBase_Executor_A.Append(this, eCodes),
                                "assert" => await JAXBase_Executor_A.Assert(this, eCodes),
                                "begin" => JAXBase_Executor_B.Begin(cmdRest),              // Version 0.6
                                "blank" => await JAXBase_Executor_B.Blank(eCodes),              // Version 0.8
                                "browse" => await JAXBase_Executor_B.Browse(eCodes),            // Version 1
                                "build" => JAXBase_Executor_B.Build(cmdRest),              // Version 1
                                "cancel" => await JAXBase_Executor_C.Cancel(eCodes),
                                "calculate" => await JAXBase_Executor_C.Calculate(eCodes),
                                "case" => JAXBase_Executor_C.Case(eCodes),
                                "catch" => await JAXBase_Executor_C.Catch(eCodes),
                                "cd" => await JAXBase_Executor_C.CD(eCodes),
                                "clear" => await JAXBase_Executor_C.Clear(eCodes),
                                "close" => await JAXBase_Executor_C.Close(eCodes),
                                "compile" => await JAXBase_Executor_C.Compile(eCodes),
                                "continue" => JAXBase_Executor_C.Continue(eCodes),        //Version 0.6
                                "copy" => JAXBase_Executor_C.Copy(cmdRest),                // Version 1
                                "create" => await JAXBase_Executor_C.Create(eCodes),      // Version 1
                                "debug" => JAXBase_Executor_D.Debug(cmdRest),              // Version 1
                                "debugout" => JAXBase_Executor_D.DebugOut(cmdRest),        // Version 1
                                "define" => await JAXBase_Executor_D.Define(cmdRest),            // Version 1
                                "delete" => await JAXBase_Executor_D.Delete(eCodes),            // Version 1
                                "dimension" => await JAXBase_Executor_D.Dimension(eCodes),
                                "directory" => await JAXBase_Executor_D.Directory(eCodes),
                                "do" => await JAXBase_Executor_D.Do(eCodes),              // Version 1
                                "dodefault" => JAXBase_Executor_D.DoDefault(eCodes),
                                "doevents" => JAXBase_Executor_D.DoEvents(cmdRest),        // Version 1
                                "drop" => JAXBase_Executor_D.Drop(cmdRest),                // Version 1
                                "edit" => await JAXBase_Executor_E.Edit(eCodes),          // Version 1
                                "else" => JAXBase_Executor_E.Else(this, eCodes),
                                "elseif" => JAXBase_Executor_E.Else(this, eCodes),              // Same action as else
                                "end" => JAXBase_Executor_E.End(this, eCodes),
                                "endcase" => JAXBase_Executor_E.EndCase(this, eCodes),
                                "enddefine" => await JAXBase_Executor_E.EndDefine(cmdRest),      // Version 1
                                "enddo" => JAXBase_Executor_E.EndDo(this, eCodes),
                                "endfor" => JAXBase_Executor_E.EndFor(this, eCodes),
                                "endif" => JAXBase_Executor_E.EndIf(this, eCodes),
                                "endprocedure" => JAXBase_Executor_E.EndProcedure(this, eCodes),
                                "endscan" => JAXBase_Executor_E.EndScan(this, eCodes),
                                "endtext" => JAXBase_Executor_E.EndText(this, eCodes),          // Version 1
                                "endtry" => JAXBase_Executor_E.EndTry(eCodes),
                                "endwith" => JAXBase_Executor_E.EndWith(this, eCodes),
                                "error" => await JAXBase_Executor_E.ErrorCall(eCodes),                                        // Version 1
                                "exit" => JAXBase_Executor_E.Exit(this, eCodes),
                                "external" => JAXBase_Executor_E.External(this, eCodes),        // Version 1
                                "finally" => JAXBase_Executor_F.Finally(eCodes),
                                "for" => await JAXBase_Executor_F.For(eCodes),
                                "foreach" => JAXBase_Executor_F.ForEach(this, eCodes),          // Version 1
                                "gather" => await JAXBase_Executor_G.Gather(eCodes),            // Version 0.8
                                "getexp" => JAXBase_Executor_G.GetExpr(cmdRest),           // Version 1
                                "goto" => await JAXBase_Executor_G.Goto(eCodes),
                                "help" => JAXBase_Executor_H.Help(this, eCodes),
                                "if" => await JAXBase_Executor_I.If(eCodes),
                                "import" => JAXBase_Executor_I.Import(cmdRest),            // Version 2
                                "index" => await JAXBase_Executor_I.Index(eCodes),
                                "insert" => await JAXBase_Executor_I.Insert(eCodes),
                                "keyboard" => JAXBase_Executor_K.Keyboard(this, eCodes),        // Version 1
                                "list" => await JAXBase_Executor_D.Display(eCodes, false),                // Version 1
                                "local" => await JAXBase_Executor_L.Local(eCodes),
                                "locate" => await JAXBase_Executor_L.Locate(eCodes),
                                "loop" => JAXBase_Executor_L.Loop(eCodes),
                                "lparameters" => await JAXBase_Executor_L.LParameters(this, eCodes),
                                "lprocedure" => JAXBase_Executor_L.LProcedure(cmdRest),
                                "md" => await JAXBase_Executor_M.MD(eCodes),
                                "modify" => await JAXBase_Executor_M.Modify(eCodes),      // Version 1
                                "mouse" => JAXBase_Executor_M.Mouse(cmdRest),              // Version 1
                                "on" => JAXBase_Executor_O.On(eCodes),                          // Version 1
                                "open" => JAXBase_Executor_O.Open(cmdRest),                // Version 0.6
                                "otherwise" => JAXBase_Executor_O.Otherwise(eCodes),
                                "pack" => await JAXBase_Executor_P.Pack(eCodes),
                                "parameters" => await JAXBase_Executor_P.Parameters(this, eCodes),
                                "play" => JAXBase_Executor_P.Play(cmdRest),                // Version 1
                                "private" => await JAXBase_Executor_P.Private(eCodes),
                                "procedure" => string.Empty,
                                "public" => await JAXBase_Executor_P.Public(eCodes),
                                "quit" => await JAXBase_Executor_Q.Quit(eCodes),
                                "rd" => await JAXBase_Executor_R.RD(eCodes),
                                "read" => JAXBase_Executor_R.Read(eCodes),                // Version 1
                                "recall" => await JAXBase_Executor_R.Recall(eCodes),
                                "register" => await JAXBase_Executor_R.Register(eCodes),
                                "reindex" => JAXBase_Executor_R.Reindex(cmdRest),          // Version 1
                                "release" => JAXBase_Executor_R.Release(cmdRest),          // Version 0.6/1
                                "remove" => JAXBase_Executor_R.Remove(cmdRest),            // Version 1
                                "rename" => await JAXBase_Executor_R.Rename(eCodes),      // Version 1
                                "replace" => await JAXBase_Executor_R.Replace(eCodes),    // Version 0.4/0.6
                                "restore" => JAXBase_Executor_R.Restore(cmdRest),          // Version 1
                                "resume" => JAXBase_Executor_R.Resume(cmdRest),            // Version 1
                                "retry" => JAXBase_Executor_R.Retry(cmdRest),              // Version 1
                                "return" => await JAXBase_Executor_R.Return(eCodes),
                                "save" => JAXBase_Executor_S.Save(eCodes),                // Version 1
                                "scan" => await JAXBase_Executor_S.Scan(eCodes),
                                "scatter" => await JAXBase_Executor_S.Scatter(eCodes),          // Version 0.8
                                "seek" => await JAXBase_Executor_S.Seek(cmdRest),                // Version 0.6
                                "select" => await JAXBase_Executor_S.Select(eCodes),
                                "set" => await JAXBase_Executor_Settings.Settings(eCodes),
                                "skip" => await JAXBase_Executor_S.Skip(eCodes),
                                "sort" => JAXBase_Executor_S.Sort(eCodes),                // Version 1
                                "store" => await JAXBase_Executor_S.Store(eCodes),
                                "suspend" => JAXBase_Executor_S.Suspend(cmdRest),          // Version 1
                                "text" => await JAXBase_Executor_T.Text(eCodes),                // Version 1
                                "throw" => await JAXBase_Executor_T.Throw(eCodes),              // Version 1
                                "try" => JAXBase_Executor_T.Try(eCodes),
                                "unlock" => JAXBase_Executor_U.Unlock(cmdRest),            // Version 0.8
                                "unpdate" => JAXBase_Executor_U.Update(cmdRest),           // Version 1
                                "until" => await JAXBase_Executor_U.Until(eCodes),
                                "use" => await JAXBase_Executor_U.Use(eCodes),                  // Version 0.6/0.8/1
                                "wait" => await JAXBase_Executor_W.Wait(eCodes),                // Version 1
                                "with" => await JAXBase_Executor_W.With(eCodes),                // Version 1
                                "zap" => await JAXBase_Executor_Z.Zap(eCodes),
                                "~~~" => await AppVars.ObjectCall(eCodes, false) is null ? "N" : "N",
                                "?" => await JAXBase_Executor_Legacy.QPrint(eCodes),
                                "??" => await JAXBase_Executor_Legacy.QQPrint(eCodes),
                                _ => throw new Exception(string.Format("Execute command {0} is not implemented", cmdString)),
                            };
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure, ex.Message);
                AppIO.DebugLog($"Error executing command {cmdString}: {ex.Message} in ExecuteCommand");
                result = ex.Message;
            }

            if (Program.CurrentApp.CurrentAppLevel < Program.CurrentApp.AppLevels.Count && Program.CurrentApp.AppLevels.Count > 1 && cmdCode != 129)
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LastCommand = cmdCode;

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
                JAXObjects.Token gc = await Program.CurrentApp.SolveFromRPNString(varInfo[0]);     // Get the variable name
                string varName = gc.Element.ValueAsString;

                int r = 1;
                int c = 1;

                if (varInfo.Length > 1)
                {
                    gc = await Program.CurrentApp.SolveFromRPNString(varInfo[1]);                  // Get the row value if it exists
                    r = gc.AsInt() > 0 ? gc.AsInt() : 1;
                }

                if (varInfo.Length > 2)
                {
                    gc = await Program.CurrentApp.SolveFromRPNString(varInfo[2]);                  // Get the col value if it exists
                    c = gc.AsInt() > 0 ? gc.AsInt() : 1;
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
                    AppIO.DebugLog(string.Format("Storing {0} ({1}) into {2}[{3},{4}]", v.Element.ValueAsString, v.Element.Type, varName, r, c), Program.CurrentApp.CurrentDS.JaxSettings.Talk == false);
                else
                    AppIO.DebugLog(string.Format("Storing {0} ({1}) into {2}", v.Element.ValueAsString, v.Element.Type, varName), Program.CurrentApp.CurrentDS.JaxSettings.Talk == false);
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
