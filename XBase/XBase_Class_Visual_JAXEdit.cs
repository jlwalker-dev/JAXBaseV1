using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvaloniaEdit.TextMate;
using JAXBase.Core;
using JAXBase.Utilities;
using TextMateSharp.Grammars;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_JAXEdit : XBase_Class_Visual_Form
    {
        private readonly AvaloniaEdit.TextEditor _textEditor;
        private readonly AvaloniaEdit.Search.SearchPanel _searchPanel;
        private readonly TextMate.Installation _textMateInstallation;
        private readonly RegistryOptions _registryOptions;

        private string? _currentFilePath;
        private bool _isDirty;

        // Status bar controls
        private readonly Avalonia.Controls.TextBlock _statusLineColumn;
        private readonly Avalonia.Controls.TextBlock _statusLines;
        private readonly Avalonia.Controls.TextBlock _statusDirty;

        public new string MyBaseClass = "EditForm";
        public new string MyDefaultName = "editform";

        public XBase_Class_Visual_JAXEdit(JAXObjectWrapper jow, string name) : base(jow, "EditForm")
        {
            //SetVisualObject(null, MyBaseClass, MyDefaultName, false, UserObject.urw);

            _textEditor = new AvaloniaEdit.TextEditor
            {
                Text = "",
                ShowLineNumbers = true,
                FontFamily = new Avalonia.Media.FontFamily("Cascadia Code, Consolas, Menlo, Monospace"),
                FontSize = 18,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };

            // SearchPanel for Find/Replace
            _searchPanel = AvaloniaEdit.Search.SearchPanel.Install(_textEditor);

            // TextMate syntax highlighting - Dark theme
            _registryOptions = new TextMateSharp.Grammars.RegistryOptions(TextMateSharp.Grammars.ThemeName.DarkPlus);
            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);

            // Apply basic highlighting (this should show colors on comments, strings, numbers, etc.)
            ApplyXBaseHighlighting();

            // Activate TextMate with a generic source scope (gives basic highlighting)
            ApplyBasicTextMateHighlighting();

            ApplyXBaseGrammar();

            // Status bar
            _statusLineColumn = new Avalonia.Controls.TextBlock { Margin = new Avalonia.Thickness(10, 0, 10, 0) };
            _statusLines = new Avalonia.Controls.TextBlock { Margin = new Avalonia.Thickness(10, 0, 10, 0) };
            _statusDirty = new Avalonia.Controls.TextBlock { Margin = new Avalonia.Thickness(10, 0, 10, 0) };

            // Events
            _textEditor.TextChanged += async (sender, e) =>
            {
                if (!_isDirty)
                {
                    _isDirty = true;
                    await UpdateWindowTitle();
                }

                UpdateStatusBar();
            };

            _textEditor.TextArea.Caret.PositionChanged += (sender, e) => UpdateStatusBar();

            _textEditor.TextArea.KeyDown += CheckKey;

            // ------------------------------------------------------------------------------
            // Menus and layout (same as your current version)
            Avalonia.Controls.Menu mainMenu = new Avalonia.Controls.Menu();

            // File Menu
            Avalonia.Controls.MenuItem fileMenu = new() { Header = "_File" };

            Avalonia.Controls.MenuItem newItem = new Avalonia.Controls.MenuItem
            {
                Header = "_New",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.N, Avalonia.Input.KeyModifiers.Control)
            };
            newItem.Click += async (sender, e) => await NewFileAsync();

            Avalonia.Controls.MenuItem openItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Open...",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.O, Avalonia.Input.KeyModifiers.Control)
            };
            openItem.Click += async (sender, e) => await OpenFileAsync();

            Avalonia.Controls.MenuItem saveItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Save",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.W, Avalonia.Input.KeyModifiers.Control)
            };
            saveItem.Click += async (sender, e) => await SaveFileAsync();

            Avalonia.Controls.MenuItem saveAsItem = new Avalonia.Controls.MenuItem
            {
                Header = "Save _As..."
            };
            saveAsItem.Click += async (sender, e) => await SaveAsFileAsync();

            Avalonia.Controls.MenuItem exitItem = new Avalonia.Controls.MenuItem
            {
                Header = "E_xit",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Q, Avalonia.Input.KeyModifiers.Control)
            };
            exitItem.Click += async (sender, e) => await ExitAsync();

            fileMenu.Items.Add(newItem);
            fileMenu.Items.Add(openItem);
            fileMenu.Items.Add(saveItem);
            fileMenu.Items.Add(saveAsItem);
            fileMenu.Items.Add(new Avalonia.Controls.Separator());
            fileMenu.Items.Add(exitItem);


            // Edit Menu
            Avalonia.Controls.MenuItem editMenu = new Avalonia.Controls.MenuItem { Header = "_Edit" };

            Avalonia.Controls.MenuItem undoItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Undo",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Z, Avalonia.Input.KeyModifiers.Control)
            };
            undoItem.Click += (sender, e) => _textEditor.Undo();

            Avalonia.Controls.MenuItem redoItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Redo",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Y, Avalonia.Input.KeyModifiers.Control)
            };
            redoItem.Click += (sender, e) => _textEditor.Redo();

            Avalonia.Controls.MenuItem cutItem = new Avalonia.Controls.MenuItem
            {
                Header = "Cu_t",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.X, Avalonia.Input.KeyModifiers.Control)
            };
            cutItem.Click += (sender, e) => _textEditor.Cut();

            Avalonia.Controls.MenuItem copyItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Copy",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.C, Avalonia.Input.KeyModifiers.Control)
            };
            copyItem.Click += (sender, e) => _textEditor.Copy();

            Avalonia.Controls.MenuItem pasteItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Paste",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.V, Avalonia.Input.KeyModifiers.Control)
            };
            pasteItem.Click += (sender, e) => _textEditor.Paste();

            Avalonia.Controls.MenuItem selectAllItem = new Avalonia.Controls.MenuItem
            {
                Header = "Select _All",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.A, Avalonia.Input.KeyModifiers.Control)
            };
            selectAllItem.Click += (sender, e) => _textEditor.SelectAll();

            Avalonia.Controls.MenuItem findItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Find...",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.F, Avalonia.Input.KeyModifiers.Control)
            };
            findItem.Click += (sender, e) => ShowFindDialog();

            Avalonia.Controls.MenuItem replaceItem = new Avalonia.Controls.MenuItem
            {
                Header = "R_eplace...",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.H, Avalonia.Input.KeyModifiers.Control)
            };
            replaceItem.Click += (sender, e) => ShowReplaceDialog();

            Avalonia.Controls.MenuItem gotoItem = new Avalonia.Controls.MenuItem
            {
                Header = "_Goto Line...",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.G, Avalonia.Input.KeyModifiers.Control)
            };
            gotoItem.Click += (sender, e) => ShowGotoLineDialog();

            editMenu.Items.Add(undoItem);
            editMenu.Items.Add(redoItem);
            editMenu.Items.Add(new Avalonia.Controls.Separator());
            editMenu.Items.Add(cutItem);
            editMenu.Items.Add(copyItem);
            editMenu.Items.Add(pasteItem);
            editMenu.Items.Add(new Avalonia.Controls.Separator());
            editMenu.Items.Add(selectAllItem);
            editMenu.Items.Add(new Avalonia.Controls.Separator());
            editMenu.Items.Add(findItem);
            editMenu.Items.Add(replaceItem);
            editMenu.Items.Add(gotoItem);

            // Format Menu (placeholder)
            // Format Menu
            Avalonia.Controls.MenuItem formatMenu = new Avalonia.Controls.MenuItem { Header = "F_ormat" };

            Avalonia.Controls.MenuItem indentItem = new Avalonia.Controls.MenuItem
            {
                Header = "Indent",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Tab, Avalonia.Input.KeyModifiers.None)
            };
            indentItem.Click += (sender, e) => SimulateKeyPress(
                Avalonia.Input.Key.Tab,
                Avalonia.Input.KeyModifiers.None);

            Avalonia.Controls.MenuItem outdentItem = new Avalonia.Controls.MenuItem
            {
                Header = "Outdent",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.Tab, Avalonia.Input.KeyModifiers.Shift)
            };
            outdentItem.Click += (sender, e) => SimulateKeyPress(
                Avalonia.Input.Key.Tab,
                Avalonia.Input.KeyModifiers.Shift);

            Avalonia.Controls.MenuItem commentItem = new Avalonia.Controls.MenuItem
            {
                Header = "Comment",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.C, Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift)
            };
            commentItem.Click += (sender, e) => CommentSelection();

            Avalonia.Controls.MenuItem uncommentItem = new Avalonia.Controls.MenuItem
            {
                Header = "Uncomment",
                InputGesture = new Avalonia.Input.KeyGesture(Avalonia.Input.Key.U, Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift)
            };
            uncommentItem.Click += (sender, e) => UncommentSelection();

            formatMenu.Items.Add(indentItem);
            formatMenu.Items.Add(outdentItem);
            formatMenu.Items.Add(new Avalonia.Controls.Separator());
            formatMenu.Items.Add(commentItem);
            formatMenu.Items.Add(uncommentItem);

            // Put the menu together
            mainMenu.Items.Add(fileMenu);
            mainMenu.Items.Add(editMenu);
            mainMenu.Items.Add(formatMenu);


            // ------------------------------------------------------------------------------
            // Status Bar
            Avalonia.Controls.DockPanel statusBar = new Avalonia.Controls.DockPanel
            {
                Background = Avalonia.Media.Brushes.LightGray,
                Height = 24
            };
            statusBar.Children.Add(_statusLineColumn);
            statusBar.Children.Add(_statusLines);
            statusBar.Children.Add(_statusDirty);


            // ------------------------------------------------------------------------------



            // Layout
            Avalonia.Controls.Grid mainGrid = new Avalonia.Controls.Grid();
            mainGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });

            Avalonia.Controls.Grid.SetRow(mainMenu, 0);
            Avalonia.Controls.Grid.SetRow(_textEditor, 1);
            Avalonia.Controls.Grid.SetRow(statusBar, 2);

            mainGrid.Children.Add(mainMenu);
            mainGrid.Children.Add(_textEditor);
            mainGrid.Children.Add(statusBar);

            Avalonia.Controls.Canvas.SetLeft(mainGrid, double.NaN);
            Avalonia.Controls.Canvas.SetTop(mainGrid, double.NaN);

            mainGrid.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            mainGrid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            InnerCanvas.Children.Add(mainGrid);

            SuspendEvents();
            InnerCanvas.KeyDown += MainWindow_KeyDown;

            //InnerCanvas .Close() =>
            //{
            //    if (this._isDirty)
            //    {
            //        bool canClose = await ShowSaveChangesPromptAsync("Do you want to save changes before closing?");
            //        if (!canClose) e.Handled = true;
            //    }
            //};

            UpdateStatusBar();
        }

        /* ------------------------------------------------------------------------------------------*
         * Key handler for texteditor control
         * ------------------------------------------------------------------------------------------*/
        private void CheckKey(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.KeyModifiers == (Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift))
            {
                if (e.Key == Avalonia.Input.Key.C)
                {
                    CommentSelection();
                    e.Handled = true;
                }
                else if (e.Key == Avalonia.Input.Key.U)
                {
                    UncommentSelection();
                    e.Handled = true;
                }
            }
            else if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Avalonia.Input.Key.W:  // Save
                        if (_currentFilePath is null)
                            SaveAsFileAsync().Wait();
                        else
                            File.WriteAllTextAsync(_currentFilePath, _textEditor.Text).Wait();

                        fakeWindow.Close();
                        e.Handled = true;
                        break;

                    case Avalonia.Input.Key.O:  // Open
                        OpenFileAsync().Wait();
                        e.Handled = true;
                        break;

                    case Avalonia.Input.Key.N:  // New
                        NewFileAsync().Wait();
                        e.Handled = true;
                        break;

                    case Avalonia.Input.Key.Q:  // Quit
                        ExitAsync().Wait();
                        e.Handled = true;
                        break;
                }
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * POSTINIT - check for any parameters to process
         * ------------------------------------------------------------------------------------------*/
        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            _currentFilePath = App.CurrentDS.JaxSettings.Default + "untitled.prg";

            bool result = await base.PostInit(callBack, parameterList);

            string[] startCmd = UserProperties["startcommand"].AsString().Split(',');

            if (startCmd.Length > 0)
            {
                switch (startCmd[0].ToLower())
                {
                    case "open":
                        if (startCmd.Length > 1 && File.Exists(startCmd[1]))
                        {
                            string code = JAXLib.FileToStr(startCmd[1]);
                            _currentFilePath = startCmd[1];

                            UserProperties["filename"].Element.Value = JAXLib.JustFName(_currentFilePath);
                            UserProperties["filepath"].Element.Value = JAXLib.JustPath(_currentFilePath);
                            UserProperties["modified"].Element.Value = false;

                            _textEditor.Text = code;
                            _isDirty = false;

                            await UpdateWindowTitle();
                            UpdateStatusBar();

                            // Give keyboard focus to the editor at start of file
                            _textEditor.Focus();
                            _textEditor.TextArea.Caret.Position = new AvaloniaEdit.TextViewPosition(1, 1);
                        }
                        break;
                }
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * GetProperty
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "column":
                        returnToken.Element.Value = _textEditor.TextArea.Caret.Column;
                        break;

                    case "line":
                        returnToken.Element.Value = _textEditor.TextArea.Caret.Line;
                        break;

                    case "modified":
                        returnToken.Element.Value = _isDirty;
                        break;

                    case "text":
                        break;

                    case "selstart":
                        returnToken.Element.Value = _textEditor.SelectionStart;
                        break;

                    case "sellength":
                        returnToken.Element.Value = _textEditor.SelectionLength;
                        break;

                    case "seltext":
                        returnToken.Element.Value = _textEditor.SelectedText;
                        break;

                    case "value":
                        returnToken.Element.Value = _textEditor.Text;
                        break;

                    default:
                        // Process standard properties
                        result = 1;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    if (result < 9)
                        returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /* ------------------------------------------------------------------------------------------*
         * SetProperty
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Success, do no further processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *       0  - Successfully processed
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token objtk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "autocenter":
                            if (objtk.Element.Type == "L")
                                fakeWindow.AutoCenter = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "autohidescrollbar":
                            if (objtk.Element.Type.Equals("L") == false)
                                result = 11;
                            break;

                        case "backcolor":
                            int colorInt = JAXUtilities.ReturnColorInt(objtk.AsString());
                            _textEditor.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(colorInt));
                            objValue = colorInt;
                            break;

                        case "borderstyle":
                            if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 3))
                                fakeWindow.BorderStyle = objtk.AsInt();
                            else
                                result = objtk.Element.Type != "N" ? 11 : 41;
                            break;

                        case "caption":
                            fakeWindow.Title = objtk.AsString();
                            break;

                        case "editortype":
                            break;

                        case "filename":
                            break;

                        case "filepath":
                            break;

                        case "fontname":
                            _textEditor.FontFamily = objtk.AsString();
                            _textEditor.FontFamily ??= "Segoe UI";
                            _textEditor.FontFamily ??= "Arial";
                            _textEditor.FontFamily ??= "Hevelica";
                            break;

                        case "fontsize":
                            _textEditor.FontSize = objtk.AsDouble() / 72 * 96;
                            break;

                        case "forecolor":
                            _textEditor.Foreground = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                            objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                            break;

                        case "fontbold":
                            _textEditor.FontWeight = objtk.AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                            break;

                        case "fontitalic":
                            _textEditor.FontStyle = objtk.AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                            break;

                        case "height":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Height = objtk.AsDouble() + HeightDelta;
                                objValue = objtk.AsDouble();
                                me.originalHeight = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "icon":
                            if (objtk.Element.Type.Equals("C"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    // set up the image and apply it
                                    var icon = string.IsNullOrEmpty(objtk.AsString()) ? null : App.JaxImages!.GetImage(objtk.AsString(), out _);
                                    icon ??= App.JaxImages!.GetImage("*jax*", out _);

                                    JAXApp.MainWindowInstance!.Icon = new Avalonia.Controls.WindowIcon(App.JaxImages!.Resize(icon, 32, 32));
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "left":
                            fakeWindow.Left = objtk.AsDouble();
                            me.originalLeft = objtk.AsDouble();
                            break;

                        case "maxbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MaxButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "minbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MinButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "maxheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxHeight = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinHeight = 0;
                                else
                                    fakeWindow.MinHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "maxwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxWidth = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinWidth = 0;
                                else
                                    fakeWindow.MinWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "showwindow":
                            if (windowLocked)
                                result = 9702;
                            else if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 2))
                                fakeWindow.ShowWindow = objtk.AsInt();
                            else
                                result = 41;
                            break;

                        case "top":
                            fakeWindow.Top = objtk.AsDouble();
                            me.originalTop = objtk.AsDouble();
                            break;

                        case "visible":
                            if (objtk.Element.Type == "L")
                            {
                                // Visibility handled in Show/Hide
                                if (objtk.AsBool() && !InInit)
                                    windowLocked = true;
                            }
                            else
                                result = 11;
                            break;

                        case "width":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Width = objtk.AsDouble() + WidthDelta;
                                objValue = objtk.AsDouble();
                                me.originalWidth = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "windowstate":
                            if (objtk.Element.Type == "N")
                            {
                                int vfpState = objtk.AsInt();
                                fakeWindow.WindowState = vfpState switch
                                {
                                    1 => Avalonia.Controls.WindowState.Minimized,
                                    2 => Avalonia.Controls.WindowState.Maximized,
                                    _ => Avalonia.Controls.WindowState.Normal   // 0 or invalid → Normal
                                };
                                objValue = vfpState;
                            }
                            else
                                result = 11;  // type mismatch
                            break;

                        case "startcommand":
                            break;

                        case "column":
                            _textEditor.TextArea.Caret.Column = objtk.AsInt();
                            break;

                        case "line":
                            _textEditor.TextArea.Caret.Line = objtk.AsInt();
                            break;

                        case "righttoleft":
                            if (objtk.Element.Type.Equals("L"))
                                _textEditor.FlowDirection = objtk.AsBool() ? Avalonia.Media.FlowDirection.RightToLeft : Avalonia.Media.FlowDirection.LeftToRight;
                            else
                                result = 11;
                            break;

                        case "text":
                            break;

                        case "scrollbars":
                            bool auto = UserProperties["autohidescrollbar"].AsBool();

                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 3))
                                {
                                    switch (objtk.AsInt())
                                    {
                                        case 0: // None
                                            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
                                            _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
                                            break;

                                        case 1: // Horizontal
                                            if (auto)
                                                _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
                                            else
                                                _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;

                                            _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
                                            break;

                                        case 2: // Vertical
                                            _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;

                                            if (auto)
                                                _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
                                            else
                                                _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            break;

                                        case 3: // Both
                                            if (auto)
                                            {
                                                _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
                                                _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
                                            }
                                            else
                                            {
                                                _textEditor.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                                _textEditor.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            }
                                            break;
                                    }
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "sellength":
                            if (objtk.Element.Type.Equals("N"))
                                _textEditor.SelectionLength = _textEditor.SelectionStart + objtk.AsInt();
                            else
                                result = 11;
                            break;

                        case "selstart":
                            if (objtk.Element.Type.Equals("N"))
                                _textEditor.SelectionStart = objtk.AsInt();
                            else
                                result = 11;
                            break;

                        case "seltext":
                            if (objtk.Element.Type.Equals("C"))
                                _textEditor.SelectedText = objtk.AsString();
                            else
                                result = 11;
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            _textEditor.Text = objtk.AsString();
                            isProgrammaticChange = false;
                            break;

                        default:
                            // Process standard properties
                            result = 1;
                            break;
                    }

                    // Was the property retrieved?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }



        private async void MainWindow_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.KeyModifiers == (Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Shift))
            {
                if (e.Key == Avalonia.Input.Key.C)
                {
                    CommentSelection();
                    e.Handled = true;
                }
                else if (e.Key == Avalonia.Input.Key.U)
                {
                    UncommentSelection();
                    e.Handled = true;
                }
            }
        }



        /* ------------------------------------------------------------------------------------------*
         * Simulate a key press
         * ------------------------------------------------------------------------------------------*/
        private void SimulateKeyPress(Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers)
        {
            var args = new Avalonia.Input.KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers,
                Source = _textEditor.TextArea,
                RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent
            };

            // Focus the TextArea first to ensure it receives the input
            _textEditor.TextArea.Focus();

            // Raise the KeyDown event on the TextArea (where AvaloniaEdit listens)
            _textEditor.TextArea.RaiseEvent(args);

            // Also raise on the editor itself as fallback
            if (!args.Handled)
                _textEditor.RaiseEvent(args);
        }


        /* ------------------------------------------------------------------------------------------*
         * Find dialog
         * ------------------------------------------------------------------------------------------*/
        private void ShowFindDialog()
        {
            _searchPanel.IsReplaceMode = false;
            _searchPanel.Open();
            _searchPanel.Focus();
        }


        /* ------------------------------------------------------------------------------------------*
         * Replace dialog
         * ------------------------------------------------------------------------------------------*/
        private void ShowReplaceDialog()
        {
            _searchPanel.IsReplaceMode = true;
            _searchPanel.Open();
            _searchPanel.Focus();
        }


        /* ------------------------------------------------------------------------------------------*
         * Update the caption 
         * ------------------------------------------------------------------------------------------*/
        private async Task UpdateWindowTitle()
        {
            string title = "File: Untitled";

            if (!string.IsNullOrEmpty(_currentFilePath))
                title = "File: " + _currentFilePath;

            await SetProperty("caption", title, 0);
        }


        /* ------------------------------------------------------------------------------------------*
         * Update the status bar
         * ------------------------------------------------------------------------------------------*/
        private void UpdateStatusBar()
        {
            int line = _textEditor.TextArea.Caret.Line;
            int column = _textEditor.TextArea.Caret.Column;
            int totalLines = _textEditor.LineCount;

            _statusLineColumn.Text = $"Ln {line}, Col {column}";
            _statusLines.Text = $"Lines: {totalLines}";
            _statusDirty.Text = _isDirty ? "Modified" : "Saved";
        }



        /* ------------------------------------------------------------------------------------------*
         * Save file dialog
         * ------------------------------------------------------------------------------------------*/
        private async Task<bool> ShowSaveChangesPromptAsync(string message)
        {
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Save Changes",
                Width = 400,
                Height = 180,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var textBlock = new Avalonia.Controls.TextBlock
            {
                Text = message,
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };

            var yesButton = new Avalonia.Controls.Button { Content = "Yes", Width = 80, Margin = new Avalonia.Thickness(10) };
            var noButton = new Avalonia.Controls.Button { Content = "No", Width = 80, Margin = new Avalonia.Thickness(10) };
            var cancelButton = new Avalonia.Controls.Button { Content = "Cancel", Width = 80, Margin = new Avalonia.Thickness(10) };

            var buttonPanel = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            buttonPanel.Children.Add(cancelButton);

            var dialogGrid = new Avalonia.Controls.Grid();
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Star });
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });

            Avalonia.Controls.Grid.SetRow(textBlock, 0);
            Avalonia.Controls.Grid.SetRow(buttonPanel, 2);

            dialogGrid.Children.Add(textBlock);
            dialogGrid.Children.Add(buttonPanel);

            dialog.Content = dialogGrid;

            var tcs = new TaskCompletionSource<bool>();

            yesButton.Click += async (s, e) => { await SaveFileAsync(); tcs.SetResult(true); dialog.Close(); };
            noButton.Click += (s, e) => { tcs.SetResult(true); dialog.Close(); };
            cancelButton.Click += (s, e) => { tcs.SetResult(false); dialog.Close(); };

            // TODO - make sure this is correct
            await dialog.ShowDialog(JAXApp.MainWindowInstance!);
            return await tcs.Task;
        }


        /* ------------------------------------------------------------------------------------------*
         * Go to a line
         * ------------------------------------------------------------------------------------------*/
        private void ShowGotoLineDialog()
        {
            var dialog = new Avalonia.Controls.Window
            {
                Title = "Goto Line",
                Width = 320,
                Height = 160,
                WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            var label = new Avalonia.Controls.TextBlock
            {
                Text = "Enter line number:",
                Margin = new Avalonia.Thickness(20, 20, 20, 8),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var textBox = new Avalonia.Controls.TextBox
            {
                Width = 200,
                Margin = new Avalonia.Thickness(20, 0, 20, 20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };

            var okButton = new Avalonia.Controls.Button
            {
                Content = "Go",
                Width = 80,
                Margin = new Avalonia.Thickness(8)
            };

            var cancelButton = new Avalonia.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Margin = new Avalonia.Thickness(8)
            };

            // Button panel
            var buttonPanel = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 20)
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            // Main dialog layout with better Grid
            var dialogGrid = new Avalonia.Controls.Grid();
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });   // Label
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });   // TextBox
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = GridLength.Auto });   // Buttons

            Avalonia.Controls.Grid.SetRow(label, 0);
            Avalonia.Controls.Grid.SetRow(textBox, 1);
            Avalonia.Controls.Grid.SetRow(buttonPanel, 2);

            dialogGrid.Children.Add(label);
            dialogGrid.Children.Add(textBox);
            dialogGrid.Children.Add(buttonPanel);

            dialog.Content = dialogGrid;

            // Wire buttons
            okButton.Click += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out int line) && line > 0 && line <= _textEditor.LineCount)
                {
                    _textEditor.TextArea.Caret.Line = line;
                    _textEditor.TextArea.Caret.Column = 1;
                    _textEditor.TextArea.Caret.BringCaretToView();
                }
                dialog.Close();
            };

            cancelButton.Click += (s, e) => dialog.Close();

            // TODO - Make sure this is correct
            dialog.ShowDialog(JAXApp.MainWindowInstance!);
        }


        /* ------------------------------------------------------------------------------------------*
         * Clear the current code and make a new file
         * ------------------------------------------------------------------------------------------*/
        private async Task NewFileAsync()
        {
            if (_isDirty)
            {
                bool canProceed = await ShowSaveChangesPromptAsync("Do you want to save changes before creating a new file?");
                if (!canProceed) return;
            }

            _textEditor.Text = "";
            _currentFilePath = null;
            _isDirty = false;
            await UpdateWindowTitle();
        }


        /* ------------------------------------------------------------------------------------------*
         * Open a file
         * ------------------------------------------------------------------------------------------*/
        private async Task OpenFileAsync()
        {
            if (_isDirty)
            {
                bool canProceed = await ShowSaveChangesPromptAsync("Do you want to save changes before opening a new file?");
                if (!canProceed) return;
            }

            var options = new FilePickerOpenOptions
            {
                Title = "Open File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Programs") { Patterns = new[] { "*.prg", "*.qpr", "*.def" } },
                    new FilePickerFileType("Text Files") { Patterns = new[] { "*.txt", "*.csv", "*.SDF", "*.h" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                }
            };

            // Get the TopLevel that belongs to THIS specific form/window
            if (me.ParentAvaloniaWindow is not null)
            {
                /// Get the storage provider and then call the dialog
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Visual)me.ParentAvaloniaWindow);

                if (topLevel == null)
                {
                    // Fallback (should not happen)
                    topLevel = Avalonia.Controls.TopLevel.GetTopLevel(JAXApp.MainWindowInstance);
                }

                var storageProvider = topLevel!.StorageProvider;
                var files = await storageProvider.OpenFilePickerAsync(options);
                if (files.Count == 0) return;

                try
                {
                    using var stream = await files[0].OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    _textEditor.Text = await reader.ReadToEndAsync();

                    _currentFilePath = files[0].Path.LocalPath;
                    _isDirty = false;
                    await UpdateWindowTitle();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error opening file: {ex.Message}");
                }
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Save the file
         * ------------------------------------------------------------------------------------------*/
        private async Task SaveFileAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveAsFileAsync();
                return;
            }

            try
            {
                await File.WriteAllTextAsync(_currentFilePath, _textEditor.Text);
                _isDirty = false;
                await UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving file: {ex.Message}");
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Save a file as
         * ------------------------------------------------------------------------------------------*/
        private async Task SaveAsFileAsync()
        {
            if (me.ParentAvaloniaWindow is not null)
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Visual)me.ParentAvaloniaWindow);

                if (topLevel == null)
                {
                    // Fallback (should not happen)
                    topLevel = Avalonia.Controls.TopLevel.GetTopLevel(JAXApp.MainWindowInstance);
                }

                var storageProvider = topLevel!.StorageProvider;

                var options = new FilePickerSaveOptions
                {
                    Title = "Save File As",
                    SuggestedFileName = string.IsNullOrEmpty(_currentFilePath)
                        ? "Untitled.prg"
                        : Path.GetFileName(_currentFilePath),
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Text Files") { Patterns = ["*.txt", "*.prg", "*.jax"] },
                        new FilePickerFileType("All Files") { Patterns = ["*"] }
                    ]
                };

                var file = await storageProvider.SaveFilePickerAsync(options);
                if (file == null) return;

                try
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, _textEditor.Text);
                    _currentFilePath = file.Path.LocalPath;
                    _isDirty = false;
                    await UpdateWindowTitle();
                }
                catch (Exception ex)
                {
                    AppIO.DebugLog($"JAXEdit - Error saving file: {ex.Message}");
                }
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * Exit the editor
         * ------------------------------------------------------------------------------------------*/
        private async Task ExitAsync()
        {
            if (_isDirty)
            {
                bool canClose = await ShowSaveChangesPromptAsync("Do you want to save changes before exiting?");
                if (!canClose) return;
            }

            fakeWindow.Close();
        }



        /* ------------------------------------------------------------------------------------------*
         * Comment a block code
         * ------------------------------------------------------------------------------------------*/
        private async void CommentSelection()
        {
            var document = _textEditor.Document;
            var textArea = _textEditor.TextArea;
            var selection = textArea.Selection;

            int startLine;
            int endLine;

            if (selection.Length > 0)
            {
                startLine = selection.StartPosition.Line;
                endLine = selection.EndPosition.Line;
            }
            else
            {
                startLine = textArea.Caret.Line;
                endLine = startLine;
            }

            using (document.RunUpdate())
            {
                for (int lineNum = startLine; lineNum <= endLine; lineNum++)
                {
                    var line = document.GetLineByNumber(lineNum);
                    // Prepend exactly "*!*" at the very beginning of the line
                    document.Insert(line.Offset, "*!*");
                }
            }

            _isDirty = true;
            await UpdateWindowTitle();
            UpdateStatusBar();
        }


        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;

            switch (methodName.ToLower())
            {
                case "queryunload":
                    await ExitAsync();
                    break;

                case "destroy":
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }

        /* ------------------------------------------------------------------------------------------*
         * Uncomment a block code
         * ------------------------------------------------------------------------------------------*/
        private async void UncommentSelection()
        {
            var document = _textEditor.Document;
            var textArea = _textEditor.TextArea;
            var selection = textArea.Selection;

            // Get start and end lines from the current selection (or current line if nothing selected)
            int startLine;
            int endLine;

            if (selection.Length > 0)
            {
                startLine = selection.StartPosition.Line;
                endLine = selection.EndPosition.Line;
            }
            else
            {
                startLine = textArea.Caret.Line;
                endLine = startLine;
            }

            using (document.RunUpdate())
            {
                for (int lineNum = startLine; lineNum <= endLine; lineNum++)
                {
                    var line = document.GetLineByNumber(lineNum);
                    string lineText = document.GetText(line.Offset, line.Length);

                    if (lineText.TrimStart().StartsWith("*!*"))
                    {
                        int commentIndex = lineText.IndexOf("*!*");
                        if (commentIndex >= 0)
                        {
                            // Remove exactly "*!*" (3 characters)
                            document.Remove(line.Offset + commentIndex, 3);
                        }
                    }
                }
            }

            _isDirty = true;
            await UpdateWindowTitle();
            UpdateStatusBar();
        }


        private void ApplyXBaseGrammar()
        {
            // Activates TextMate with DarkPlus theme and a generic source scope.
            // This should give basic syntax coloring (comments, strings, numbers, some keywords).
            string scopeName = _registryOptions.GetScopeByLanguageId("source");
            if (!string.IsNullOrEmpty(scopeName))
            {
                _textMateInstallation.SetGrammar(scopeName);
            }
        }

        private void ApplyBasicTextMateHighlighting()
        {
            // This activates the TextMate engine with the DarkPlus theme.
            // It will color comments, strings, numbers, and many common keywords.
            // XBase-specific coloring will be limited until we add a real grammar file.
            string scopeName = _registryOptions.GetScopeByLanguageId("source");
            if (!string.IsNullOrEmpty(scopeName))
            {
                _textMateInstallation.SetGrammar(scopeName);
            }
        }


        private void ApplyXBaseHighlighting()
        {
        }



        public override string[] JAXMethods()
        {
            return
                [
                "addobject", "addproperty", "move", "readexpression", "readmethod", "refresh", "release",
                "removeobject", "resettodefault", "saveas", "saveasclass", "setall", "setfocus", "setmousepointer",
                "show", "writeexpression", "writemethod", "zorder",

                // New methods for the editor
                "editorcommand"
                ];
        }

        public override string[] JAXEvents()
        {
            return
            [
                "activate","click","dblclick","deactivate","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "moved","queryunload","resize","rightclick","scrolled","unload","visiblechanged"

                // New events for the editor
            ];
        }


        public override string[] JAXProperties()
        {
            return
            [
                "alwaysontop,L,false", "autocenter,L,true","autohidescrollbar,L,false",
                "backcolor,R,255|255|255","baseclass,C!,form","bindcontrols,L,true","borderstyle,N,3",
                "caption,C,Editor","class,C!,JAXEdit","classlibrary,C!,","closable,L,true","comment,C,","controlbox,L,true","controlcount,N!,0",
                "datasession,N,1","datasessionid,N!,1",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","FontSize,N,12","forecolor,R,0",
                "Height,N,720",
                "icon,C,*jax*",
                "keypreview,L,false",
                "left,N,0","lockscreen,L,false",
                "maxbutton,L,true","maxheight,N,-1","maxwidth,N,-1","minbutton,L,true","minheight,N,-1","minwidth,N,-1","mousepointer,n,0","moveable,L,true",
                "name,C,JAXEdit",
                "objects,*,",
                "parent,o!$,","parentclass,C!$,","picture,C,",
                "righttoleft,L,false",
                "scalefactor,N,0","scrollbars,n,3","showintaskbar,L,.T.","showwindow,N,2",
                "tabindex,N,1","tabstop,L,true","tag,C,","top,N,0","tooltiptext,c,",
                "visible,L,true",
                "width,N,1280","windowstate,N,0","windowtype,N,0",
                
                // Properties for editor control
                "startcommand,c,","editortype,n,0,","filename,c,","filepath,c,","modified,l!,false",
                "line,n,1","column,n,1","seltext,c,","selstart,n,0","sellength,n,0","value,c,","text,c,"
            ];
        }
    }
}
