using JAXBase.Core;

namespace JAXBase.Executer
{
    internal class JAXBase_Executer_Internal
    {

        /* TODO
         * 
         * Add to the ProcMap of this applevel
         * 
         */
        public static string ProcMap(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
