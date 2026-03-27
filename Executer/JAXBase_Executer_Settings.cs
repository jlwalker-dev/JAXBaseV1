using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    /*
     *     eCodes.SUBCMD contains set command (alternate, bell, etc)
     *     eCodes.To contains the TO expression
     *     eCodes.Expression contains the ON/OFF expression
     */
    public class JAXBase_Executer_Settings
    {
        public static async Task<string> Settings(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string status = string.Empty;

            JAXObjects.Token answer;
            string settingName = eCodes.SUBCMD;
            string Expression;
            JAXObjects.Token ToExpression = new();
            bool HasOnOff = false;
            bool HasOther = false;
            bool IsOn = false;
            int ds;
            int wa;
            int nds;

            try
            {
                string settingValue = string.Empty;
                ToExpression.Element.Value = string.Empty;

                // This method is used to get or set settings for the JAXBase executer.
                // If settingValue is provided, it sets the value; otherwise, it retrieves the current value.
                // For simplicity, we will just return a formatted string.
                if (string.IsNullOrEmpty(settingName) == false)
                {
                    status = $"Setting {settingName.ToUpper()} ";

                    // ON/OFF
                    if (eCodes.Expressions.Count > 0)
                    {
                        if (eCodes.Expressions.Count > 1) throw new Exception("10|");

                        if (string.IsNullOrWhiteSpace(eCodes.Expressions[0].RNPExpr) == false)
                        {
                            answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                            if (answer.Element.Type.Equals("C"))
                            {
                                Expression = answer.AsString();
                                if (JAXLib.InList(Expression.ToLower(), "on", "off"))
                                {
                                    HasOnOff = true;
                                    IsOn = Expression.Equals("ON", StringComparison.OrdinalIgnoreCase);
                                }
                                else
                                {
                                    // Is there something other than ON/OFF?
                                    HasOther = string.IsNullOrWhiteSpace(answer.AsString()) == false;
                                }
                            }
                            else
                                throw new Exception("11|");
                        }
                    }

                    if (eCodes.To.Count > 0)
                    {
                        if (eCodes.To.Count > 1) throw new Exception("10|");

                        if (string.IsNullOrWhiteSpace(eCodes.To[0].Name) == false)
                            ToExpression = await jbe.App.SolveFromRPNString(eCodes.To[0].Name);
                    }

                    // SET the setting's value
                    switch (settingName.ToLower())
                    {
                        // -------------------------------------
                        // ON/OFF Settings
                        // -------------------------------------
                        case "aiagent":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.AIAgent = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");

                            // If OFF, then all AI classses will disconnect the next
                            // time there is any communiction/connection activity
                            // or when the watchdog timer fires.
                            break;

                        case "ansi":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.ANSI = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "asserts":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Asserts = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "autoincerror":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.AutoIncError = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "autosave":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.AutoSave = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "confirm":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Confirm = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "cpdialog":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.CP_Dialog = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "cursor":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Cursor = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "debug":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Debug = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "deleted":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Deleted = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "development":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Development = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "echo":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Echo = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "exact":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Exact = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "exclusive":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Exclusive = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "fixed":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Fixed = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "fullpath":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.FullPath = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "headings":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Headings = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "includesource":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.IncludeSource = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "lock":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Lock = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "logerrors":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.LogErrors = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "multilocks":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.MultiLocks = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "near":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Near = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "null":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Null = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "readborder":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Readborder = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "safety":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Safety = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "seconds":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Seconds = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "space":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Space = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "sqlbuffering":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.SQLBuffering = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "status":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Status = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "step":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Step = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");

                            //if (jbe.App.CurrentDS.JaxSettings.Step == false && jbe.App.JaxDebugger is not null)
                            //{
                            //    // Shut down the debugger and resume execution of the program
                            //    jbe.App.JaxDebugger.EndDebugging();
                            //    jbe.App.JaxDebugger = null;
                            //}
                            break;

                        case "sysformats":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.SysFormats = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "tableprompt":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.TablePrompt = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "trbetween":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.TRBetween = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "unique":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Unique = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        case "varcharmapping":
                            if (HasOnOff == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.VarCharMapping = IsOn;
                            status += "is " + (IsOn ? "ON" : "OFF");
                            break;

                        // -------------------------------------
                        // INT Values
                        // -------------------------------------
                        case "blocksize":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.BlockSize = JAXLib.Between(ToExpression.AsInt(), 1, 1024) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.BlockSize.ToString();
                            break;

                        case "datasession":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.DataSession = ToExpression.AsInt() > 0 ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.DataSession.ToString();
                            break;

                        case "fdow":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.FDOW = JAXLib.Between(ToExpression.AsInt(), 1, 7) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.FDOW.ToString();
                            break;

                        case "fweek":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.FWeek = JAXLib.Between(ToExpression.AsInt(), 1, 3) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.FWeek.ToString();
                            break;

                        case "hours":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Hours = JAXLib.InList(ToExpression.AsInt(), 12, 24) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.Hours.ToString();
                            break;

                        case "memowidth":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.MemoWidth = JAXLib.Between(ToExpression.AsDouble(), 0D, 8000D) ? (uint)ToExpression.AsDouble() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.MemoWidth.ToString();
                            break;

                        case "odometer":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Odometer = JAXLib.Between(ToExpression.AsInt(), 1, 10000) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.Odometer.ToString();
                            break;

                        case "strictdate":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.StrictDate = JAXLib.Between(ToExpression.AsInt(), 0, 2) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.StrictDate.ToString();
                            break;

                        case "tablevalidate":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.TableValidate = JAXLib.Between(ToExpression.AsInt(), 1, 15) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.TableValidate.ToString();
                            break;

                        case "topicid":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.Topic_ID = ToExpression.AsInt() > 0 ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.Topic_ID.ToString();
                            break;

                        case "typeahead":
                            if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");
                            jbe.App.CurrentDS.JaxSettings.TypeAhead = JAXLib.Between(ToExpression.AsInt(), 0, 255) ? ToExpression.AsInt() : throw new Exception("3003|");
                            status += "is " + jbe.App.CurrentDS.JaxSettings.TypeAhead.ToString();
                            break;


                        // ------------------------------------------------------------------------------------------------------------
                        // The following settings are more complex 
                        // ------------------------------------------------------------------------------------------------------------
                        case "alternate":       // ON/OFF | TO [FileName [ADDITIVE]]

                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Alternate = IsOn;
                            }

                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Alternate = false;      // Ending all output to the current file and autosetting to off

                                jbe.App.CurrentDS.JaxSettings.Alternate_Name = settingValue.Trim();
                            }
                            break;

                        case "bell":            // ON/OFF or TO cWaveFileName|cMP3FileName
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Bell = IsOn;
                            }

                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Bell = false;      // Ending all output to the current file and autosetting to off

                                jbe.App.CurrentDS.JaxSettings.Bell_Name = settingValue.Trim();
                            }
                            break;


                        case "carry":           // ON/OFF | TO [FieldList [ADDITIVE]]
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Carry = IsOn;
                            }

                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Carry = false;      // Ending all output to the current file and autosetting to off

                                jbe.App.CurrentDS.JaxSettings.Carry_Name = settingValue.Trim();
                            }
                            break;

                        case "debugout":        // TO [FileName [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "library":         // TO [FileName [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "path":            // TO [Path] [ADDITIVE]]  
                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Path = string.Empty;
                                else
                                {
                                    if (eCodes.Flags.Length == 0 || Array.IndexOf(eCodes.Flags, "additive") < 0)
                                    {
                                        // "additive" flag not included, so overwrite the file
                                        jbe.App.CurrentDS.JaxSettings.Path = settingValue;
                                    }
                                    else
                                        jbe.App.CurrentDS.JaxSettings.Path += (";" + settingValue.Trim()).Trim(';');

                                    jbe.App.Talk("Path is " + jbe.App.CurrentDS.JaxSettings.Path);
                                }
                            }
                            break;

                        case "century":         // ON/OFF | TO [nCentury [ROLLOVER nYear]]
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Century = IsOn;
                            }

                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("N") == false) throw new Exception("11|");

                                jbe.App.CurrentDS.JaxSettings.Century_Current = JAXLib.Between(ToExpression.AsInt(), 0, 99) ? ToExpression.AsInt() : throw new Exception("3031|");      // Ending all output to the current file and autosetting to off
                            }

                            break;

                        case "classlib":        // TO ClassLibraryName [IN APPFileName | EXEFileName] [ADDITIVE][ALIAS AliasName]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "clock":           // ON | OFF | STATUS    -or -  TO[nRow, nColumn]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "coverage":        // ON/OFF | TO [FileName][ADDITIVE]
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                                jbe.App.CurrentDS.JaxSettings.Coverage = IsOn;


                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Coverage = false;      // Ending all output to the current file and autosetting to off

                                if (eCodes.Flags.Length == 0 || Array.IndexOf(eCodes.Flags, "additive") < 0)
                                {
                                    // "additive" flag not included, so overwrite the file
                                    JAXLib.StrToFile("", settingValue.Trim(), 0);
                                }

                                jbe.App.CurrentDS.JaxSettings.Coverage_Name = settingValue.Trim();
                            }
                            break;

                        case "console":         // [name] [SIZE <nCols>,<nRows> | FULL] [AT <nLeft>,<nTop>] [ON | OFF | ACTIVE | INACTIVE] 
                            string consoleName = eCodes.NAME.ToLower().Trim();
                            consoleName = string.IsNullOrWhiteSpace(consoleName) ? "default" : consoleName;

                            status += consoleName.ToUpper() + " to ";

                            //if (HasOnOff)
                            //    jbe.App.JAXConsoles[consoleName].Visible(IsOn);

                            if (string.IsNullOrWhiteSpace(settingValue) == false)
                            {
                                switch (settingValue.ToLower())
                                {
                                    case "inactive":
                                        status += settingValue.ToUpper();
                                        //jbe.App.JAXConsoles[consoleName].Active(false);
                                        break;

                                    default:
                                        status += "";
                                        //jbe.App.JAXConsoles[consoleName].Active(true);
                                        break;
                                }
                            }

                            break;

                        case "default":
                            if (ToExpression.Element.Type.Equals("C"))
                            {
                                if (string.IsNullOrWhiteSpace(ToExpression.AsString()) == false)
                                {
                                    // Does the path exist?
                                    if (Directory.Exists(ToExpression.AsString()))
                                    {
                                        // Set the default path
                                        jbe.App.CurrentDS.JaxSettings.Default = JAXLib.Addbs(ToExpression.AsString());
                                        status += "directory to " + jbe.App.CurrentDS.JaxSettings.Default;
                                    }
                                    else
                                        throw new Exception("202|" + ToExpression.AsString());
                                }
                            }
                            else
                                throw new Exception("11|");
                            break;


                        case "device":          // TO SCREEN | TO PRINTER [PROMPT] | TO FILE FileName
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "eventlist":       // TO [EventName1 [, EventName2 ...] [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "eventtracking":   // ON/OFF/PROMPT | TO [FileName [ADDITIVE]]
                            if (Array.IndexOf(eCodes.Flags, "on") < 0 || Array.IndexOf(eCodes.Flags, "off") < 0 || eCodes.Flags.Length == 0) throw new Exception("11|");
                            if (Array.IndexOf(eCodes.Flags, "on") >= 0)
                                jbe.App.CurrentDS.JaxSettings.Coverage = true;
                            else if (Array.IndexOf(eCodes.Flags, "off") >= 0 || (eCodes.Flags.Length == 0 && eCodes.To.Count == 0))
                                jbe.App.CurrentDS.JaxSettings.Coverage = false;

                            if (Array.IndexOf(eCodes.Flags, "prompt") >= 0)
                            {
                                // TODO
                            }

                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Coverage = false;      // Ending all output to the current file and autosetting to off

                                if (eCodes.Flags.Length == 0 || Array.IndexOf(eCodes.Flags, "additive") < 0)
                                {
                                    // "additive" flag not included, so overwrite the file
                                    JAXLib.StrToFile("", settingValue.Trim(), 0);
                                }

                                jbe.App.CurrentDS.JaxSettings.Coverage_Name = settingValue.Trim();
                            }
                            break;

                        case "filter":          // ON|OFF|TO [lExpression] [IN nWorkArea | cTableAlias]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "function":        // nFunctionKeyNumber | KeyLabelName TO [eExpression]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "help":            // ON/OFF | TO [FileName] [COLLECTION [cCollectionURL]] [SYSTEM]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "index":           // TO [IndexFileList ] [ORDER nIndexNumber | IDXIndexFileName] [IN nWorkArea|cAlias] [SESSION nSessionID]
                            ds = jbe.App.CurrentDataSession;
                            wa = jbe.App.CurrentDS.CurrentWorkArea();

                            nds = eCodes.SESSION;

                            try
                            {
                                // Session setting
                                if (nds != 0)
                                {
                                    if (nds > 0)
                                        jbe.App.SetDataSession(nds);
                                    else
                                        throw new Exception("4016|");
                                }

                                // Workarea setting
                                JAXObjects.Token w = new();
                                w = await jbe.App.SolveFromRPNString(eCodes.InExpr);

                                if (w.Element.Type.Equals("C"))
                                    jbe.App.CurrentDS.SelectWorkArea(w.AsString());
                                else if (w.Element.Type.Equals("N"))
                                {
                                    if (w.AsInt() != 0)
                                    {
                                        if (w.AsInt() > 0)
                                            jbe.App.CurrentDS.SelectWorkArea(w.AsInt());
                                        else
                                            throw new Exception("4016|");
                                    }
                                }
                                else
                                    throw new Exception("11|");

                                // Open, select, or set no order
                                JAXDirectDBF db = jbe.App.CurrentDS.CurrentWA;
                                if (db is not null && db.DbfInfo.DBFStream is not null)
                                {
                                    if (string.IsNullOrEmpty(eCodes.NAME))
                                        db.DbfInfo.ControllingIDX = -1; // No name = no controlling index
                                    else
                                        await db.IDXOpen(eCodes.NAME);        // Open or select

                                    if (db.DbfInfo.ControllingIDX >= 0)
                                    {
                                        // Set descending order?
                                        db.DbfInfo.IDX[db.DbfInfo.ControllingIDX].NaturalOrder = Array.IndexOf(eCodes.Flags, "des") < 0;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                            }

                            // Revert back to the original session and area
                            jbe.App.SetDataSession(ds);
                            jbe.App.CurrentDS.SelectWorkArea(wa);
                            break;


                        case "key":             // TO [eExpression1 | RANGE eExpression2 [, eExpression3]] [IN cTableAlias | nWorkArea]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "mark":            // OF MENU|POPUP MenuBarName TO lExpression1
                                                // BAR nMenuItemNumber OF MenuName2 TO lExpression3
                                                // TO [cDelimiter]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "message":         // TO [cMessageText]
                                                // TO [nRow [LEFT | CENTER | RIGHT]]
                                                // WINDOW [WindowName]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "nocptrans":       // TO [FieldName1 [, FieldName2 ...]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "notify":          // [CURSOR] ON | OFF
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "order":           // TO [nIndexNumber | IDXIndexFileName] [IN nWorkArea | cTableAlias] [ASCENDING | DESCENDING]]
                            ds = jbe.App.CurrentDataSession;
                            wa = jbe.App.CurrentDS.CurrentWorkArea();

                            nds = eCodes.SESSION;

                            try
                            {
                                if (nds != 0)
                                {
                                    if (nds > 0)
                                        jbe.App.SetDataSession(nds);
                                    else
                                        throw new Exception("4016|");
                                }

                                JAXObjects.Token w = new();
                                w = await jbe.App.SolveFromRPNString(eCodes.InExpr);

                                if (w.Element.Type.Equals("C"))
                                    jbe.App.CurrentDS.SelectWorkArea(w.AsString());
                                else if (w.Element.Type.Equals("N"))
                                {
                                    if (w.AsInt() != 0)
                                    {
                                        if (w.AsInt() > 0)
                                            jbe.App.CurrentDS.SelectWorkArea(w.AsInt());
                                        else
                                            throw new Exception("4016|");
                                    }
                                }
                                else
                                    throw new Exception("11|");

                                // Open, select or set no controlling index
                                JAXDirectDBF db = jbe.App.CurrentDS.CurrentWA;
                                if (db is not null && db.DbfInfo.DBFStream is not null)
                                {
                                    if (string.IsNullOrEmpty(eCodes.NAME))
                                        db.DbfInfo.ControllingIDX = -1; // No name = no controlling index
                                    else
                                        await db.IDXOpen(eCodes.NAME);        // Open or select

                                    if (db.DbfInfo.ControllingIDX >= 0)
                                    {
                                        // WITH ASCENDING|DESCENDING
                                        if (eCodes.With.Count != 1) throw new Exception("1233||Invalid WITH clause");
                                        JAXObjects.Token ord = await jbe.App.SolveFromRPNString(eCodes.With[0].RNPExpr);
                                        if (ord.Element.Type.Equals("C"))
                                            db.DbfInfo.IDX[db.DbfInfo.ControllingIDX].NaturalOrder = ord.AsString().StartsWith("desc", StringComparison.OrdinalIgnoreCase) == false;
                                        else
                                            throw new Exception("11|");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                            }
                            break;

                        case "printer":         // ON | OFF | PROMPT
                            if (eCodes.Flags.Length > 0)
                            {
                                if (Array.IndexOf(eCodes.Flags, "on") >= 0)
                                    jbe.App.CurrentDS.JaxSettings.Printer = jbe.App.CurrentDS.JaxSettings.Printer_Default;
                                else if (Array.IndexOf(eCodes.Flags, "off") >= 0)
                                    jbe.App.CurrentDS.JaxSettings.Printer = -1;

                                if (Array.IndexOf(eCodes.Flags, "prompt") >= 0)
                                {
                                    // TODO - Set the default printer ID
                                }
                            }
                            else
                                jbe.App.CurrentDS.JaxSettings.Printer = -1;

                            break;

                        case "procedure":       // TO [FileName1 [, FileName2, ...]] [ADDITIVE]
                            if (eCodes.To.Count < 1 || Array.IndexOf(eCodes.Flags, "additive") < 0)
                            {
                                // Kill all procedure files in cache
                            }

                            // Add the list of procedure files
                            for (int i = 0; i < eCodes.To.Count; i++)
                            {
                                JAXObjects.Token pfile = await jbe.App.SolveFromRPNString(eCodes.To[i].Name);

                                if (pfile.TType.Equals("S") && pfile.Element.Type.Equals("C"))
                                    await AppHelper.LoadPrgIntoCache(jbe.App, "P", pfile.AsString(), string.Empty);
                                else
                                    throw new Exception("11");
                            }
                            break;

                        case "refresh":         // TO nSeconds1 [, nSeconds2]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "relation":        // TO [eExpression1 INTO nWorkArea1 | cTableAlias1 [, eExpression2 INTO nWorkArea2 | cTableAlias2...] [IN nWorkArea | cTableAlias] [ADDITIVE]]
                                                // TO IN nWorkArea | cTableAlias
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "reprocess":       // TO nAttempts [SECONDS] [SYSTEM] | TO AUTOMATIC [SYSTEM]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "resource":        // ON/OFF | TO [FileName]
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Resource = IsOn;
                            }


                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Resource = false;      // Ending all output to the current file and autosetting to off

                                jbe.App.CurrentDS.JaxSettings.Resource_Name = settingValue.Trim();
                            }
                            break;


                        case "view":
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "skip":            // TO [TableAlias1 [, TableAlias2 ...]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "sysmenu":         // TO ON | OFF | AUTOMATIC | TO [MenuList] | TO [MenuTitleList] | TO[DEFAULT] | TO LTRJUSTIFY | TO RTLJUSTIFY | SAVE | NOSAVE
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "talk":
                            if (HasOther) throw new Exception("11|");
                            if (HasOnOff)
                            {
                                status += " is " + (IsOn ? "ON" : "OFF");
                                jbe.App.CurrentDS.JaxSettings.Talk = IsOn;
                            }


                            if (eCodes.To.Count > 0)
                            {
                                if (ToExpression.Element.Type.Equals("C") == false) throw new Exception("11|");

                                settingValue = ToExpression.AsString();
                                if (string.IsNullOrWhiteSpace(settingValue))
                                    jbe.App.CurrentDS.JaxSettings.Talk = false;      // Ending all output to the current file and autosetting to off

                                if (string.IsNullOrWhiteSpace(settingValue) == false && settingValue.Trim().Equals("default", StringComparison.OrdinalIgnoreCase) == false)
                                    throw new Exception("4013|" + settingValue);

                                jbe.App.CurrentDS.JaxSettings.Talk_Console = settingValue.ToLower().Trim();
                            }
                            break;

                        case "textmerge":       // [ON | OFF] [TO [FileName] MEMVAR VarName [ADDITIVE]] [WINDOW WindowName][SHOW | NOSHOW]
                                                // DELIMITERS [TO cLeftDelimiter [, cRightDelimiter]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "topic":           // TO [cHelpTopicName | lExpression]
                                                // ID TO nHelpContextID
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "typeconvert":
                            if (HasOnOff)
                                jbe.App.CurrentDS.JaxSettings.TypeConvert = IsOn;
                            break;

                        default:
                            throw new Exception("1999|Unsupported setting " + settingName);
                    }
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }



            if (string.IsNullOrEmpty(status))
                throw new Exception($"10||Setting {settingName} not found or could not be set.");

            jbe.App.Talk(status);
            return string.Empty;
        }

        public static JAXObjects.Token GetSettings(AppClass App, string settingName, int idx)
        {
            JAXObjects.Token answer = new();

            try
            {
                // This method is used to get or set settings for the JAXBase executer.
                // If settingValue is provided, it sets the value; otherwise, it retrieves the current value.
                // For simplicity, we will just return a formatted string.
                if (string.IsNullOrEmpty(settingName) == false)
                {
                    // SET the setting's value
                    switch (settingName.ToLower())
                    {
                        // ON/OFF Settings
                        case "ansi": answer.Element.Value = App.CurrentDS.JaxSettings.ANSI; break;
                        case "asserts": answer.Element.Value = App.CurrentDS.JaxSettings.Asserts; break;
                        case "autoincerror": answer.Element.Value = App.CurrentDS.JaxSettings.AutoIncError; break;
                        case "autosave": answer.Element.Value = App.CurrentDS.JaxSettings.AutoSave; break;
                        case "confirm": answer.Element.Value = App.CurrentDS.JaxSettings.Confirm; break;
                        case "cpdialog": answer.Element.Value = App.CurrentDS.JaxSettings.CP_Dialog; break;
                        case "cursor": answer.Element.Value = App.CurrentDS.JaxSettings.Cursor; break;
                        case "debug": answer.Element.Value = App.CurrentDS.JaxSettings.Debug; break;
                        case "deleted": answer.Element.Value = App.CurrentDS.JaxSettings.Deleted; break;
                        case "development": answer.Element.Value = App.CurrentDS.JaxSettings.Development; break;
                        case "echo": answer.Element.Value = App.CurrentDS.JaxSettings.Echo; break;
                        case "exact": answer.Element.Value = App.CurrentDS.JaxSettings.Exact; break;
                        case "exclusive": answer.Element.Value = App.CurrentDS.JaxSettings.Exclusive; break;
                        case "fixed": answer.Element.Value = App.CurrentDS.JaxSettings.Fixed; break;
                        case "fullpath": answer.Element.Value = App.CurrentDS.JaxSettings.FullPath; break;
                        case "headings": answer.Element.Value = App.CurrentDS.JaxSettings.Headings; break;
                        case "lock": answer.Element.Value = App.CurrentDS.JaxSettings.Lock; break;
                        case "logerrors": answer.Element.Value = App.CurrentDS.JaxSettings.LogErrors; break;
                        case "multilocks": answer.Element.Value = App.CurrentDS.JaxSettings.MultiLocks; break;
                        case "near": answer.Element.Value = App.CurrentDS.JaxSettings.Near; break;
                        case "null": answer.Element.Value = App.CurrentDS.JaxSettings.Null; break;
                        case "readborder": answer.Element.Value = App.CurrentDS.JaxSettings.Readborder; break;
                        case "safety": answer.Element.Value = App.CurrentDS.JaxSettings.Safety; break;
                        case "seconds": answer.Element.Value = App.CurrentDS.JaxSettings.Seconds; break;
                        case "space": answer.Element.Value = App.CurrentDS.JaxSettings.Space; break;
                        case "sqlbuffering": answer.Element.Value = App.CurrentDS.JaxSettings.SQLBuffering; break;
                        case "status": answer.Element.Value = App.CurrentDS.JaxSettings.Status; break;
                        case "step": answer.Element.Value = App.CurrentDS.JaxSettings.Step; break;
                        case "sysformats": answer.Element.Value = App.CurrentDS.JaxSettings.SysFormats; break;
                        case "tableprompt": answer.Element.Value = App.CurrentDS.JaxSettings.TablePrompt; break;
                        case "trbetween": answer.Element.Value = App.CurrentDS.JaxSettings.TRBetween; break;
                        case "unique": answer.Element.Value = App.CurrentDS.JaxSettings.Unique; break;
                        case "varcharmapping": answer.Element.Value = App.CurrentDS.JaxSettings.VarCharMapping; break;
                        case "typeconvert": answer.Element.Value = App.CurrentDS.JaxSettings.TypeConvert; break;

                        // INT Values
                        case "blocksize": answer.Element.Value = App.CurrentDS.JaxSettings.BlockSize; break;
                        case "datasession": answer.Element.Value = App.CurrentDS.JaxSettings.DataSession; break;
                        case "fdow": answer.Element.Value = App.CurrentDS.JaxSettings.FDOW; break;
                        case "fweek": answer.Element.Value = App.CurrentDS.JaxSettings.FWeek; break;
                        case "hours": answer.Element.Value = App.CurrentDS.JaxSettings.Hours; break;
                        case "memowidth": answer.Element.Value = App.CurrentDS.JaxSettings.MemoWidth; break;
                        case "odometer": answer.Element.Value = App.CurrentDS.JaxSettings.Odometer; break;
                        case "strictdate": answer.Element.Value = App.CurrentDS.JaxSettings.StrictDate; break;
                        case "tablevalidate": answer.Element.Value = App.CurrentDS.JaxSettings.TableValidate; break;
                        case "topicid": answer.Element.Value = App.CurrentDS.JaxSettings.Topic_ID; break;
                        case "typeahead": answer.Element.Value = App.CurrentDS.JaxSettings.TypeAhead; break;


                        // ------------------------------------------------------------------------------------------------------------
                        // The following settings are more complex 
                        // ------------------------------------------------------------------------------------------------------------
                        case "alternate":       // ON/OFF | TO [FileName [ADDITIVE]]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Alternate;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Alternate_Name;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "bell":            // ON/OFF or TO cWaveFileName|cMP3FileName
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Bell;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Bell_Name;
                            else answer.Element.Value = string.Empty;
                            break;


                        case "carry":           // ON/OFF | TO [FieldList [ADDITIVE]]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Carry;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Carry_Name;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "debugout":        // TO [FileName [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "library":         // TO [FileName [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "path":            // TO [Path] [ADDITIVE]]  
                            answer.Element.Value = App.CurrentDS.JaxSettings.Path;
                            break;

                        case "century":         // ON/OFF | TO [nCentury [ROLLOVER nYear]]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Century;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Century_Current;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "classlib":        // TO ClassLibraryName [IN APPFileName | EXEFileName] [ADDITIVE][ALIAS AliasName]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "clock":           // ON | OFF | STATUS    -or -  TO[nRow, nColumn]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "coverage":        // ON/OFF | TO [FileName][ADDITIVE]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Coverage;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Coverage_Name;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "console":         // [name] [SIZE <nCols>,<nRows> | FULL] [AT <nLeft>,<nTop>] [ON | OFF | ACTIVE | INACTIVE] 
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Console;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Console_Name;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "device":          // TO SCREEN | TO PRINTER [PROMPT] | TO FILE FileName
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "eventlist":       // TO [EventName1 [, EventName2 ...] [ADDITIVE]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "eventtracking":   // ON/OFF/PROMPT | TO [FileName [ADDITIVE]]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.EventTracking;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.EventTracking_Name;
                            else answer.Element.Value = string.Empty;
                            break;


                        case "filter":          // ON|OFF|TO [lExpression] [IN nWorkArea | cTableAlias]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "function":        // nFunctionKeyNumber | KeyLabelName TO [eExpression]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "help":            // ON/OFF | TO [FileName] [COLLECTION [cCollectionURL]] [SYSTEM]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "index":           // TO [IndexFileList | ? ] [ORDER nIndexNumber | IDXIndexFileName | [TAG] TagName[OF CDXFileName][ASCENDING | DESCENDING]] [ADDITIVE]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "key":             // TO [eExpression1 | RANGE eExpression2 [, eExpression3]] [IN cTableAlias | nWorkArea]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "mark":            // OF MENU|POPUP MenuBarName TO lExpression1
                                                // BAR nMenuItemNumber OF MenuName2 TO lExpression3
                                                // TO [cDelimiter]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "message":         // TO [cMessageText]
                                                // TO [nRow [LEFT | CENTER | RIGHT]]
                                                // WINDOW [WindowName]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "nocptrans":       // TO [FieldName1 [, FieldName2 ...]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "notify":          // [CURSOR] ON | OFF
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "order":           // TO [nIndexNumber | IDXIndexFileName] [IN nWorkArea | cTableAlias] [ASCENDING | DESCENDING]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "printer":         // ON | OFF | PROMPT
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Printer >= 0;
                            else if (idx == 1) answer.Element.Value = "PRINTEROBJECT"; // TODO
                            else answer.Element.Value = string.Empty;
                            break;

                        case "procedure":       // TO [FileName1 [, FileName2, ...]] [ADDITIVE]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "refresh":         // TO nSeconds1 [, nSeconds2]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "relation":        // TO [eExpression1 INTO nWorkArea1 | cTableAlias1 [, eExpression2 INTO nWorkArea2 | cTableAlias2...] [IN nWorkArea | cTableAlias] [ADDITIVE]]
                                                // TO IN nWorkArea | cTableAlias
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "reprocess":       // TO nAttempts [SECONDS] [SYSTEM] | TO AUTOMATIC [SYSTEM]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "resource":        // ON/OFF | TO [FileName]
                            if (idx == 0) answer.Element.Value = App.CurrentDS.JaxSettings.Resource;
                            else if (idx == 1) answer.Element.Value = App.CurrentDS.JaxSettings.Resource_Name;
                            else answer.Element.Value = string.Empty;
                            break;

                        case "view":
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "skip":            // TO [TableAlias1 [, TableAlias2 ...]]
                                                // OF MENU|POPUP MenuBarName1 lExpression1
                                                // OF PAD MenuTitleName OF MenuBarName2 lExpression2
                                                // OF BAR nMenuItemNumber | SystemItemName OF MenuName2 lExpression4
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "sysmenu":         // TO ON | OFF | AUTOMATIC | TO [MenuList] | TO [MenuTitleList] | TO[DEFAULT] | TO LTRJUSTIFY | TO RTLJUSTIFY | SAVE | NOSAVE
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "textmerge":       // [ON | OFF] [TO [FileName] MEMVAR VarName [ADDITIVE]] [WINDOW WindowName][SHOW | NOSHOW]
                                                // DELIMITERS [TO cLeftDelimiter [, cRightDelimiter]]
                            throw new Exception("1999|Unsupported setting " + settingName);

                        case "topic":           // TO [cHelpTopicName | lExpression]
                                                // ID TO nHelpContextID
                            throw new Exception("1999|Unsupported setting " + settingName);

                        default:
                            throw new Exception("1999|Unsupported setting " + settingName);
                    }
                }
            }
            catch (Exception ex)
            {
                App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return answer;
        }
    }
}