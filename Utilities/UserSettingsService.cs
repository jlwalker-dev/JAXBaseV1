using Avalonia;
using System.Text.Json;


namespace JAXBase.Utilities
{
    // Settings model
    public class AppSettings
    {
        public double WindowWidth { get; set; } = 800;
        public double WindowHeight { get; set; } = 600;
        public double WindowLeft { get; set; } = -1;   // -1 = use default / center
        public double WindowTop { get; set; } = -1;
        public string? IconName { get; set; }          // e.g. "Assets/skull-icon.ico"
        public int Monitor { get; set; } = 1;
    }

    // Settings service
    public static class SettingsService
    {
        private static readonly string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JAXBase");
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings: {ex.Message}");
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
