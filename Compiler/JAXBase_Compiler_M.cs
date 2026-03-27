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
                cmdRest = jbc.GetNextToken(cmdRest, " ", out string mType);
                mType = mType.ToLower().Trim();

                mType = mType switch
                {
                    "clas"=>"class",
                    "comm"=>"command",
                    "labe"=>"label",
                    "proj"=>"project",
                    "quer"=>"query",
                    "repo"=>"report",
                    _ => mType
                };

                string[] kwrd = [mType];
                cmdRest = mType + " " + cmdRest;

                switch (mType)
                {
                    case "class":
                        result = jbc.Key_Parser(cmdRest, kwrd, "XX0,OF0", []);
                        break;

                    case "command":
                    case "file":
                    case "form":
                    case "label":
                    case "menu":
                    case "memo":
                    case "project":
                    case "query":
                    case "report":
                        result = jbc.Key_Parser(cmdRest, kwrd, "XX0", []);
                        break;


                    case "structure":
                    case "stru":
                        result = jbc.Key_Parser(cmdRest, kwrd, "", []);
                        break;
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
