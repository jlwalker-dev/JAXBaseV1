using JAXBase.Core;
using JAXBase.XBase;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_T
    {


        /* TODO
         * 
         * TEXT
         * 
         */
        public static string Text(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*  TODO NOW
         *  
         * THROW nError, cMessage
         * 
         */
        public static async Task<string> Throw(ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {

                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (answer.Element.Type.Equals("N"))
                {
                    // Only throw the error if a positive non-zero value
                    if (answer.AsInt() > 0)
                        throw new Exception($"{answer.AsInt()}|{eCodes.MESSAGE}");
                    else
                        throw new Exception("1300|");
                }
                else
                    throw new Exception("11|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * TRY
         * 
         * Push a new TRYclass to the current app level's loop stack.  
         * 
         * If an error is occurs, the loopstack is searched to  find the most 
         * recent TRY position in the loopstack.
         * 
         * If a TRY loop is found, control is sent back to the level
         * where the try was registered and we'll look for the cooresponding 
         * CATCH statement.  If it's not found, that raises all sorts of
         * system errors.
         * 
         * Successfully finding a CATCH causes the code in the CATCH to be
         * executed until another CATCH, FINALLY, or ENDTRY is found. If 
         * another CATCH is found, the process looks for FINALLY or ENDTRY
         * in the current code block.
         * 
         * If FINALLY is found, the FINALLY flag replaces the CATCH flag in
         * the loop stack and processing continues until ENDTRY.
         * 
         * A RESUME or RESUME NEXT will send the code back to the app level
         * and position to restart processing, bypassing the EndTry and
         * resetting the phase to 0.
         * 
         * If the ENDTRY is found, the phase goes to 0, all app levels
         * above the current are tossed as the ENDTRY indicates that
         * we don't want to resume.
         * 
         * If another error occurs while a CATCH or FINALLY flag is exposed, it 
         * means there is no error handling in that CATCH or FINALLY block and
         * an unhandled exception error occurs.  If another TRY/CATCh or 
         * ON ERROR is in control, then control transfers to that.
         * 
         */
        public static string Try(ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                string tryCode = (eCodes.SUBCMD.Length > 0 ? eCodes.SUBCMD : throw new Exception("Missing TRY ID"));
                AppLoop.PushLoop(tryCode);

                int prgPos = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;
                string PrgCode = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];

                TryClass lc = new()
                {
                    Code = tryCode,
                    TryPhase = 1,
                    Level = Program.CurrentApp.AppLevels.Count - 1,
                    PrgPos = prgPos
                };

                string plead = AppClass.cmdByte.ToString();
                string cmdString;

                // Find all CATCH statements
                int thisPos = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrgPos;

                while (true)
                {
                    //Program.CurrentApp.utl.Conv64(Program.CurrentApp.CmdList.IndexOf("catch"), 2, out string b64);
                    cmdString = plead + Program.CurrentApp.MiscInfo["catchcmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + tryCode + AppClass.stmtDelimiter;
                    prgPos = Program.CurrentApp.utl.FindByteSequence(PrgCode, cmdString, thisPos);
                    if (prgPos < 0) break;

                    lc.CasePos.Add(prgPos);
                    thisPos = prgPos + cmdString.Length;
                }

                cmdString = plead + Program.CurrentApp.MiscInfo["finallycmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + tryCode + AppClass.stmtDelimiter;
                lc.FinallyPos = Program.CurrentApp.utl.FindByteSequence(PrgCode, cmdString, thisPos);
                cmdString = plead + Program.CurrentApp.MiscInfo["endtrycmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + tryCode + AppClass.stmtDelimiter;
                lc.EndTry = Program.CurrentApp.utl.FindByteSequence(PrgCode, cmdString, thisPos);

                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack[^1] = lc;
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return result;
        }
    }
}
