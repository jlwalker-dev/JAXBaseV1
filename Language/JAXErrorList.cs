/* ================================================================================================= *
 * This is the JAXErrorList class which contains a static method for retrieving error 
 * messages based on error numbers.  A secondary argument can be passed to insert into 
 * the error message if the message contains a placeholder.
 * 
 * Most of the error messages below 2300 are based on Visual FoxPro error numbers and 
 * messages, but there are many custom error numbers and messages that are specific 
 * to the JAXBase system.
 * 
 * You can find the main list of error messages in the Language/EnglishLanguagePack.cs file, which 
 * is loaded at runtime.
 * ================================================================================================= */
using JAXBase.Core;

namespace JAXBase.Language
{
    public static class JAXError
    {
        public static string JAXErrMsg(int errNo, string arg)
        {
            string template;

            if (!Program.CurrentApp.ActiveLanguagePack.ErrorMessages.TryGetValue(errNo, out template!))
                template = $"Unhandled system error {errNo}: {{0}}";

            // Simple placeholder replacement
            // Supports {*}, {0}, {!} etc. by replacing with the provided arg
            if (string.IsNullOrEmpty(arg))
            {
                return template;
            }

            string result = template
                .Replace("{*}", $"'{arg.ToUpper()}'")
                .Replace("{0}", arg)
                .Replace("{!}", arg.ToUpper());

            return result;
        }
    }
}