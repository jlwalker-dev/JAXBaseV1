/******************************************************************************************************************************************
 * This is the main language manager
 ******************************************************************************************************************************************/
using JAXBase.Core;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JAXBase.Utilities.Utilities;

namespace JAXBase.Language
{
    // Static classes that hold routines to mimic other actions of other xBase variations
    public class JAXLanguage
    {
        // https://stackoverflow.com/questions/577411/how-can-i-find-the-state-of-numlock-capslock-and-scrolllock-in-net
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        /*-------------------------------------------------------------------------------------------------*
         * TODO - menu, popup
         *-------------------------------------------------------------------------------------------------*/
        public static void Activate(AppClass app, string cmd, string cmdExtra)
        {

        }

        /*-------------------------------------------------------------------------------------------------*
         * TODO - Cancel - quite the program
         *-------------------------------------------------------------------------------------------------*/
        public static void Cancel()
        {

        }

        /*-------------------------------------------------------------------------------------------------*
         * Clear all memory, popups, menus, widows, and close all tables and databases
         *-------------------------------------------------------------------------------------------------*/
        public static void Clear_All(AppClass app)
        {
            // Clear popups
            Clear_Popups(app);

            // Clear menus
            Clear_Menus(app);

            // Clear windows
            Clear_Windows(app);

            // Close all tables
            Clear_Tables(app);

            // Close all databases
            Clear_Databases(app);

            // Clear memory
            Clear_Memory(app);
        }

        public static void Clear_Popups(AppClass app) { }
        public static void Clear_Menus(AppClass app) { }
        public static void Clear_Windows(AppClass app) { }
        public static void Clear_Tables(AppClass app) { }
        public static void Clear_Databases(AppClass app) { }
        public static void Clear_Read(AppClass app, bool clearAll) { }

        /*-------------------------------------------------------------------------------------------------*
         * Clear all variables from all levels
         *-------------------------------------------------------------------------------------------------*/
        public static void Clear_Memory(AppClass app)
        {
            for (int i = app.AppLevels.Count; i > 0; i--)
            {
                app.AppLevels[i - 1].LocalVars = new();
                app.AppLevels[i - 1].PrivateVars = new();
            }
        }


        /*-------------------------------------------------------------------------------------------------*
         * Quick and dirty returning a formatted string based on type
         *-------------------------------------------------------------------------------------------------*/
        public static string GetDisplayValue(JAXObjects.SimpleToken simpleToken)
        {
            string sExpr;

            if (simpleToken.Type.Equals("D"))
                sExpr = string.Format("{0} ", simpleToken.Value.ToString()).Replace("-", "/")[..10];
            else if (simpleToken.Type.Equals("T"))
            {
                sExpr = string.Format("{0} ", simpleToken.Value.ToString()).Replace("-", "/").Replace("T", " ");
                string stime = sExpr[11..].Trim();
                if (int.TryParse(stime[..2], out int ihrs) == false)
                    ihrs = 0;

                if (ihrs > 12)
                    sExpr = string.Concat(sExpr.AsSpan()[..11], (ihrs - 12).ToString("00"), sExpr.AsSpan(14), " PM");
                else if (ihrs == 0)
                    sExpr = string.Concat(sExpr.AsSpan()[..11], "12", sExpr.AsSpan(14), " AM");
                else if (ihrs == 12)
                    sExpr += " PM";
                else
                    sExpr += " AM";
            }
            else
                sExpr = string.Format("{0} ", simpleToken.Value.ToString());

            return sExpr;
        }




        /*-------------------------------------------------------------------------------------------------*
         * Mimics the inkey() functionality without returning the mousekey record
         *-------------------------------------------------------------------------------------------------*/
        public static int InKey(int milliwait)
        {
            return InKeyMouse(milliwait, 1, true, out _);
        }

        /*-------------------------------------------------------------------------------------------------*
         *** Windows ***
         *
         * Format Codes
         * ------------
         * Specifies the input and output formatting fo a control's Value property.
         * 
         * 
         * Text/Edit box
         * K   Selects all the text

         * Spinner control 
         * $   Displays the currency symbol.
         * ^   Displays numeric data using scientific notation.
         * K   Selects all the text when the control gets the focus.
         * L   Displays leading zeros instead of spaces in the text box.
         * R   Displays the format mask for the text box that is specified in the InputMask property. 
         *     The mask formats data for easier entry and clearer display (for example, if the mask is 99-999, the number 12345 is displayed as 12-345), but is not stored as part of the data. Use only with character or numeric data.
         * 
         * Text Box
         * !   Converts alphabetic characters to uppercase. Use with data of Character type only.
         * $   Displays the currency symbol. The ControlSource property must specify a numeric source for the text box.
         * ^   Displays numeric data using scientific notation. The ControlSource property must specify a numeric source for the text box.
         * A   Allows alphabetic characters only with no spaces or punctuation marks.
         * D   Uses the current SET DATE format.
         * E   Edits Date values as British dates. 
         * F   Prevents Varchar values from being padded with trailing spaces in text boxes or Varbinary values from being padded with trailing zeroes (0s).
         *
         *     Note  
         *     For text boxes bound to data with Varchar type, the MaxLength property must be set to a nonzero value to permit input for the desired number of characters. 
         * 
         *     Note
         *     When you drag a field with Varchar type to a form, the created text box's Format property is set to "F", and the MaxLength property is set to the maximum length for the Varchar field. When you drag a Grid control to a form and create TextBox controls for columns with Varchar type, the Format property is set to "F", and the MaxLength property is set to the maximum length for the Varchar field. 
         * 
         *     Note
         *     When you drag a field with Varbinary type to a form or when you drag a Grid control to a form and create TextBox controls for columns with Varbinary type, the following occur:
         *     Format property is set to "F".
         *     MaxLength property is set to the maximum length multiplied by 2 for the Varchar field. 
         *     InputMask property is filled with "H" up to the value of the MaxLength property.
         * 
         * K   Selects all the text when the control gets the focus.
         * L   Displays leading zeros instead of spaces in the text box. The ControlSource property must specify a numeric source for the text box.
         * M   Included for backward compatibility.
         * R   Displays the format mask for the text box that is specified in the InputMask property. 
         *     The mask formats data for easier entry and clearer display. For example, if the mask is 99-999, the number 12345 is displayed as 12-345 but is not stored as part of the data. Use only with Character or Numeric data. 
         * T   Trims leading and trailing blanks from the input field.
         * 
         * YS  Displays Date values in a short date format determined by the Windows Control Panel short date setting.
         * YL  Displays Date values in a long date format determined by the Windows Control Panel long date setting.
         * 
         * EditBox control, TextBox control, Column object, and Spinner control
         * Z   Displays the value as blank if it is 0, except when the control has focus.
         *     Dates are also supported in these controls. The / / date delimiters are not displayed unless the control has focus.
         * 
         * Input Mask
         * ----------
         * !   Converts lowercase letters to uppercase letters.
         * #   Permit entry of digits, spaces, and signs, such as the minus sign ( – ).
         * $   Displays the current currency symbol, as specified by the SET CURRENCY command, in a fixed position.
         * $$  Displays floating currency symbol that always appears adjacent to the digits in a spinner or text box.
         * ,   Displays the current digit-grouping, or separator, symbol as set by the Regional and Language Options setting in the Windows Control Panel. 
         * .   Displays the current decimal point character as specified by the SET POINT command setting. (Default is a period (.))
         * 9   Permits entry of digits and signs.
         * A   Permits alphabetic characters only.
         * H   Prevents entry of non-hexadecimal symbols in the specified position.
         * L   Permits logical data only.
         * N   Permits letters and digits only.
         * U   Permits alphabetic characters only and converts them to uppercase (A - Z).
         * W   Permits alphabetic characters only and converts them to lowercase (a - z).
         * X   Permits any character.
         * Y   Permits the letters, Y, y, N, and n for the logical values True (.T.) and False (.F.), respectively.
         * 
         * 
         * Move Value
         * ----------
         * Returns integer indicating how the user exited the textbox
         * 9/15    - tab and backtab
         * 13/10   - Enter key and Ctrl+Enter
         * 5/24    - Up/Down arrows
         * 
         * If Confirm is off and the user types past the end of the textbox then a 13 ir returned
         * 
         * 
         * NOTE: For Windows form, MASKEDTEXTBOX is the closes we've got to a VFP text box in C#
         * 
         * Take a string and expected type and then see if it converts correctly, returning a bool as the result
         * and sending out a siple token with the value.  If it does not convert, it returns a null value in 
         * the simple token.
         *-------------------------------------------------------------------------------------------------*/
        public static bool ConvertAndValidateAsType(string inputString, string outputType, out JAXObjects.SimpleToken stoken)
        {
            bool result = true;
            stoken = new();
            stoken.MakeNull();

            switch (outputType)
            {
                case "C":
                    stoken.Value = inputString;
                    break;

                case "T":
                    // step through and fix so that " x" and "x " becomes "0x"
                    // change xx/xx/xx to xx/xx/xxxx
                    // DT needs AM or PM at end
                    if (DateTime.TryParse(inputString, out DateTime dtm) == false)
                        result = false;
                    else
                        stoken.Value = dtm;
                    break;

                case "D":
                    // step through and fix so that " x" and "x " becomes "0x"
                    // change xx/xx/xx to xx/xx/xxxx
                    // DT needs AM or PM at end
                    if (DateOnly.TryParse(inputString, out DateOnly dto) == false)
                        result = false;
                    else
                        stoken.Value = dto;
                    break;

                case "L":
                    if (JAXLib.InList(inputString.ToUpper().Trim(), ".T.", ".F.", "Y", "N", "0", "1", "TRUE", "FALSE"))
                        stoken.Value = JAXLib.InList(inputString.ToUpper().Trim(), ".T.", "Y", "1", "TRUE");
                    else
                        result = false;
                    break;

                case "N":
                    inputString = inputString.Replace(" ", "");     // Clean it up for string to numeric conversion

                    // Kill any characters that don't belong
                    for (int i = 0; i < inputString.Length; i++)
                    {
                        if (("0123456789.".Contains(inputString[i]) || (i == 0 && "+-".Contains(inputString[i]))) == false)
                            inputString = inputString.Remove(i, 1);
                    }

                    if (double.TryParse(inputString, out double dbnum) == false)
                        result = false;
                    else
                        stoken.Value = dbnum;
                    break;

                default:
                    throw new Exception(string.Format("Unknown variable type {0}", outputType));
            }

            return result;
        }

        /*-------------------------------------------------------------------------------------------------*
         * Align a vaue with the input mask
         * 
         * Take a simple token and place it into a string using the supplied input mask.
         * A string will left align, a date will format into the provided string, a
         * number will be right justified and if there is a period in the mask it will
         * align with the decimal.
         *-------------------------------------------------------------------------------------------------*/
        public static string AlignValueWithMask(JAXObjects.SimpleToken sToken, string inputMask)
        {
            // pad the input value if needed so everything
            // matches in size
            int fieldLen = inputMask.Length;
            string inputValue = sToken.ValueAsString;

            int charPos = 0;
            int inputPos = 0;

            char[] inputBuffer = inputValue.ToCharArray();
            char[] inputChars = new string(' ', fieldLen).ToCharArray();
            char[] maskChars = inputMask.ToCharArray();
            int decPos = inputMask.IndexOf('.');
            string elementType = sToken.Type;

            if (JAXLib.InList(sToken.Type, "X", "U"))
            {
                // Unknown and null values are not allowed
                // pass the value back as all blanks
            }
            else
            {
                while (true)
                {

                    if (inputPos >= inputBuffer.Length || charPos >= fieldLen)
                    {
                        // We're at the end of the input string
                        // or at the end of the input mask
                        // so we're done and we ignore anything
                        // left in the input buffer
                        break;
                    }

                    if ("X9Y!".Contains(maskChars[charPos]) == false)
                    {
                        // Unknown format code so just stick it in
                        // and loop around in case there's another one
                        inputChars[charPos] = maskChars[charPos++];
                    }
                    else
                    {
                        int key = inputBuffer[inputPos++];

                        char k = (char)key;

                        // Put the character through the input mask
                        if (maskChars[charPos].Equals('X'))                                     // Any character
                            inputChars[charPos++] = k;
                        else if (maskChars[charPos].Equals('!'))                                // Upper case
                            inputChars[charPos++] = k.ToString().ToUpper()[0];
                        else if (maskChars[charPos].Equals('9') && JAXLib.Between(k, '0', '9')) // Digits only
                            inputChars[charPos++] = k;
                        else if (k.Equals('.') && elementType.Equals("N"))                      // Handle the period for a numeric type
                        {
                            // If this is a numeric entry
                            // if user entered the period and it's to the right
                            // then move everything over to align with the period
                            // else if there is no period in the string thus far
                            // then add it here
                            if (decPos >= charPos && charPos > 0)
                            {
                                // We have a decimal in the mask
                                // so we need to align what we have
                                // with the decimal
                                int j = charPos - 1;  // Place on last char pos as this one should be blank
                                int p = decPos - 1;
                                for (int i = decPos - 1; i >= 0; i--)
                                {
                                    if (j < 0)
                                    {
                                        // Blank out everything before this
                                        while (p >= 0)
                                            inputChars[p--] = ' ';
                                        break;
                                    }
                                    else
                                    {
                                        if (inputChars[j].Equals(',') == false)
                                            inputChars[p--] = j >= 0 ? inputChars[j--] : ' ';
                                        else
                                            j--;
                                    }
                                }

                                inputChars[decPos] = '.';
                                charPos = decPos + 1;
                            }
                            else
                            {
                                // No decimal in the mask, so put it here
                                decPos = charPos;
                                inputChars[charPos++] = k;
                            }
                        }
                    }
                }
            }

            return new string(inputChars);
        }


        /*-------------------------------------------------------------------------------------------------*
         * Expanded MouseKeyAction class
         *-------------------------------------------------------------------------------------------------*/
        // App Code modified using https://stackoverflow.com/questions/1944481/console-app-mouse-click-x-y-coordinate-detection-comparison
        public class MouseKeyAction
        {
            public int EventType = 0;  // 0=nothing, 1=keyboard, 2=mouse
            public int keyCode = 0;
            public int keyState = 0;
            public int scanCode = 0;
            public bool Alt = false;
            public bool Shift = false;
            public bool Ctrl = false;
            public bool RtAlt = false;
            public bool RtCtrl = false;
            public bool KeyPad = false;
            public bool CapsLock = false;
            public bool ScrollLock = false;
            public bool NumLock = false;
            public string KeyLabel = string.Empty;

            public int InKeyCode = 0;

            public bool LeftButton = false;
            public bool RightButton = false;
            public int MouseButton = 0;
            public int MouseScroll = 0;
            public int xPos = 0;
            public int yPos = 0;
        }


        /*-------------------------------------------------------------------------------------------------*
         * Testing routine for the InKeyMouse rountine
         * Downloaded the example program from https://www.medo64.com/2013/05/console-mouse-input-in-c/
         *-------------------------------------------------------------------------------------------------*/
        public static void KeyMouseTest()
        {
            int k = 0;
            while (k != 27)
            {
                k = InKeyMouse(0, 100, out MouseKeyAction action);

                Console.SetCursorPosition(0, 0);
                Console.WriteLine(string.Format("Event Type ={0}", action.EventType));

                if (action.EventType == 1)
                {
                    Console.WriteLine(string.Format("         Key={0}     ", JAXLib.Between(k, 32, 127) ? ((char)k).ToString() : k));
                    Console.WriteLine(string.Format("        Ctrl={0}     ", action.Ctrl));
                    Console.WriteLine(string.Format("         Alt={0}     ", action.Alt));
                    Console.WriteLine(string.Format("       Shift={0}     ", action.Shift));
                    Console.WriteLine(string.Format("   Right Alt={0}     ", action.RtAlt));
                    Console.WriteLine(string.Format("  Right Ctrl={0}     ", action.RtCtrl));
                    Console.WriteLine(string.Format("      KeyPad={0}     ", action.KeyPad));
                    Console.WriteLine(string.Format("                     ", action.RtAlt));
                }

                if (action.EventType == 2)
                {
                    Console.WriteLine(string.Format(" Left Button={0}     ", action.LeftButton));
                    Console.WriteLine(string.Format("Right Button={0}     ", action.RightButton));
                    Console.WriteLine(string.Format("        XPos={0}     ", action.xPos));
                    Console.WriteLine(string.Format("        YPos={0}     ", action.yPos));
                    Console.WriteLine(string.Format("                     ", action.RtAlt));
                    Console.WriteLine(string.Format("                     ", action.RtAlt));
                    Console.WriteLine(string.Format("                     ", action.RtAlt));
                    Console.WriteLine(string.Format("                     ", action.RtAlt));
                }
            }
        }


        /*-------------------------------------------------------------------------------------------------*
         * This routine looks for a keystroke or mouse key action.  It will also allow you to move the
         * mouse cursor around the form.  As of 2/15/2025 I have not yet upgraded the DOS forms to 
         * use the mouse, and probably won't as all of this is done with little or no extra code 
         * in the Windows forms.
         * 
         * Expanded from the source I downloaded at https://www.medo64.com/content/consolemouse.zip
         * to act like the inkey() routine.  The MouseKeyAction returns a lot more and gives
         * you the ability to know if the shift/alt/ctrl keys were down along with keypad and other
         * toggled information.
         *-------------------------------------------------------------------------------------------------*/
        public static int InKeyMouse(int milliwait, int cursorSize, out MouseKeyAction mouseKeyAction)
        {
            return InKeyMouse(milliwait, cursorSize, true, out mouseKeyAction);
        }

        public static int InKeyMouse(int milliwait, int cursorSize, bool UseMouse, out MouseKeyAction mouseKeyAction)
        {
            int iKey = 0;

            DateTime waitUntil = milliwait == 0 ? DateTime.Now.AddYears(99) : DateTime.Now.AddMilliseconds(milliwait);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.CursorSize = JAXLib.Between(cursorSize, 1, 100) ? cursorSize : 1;

            mouseKeyAction = new();
            var handle = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);

            int mode = 0;
            if (!(NativeMethods.GetConsoleMode(handle, ref mode))) { throw new Win32Exception(); }

            mode |= NativeMethods.ENABLE_MOUSE_INPUT;
            mode &= ~NativeMethods.ENABLE_QUICK_EDIT_MODE;
            mode |= NativeMethods.ENABLE_EXTENDED_FLAGS;

            if (!NativeMethods.SetConsoleMode(handle, mode)) { throw new Win32Exception(); }

            var record = new NativeMethods.INPUT_RECORD();
            uint recordLen = 0;
            bool waiting = true;
            while (waiting && waitUntil > DateTime.Now)
            {
                mouseKeyAction.EventType = 0;

                if (!NativeMethods.ReadConsoleInput(handle, ref record, 1, ref recordLen)) { throw new Win32Exception(); }

                // Update for caps, num, and scroll lock settings
                // https://stackoverflow.com/questions/577411/how-can-i-find-the-state-of-numlock-capslock-and-scrolllock-in-net
                mouseKeyAction.CapsLock = (((ushort)GetKeyState(0x14)) & 0xffff) != 0;
                mouseKeyAction.NumLock = (((ushort)GetKeyState(0x90)) & 0xffff) != 0;
                mouseKeyAction.ScrollLock = (((ushort)GetKeyState(0x91)) & 0xffff) != 0;

                switch (record.EventType)
                {
                    case NativeMethods.MOUSE_EVENT:
                        // Button State Left=0x0001, right=0x0002, scroll up 0x780000, scroll down 0xFF880000
                        // Event flags - normal state 0x0001 - button down no move 0x0000, scrolling 0x0004
                        if (record.MouseEvent.dwButtonState > 0 && record.MouseEvent.dwButtonState < 3)
                        {
                            // Button event!
                            mouseKeyAction.LeftButton = ((byte)record.MouseEvent.dwButtonState & 0x01) > 0;
                            mouseKeyAction.RightButton = ((byte)record.MouseEvent.dwButtonState & 0x02) > 0;
                            mouseKeyAction.Alt = ((byte)record.MouseEvent.dwButtonState & 0x02) > 0;
                            mouseKeyAction.Ctrl = ((byte)record.MouseEvent.dwButtonState & 0x08) > 0;
                            mouseKeyAction.Alt = ((byte)record.MouseEvent.dwButtonState & 0x10) > 0;
                            mouseKeyAction.EventType = 2;
                        }

                        mouseKeyAction.xPos = record.MouseEvent.dwMousePosition.X;
                        mouseKeyAction.yPos = record.MouseEvent.dwMousePosition.Y;
                        Console.SetCursorPosition(mouseKeyAction.xPos, mouseKeyAction.yPos);

                        if (mouseKeyAction.LeftButton || mouseKeyAction.RightButton)
                        {
                            waiting = false;
                            iKey = 0;
                        }
                        break;

                    case NativeMethods.KEY_EVENT:
                        mouseKeyAction.keyCode = record.KeyEvent.wVirtualKeyCode;
                        mouseKeyAction.keyState = record.KeyEvent.dwControlKeyState;
                        mouseKeyAction.scanCode = record.KeyEvent.wVirtualScanCode;
                        mouseKeyAction.RtAlt = ((byte)mouseKeyAction.keyState & 0x101) == 0x101;
                        mouseKeyAction.RtCtrl = ((byte)mouseKeyAction.keyState & 0x104) == 0x104;

                        mouseKeyAction.Alt = ((byte)mouseKeyAction.keyState & 0x02) == 0x02 || mouseKeyAction.RtAlt;
                        mouseKeyAction.Ctrl = ((byte)mouseKeyAction.keyState & 0x08) > 0x08 || mouseKeyAction.RtCtrl;
                        mouseKeyAction.Shift = ((byte)mouseKeyAction.keyState & 0x10) == 0x10;
                        mouseKeyAction.KeyPad = false;
                        mouseKeyAction.EventType = 1;

                        // Keydown and KeyUp fire the events.  Only listen if key is down
                        if (mouseKeyAction.keyCode != 0)
                        {
                            string keycode = string.Format("{0}.{1}", mouseKeyAction.keyCode, mouseKeyAction.scanCode);
                            waiting = false;

                            // Is it Alt+A-Z or Alt+F1-12?
                            if (mouseKeyAction.Alt && !mouseKeyAction.Shift && !mouseKeyAction.Ctrl)
                            {
                                if (mouseKeyAction.keyCode != 18)
                                    waiting = false;

                                if (JAXLib.Between(mouseKeyAction.keyCode, 65, 90))
                                {
                                    // A - Z
                                    iKey = -(mouseKeyAction.keyCode - 64);
                                }
                                else
                                {
                                    // F1 - F12
                                    iKey = mouseKeyAction.keyCode switch
                                    {
                                        112 => -101,
                                        113 => -102,
                                        114 => -103,
                                        115 => -104,
                                        116 => -105,
                                        117 => -106,
                                        118 => -107,
                                        119 => -108,
                                        120 => -109,
                                        121 => -110,
                                        122 => -111,
                                        123 => -112,
                                        _ => 0
                                    };
                                }
                            }

                            // Only tested on a Logitec MK120 US keyboard
                            // Is it from the middle keypad area? 
                            if (iKey == 0)
                            {
                                iKey = keycode switch
                                {
                                    "45.82" => 256,     // Insert
                                    "36.71" => 1,       // Home
                                    "33.73" => 18,      // Page Up
                                    "46.83" => 7,       // Del
                                    "35.79" => 6,       // End
                                    "34.81" => 3,       // Page Down
                                    "44.55" => 53,      // Print Screen
                                    "38.72" => 5,       // Up Arrow
                                    "37.75" => 19,      // Left Arrow
                                    "40.80" => 24,      // Down Arrow
                                    "39.77" => 4,       // Right Arrow
                                    "145.70" => (mouseKeyAction.keyState & 0x64) == 0x64 ? 145 : 256 + 145,         // Scroll Lock
                                    "144.69" => (mouseKeyAction.keyState & 0x120) == 0x120 ? 144 : 256 + 144,       // Num Lock
                                    "20.58" => (mouseKeyAction.keyState & 0x80) == 0x80 ? 20 : 256 + 20,            // Caps Lock
                                    "111.53" => 47,     // "/"
                                    "106.55" => 42,     // *
                                    "109.74" => 45,     // -
                                    "107.78" => 43,     // +
                                    "13.28" => 13,      // Enter
                                    "103.71" => 55,     // 7
                                    "104.72" => 56,     // 8
                                    "105.73" => 57,     // 9
                                    "100.75" => 52,     // 4
                                    "101.76" => 53,     // 5
                                    "102.77" => 54,     // 6
                                    "97.79" => 49,      // 1
                                    "98.80" => 50,      // 2
                                    "99.81" => 51,      // 3
                                    "96.82" => 48,      // 0
                                    "110.83" => 46,     // .
                                    _ => 0
                                };

                                if (iKey > 0)
                                {
                                    mouseKeyAction.KeyLabel = keycode switch
                                    {
                                        "45.82" => "{Ins}",             // Insert
                                        "36.71" => "{Home}",            // Home
                                        "33.73" => "{PgUp}",            // Page Up
                                        "46.83" => "{Del}",             // Del
                                        "35.79" => "{End}",             // End
                                        "34.81" => "{PgDn}",            // Page Down
                                        "44.55" => "{PrtScrn}",         // Print Screen
                                        "38.72" => "{Up Arrow}",        // Up Arrow
                                        "37.75" => "{Lft Arrow}",       // Left Arrow
                                        "40.80" => "{Dn Arrow}",        // Down Arrow
                                        "39.77" => "{Rt Arrow}",        // Right Arrow
                                        "145.70" => "{Scroll Lock}",    // Scroll Lock
                                        "144.69" => "{Num Lock}",       // Num Lock
                                        "20.58" => "{Caps Lock}",       // Caps Lock
                                        _ => record.KeyEvent.UnicodeChar > 0 ? record.KeyEvent.UnicodeChar.ToString() : string.Empty
                                    };
                                }
                            }

                            if (iKey == 0)
                            {
                                // Not a special alt key or one of the keypads, so try to figure it out
                                iKey = mouseKeyAction.keyCode switch
                                {
                                    8 => mouseKeyAction.Shift ? 127 : (mouseKeyAction.Ctrl ? 127 : (mouseKeyAction.Alt ? 14 : 127)),    // Backspace
                                    9 => mouseKeyAction.Shift ? 15 : (mouseKeyAction.Ctrl ? 148 : (mouseKeyAction.Alt ? 0 : 9)),        // Tab
                                    13 => mouseKeyAction.Alt ? 166 : (mouseKeyAction.Ctrl ? 10 : 13),                                   // Enter
                                    27 => mouseKeyAction.Shift ? 50 : (mouseKeyAction.Ctrl ? 27 : (mouseKeyAction.Alt ? 1 : 27)),       // Esc
                                    32 => mouseKeyAction.Shift ? 32 : (mouseKeyAction.Ctrl ? 32 : (mouseKeyAction.Alt ? 57 : 32)),      // Spacebar
                                    48 => mouseKeyAction.Shift ? 41 : 48,                                                               // 0
                                    49 => mouseKeyAction.Shift ? 33 : 49,                                                               // 1
                                    50 => mouseKeyAction.Shift ? 64 : 50,                                                               // 2
                                    51 => mouseKeyAction.Shift ? 35 : 51,                                                               // 3
                                    52 => mouseKeyAction.Shift ? 36 : 52,                                                               // 4
                                    53 => mouseKeyAction.Shift ? 37 : 53,                                                               // 5
                                    54 => mouseKeyAction.Shift ? 94 : 54,                                                               // 6
                                    55 => mouseKeyAction.Shift ? 38 : 55,                                                               // 7
                                    56 => mouseKeyAction.Shift ? 42 : 56,                                                               // 8
                                    57 => mouseKeyAction.Shift ? 40 : 57,                                                               // 9
                                    112 => mouseKeyAction.Shift ? 84 : (mouseKeyAction.Ctrl ? 94 : (mouseKeyAction.Alt ? 104 : 28)),    // F1
                                    113 => mouseKeyAction.Shift ? 86 : (mouseKeyAction.Ctrl ? 95 : (mouseKeyAction.Alt ? 104 : -1)),    // F2
                                    114 => mouseKeyAction.Shift ? 87 : (mouseKeyAction.Ctrl ? 96 : (mouseKeyAction.Alt ? 105 : -2)),    // F3
                                    115 => mouseKeyAction.Shift ? 87 : (mouseKeyAction.Ctrl ? 97 : (mouseKeyAction.Alt ? 106 : -3)),    // F4
                                    116 => mouseKeyAction.Shift ? 88 : (mouseKeyAction.Ctrl ? 98 : (mouseKeyAction.Alt ? 107 : -4)),    // F5
                                    117 => mouseKeyAction.Shift ? 89 : (mouseKeyAction.Ctrl ? 99 : (mouseKeyAction.Alt ? 108 : -5)),    // F6
                                    118 => mouseKeyAction.Shift ? 90 : (mouseKeyAction.Ctrl ? 100 : (mouseKeyAction.Alt ? 109 : -6)),   // F7
                                    119 => mouseKeyAction.Shift ? 91 : (mouseKeyAction.Ctrl ? 101 : (mouseKeyAction.Alt ? 110 : -7)),   // F8
                                    120 => mouseKeyAction.Shift ? 92 : (mouseKeyAction.Ctrl ? 102 : (mouseKeyAction.Alt ? 111 : -8)),   // F9
                                    121 => mouseKeyAction.Shift ? 93 : (mouseKeyAction.Ctrl ? 103 : (mouseKeyAction.Alt ? 112 : -9)),   // F10
                                    122 => mouseKeyAction.Shift ? 135 : (mouseKeyAction.Ctrl ? 137 : (mouseKeyAction.Alt ? 139 : 133)), // F11
                                    123 => mouseKeyAction.Shift ? 136 : (mouseKeyAction.Ctrl ? 138 : (mouseKeyAction.Alt ? 140 : 134)), // F12
                                    186 => mouseKeyAction.Shift ? 59 : 58,                                                              // ;
                                    187 => mouseKeyAction.Shift ? 61 : 43,                                                              // =
                                    188 => mouseKeyAction.Shift ? 44 : 60,                                                              // ,
                                    189 => mouseKeyAction.Shift ? 45 : 95,                                                              // _
                                    190 => mouseKeyAction.Shift ? 46 : 62,                                                              // .
                                    191 => mouseKeyAction.Shift ? 47 : 63,                                                              // /
                                    192 => mouseKeyAction.Shift ? 96 : 126,                                                             // `
                                    222 => mouseKeyAction.Shift ? 39 : 34,                                                              // '
                                    219 => mouseKeyAction.Shift ? 91 : 123,                                                             // [
                                    220 => mouseKeyAction.Shift ? 92 : 124,                                                             // \
                                    221 => mouseKeyAction.Shift ? 93 : 125,                                                             // ]
                                    _ => 0 // did not translate
                                };

                                if (iKey > 0)
                                {
                                    mouseKeyAction.KeyLabel = mouseKeyAction.keyCode switch
                                    {
                                        8 => "{Backspace}",
                                        9 => "{Tab}",
                                        13 => "{Enter}",
                                        27 => "{Esc}",
                                        32 => "{Space}",
                                        112 => mouseKeyAction.Shift ? "{Shift+F1}" : (mouseKeyAction.Ctrl ? "{Ctrl+F1}" : (mouseKeyAction.Alt ? "{Alt+F1}" : "{F1}")),
                                        113 => mouseKeyAction.Shift ? "{Shift+F2}" : (mouseKeyAction.Ctrl ? "{Ctrl+F2}" : (mouseKeyAction.Alt ? "{Alt+F2}" : "{F2}")),
                                        114 => mouseKeyAction.Shift ? "{Shift+F3}" : (mouseKeyAction.Ctrl ? "{Ctrl+F3}" : (mouseKeyAction.Alt ? "{Alt+F3}" : "{F3}")),
                                        115 => mouseKeyAction.Shift ? "{Shift+F4}" : (mouseKeyAction.Ctrl ? "{Ctrl+F4}" : (mouseKeyAction.Alt ? "{Alt+F4}" : "{F4}")),
                                        116 => mouseKeyAction.Shift ? "{Shift+F5}" : (mouseKeyAction.Ctrl ? "{Ctrl+F5}" : (mouseKeyAction.Alt ? "{Alt+F5}" : "{F5}")),
                                        117 => mouseKeyAction.Shift ? "{Shift+F6}" : (mouseKeyAction.Ctrl ? "{Ctrl+F6}" : (mouseKeyAction.Alt ? "{Alt+F6}" : "{F6}")),
                                        118 => mouseKeyAction.Shift ? "{Shift+F7}" : (mouseKeyAction.Ctrl ? "{Ctrl+F7}" : (mouseKeyAction.Alt ? "{Alt+F7}" : "{F7}")),
                                        119 => mouseKeyAction.Shift ? "{Shift+F8}" : (mouseKeyAction.Ctrl ? "{Ctrl+F8}" : (mouseKeyAction.Alt ? "{Alt+F8}" : "{F8}")),
                                        120 => mouseKeyAction.Shift ? "{Shift+F9}" : (mouseKeyAction.Ctrl ? "{Ctrl+F9}" : (mouseKeyAction.Alt ? "{Alt+F9}" : "{F9}")),
                                        121 => mouseKeyAction.Shift ? "{Shift+F10}" : (mouseKeyAction.Ctrl ? "{Ctrl+F10}" : (mouseKeyAction.Alt ? "{Alt+F10}" : "{F10}")),
                                        122 => mouseKeyAction.Shift ? "{Shift+F11}" : (mouseKeyAction.Ctrl ? "{Ctrl+F11}" : (mouseKeyAction.Alt ? "{Alt+F11}" : "{F11}")),
                                        123 => mouseKeyAction.Shift ? "{Shift+F12}" : (mouseKeyAction.Ctrl ? "{Ctrl+F12}" : (mouseKeyAction.Alt ? "{Alt+F12}" : "{F12}")),
                                        _ => record.KeyEvent.UnicodeChar > 0 ? record.KeyEvent.UnicodeChar.ToString() : string.Empty
                                    };
                                }

                                // Perhaps it's not a special key?
                                if (iKey == 0 && JAXLib.Between(mouseKeyAction.keyCode, 32, 127))
                                {
                                    string akey = mouseKeyAction.Shift ? ((char)mouseKeyAction.keyCode).ToString().ToUpper() : ((char)mouseKeyAction.keyCode).ToString().ToLower();

                                    if (mouseKeyAction.Shift || mouseKeyAction.CapsLock)
                                        iKey = akey.ToUpper()[0];
                                    else
                                        iKey = akey.ToLower()[0];
                                }
                            }
                        }

                        // If a key up event occured, mark it with a -999
                        if (record.KeyEvent.bKeyDown == false) iKey = -9999;

                        waiting = iKey == 0;
                        break;
                }

            }
            return iKey;
        }

        private class NativeMethods
        {

            public const Int32 STD_INPUT_HANDLE = -10;

            public const Int32 ENABLE_MOUSE_INPUT = 0x0010;
            public const Int32 ENABLE_QUICK_EDIT_MODE = 0x0040;
            public const Int32 ENABLE_EXTENDED_FLAGS = 0x0080;

            public const Int32 KEY_EVENT = 1;
            public const Int32 MOUSE_EVENT = 2;


            [DebuggerDisplay("EventType: {EventType}")]
            [StructLayout(LayoutKind.Explicit)]
            public struct INPUT_RECORD
            {
                [FieldOffset(0)]
                public Int16 EventType;
                [FieldOffset(4)]
                public KEY_EVENT_RECORD KeyEvent;
                [FieldOffset(4)]
                public MOUSE_EVENT_RECORD MouseEvent;
            }

            [DebuggerDisplay("{dwMousePosition.X}, {dwMousePosition.Y}")]
            public struct MOUSE_EVENT_RECORD
            {
                public COORD dwMousePosition;
                public Int32 dwButtonState;
                public Int32 dwControlKeyState;
                public Int32 dwEventFlags;
            }

            [DebuggerDisplay("{X}, {Y}")]
            public struct COORD
            {
                public UInt16 X;
                public UInt16 Y;
            }

            [DebuggerDisplay("KeyCode: {wVirtualKeyCode}")]
            [StructLayout(LayoutKind.Explicit)]
            public struct KEY_EVENT_RECORD
            {
                [FieldOffset(0)]
                [MarshalAsAttribute(UnmanagedType.Bool)]
                public Boolean bKeyDown;
                [FieldOffset(4)]
                public UInt16 wRepeatCount;
                [FieldOffset(6)]
                public UInt16 wVirtualKeyCode;
                [FieldOffset(8)]
                public UInt16 wVirtualScanCode;
                [FieldOffset(10)]
                public Char UnicodeChar;
                [FieldOffset(10)]
                public Byte AsciiChar;
                [FieldOffset(12)]
                public Int32 dwControlKeyState;
            };


            public class ConsoleHandle : SafeHandleMinusOneIsInvalid
            {
                public ConsoleHandle() : base(false) { }

                protected override bool ReleaseHandle()
                {
                    return true; //releasing console handle is not our business
                }
            }


            [DllImportAttribute("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean GetConsoleMode(ConsoleHandle hConsoleHandle, ref Int32 lpMode);

            [DllImportAttribute("kernel32.dll", SetLastError = true)]
            public static extern ConsoleHandle GetStdHandle(Int32 nStdHandle);

            [DllImportAttribute("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean ReadConsoleInput(ConsoleHandle hConsoleInput, ref INPUT_RECORD lpBuffer, UInt32 nLength, ref UInt32 lpNumberOfEventsRead);

            [DllImportAttribute("kernel32.dll", SetLastError = true)]
            [return: MarshalAsAttribute(UnmanagedType.Bool)]
            public static extern Boolean SetConsoleMode(ConsoleHandle hConsoleHandle, Int32 dwMode);
        }
    }
}
