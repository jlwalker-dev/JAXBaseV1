using JAXBase.Core;
using JAXBase.XBase;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_O
    {
        /* 
         * Set the ON command by storing the source code passed
         * to it for later execution.  If no source code is
         * passed, the command is effectively turned off.
         * 
         *      ON ERROR
         *      ON KEY LABEL
         *      ON SHUTDOWN
         * 
         */
        public static string On(ExecuterCodes eCodes)
        {
            try
            {
                switch (eCodes.SUBCMD.ToLower())
                {
                    case "L":       // ON KEY LABEL
                        // Set or remove the on key handler from the dictionary
                        string keylabel = eCodes.ON.Trim().ToLower();
                        string code2Execute = eCodes.COMMAND.Trim();

                        if (Program.CurrentApp.OnKeyLabel.ContainsKey(keylabel) == false && code2Execute.Length > 0)
                        {
                            Program.CurrentApp.OnKeyLabel.Add(keylabel, code2Execute); // Add key label
                            AppIO.SetOnKeyLabel(keylabel, false);
                        }
                        if (Program.CurrentApp.OnKeyLabel.ContainsKey(keylabel) && code2Execute.Length > 0)
                        {
                            Program.CurrentApp.OnKeyLabel[keylabel] = code2Execute;    // Update key label
                            AppIO.SetOnKeyLabel(keylabel, false);
                        }
                        else if (Program.CurrentApp.OnKeyLabel.ContainsKey(keylabel) && code2Execute.Length == 0)
                        {
                            Program.CurrentApp.OnKeyLabel.Remove(keylabel);            // Remove key label
                            AppIO.SetOnKeyLabel(keylabel, false);
                        }

                        if (Program.CurrentApp.OnKeyLabel.ContainsKey(keylabel) == false)
                            Program.CurrentApp.OnKeyLabel.Add(keylabel, eCodes.COMMAND);
                        else
                            Program.CurrentApp.OnKeyLabel[keylabel] = eCodes.COMMAND;
                        break;

                    case "E":       // ON ERROR
                        Program.CurrentApp.OnErrorCommand = eCodes.COMMAND;
                        break;

                    case "S":       // ON SHUTDOWN
                        Program.CurrentApp.OnShutDownCommand = eCodes.COMMAND;
                        break;

                    default:
                        throw new Exception($"1999||Unsupported ON code '{eCodes.SUBCMD}'");
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /* TODO 
         * 
         * OPEN DATABASE
         * 
         */
        public static string Open(string CmdString)
        {
            return string.Empty;
        }


        /* 
         * 
         * OTHERWISE
         * If we stumble onto this, we will look for an endcase because we should only be loading 
         * this command in a DO CASE statement. The DO CASE statement jumps through the related 
         * case statements until if finds a case expression that is true, otherwise, or endcase.
         * 
         * When an expression is true, it starts with the next command record and continues until
         * it finds another case statement, otherwise or an end case.
         * 
         */
        public static string Otherwise( ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                string cEndCase = AppClass.cmdByte.ToString() + Program.CurrentApp.MiscInfo["endcasecmd"] + eCodes.SUBCMD;

                // Find the endcase
                int pos = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx].IndexOf(cEndCase);

                if (pos < 0)
                    throw new Exception("1211|");   // If/Else/Endif stmt is missing
                else
                {
                    Program.CurrentApp.utl.Conv64(pos, 3, out string pos2);
                    result = "Y" + pos2; // Return the position of the endcase
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
