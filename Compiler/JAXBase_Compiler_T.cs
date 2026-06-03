using JAXBase.Core;
using System.Text;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_T
    {

        /* TODO
         * 
         * 
         */
        public static string Text(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = "";

            try
            {
                string[] t = cmdLine.Split(Environment.NewLine);

                // Line 1 is the TEXT line
                cmdLine = t[0];

                // Get the rest of the text
                StringBuilder sb = new();
                for (int i = 1; i < t.Length; i++)
                    sb.Append(t[i] + Environment.NewLine);

                string txt = sb.ToString();

                // Get the command
                result = jbc.Generic_Parser(cmdLine, "TO0,FG0,PT0", ["additive", "textmerge", "noshow"]);

                // Get everything up to the next ENDTEXT
                result += AppClass.stmtDelimiter.ToString() + Program.CurrentApp.CompilerXRef["TX"].ToString() + txt;
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
