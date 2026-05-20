using DynamicData;
using JAXBase.Core;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_E
    {


        /* TODO
         * 
         * EDIT [FIELDS FieldList] [NAME ObjectName] [NOAPPEND] [NODELETE] [NOMODIFY] [TIMEOUT nSeconds] [TITLE cTitleText] 
         * 
         */
        public static async Task<string> Edit(ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                JAXObjects.Token answer = new();
                JAXObjectWrapper emptyObj = new(Program.CurrentApp, "empty", "", []);

                // Add fields to object
                string fieldList = string.Empty;
                for (int i = 0; i < eCodes.Fields.Count; i++)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Fields[i].Name);
                    if (answer.Element.Type.Equals("C"))
                        fieldList += answer.AsString().Trim() + ",";
                }

                emptyObj.AddPropertyValue("parent", "E");   // Called by EDIT command
                emptyObj.AddPropertyValue("fields", fieldList.Trim(','));
                emptyObj.AddPropertyValue("name", eCodes.NAME);
                emptyObj.AddPropertyValue("noappend", eCodes.Flags.Contains("noa"));
                emptyObj.AddPropertyValue("nodelete", eCodes.Flags.Contains("nod"));
                emptyObj.AddPropertyValue("nomodify", eCodes.Flags.Contains("nom"));
                emptyObj.AddPropertyValue("timeout", eCodes.TIME);
                emptyObj.AddPropertyValue("title", eCodes.TITLE);   // TODO

                // Get the JSON string
                JAXObjects.Token json = new();
                json.Element.Value = JAXUtilities.JAXObjectWrapperJsonSerializer.ToJson(emptyObj, Newtonsoft.Json.Formatting.None);

                // Set up the JSON string to be passed to the editor program
                Program.CurrentApp.ParameterClassList.Clear();

                // TODO - CALL THE EDTIOR PROGRAM
                string editor = Program.CurrentApp.JaxVariables._EditPRG;
                if (string.IsNullOrWhiteSpace(editor))
                    throw new Exception("2600|");

                if (File.Exists(editor))
                {
                    // Push the json parameter
                    Program.CurrentApp.ParameterClassList.Add(new() { Type = "T", token = json });

                    // Make the call
                    await AppHelper.LoadForExecute("P", editor, string.Empty);
                }
                else
                    throw new Exception($"2601|{editor}");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * ELSE
         * 
         */
        public static string Else(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            string PrgCode = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                // Look for the endif and jump past it
                string lp = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endifcmd"] + eCodes.SUBCMD;
                int f = PrgCode.IndexOf(lp);

                if (f < 0)
                    throw new Exception("1211|");
                else
                {
                    Program.CurrentApp.utl.Conv64(f, 3, out string lp2);
                    result = "Y" + lp2;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * ENDTRANSACTION
         * 
         */
        public static string End(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (eCodes.Flags.Length == 0 || Array.IndexOf(eCodes.Flags, "transaction") < 0)
                    throw new Exception("1591|");

                throw new Exception("1999|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * 
         * ENDCASE - continue to next statement 
         * 
         */
        public static string EndCase(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * ENDDEFINE
         * 
         */
        public static async Task<string> EndDefine(string cmdRest)
        {
            try
            {
                if (Program.CurrentApp.InDefineObject is not null)
                {
                    JAXObjects.Token tk = await Program.CurrentApp.InDefineObject!.GetProperty("name");
                    if (tk.Element.IsNull() == false)
                    {
                        string name = tk.AsString().ToLower().Trim();

                        if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].UserObjects.ContainsKey(name))
                            Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].UserObjects[name] = Program.CurrentApp.InDefineObject;
                        else
                            Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].UserObjects.Add(name, Program.CurrentApp.InDefineObject);

                        Program.CurrentApp.InDefine = string.Empty;
                        Program.CurrentApp.CurrentClassMethod = string.Empty;
                        Program.CurrentApp.InDefineObject = null;
                    }
                    else
                        throw new Exception($"{Program.CurrentApp.InDefineObject.GetErrorNo()}|");
                }
                else
                    throw new Exception("1928|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /* 
         * 
         * ENDDO
         * 
         */
        public static string EndDo(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                string loc = Program.CurrentApp.MiscInfo["docmd"] + eCodes.SUBCMD;

                string PrgCode = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];
                int f = PrgCode.IndexOf(loc) - 1;

                if (f < 0)
                    throw new Exception("1209|");
                else
                {
                    Program.CurrentApp.utl.Conv64(f, 3, out string pos);
                    result = "X" + pos;
                    AppLoop.PopLoopStack(); // drop the if loop from the stack
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * ENDFOR
         * 
         */
        public static string EndFor(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                string loc = Program.CurrentApp.MiscInfo["forcmd"] + eCodes.SUBCMD;

                string PrgCode = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];
                int f = PrgCode.IndexOf(loc) - 1;

                if (f < 0)
                    throw new Exception("1207|");
                else
                {
                    Program.CurrentApp.utl.Conv64(f, 3, out string pos);
                    result = "X" + pos;
                    //Program.CurrentApp.PopLoopStack(); // drop the if loop from the stack
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * ENDIF
         * 
         */
        public static string EndIf(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                // Make sure we have matching IF/ENDIF
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /*
         * 
         * ENDPROCEDURE
         * 
         */
        public static string EndProcedure(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                Program.CurrentApp.ReturnValue.Element.Value = true;
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return "Z";
        }


        /* 
         * 
         * ENDSCAN
         * 
         */
        public static string EndScan(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                string loc = Program.CurrentApp.MiscInfo["scancmd"] + eCodes.SUBCMD;

                string PrgCode = Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx];
                int f = PrgCode.IndexOf(loc) - 1;

                if (f < 0)
                    throw new Exception("1203|");
                else
                {
                    Program.CurrentApp.utl.Conv64(f, 3, out string pos);
                    result = "X" + pos;
                    AppLoop.PopLoopStack(); // drop the if loop from the stack
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * ENDTEXT
         * 
         */
        public static string EndText(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * ENDTRY
         * 
         */
        public static string EndTry(ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                int tryPos = -1;

                // Look for the correct try in the current App Level
                for (int i = 0; i < Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack.Count; i++)
                {
                    if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack[i].Code == eCodes.SUBCMD)
                    {
                        tryPos = i;
                        break;
                    }
                }

                if (tryPos < 0)
                    throw new Exception("2058||");   // Some tom-foolery is going on with the Try code
                else
                {
                    // Throw away any AppLevels above the current as being here indicates we just
                    // processed a catch statement and control fell to the EndTry command.
                    // We no longer have the ability to call RESUME or RESUME NEXT.
                    while (Program.CurrentApp.AppLevels.Count > Program.CurrentApp.CurrentAppLevel + 1)
                        Program.CurrentApp.AppLevels.RemoveAt(Program.CurrentApp.AppLevels.Count - 1);

                    // Reset the Try Phase to 0 for this and all following TRY structures as we
                    // may be dealing with nested TRY structures.  If they are not nested then
                    // they will already be set to zero.  All TRY entries before this one
                    // will either be 1 or 0.
                    for (int i = tryPos; i < Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack.Count; i++)
                        Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].TryStack[i].TryPhase = 0;

                    Program.CurrentApp.InErrorTrap = false;

                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * ENDWITH
         * 
         */
        public static string EndWith(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
                if (Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count == 0) throw new Exception("1939|");
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.RemoveAt(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count - 1);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        public static async Task<string> ErrorCall(ExecuterCodes eCodes)
        {
            string result = "";

            if (eCodes.Expressions.Count > 0)
            {
                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                if (answer.Element.Type.Equals("N"))
                    AppErrorHandling.SetError(answer.AsInt(), $"{answer.AsInt()}|", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                else
                    throw new Exception("11||ERROR expression is not numeric");
            }
            else
                throw new Exception("10||Missing expression");

            return result;
        }


        /* 
         * 
         * EXIT
         * 
         */
        public static string Exit(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");

                string PrgCode = Program.CurrentApp.PRGCache.Count > 0 ? Program.CurrentApp.PRGCache[Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PRGCacheIdx] : string.Empty;

                // What loop are we currently in?
                string loopType = AppLoop.PopLoopStack();
                string loop = string.Empty;

                switch (loopType[0])
                {
                    case 'S':   // SCAN
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endscancmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + loopType + AppClass.cmdEnd;
                        break;

                    case 'W':   // WHILE
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["enddocmd"]+ Program.CurrentApp.CompilerXRef["CS"].ToString() + loopType + AppClass.cmdEnd;
                        break;

                    case 'F':   // FOR
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["endforcmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + loopType + AppClass.cmdEnd;
                        break;

                    case 'U':   // UNTIL
                        loop = AppClass.cmdByte + Program.CurrentApp.MiscInfo["untilcmd"] + Program.CurrentApp.CompilerXRef["CS"].ToString() + loopType + AppClass.cmdEnd;
                        break;

                    default:    // ERROR
                        throw new Exception("Unsupported loop type " + loopType[0]);
                }

                int pos = PrgCode.IndexOf(loop);

                if (pos < 0)
                    switch (loopType[0])
                    {
                        case 'S':   // SCAN
                            throw new Exception("1203|");

                        case 'W':   // WHILE
                            throw new Exception("1209|");

                        case 'F':   // FOR
                            throw new Exception("1207|");

                        case 'U':   // UNTIL
                            throw new Exception("1210|");
                    }
                else
                {
                    Program.CurrentApp.utl.Conv64(++pos, 3, out result);
                    result = "Y" + result;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * EXTERNAL
         * 
         */
        public static string External(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.AppLevels.Count < 2) throw new Exception("2|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

    }
}
