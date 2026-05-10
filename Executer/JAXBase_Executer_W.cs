using JAXBase.XBase;
using JAXBase.Utilities;
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
        public static async Task<string> Wait(ExecuterCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            string result = string.Empty;

            try
            {
                JAXObjects.Token answer = new();

                if (Program.CurrentApp.WaitWindow is not null)
                {
                    Program.CurrentApp.WaitWindow.Close();
                    Program.CurrentApp.WaitWindow = null;
                }

                if (eCodes.Expressions.Count == 1)
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                else if (Array.IndexOf(eCodes.Flags, "clear") > 0)
                    answer.Element.Value = "Press a key...";
                else
                    answer.Element.Value = string.Empty;

                string msg = answer.AsString();

                string varName = string.Empty;
                if (eCodes.To.Count > 0)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.To[0].Name);
                    if (answer.Element.Type.Equals("C"))
                        varName = answer.AsString();
                    else
                        throw new Exception("11|");
                }

                bool wait4 = Array.IndexOf(eCodes.Flags, "wait") > 0 || eCodes.To.Count > 0;
                Program.CurrentApp.WaitWindow = JAXLib.WaitWindow(Program.CurrentApp, msg, eCodes.At.row, eCodes.At.col, Array.IndexOf(eCodes.Flags, "clear") > 0, wait4, eCodes.TIME, out string retval);

                if (string.IsNullOrWhiteSpace(varName) == false)
                    await AppVars.SetVarFromExpression(varName, retval, true);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
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
        public static async Task<string> With(ExecuterCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                if (eCodes.Expressions.Count != 1)
                    throw new Exception($"10||WITH variable expression has {eCodes.Expressions.Count} components");

                JAXObjects.Token tk = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (tk.Element.Type.Equals("C") == false)
                    throw new Exception($"11||Expecting variable in WITH statement");

                // now look for the variable
                string varName = tk.AsString();
                AppIO.DebugLog($"With -> {varName}");

                JAXObjectWrapper? parent = null;

                if (varName[0] == '.' && Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count > 0)
                {
                    // TODO - this could be layered many deep
                    // With obj
                    //    with .obj
                    //        with .obj ...
                    string parentVar = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[^1];
                    JAXObjects.Token ptk = await AppVars.GetVarFromExpression(parentVar, null);

                    if (ptk.Element.Type.Equals("O") == false)
                        throw new Exception("11||WRONG! Parent in stack was not an object!");

                    parent = (JAXObjectWrapper)ptk.Element.Value;
                }

                // Make sure it's an object
                tk = await AppVars.GetVarFromExpression(varName, parent);

                if (tk.Element.Type.Equals("O") == false)
                    throw new Exception($"11||With variable {varName} is not an object");

                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Add(varName);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

    }
}
