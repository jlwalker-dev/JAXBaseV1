using JAXBase.Core;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_Legacy
    {
        /*
         * ? statement
         */
        public static async Task<string> QPrint(ExecutorCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            string result = await SolveQPrint(eCodes);

            if (AppErrorHandling.ErrorCount() == 0)
            {
                AppIO.SendToIDE(System.Environment.NewLine+result);

                if (Program.CurrentApp.CurrentDS.JaxSettings.Alternate && string.IsNullOrWhiteSpace(Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name) == false)
                    JAXLib.StrToFile(result, Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name, 1);
            }

            return string.Empty;
        }


        /*
         * ?? statement
         */
        public static async Task<string> QQPrint(ExecutorCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            string result = await SolveQPrint(eCodes);

            if (AppErrorHandling.ErrorCount() == 0)
            {
                AppIO.SendToIDE(result);

                if (Program.CurrentApp.CurrentDS.JaxSettings.Alternate && string.IsNullOrWhiteSpace(Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name) == false)
                    JAXLib.StrToFile(result, Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name, 1);
            }

            return "";
        }


        /*
         * Resolve the ? and ?? statement body
         */
        public static async Task<string> SolveQPrint(ExecutorCodes eCodes)
        {
            string result = string.Empty;

            foreach (ExCodeRPN rpn in eCodes.Expressions)
            {
                if (string.IsNullOrEmpty(rpn.RNPExpr))
                    continue;

                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(rpn.RNPExpr);

                // M and E types get converted to blank string
                if ("EM".Contains(answer.TType))
                    answer = new("");

                result += answer.AsString() + " ";

            }

            return result.Length > 0 ? result[..^1] : string.Empty;
        }
    }
}
