using JAXBase.Core;
using JAXBase.Core.Extensions;

namespace JAXBase.Language
{
    /// <summary>
    /// Built-in English language pack. Remains part of the core.
    /// </summary>
    public class EnglishLanguagePack : IJAXBaseExtension, ILanguagePack
    {
        // ILanguagePack implementation
        public string LanguageCode => "en";

        public Dictionary<string, string> MathFunctions { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> JAXCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> CommandParts { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> eCodeCommand { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> SetCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> JaxObjects { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> SpecialKeys { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<int, string> Phrase { get; } = new Dictionary<int, string>();

        public Dictionary<int, string> ErrorMessages { get; } = new Dictionary<int, string>();

        // IJAXBaseExtension implementation
        public string Name => "JAXBase English Language Pack";
        public Version Version => new(1, 0, 0);
        public string Description => "Built-in English language support for JAXBase.";


        public void Initialize(IExtensionContext context)
        {
            // Populate Keywords from existing lists
            foreach (var cmd in JAXLanguageLists.JAXCommands)
                JAXCommands[cmd] = cmd;

            foreach (var fn in JAXLanguageLists.MathFunctions)
            {
                string key = fn.TrimEnd('(').ToUpper();
                MathFunctions[key] = key;
            }

            foreach (var setCmd in JAXLanguageLists.SetCommands)
                SetCommands[setCmd] = setCmd;

            foreach (var eCodeCmd in JAXLanguageLists.eCodeCommands)
                eCodeCommand[eCodeCmd] = eCodeCmd;

            foreach (var obj in JAXLanguageLists.JAXObjects)
                JaxObjects[obj] = obj;

            foreach (var key in JAXLanguageLists.SpecialKeys)
                SpecialKeys[key] = key;

            foreach (var part in JAXLanguageLists.JAXCommandParts)
                CommandParts[part] = part;

            foreach (var err in JAXError.ErrorMessages)
                ErrorMessages[err.Key] = err.Value;

            foreach (var msg in JAXLanguageLists.JAXPrases)
                Phrase[msg.Key] = msg.Value;

            context.RegisterLanguagePack(LanguageCode, this);
            AppIO.DebugLog("English language pack initialized.");
        }

        public Task InitializeAsync(IExtensionContext context) => Task.CompletedTask;
    }
}