using JAXBase.Core;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_A
    {

        /* 
         * ACTIVATE CONSOLE [name]
         * 
         * CONSOLE/name/AT/MenuExpr/flags
         */
        public static string Activate(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdRest, " ", out string device);

                switch (device.ToLower())
                {
                    case "cons":
                    case "console":
                        result = jbc.CompilerXRef["CS"].ToString() + "console" + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "XX0", []);
                        break;

                    default:
                        throw new Exception(string.Format("10||ACTIVATE type '{0}' is not implemented", device));
                }
            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }



        /* 
         * ADD OBJECT [PROTECTED] Name1 AS ClassName2 [NOINIT] [WITH cPropertylist]
         * 
         * ADD CLASS Name1 [OF ClassLibraryName2] TO ClassLibraryName3 [OVERWRITE]
         *
         * ADD TABLE Name1 | ?   [NAME LongTableName]
         * 
         * OBJCECT|CLASS|TABLE / Name1 / Name2 / Name3 / WITH exprList / Flags
         * 
         * 
         */
        public static string Add(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdRest, string.Empty, out string addCmd);

                if (addCmd.Equals("class", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["object"], "XX0,AS1,WT0,FG1", ["protected", "noinit"]);
                else if (addCmd.Equals("object", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["class"], "XX0,OF0,TO3,FG1", ["overwrite"]);
                else if (addCmd.Equals("table", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["table"], "XX0,NM0,FG1", []);
                else
                    throw new Exception(string.Format("1999||Unknown add type {0}", addCmd));

            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * ALTER TABLE TableName ADD COLUMN FieldName1 Type (width,precision) NULL|NOT NULL AUTOINC NEXTVALUE expr STEP expr UNIQUE | CANDIDATE [DESCENDING] 
         * ALTER TABLE TableName ALTER COLUMN FieldName1 Type (width,precision) NULL|NOT NULL AUTOINC NEXTVALUE expr STEP expr UNIQUE | CANDIDATE [DESCENDING] 
         * ALTER TABLE TableName DROP COLUMN name
         * SQL ALTER
         *      
         */
        public static string Alter(AppClass app, string cmdRest)
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



        /* 
         * 
         * APPEND [BLANK] [IN nWorkArea | cTableAlias] [NOMENU]
         * 
         * APPEND FROM ARRAY ArrayName [FOR lExpression] [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         * 
         * APPEND FROM FileName [FOR lExpression] FIELDS fieldlist [[DELIMITED [WITH Delimiter | WITH BLANK | WITH TAB | WITH CHARACTER Delimiter]] [TYPE  SDF | CSV | XLS | XL5 | XL8 | XLX [SHEET cSheetName]] [AS nCodePage]
         * 
         * APPEND GENERAL GeneralFieldName [FROM FileName] [DATA cExpression] [LINK] [CLASS OLEClassName] 
         * 
         * APPEND MEMO MemoFieldName FROM FileName [OVERWRITE] [AS nCodePage]
         * 
         * APPEND PROCEDURES FROM FileName   [AS nCodePage] [OVERWRITE]
         * 
         * TypeName / name / FROM / IN / FOR / AS / TYPE / DATA / CLASS / SHEET / FIELDS / FLAGS
         * 
         */
        public static string Append(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdRest, string.Empty, out string addCmd);

                if (addCmd.Length == 0)
                    result = jbc.StrictBreak(cmdRest, "FV1,SC0,FR0,WH0,NM0,TM0,TT0", ["noappend", "nodelete", "nomodify"], string.Empty);
                else if (addCmd.Equals("blank", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["blank"], "FR0,IN0,SS0", ["nomenu"]);
                else if (addCmd.Equals("from", StringComparison.OrdinalIgnoreCase))
                {
                    if (cmdRest.Contains(" array ", StringComparison.OrdinalIgnoreCase))
                        result = jbc.CompilerXRef["CS"].ToString() + "array" + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "FM1,IN0,FV3,FG1", ["nomenu"]);
                    else
                        result = jbc.CompilerXRef["CS"].ToString() + "file" + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "FM0,FR0,AS0,TY2,DA1,SH0,FV1,FG1", []);
                }
                else if (addCmd.Equals("general", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["general"], "XX0,FM1,DA0,CL0,FG1", ["link"]);
                else if (addCmd.Equals("memo", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["memo"], "XX0,FM0,AS0,FG1", ["overwrite"]);
                else if (addCmd.Equals("procedures", StringComparison.OrdinalIgnoreCase))
                    result = jbc.Key_Parser(cmdRest, ["procedures"], "FM1,AS0,FG1", ["overwrite"]);
                else
                    throw new Exception(string.Format("1999||Unknown add type {0}", addCmd));
            }
            catch (Exception ex)
            {
                jbc.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}

