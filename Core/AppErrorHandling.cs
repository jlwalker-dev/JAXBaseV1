using JAXBase.Language;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Core
{
    public static class AppErrorHandling
    {
        public static void HandleException(string curMethod, string exMessage)
        {
            int errCode = 0;

            // Sure don't want to be here, but if we are, get it logged
            if (exMessage.Contains('|'))
            {
                string[] errMsg = exMessage.Split('|');
                if (errMsg.Length > 1)
                {
                    if (int.TryParse(errMsg[0], out errCode))
                        SetError(errCode, $"{errCode}|{errMsg[1].Trim()}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }

            if (errCode == 0)
                SetError(9999, $"9999|{curMethod}|{exMessage}", curMethod);
        }


        /* ------------------------------------------------------------------------------------------*
         * Records all errors and reports errors in an xBase manner.  
         * Current error handler will be called from here.
         * 
         * ErrMessage Parameter handling
         *      If just a message, record to system log
         *      If pipe delimited, expected format is:
         *          JAXErr|MsgParameter|System message
         *          JaxErr will expand and use the MsgParameter
         *          and report to System log along with the 
         *          System message if present
         *          
         * TODO - tie in error handling
         * ------------------------------------------------------------------------------------------*/
        public static void SetError(int ErrNo, string ErrMessage, string ErrProcedure)
        {
            int jaxErr = ErrNo;
            string jaxErrMsg = ErrMessage;

            if (ErrMessage.Contains('|'))
            {
                string[] msg = ErrMessage.Split("|");
                if (int.TryParse(msg[0], out int err)) jaxErr = err;

                jaxErrMsg = JAXError.JAXErrMsg(jaxErr, msg.Length > 1 ? msg[1] : string.Empty);
            }

            JAXErrors e = new()
            {
                ErrorNo = jaxErr,
                ErrorMessage = jaxErrMsg,
                ErrorProcedure = Program.CurrentApp.AppLevels.Count > 0 ? Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgName + (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure.Length > 0 ? "." + Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure : string.Empty) : ErrProcedure,
                ErrorSource = Program.CurrentApp.AppLevels.Count > 0 && Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine > 0 ? Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode : string.Empty,
                ErrorLine = Program.CurrentApp.AppLevels.Count > 0 ? Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine : 0
            };

            string errTextMsg = (Program.CurrentApp.AppLevels.Count > 0 && Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine > 0) ?
                string.Format("     Error {0} @ Line {1} in {2} - {3}", e.ErrorNo, e.ErrorLine, e.ErrorProcedure, e.ErrorMessage) :
                string.Format("     Error {0} in {1} - {2}", e.ErrorNo, e.ErrorProcedure, e.ErrorMessage);

            if (Program.CurrentApp.CurrentDS.JaxSettings.Alternate && string.IsNullOrWhiteSpace(Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name) == false)
                JAXLib.StrToFile(errTextMsg, Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name, 1);

            AppIO.DebugLog($"{errTextMsg} - {ErrMessage}", false);

            Program.CurrentApp.Errors.Add(e);

            if (jaxErr > 0)
            {
                if (Program.CurrentApp.AppLevels.Count > 0 && Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine > 0)
                {
                    if (Program.CurrentApp.CodeCache.Count > Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx)
                    {
                        string sFile = Program.CurrentApp.CodeCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx].SourceFile;
                        if (File.Exists(sFile))
                        {
                            // Source is available, so get the line of code in error
                            // TODO - check to see we need to concatenate lines to
                            // display the entire line of source code
                            string pCode = JAXLib.FileToStr(sFile);
                            pCode = pCode.Replace("\n", "");
                            string[] pcd = pCode.Split("\r");

                            if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine - 1 < pcd.Length)
                                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode = pcd[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine - 1];
                        }
                    }
                    else
                    {
                        AppIO.DebugLog(string.Format("CodeCache.Count={0}, AppLevels[{1}].PRGCacheIdx={2} in {3}", Program.CurrentApp.CodeCache.Count, Program.CurrentApp.AppLevels.Count - 1, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx, System.Reflection.MethodBase.GetCurrentMethod()!.Name), false);
                    }

                    CallOnError(Program.CurrentApp.Errors.Count - 1);
                }
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * OnError 
         * ------------------------------------------------------------------------------------------*/
        public static void CallOnError(int ErrorToHandle)
        {
            Program.CurrentApp.CurrentError = ErrorToHandle;

            // Need to implement CURRENTLEVEL since RESUME and RESUME NEXT
            // may send control back to the routine that has the error
            int appLevel = Program.CurrentApp.CurrentAppLevel;
            int stackPos = -1;

            // Look for the first TRY in all applevels starting with this one
            // which has it's TryPhase set to 1, meaning that it's active.
            while (appLevel > 0)
            {
                if (Program.CurrentApp.AppLevels[appLevel].TryStack.Count > 0)
                {
                    stackPos = Program.CurrentApp.AppLevels[appLevel].TryStack.Count - 1;

                    // Look for the first untriggered try in this app level
                    while (stackPos >= 0)
                    {
                        if (Program.CurrentApp.AppLevels[appLevel].TryStack[stackPos].TryPhase == 1)
                            break;
                        else
                        {
                            // If this try is already sprung then the error must have occured
                            // in the CATCH or FINALLY meaning we mark it as in error
                            Program.CurrentApp.AppLevels[appLevel].TryStack[stackPos].TryPhase = 99;
                        }

                        stackPos--;
                    }
                }

                if (stackPos < 0)
                    appLevel--;
                else
                    break;
            }

            if (appLevel > 0 && stackPos >= 0)
            {
                // Found a Try with TryPhase==1
                TryClass tryCode = Program.CurrentApp.AppLevels[appLevel].TryStack[stackPos];

                Program.CurrentApp.CurrentAppLevel = appLevel;
                Program.CurrentApp.AppLevels[appLevel].PrgPos = tryCode.CasePos[0];
                tryCode.TryPhase = 2;
                Program.CurrentApp.InError = 1;
                Program.CurrentApp.InErrorTrap = true;
            }
            else
                appLevel = -1;  // No unsprung Try/Catch found

            // If i<0 then there is no TRY in play and we need
            // to see if there's an active ON ERROR command
            if (appLevel < 0 && Program.CurrentApp.OnErrorCommand.Length > 0)
            {
                Program.CurrentApp.InError = 2;
                Program.CurrentApp.InErrorTrap = true;
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        public static void ClearErrors() { Program.CurrentApp.Errors.Clear(); }
        public static int ErrorCount() { return Program.CurrentApp.Errors.Count; }

        // Classes can push a number of errors into there Error list so  CurrentError will point
        // at the first error pushed. If CurrentError<0 then grab the first error in the list,
        // otherwise the error number pointed at by CurrentError is returned.
        public static int LastErrorNo()
        {
            int result = 0;

            if (Program.CurrentApp is not null)
            {
                if (Program.CurrentApp.Errors.Count > 0)
                {
                    if (Program.CurrentApp.CurrentError < 0)
                        result = Program.CurrentApp.Errors[0].ErrorNo;
                    else
                        result = Program.CurrentApp.Errors[Program.CurrentApp.CurrentError].ErrorNo;
                }
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * Returns the current error object.  If CurrentError<0 then the first one is
         * returned.  Otherwise the error pointed at by CurrentError is returned.
         * ------------------------------------------------------------------------------------------*/
        public static JAXErrors GetCurrentError()
        {
            JAXErrors result = new();

            if (Program.CurrentApp is not null)
            {
                if (Program.CurrentApp.Errors.Count > 0)
                {
                    int i = Program.CurrentApp.CurrentError >= 0 ? Program.CurrentApp.CurrentError : 0;
                    if (i < 0) i = 0;
                    if (i >= Program.CurrentApp.Errors.Count) i = Program.CurrentApp.Errors.Count - 1;

                    result.ErrorNo = Program.CurrentApp.Errors[i].ErrorNo;
                    result.ErrorLine = Program.CurrentApp.Errors[i].ErrorLine;
                    result.ErrorMessage = Program.CurrentApp.Errors[i].ErrorMessage;
                    result.ErrorProcedure = Program.CurrentApp.Errors[i].ErrorProcedure;
                }
            }

            return result;
        }
    }
}
