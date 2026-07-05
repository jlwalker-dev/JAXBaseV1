using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace JAXBase.Core
{
    public partial class JAXApp : Avalonia.Application
    {
        // This is the desktop for the system!
        public static MainWindow? MainWindowInstance { get; set; }
        public static MainWindow GetMainWindow()
        {
            return MainWindowInstance ?? throw new InvalidOperationException("MainWindow has not been created yet.");
        }

        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            RequestedThemeVariant = ThemeVariant.Light;
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (!Program.CurrentApp.RuntimeFlag)
                {
                    var mainWindow = new MainWindow(Program.CurrentApp);
                    desktop.MainWindow = mainWindow;

                    // Assign the static reference here
                    //MainWindowInstance = mainWindow;
                }
                // In runtime mode, no window for now (app exits after bootstrap)
            }

            base.OnFrameworkInitializationCompleted();

            AppIO.LoadWindowSettings();
        }
    }
}