using Avalonia.Controls;
using Avalonia.Platform.Storage;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_CodeBox : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "CodeBox";
        public new string MyDefaultName { get; } = "codebox";

        private bool _isDirty = false;
        private string _currentFilePath = "";
        private readonly AvaloniaEdit.Search.SearchPanel _searchPanel;

        public AvaloniaEdit.TextEditor CodeBx => (AvaloniaEdit.TextEditor)me.avaloniaObject!;

        public XBase_Class_Visual_CodeBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new AvaloniaEdit.TextEditor(), "CodeBox", "codebox", true, UserObject.urw);
            _searchPanel = AvaloniaEdit.Search.SearchPanel.Install(CodeBx);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            // Was a filename sent?
            string filename = UserProperties["filename"].AsString();
            if (string.IsNullOrEmpty(filename) == false)
                await OpenFileAsync(filename);

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Successfully processed but do not update user property value
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "sellength":
                            if (tk.Element.Type.Equals("N"))
                                CodeBx.SelectionLength = CodeBx.SelectionStart + tk.AsInt();
                            else
                                result = 11;
                            break;

                        case "selstart":
                            if (tk.Element.Type.Equals("N"))
                            {
                                isProgrammaticChange = true;
                                CodeBx.SelectionStart = tk.AsInt();
                                isProgrammaticChange = false;
                            }
                            else
                                result = 11;
                            break;

                        case "seltext":
                            if (tk.Element.Type.Equals("C"))
                                CodeBx.SelectedText = tk.AsString();
                            else
                                result = 11;
                            break;

                        case "text":
                            isProgrammaticChange = true;
                            CodeBx.Text = tk.AsString();
                            isProgrammaticChange = false;
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            CodeBx.Text = tk.AsString();
                            //CodeBx.SetValue(tk.AsString());
                            isProgrammaticChange = false;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = JAXLib.InList(result, 0, 9) ? 9 : result;
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 9))
                    {
                        // Did we process it?
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
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
                    case "sellength":
                        returnToken.Element.Value = CodeBx.SelectionLength;
                        break;

                    case "selstart":
                        returnToken.Element.Value = CodeBx.SelectionStart;
                        break;

                    case "seltext":
                        returnToken.Element.Value = CodeBx.SelectedText;
                        break;

                    case "text":
                        returnToken.Element.Value = CodeBx.Text ?? string.Empty;
                        break;

                    case "value":
                        returnToken.Element.Value = CodeBx.Text ?? string.Empty;
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]);
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","move","readexpression","readmethod","refresh","resettodefault","saveclass",
                "settooriginalvalue","setfocus","textcommand","writeexpression","writemethod","zorder",
                ];
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus",
                "init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","valid","visiblechanged","when"
                ];
        }


        /*------------------------------------------------------------------------------------------*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - can't change after initialization
         *          $ Special Handling - do not auto process
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
                [
                "anchor,n,0",
                "backcolor,R,255|255|255","backstyle,n,1","bordercolor,R,0","borderstyle,n,1","borderwidth,n,1","baseclass,C!,codebox",
                "class,C!,codebox","classlibrary,C!,","Comment,C,",
                "Enabled,L,true",
                "filename,c,","fontbold,L,false","fontitalic,L,false","fontname,C,Courier New","fontsize,N,9","forecolor,R,0","format,c,",
                "height,n,50","hotkey,c,",
                "left,N,0",
                "name,c,codebox",
                "originalvalue,,",
                "parent,o!,","parentclass,C!,",
                "readonly,l,false","righttoleft,L,false",
                "scrollbars,n,2","sellength,n,0","selstart,n,0","seltext,c,",
                "tabindex,n,1","tabstop,l,true","tag,C,","text,c,","top,N,0","tooltiptext,c,",
                "value,C,","visible,l,true",
                "width,N,100"
                ];
        }


        /* ------------------------------------------------------------------------------------------*
         * Handle method calls for CodeBox
         * 
         * Commands that need keyboard coded support
         *      Comment, Uncomment, Save, Save As, Open, Close
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower().Trim();

            //AppIO.DebugLog($"CodeBox Method Called: {methodName}");

            string filename = "";

            switch (methodName)
            {
                case "textcommand":
                    if (Program.CurrentApp.ParameterClassList.Count == 1)
                    {
                        if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        {
                            string cmd = Program.CurrentApp.ParameterClassList[0].token.AsString().ToLower();

                            switch (cmd)
                            {
                                case "co":
                                    if (Program.CurrentApp.ParameterClassList.Count == 1 && Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                                        filename = Program.CurrentApp.ParameterClassList[0].token.AsString();

                                    await OpenFileAsync(filename);
                                    break;

                                case "cn":
                                    await NewFileAsync();
                                    break;

                                case "cs":
                                    if (Program.CurrentApp.ParameterClassList.Count == 1 && Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                                        filename = Program.CurrentApp.ParameterClassList[0].token.AsString();

                                    result = await SaveAsFileAsync(filename);
                                    break;

                                case "sa":
                                    _currentFilePath = "";   // Clear current file path to force SaveAs behavior
                                    if (Program.CurrentApp.ParameterClassList.Count == 1 && Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                                        filename = Program.CurrentApp.ParameterClassList[0].token.AsString();

                                    result = await SaveAsFileAsync(filename);
                                    break;

                                case "cq":
                                    break;

                                case "cf":
                                    CodeBoxFind();
                                    break;

                                case "ch":
                                    CodeBoxFindReplace();
                                    break;


                                case "cc":
                                    SimulateKeyPress(Avalonia.Input.Key.C, Avalonia.Input.KeyModifiers.Control);
                                    break;

                                case "cx":
                                    SimulateKeyPress(Avalonia.Input.Key.X, Avalonia.Input.KeyModifiers.Control);
                                    break;

                                case "cv":
                                    SimulateKeyPress(Avalonia.Input.Key.V, Avalonia.Input.KeyModifiers.Control);
                                    break;

                                case "cz":
                                    SimulateKeyPress(Avalonia.Input.Key.Z, Avalonia.Input.KeyModifiers.Control);
                                    break;

                                case "cr":
                                    SimulateKeyPress(Avalonia.Input.Key.R, Avalonia.Input.KeyModifiers.Control);
                                    break;

                                case "tb":
                                    SimulateKeyPress(Avalonia.Input.Key.Tab, Avalonia.Input.KeyModifiers.None);
                                    break;

                                case "st":
                                    SimulateKeyPress(Avalonia.Input.Key.Tab, Avalonia.Input.KeyModifiers.Shift);
                                    break;

                                case "ac":
                                    Comment();
                                    break;

                                case "au":
                                    Uncomment();
                                    break;
                            }
                        }
                    }
                    break;

                case "setfocus":
                    CodeBx.TextArea.Focus();    // TODO - Why doesn't this work?
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }



        /* ------------------------------------------------------------------------------------------*
         * Method call logic for CodeBox
         *------------------------------------------------------------------------------------------*/
        private void SimulateKeyPress(Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers)
        {
            var args = new Avalonia.Input.KeyEventArgs
            {
                Key = key,
                KeyModifiers = modifiers,
                Source = CodeBx.TextArea,
                RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent
            };

            // Focus the TextArea first to ensure it receives the input
            CodeBx.TextArea.Focus();

            // Raise the KeyDown event on the TextArea (where AvaloniaEdit listens)
            CodeBx.TextArea.RaiseEvent(args);

            // Also raise on the editor itself as fallback
            if (!args.Handled)
            {
                AppIO.DebugLog("Key event not handled by TextArea, raising on CodeBox");
                CodeBx.RaiseEvent(args);
            }
        }

        private void CodeBoxFind() { ShowFindDialog(); }
        private void CodeBoxFindReplace() { ShowReplaceDialog(); }

        private void ShowFindDialog()
        {
            this._searchPanel.IsReplaceMode = false;
            this._searchPanel.Open();
            this._searchPanel.Focus();
        }

        private void ShowReplaceDialog()
        {
            this._searchPanel.IsReplaceMode = true;
            this._searchPanel.Open();
            this._searchPanel.Focus();
        }


        // Comment the selected lines by prepending "*!*" at the very beginning
        // of each line.  If no selection, then comment the current line.
        private void Comment()
        {
            var document = CodeBx.Document;
            var textArea = CodeBx.TextArea;
            var selection = textArea.Selection;

            int startLine;
            int endLine;

            if (selection.Length > 0)
            {
                startLine = selection.StartPosition.Line;
                endLine = selection.EndPosition.Line;

                // If the text was selected from bottom to top
                // then we need to reverse the start/end lines
                if (startLine > endLine)
                {
                    int holdLine = startLine;
                    startLine = endLine;
                    endLine = holdLine;
                }
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

            this._isDirty = true;
            //this.UpdateWindowTitle();
            //this.UpdateStatusBar();
        }


        // Uncomment the selected lines by removing "*!*" if it
        // exists at the very beginning of each line.
        private void Uncomment()
        {
            var document = CodeBx.Document;
            var textArea = CodeBx.TextArea;
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

            this._isDirty = true;
            //this.UpdateWindowTitle();
            //this.UpdateStatusBar();
        }

        private async Task NewFileAsync()
        {
            if (this._isDirty)
            {
                bool canProceed = await this.ShowSaveChangesPromptAsync("Do you want to save changes before creating a new file?");
                if (!canProceed) return;
            }

            CodeBx.Text = "";
            this._currentFilePath = "unamed.prg";
            this._isDirty = false;
            //this.UpdateWindowTitle();
        }

        private async Task OpenFileAsync(string fileName)
        {
            int result = 0;
            string msg = "";

            // TODO - Tie ths in?
            //if (me.ParentAvaloniaWindow is not null)
            //{
            //    var topLevel = Avalonia.Controls.TopLevel.GetTopLevel((Avalonia.Visual)me.ParentAvaloniaWindow);

            //    if (topLevel == null)
            //    {
            //        // Fallback (should not happen)
            //        topLevel = Avalonia.Controls.TopLevel.GetTopLevel(JAXApp.MainWindowInstance);
            //    }

            //    var storageProvider = topLevel!.StorageProvider;
            //}

            var topLevel = TopLevel.GetTopLevel(CodeBx);

            var storageProvider = topLevel?.StorageProvider;

            // Did storageprovider come back as null?
            if (storageProvider is null)
                result = 9960;
            else
            {
                if (_isDirty)
                {
                    bool canProceed = await this.ShowSaveChangesPromptAsync("Do you want to save changes before opening a new file?");
                    if (!canProceed) return;
                }

                if (File.Exists(fileName))
                {
                    // Open the requested file directly without showing the file picker
                    CodeBx.Text = await File.ReadAllTextAsync(fileName);
                }
                else
                {
                    // Open the file picker to select a file to open
                    var options = new FilePickerOpenOptions
                    {
                        Title = "Open File",
                        FileTypeFilter =
                        [
                            new FilePickerFileType("Text Files") { Patterns = [ "*.txt", "*.prg" ] },
                            new FilePickerFileType("All Files") { Patterns = [ "*" ] }
                        ]
                    };

                    var files = await storageProvider.OpenFilePickerAsync(options);
                    if (files.Count == 0)
                        result = 6700;
                    else
                    {
                        try
                        {
                            using var stream = await files[0].OpenReadAsync();
                            using var reader = new StreamReader(stream);
                            CodeBx.Text = await reader.ReadToEndAsync();

                            this._currentFilePath = files[0].Path.LocalPath;
                            this._isDirty = false;
                            //this.UpdateWindowTitle();
                        }
                        catch (ArgumentException ex) { result = 202; msg = ex.Message; }
                        catch (PathTooLongException ex) { result = 202; msg = ex.Message; }
                        catch (NotSupportedException ex) { result = 202; msg = ex.Message; }
                        catch (UnauthorizedAccessException ex) { result = 2021; msg = ex.Message; }
                        catch (IOException ex) { result = 334; msg = ex.Message; }
                        catch (Exception ex) { result = 2020; msg = ex.Message; }
                    }
                }
            }

            // Was an error reported?
            if (result > 0 && result != 6700)
                _AddError(result, 0, _currentFilePath + "|" + msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
        }


        /*
         * Save the file.  If no filename, then call SaveAs
         */
        private async Task SaveFileAsync()
        {
            int result = 0;
            string msg = "";

            if (string.IsNullOrEmpty(this._currentFilePath))
            {
                await this.SaveAsFileAsync(_currentFilePath);
                return;
            }

            try
            {
                await File.WriteAllTextAsync(this._currentFilePath, CodeBx.Text);
                this._isDirty = false;
                //this.UpdateWindowTitle();
            }
            catch (ArgumentException ex) { result = 202; msg = ex.Message; }
            catch (PathTooLongException ex) { result = 202; msg = ex.Message; }
            catch (NotSupportedException ex) { result = 202; msg = ex.Message; }
            catch (UnauthorizedAccessException ex) { result = 2021; msg = ex.Message; }
            catch (IOException ex) { result = 334; msg = ex.Message; }
            catch (Exception ex) { result = 2020; msg = ex.Message; }

            // Was an error reported?
            if (result > 0 && result != 6700)
                _AddError(result, 0, _currentFilePath + "|" + msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
        }


        /*
         * Save the file.  If filename is provided, use that name as default 
         */
        private async Task<int> SaveAsFileAsync(string filename)
        {
            int result = 0;

            if (string.IsNullOrWhiteSpace(filename) == false)
            {
                // Filename provided, save directly without showing the file picker
                // If the provided filename is just a name without path, prepend the default path
                if (string.IsNullOrWhiteSpace(JAXLib.JustPath(filename)))
                    filename = Program.CurrentApp.CurrentDS.JaxSettings.Default + filename;

                // If the provided filename does not have an extension, add .prg by default
                if (string.IsNullOrWhiteSpace(JAXLib.JustExt(filename)) && filename[^1] != '.')
                    filename += ".prg";

                // Get the absolute path for the filename in case
                // we were provided a relative path
                filename = Path.GetFullPath(filename);
            }

            string msg = "";

            // Show the file picker to select where to save
            var topLevel = TopLevel.GetTopLevel(CodeBx);
            var storageProvider = topLevel?.StorageProvider;
            if (storageProvider == null)
                result = 9960;
            else
            {
                var options = new FilePickerSaveOptions
                {
                    Title = "Save File As",
                    SuggestedFileName = string.IsNullOrEmpty(filename) ? "Untitled.prg" : Path.GetFileName(filename),
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Text Files") { Patterns =[ "*.txt", "*.prg", "*.jax" ] },
                        new FilePickerFileType("All Files") { Patterns = [ "*" ] }
                    ]
                };

                var file = await storageProvider.SaveFilePickerAsync(options);
                if (file == null)
                    result = 6700;
                else
                {
                    filename = file.Path.LocalPath;

                    try
                    {
                        // Save the file
                        await File.WriteAllTextAsync(filename, CodeBx.Text);
                        this._currentFilePath = filename;
                        this._isDirty = false;
                        //this.UpdateWindowTitle();
                    }
                    catch (ArgumentException ex) { result = 202; msg = ex.Message; }
                    catch (PathTooLongException ex) { result = 202; msg = ex.Message; }
                    catch (NotSupportedException ex) { result = 202; msg = ex.Message; }
                    catch (UnauthorizedAccessException ex) { result = 2021; msg = ex.Message; }
                    catch (IOException ex) { result = 334; msg = ex.Message; }
                    catch (Exception ex) { result = 2020; msg = ex.Message; }
                }
            }

            // Was an error reported?
            if (result > 0 && result != 6700)
                _AddError(result, 0, filename + "|" + msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

            return result;
        }

        // TODO - Can we tie this into the destroy method?
        private async Task ExitAsync()
        {
            if (this._isDirty)
            {
                bool canClose = await this.ShowSaveChangesPromptAsync("Do you want to save changes before exiting?");
                if (!canClose) return;
            }
        }


        // Dialog for saving changes
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
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Auto });
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Star });
            dialogGrid.RowDefinitions.Add(new Avalonia.Controls.RowDefinition { Height = Avalonia.Controls.GridLength.Auto });

            Avalonia.Controls.Grid.SetRow(textBlock, 0);
            Avalonia.Controls.Grid.SetRow(buttonPanel, 2);

            dialogGrid.Children.Add(textBlock);
            dialogGrid.Children.Add(buttonPanel);

            dialog.Content = dialogGrid;

            var tcs = new TaskCompletionSource<bool>();

            yesButton.Click += async (s, e) => { await this.SaveFileAsync(); tcs.SetResult(true); dialog.Close(); };
            noButton.Click += (s, e) => { tcs.SetResult(true); dialog.Close(); };
            cancelButton.Click += (s, e) => { tcs.SetResult(false); dialog.Close(); };

            await dialog.ShowDialog(JAXApp.MainWindowInstance!);
            return await tcs.Task;
        }
    }
}