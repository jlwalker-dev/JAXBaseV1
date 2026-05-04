using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_Q
    {

        /*
         * 
         * QUIT (nExpression)
         * 
         */
        public static async Task<string> Quit(AppClass app, ExecuterCodes? eCodes)
        {
            AppIO.DebugLog("QUIT command received");

            JAXObjects.Token answer = new();
            answer.Element.Value = 0;

            if (eCodes is not null)
            {
                if (eCodes.Expressions.Count > 0)
                {
                    answer = await app.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                    if (answer.Element.Type.Equals("N"))
                    {
                        AppIO.DebugLog($"QUIT evaluated to {answer.AsString()}");
                        app.ReturnValue.Element.Value = answer.AsInt();
                    }
                    else
                    {
                        AppIO.DebugLog($"QUIT evaluated to {answer.AsString()} type {answer.Element.Type}");
                        answer.Element.Value = 11;
                    }
                }
            }

            AppIO.DebugLog("Releasing all applevels and cache");
            app.AppLevels = [];
            app.CodeCache = [];
            app.PRGCache = [];

            // Exit the application with the given return code
            AppIO.DebugLog($"Requesting application exit with return code {app.ReturnValue.AsInt()}");
            System.Environment.Exit(answer.AsInt());

            AppIO.DebugLog("System.Environment.Exit failed to terminate the program");
            return "";
        }
    }
}
