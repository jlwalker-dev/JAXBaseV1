using JAXBase.Core;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_N
    {

        /* 
         * 
         *  NODEFAULT
         *  
         */
        public static string NoDefault(AppClass app, string cmdLine)
        {
            AppErrorHandling.ClearErrors();
            string result = string.Empty;

            try
            {
                // Clear off the DoDefaults flag
                app.AppLevels[Program.CurrentApp.CurrentAppLevel].DoDefault = false;
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


    }
}
