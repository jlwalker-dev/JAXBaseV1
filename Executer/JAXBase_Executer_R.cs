using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_R
    {

        /*
         * 
         * RD path
         * 
         */
        public static async Task<string> RD(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = eCodes.Expressions.Count > 0 ? await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr) : throw new Exception("10|");
            if (answer.Element.Type.Equals("C") == false) throw new Exception("11|");
            string dirName = answer.AsString();
            FilerLib.RemoveDir(dirName);
            return string.Empty;
        }

        /*
         * 
         * Read Events
         * 
         */
        public static string Read(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            // We're performing a read events without attaching to a form
            jbe.App.AppLevels[Program.CurrentApp.CurrentAppLevel].InReadEvents = true;
            return string.Empty;
        }


        /*
         * 
         * RECALL [Scope] [FOR lExpression1] [WHILE lExpression2] [NOOPTIMIZE] [IN nWorkArea | cTableAlias]
         * 
         */
        public static async Task<string> Recall(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            return await JAXBase_Executer_D.DeleteFor(jbe, eCodes, false);
        }


        /*
         * 
         * REGISTER [IMAGE|SOUND|VIDEO] cFileName AS cMediaName
         * 
         */
        public static async Task<string> Register(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new("");

            // Get the media name
            if (eCodes.As.Count == 1)
                answer = await jbe.App.SolveFromRPNString(eCodes.As[0]);
            else if (eCodes.As.Count > 1)
                throw new Exception("10|");

            string mediaName = answer.AsString();

            // Get the file name
            if (eCodes.Expressions.Count == 1)
                answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
            else
                throw new Exception("10|");

            string fileName = answer.AsString();

            // What kind of media is it?
            if (string.IsNullOrWhiteSpace(eCodes.SUBCMD) || eCodes.SUBCMD.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                // Image
                jbe.App.JaxImages!.RegisterMedia(fileName, "I", mediaName, out _);
            }
            else if (eCodes.SUBCMD.Equals("Sound", StringComparison.OrdinalIgnoreCase))
            {
                // Sound
                jbe.App.JaxImages!.RegisterMedia(fileName, "S", mediaName, out _);
            }
            else if (eCodes.SUBCMD.Equals("Video", StringComparison.OrdinalIgnoreCase))
            {
                // Video
                jbe.App.JaxImages!.RegisterMedia(fileName, "V", mediaName, out _);
            }

            return "";
        }

        /* TODO NOW
         * 
         * Reindex
         * 
         */
        public static string Reindex(AppClass app, string cmdRest)
        {
            return string.Empty;
        }



        /* TODO NOW
         * 
         * RELEASE
         * 
         */
        public static string Release(AppClass app, string cmdRest)
        {
            return string.Empty;
        }



        /* TODO
         * 
         * REMOVE
         * 
         */
        public static string Remove(AppClass app, string cmdRest)
        {
            return string.Empty;
        }


        /* TODO NOW
         * 
         * RENAME 
         * 
         */
        public static async Task<string> Rename(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            int err = 0;
            string msg = string.Empty;
            string msg2 = string.Empty;

            if (string.IsNullOrWhiteSpace(eCodes.SUBCMD))
            {
                // RENAME file1 TO file2
                JAXObjects.Token sourceFile = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                JAXObjects.Token targetFile = await jbe.App.SolveFromRPNString(eCodes.To[0].Name);

                // Get the source file location
                string sFile = AppHelper.FindPathForFile(jbe.App, sourceFile.AsString()) + sourceFile.AsString();
                string sPath = JAXLib.JustFullPath(sFile);

                // Set the target file location if not specified
                string tFile = targetFile.AsString();


                if (tFile.Length > 0)
                {
                    string tPath = JAXLib.JustFullPath(tFile);
                    msg = tFile;

                    if (string.IsNullOrWhiteSpace(tPath) || (tPath.Length > 2 && (tPath[1] != ':' && tPath[..2].Equals(@"\\") == false)))
                        tFile = (sPath[0] == '\\' ? sPath[1..] : sPath) + tFile;

                    // Rename with optional move
                    try
                    {
                        File.Move(sFile, tFile);
                    }
                    catch (IOException ex)
                    {
                        err = 2223;
                        msg2 = ex.Message;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        err = 2222;
                        msg = string.Empty;
                        msg2 = ex.Message;
                    }
                    catch (Exception ex)
                    {
                        err = 9999;
                        msg = ex.Message;
                        msg2 = ex.Message;
                    }
                }
                else
                    err = 10;
            }
            else if (eCodes.SUBCMD.Equals("table", StringComparison.OrdinalIgnoreCase))
            {

            }
            else if (eCodes.SUBCMD.Equals("class", StringComparison.OrdinalIgnoreCase))
            {

            }
            else
                err = 10;

            if (err > 0)
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, $"{err}|");

            return string.Empty;
        }




        /*
         * 
         * REPLACE FieldName1 WITH eExpression1 [, FieldName2 WITH eExpression2...] [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [NOOPTIMIZE]
         * 
         * REPLACE FROM Array|JSON [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [NOOPTIMIZE]
         * 
         */
        public static async Task<string> Replace(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            JAXObjects.Token answer = new();

            try
            {
                // Starting workarea
                int wa = jbe.App.CurrentDS.CurrentWorkArea();

                if (string.IsNullOrWhiteSpace(eCodes.InExpr) == false)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.InExpr);

                    if (answer.Element.Type.Equals("C"))
                        jbe.App.CurrentDS.SelectWorkArea(answer.AsString());
                    else if (answer.Element.Type.Equals("N"))
                        jbe.App.CurrentDS.SelectWorkArea(answer.AsInt());
                    else
                        throw new Exception("11|");
                }

                JAXDirectDBF WorkArea = jbe.App.CurrentDS.CurrentWA;

                int MaxCount = jbe.App.CurrentDS.CurrentWA.DbfInfo.RecCount;

                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, jbe.App.CurrentDS.CurrentWA, false);  // Don't fix rec pos if blank scope

                while (jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFEOF == false)
                {
                    // Is there a WHILE clause?
                    if (string.IsNullOrWhiteSpace(eCodes.WhileExpr) == false)
                    {
                        // Get the value of the while clause
                        answer = await jbe.App.SolveFromRPNString(eCodes.WhileExpr);

                        // Is it a logical value?
                        if (answer.Element.Type.Equals("L"))
                        {
                            // If it's false, we're done looping
                            if (answer.AsBool() == false)
                                break;
                        }
                        else
                            throw new Exception("11|");
                    }

                    // If no FOR expression or the FOR expression evaluates to true then process this record.
                    // The FOR expression is used to scan records and selectively deal with those that match.
                    if (string.IsNullOrWhiteSpace(eCodes.ForExpr))
                        answer.Element.Value = true;                        // Nothing to parse, assume true
                    else
                        answer = await jbe.App.SolveFromRPNString(eCodes.ForExpr);    // Parse the for expression

                    // If the answer is a logical
                    if (answer.Element.Type.Equals("L"))
                    {
                        if (answer.AsBool())
                        {
                            // FOR expression matches, so do the replace
                            if (string.IsNullOrWhiteSpace(eCodes.From.Name))
                            {
                                // REPLACE FieldName1 WITH eExpression1
                                for (int i = 0; i < eCodes.Fields.Count; i++)
                                {
                                    answer = await jbe.App.SolveFromRPNString(eCodes.Fields[i].Name);
                                    if (answer.Element.Type.Equals("C"))
                                    {
                                        string fieldName = answer.AsString();
                                        answer = await jbe.App.SolveFromRPNString(eCodes.With[i].RNPExpr);

                                        // Put the expression into the field name respecting the buffering flag
                                        await WorkArea.DBFReplaceField(fieldName, answer, WorkArea.DbfInfo.Buffered == false);
                                    }
                                    else
                                        throw new Exception("11|");
                                }
                            }
                            else
                            {
                                // REPLACE FROM
                                answer = await jbe.App.SolveFromRPNString(eCodes.From.Name);
                                if (answer.Element.Type.Equals("C"))
                                {
                                    // If ( or [ appears in the variable name, it's just an array element
                                    bool IsArrayElement = answer.AsString().Contains("[") || answer.AsString().Contains("(");

                                    // Get the variable if it exists
                                    answer = await AppVars.GetVarFromExpression(answer.AsString(), null);

                                    // Is it an array or a simple value?
                                    if (answer.TType.Equals("A") && IsArrayElement == false)
                                    {
                                        // We have an array being passed in! - TODO

                                    }
                                    else if (answer.Element.Type.Equals("C"))
                                    {
                                        // It's just a simple var or array element being passed in
                                        // so it needs to be a JSON string in order to proceed. - TODO
                                    }
                                }
                                else
                                    throw new Exception("11|");
                            }

                            // If while and for expressions allow processing of this record
                            // then see if the JAXScope has been reached
                            if (jaxScope.IsDone()) break;

                            // If we're still processing, go to the next record
                            await jbe.App.CurrentDS.CurrentWA.DBFSkipRecord(1);

                            if (string.IsNullOrEmpty(eCodes.WhileExpr + eCodes.ForExpr))
                                break;
                        }
                    }
                    else
                        throw new Exception("11|"); // FOR expression was not logical
                }

                jbe.App.CurrentDS.SelectWorkArea(wa);

                result = string.Format("{0} records replaced", jaxScope.RecordsRead);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * RESTORE
         * 
         */
        public static string Restore(AppClass app, string cmdRest)
        {
            return string.Empty;
        }


        /* TODO
         * 
         * RESUME
         * 
         */
        public static string Resume(AppClass app, string cmdRest)
        {
            return string.Empty;
        }



        /* TODO
         * 
         * RETRY
         * 
         */
        public static string Retry(AppClass app, string cmdRest)
        {
            return string.Empty;
        }


        /*
         * 
         * RETURN [Expression]
         * 
         */
        public static async Task<string> Return(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            try
            {
                if (jbe.App.AppLevels.Count < 2) throw new Exception("2|");
                JAXObjects.Token answer = new();

                // Load expression to return stack
                if (eCodes.Expressions.Count > 0)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                    if (answer.TType.Equals("A"))
                        jbe.App.ReturnValue = answer;   // Returning an array
                    else
                        jbe.App.ReturnValue.Element.Value = answer.Element.Value;   // Returning a value or object
                }

                // Search for the return location
                string prg = string.Empty;

                if (eCodes.To.Count == 1)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.To[0].Name);
                    if (answer.Element.Type.Equals("C"))
                        prg = answer.AsString();
                    else
                        throw new Exception("11|");
                }
                else if (eCodes.Expressions.Count == 0)
                    jbe.App.ReturnValue.Element.Value = true;
                else
                    if (eCodes.Expressions.Count > 1)
                        throw new Exception("10|");


                // If we have a prg name, look for it
                if (string.IsNullOrWhiteSpace(prg) == false)
                {
                    int j = -1;
                    if (prg.Equals("master", StringComparison.OrdinalIgnoreCase))
                        j = 1;
                    else
                    {
                        for (int i = jbe.App.AppLevels.Count; i > 0; i++)
                        {
                            if (jbe.App.AppLevels[i].PrgName.Equals(prg, StringComparison.OrdinalIgnoreCase))
                            {
                                j = i;
                                break;
                            }
                        }

                        if (j < 0)
                            throw new Exception("1992|" + prg.ToUpper());
                    }

                    if (j > 0)
                    {
                        // Remove everything higher than this AppLevel location
                        while (jbe.App.AppLevels.Count > j)
                            jbe.App.AppLevels.RemoveAt(j + 1);

                        Program.CurrentApp.CurrentAppLevel = j - 1;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // Done with this level
            return "L";
        }
    }
}
