using Avalonia.Input;
using JAXBase.Language;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Core
{
    public static class AppIO
    {
        // Set talk ON/OFF will control when the TALK works
        // Send to the default console
        public static void Talk(string text)
        {
            // Send to the IDE Screen
            if (Program.CurrentApp.CurrentDS.JaxSettings.Talk)
                SendToIDE(System.Environment.NewLine + text);

            AppIO.DebugLog("TALK: " + text, true); // Always write to the log file
        }

        public static void SendToIDE(string text)
        {
            // Write results to screen
            JAXApp.MainWindowInstance?.AppendMainOutput(text);

            if (Program.CurrentApp.CurrentDS.JaxSettings.Alternate && !string.IsNullOrWhiteSpace(Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name))
            {
                JAXLib.StrToFile(text, Program.CurrentApp.CurrentDS.JaxSettings.Alternate_Name, 3);
            }
        }

        // Set debug ON/OFF will control when the debug works
        // Send to the default console
        public static void DebugLog(string text) { AppIO.DebugLog(text, true); }


        public static void DebugLog(string text, bool writeToFileOnly)
        {
            if (Program.CurrentApp.CurrentDS.JaxSettings.Debug)
            {
                string debugText = DateTime.Now.ToString("MM/dd HH:mm:ss.ffff").PadRight(20) + text;
                JAXLib.StrToFile(debugText, Program.CurrentApp.AppLogFile, 3);

                if (writeToFileOnly == false)
                {
                    // Write to mainwindow if available, otherwise ignore
                    //if (JAXConsoles["default"].active)
                    //    JAXConsoles["default"].WriteLine(text);
                }
            }
        }


        public static async Task ShowDialogAsync(Avalonia.Controls.Window dialog, Avalonia.Controls.Window owner)
        {
            Interlocked.Increment(ref Program.CurrentApp._openDialogCount);
            try
            {
                await dialog.ShowDialog(owner);
            }
            finally
            {
                Interlocked.Decrement(ref Program.CurrentApp._openDialogCount);
            }
        }

        public static async Task<T?> ShowDialogAsync<T>(Avalonia.Controls.Window dialog, Avalonia.Controls.Window owner)
        {
            Interlocked.Increment(ref Program.CurrentApp._openDialogCount);  // Or use lock if preferred
            try
            {
                return await dialog.ShowDialog<T>(owner);
            }
            finally
            {
                Interlocked.Decrement(ref Program.CurrentApp._openDialogCount);
            }
        }


        // MainWindow properties save and restore
        private static AppSettings? _currentSettings;

        public static void LoadWindowSettings()
        {
            if (JAXApp.MainWindowInstance == null) return;

            _currentSettings = SettingsService.Load();

            if (_currentSettings.Monitor > 0 && _currentSettings.Monitor <= MonitorLib.GetAvailableMonitorCount())
                Program.CurrentApp._screen!.SetProperty("monitor", _currentSettings.Monitor, 0).Wait();

            // Restore size & position
            if (_currentSettings.WindowWidth > 100)
                Program.CurrentApp._screen!.SetProperty("width", _currentSettings.WindowWidth, 0).Wait();

            if (_currentSettings.WindowHeight > 100)
                Program.CurrentApp._screen!.SetProperty("height", _currentSettings.WindowHeight, 0).Wait();

            if (_currentSettings.WindowLeft >= 0 && _currentSettings.WindowTop >= 0)
            {
                JAXApp.MainWindowInstance.WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.Manual;
                Program.CurrentApp._screen!.SetProperty("left", (int)_currentSettings.WindowLeft, 0).Wait();
                Program.CurrentApp._screen.SetProperty("top", (int)_currentSettings.WindowTop, 0).Wait();
            }

            // Restore icon (if you saved the name/path)
            if (!string.IsNullOrEmpty(_currentSettings.IconName))
                Program.CurrentApp._screen!.SetProperty("icon", _currentSettings.IconName, 0).Wait();
            else
                Program.CurrentApp._screen!.SetProperty("icon", "*jax*", 0).Wait();
        }

        public static void SaveWindowSettings()
        {
            if (JAXApp.MainWindowInstance == null) return;

            _currentSettings ??= new AppSettings();

            _currentSettings.WindowWidth = JAXApp.MainWindowInstance.Width;
            _currentSettings.WindowHeight = JAXApp.MainWindowInstance.Height;
            _currentSettings.WindowLeft = JAXApp.MainWindowInstance.Position.X;
            _currentSettings.WindowTop = JAXApp.MainWindowInstance.Position.Y;

            // Save current icon name if you want
            _currentSettings.IconName = Program.CurrentApp._screen!.thisObject!.UserProperties["icon"].AsString();

            SettingsService.Save(_currentSettings);
        }


        /*-----------------------------------------------------------*
         * Fix directory strings containing %% variables
         * 
         * TODO - Test this in Linux ASAP
         *-----------------------------------------------------------*/
        public static string FixDirectory(string dir)
        {
            dir = dir.Replace("%userprofile%", Program.CurrentApp.UserFolder, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%temp%", Program.CurrentApp.AppTempFolder, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%app%", Program.CurrentApp.AppBaseFolder, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%work%", Program.CurrentApp.AppWorkFolder, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%exe%", Program.CurrentApp.ExeFolder, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%allusersprofile%", Program.CurrentApp.JaxVariables._AppPath, StringComparison.OrdinalIgnoreCase);
            dir = dir.Replace("%programdata%", Program.CurrentApp.JaxVariables._AppPath, StringComparison.OrdinalIgnoreCase);

            return dir;
        }


        public static KeyClass KeyLabel(string key)
        {
            KeyClass result = new();

            key = key.ToUpper();
            string simpleKeys = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ`~!@#$%^&*()-_=+[{]}\\|;:'\",<.>/?";
            bool ALT = false;
            bool CTRL = false;
            bool SHIFT = false;

            int loop = 0;
            while (true)
            {
                key = key.Trim();

                if (key.Length > 3 && key[..3].Equals("ALT") && ALT == false)
                {
                    result.ALT = true;
                    key = key[3..].Trim();
                }
                else if (key.Length > 4 && key[..4].Equals("CTRL") && CTRL == false)
                {
                    result.CTRL = true;
                    key = key[4..].Trim();
                }
                else if (key.Length > 5 && key[..5].Equals("SHIFT") && SHIFT == false)
                {
                    result.SHIFT = true;
                    key = key[5..].Trim();
                }
                else if (key.Length > 1 && key[..2] != "++" && key[..1].Equals("+") && loop > 0)
                {
                    // Just eat the + sign
                    key = key[..1].Trim();
                }
                else
                    break;

                loop++;
            }

            // Now process the keys
            if (key.Length == 1 && simpleKeys.Contains(key))
            {
                // Simple key handler
                int intkey = (int)(key[0]);

                if (SHIFT || (CTRL && ALT))
                    throw new Exception("10|");

                result.aKey = (Avalonia.Input.Key)intkey;
                result.iKey = intkey;
                result.keyLabel = key;
                if ((SHIFT && ALT) || (SHIFT && CTRL) || (CTRL && ALT)) throw new Exception("10|");
            }
            else
            {
                int splkey = JAXLanguageLists.SpecialKeys.IndexOf(key) + 1;
                result.keyLabel = key;

                if (splkey > 0)
                {
                    if (splkey < 10)
                    {
                        switch (splkey)
                        {
                            case 0:
                                result.iKey = 9;
                                result.aKey = Avalonia.Input.Key.Tab;
                                if (CTRL) throw new Exception("10|");
                                break;
                            case 1:
                                result.iKey = 15;
                                result.aKey = Avalonia.Input.Key.Tab;
                                if (CTRL) throw new Exception("10|");
                                break;
                            case 2:
                                result.iKey = 9;
                                result.aKey = (Avalonia.Input.Key)'{';
                                if (CTRL || ALT || SHIFT) throw new Exception("10|");
                                break;
                            case 3:
                                result.iKey = 15;
                                result.aKey = (Avalonia.Input.Key)'}';
                                if (CTRL || ALT || SHIFT) throw new Exception("10|");
                                break;
                            case 4:
                                result.iKey = 13;
                                result.aKey = Avalonia.Input.Key.Enter;
                                if (ALT || SHIFT) throw new Exception("10|");
                                break;
                            case 5:
                                result.iKey = 33;
                                result.aKey = Avalonia.Input.Key.Space;
                                if (ALT || SHIFT) throw new Exception("10|");
                                break;
                            case 6:
                                result.iKey = 27;
                                result.aKey = Avalonia.Input.Key.Escape;
                                if (CTRL || ALT || SHIFT) throw new Exception("10|");
                                break;
                            case 7:
                                result.iKey = 7;
                                result.aKey = Avalonia.Input.Key.Delete;
                                if (SHIFT) throw new Exception("10|");
                                break;
                        }
                    }
                    else
                    {
                        switch (key)
                        {
                            case "F1":
                                result.iKey = 28;
                                result.aKey = Avalonia.Input.Key.F1;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F2":
                                result.iKey = -1;
                                result.aKey = Avalonia.Input.Key.F2;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F3":
                                result.iKey = -2;
                                result.aKey = Avalonia.Input.Key.F3;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F4":
                                result.iKey = -3;
                                result.aKey = Avalonia.Input.Key.F4;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F5":
                                result.iKey = -4;
                                result.aKey = Avalonia.Input.Key.F5;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F6":
                                result.iKey = -5;
                                result.aKey = Avalonia.Input.Key.F6;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F7":
                                result.iKey = -6;
                                result.aKey = Avalonia.Input.Key.F7;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F8":
                                result.iKey = -7;
                                result.aKey = Avalonia.Input.Key.F8;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F9":
                                result.iKey = -8;
                                result.aKey = Avalonia.Input.Key.F9;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F10":
                                result.iKey = -9;
                                result.aKey = Avalonia.Input.Key.F10;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F11":
                                result.iKey = 133;
                                result.aKey = Avalonia.Input.Key.F11;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "F12":
                                result.iKey = 134;
                                result.aKey = Avalonia.Input.Key.F12;
                                if ((SHIFT && CTRL) || (SHIFT && ALT) || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "LEFTARROW":
                                result.iKey = 19;
                                result.aKey = Avalonia.Input.Key.Left;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "RIGHTARROW":
                                result.iKey = 4;
                                result.aKey = Avalonia.Input.Key.Right;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "UPARROW":
                                result.iKey = 5;
                                result.aKey = Avalonia.Input.Key.Up;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "DOWNARROW":
                                result.iKey = 24;
                                result.aKey = Avalonia.Input.Key.Down;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "HOME":
                                result.iKey = 1;
                                result.aKey = Avalonia.Input.Key.Home;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "END":
                                result.iKey = 6;
                                result.aKey = Avalonia.Input.Key.End;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "PGUP":
                                result.iKey = 18;
                                result.aKey = Avalonia.Input.Key.PageUp;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "PGDN":
                                result.iKey = 3;
                                result.aKey = Avalonia.Input.Key.PageDown;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "INS":
                                result.iKey = 22;
                                result.aKey = Avalonia.Input.Key.Insert;
                                if (SHIFT || (CTRL && ALT)) throw new Exception("10|");
                                break;
                            case "BACKSPACE":
                                result.iKey = 127;
                                result.aKey = Avalonia.Input.Key.Back;
                                if (SHIFT || ALT) throw new Exception("10|");
                                break;
                            case "LEFTMOUSE":
                                result.iKey = 251;
                                if (SHIFT || ALT || CTRL) throw new Exception("10|");
                                break;
                            case "RIGHTMOUSE":
                                result.iKey = 252;
                                if (SHIFT || ALT || CTRL) throw new Exception("10|");
                                break;
                            case "MIDDLEMOUSE":
                                result.iKey = 253;
                                if (SHIFT || ALT || CTRL) throw new Exception("10|");
                                break;
                            case "MOUSE":
                                result.iKey = 255;
                                if (SHIFT || ALT || CTRL) throw new Exception("10|");
                                break;
                        }
                    }
                }
                else
                    throw new Exception("10|");
            }

            return result;
        }


        // This handles updating all registered windows with the
        // current changes for the on key label handler
        //
        // TODO - Need to get onto Window handling and registration
        public static void SetOnKeyLabel(string keylabel, bool remove)
        {
            KeyClass key = AppIO.KeyLabel(keylabel);

            if (key.aKey != Key.None && key.iKey != 0)
            {
                KeyModifiers km;

                if (key.CTRL && key.ALT && key.SHIFT)
                    km = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt;
                else if (key.CTRL && key.ALT)
                    km = KeyModifiers.Control | KeyModifiers.Alt;
                else if (key.CTRL && key.SHIFT)
                    km = KeyModifiers.Control | KeyModifiers.Shift;
                else if (key.SHIFT && key.ALT)
                    km = KeyModifiers.Shift | KeyModifiers.Alt;
                else if (key.SHIFT)
                    km = KeyModifiers.Shift;
                else if (key.ALT)
                    km = KeyModifiers.Alt;
                else if (key.CTRL)
                    km = KeyModifiers.Control;
                else
                    km = KeyModifiers.None;

                var kg = new KeyGesture(key.aKey, km);

                if (remove)
                {
                    // Remove this ON KEY LABEL
                    if (JAXApp.MainWindowInstance is not null)
                    {

                        for (int i = JAXApp.MainWindowInstance.KeyBindings.Count - 1; i >= 0; i--)
                        {
                            var kb = JAXApp.MainWindowInstance.KeyBindings[i];
                            if (kb.Gesture?.Equals(kg) == true)
                                JAXApp.MainWindowInstance.KeyBindings.RemoveAt(i);
                        }
                    }
                }
                else if (key.iKey > 250)
                {
                    // TODO - INVESTIGATE MOUSE KEY EMULATION
                    //        Might just remove them
                }
                else
                {
                    // Add this  ON KEY LABEL to the main window
                    JAXApp.MainWindowInstance?.KeyBindings.Add(new KeyBinding
                    {
                        CommandParameter = key.keyLabel,
                        Gesture = kg,
                        Command = ReactiveUI.ReactiveCommand.Create<object>(param => { ONKeyExecute(param); })
                    });
                }
            }
        }

        public static void ONKeyExecute(object? keyLabel)
        {
            int iii = 0;
        }
    }
}
