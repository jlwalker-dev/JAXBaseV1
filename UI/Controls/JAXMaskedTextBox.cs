/*
 * 2026-03-30 - JLW
 *      Worked with Grok to come up with a basic routine to handle masked input.
 *      I will modify it as I support further mask and format options.
 *      
 */
using Avalonia.Input;
using JAXBase.Core;
using JAXBase.Utilities;
using System.Globalization;
using System.Text;

namespace JAXBase.UI.Controls
{
    /// <summary>
    /// JAXBase wrapper around Avalonia MaskedTextBox with hybrid mask support.
    /// Supports real-time uppercase enforcement for '!' and '^' via TextInput 
    /// and also applies formatting when .Text is set programmatically.
    /// </summary>
    public class JAXMaskedTextBox : Avalonia.Controls.MaskedTextBox
    {
        private MaskConversionResult? _maskResult;
        private string _jaxBaseMask = "";
        private string _jaxBaseFormat = "";

        private bool _textIsNull = false;
        private bool _isDisplayingNull = false;
        private bool _maskSkip = false;         // Don't skip by default
        private char _maskFillChar = '\0';      // Don't fill if \0
        private string NullDisplayText = "";

        /// <summary>
        /// Gets or sets whether to display ".NULL." when the underlying value is null or empty.
        /// Default is true for VFP-like behavior.
        /// </summary>
        //public bool ShowNullAsText { get; set; } = true;


        public string ForcedDataType { get; private set; } = "";     // C, D, T, N, L
        public bool SelectOnFocus { get; private set; } = false;
        public bool RemoveMaskCharacters { get; private set; } = false;
        public bool Format_UpperCase { get; private set; } = false;
        public bool Format_Trim { get; private set; } = false;
        public bool Format_BlankZero { get; private set; } = false;
        public bool TextIsNull => _textIsNull;


        // An extra masking ability that VFP does not have.
        // If true, then if slash, minus, or period keys are
        // pressed, it attempts to move the caret to the right
        // of the next matching mask character.
        public bool MaskCharSkip
        {
            get => _maskSkip;
            set
            {
                _maskSkip = value;
            }
        }

        // If the MaskCharSkip is set to true then the
        // current characters in the mask area are right
        // justified and this charcter is used to fill in
        // the blank spaces on the left.
        // For example, MaskFillChar="0" in the situation
        // were the ^ is the current caret poistion and the
        // user hits the period key:
        //      123.2^_.___ becomes 123.002.^__
        public char MaskFillChar
        {
            get => _maskFillChar;
            set
            {
                _maskFillChar = value;
            }
        }

        /// <summary>
        /// Gets or sets the JAXBase-style mask string.
        /// </summary>
        public string JAXMask
        {
            get => _jaxBaseMask;
            set
            {
                _jaxBaseMask = value ?? "";
                ApplyJAXMask();
            }
        }

        public string JAXFormat
        {
            get => _jaxBaseFormat;
            set
            {
                _jaxBaseFormat = value ?? "";
                ApplyJAXFormat();
            }
        }

        public new char PromptChar
        {
            get => base.PromptChar;
            set => base.PromptChar = value;
        }

        public string NullDisplay
        {
            get => NullDisplayText;
            set => NullDisplayText = (value ?? "").ToString();
        }


        public JAXMaskedTextBox()
        {
            PromptChar = ' ';   // VFP compatible display

            // Wire up events
            KeyDown += OnKeyDown;
            TextChanged += OnTextChanged;
        }


        private void ApplyJAXMask()
        {
            if (string.IsNullOrEmpty(_jaxBaseMask))
            {
                Mask = string.Empty;
                _maskResult = null;
                return;
            }

            _maskResult = MaskHelper.ConvertToAvaloniaMask(_jaxBaseMask);
            Mask = _maskResult.AvaloniaMask;

            // Re-apply formatting to current text if any
            if (!string.IsNullOrEmpty(Text))
            {
                ApplyMaskFormattingToCurrentText();
            }
        }

        /// <summary>
        /// Intercepts keydown and decides if a mask skip is required
        /// Returns -1 if no more matches are found.
        /// </summary>
        private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (_maskSkip)
            {
                if (e.Key == Key.OemPeriod || e.Key == Key.Decimal || e.Key == Key.OemMinus)
                {
                    char mc = e.Key switch
                    {
                        Key.OemMinus => '-',
                        Key.OemPeriod => '.',
                        Key.Decimal => '.',
                        Key.Oem2 => '/',
                        _ => '.'
                    };

                    if (!string.IsNullOrEmpty(Mask) && Mask.Contains(mc))
                    {
                        int newCaret = FindNextPosition(CaretIndex, mc);
                        if (newCaret >= 0)
                        {
                            TextChanged -= OnTextChanged;
                            Text = FormatWithPadding(Text ?? "", mc, newCaret);
                            TextChanged += OnTextChanged;

                            // Simple delay - works in most Avalonia scenarios
                            System.Threading.Tasks.Task.Delay(1).ContinueWith(_ =>
                            {
                                CaretIndex = newCaret;
                            }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Finds the position right after the next specified mask character, starting from current caret.
        /// Returns -1 if no more matches are found.
        /// </summary>
        private int FindNextPosition(int currentCaret, char mchar)
        {
            if (string.IsNullOrEmpty(Mask))
                return -1;

            for (int i = currentCaret; i < Mask.Length; i++)
            {
                if (Mask[i] == mchar)
                    return i + 1;   // position right after the period
            }

            return -1; // no more matching mask characters
        }

        public string FormatWithPadding(string inputString, char maskChar, int caretPos)
        {
            string result = "";

            if (string.IsNullOrEmpty(inputString) || _maskFillChar=='\0')
                return inputString;

            // Split into sections by period
            string[] sections = inputString.Split(maskChar);

            int curPos = 0;
            for (int i = 0; i < sections.Length; i++)
            {
                curPos += sections[i].Length + 1;
                if (curPos <= caretPos)
                {
                    int l = sections[i].Length;
                    string c = sections[i].Trim();
                    string m = new string(_maskFillChar, l - c.Length);
                    result += m + c + maskChar;
                }
                else
                {
                    result += sections[i] + (i < sections.Length - 1 ? maskChar : "");
                }
            }

            return result;
        }


        /// <summary>
        /// Called after any text change (including programmatic .Text = )
        /// </summary>
        private void OnTextChanged(object? sender, EventArgs e)
        {
            AppIO.DebugLog($"OnTextChanged has {Text}");

            if (_isDisplayingNull)
                return;   // protect the .NULL. display

            if ((_maskResult == null || _maskResult.SpecialPositions.Count == 0) && Format_UpperCase == false)
                return;

            // Re-apply formatting to the entire current text
            ApplyMaskFormattingToCurrentText();
        }

        /// <summary>
        /// Applies ! and ^ rules to the entire current Text value.
        /// This ensures programmatic .Text assignments are also formatted correctly.
        /// </summary>
        private void ApplyMaskFormattingToCurrentText()
        {
            if ((string.IsNullOrEmpty(Text) || _maskResult == null) && Format_UpperCase == false)
                return;

            StringBuilder formatted = new StringBuilder(Text);
            bool changed = false;

            if (Text is not null)
            {
                for (int i = 0; i < Text.Length; i++)
                {
                    int jaxIndex = GetJAXMaskIndex(i);

                    if (_maskResult is not null)
                    {
                        SpecialMaskPosition? special = _maskResult.SpecialPositions.FirstOrDefault(p => p.Position == jaxIndex);

                        if (special != null && char.IsLetter(Text[i]))
                        {
                            char upper = char.ToUpper(Text[i], CultureInfo.InvariantCulture);
                            if (formatted[i] != upper || Format_UpperCase)
                            {
                                formatted[i] = upper;
                                changed = true;
                            }
                        }
                    }
                    else
                    {
                        // Fix upper case if ! is in format string
                        if (Format_UpperCase && char.IsLetter(Text[i]))
                        {
                            char upper = char.ToUpper(Text[i], CultureInfo.InvariantCulture);
                            if (formatted[i] != upper)
                            {
                                formatted[i] = upper;
                                changed = true;
                            }
                        }
                    }
                }
            }

            if (changed)
            {
                // Temporarily detach handler to prevent recursion
                TextChanged -= OnTextChanged;
                Text = formatted.ToString();
                TextChanged += OnTextChanged;

                // Restore caret position (very important)
                CaretIndex = System.Math.Min(CaretIndex, Text.Length);
            }
        }




        /// <summary>
        /// Maps a caret index in the displayed text back to the original JAXBase mask index,
        /// skipping literal characters (- / .).
        /// This is critical for correct caret positioning and rule application.
        /// </summary>
        private int GetJAXMaskIndex(int displayIndex)
        {
            if (_maskResult == null || string.IsNullOrEmpty(_jaxBaseMask))
                return displayIndex;

            int jaxIndex = 0;
            int currentDisplay = 0;

            for (int i = 0; i < _jaxBaseMask.Length && currentDisplay <= displayIndex; i++)
            {
                char c = _jaxBaseMask[i];

                if (c == '!' || c == 'A' || c == '^' || c == '9' || c == '#')
                {
                    if (currentDisplay == displayIndex)
                        return jaxIndex;

                    currentDisplay++;
                    jaxIndex++;
                }
                else
                {
                    // literal character (- / .) occupies a display position but not a JAX index
                    if (currentDisplay == displayIndex)
                        return -1; // literal position - no special rule

                    currentDisplay++;
                }
            }

            return jaxIndex;
        }

        /// <summary>
        /// Public method to force re-application of mask formatting (useful after data binding, etc.)
        /// </summary>
        public void RefreshMaskFormatting()
        {
            ApplyMaskFormattingToCurrentText();
        }


        // Start of my own coding
        private void ApplyJAXFormat()
        {
            _jaxBaseFormat = _jaxBaseFormat.ToUpper();

            SelectOnFocus = _jaxBaseFormat.Contains("K");
            RemoveMaskCharacters = _jaxBaseFormat.Contains("R");
            Format_UpperCase = _jaxBaseFormat.Contains("!");
            Format_Trim = _jaxBaseFormat.Replace("@T", "").Contains("T");
            Format_BlankZero = _jaxBaseFormat.Contains("Z");

            if (_jaxBaseFormat.Contains("@D"))
                ForcedDataType = "D";
            else if (_jaxBaseFormat.Contains("@T"))
                ForcedDataType = "T";
            else if (_jaxBaseFormat.Contains("@L"))
                ForcedDataType = "L";
            else if (_jaxBaseFormat.Contains("@N"))
                ForcedDataType = "N";
            else if (_jaxBaseFormat.Contains("@C"))
                ForcedDataType = "C";
        }


        /*
         * Called by parent code when control receives focus
         * 
         * 0 = ok to receive focus
         * 1 = error returned
         * 
         */
        public int OnGotFocus()
        {
            int result = 0;
            _textIsNull = false;

            if (SelectOnFocus) this.SelectAll();

            if (this.Text is null)
            {
                _textIsNull = true;
                // TODO - Handle the null display
            }

            return result;
        }

        /*
         * Called by parent code when control loses focus
         * 
         *  0 = ok to lose focus
         *  1 = error
         *  
         */
        public int OnLostFocus()
        {
            int result = 0;
            this.Text = GetCurrentText();
            return result;
        }


        /*  
         *  Return the current text as a string
         */
        public string GetCurrentText()
        {
            string result = "";

            if (Format_Trim && this.Text is not null)
                this.Text = this.Text.Trim();

            if (Format_BlankZero && this.Text is not null && ForcedDataType.Equals("N"))
                if (double.TryParse(this.Text, out double dbl) == false || dbl == 0.00)
                    this.Text = "";

            if (_textIsNull && this.Text is null)
            {
                // Handle null display on exit
            }

            return result;
        }



        /* 
         * Return the current value of the textbox as a Token value
         */
        public JAXObjects.Token GetValue()
        {
            JAXObjects.Token result = new();

            string preResult = this.Text ?? "";

            if (ForcedDataType.Equals("C") || string.IsNullOrEmpty(ForcedDataType))
            {
                // It's a character return value
                if (Format_Trim)
                    preResult = preResult.Trim();

                string mask = this.Mask ?? "";
                if (mask.Length > preResult.Length) preResult = preResult.Trim();

                if (RemoveMaskCharacters)
                {
                    mask = JAXLib.ChrTran(mask, "!^A9#", "");
                    preResult = JAXLib.ChrTran(preResult, mask, "");
                }

                result.Element.Value = preResult;
            }
            return result;
        }


        /// <summary>
        /// Override to handle null display specially.
        /// Call this when setting the value from JAXBase (e.g. via .Value or data binding).
        /// </summary>
        public void SetValue(object? value)
        {
            JAXObjects.Token tok = new();

            if (value is null)
                tok.Element.MakeNull();
            else
                tok.Element.Value = value;

            //if (!ShowNullAsText || value != null && !string.IsNullOrEmpty(value.ToString()))
            if (value != null)
            {
                _isDisplayingNull = false;
                Text = value?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(ForcedDataType) && value is not null)
                    ForcedDataType = tok.Element.Type;

                return;
            }

            // Show .NULL. instead of mask or empty
            _isDisplayingNull = true;
            Mask = string.Empty;                    // temporarily disable mask
            Text = NullDisplayText;
            IsReadOnly = true;                      // prevent editing while showing .NULL.
        }
    }
}