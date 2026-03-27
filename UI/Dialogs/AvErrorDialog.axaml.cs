using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace JAXBase
{
    public partial class AvErrorDialog : Window
    {
        public AvErrorDialog()
        {
            InitializeComponent();
            this.Background= new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.PaleVioletRed);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetMessage(string message)
        {
            this.FindControl<TextBlock>("MessageText")!.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}