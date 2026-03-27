/* --------------------------------------------------------------------------------------------------*
 * Derived from Simple-Notepad: https://github.com/Lavertis/simple-notepad
 * MIT License
 *
 * Copyright (c) 2017 Lavertis
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.MIT License
 * 
 * --------------------------------------------------------------------------------------------------
 * Modified by Jon Lee Walker for the JAXBase project 9/18/2025
 * 
 * On exit request, the form is hidden and must be closed by the calling routine.
 * 
 * TODO - 
 * TODO - TAB should move text block right and SHIFT+TAB should move text block left
 * TODO - 
 * TODO - 
 * 
 * Look into Avalonia for cross-platform winforms
 * <!-- You keep your .csproj as WinForms, but add Avalonia -->
 * <UseAvaloniaWinForms>true</UseAvaloniaWinForms>
 * 
 * 2025-12-10 - JLW
 *      Worked with Grok to get the TAB/SHIFT+Tab and UNDO/REDO changes put in.  Grok is
 *      really great for simple stuff, but the more complex the problem, the more work
 *      you need to put in.  And you better check every bit of the code before asking
 *      the next question or closing out the session.  It took a good 45 minutes to
 *      get a working TAB control and another 30 for UNDO/REDO.
 *      
 *      If I was doing this  in VFP, I would never have asked for help, but, as I've said, I'm
 *      not a C# developer and some things I'd 
 *      
 *      Futher TODO:
 *      - Paste does not inclue line termination characters and adds spaces to the end
 *      - When moving up or down through the code, the column position is not remembered
 *
 *      - When editing a comment block, it would be great to automatically add another
 *      - asterisk when I hit ENTER.
 *       
 *      - Auto-indent would be FAN-FRACKIN'-TASTIC!
 *      
 *      - Also, for current doc and tie-in for editor methods
 *          Add - Find?  Find next?
 *          Add - Find/Replace?
 *
 *      - Add tab space control option
 *      - CTRL+K D to fix all indentation would be AWESOME!
 *
 *      Version 2 wishlist
 *      + On the fly syntax checking
 *      + Color coded syntax like VFP editor
 *      + Auto-compile during save (but not save-as)
 *      + Grok assisted coding
 *       
 * --------------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.XBase;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace JAXBase
{
    public partial class PrgEdit : Form
    {
        readonly AppClass App;
        string path = string.Empty;
        readonly string startCommand = string.Empty;
        string MD5Start = string.Empty;
        readonly int TabSize = 4;
        bool _justPastedCleanText = false;

        JAXEditorResult ReturnResult = new();

        /* ------------------------------------------------------------------------------------------*
         * Load the form with
         *      startCommanmd
         *          F=File, T=Text (not a file)
         *          
         *      fileName
         *          Name of file or method to display in caption
         *          
         *      startValue
         *          Text to place in the edit region
         *          
         * Save the fileName, place the text into the editing region, get a checksum of the text
         * for detecting changes at exit, clear the ReturnCommand and ReturnValue and then
         * set the form as Visible.
         *          
         * -----------------------------------------------------------------------------------------*/
        public PrgEdit(AppClass app, string startCommand, string fileName, string startValue)
        {
            App = app;
            InitializeComponent();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                this.Text = "Untitled";
                path = string.Empty;
            }
            else
            {
                this.Text = fileName;
                path = fileName;
            }

            ReturnResult.Name = fileName;
            ReturnResult.Type = startCommand;

            // Enable drop support (required for drag-drop, but not needed for Ctrl+V paste)
            textBox1.AllowDrop = true;  // Optional — only if you want file/text drag-drop too

            // Wire up your existing Tab handler
            //textBox1.KeyDown += textBox1_KeyDown;

            // For other paste methods (middle-click, menu paste, etc.)
            textBox1.TextChanged += textBox1_TextChanged;
            textBox1.Clear();
            textBox1.ClearUndo();

            this.KeyPreview = true;
            textBox1.KeyDown += textBox1_KeyDown;

            this.textBox1.Text = startValue;
            this.textBox1.Select(0, 0);
            this.MD5Start = App.utl.GetFileCheckSum_MD5(startValue);
            this.startCommand = startCommand;

            switch (startCommand)
            {
                case "F":
                    break;

                default:
                    openToolStripMenuItem.Enabled = false;
                    newToolStripMenuItem.Enabled = false;
                    break;
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * Open a file  
         * -----------------------------------------------------------------------------------------*/
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Programs (*.prg)|*.prg|Class Defininitions (*.def)|*.def|Include (*.h)|*.h|All Files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = File.ReadAllText(path = openFileDialog1.FileName);
                MD5Start = App.utl.GetFileCheckSum_MD5(textBox1.Text);

                ReturnResult = new()
                {
                    Name = openFileDialog1.FileName,
                    Type = "F"
                };
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * SaveAs dialog allows you to save to a new file name.  
         * If the editor is pointing at a file, the new file becomes the edited object.
         * 
         * If the editor is pointing at a text string, the file is saved but the string is
         * still being edited and will be returned if CTRL+W or save is selected.
         * -----------------------------------------------------------------------------------------*/
        private void saveAsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // Saving to a file from a file resets what we're working on
                if (this.startCommand.Equals("F"))
                {
                    this.MD5Start = App.utl.GetFileCheckSum_MD5(textBox1.Text);
                    this.Name = saveFileDialog1.FileName;
                }
                else
                {
                    // Save text object as file, but still editing text object
                    File.WriteAllText(path = saveFileDialog1.FileName, textBox1.Text);
                }
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Save and exit the form  
         * -----------------------------------------------------------------------------------------*/
        private void saveToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (startCommand.Equals("F"))
            {
                string currentMD5 = App.utl.GetFileCheckSum_MD5(textBox1.Text);
                if (string.IsNullOrEmpty(MD5Start) || MD5Start.Equals(currentMD5) == false) // Did anything change?
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        File.WriteAllText(path, textBox1.Text);
                        ReturnResult.Name = path;
                    }
                    else
                        saveAsToolStripMenuItem_Click(sender, e);
                }
            }

            ReturnResult.Text = textBox1.Text;
            ReturnResult.Command = "S"; // Save
            MD5Start = App.utl.GetFileCheckSum_MD5(textBox1.Text);
            this.Close();
        }

        /* ------------------------------------------------------------------------------------------*
         * Quit without exit
         * 
         * If changes were made, the exit prompt will ask if you want to save
         * your changes, otherwise it will just exit.
         * -----------------------------------------------------------------------------------------*/
        private void exitPrompt()
        {
            if (MD5Start.Equals(App.utl.GetFileCheckSum_MD5(this.textBox1.Text)) == false)
            {
                DialogResult = MessageBox.Show("Do you want to save current file?",
                    "Notepad",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
            }
        }

        /* ------------------------------------------------------------------------------------------*
         *  New file
         * -----------------------------------------------------------------------------------------*/
        private void newToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBox1.Text))
            {
                if (MD5Start.Equals(App.utl.GetFileCheckSum_MD5(this.textBox1.Text)) == false)
                {
                    exitPrompt();

                    if (DialogResult == DialogResult.Yes)
                    {
                        saveToolStripMenuItem_Click(sender, e);
                        textBox1.Text = String.Empty;
                        path = String.Empty; ;
                    }
                    else if (DialogResult == DialogResult.No)
                    {
                        textBox1.Text = String.Empty;
                        path = String.Empty;
                    }
                }
                else
                {
                    // Nothing changed so clear it out and start new
                    textBox1.Text = String.Empty;
                    path = String.Empty;
                    string t = ReturnResult.Type;
                    MD5Start = string.Empty;

                    ReturnResult = new()
                    {
                        Type = t
                    };
                }
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Various hooks for menu items   
         * -----------------------------------------------------------------------------------------*/
        private void selectAllToolStripMenuItem_Click(object? sender, EventArgs e) => textBox1.SelectAll();

        private void cutToolStripMenuItem_Click(object? sender, EventArgs e) => textBox1.Cut();

        private void copyToolStripMenuItem_Click(object? sender, EventArgs e) => textBox1.Copy();

        private void pasteToolStripMenuItem_Click(object? sender, EventArgs e) => textBox1.Paste();

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) => textBox1.SelectedText = String.Empty;

        private void redoToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (textBox1 is HistoryTextBox ht)
                ht.Redo();
        }

        private void UndoToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (textBox1 is HistoryTextBox ht)
                ht.Undo();
            else if (textBox1.CanUndo)
                textBox1.Undo();
        }

        /* ------------------------------------------------------------------------------------------*
         * Word Wrap
         * -----------------------------------------------------------------------------------------*/
        private void wordWrapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wordWrapToolStripMenuItem.Checked == true)
            {
                textBox1.WordWrap = false;
                textBox1.ScrollBars = ScrollBars.Both;
                wordWrapToolStripMenuItem.Checked = false;
            }
            else
            {
                textBox1.WordWrap = true;
                textBox1.ScrollBars = ScrollBars.Vertical;
                wordWrapToolStripMenuItem.Checked = true;
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Font
         * -----------------------------------------------------------------------------------------*/
        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Font = textBox1.Font = new System.Drawing.Font(fontDialog1.Font, fontDialog1.Font.Style);
                textBox1.ForeColor = fontDialog1.Color;
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * User clicked the close icon
         * -----------------------------------------------------------------------------------------*/
        private void exitToolStripMenuItem_Click(object? sender, EventArgs e) => this.Close();

        /* ------------------------------------------------------------------------------------------*
         *  Make sure it's ok for the form to close
         * -----------------------------------------------------------------------------------------*/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MD5Start) == false && MD5Start.Equals(App.utl.GetFileCheckSum_MD5(this.textBox1.Text)) == false)
            {
                exitPrompt();

                if (DialogResult == DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
                else if (DialogResult == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        /* ------------------------------------------------------------------------------------------*
         * Look for CTRL+A, CTRL+N, CTRL+S, and TAB
         * -----------------------------------------------------------------------------------------*/
        private void textBox1_KeyDown(object? sender, KeyEventArgs e)
        {

            // Ctrl+Z = Undo
            if (e.Control && e.KeyCode == Keys.Z)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                UndoToolStripMenuItem_Click(sender, e);
                //if (textBox1 is HistoryTextBox ht)
                //    ht.Undo();
                //else if (textBox1.CanUndo)
                //    textBox1.Undo();
                //return;
            }

            if (e.Control && e.KeyCode == Keys.Y)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                redoToolStripMenuItem_Click(sender, e);
                //if (textBox1 is HistoryTextBox ht)
                //    ht.Redo();
                //return;
            }

            if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                BlockIndent(!e.Shift);   // true = indent, false = unindent
            }

            // Intercept pasted text
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                PasteCleanText();
                return;
            }

            // Your existing Ctrl+A, Ctrl+N etc. stay exactly the same
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.A: e.SuppressKeyPress = true; textBox1.SelectAll(); break;
                    case Keys.N: e.SuppressKeyPress = true; newToolStripMenuItem_Click(sender, e); break;
                    case Keys.Q: e.SuppressKeyPress = true; exitToolStripMenuItem_Click(sender, e); break;
                    case Keys.W: e.SuppressKeyPress = true; saveToolStripMenuItem_Click(sender, e); break;
                }
            }

            UpdateUndoRedoMenu();
        }

        private void textBox1_TextChanged(object? sender, EventArgs e)
        {

            // This catches middle-click paste, menu paste, Shift+Insert, etc.
            if (!_justPastedCleanText) return;

            _justPastedCleanText = false; // reset for next time

            UpdateUndoRedoMenu();
        }

        private void PasteCleanText()
        {
            if (!Clipboard.ContainsText()) return;

            string raw = Clipboard.GetText();

            string clean = CleanPastedText(raw);

            _justPastedCleanText = true; // tell TextChanged not to interfere

            // Insert the clean text at cursor/selection
            textBox1.SelectedText = clean;

            // Optional: auto-indent the newly pasted block like VS Code does
            AutoIndentPastedBlock();
        }

        private string CleanPastedText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Remove common garbage
            var cleanedLines = lines
                .Select(line => line
                    .Replace("\u00A0", " ")      // non-breaking space
                    .Replace("\u200B", "")      // zero-width space
                    .Replace("\u201C", "\"")
                    .Replace("\u201D", "\"")
                    .Replace("\u2018", "'")
                    .Replace("\u2019", "'"))
                .ToArray();

            // Remove completely empty lines at start/end (optional)
            cleanedLines = cleanedLines
                .SkipWhile(string.IsNullOrWhiteSpace)
                .Reverse()
                .SkipWhile(string.IsNullOrWhiteSpace)
                .Reverse()
                .ToArray();

            return string.Join("\r\n", cleanedLines);
        }

        // Optional cherry on top: auto-indent the whole pasted block to match current line
        private void AutoIndentPastedBlock()
        {
            int pasteStart = textBox1.SelectionStart;
            int pasteEnd = pasteStart + textBox1.SelectionLength;

            int startLine = textBox1.GetLineFromCharIndex(pasteStart);
            int endLine = textBox1.GetLineFromCharIndex(pasteEnd);

            // Indent the pasted block to match the current line's indent
            int currentLine = textBox1.GetLineFromCharIndex(pasteStart);
            string currentIndent = GetLineIndent(textBox1, currentLine);

            if (currentLine == startLine)
            {
                // Re-indent the block
                BlockIndent(true); // or false if you prefer not
            }
        }

        private string GetLineIndent(TextBox tb, int line)
        {
            int lineStart = tb.GetFirstCharIndexFromLine(line);
            int pos = lineStart;
            while (pos < tb.TextLength && pos < tb.TextLength && char.IsWhiteSpace(tb.Text[pos])) pos++;
            return tb.Text.Substring(lineStart, pos - lineStart);
        }


        private void BlockIndent(bool indent)
        {
            int start = textBox1.SelectionStart;
            int length = textBox1.SelectionLength;

            // If nothing selected → regular tab behavior
            if (length == 0)
            {
                int col = start - textBox1.GetFirstCharIndexOfCurrentLine();
                int add = TabSize - (col % TabSize);
                textBox1.SelectedText = new string(' ', add == 0 ? TabSize : add);
                return;
            }

            int firstLine = textBox1.GetLineFromCharIndex(start);
            int lastLine = textBox1.GetLineFromCharIndex(start + length);

            // Don't include the line where selection ends if it's at column 0
            if (start + length == textBox1.GetFirstCharIndexFromLine(lastLine))
                lastLine--;

            string text = textBox1.Text;
            var sb = new StringBuilder(text.Length + (lastLine - firstLine + 1) * TabSize);

            int lineIndex = 0;
            int pos = 0;

            while (pos < text.Length)
            {
                int lineStart = pos;
                while (pos < text.Length && text[pos] != '\r' && text[pos] != '\n')
                    pos++;

                string lineContent = text.Substring(lineStart, pos - lineStart);

                string lineEnding = "";
                if (pos < text.Length)
                {
                    if (text[pos] == '\n' && pos + 1 < text.Length && text[pos + 1] == '\r')
                    {
                        lineEnding = "\r\n";
                        pos += 2;
                    }
                    else if (text[pos] == '\r' && pos + 1 < text.Length && text[pos + 1] == '\n')
                    {
                        lineEnding = "\r\n";
                        pos += 2;
                    }
                    else
                    {
                        lineEnding = text[pos].ToString();
                        pos++;
                    }
                }

                if (lineIndex >= firstLine && lineIndex <= lastLine)
                {
                    if (indent)
                        sb.Append(' ', TabSize);
                    else
                    {
                        int remove = 0;
                        while (remove < lineContent.Length && remove < TabSize && lineContent[remove] == ' ')
                            remove++;
                        lineContent = lineContent.Substring(remove);
                    }
                }

                sb.Append(lineContent);
                sb.Append(lineEnding);
                lineIndex++;
            }

            // One single assignment — no side effects
            textBox1.Text = sb.ToString();

            // Restore selection
            int delta = indent ? TabSize : -TabSize;
            int affectedLines = lastLine - firstLine + 1;

            int newStart = start + delta;                     // first line always gets shifted

            // --------------------------------------------------------------------------------------
            // I came up with this line of code because Grok's was wrong.
            int newLength = length + (affectedLines - 1) * (delta - 1) + affectedLines - 1;
            // --------------------------------------------------------------------------------------

            if (newLength < 0) newLength = 0;

            textBox1.Select(newStart, newLength);
            textBox1.ScrollToCaret();
        }



        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void MoveCursorToLineAndColumn(int lineNumber, int columnNumber, bool zeroBased = true)
        {
            if (!textBox1.Multiline)
                return;

            // Convert to 0-based if user gave 1-based numbers
            int line = zeroBased ? lineNumber : lineNumber - 1;
            int col = zeroBased ? columnNumber : columnNumber - 1;

            if (line < 0 || col < 0)
                return;

            // Get the character index of the first character of the desired line
            int firstCharOfLine = textBox1.GetFirstCharIndexFromLine(line);

            // Safety check: if line doesn't exist yet (e.g. only 20 lines but you ask for line 50)
            if (firstCharOfLine == -1)
            {
                // Go to end of text instead
                textBox1.SelectionStart = textBox1.TextLength;
                textBox1.ScrollToCaret();
                return;
            }

            // Final position = start of line + column offset
            int targetIndex = firstCharOfLine + col;

            // Don't go past the actual text length
            if (targetIndex > textBox1.TextLength)
                targetIndex = textBox1.TextLength;

            // Move cursor and scroll to it
            textBox1.SelectionStart = targetIndex;
            textBox1.SelectionLength = 0;     // removes any selection
            textBox1.ScrollToCaret();         // makes sure the cursor is visible
            textBox1.Focus();                 // optional: give focus
        }


        public class HistoryTextBox : TextBox
        {
            private readonly List<string> _undoStack = new List<string>();
            private readonly List<string> _redoStack = new List<string>();
            private string _currentText = string.Empty;
            private bool _internalChange = false;
            private bool _isUndoingOrRedoing = false;


            // Add these two tiny methods to your HistoryTextBox class
            public new bool CanUndo => _undoStack.Count > 0;
            public bool CanRedo => _redoStack.Count > 0;

            public string GetUndoActionName() => "Typing"; // you can make this smarter later
            public string GetRedoActionName() => "Typing";

            public HistoryTextBox()
            {
                // Save initial empty state
                SaveState();
            }

            private void SaveState()
            {
                // Only save if text actually changed
                if (this.Text != _currentText)
                {
                    _undoStack.Add(_currentText);
                    _currentText = this.Text;

                    // Clear redo when new action occurs
                    if (!_isUndoingOrRedoing)
                        _redoStack.Clear();

                    // Limit undo stack size
                    if (_undoStack.Count > 1000)
                        _undoStack.RemoveAt(0);
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                // Ctrl+Z → Undo
                if (e.Control && e.KeyCode == Keys.Z && !e.Alt)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Undo();
                    return;
                }

                // Ctrl+Y → Redo
                if (e.Control && e.KeyCode == Keys.Y && !e.Alt)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Redo();
                    return;
                }

                // For typing: save state BEFORE the character appears
                if (!e.Control && !e.Alt &&
                    (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete ||
                     (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                     (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) ||
                     e.KeyCode == Keys.Space || e.KeyCode == Keys.OemPeriod))
                {
                    SaveState();  // ← saves the text BEFORE this key changes it
                }

                base.OnKeyDown(e);
            }

            protected override void OnTextChanged(EventArgs e)
            {
                if (_internalChange)
                {
                    base.OnTextChanged(e);
                    return;
                }

                // Handle paste, delete-all, drag-drop, block indent, etc.
                SaveState();

                base.OnTextChanged(e);
            }

            public new void Undo()
            {
                if (_undoStack.Count == 0) return;

                _isUndoingOrRedoing = true;

                _redoStack.Add(_currentText);
                _currentText = _undoStack[_undoStack.Count - 1];
                _undoStack.RemoveAt(_undoStack.Count - 1);

                _internalChange = true;
                this.Text = _currentText;
                this.SelectionStart = this.Text.Length;
                _internalChange = false;

                _isUndoingOrRedoing = false;
            }

            public void Redo()
            {
                if (_redoStack.Count == 0) return;

                _isUndoingOrRedoing = true;

                _undoStack.Add(_currentText);
                _currentText = _redoStack[_redoStack.Count - 1];
                _redoStack.RemoveAt(_redoStack.Count - 1);

                _internalChange = true;
                this.Text = _currentText;
                this.SelectionStart = this.Text.Length;
                _internalChange = false;

                _isUndoingOrRedoing = false;
            }
        }

        //private void textBox1_TextChanged(object sender, EventArgs e) => UpdateUndoRedoMenu();
        //private void textBox1_KeyDown(object sender, KeyEventArgs e) => UpdateUndoRedoMenu();

        private void UpdateUndoRedoMenu()
        {
            if (textBox1 is HistoryTextBox ht)
            {
                undoToolStripMenuItem.Enabled = ht.CanUndo;
                redoToolStripMenuItem.Enabled = ht.CanRedo;

                undoToolStripMenuItem.Text = ht.CanUndo ? $"&Undo {ht.GetUndoActionName()}" : "&Undo";
                redoToolStripMenuItem.Text = ht.CanRedo ? $"&Redo {ht.GetRedoActionName()}" : "&Redo";
            }
        }

    }
}
