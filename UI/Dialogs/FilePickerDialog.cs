using JAXBase.Core;

namespace JAXBase.UI.Dialogs
{
    public class FilePickerDialog : Avalonia.Controls.Window
    {
        private Avalonia.Controls.ListBox fileList;
        private Avalonia.Controls.TextBox pathTextBox;
        private string currentDirectory;

        public FilePickerDialog()
        {
            this.Width = 500;
            this.Height = 500;
            this.Title = "Select File";
            this.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);

            currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var grid = new Avalonia.Controls.Grid();
            grid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Auto });
            grid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Star });
            grid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Auto });

            // Path display
            var pathPanel = new Avalonia.Controls.StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };
            var pathLabel = new Avalonia.Controls.TextBlock { Text = "Folder:", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            pathTextBox = new Avalonia.Controls.TextBox { Text = currentDirectory, IsReadOnly = true, Width = 400 };
            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(pathTextBox);
            Avalonia.Controls.Grid.SetRow(pathPanel, 0);
            grid.Children.Add(pathPanel);

            // File list
            fileList = new Avalonia.Controls.ListBox();
            fileList.DoubleTapped += OnFileListDoubleTapped;
            PopulateFileList();
            Avalonia.Controls.Grid.SetRow(fileList, 1);
            grid.Children.Add(fileList);

            // Buttons
            var buttonPanel = new Avalonia.Controls.StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, Spacing = 10 };
            var openButton = new Avalonia.Controls.Button { Content = "Open" };
            openButton.Click += OnOpenClicked;
            var cancelButton = new Avalonia.Controls.Button { Content = "Cancel" };
            cancelButton.Click += OnCancelClicked;
            buttonPanel.Children.Add(openButton);
            buttonPanel.Children.Add(cancelButton);
            Avalonia.Controls.Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            this.Content = grid;
        }

        private void PopulateFileList()
        {
            fileList.Items.Clear();
            fileList.Items.Add(".."); // Parent directory

            try
            {
                // Add directories
                foreach (var dir in Directory.GetDirectories(currentDirectory))
                {
                    fileList.Items.Add(Path.GetFileName(dir) + "/");
                }

                // Add files
                foreach (var file in Directory.GetFiles(currentDirectory))
                {
                    fileList.Items.Add(Path.GetFileName(file));
                }
            }
            catch (Exception ex)
            {
                // Handle access errors (e.g., unauthorized)
                fileList.Items.Add($"Error: {ex.Message}");
            }

            pathTextBox.Text = currentDirectory;
        }

        private void OnFileListDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (fileList.SelectedItem is string selected)
            {
                if (selected == "..")
                {
                    var parent = Directory.GetParent(currentDirectory);
                    if (parent != null)
                    {
                        currentDirectory = parent.FullName;
                        PopulateFileList();
                    }
                }
                else if (selected.EndsWith("/"))
                {
                    currentDirectory = Path.Combine(currentDirectory, selected.TrimEnd('/'));
                    PopulateFileList();
                }
                // Ignore double-tap on files; use Open button for selection
            }
        }

        private void OnOpenClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (fileList.SelectedItem is string selected && !selected.EndsWith("/") && selected != "..")
            {
                this.Close(Path.Combine(currentDirectory, selected));
            }
        }

        private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close(null);
        }
    }

    public class DialogHelper
    {
        public async Task ShowFilePicker(AppClass app)
        {
            Avalonia.Controls.Window? owner = null;

            // Try to use the main window if it exists
            try
            {
                owner = JAXApp.GetMainWindow(); // Assuming JAXApp is your app class with the provided GetMainWindow method
            }
            catch (InvalidOperationException)
            {
                // Main window not available, create dummy owner
                owner = new Avalonia.Controls.Window
                {
                    Width = 1,
                    Height = 1,
                    Position = new Avalonia.PixelPoint(-10000, -10000), // Off-screen to ensure invisibility
                    ShowInTaskbar = false,
                    WindowState = Avalonia.Controls.WindowState.Minimized,
                    CanResize = false,
                    Background = Avalonia.Media.Brushes.Transparent // Optional: make fully transparent
                };

                // Show the dummy to create a native handle (required for ownership)
                owner.Show();
            }

            app.fileDialog = new FilePickerDialog();
            var selectedFile = await app.ShowDialogAsync<string?>(app.fileDialog, owner);

            app.ReturnValue.Element.Value = selectedFile ?? string.Empty;
        }
    }
}