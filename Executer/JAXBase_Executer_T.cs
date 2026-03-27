using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_T
    {


        /* TODO
         * 
         * TEXT
         * 
         */
        public static string Text(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*  TODO NOW
         *  
         * THROW nError, cMessage
         * 
         */
        public static async Task<string> Throw(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {

                JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
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
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * TRY
         * 
         * Push the TRY loop flag to the loop stack.  
         * 
         * If an error is occurs, the loopstack is searched to  find the most 
         * recent TRY position in the loopstack.
         * 
         * If no TRY is found in the loop stack while in a class definition, 
         * special processing is performed (please see the explaination in
         * the BASE_CLASS.cs)
         * 
         * If a TRY loop flag is found, a cooresponding CATCH will be searched
         * for in the current code block.
         * 
         * If a corresponding CATCH flag cannot be found, and unhandled
         * exception will be raised and processing will stop for that code
         * block and the appropriate error set, after which control will be
         * sent to the calling code block.
         * 
         * Successfully finding a CATCH causes the code in the CATCH to be
         * executed until another CATCH, FINALLY, or ENDTRY is found.
         * 
         * If another CATCH is found, the process looks for FINALLY or ENDTRY
         * in the current code block.
         * 
         * If FINALLY is found, the FINALLY flag replaces the CATCH flag in
         * the loop stack and processing continues until ENDTRY.
         * 
         * When ENDTRY is found, the current CATCH or FINALLY loop flag is 
         * dropped and processing continues for that code block.
         * 
         * If another error occurs while a CATCH or FINALLY flag is exposed, it 
         * means there is no error handling in that CATCH or FINALLY block and
         * an unhandled exception error occurs.  Processing then ends for that 
         * code block and falls back to the parent code block with that new 
         * error in place.
         * 
         */
        public static string Try(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                string PrgCode = jbe.App.PRGCache[jbe.App.AppLevels[^1].PRGCacheIdx];

                string thisLoop = eCodes.SUBCMD.Length > 0 ? eCodes.SUBCMD : throw new Exception("Missing TRY ID");
                jbe.App.PushLoop(thisLoop); // We are just pushing the TRY to the loop stack
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return result;
        }
    }
}
