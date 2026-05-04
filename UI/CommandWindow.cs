using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.UI
{
    public static class CommandWindow
    {
        private static string _history = "";
        public static readonly string _classID="0000000000";

        public static JAXBase.FloatingPanel Create(AppClass app, string title = "Command Window")
        {
            if (JAXApp.MainWindowInstance == null)
                throw new System.InvalidOperationException("MainWindow has not been created yet.");

            var panel = JAXApp.MainWindowInstance.CreateFloatingPanel(title);

            var commandTextBox = new Avalonia.Controls.TextBox
            {
                Name = "txtBox",
                FontFamily = new Avalonia.Media.FontFamily("Consolas, monospace"),
                FontSize = 16,
                AcceptsTab = true,
                AcceptsReturn = false,
                IsReadOnly = false,
                IsEnabled = true,
                Text = "",
                CaretBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Black)
            };

            Avalonia.Controls.Canvas.SetLeft(commandTextBox, 6);
            Avalonia.Controls.Canvas.SetTop(commandTextBox, 6);
            panel.InnerCanvas.Children.Add(commandTextBox);

            panel.LayoutUpdated += OnFirstLayout;
            void OnFirstLayout(object? sender, System.EventArgs e)
            {
                panel.LayoutUpdated -= OnFirstLayout;
                if (commandTextBox != null && panel.InnerCanvas.Bounds.Width > 10)
                {
                    commandTextBox.Width = panel.InnerCanvas.Bounds.Width - 12;
                    commandTextBox.Height = panel.InnerCanvas.Bounds.Height - 12;
                }
            }

            commandTextBox.KeyDown += (s, e) => TxtBox_KeyDown(s, e, app, commandTextBox, panel);

            panel.SizeChanged += (s, e) => ResizeContent(panel, commandTextBox);

            panel.PointerPressed += (s, e) =>
            {
                commandTextBox.Focus();
                ResetCursor(commandTextBox);
            };

            panel.GotFocus += (s, e) => commandTextBox.Focus();

            // Inside CommandWindow.Create(), after adding the TextBox to the panel
            commandTextBox.Loaded += (s, e) =>
            {
                commandTextBox.Focus();
                ResetCursor(commandTextBox);
            };

            return panel;   // ← now returns the FloatingPanel
        }


        // Resize the text panel.  I think it's not docked because
        // because that was causing some issues visually.  May have
        // been the problem with the weird gaps on the right and
        // bottom edges.  Which I had paid more attention to that.
        private static void ResizeContent(JAXBase.FloatingPanel panel, Avalonia.Controls.TextBox txt)
        {
            if (txt == null) return;
            txt.Width = panel.InnerCanvas.Bounds.Width - 12;
            txt.Height = panel.InnerCanvas.Bounds.Height - 12;
        }

        // Go to the end of the command list
        private static void ResetCursor(Avalonia.Controls.TextBox txt)
        {
            txt.CaretIndex = txt.Text?.Length ?? 0;
            txt.Focus();
        }

        // This is where Grok lost his mind.  He could not figure out the command history thing at all and we went around and around
        // because I was too tired to actually sit down and look at the problem.  Got frustrated, said some particularly nasty things
        // to the poor child and went to bed.
        // 
        // Next day I woke up and solved the problem in about 45 minutes and I now have a decent understanding of the code.
        // It was time well spent.
        private static async void TxtBox_KeyDown(
            object? sender,
            Avalonia.Input.KeyEventArgs e,
            AppClass app,
            Avalonia.Controls.TextBox txt,
            JAXBase.FloatingPanel commandPanel)   // ← added parameter
        {
            if (e.Key == Avalonia.Input.Key.Enter && (e.KeyModifiers & Avalonia.Input.KeyModifiers.Shift) == 0)
            {
                e.Handled = true;

                string text = txt.Text ?? "";
                int caret = txt.CaretIndex;

                int lineStart = text.LastIndexOf('\n', caret - 1) + 1;
                string command = text.Substring(lineStart, caret - lineStart).Trim();

                if (string.IsNullOrWhiteSpace(command)) return;

                _history += (_history.Length < 1 ? "" : System.Environment.NewLine) + command;

                if (caret < text.Length)
                {
                    txt.Text = _history;
                }

                AppErrorHandling.ClearErrors();

                if (app.CurrentDS.JaxSettings.Alternate && !string.IsNullOrWhiteSpace(app.CurrentDS.JaxSettings.Alternate_Name))
                {
                    JAXLib.StrToFile(command, app.CurrentDS.JaxSettings.Alternate_Name, 1);
                }

                string compiled = app.JaxCompiler.CompileLine(command, false);
                string output = "";

                // ────────────────────────────────────────────────
                // Hide command window right before execution
                // ────────────────────────────────────────────────
                bool wasVisible = commandPanel.IsVisible;
                commandPanel.IsVisible = false;

                try
                {
                    if (compiled.Length > 1)
                    {
                        output = await app.JaxExecuter.ExecuteCommand(compiled) ?? "";

                        if (!string.IsNullOrEmpty(output))
                        {
                            JAXApp.MainWindowInstance?.AppendMainOutput(output);

                            if (app.CurrentDS.JaxSettings.Alternate && !string.IsNullOrWhiteSpace(app.CurrentDS.JaxSettings.Alternate_Name))
                            {
                                JAXLib.StrToFile(output, app.CurrentDS.JaxSettings.Alternate_Name, 3);
                            }
                        }
                    }

                    txt.Text += System.Environment.NewLine;
                    ResetCursor(txt);

                    if (AppErrorHandling.ErrorCount() > 0)
                    {
                        var err = AppErrorHandling.GetCurrentError();
                        var dialog = new AvErrorDialog();
                        dialog.SetMessage(err.ErrorMessage);
                        dialog.Title = $"Error {err.ErrorNo}";
                        await dialog.ShowDialog(JAXApp.MainWindowInstance!);
                    }
                }
                finally
                {
                    // Restore visibility and immediately give focus back to the textbox
                    commandPanel.IsVisible = wasVisible;

                    if (wasVisible)
                    {
                        txt.Focus();
                        ResetCursor(txt);   // also moves caret to end — usually what you want after execution
                    }
                }
            }
        }
    }
}
