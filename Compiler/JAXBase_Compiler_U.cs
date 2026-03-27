using JAXBase.Core;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_U
    {

        /* TODO
         * 
         * USE TableName [IN nWorkArea | cAlias] [AGAIN] 
         *     [ALIAS cTableAlias] [EXCLUSIVE] [SHARED] [NOUPDATE] 
         *     [INDEX cIndex1 [ASCENDING | DESCENDING] [, cIndex2 [ASCENDING | DESCENDING]]
         * 
         * USE cDbName!Table [IN nWorkArea] [ALIAS cAlias] [WHERE lExpression] [NOUPDATE]
         * 
         */
        public static string Until(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                string loop = jbc.App.GetLoopStack();
                result = jbc.CompilerXRef["CS"].ToString() + loop + AppClass.stmtDelimiter + jbc.Struct_Parser(cmdLine, "UE", "XX*", []);
            }

            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }



        /* TODO
         *
         * UPDATE cSQLTable
         *    SET Column_Name1 = eExpression1 [, Column_Name2 = eExpression2 ...]
         *    [FROM [FORCE] Table_List_Item [[, ...] | [JOIN [ Table_List_Item]]]
         *    WHERE FilterCondition
         *
         * UPDATE FROM nWorkArea | cAlias TO SQLTable
         * 
         */
        public static string Update(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;
            try
            {
                // Is it the old update with no where or join?
                if (cmdLine.Contains(" join ", StringComparison.OrdinalIgnoreCase) == false && cmdLine.Contains(" where ", StringComparison.OrdinalIgnoreCase) == false)
                {
                    // Backwards compatible UPDATE (same as replace)
                    // result=JAXBase_Compile_R.Replace(app,cmdLine);
                }
                else
                {
                    // SQL Update
                    throw new Exception("SQL UPDATE is not implemented in version 1");
                }
            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }
            return result;
        }



        /* TODO
         * 
         * USE TableName [IN nWorkArea | cAlias] [AGAIN] 
         *     [ALIAS cTableAlias] [EXCLUSIVE] [SHARED] [NOUPDATE] 
         *     [INDEX cIndex1 [ASCENDING | DESCENDING] [, cIndex2 [ASCENDING | DESCENDING]]
         * 
         * USE cDbName!Table [IN nWorkArea] [ALIAS cAlias] [WHERE lExpression] [NOUPDATE]
         * 
         */
        public static string Use(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
            }

            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }
    }
}
