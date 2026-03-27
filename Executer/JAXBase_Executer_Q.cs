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
            app.DebugLog("QUIT command received");

            JAXObjects.Token answer = new();
            answer.Element.Value = 0;

            if (eCodes is not null)
            {
                if (eCodes.Expressions.Count > 0)
                {
                    answer = await app.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                    if (answer.Element.Type.Equals("N"))
                    {
                        app.DebugLog($"QUIT evaluated to {answer.AsString()}");
                        app.ReturnValue.Element.Value = answer.AsInt();
                    }
                    else
                    {
                        app.DebugLog($"QUIT evaluated to {answer.AsString()} type {answer.Element.Type}");
                        answer.Element.Value = 11;
                    }
                }
            }

            app.DebugLog("Releasing all applevels and cache");
            app.AppLevels = [];
            app.CodeCache = [];
            app.PRGCache = [];

            app.DebugLog("Releasing all consoles");
            // Release all consoles
            //foreach (KeyValuePair<string, JAXConsole> c in app.JAXConsoles)
            //    c.Value.Release();

            // Exit the application with the given return code
            app.DebugLog($"Requesting application exit with return code {app.ReturnValue.AsString()}");
            Application.Exit();
            return string.Empty;
        }
    }
}
