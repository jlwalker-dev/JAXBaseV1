using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_G
    {

        /* TODO NOW
         * 
         * GATHER
         * 
         */
        public static string Gather(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO NOW
         * 
         * GETEXPR()
         * 
         */
        public static string GetExpr(string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * GOTO
         * 
         * Move Record Pointer to record potision
         * 
         */
        public static async Task<string> Goto( ExecutorCodes eCodes)
        {
            string result = string.Empty;

            // Where are we?
            int ds = Program.CurrentApp.CurrentDataSession;
            int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

            try
            {

                if (eCodes.SESSION > 0)
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);

                int cwa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (eCodes.Expressions.Count != 1) throw new Exception("10|");

                JAXObjects.Token tk = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (tk.Element.Type.Equals("C"))
                {
                    if (tk.AsString().Equals("TOP"))
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("TOP");
                    else if (tk.AsString().Equals("BOTTOM"))
                        await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord("BOTTOM");
                    else
                        throw new Exception($"12|{tk.AsString()}");
                }
                else if (tk.Element.Type.Equals("N"))
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFGotoRecord(tk.AsInt());
                else 
                    throw new Exception("11|");

                // Back to where we were
                Program.CurrentApp.CurrentDS.SelectWorkArea(cwa);

                Program.CurrentApp.SetDataSession(ds);
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa); // Restore workarea
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
