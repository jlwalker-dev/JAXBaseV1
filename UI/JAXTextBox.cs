/*
 * 2026-01-17 - JLW
 *      Worked with GROK to create this class.  Mask/Format/Value needed the hidden statement
 *      added, otherwise the syntax was bug-free.  Now I just need to find the time to test 
 *      it out and fix issues.
 *      
 *      As I go through and begin to understand it, I'll document the hell out of it.
 *      
 *      I know it currently sets _value to null if the input is invalid.  I will
 *      need to change that to the empty value based on type.
 *      
 *      Will also need to validate the value.  I could ignore the format
 *      if the wrong type of value was given (like boolean for @N format) but
 *      I think I'll just toss an error.
 *      
 *      Or perhaps I'll try to convert and toss if it it can't be converted. Too
 *      much to think about now, though.  Working on grids and needed to get
 *      this off my mind.
 *      
 */
using JAXBase.Core;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.UI
{
    public class JAXTextBox : TextBox
    {
        private string _format = "";
        private string _mask = "";
        private JAXObjects.Token _value;

        private enum InputType
        {
            StringPlain,
            StringUpper,
            StringWithLiterals,
            StringNoLiterals,
            Numeric,
            Date,
            DateTime,
            BooleanDotTF,
            BooleanTF,
            BooleanYN,
            Email
        }

        private InputType _inputType = InputType.StringPlain;
        private string _effectiveMask = "";
        private bool _hasLiterals = false;
        private char _decimalSep;
        private char _groupSep;

        // AM/PM support for @T
        private bool _is12HourFormat = false;
        private bool _hasAmPm = false;
        private string _amPmMask = ""; // "pp" or "PP"

        public JAXTextBox()
        {
            _value = new JAXObjects.Token();
            _value.Element.MakeNull();
            TextAlign = HorizontalAlignment.Left;
            KeyPress += JAXTextBox_KeyPress;
            KeyDown += JAXTextBox_KeyDown;
            TextChanged += JAXTextBox_TextChanged;
            Leave += JAXTextBox_Leave;
            Enter += JAXTextBox_Enter;
            UpdateCultureSeparators();
        }

        
        [Category("Behavior"), Description("The @ command (e.g. @N, @D, @L, @C, @S, @!, @E, @B, @Y)")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]   // No initialization code, runtime only
        public string Format
        {
            get => _format;
            set
            {
                _format = value?.Trim() ?? "";
                ParseFormatAndMask();
            }
        }

        [Category("Behavior"), Description("The picture/mask template (e.g. 9999.99, MM/dd/yyyy, !999-99-99)")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]   // No initialization code, runtime only
        public string Mask
        {
            get => _mask;
            set
            {
                _mask = value?.Trim() ?? "";
                ParseFormatAndMask();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]   // No initialization code, runtime only
        public JAXObjects.Token Value
        {
            get
            {
                RefreshValueFromText();
                return _value;
            }
            set
            {
                if (value == null)
                {
                    _value = new JAXObjects.Token();
                    _value.Element.MakeNull();
                }
                else
                {
                    _value = value;
                }
                ApplyCurrentValueToText();
            }
        }

        private void UpdateCultureSeparators()
        {
            var nf = NumberFormatInfo.CurrentInfo;
            _decimalSep = nf.NumberDecimalSeparator[0];
            _groupSep = nf.NumberGroupSeparator[0];
        }

        private bool IsFormatMaskCompatible()
        {
            if (string.IsNullOrEmpty(_format)) return true;

            string f = _format.Trim().ToUpperInvariant();
            if (!f.StartsWith("@")) return true;

            char cmd = f.Length > 1 ? f[1] : '\0';

            switch (cmd)
            {
                case 'N':
                    return _effectiveMask.All(c => char.IsDigit(c) || c == '9' || c == '#' || c == '.' || c == '-' || c == _groupSep || c == _decimalSep || c == '!');
                case 'D':
                    return _effectiveMask.Any(c => "MmDdYy".Contains(c));
                case 'T':
                    return _effectiveMask.Any(c => "HhMmSs:".Contains(c)) || _effectiveMask.Contains("pp") || _effectiveMask.Contains("PP");
                case 'L':
                case 'B':
                case 'Y':
                    return _effectiveMask == ".!." || _effectiveMask == "!" || string.IsNullOrEmpty(_effectiveMask);
                default:
                    return true;
            }
        }

        private void ParseFormatAndMask()
        {
            // Preserve current value before we potentially change type/mask
            RefreshValueFromText();

            _inputType = InputType.StringPlain;
            _effectiveMask = _mask;
            _hasLiterals = false;
            _is12HourFormat = false;
            _hasAmPm = false;
            _amPmMask = "";

            if (string.IsNullOrEmpty(_format))
            {
                MaxLength = string.IsNullOrEmpty(_mask) ? 32767 : _mask.Length;
                return;
            }

            string fmt = _format.ToUpperInvariant();
            if (fmt.StartsWith("@"))
            {
                string cmd = fmt.Length > 1 ? fmt.Substring(1, 1) : "";
                string rest = fmt.Length > 2 ? fmt.Substring(2).Trim() : "";
                switch (cmd)
                {
                    case "N": _inputType = InputType.Numeric; _effectiveMask = string.IsNullOrEmpty(rest) ? "999999.99" : rest; break;
                    case "D": _inputType = InputType.Date; _effectiveMask = string.IsNullOrEmpty(rest) ? "MM/dd/yyyy" : rest; break;
                    case "T":
                        _inputType = InputType.DateTime;
                        _effectiveMask = string.IsNullOrEmpty(rest) ? "MM/dd/yyyy HH:mm:ss" : rest;
                        AnalyzeDateTimeMask();
                        break;
                    case "L": _inputType = InputType.BooleanDotTF; _effectiveMask = ".!."; break;
                    case "B": _inputType = InputType.BooleanTF; _effectiveMask = "!"; break;
                    case "Y": _inputType = InputType.BooleanYN; _effectiveMask = "!"; break;
                    case "!": _inputType = InputType.StringUpper; break;
                    case "E": _inputType = InputType.Email; _effectiveMask = ""; break;
                    case "S": _inputType = InputType.StringNoLiterals; break;
                    case "C": _inputType = InputType.StringWithLiterals; break;
                    default: _inputType = InputType.StringPlain; break;
                }
            }

            if (_effectiveMask.Length > 0 && _inputType != InputType.StringPlain && _inputType != InputType.StringUpper && _inputType != InputType.Email)
            {
                MaxLength = _effectiveMask.Length;
            }
            else
            {
                MaxLength = 32767;
            }

            _hasLiterals = _effectiveMask.Any(c => !IsMaskChar(c));

            // Only apply if it looks safe
            if (IsFormatMaskCompatible())
            {
                ApplyCurrentValueToText();
            }
        }

        private void AnalyzeDateTimeMask()
        {
            _is12HourFormat = _effectiveMask.Contains("hh") || _effectiveMask.Contains("h");
            _hasAmPm = _effectiveMask.Contains("pp") || _effectiveMask.Contains("PP");
            if (_hasAmPm)
            {
                _amPmMask = _effectiveMask.Contains("pp") ? "pp" : "PP";
            }
        }

        private bool IsMaskChar(char c)
        {
            return c is '9' or '!' or '#' or 'M' or 'm' or 'd' or 'D' or 'y' or 'Y' or 'H' or 'h' or 's' or '.';
        }

        private void JAXTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            int pos = SelectionStart;
            if (pos >= Text.Length) pos = Text.Length - 1;
            if (!IsInputAllowed(e.KeyChar, pos))
            {
                e.Handled = true;
                return;
            }
            // Boolean special cases
            if (_inputType == InputType.BooleanDotTF && pos == 1)
            {
                char ch = char.ToUpper(e.KeyChar);
                if (ch != 'T' && ch != 'F') { e.Handled = true; return; }
                e.KeyChar = ch;
            }
            else if ((_inputType == InputType.BooleanTF || _inputType == InputType.BooleanYN) && pos == 0)
            {
                char ch = char.ToUpper(e.KeyChar);
                if (_inputType == InputType.BooleanTF && ch != 'T' && ch != 'F') { e.Handled = true; return; }
                if (_inputType == InputType.BooleanYN && ch != 'Y' && ch != 'N') { e.Handled = true; return; }
                e.KeyChar = ch;
            }
            else if (_inputType == InputType.StringUpper || _inputType == InputType.StringWithLiterals || _inputType == InputType.StringNoLiterals)
            {
                e.KeyChar = char.ToUpper(e.KeyChar);
            }
            // AM/PM input handling
            else if (_inputType == InputType.DateTime && _hasAmPm)
            {
                int amPmStart = _effectiveMask.IndexOf(_amPmMask);
                if (amPmStart >= 0 && pos >= amPmStart && pos < amPmStart + 2)
                {
                    char ch = char.ToUpper(e.KeyChar);
                    if (pos == amPmStart)
                    {
                        if (ch != 'A' && ch != 'P') { e.Handled = true; return; }
                    }
                    else if (pos == amPmStart + 1)
                    {
                        if (ch != 'M') { e.Handled = true; return; }
                    }
                    // Force case to match mask
                    e.KeyChar = (_amPmMask == "pp") ? char.ToLower(e.KeyChar) : char.ToUpper(e.KeyChar);
                }
            }
        }

        private bool IsInputAllowed(char ch, int pos)
        {
            if (string.IsNullOrEmpty(_effectiveMask)) return true;
            if (pos >= _effectiveMask.Length) return false;
            char maskChar = _effectiveMask[pos];
            if (!IsMaskChar(maskChar))
            {
                if (_inputType == InputType.StringNoLiterals && ch == maskChar) return false;
                return ch == maskChar;
            }
            switch (maskChar)
            {
                case '9': return char.IsDigit(ch) || (ch == '-' && pos == 0 && _inputType == InputType.Numeric);
                case '!': return true;
                case '#': return char.IsLetter(ch);
                case 'M':
                case 'm':
                case 'd':
                case 'D':
                case 'y':
                case 'Y':
                case 'H':
                case 'h':
                case 's': return char.IsDigit(ch);
                default: return false;
            }
        }

        private void JAXTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (_inputType == InputType.StringUpper || _inputType == InputType.StringWithLiterals || _inputType == InputType.StringNoLiterals)
            {
                int sel = SelectionStart;
                Text = Text.ToUpperInvariant();
                SelectionStart = sel;
            }
            // Force boolean display
            if (_inputType == InputType.BooleanDotTF)
            {
                if (Text.Length == 3 && (Text[1] == 'T' || Text[1] == 'F')) return;
                Text = Text.Length > 1 && char.ToUpper(Text[1]) is 'T' or 'F' ? $".{char.ToUpper(Text[1])}." : ". .";
                SelectionStart = 1;
            }
            else if (_inputType == InputType.BooleanTF)
            {
                if (Text.Length == 1 && (Text[0] == 'T' || Text[0] == 'F')) return;
                Text = Text.Length > 0 && char.ToUpper(Text[0]) is 'T' or 'F' ? char.ToUpper(Text[0]).ToString() : " ";
                SelectionStart = 0;
            }
            else if (_inputType == InputType.BooleanYN)
            {
                if (Text.Length == 1 && (Text[0] == 'Y' || Text[0] == 'N')) return;
                Text = Text.Length > 0 && char.ToUpper(Text[0]) is 'Y' or 'N' ? char.ToUpper(Text[0]).ToString() : " ";
                SelectionStart = 0;
            }
        }

        private void JAXTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                HandlePaste();
            }
            else if (e.Control && e.KeyCode == Keys.X)
            {
                e.Handled = true;
                HandleCut();
            }
            // Copy uses default behavior (full text)
        }

        private void HandlePaste()
        {
            if (!Clipboard.ContainsText()) return;
            string pasted = Clipboard.GetText();
            int pos = SelectionStart;
            int len = SelectionLength;
            StringBuilder newText = new StringBuilder(Text.Substring(0, pos));
            int maskPos = pos;
            foreach (char ch in pasted)
            {
                if (maskPos >= _effectiveMask.Length) break;
                if (IsInputAllowed(ch, maskPos))
                {
                    newText.Append(ch);
                    maskPos++;
                }
                else if (_inputType == InputType.StringNoLiterals && ch == _effectiveMask[maskPos])
                {
                    maskPos++; // skip literal
                }
            }
            newText.Append(Text.Substring(pos + len));
            Text = newText.ToString();
            SelectionStart = pos + (maskPos - pos);
        }

        private void HandleCut()
        {
            int pos = SelectionStart;
            int len = SelectionLength;
            if (len == 0) return;
            Text = Text.Substring(0, pos) + Text.Substring(pos + len);
            SelectionStart = pos;
        }

        private void JAXTextBox_Leave(object? sender, EventArgs e)
        {
            RefreshValueFromText();
            if (_value.Element.Type == "X")
            {
                Text = "";
                return;
            }
            switch (_inputType)
            {
                case InputType.Date:
                    if (_value.Element.Value is DateOnly d)
                        Text = d.ToString(_effectiveMask, CultureInfo.CurrentCulture);
                    break;
                case InputType.DateTime:
                    if (_value.Element.Value is DateTime dt)
                        Text = FormatDateTimeWithAmPm(dt);
                    break;
                case InputType.Numeric:
                    if (_value.Element.Value is double n)
                        Text = n.ToString("N2", CultureInfo.CurrentCulture);
                    break;
            }
        }

        private string FormatDateTimeWithAmPm(DateTime dt)
        {
            // Replace pp/PP with tt for standard formatting, then adjust case
            string format = _effectiveMask
                .Replace("pp", "tt")
                .Replace("PP", "tt");
            string result = dt.ToString(format, CultureInfo.CurrentCulture);
            if (_hasAmPm && _amPmMask.Length == 2)
            {
                string ampm = dt.ToString("tt", CultureInfo.CurrentCulture);
                if (_amPmMask == "pp")
                    ampm = ampm.ToLowerInvariant();
                else if (_amPmMask == "PP")
                    ampm = ampm.ToUpperInvariant();
                int amPmIndex = _effectiveMask.IndexOf(_amPmMask);
                if (amPmIndex >= 0)
                {
                    result = result.Remove(amPmIndex, 2).Insert(amPmIndex, ampm);
                }
            }
            return result;
        }

        private void JAXTextBox_Enter(object? sender, EventArgs e)
        {
            if (Text.Length > 0) SelectAll();
        }

        private void RefreshValueFromText()
        {
            string cleaned = GetCleanedInput();
            _value = new JAXObjects.Token();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                _value.Element.MakeNull();
                return;
            }
            try
            {
                switch (_inputType)
                {
                    case InputType.Numeric:
                        string numClean = cleaned.Replace(_groupSep.ToString(), "");
                        if (double.TryParse(numClean, NumberStyles.Any, CultureInfo.CurrentCulture, out double n))
                            _value.Element.Value = n;
                        else
                            _value.Element.MakeNull();
                        break;
                    case InputType.Date:
                        if (DateOnly.TryParse(cleaned, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateOnly d))
                            _value.Element.Value = d;
                        else
                            _value.Element.MakeNull();
                        break;
                    case InputType.DateTime:
                        string parseFormat = _effectiveMask.Replace("pp", "tt").Replace("PP", "tt");
                        if (DateTime.TryParseExact(Text, parseFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dtExact))
                            _value.Element.Value = dtExact;
                        else if (DateTime.TryParse(Text, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime dt))
                            _value.Element.Value = dt;
                        else
                            _value.Element.MakeNull();
                        break;
                    case InputType.BooleanDotTF:
                    case InputType.BooleanTF:
                    case InputType.BooleanYN:
                        bool b = false;
                        if (_inputType == InputType.BooleanDotTF)
                            b = cleaned.Length == 3 && cleaned[1] == 'T';
                        else if (_inputType == InputType.BooleanTF)
                            b = cleaned == "T";
                        else
                            b = cleaned == "Y";
                        _value.Element.Value = b;
                        break;
                    case InputType.Email:
                        if (Regex.IsMatch(cleaned, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            _value.Element.Value = cleaned;
                        else
                            _value.Element.MakeNull();
                        break;
                    default:
                        _value.Element.Value = cleaned;
                        break;
                }
            }
            catch
            {
                _value.Element.MakeNull();
            }
        }

        private string GetCleanedInput()
        {
            if (_inputType == InputType.StringWithLiterals || _inputType == InputType.StringPlain)
                return Text;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Text.Length && i < _effectiveMask.Length; i++)
            {
                if (IsMaskChar(_effectiveMask[i]))
                    sb.Append(Text[i]);
            }
            return sb.ToString().Trim();
        }

        private void ApplyCurrentValueToText()
        {
            if (_value == null || _value.Element.Type == "X" || _value.Element.IsNull())
            {
                Text = "";
                return;
            }

            if (!IsFormatMaskCompatible())
            {
                // prevent WFO1000 by not pushing bad state
                return;
            }

            object val = _value.Element.Value;
            if (val == null)
            {
                Text = "";
                return;
            }
            switch (_inputType)
            {
                case InputType.Numeric:
                    if (val is double d) Text = d.ToString("N2", CultureInfo.CurrentCulture);
                    break;
                case InputType.Date:
                    if (val is DateOnly da) Text = da.ToString(_effectiveMask, CultureInfo.CurrentCulture);
                    break;
                case InputType.DateTime:
                    if (val is DateTime dtm) Text = FormatDateTimeWithAmPm(dtm);
                    break;
                case InputType.BooleanDotTF:
                    Text = (bool)val ? ".T." : ".F.";
                    break;
                case InputType.BooleanTF:
                    Text = (bool)val ? "T" : "F";
                    break;
                case InputType.BooleanYN:
                    Text = (bool)val ? "Y" : "N";
                    break;
                default:
                    Text = val.ToString()?.ToUpperInvariant() ?? "";
                    break;
            }
        }
    }
}