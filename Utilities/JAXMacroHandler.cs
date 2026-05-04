using JAXBase.Core;

namespace JAXBase.Utilities
{
    public class JAXMacroHandler
    {
        /*
         * This is the macro expansion handler!  Simply put we receive a command line
         * and find the first macro to expand and then loop.  
         * 
         * If there is a remark command somewhere in the line (&&) then the line is 
         * chopped off starting at the remark command.
         * 
         * If there is no macro to expand, it just returns what it has
         */
        public static async Task<string> Expand(AppClass app, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                int f = cmdLine.IndexOf("&&");

                // Was there a remark in the line?
                if (f == 0)
                    cmdLine = string.Empty;
                else if (f > 0)
                    cmdLine = cmdLine[..(f - 1)];

                f = cmdLine.IndexOf('&');

                if (f < 0)
                    result = cmdLine;
                else
                {

                    while (true)
                    {
                        if (f > 0)
                        {
                            result += cmdLine[..f];
                            cmdLine = cmdLine[f..];
                        }

                        cmdLine = JAXUtilities.GetNextToken(cmdLine, " .=", out string macro);

                        // Trim the & and get the macro value
                        macro = macro[1..];
                        JAXObjects.Token tok = await AppVars.GetVarFromExpression(macro, null);

                        cmdLine = tok.AsString().Trim() + cmdLine;

                        // Anything else to expand?
                        f = cmdLine.IndexOf('&');

                        if (f < 0)  // if not, add it to the result
                            result += cmdLine;
                    }
                }
            }
            catch (Exception e)
            {
                AppErrorHandling.SetError(9987,e.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                throw new Exception("9987|");
            }

            // Clean it up before returning the result
            result = result.Trim();

            return result;
        }
    }
}
