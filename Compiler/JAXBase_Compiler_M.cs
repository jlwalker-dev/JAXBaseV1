using JAXBase.Core;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler_M
    {
        /*
         * 
         * MODIFY CLASS     ClassName [OF ClassLibraryName1]
         * MODIFY COMMAND   FileName 
         * MODIFY FILE      FileName
         * MODIFY FORM      FormName
         * MODIFY GENERAL   GeneralField1
         * MODIFY LABEL     FileNamE
         * MODIFY MEMO      MemoField1 [, MemoField2 ...] 
         * MODIFY MENU      FileName
         * MODIFY PROJECT   FileName
         * MODIFY QUERY     FileName
         * MODIFY REPORT    FileName
         * MODIFY STRUCTURE
         * 
         */
        public static string Modify(JAXBase_Compiler jbc, string cmdRest)
        {
            string result = string.Empty;

            try
            {
                cmdRest = jbc.GetNextToken(cmdRest, " ", out string token);

                if (jbc.lang!.Abreviations.TryGetValue(token.ToLower(), out string? abbr))
                    token = abbr;

                if (jbc.lang!.CommandParts.TryGetValue(token.ToLower(), out string? cmdPart))
                    token = cmdPart;

                token = token.ToLower().Trim();

                string mType = token switch
                {
                    "blob" => "B",
                    "class" => "C",
                    "classlib" => "V",
                    "comm" => "P",
                    "command" => "P",
                    "form" => "M",
                    "gen" => "G",
                    "general" => "G",
                    "image" => "I",
                    "label" => "L",
                    "menu" => "U",
                    "memo" => "O",
                    "proj" => "J",
                    "project" => "J",
                    "query" => "Q",
                    "report" => "R",
                    "stru" => "S",
                    "structure" => "S",
                    _ => "F"
                };

                string[] kwrd = [mType];
                cmdRest = mType + " " + cmdRest;

                switch (mType)
                {
                    case "C":
                        result = jbc.Key_Parser(cmdRest, kwrd, "XX0,OF0", []);
                        break;

                    case "P":
                    case "F":
                    case "M":
                    case "L":
                    case "U":
                    case "O":
                    case "J":
                    case "Q":
                    case "R":
                    case "V":
                        result = jbc.Key_Parser(cmdRest, kwrd, "XX0", []);
                        break;


                    case "S":
                        result = jbc.Key_Parser(cmdRest, kwrd, "", []);
                        break;
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
