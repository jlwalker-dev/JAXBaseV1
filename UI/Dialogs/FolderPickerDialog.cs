using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace JAXBase.UI.Dialogs
{
    public class FolderPickerDialog : Window
    {
        private readonly Avalonia.Controls.ListBox folderList;
        private readonly Avalonia.Controls.TextBox pathTextBox;
        private string currentDirectory;

        public FolderPickerDialog()
        {
            this.Width = 600;
            this.Height = 500;
            this.Title = "Select Folder";
            this.Background = new SolidColorBrush(Colors.White);
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Path display
            var pathPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(8)
            };

            var pathLabel = new TextBlock
            {
                Text = "Folder:",
                VerticalAlignment = VerticalAlignment.Center
            };

            pathTextBox = new Avalonia.Controls.TextBox
            {
                Text = currentDirectory,
                IsReadOnly = true,
                Width = 480
            };

            pathPanel.Children.Add(pathLabel);
            pathPanel.Children.Add(pathTextBox);
            Grid.SetRow(pathPanel, 0);
            grid.Children.Add(pathPanel);

            // Folder list
            folderList = new Avalonia.Controls.ListBox();
            folderList.DoubleTapped += OnFolderListDoubleTapped;
            PopulateFolderList();
            Grid.SetRow(folderList, 1);
            grid.Children.Add(folderList);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(8)
            };

            var selectButton = new Avalonia.Controls.Button { Content = "Select Folder" };
            selectButton.Click += OnSelectClicked;

            var cancelButton = new Avalonia.Controls.Button { Content = "Cancel" };
            cancelButton.Click += OnCancelClicked;

            buttonPanel.Children.Add(selectButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            this.Content = grid;
        }

        private void PopulateFolderList()
        {
            folderList.Items.Clear();
            folderList.Items.Add(".."); // Parent directory

            try
            {
                foreach (var dir in Directory.GetDirectories(currentDirectory))
                {
                    folderList.Items.Add(Path.GetFileName(dir) + "/");
                }
            }
            catch (Exception ex)
            {
                folderList.Items.Add($"Error: {ex.Message}");
            }

            pathTextBox.Text = currentDirectory;
        }

        private void OnFolderListDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (folderList.SelectedItem is not string selected)
                return;

            if (selected == "..")
            {
                var parent = Directory.GetParent(currentDirectory);
                if (parent != null)
                {
                    currentDirectory = parent.FullName;
                    PopulateFolderList();
                }
            }
            else if (selected.EndsWith("/"))
            {
                currentDirectory = Path.Combine(currentDirectory, selected.TrimEnd('/'));
                PopulateFolderList();
            }
        }

        private void OnSelectClicked(object? sender, RoutedEventArgs e)
        {
            // Return the currently browsed folder (most common UX for folder pickers)
            this.Close(currentDirectory);
        }

        private void OnCancelClicked(object? sender, RoutedEventArgs e)
        {
            this.Close(null);
        }
    }
}