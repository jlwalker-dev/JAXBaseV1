using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.Compiler
{
    public static class JAXBase_Compiler_C
    {
        /*
         * CLEAR [ALL | CLASS ClassName | CLASSLIB ClassLibraryName | CONSOLE [ConsoleName]
         *      | DEBUG | DLLS [cAliasNameList]
         *      | EVENTS | ERROR | FIELDS | GETS | MACROS | MEMORY 
         *      | MENUS | POPUPS | PROGRAM [FileName] | PROMPT | READ [ALL] | RESOURCES [FileName] 
         *      | TYPEAHEAD | WINDOWS [windowname]]
         *      
         * 
         * TYPE / [name]
         * 
         */
        public static string Clear(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(cmdRest) == false)
                {
                    jbc.GetNextToken(cmdRest, string.Empty, out string token);

                    if (JAXLib.InListC(token, "class", "classlib", "console", "dlls", "program", "read", "resources", "windows"))
                    {
                        cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out token).ToLower();

                        string mType = token switch
                        {
                            "class" => "C",
                            "classlib" => "V",
                            "console" => "N",
                            "dlls" => "L",
                            "program" => "P",
                            "prog" => "P",
                            "read" => "A",
                            "resources" => "S",
                            _ => throw new Exception("9999|")
                        };

                        result = Program.CurrentApp.JaxCompiler.CompilerXRef["CS"].ToString() + mType + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "XX0", []);
                    }
                    else
                    {
                        cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out token).ToLower();
                        string mType = token switch
                        {
                            "all" => "*",
                            "debug" => "D",
                            "events" => "E",
                            "error" => "R",
                            "errors" => "R",
                            "fields" => "F",
                            "memory" => "M",
                            "typeahead" => "T",
                            _ => throw new Exception("9999|")
                        };

                        result = Program.CurrentApp.JaxCompiler.CompilerXRef["CS"].ToString() + mType + AppClass.stmtDelimiter;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * COPY FILE FileName1 TO FileName2
         * 
         * COPY INDEXES IndexFileList | ALL 
         * 
         * COPY MEMO MemoFieldName TO FileName [ADDITIVE] [AS nCodePage]
         * 
         * COPY PROCEDURES TO FileName [AS nCodePage] [ADDITIVE]
         * 
         * COPY STRUCTURE TO TableName [FIELDS FieldList] 
         * 
         * COPY STRUCTURE EXTENDED TO FileName [DATABASE DatabaseName [NAME LongTableName]] [FIELDS FieldList]
         * 
         * COPY TO ARRAY ArrayName [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton] [Scope] [FOR lExpression1] [WHILE lExpression2] [NOOPTIMIZE]
         * 
         * COPY TO FileName 
         *      [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         *      [Scope] [FOR lExpression1] [WHILE lExpression2] 
         *      [ TYPE [ XLS | XL5 | XLSX | CALC ] | [ SDF | CSV  [DELIMITED [ WITH Delimiter | WITH BLANK | WITH TAB | WITH CHARACTER Delimiter ]]
         *      [AS nCodePage]
         *          
         *  
         */
        public static string Copy(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out string tok);
                tok = tok.ToLower();

                switch (tok)
                {
                    case "file":
                        result = jbc.Key_Parser(cmdRest, [tok], "XX0,TO3,FG1", []);
                        break;

                    case "indexes":
                        result = jbc.Key_Parser(cmdRest, [tok], "XX0,FG1", ["all"]);
                        break;

                    case "procedures":
                        result = jbc.Key_Parser(cmdRest, [tok], "XX0,TO3,AS0,FG1", []);
                        break;

                    case "structure":
                        result = jbc.Key_Parser(cmdRest, [tok], "TO3,AS0,FG1", []);
                        break;

                    case "to":
                        cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out tok); // Eat TO

                        jbc.GetNextToken(cmdRest, string.Empty, out tok); // Get next token

                        if (tok.Equals("array", StringComparison.OrdinalIgnoreCase))
                        {
                            // TO ARRAY
                            cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out _); // Eat ARRAY
                            result = "to" + AppClass.expDelimiter + "A" + AppClass.expParam + jbc.Generic_Parser(cmdRest, "XX0,FV3,SC0,FR0,WH0", ["nooptimize"]);
                        }
                        else
                        {
                            // TO File
                            result = "F" + AppClass.expParam + jbc.Generic_Parser(cmdRest, "XX0,FV3,SC0,FR0,WH0,TY2", ["nooptimize"]);
                        }
                        break;

                    default:
                        throw new Exception("10||Unkown copy command " + tok.ToUpper());
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * CREATE CLASS ClassName | ? [OF ClassLibraryName1 | ?] [AS cBaseClassName [FROM ClassLibraryName2]] [NOWAIT]
         * 
         * CREATE CLASSLIB ClassLibraryName
         * 
         * CREATE COLORSET ColorSetName
         * 
         * CREATE [FileName]
         * 
         * CREATE CURSOR alias_name ( FieldName1 FieldType [( nFieldWidth [, nPrecision] )] [NULL | NOT NULL] [AUTOINC [NEXTVALUE NextValue [STEP StepValue]]] [UNIQUE [COLLATE cCollateSequence]] [NOCPTRANS] [, FieldName2 ... ] | FROM ARRAY ArrayName [CODEPAGE nCodePage]
         * 
         * CREATE DATABASE [DatabaseName]
         * 
         * CREATE FORM [FormName] [AS cClassName FROM cClassLibraryName] [NOWAIT] [SAVE] [DEFAULT]
         * 
         * CREATE LABEL [FileName] [NOWAIT] [SAVE]
         * 
         * CREATE MENU [FileName] [NOWAIT] [SAVE]
         * 
         * CREATE PROJECT [FileName] [NOWAIT] [SAVE]
         * 
         * CREATE QUERY [FileName] [NOWAIT]
         * 
         * CREATE REPORT [FileName] [NOWAIT] [SAVE]
         * 
         * CREATE TABLE TableName1 ( FieldName1 FieldType [( nFieldWidth [, nPrecision] )] [NULL | NOT NULL] [AUTOINC [NEXTVALUE NextValue [STEP StepValue]]] [UNIQUE [COLLATE cCollateSequence]] [NOCPTRANS] [, FieldName2 ... ] | FROM ARRAY ArrayName  [CODEPAGE nCodePage]
         * 
         */
        public static string Create(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            cmdRest = jbc.GetNextToken(cmdRest, string.Empty, out string tok);
            tok = tok.ToLower();

            switch (tok)
            {
                case "class":
                    result = jbc.Key_Parser(cmdRest, [tok], "XX0,OF0,AS1,FM0,FG1", ["nowait"]);
                    break;

                case "classlib":
                case "colorset":
                case "database":
                    result = jbc.Key_Parser(cmdRest, [tok], "XX0", []);
                    break;

                case "form":
                    result = jbc.Key_Parser(cmdRest, [tok], "XX0,AS1,FM0,FG1", ["nowait", "save", "default"]);
                    break;

                case "label":
                case "menu":
                case "project":
                case "report":
                    result = jbc.Key_Parser(cmdRest, [tok], "XX0,FG1", ["nowait", "save"]);
                    break;

                case "query":
                    result = jbc.Key_Parser(cmdRest, [tok], "XX0,FG1", ["nowait"]);
                    break;

                case "cursor":
                case "table":
                    if (cmdRest.Contains(" FROM ARRAY ", StringComparison.OrdinalIgnoreCase))
                        result = jbc.CompilerXRef["CS"].ToString() + tok[..1].ToUpper() + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "XX0,FM0", []);
                    else
                        result = jbc.CompilerXRef["CS"].ToString() + tok[..1].ToUpper() + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "XX0,TB0", []);
                    break;

                default:    // Open the table designer
                    result = string.Empty + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdRest, "X00", []);
                    break;
            }

            return result;
        }
    }
}
