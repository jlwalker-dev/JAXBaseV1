using System.Xml.Linq;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_R
    {

        /* TODO partial
         * 
         * RENAME command
         * 
         */
        public static string Rename(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, string.Empty, out string cmd);
                if (cmd.Equals("class",StringComparison.OrdinalIgnoreCase))
                {
                    // RENAME CLASS ClassName1 OF ClassLibraryName TO ClassName2
                    result = jbc.Key_Parser(cmdLine, ["class"], "XX0,OF0,TO3", []);
                }
                else if (cmd.Equals("table",StringComparison.OrdinalIgnoreCase))
                {
                    // RENAME TABLE TableName1[OF database] TO TableName2
                    result = jbc.Key_Parser(cmdLine, ["table"], "XX0,OF0,TO3", []);
                }
                else
                {
                    // RENAME FileName1 TO FileName2
                    result = jbc.Generic_Parser(cmdLine, "XX0,TO3", []);
                }
            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * REPLACE FieldName1 WITH eExpression1 [ADDITIVE] [, FieldName2 WITH eExpression2 [ADDITIVE]] ... [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [NOOPTIMIZE]
         * 
         * REPLACE FROM ARRAY ArrayName [FIELDS FieldList] [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [NOOPTIMIZE]
         * 
         * 
         */
        public static string Replace(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, string.Empty, out string cmd);

                if (cmd.Equals("from", StringComparison.OrdinalIgnoreCase))
                {
                    // REPLACE FROM ARRAY | JSON
                    result = jbc.Generic_Parser(cmdLine, "FM0,FL0,SC0,FR0,WH0,IN0", ["nooptimize"]);

                }
                else
                {
                    // REPLACE Field WITH Expression
                    result = jbc.StrictBreak(cmdLine, "FW0,SC0,FR0,WH0,IN0,SS0", ["nooptimize"],"FW0");
                    //result = jbc.Generic_Parser(cmdLine, "FW0,SC0,FR0,WH0,IN0,SS0", ["nooptimize"]);
                }
            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
