using JAXBase.Core;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_I
    {

        /* TODO - BEFORE flag everywhere?
         * 
         * INSERT INTO dbf_name [(FieldName1 [, FieldName2, ...])] VALUES (eExpression1 [, eExpression2, ...]) [BEFORE]
         * 
         * INSERT INTO dbf_name FROM ARRAY ArrayName | FROM MEMVAR | FROM NAME ObjectName [BEFORE]
         * 
         * INSERT INTO dbf_name [BLANK][BEFORE]
         * 
         * Returns: F/dbfExpr/fldExpr/valExpr
         *          A/dbfExpr/fromType/fromLoc
         *          B/dbfExpr///flags
         */
        public static string Insert(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                Dictionary<string, string> code = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string cmd = string.Empty;

                string values = jbc.lang!.CommandParts.TryGetValue("values", out string? val) ? ")" + val + "(" : throw new Exception("1999|VALUES");
                string from = jbc.lang!.CommandParts.TryGetValue("from", out string? frm) ? " " + frm + " " : throw new Exception("1999|FROM");

                if (cmdLine.Replace(" ", string.Empty).Contains(values, StringComparison.OrdinalIgnoreCase))
                {
                    cmd = "F";  // Varlist
                    result = jbc.Generic_Parser(cmdLine, "IT0,XX#,VL0", []);
                }
                else if (cmdLine.Contains(from, StringComparison.OrdinalIgnoreCase))
                {
                    cmd = "A";  // memory vars
                    result = jbc.Generic_Parser(cmdLine, "IT0,FM1", []);
                }
                else
                {
                    cmd = "B";  // append/insert a blank or empty record
                    result = jbc.Generic_Parser(cmdLine, "IN0", ["blank", "before"]);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
