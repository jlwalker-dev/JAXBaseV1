using JAXBase.Core;

namespace JAXBase.Executor
{
    internal class JAXBase_Executor_Internal
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
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
