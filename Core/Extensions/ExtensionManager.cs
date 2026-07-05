using System.Reflection;
using System.Runtime.Loader;

namespace JAXBase.Core.Extensions
{
    /// <summary>
    /// Central manager for loading and registering JAXBase extensions.
    /// Supports on-demand loading with no unloading.
    /// </summary>
    public static class ExtensionManager
    {
        private static readonly Dictionary<string, IJAXBaseExtension> _loadedExtensions = [];
        private static IExtensionContext? _globalContext;   // Made nullable

        /// <summary>
        /// Initialize the manager with the global registration context.
        /// Call this once early in startup.
        /// </summary>
        public static void Initialize(IExtensionContext context)
        {
            _globalContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Load a language pack based on ISO code (e.g., "en", "es").
        /// </summary>
        public static async Task LoadLanguagePackAsync(string isoLanguage)
        {
            if (string.IsNullOrWhiteSpace(isoLanguage))
                isoLanguage = "en";

            string key = $"lang_{isoLanguage.ToLower()}";

            if (_loadedExtensions.ContainsKey(key))
                return; // already loaded

            // Build expected assembly name/path - adjust as needed for your layout
            string assemblyName = $"JAXBase.Language.{isoLanguage}";
            string dllPath = Path.Combine(AppContext.BaseDirectory, "Extensions", $"{assemblyName}.dll");

            try
            {
                Assembly assembly;

                if (File.Exists(dllPath))
                {
                    var loadContext = new AssemblyLoadContext(assemblyName, isCollectible: false);
                    assembly = loadContext.LoadFromAssemblyPath(dllPath);
                }
                else
                {
                    // Fallback to core assembly for English
                    assembly = typeof(Program).Assembly;
                }

                // Find types implementing IJAXBaseExtension
                var extensionType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IJAXBaseExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (extensionType != null)
                {
                    var extension = (IJAXBaseExtension)Activator.CreateInstance(extensionType)!;  // Null-forgiving operator

                    if (_globalContext == null)
                        throw new InvalidOperationException("ExtensionManager has not been initialized.");

                    extension.Initialize(_globalContext);
                    await extension.InitializeAsync(_globalContext);

                    _loadedExtensions[key] = extension;
                    AppIO.DebugLog($"Successfully loaded extension: {extension.Name} ({isoLanguage})");
                }
            }
            catch (Exception ex)
            {
                AppIO.DebugLog($"Failed to load language pack '{isoLanguage}': {ex.Message}");
                // Continue with English fallback
            }
        }
    }
}