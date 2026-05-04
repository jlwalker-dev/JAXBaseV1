using JAXBase.Core;
using JAXBase.XBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAXBase.Executer
{
    internal class JAXBase_Executer_H
    {
        /* TODO
         * 
         * HELP
         * 
         */
        public static string Help(JAXBase_Executer jbe, ExecuterCodes eCodes)
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
