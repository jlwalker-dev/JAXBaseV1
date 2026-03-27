using JAXBase.Core;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_N
    {

        /* 
         * 
         *  NODEFAULT
         *  
         */
        public static string NoDefault(AppClass app, string cmdLine)
        {
            app.ClearErrors();
            string result = string.Empty;

            try
            {
                // Clear off the DoDefaults flag
                app.AppLevels[^1].DoDefault = false;
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


    }
}
