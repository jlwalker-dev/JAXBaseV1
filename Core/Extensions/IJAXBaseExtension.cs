namespace JAXBase.Core.Extensions
{
    /// <summary>
    /// Main interface that all JAXBase extensions (including language packs) must implement.
    /// </summary>
    public interface IJAXBaseExtension
    {
        string Name { get; }
        Version Version { get; }
        string Description { get; }

        void Initialize(IExtensionContext context);
        Task InitializeAsync(IExtensionContext context) => Task.CompletedTask;
    }

    /// <summary>
    /// Context passed to extensions during initialization for safe registration.
    /// </summary>
    public interface IExtensionContext
    {
        void RegisterCommand(string name, Delegate handler, CommandMetadata? metadata = null);
        void RegisterFunction(string name, Delegate handler, FunctionMetadata? metadata = null);
        void RegisterClass(string className, Type classType, ClassMetadata? metadata = null);
        void RegisterLanguagePack(string languageCode, ILanguagePack pack);
    }

    /// <summary>
    /// Language pack contract - provides localized keywords, errors, etc.
    /// </summary>
    public interface ILanguagePack
    {
        string LanguageCode { get; }                    // e.g., "en", "es"
        Dictionary<string, string> MathFunctions { get; }
        Dictionary<string, string> JAXCommands { get; }
        Dictionary<string, string> SetCommands { get; }
        Dictionary<string, string> CommandParts { get; }
        Dictionary<string, string> eCodeCommand { get; }
        Dictionary<string, string> JaxObjects { get; }
        Dictionary<string, string> SpecialKeys { get; }

        Dictionary<int, string> Phrase { get; }

        Dictionary<int, string> ErrorMessages { get; }
        // Add more dictionaries as needed (SET commands, SpecialKeys, etc.)
    }

    // Simple metadata classes
    public class CommandMetadata
    {
        public string HelpText { get; set; } = string.Empty;
        public bool IsThreadSafe { get; set; } = true;
    }

    public class FunctionMetadata
    {
        public string HelpText { get; set; } = string.Empty;
        public int MinParameters { get; set; } = 0;
        public int MaxParameters { get; set; } = int.MaxValue;
    }

    public class ClassMetadata
    {
        public string HelpText { get; set; } = string.Empty;
    }
}