using System;

namespace JAXBase.Core.Extensions
{
    /// <summary>
    /// Default implementation of IExtensionContext.
    /// Expand registration methods as your runtime grows.
    /// </summary>
    public class DefaultExtensionContext : IExtensionContext
    {
        public void RegisterCommand(string name, Delegate handler, CommandMetadata? metadata = null)
        {
            // Wire into your command dispatcher here
            AppIO.DebugLog($"Registered command: {name}");
        }

        public void RegisterFunction(string name, Delegate handler, FunctionMetadata? metadata = null)
        {
            // Wire into function resolver
            AppIO.DebugLog($"Registered function: {name}");
        }

        public void RegisterClass(string className, Type classType, ClassMetadata? metadata = null)
        {
            // Wire into class factory
            AppIO.DebugLog($"Registered class: {className}");
        }

        public void RegisterLanguagePack(string languageCode, ILanguagePack pack)
        {
            // Store active language pack in AppClass or a LanguageManager
            AppIO.DebugLog($"Registered language pack: {languageCode}");
            // Example: Program.CurrentApp.ActiveLanguagePack = pack;
        }
    }
}