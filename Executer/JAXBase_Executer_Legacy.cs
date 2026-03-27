using JAXBase.Core;
using JAXBase.Utilities.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_Legacy
    {
        /*
         * ? statement
         */
        public static async Task<string> QPrint(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.ClearErrors();
            string result = await SolveQPrint(jbe, eCodes);

            if (jbe.App.ErrorCount() == 0)
            {
                jbe.App.SendToIDE(System.Environment.NewLine+result);

                if (jbe.App.CurrentDS.JaxSettings.Alternate && string.IsNullOrWhiteSpace(jbe.App.CurrentDS.JaxSettings.Alternate_Name) == false)
                    JAXLib.StrToFile(result, jbe.App.CurrentDS.JaxSettings.Alternate_Name, 1);
            }

            return string.Empty;
        }


        /*
         * ?? statement
         */
        public static async Task<string> QQPrint(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.ClearErrors();
            string result = await SolveQPrint(jbe, eCodes);

            if (jbe.App.ErrorCount() == 0)
            {
                jbe.App.SendToIDE(result);

                if (jbe.App.CurrentDS.JaxSettings.Alternate && string.IsNullOrWhiteSpace(jbe.App.CurrentDS.JaxSettings.Alternate_Name) == false)
                    JAXLib.StrToFile(result, jbe.App.CurrentDS.JaxSettings.Alternate_Name, 1);
            }

            return string.Empty;
        }


        /*
         * Resolve the ? and ?? statement body
         */
        public static async Task<string> SolveQPrint(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            foreach (ExCodeRPN rpn in eCodes.Expressions)
            {
                if (string.IsNullOrEmpty(rpn.RNPExpr))
                    continue;

                if (rpn.RNPExpr.Contains("propertyinfo",StringComparison.OrdinalIgnoreCase))
                {
                    int iii = 0;
                }

                JAXObjects.Token answer = await jbe.App.SolveFromRPNString(rpn.RNPExpr);
                result += answer.AsString() + " ";
            }

            return result.Length > 0 ? result[..^1] : string.Empty;
        }


        /*
         * Place source code into the current AppLevel
         */
        public static string SourceCode(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.AppLevels[^1].CurrentLineOfCode = eCodes.COMMAND;
            return string.Empty;
        }
    }
}
