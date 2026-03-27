using JAXBase.XBase;
using JAXBase.Utilities.Utilities;
using JAXBase.Core;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_W
    {
        /* 
         * 
         * WAIT [cMessageText] [TO VarName] [AT nRow, nColumn] [NOWAIT] [CLEAR | NOCLEAR] [TIMEOUT nSeconds]
         * 
         */
        public static async Task<string> Wait(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.ClearErrors();
            string result = string.Empty;

            try
            {
                JAXObjects.Token answer = new();

                if (jbe.App.WaitWindow is not null)
                {
                    jbe.App.WaitWindow.Close();
                    jbe.App.WaitWindow = null;
                }

                if (eCodes.Expressions.Count == 1)
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                else if (Array.IndexOf(eCodes.Flags, "clear") > 0)
                    answer.Element.Value = "Press a key...";
                else
                    answer.Element.Value = string.Empty;

                string msg = answer.AsString();

                string varName = string.Empty;
                if (eCodes.To.Count > 0)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.To[0].Name);
                    if (answer.Element.Type.Equals("C"))
                        varName = answer.AsString();
                    else
                        throw new Exception("11|");
                }

                bool wait4 = Array.IndexOf(eCodes.Flags, "wait") > 0 || eCodes.To.Count > 0;
                jbe.App.WaitWindow = JAXLib.WaitWindow(jbe.App, msg, eCodes.At.row, eCodes.At.col, Array.IndexOf(eCodes.Flags, "clear") > 0, wait4, eCodes.TIME, out string retval);

                if (string.IsNullOrWhiteSpace(varName) == false)
                    await jbe.App.SetVarFromExpression(varName, retval, true);
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * WITH ObjectName [AS <Type> [OF <Class Library>]]
         *     [Statements]
         * ENDWITH
         * 
         */
        public static async Task<string> With(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.ClearErrors();
            string result = string.Empty;

            try
            {
                if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                if (eCodes.Expressions.Count != 1)
                    throw new Exception($"10||WITH variable expression has {eCodes.Expressions.Count} components");

                JAXObjects.Token tk = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (tk.Element.Type.Equals("C") == false)
                    throw new Exception($"11||Expecting variable in WITH statement");

                // now look for the variable
                string varName = tk.AsString();
                jbe.App.DebugLog($"With -> {varName}");

                JAXObjectWrapper? parent = null;

                if (varName[0] == '.' && jbe.App.AppLevels[^1].WithStack.Count > 0)
                {
                    // TODO - this could be layered many deep
                    // With obj
                    //    with .obj
                    //        with .obj ...
                    string parentVar = jbe.App.AppLevels[^1].WithStack[^1];
                    JAXObjects.Token ptk = await jbe.App.GetVarFromExpression(parentVar, null);

                    if (ptk.Element.Type.Equals("O") == false)
                        throw new Exception("11||WRONG! Parent in stack was not an object!");

                    parent = (JAXObjectWrapper)ptk.Element.Value;
                }

                // Make sure it's an object
                tk = await jbe.App.GetVarFromExpression(varName, parent);

                if (tk.Element.Type.Equals("O") == false)
                    throw new Exception($"11||With variable {varName} is not an object");

                jbe.App.AppLevels[^1].WithStack.Add(varName);
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

    }
}
