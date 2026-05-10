using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_Z
    {
        /*
         * 
         * ZAP IN nWorkArea | cAlias
         * 
         */
        public static async Task<string> Zap(ExecuterCodes eCodes)
        {
            string result = string.Empty;
            int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

            try
            {
                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                await Program.CurrentApp.CurrentDS.CurrentWA.DBFZap();
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            finally
            {
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa);
            }

            return result;
        }

    }
}
