using JAXBase.Core;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_S
    {

        /* TODO
         * 
         * SELECT nWorkArea | cTableAlias
         * 
         * SELECT [ALL | DISTINCT] [TOP nExpr [PERCENT]] Select_List_Item [, ...]
         *    FROM [FORCE] Table_List_Item [, ...]
         *    [[JoinType] JOIN DatabaseName!]Table [[AS] Local_Alias]
         *    [ON JoinCondition [AND | OR [JoinCondition | FilterCondition] ...] 
         *    [WITH (BUFFERING = lExpr)]
         *    [WHERE JoinCondition | FilterCondition [AND | OR JoinCondition | FilterCondition] ...]
         *    [GROUP BY Column_List_Item [, ...]] [HAVING FilterCondition [AND | OR ...]]
         *    [UNION [ALL] SELECTCommand]
         *    [ORDER BY Order_Item [ASC | DESC] [, ...]]
         *    [INTO StorageDestination | TO DisplayDestination]
         *    [PREFERENCE PreferenceName] [NOCONSOLE] [PLAIN] [NOWAIT]
         * 
         */
        public static string Select(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                if (cmdLine.Contains("FROM ", StringComparison.OrdinalIgnoreCase))
                {
                    // SELECT SQL command
                }
                else
                {
                    //result = jbc.Generic_Parser(cmdLine, "XX0,IN0,SS0", []);
                    result = jbc.StrictBreak(cmdLine, "XX0,IN0,SS0",[], "XX0");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * SORT TO TableName ON FieldName1 [/A | /D] [/C]
         *      [, FieldName2 [/A | /D] [/C] ...]   [ASCENDING | DESCENDING]
         *      [Scope] [FOR lExpression1] [WHILE lExpression2]
         *      [FIELDS FieldNameList   | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton] 
         *      [NOOPTIMIZE]
         * 
         */
        public static string Sort(JAXBase_Compiler jbc, string cmdLine)
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
    }
}
