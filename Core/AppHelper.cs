/*
 * AppHelper
 * 
 * 2025-07-27 - JLW
 *      These helper routines are placed here to make the AppClass smaller.
 *      
 * 
 * All non-exe compiled JAX source starts with a Header that is pipe delimited, starts with
 * a headerStartByte (0x06) and ends with a headerEndByte (0x07), and is broken into the
 * following field structure:
 * 
 *  Cell    Description
 *     0    Stem of full file name
 *     1    CurrentMajorVersion.CurrentMinorVersion of JAX system
 *     2    Fully Qualified File Name of the original source file
 *     3    MD5 checksum
 *     4    Datetime when this file was compiled
 *     5    Starting Procedure (blank if not an APP file)
 * 
 * 
 * All compiled JAX source has a Procedure MAP immediately following the Header which starts with the 
 * headerMapStartByte (0x08) and ends with the headerMapEndByte (0x09).  Each entry is delimited with a 
 * stmtDelimiter and the entries are made up of any procedure/function names found in the file.  
 * 
 * NOTE: There is no fuctional difference between the FUNCTION and PROCEDURE keywords in JAXBase. Therefore, 
 * the FUNCTION keyword is converted to PROCEDURE and the ENDFUNC keyword is converted to ENDPROC during 
 * the compile processes.
 * 
 */
using JAXBase.Data;
using JAXBase.Language;
using JAXBase.Math;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using static JAXBase.Core.AppClass;

namespace JAXBase.Core
{
    public class AppHelper
    {

        /*
         * Utililty routines
         */

        /* --------------------------------------------------------------------------------------------------*
         * Crucial logic for variables and other names.  Checks to make sure the name is legal.  
         * May begin with one or more underscores, but otherwise must start with a letter
         * and may contain letters, numbers, and underscores.
         * 
         * Ths routine only looks at name parts, so "object.property" will return false.
         * 
         * Since different names have different maximum allowed lengths, length is not checked
         * as long as there is at least 1 character.
         * --------------------------------------------------------------------------------------------------*/
        public static bool IsLegalObjectName(string name)
        {
            bool result = true;
            name = name.Trim('_').ToLower();

            if (name.Length > 0)
            {
                if (JAXLib.Between(name[0], 'a', 'z'))
                {
                    for (int i = 0; i < name.Length; i++)
                    {
                        if ("abcdefghijklmnopqrstuvwxyz012345678_".Contains(name[i]) == false)
                        {
                            // Found an invalid character
                            result = false;
                            break;
                        }
                    }
                }
            }
            else
                result= false;  // Blank names not allowed

            return result;
        }

        /* --------------------------------------------------------------------------------------------------*
         * Return a string value as a token of the requested type
         * --------------------------------------------------------------------------------------------------*/
        public static JAXObjects.Token ReturnStringAsTokenOfType(string val, string setAsType)
        {
            JAXObjects.Token tk = new();
            setAsType = setAsType[..1].ToUpper();

            if ("CDLNO)".Contains(setAsType) == false) throw new Exception("1732|");

            switch (setAsType)
            {
                case "C":   // Character
                    tk.Element.Value = val;
                    break;

                case "D":   // DateOnly
                    if (DateOnly.TryParse(val, out DateOnly ddo) == false) ddo = DateOnly.MinValue;
                    tk.Element.Value = ddo;
                    break;

                case "L":   // Logical
                    tk.Element.Value = JAXLib.InList(val.ToLower(), ".t.", "true");
                    break;

                case "N":   // Numeric
                    if (double.TryParse(val, out double dd) == false) dd = 0D;
                    tk.Element.Value = dd;
                    break;

                case "T":   // DateTime
                    if (DateTime.TryParse(val, out DateTime dt) == false) dt = DateTime.MinValue;
                    tk.Element.Value = dt;
                    break;

                case "O":   // JAXObjectWrapper - defaulted to O=.NULL. and *= empty array.
                    JAXObjectWrapper jow = new(Program.CurrentApp, val, string.Empty, []);
                    tk.Element.Value = jow;
                    break;
            }

            return tk;
        }

        /* --------------------------------------------------------------------------------------------------*
         * Get an RPN or literal expression string and return the value
         * as an JAXObjects Token
         * --------------------------------------------------------------------------------------------------*/
        public static async Task<JAXObjects.Token> ProcessExpression(string expr)
        {
            JAXObjects.Token answer = new();

            try
            {
                if (expr[0] == literalStart)
                {
                    // Process a literal, returning as a string
                    if (expr[^1] != literalEnd)
                        throw new Exception("10||Missing end of  literal expression");

                    answer.Element.Value = expr.TrimStart(literalStart).TrimEnd(literalEnd);
                }
                else if (expr[0] == expByte)
                {
                    List<string> rpnList = [];

                    if (expr[^1] != expEnd)
                        throw new Exception("10||Missing end of expression string");

                    // Break out the expressions
                    string[] r = expr.TrimStart(expByte).TrimEnd(expEnd).Split(expParam);
                    for (int i = 0; i < r.Length; i++)
                    {
                        if (r[i].Length > 0)
                            rpnList.Add(r[i]);
                    }

                    if (rpnList.Count == 0)
                        throw new Exception("10||Empty expression List");

                    JAXMath jaxMath = new();
                    answer = await jaxMath.MathSolve(rpnList);
                }
                else
                    throw new Exception(string.Format("10||Unknown command byte {0}", expr[0]));
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(9904, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return answer;
        }


        /* ==================================================================================================*
         * This section loads code into cache
         *      Program, Query, and View source (P,Q,V) are loaded into CodeCache List in App
         *      All others are loaded into ClassCache List in App
         * ==================================================================================================*/

        /* --------------------------------------------------------------------------------------------------*
         * Routine to load code into Cache if it's not already loaded
         * 
         * If a FQFN is passed, then that exact match must exist, but if
         * just the stem, type is used to determine if something has been
         * already loaded into the code cache.
         * --------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Load a file into cache, compiling from source if required
        /// </summary>
        /// <param name="app"></param>
        /// <param name="type"></param>
        /// <param name="fName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> LoadFileIntoCache(string type, string fName)
        {
            string ext = JAXLib.JustExt(fName);
            string path = JAXLib.JustFullPath(fName);
            string stem = JAXLib.JustStem(fName);
            ExtensionTypes xTypes = AppHelper.GetCodeFileExtensions(type);

            string FinalFileName = string.Empty;
            int loaded = -1;

            if (path.Length == 0 && ext.Length == 0)
            {
                // Just have a stem and type.  Is it already loaded?
                loaded = IsStemLoaded(type, stem);
            }
            else if (path.Length > 0 && ext.Length > 0)
            {
                // Check to see if the FQFN is loaded.  If it doesn't have
                // a compiled file extension, it won't ever be true
                loaded = IsFileLoaded(fName);
            }

            // Is it already loaded?
            if (loaded < 0)
            {
                // It's not loaded so we have work to do
                if (path.Length > 0 && ext.Length > 0 && File.Exists(fName))
                {
                    // Put what we have into the FinalFileName
                    FinalFileName = fName;
                }
                else
                {
                    // We have an extension, is it the correct type?
                    if (ext.Length > 0 && xTypes.IsJAXCodeExtension(ext) == false)
                    {
                        // Non-standard extension - look for the file as written
                        path = FindPathForFile(stem + "." + ext);

                        if (path.Length > 0)
                            FinalFileName = path + stem + "." + ext;
                    }
                    else
                    {
                        string cCodePath = string.Empty;
                        string sCodePath = string.Empty;
                        string tCodePath = string.Empty;

                        if (path.Length == 0)
                        {
                            // Look for the compiled code name
                            cCodePath = FindPathForFile(stem + "." + xTypes.CompiledCode);

                            // Look for the source code name
                            sCodePath = FindPathForFile(stem + "." + xTypes.SourceCode);

                            // Look for the table source name
                            tCodePath = FindPathForFile(stem + "." + xTypes.SourceTable);
                        }

                        if (path.Length == 0)
                        {
                            if (cCodePath.Length > 0)
                            {
                                ext = xTypes.CompiledCode;
                                path = cCodePath;
                            }
                            else if (sCodePath.Length > 0)
                            {
                                ext = xTypes.SourceCode;
                                path = sCodePath;
                            }
                            else if (tCodePath.Length > 0)
                            {
                                ext = xTypes.SourceTable;
                                path = tCodePath;
                            }
                            else
                                throw new Exception("1|" + fName);
                        }

                        FinalFileName = path + stem + "." + ext;
                    }
                }

                // We should have a source or compiled file in hand
                if (FinalFileName.Length > 0 && File.Exists(FinalFileName))
                {
                    AppIO.DebugLog("Loading file into cache: " + FinalFileName);

                    // Is it compiled code or source code?
                    ext = JAXLib.JustExt(FinalFileName);

                    if (ext.Equals("app", StringComparison.OrdinalIgnoreCase) == false &&
                        ext.Equals(xTypes.CompiledCode, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        // If it's not a compiled file (including an APP), then we've got some work to do
                        string cFileName =
                            JAXLib.JustFullPath(FinalFileName)
                            + JAXLib.JustFName(FinalFileName)
                            + "." + xTypes.CompiledCode;

                        if (ext.Equals(xTypes.SourceCode, StringComparison.OrdinalIgnoreCase))
                        {
                            // We have the source code name so compile it
                            // and then pass the compiled file name
                            string cCode = CompileModule(FinalFileName, type);

                            // Attempt to compile it
                            if (JAXLib.StrToFile(cCode, cFileName, 0) >= 0)
                                FinalFileName = cFileName;      // Success - use the compiled name
                            else
                                FinalFileName = string.Empty;   // Failure - blank out the name
                        }
                        else if (ext.Equals(xTypes.SourceTable, StringComparison.OrdinalIgnoreCase))
                        {
                            // We have the source table name so call the correct
                            // table to source file processor
                            FinalFileName = type switch
                            {
                                // Class library
                                "C" => throw new Exception("1999| - Can't convert class library"),

                                // Class definition
                                "D" => throw new Exception("1999| - Can't convert class definition"),

                                // Form
                                "F" => await ConvertFormTable(FinalFileName),

                                // Label
                                "L" => throw new Exception("1999| - Can't convert label definition"),

                                // Menu
                                "M" => throw new Exception("1999| - Can't convert menu definition"),

                                // Popup
                                "O" => throw new Exception("1999| - Can't convert popup definition"),

                                // Query
                                "Q" => throw new Exception("1999| - Can't convert query definition"),

                                // Report
                                "R" => throw new Exception("1999| - Can't convert report definition"),

                                // View
                                "V" => throw new Exception("1999| - Can't convert view definition"),

                                // Unknown
                                _ => throw new Exception("1999| - Unknown definition type " + type),
                            };
                            FinalFileName = CompileModule(FinalFileName, type);
                        }
                        else
                        {
                            // We have a non-standard extension
                            throw new Exception("1999|");

                            // TODO 
                            // Is it a table?
                            // Load the first 1024 bytes
                            // Is it a compiled code header?
                            // Is it an APP header?
                            // Is it possible source code?
                            // Otherwise error out
                        }
                    }

                    // We should now be pointing at a file containing compiled code
                    if (File.Exists(FinalFileName))
                    {
                        ext = JAXLib.JustExt(FinalFileName);
                        if (ext.Equals("app", StringComparison.OrdinalIgnoreCase))
                        {
                            // Load the app file
                            loaded = LoadAppIntoCache(FinalFileName);
                        }
                        else if (ext.Equals(xTypes.CompiledCode, StringComparison.OrdinalIgnoreCase))
                        {
                            // Load the compiled file
                            switch (type)
                            {
                                case "D":       // Class definition
                                    loaded = LoadClassDefIntoCache(fName);
                                    break;

                                case "P":       // Compiled code
                                case "Q":       // Query
                                case "V":       // View
                                    loaded = LoadPRGIntoCache(type, FinalFileName);
                                    break;

                                case "C":       // Class Library
                                case "F":       // Form
                                case "L":       // Label
                                case "M":       // Menu
                                case "O":       // Popup
                                case "R":       // Report
                                    throw new Exception("1999| - Can't load  " + fName);
                            }
                        }
                        else
                        {
                            // Toss and error for the bad extension type
                            throw new Exception("1999|- Bad extension type " + type);
                        }
                    }
                }
            }

            return loaded;
        }


        /*
         * Look for a loaded codecache file based on stem and type
         */
        public static int IsStemLoaded(string type, string stem)
        {
            int result = -1;

            for (int i = 0; i < Program.CurrentApp.CodeCache.Count; i++)
            {
                if (Program.CurrentApp.CodeCache[i].Name.Equals(stem, StringComparison.OrdinalIgnoreCase) && Program.CurrentApp.CodeCache[i].Type.Equals(type))
                {
                    result = Program.CurrentApp.CodeCache[i].Procedures[stem.ToLower()];
                    break;
                }

                if (Program.CurrentApp.CodeCache[i].Procedures.ContainsKey(stem.ToLower()))
                {
                    result = Program.CurrentApp.CodeCache[i].Procedures[stem.ToLower()];
                    break;
                }
            }

            return result;
        }


        /*
         * Look for a loaded codecache file by compiled file name
         */
        public static int IsFileLoaded(string cFileName)
        {
            int result = -1;

            for (int i = 0; i < Program.CurrentApp.CodeCache.Count; i++)
            {
                if (Program.CurrentApp.CodeCache[i].FQFN.Equals(cFileName, StringComparison.OrdinalIgnoreCase))
                {
                    result = i;
                    break;
                }
            }

            return result;
        }

        /* --------------------------------------------------------------------------------------------------*
         * This routine loads a compiled Program, View, or Query file into the codecache
         * --------------------------------------------------------------------------------------------------*/
        private static int LoadPRGIntoCache(string type, string fName)
        {
            int result = -1;

            if (File.Exists(fName))
            {
                AppIO.DebugLog($"Loading PRG into cache: {fName}");

                // Get the code
                string cCode = JAXLib.FileToStr(fName);

                // Get the header
                int f = cCode.IndexOf(headerEndByte) + 1;
                if (f < 1)
                    throw new Exception("9992|" + fName);

                string header = cCode[..f];
                cCode = cCode[f..];

                AppIO.DebugLog("Breaking Header");
                FileHeader fileHeader = BreakHeader(fName, header);

                Program.CurrentApp.CodeCache.Add(BreakHeaderMap(type, fileHeader, cCode));        // Add the new definition to CodeCache
                result = Program.CurrentApp.CodeCache[^1].Procedures[Program.CurrentApp.CodeCache[^1].Name.ToLower()];
            }

            return result;
        }

        /*
         * Break out the procedures using the ProcedureMap and put
         * each procedure into it's own PRGCache.
         */
        public static CCodeCache BreakHeaderMap(string type, FileHeader fileHeader, string cCode)
        {
            // Get the map
            int f = cCode.IndexOf(headerMapEndByte) + 1;
            if (f < 1)
                throw new Exception("9992|" + fileHeader.SourceFQFN);

            AppIO.DebugLog("Breaking Procedure Map");
            string map = cCode[..f];
            cCode = cCode[f..];
            map = map.TrimStart(headerMapStartByte).TrimEnd(headerMapEndByte);
            string[] maps = map.Trim(AppClass.stmtDelimiter).Split(AppClass.stmtDelimiter);
            AppIO.DebugLog($"{maps.Length} procedures found");

            // Place it in the codecache
            CCodeCache cc = new()
            {
                Type = type,
                SourceFile = fileHeader.SourceFQFN,
                FileStem = fileHeader.Stem,
                FQFN = fileHeader.SourceFQFN,
                Version = fileHeader.CompilerVersion,
                CompileDT = fileHeader.CompiledAt,
                Name = fileHeader.Stem,
                MD5 = fileHeader.MD5,
                StartProc = fileHeader.Stem,
                Procedures = []
            };

            // Decompile what we have
            //app.lists.Decompile(app, "DECOMPILE_IT", cCode);

            // Fill the Procedures dictionary
            for (int i = 0; i < maps.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(maps[i]) == false)
                {
                    string b64 = maps[i][..4];
                    int loc = Program.CurrentApp.utl.Conv64ToInt(b64);
                    string name = maps[i][4..].Trim().ToLower();

                    string proc;
                    if (loc >= cCode.Length)
                    {
                        proc = cCode;
                        cCode = string.Empty;
                    }
                    else
                    {
                        proc = cCode[..loc];
                        cCode = cCode[loc..];
                    }

                    AppIO.DebugLog($"Adding {name.ToUpper()} to cache with length of {loc} bytes");
                    Program.CurrentApp.PRGCache.Add(proc);                             // Add the procedure to the PRGCache

                    // TODO - We overwrite duplicates, which we'll need to double
                    // check to see if it can happen during compile
                    cc.Procedures.Remove(name);

                    // Add to the CoceCache PROCEDURES dictionary
                    cc.Procedures.Add(name, Program.CurrentApp.PRGCache.Count - 1);    // Add the record to the Procedures Dictionary
                }
            }

            return cc;
        }
        /* --------------------------------------------------------------------------------------------------*
         * This routine loads an application file into the codecache
         * --------------------------------------------------------------------------------------------------*/
        private static int LoadAppIntoCache(string fName)
        {
            int result = -1;
            return result;
        }


        /* --------------------------------------------------------------------------------------------------*
         * Load a DEFINE CLASS name AS ParentClass of ClassLibrary
         * --------------------------------------------------------------------------------------------------*/
        public static int LoadClassDefIntoCache(string cCode)
        {
            int result = -1;
            string[] DCode = cCode.Replace(((char)10).ToString(), "").Split((char)13);
            List<string> DefCode = [];

            int i = 0;
            while (i < DCode.Length)
            {
                if (DCode[i].Trim().Length == 0)
                {
                    i++;
                    continue;
                }

                DefCode.Add(DCode[i].Trim());

                // Is there a continuation mark at the end of the line?
                if (DCode[i][^1].Equals(';') && i < DCode.Length - 1)
                {
                    // Append lines until there is no more continuation mark
                    while (true)
                    {
                        // Clear the current continuation mark
                        DefCode.Add(DefCode[i].TrimEnd(';'));

                        // Go to the next line
                        i++;

                        // Are we still in the bounds of the array?
                        if (i < DCode.Length)
                        {
                            // Skip empty lines
                            while (DCode[i].Trim().Length == 0 && i < DCode.Length)
                                i++;

                            // If still in bounds, add this line
                            if (i < DCode.Length)
                                DefCode[^1] += " " + DCode[i].Trim();   // Add this line
                            else
                                break;  // Out of bounds

                            // If no contiuation mark, break out, else loop for next line
                            if (DefCode[i][^1].Equals(";") == false) break;
                        }
                    }
                }
                else
                {
                    // No continuation mark, so just load the line
                    DefCode.Add(DefCode[i].Trim());
                    i++;
                }
            }


            // Now process the definition

            // Line 1 should be the DEFINE CLASS statement

            // Now continue looking for these things
            //      Protected varlist
            //      Hidden varlist
            //      Property=value
            //      Dimension statement
            //      AddObject name as [NOINIT] with propertylist
            //      [PROTECTED][HIDDEN] Procedure/Function statement
            //      EndProc/EndFunc
            //      EndDefine
            for (int j = 0; j < DefCode.Count; j++)
            {

            }

            return result;
        }


























        /*
         * Look for a named object in the PRGCache using the CodeCache to find it
         * If found, return the index else -1
         */
        public static int GetCachedCodeIDX(string type, string name)
        {
            int cCode = -1;

            for (int i = Program.CurrentApp.CodeCache.Count - 1; i >= 0; i--)
            {
                if (Program.CurrentApp.CodeCache[i].Type.Equals(type))
                {
                    if (Program.CurrentApp.CodeCache[i].Procedures.TryGetValue(name, out int idx))
                    {
                        cCode = idx;
                        break;
                    }
                }
            }

            return cCode;
        }




        /// <summary>
        /// Load a file from cache into the next applevel
        /// </summary>
        /// <param name="app"></param>
        /// <param name="pType"></param>
        /// <param name="prgParent"></param>
        /// <param name="prgFileName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> LoadForExecute(string pType, string prgParent, string prgFileName)
        {
            bool result = true;

            // Only these types get executed directly, the rest are loaded
            // into a variable as an object, so nothing happens here
            if ("PQV".Contains(pType))
            {
                int ccIDX = await LoadPrgIntoCache(pType, prgParent, prgFileName);

                if (ccIDX < 0)
                    throw new Exception("1|" + prgFileName);

                // If we get here, it's been located
                string prgStem = JAXLib.JustStem(prgFileName).ToLower();

                AppLevel alvl = new()
                {
                    CodeCacheIDX = ccIDX,
                    CodeCacheName = Program.CurrentApp.CodeCache[ccIDX].Name,
                    PRGCacheIdx = Program.CurrentApp.CodeCache[ccIDX].Procedures[prgStem],
                    PrgName = Program.CurrentApp.CodeCache[ccIDX].FQFN,
                    Procedure = prgStem
                };

                Program.CurrentApp.AppLevels.Add(alvl);
                Program.CurrentApp.CurrentAppLevel = Program.CurrentApp.AppLevels.Count;
            }
            else
                throw new Exception("9990||Illegal call in LoadForExecute: pType=" + pType);   // Bad Call

            return result;
        }

        /*-------------------------------------------------------------------------------------------------*
         * Look for this file type to load into the cache:
         *      Is it already loaded or can be loaded?
         *          1) Is it in the App Levels?
         *          2) Is the name in the procedures cache?
         *          3) Is the name in the code cache?
         *-------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Load program, query, or view code into the cache
        /// </summary>
        /// <param name="app"></param>
        /// <param name="pType"></param>
        /// <param name="prgParent"></param>
        /// <param name="prgFileName"></param>
        /// <returns></returns>
        public static async Task<int> LoadPrgIntoCache(string pType, string prgParent, string prgFileName)
        {
            int ccIDX = -1;

            try
            {
                // We only look for these types and error out on anything else
                if ("PQV".Contains(pType))
                {
                    // First check to make sure it's already loaded
                    if (prgParent.Length > 0)
                        await LoadFileIntoCache(pType, prgParent);
                    else
                        await LoadFileIntoCache(pType, prgFileName);

                    if (string.IsNullOrWhiteSpace(prgFileName) == false)
                    {
                        // Lood for the prgFileName
                        string prgPath = JAXLib.JustFullPath(prgFileName);
                        string prgStem = JAXLib.JustStem(prgFileName);
                        string prgExt = JAXLib.JustExt(prgFileName);
                        string prgCode = string.Empty;

                        // -----------------------------------------------------------------
                        // Look for it in the App Levels
                        // -----------------------------------------------------------------
                        for (int i = Program.CurrentApp.AppLevels.Count - 1; i > 0; i--)
                        {
                            var alv = Program.CurrentApp.AppLevels[i];
                            if (pType.Equals(alv.PrgType, StringComparison.OrdinalIgnoreCase) &&
                                alv.CodeCacheName.Equals(prgParent, StringComparison.OrdinalIgnoreCase))
                            {
                                if (Program.CurrentApp.CodeCache[Program.CurrentApp.AppLevels[i].CodeCacheIDX].Procedures.ContainsKey(prgStem))
                                {
                                    // Found it!
                                    ccIDX = Program.CurrentApp.AppLevels[i].CodeCacheIDX;
                                    break;
                                }
                            }
                        }

                        // -----------------------------------------------------------------
                        // Look for it in the Code Cache
                        // -----------------------------------------------------------------
                        if (ccIDX < 0)
                        {
                            for (int i = Program.CurrentApp.CodeCache.Count - 1; i >= 0; i--)
                            {
                                var ccd = Program.CurrentApp.CodeCache[i];
                                if (ccd.Procedures.ContainsKey(prgStem)
                                    && ccd.Type.Equals(pType, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Found it!
                                    ccIDX = Program.CurrentApp.AppLevels[i].CodeCacheIDX;
                                    break;
                                }
                            }
                        }

                        // Should not be negative
                        if (ccIDX < 0)
                            throw new Exception("1|" + prgFileName);
                    }
                }
                else
                    throw new Exception("9999|");   // Should not be here
            }
            catch (Exception e)
            {
                ccIDX = -1;
                AppErrorHandling.SetError(9905, e.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return ccIDX;
        }












        /*-------------------------------------------------------------------------------------------------*
         * Look for this class definition to load into the cache
         *-------------------------------------------------------------------------------------------------*/
        public static async Task<bool> LoadClassIntoCache(string pType, string prgParent, string prgFileName)
        {
            bool result;

            try
            {
                // First check to make sure it's already loaded
                if (prgParent.Length > 0)
                    await LoadFileIntoCache(pType, prgParent);
                else
                    await LoadFileIntoCache(pType, prgFileName);

                string prgStem = JAXLib.JustStem(prgFileName);
                int idx = -1;

                // -----------------------------------------------------------------
                // Look for it in the Class Defs
                // -----------------------------------------------------------------
                for (int i = Program.CurrentApp.ClassDefinitions.Count - 1; i >= 0; i--)
                {
                    if (Program.CurrentApp.ClassDefinitions[i].Name.Equals(prgStem, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }

                if (idx < 0)
                    throw new Exception("1|" + prgFileName);

                result = true;
            }
            catch (Exception e)
            {
                result = false;
                AppErrorHandling.SetError(9999, e.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }










        /* ------------------------------------------------------------------*
         *  Compile and save a source file which is expected to exist
         * ------------------------------------------------------------------*/
        public static string CompileModule(string fName, string type)
        {
            string fResult;
            string fStem = JAXLib.JustStem(fName);

            if (File.Exists(fName))
            {
                // ------------------------------------------------------------------
                // Compile it
                // ------------------------------------------------------------------
                string code = JAXLib.FileToStr(fName).Trim();
                string MD5 = Program.CurrentApp.utl.GetFileCheckSum_MD5(code);
                string compiledCode = Program.CurrentApp.JaxCompiler.CompileBlock(code, false, out int errCount);

                if (errCount > 0) throw new Exception("9997|" + fName);

                // ------------------------------------------------------------------*
                // Get the header and procedure map
                // ------------------------------------------------------------------
                AppIO.DebugLog("Creating Header");
                string header = CreateHeader(fName, MD5, type, fStem);

                AppIO.DebugLog("Creating Procedure Map");
                string pmap = CreateProcedureMap(compiledCode, fStem);

                // ------------------------------------------------------------------
                // Save the file
                // ------------------------------------------------------------------
                fResult = JAXLib.JustFullPath(fName) + fStem + (type.Equals("S", StringComparison.OrdinalIgnoreCase) ? ".jxs" : ".jxp");
                if (File.Exists(fResult)) FilerLib.DeleteFile(fResult);

                AppIO.DebugLog($"Saving file {fResult}");
                JAXLib.StrToFile(header + pmap + compiledCode, fResult, 0);
            }
            else
                throw new Exception("1|" + fName);

            return fResult;
        }


        /* ------------------------------------------------------------------*
         * Create a Class Definition from VFP SCX table using just the
         * platform='WINDOWS' records.
         * 
         * VFP always creates an single form SCX so that there is a header 
         * record followed by the dataenvironment, followed by cursor 
         * definitions, followed by the form definitions, followed by 
         * everything else, and ending with a trailer record.
         * 
         * 
         *  2025-06-22
         *      Formsets are not supported, yet, and I am waiting until
         *      I get into c++ before I consider supporting them.
         *      
         * ------------------------------------------------------------------*/
        /// <summary>
        /// Convert an SCX file to a source code file
        /// </summary>
        /// <param name="app"></param>
        /// <param name="fName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> ConvertFormTable(string fName)
        {
            ExtensionTypes Extension = AppHelper.GetCodeFileExtensions("F");

            string crlf = ((char)13).ToString() + ((char)10).ToString();
            string cr = ((char)13).ToString();

            string FormFile;
            string objName = JAXLib.JustStem(fName);

            // Force it to end with an scx extension - no screwing around with other extensions
            if (JAXLib.JustExt(fName).Equals(Extension.SourceTable, StringComparison.OrdinalIgnoreCase) == false)
                fName = JAXLib.JustFullPath(fName) + JAXLib.JustStem(fName) + "." + Extension.SourceTable;

            // If it's not here, look for it in the path list
            if (File.Exists(fName) == false)
                fName = FindPathForFile(fName) + JAXLib.JustFName(fName);

            // Is it available?
            if (File.Exists(fName))
            {
                // Create a table instance

                // Open up the scx
                if (await Program.CurrentApp.CurrentDS.OpenTable(fName, "thisscx") == 0)
                {
                    // find record where platform="COMMENT" and UniqueID="RESERVED" and load
                    // Properties for font information
                    JAXDirectDBF jdbf = Program.CurrentApp.CurrentDS.CurrentWA;

                    // 2025-07-07 - Added ability to autoload memo info when reading record(s)
                    DataTable dt = await jdbf.DBFSelect("properties", "top 1", "platform='COMMENT' and uniqueid='RESERVED'", true);
                    if (dt.Rows.Count == 0) throw new Exception("8000|Missing 'RESERVED' record");
                    string FormFontInfo = GetFieldToken(dt.Rows[0], "properties").AsString().Replace(((char)10).ToString(), "");
                    string[] FormFonts = FormFontInfo.Split((char)13);
                    FormFile = JAXLib.JustFullPath(fName) + JAXLib.JustStem(fName) + "." + Extension.SourceCode; // Intermediate prg for form
                    FilerLib.DeleteFile(FormFile);

                    JAXLib.StrToFile("* Form Name: " + fName + crlf, FormFile, 1);
                    JAXLib.StrToFile("* Created  : " + DateTime.Now.ToString() + crlf, FormFile, 1);
                    JAXLib.StrToFile("* JAXBase  : " + CurrentMajorVersion.ToString()
                        + "." + CurrentMinorVersion.ToString() + crlf, FormFile, 1);
                    JAXLib.StrToFile("* ================================================================" + crlf, FormFile, 1);


                    // ------------------------------------------------------------------------------------
                    // TODO - DO FORM FormName
                    //      [NAME VarName [LINKED]]
                    //      [WITH cParameterList]
                    //      [TO VarName][NOREAD][NOSHOW]
                    // ------------------------------------------------------------------------------------
                    JAXLib.StrToFile(string.Format("release {0}", objName) + crlf, FormFile, 1);
                    JAXLib.StrToFile(string.Format("public {0}", objName) + crlf, FormFile, 1);
                    JAXLib.StrToFile(string.Format("{0}=createobject('{1}')", objName, objName) + crlf, FormFile, 1);
                    JAXLib.StrToFile(string.Format("{0}.show()", objName) + crlf, FormFile, 1);
                    JAXLib.StrToFile("return" + crlf, FormFile, 1);
                    JAXLib.StrToFile(crlf, FormFile, 1);
                    // ------------------------------------------------------------------------------------

                    // Class=form: get class, baseclass, classloc, objname, properties, reserved3, and methods
                    Dictionary<string, string> ParentChild = [];
                    dt = await jdbf.DBFSelect("*", "all", "platform='WINDOWS' and not deleted()", true);

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string parent = GetFieldToken(dt.Rows[i], "parent").AsString().Trim();
                        string objname = GetFieldToken(dt.Rows[i], "objname").AsString().Trim();

                        if (ParentChild.ContainsKey(parent.ToLower()) == false)
                            ParentChild.Add(parent.ToLower(), objname);
                        else
                            ParentChild[parent.ToLower()] += cr + objName;
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        ProcessSCXClassEntry(dt.Rows[i], ParentChild, FormFile, FormFonts);
                    }
                }
                else
                    throw new Exception("52|");
            }
            else
                throw new Exception("1|" + fName);

            return FormFile;
        }


        /* ------------------------------------------------------------------*
         * Grab the field information from the datarow (memo fields must 
         * already be loaded) and return the value back.  This routine is 
         * designed to only work on data rows and will not fetch data that 
         * is not already loaded into the row. Memo fields return just the 
         * value part of the JAXMemo object.
         * ------------------------------------------------------------------*/
        public static JAXObjects.Token GetFieldToken(DataRow row, string fieldname)
        {
            JAXObjects.Token tk = new();

            try
            {
                tk.Element.Value = row[fieldname];

                // Is it a Blob/General/Memo field?
                if (tk.Element.Type.Equals("O"))
                {
                    JAXTables.JAXMemo mInfo = (JAXTables.JAXMemo)tk.Element.Value;
                    tk = new();
                    tk.Element.Value = mInfo.Value; // Load the token with the value of the memo field
                }
            }
            catch (Exception ex)
            {
                tk.TType = "U";
                AppErrorHandling.SetError(9999, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            if (tk.TType.Equals("U")) throw new Exception("4012|" + fieldname);

            return tk;
        }

        public static void ProcessSCXClassEntry(DataRow row, Dictionary<string, string> ParentChild, string formFile, string[] formFontInfo)
        {
            string crlf = ((char)13).ToString() + ((char)10).ToString();
            string cr = ((char)13).ToString();

            string className = GetFieldToken(row, "class").AsString().Trim();
            string objName = GetFieldToken(row, "objName").AsString().Trim();
            string baseName = GetFieldToken(row, "baseclass").AsString().Trim();

            if (className.Equals("dataenvironment", StringComparison.OrdinalIgnoreCase))
            {
                // TODO - POC will not have a data environment
                // A dataenvironment loads in its own datasession and is 
                // a special object for forms in VFP, but we may do more in JAX...
                // Have to think about that!  Why not a dataenvironment for
                // more than just forms and formsets?  A data environment
                // could just as easily be set for a dropdown list, grid,
                // container, collection, etc.
                //
                // Definitely something to ponder!
                //
                // In VFP:
                // ---------------------------------------
                // Properties: Application, AutoCloseTables, AutoOpenTables, Comment, DataSource,
                //      DataSourceType, InitialSelectedAlias, Name, Objects, OpenViews, Tag
                // Events: AfterCloseTables, BeforeOpenTables, Destroy, Error, Init
                // Methods: AddObject, AddProperty, CloseTables, OpenTables, ReadExpression, ReadMethod,
                //      RemoveObject, ResetToDefault, SaveAsClass, WriteExpression
                //
                // Ideas for Additions in JAX:
                // ---------------------------------------
                // Properties: N/A
                // Events: AfterOpenTables, BeforeCloseTables
                // Methods: N/A
                //
            }
            else
            {
                // We don't do "OF CLASS LIBRARY [OLEPUBLIC]"
                JAXLib.StrToFile(string.Format("define class {0} as {1}" + crlf, objName, className), formFile, 1);

                JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);
                JAXLib.StrToFile("* Properties" + crlf, formFile, 1);
                JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);

                // Reserved3 holds user defined property definitions
                string[] r3Props = GetFieldToken(row, "reserved3").AsString().Replace(((char)10).ToString(), "").Split((char)13);
                for (int i = 0; i < r3Props.Length; i++)
                {
                    string r3p = r3Props[i].Trim();
                    if (r3p.Length > 0 && r3p[0].Equals('^'))
                    {
                        JAXLib.StrToFile("dimension " + r3p[1..] + crlf, formFile, 1);
                    }
                }

                // Load properties
                string[] props = (GetFieldToken(row, "properties").AsString()).Replace(((char)10).ToString(), "").Split((char)13);
                for (int i = 0; i < props.Length; i++)
                {
                    JAXLib.StrToFile(props[i] + crlf, formFile, 1);
                }
                JAXLib.StrToFile(crlf, formFile, 1);

                if (baseName.Equals("form", StringComparison.OrdinalIgnoreCase))
                {
                    string[] fontInfo = formFontInfo[0].Split(",");
                    if (fontInfo.Length > 8)
                    {
                        JAXLib.StrToFile("FontName='" + fontInfo[0].Trim() + "'" + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[1] + crlf, formFile, 1);
                        JAXLib.StrToFile("FontSize=" + fontInfo[2] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[3] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[4] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[5] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[6] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[7] + crlf, formFile, 1);
                        //JAXLib.StrToFile("Font????=" + fontInfo[8] + crlf, formFile, 1);
                    }
                }

                // Add all child objects with ADD OBJECT CLASS AS NAME
                if (ParentChild.ContainsKey(objName.ToLower()))
                {
                    JAXLib.StrToFile(crlf, formFile, 1);
                    JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);
                    JAXLib.StrToFile("* Children" + crlf, formFile, 1);
                    JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);
                    string[] children = ParentChild[objName.ToLower()].Split(cr);
                    for (int i = 0; i < children.Length; i++)
                    {
                        JAXLib.StrToFile(string.Format("add object {0} as {0}" + crlf, children[i]), formFile, 1);
                    }
                }

                // Load methods
                string mCode = GetFieldToken(row, "methods").AsString();

                if (mCode.Length > 0)
                {
                    JAXLib.StrToFile(crlf, formFile, 1);
                    JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);
                    JAXLib.StrToFile("* Methods" + crlf, formFile, 1);
                    JAXLib.StrToFile("* -------------------------------------------------" + crlf, formFile, 1);
                    JAXLib.StrToFile("" + crlf, formFile, 1);
                    JAXLib.StrToFile(mCode, formFile, 1);
                    JAXLib.StrToFile(crlf, formFile, 1);
                }

                JAXLib.StrToFile("enddefine" + crlf, formFile, 1);
            }
        }


        /*
         * This routine is used to get the next parameter or to handle
         * a parameter passed to it.  Result is returned as a token.  
         * If Null is passed, the next parameter is retrieved and 
         * removed from the list, and if no parameter is available
         * it return a Token.Element.isNull()
         * 
         * Type:    P - PrivateVars (Public if Level=0) - ByRef
         *          L - LocalVars - ByRef
         *          M - Math string (such as "(4+3)*7")
         *          R - RPN Expression - By Value
         *          V - Var Expression to search - By Value
         *          T - Token value
         *          
         * Level:   0 = These PrivateVars are the Public vars
         *         >0 = P = PrivateVars, L=LocalVars
         *          Ignore if Type R or V
         *          
         */
        public static async Task<JAXObjects.Token> GetParameterToken(ParameterClass? p)
        {
            JAXObjects.Token result = new();

            if (p is null)
            {
                if (Program.CurrentApp.ParameterClassList.Count > 0)
                {
                    p = Program.CurrentApp.ParameterClassList[0];
                    Program.CurrentApp.ParameterClassList.RemoveAt(0);
                }
            }


            if (p is null)
            {
                // Return a null
                result.Element.MakeNull();
            }
            else
            {
                switch (p.Type)
                {
                    case "P":   // Private variable
                        result = Program.CurrentApp.AppLevels[p.Level].PrivateVars.jaxObject[p.RefVal.ToLower()];
                        break;

                    case "L":   // Local variable
                        result = Program.CurrentApp.AppLevels[p.Level].LocalVars.jaxObject[p.RefVal.ToLower()];
                        break;

                    case "T":   // Token value
                    case "N":   // Named token value
                        result.Element.Value = p.token.Element.Value;
                        break;

                    case "R":   // RPN
                        result = await Program.CurrentApp.SolveFromRPNString(p.RefVal);
                        break;

                    case "M":   // Math String
                        GenericClass gc = await Program.CurrentApp.JaxMath.SolveMath(p.RefVal);
                        result.CopyFrom(gc.Value);
                        break;

                    default:
                        throw new Exception($"1999|GetParametervalue Type ={p.Type}");
                }
            }

            return result;
        }

        /*-------------------------------------------------------------*
         * Just return the value of a parameter
         * 
         * An array will return just the first element
         *-------------------------------------------------------------*/
        public static async Task<object?> GetParameterValue(ParameterClass p)
        {
            JAXObjects.Token result = new();
            JAXObjects.Token answer = await GetParameterToken(p);
            if (answer.TType.Equals("A"))
                result.Element.Value = answer._avalue[0].Value;
            else
                result.CopyFrom(answer);

            return result.Element.Value;
        }




        /// <summary>
        /// Push an object to the parameter list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="val"></param>
        public static void PushParameterValue(object? val)
        {
            ParameterClass p = new() { Type = "T" };

            if (val is null)
                p.token.Element.MakeNull();
            else
                p.token.Element.Value = val;

            Program.CurrentApp.ParameterClassList.Add(p);
        }


        /*
         * Create a header for the compiled program code
         */
        /// <summary>
        /// Creates header usingg provided information
        /// </summary>
        /// <param name="fName"></param>
        /// <param name="MD5"></param>
        /// <param name="type"></param>
        /// <param name="startingProc"></param>
        /// <returns>Header string</returns>
        public static string CreateHeader(string fName, string MD5, string type, string startingProc)
        {
            return headerStartByte.ToString() + type + "|" + JAXLib.JustStem(fName) + "|"
                    + CurrentMajorVersion.ToString() + "." + CurrentMinorVersion.ToString() + "|"
                    + fName + "|" + MD5 + "|" + DateTime.Now.ToString() + "|"
                    + startingProc + "|" + headerEndByte.ToString();
        }


        /// <summary>
        /// Breaks up header string into a FileHeader class object
        /// </summary>
        /// <param name="fName"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static FileHeader BreakHeader(string fName, string header)
        {
            header = header.TrimStart(headerStartByte).TrimEnd(headerEndByte);
            string[] aHeader = header.Split("|");
            FileHeader headerClass = new();

            if (aHeader.Length < 7)
                throw new Exception($"9999|{fName}| - Header Cell Count is less than 6");

            if ("CDFLMOPQRV".Contains(aHeader[0]))
                headerClass.Type = aHeader[0];
            else
                throw new Exception($"9999|{fName}| - Invalid Type");

            if (aHeader[1].Length > 0)
                headerClass.Stem = aHeader[1];
            else
                throw new Exception($"9999|{fName}| - Invalid Stem");

            if (aHeader[2].Length == 0 || float.TryParse(aHeader[2], out headerClass.CompilerVersion) == false)
                throw new Exception($"9999|{fName}| - Invalid Compiler Version");

            if (aHeader[3].Length > 0)
                headerClass.SourceFQFN = aHeader[3];
            else
                throw new Exception($"9999|{fName} - Invalid FQFN");

            if (aHeader[4].Length > 0)
                headerClass.MD5 = aHeader[4];
            else
                throw new Exception($"9999|{fName}| - Invalid MD5 checksum");

            if (aHeader[5].Length == 0 || DateTime.TryParse(aHeader[5], out headerClass.CompiledAt) == false)
                throw new Exception($"9999|{fName}| - Invalid Compiled DateTime");

            headerClass.StartingProc = aHeader[6].Length > 0 ? aHeader[6] : aHeader[1];

            return headerClass;
        }

        /*
         * Create the procedures map which is a list of all procedures and 
         * functions found in the code, sorted by position in the format:
         *      <Pos x64><Name>
         *      Example: 00F4MyFunc
         * 
         * Each mapping is separated with a stmtDelimiter character
         * 
         */
        /// <summary>
        /// Create a map of all procedures found in a compiled code block.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="code"></param>
        /// <returns>String of statement delimited procedures in format: 4 digit base 64 location 
        /// followed by name of procedure</returns>
        /// <exception cref="Exception"></exception>
        public static string CreateProcedureMap(string compiledCode, string fStem)
        {
            // ------------------------------------------------------------------
            // Create the procedures map which will create a list of all
            // procedures and functions found in the code, sorted by position
            // in the format:
            // 
            // Bytes Description
            // ----- ----------------------------------------------------------
            //    4  Base 64 value of number of bytes in the procedure/fucntion
            //    n  Name of program, procedure, or function
            // ------------------------------------------------------------------
            int b = 0;  // Current location in code
            int procStart = 0;

            StringBuilder ProcMap = new();

            //byte[] code = Encoding.UTF8.GetBytes(compiledCode);

            // Procedure command byte
            int ibyte = Array.IndexOf(JAXLanguageLists.JAXCommands, "PROCEDURE");
            Program.CurrentApp.utl.Conv64(ibyte, 2, out string p64);
            p64 = AppClass.cmdByte + p64;

            string procName = fStem;
            int f;
            // Look for each instance in the compiled code
            while (b < compiledCode.Length)
            {
                b = Program.CurrentApp.utl.FindByteSequence(compiledCode, p64, b);

                // Is this the start of a new procedure?
                if (b >= 0)
                {
                    string h = string.Empty;
                    string d = string.Empty;
                    string c = string.Empty;

                    for (int jj = b; jj < b + 15; jj++)
                    {
                        int bb = compiledCode[jj];
                        h += " " + bb.ToString("X2") + " ";
                        d += JAXLib.Right("    " + bb.ToString("D3").TrimStart('0', ' ') + " ", 4);
                        c += bb > 32 && bb < 127 ? " " + (char)bb + "  " : "    ";
                    }

                    AppIO.DebugLog(string.Empty);
                    AppIO.DebugLog(string.Empty);
                    AppIO.DebugLog($"Code starting at {b}");
                    AppIO.DebugLog("===========================");
                    AppIO.DebugLog(h);
                    AppIO.DebugLog(d);
                    AppIO.DebugLog(c);
                    AppIO.DebugLog(string.Empty);
                    AppIO.DebugLog(string.Empty);

                    // Get the length of the code for this procedure
                    int plen = b - procStart;
                    Program.CurrentApp.utl.Conv64(plen, 4, out string bp64);

                    // Add it to the map
                    AppIO.DebugLog($"Adding procedure {procName} - length of {plen} bytes");
                    ProcMap.Append(bp64 + procName + stmtDelimiter.ToString());

                    // It is!  So get the name of the procedure
                    // and strip it off the front of the code block
                    //f = Array.IndexOf(code, (byte)AppClass.cmdEnd, b);
                    f = compiledCode.IndexOf(AppClass.cmdEnd, b);
                    if (f < 0)
                    {
                        throw new Exception("9999||Unexpected end of code");
                    }
                    else
                    {
                        // Here's the name
                        //procName = Encoding.ASCII.GetString(code, b + 6, f - b - 7);
                        procName = compiledCode.Substring(b + 5, f - b - 6);
                        //procName = compiledCode[(b + 6)..(f - b - 7)];
                        procStart = b;

                        // Move to the next statement
                        if (f < compiledCode.Length)
                            b = f + 1;
                        else
                            b = compiledCode.Length;
                    }
                }
                else
                    break;
            }

            if (string.IsNullOrWhiteSpace(procName) == false)
            {
                // Get the length of the code
                b = compiledCode.Length - procStart;
                Program.CurrentApp.utl.Conv64(b, 4, out string bp64);

                // Add it to the map
                AppIO.DebugLog($"Adding procedure {procName} - length of {b} bytes");
                ProcMap.Append(bp64 + procName + stmtDelimiter.ToString());
            }

            // Make the proc map command - if there is a mapping then b>0
            return AppClass.headerMapStartByte.ToString() + ProcMap + AppClass.headerMapEndByte.ToString();
        }




        /// <summary>
        /// Returns the name of the compiled file to run, and if not running a program and finds
        /// source that is newer, will attemp to compile the source.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="fileName"></param>
        /// <param name="type"></param>
        /// <returns>FQFN of compiled program, empty string if not found or an error was returned</returns>
        public static string ReturnCompiledFileName(string fileName, string type)
        {
            string result = string.Empty;
            string jxpFileName = string.Empty;
            string fileExt = JAXLib.JustExt(fileName).ToLower();

            try
            {
                // Does the codecache contain this file?
                // Now compare source to compiled for date/time
                if (File.Exists(fileName))
                {
                    if (JAXLanguageLists.RunTimeExtensions.Contains(fileExt))
                    {
                        // It's a valid runtime extension so load and check
                        string cCode = JAXLib.FileToStr(fileName);
                        bool isCompiled = fileExt.Equals("exe") == false && cCode[0] == headerStartByte;

                        if (isCompiled)
                            result = fileName;
                        else
                            throw new Exception("1196|" + fileName.ToUpper());
                    }
                    else
                    {
                        // Assume its a valid source file, look for it's compiled form
                        result = JAXLib.JustFullPath(fileName) + JAXLib.JustStem(fileName)
                            + type.ToUpper() switch
                            {
                                "S" => ".jxs",
                                "M" => ".jxm",
                                "Q" => ".jxq",
                                "V" => ".jxv",
                                _ => ".jxp"
                            };

                        if (File.Exists(result) == false)
                            throw new Exception("1|" + result); // Doesn't exist -  Try to compile it
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(9903, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }



        /*-------------------------------------------------------------------------------------------------*
         * Compile a block of code and run it
         *-------------------------------------------------------------------------------------------------*/
        //public static string CompileAndRun(AppClass app, JAXObjectWrapper? ThisObject,  MethodClass MethodCall, string prg)
        //{
        //    string results = string.Empty;

        //    JAXBase_Compiler JAXCompiler = new(app);

        //    string compiledCode = JAXCompiler.CompileBlock(MethodCall.PrgCall, false, out int errCount);

        //    if (compiledCode.Length > 1)
        //    {
        //        // TODO Push the parameter list to the stack

        //        // Execute the code
        //        results += app.JaxExecutor.ExecuteBlock(null, compiledCode) + "\r\n";
        //    }

        //    return results;
        //}

        /*-------------------------------------------------------------------------------------------------*
         * 
         * Checks to see if a provided name exists.  If it does and we can overwrite, then
         * send out that name.  If the name is blank, use the short name to create a template
         * and loop until we find an open variable name and send out that name.
         * 
         * Return:  Numeric
         *              0=name does not exist
         *              1=name exists
         *              
         * Out:     String
         *              Name that was used with the returned result
         * 
         *-------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Checks to see if the provided object name exists.  If a blank name is sent, creates a default 
        /// name based on the object type.  If it does exist and can be overwritten, returns the name.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="objName"></param>
        /// <param name="shortName"></param>
        /// <param name="oName"></param>
        /// <returns>Name string if valid, empty string indicates name exists and 
        /// can't be overwritten</returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> CheckObjectName(string objName, string shortName)
        {
            objName = objName.Trim();
            shortName = shortName.Trim();
            string oName = "";

            if (JAXLib.ChrTran(objName.ToLower(), "abcdefghijklmnopqrstuvwxyz0123456789-_", "").Length > 0)
                throw new Exception(string.Format("10||Object name '{0}' is invalid", objName.ToUpper()));

            // Create the template for testing
            string testName = objName.Length < 1 ? shortName + "{0}" : objName;

            int i = 0;
            while (true)
            {
                i++;
                oName = string.Format(testName, i);  // Get the name to test

                if ((await AppVars.GetVarToken(oName)).TType.Equals("U"))
                    break;  // Name doesn't exist

                // If the name has {0} in it, we loop, otherwise
                // return a result of 1 to show it exists
                if (testName.Contains("{0}") == false)
                {
                    break;
                }
            }

            return oName;
        }



        /* ------------------------------------------------------------*
         * Returns list of the exetensions for each JAXBase file type
         * ------------------------------------------------------------*/
        public static ExtensionTypes GetCodeFileExtensions(string type)
        {
            ExtensionTypes result = new();

            switch (type.ToUpper())
            {
                case "C":       // Class Library
                    result.SourceTable = "jcl";
                    result.MemoFile = "jct";
                    result.SourceCode = "jxcp";
                    result.CompiledCode = "jxc";
                    result.VFPTable = "vcx";

                    break;

                case "D":       // Class definition source
                    result.SourceTable = "";
                    result.SourceCode = "def";
                    result.CompiledCode = "jxd";
                    result.VFPTable = string.Empty;
                    break;

                case "F":       // Form
                    result.SourceTable = "jfm";
                    result.MemoFile = "jft";
                    result.SourceCode = "jxfp";
                    result.CompiledCode = "jxf";
                    result.VFPTable = "scx";
                    break;

                case "L":       // Label
                    result.SourceTable = "jlb";
                    result.MemoFile = "jlt";
                    result.SourceCode = "jxlp";
                    result.CompiledCode = "jxl";
                    result.VFPTable = "lbx";
                    break;

                case "M":       // Menu
                    result.SourceTable = "jmn";
                    result.MemoFile = "jmt";
                    result.SourceCode = "jxmp";
                    result.CompiledCode = "jxm";
                    result.VFPTable = "mnx";
                    break;

                case "O":       // Popup
                    result.SourceTable = "jpu";
                    result.MemoFile = "jpt";
                    result.SourceCode = "jxup";
                    result.CompiledCode = "jxu";
                    result.VFPTable = string.Empty;
                    break;

                case "P":       // PRG
                    result.SourceTable = "";
                    result.SourceCode = "prg";
                    result.CompiledCode = "jxp";
                    result.VFPTable = string.Empty;
                    break;

                case "Q":       // Query
                    result.SourceTable = "jqr";
                    result.MemoFile = "jqt";
                    result.SourceCode = "qry";
                    result.CompiledCode = "jxq";
                    result.VFPTable = "qcx";
                    break;

                case "R":       // Report
                    result.SourceTable = "jrp";
                    result.MemoFile = "jrt";
                    result.SourceCode = "jxrp";
                    result.CompiledCode = "jxr";
                    result.VFPTable = "rpx";
                    break;

                case "V":       // View
                    result.SourceTable = "jvw";
                    result.MemoFile = "jvt";
                    result.SourceCode = "jxvp";
                    result.CompiledCode = "jxv";
                    result.VFPTable = string.Empty;
                    break;

                default:
                    throw new Exception("1999|File Type " + type);
            }

            return result;
        }


        /*-------------------------------------------------------------------------------------------------*
         * Initialize an object and return it's list position
         *-------------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Register a named object in the system and return the name chosen
        /// </summary>
        /// <param name="app"></param>
        /// <param name="baseClass"></param>
        /// <param name="className"></param>
        /// <returns>Objects position in SysObjects list</returns>
        public static string RegisterObject(string name, string className)
        {
            string result = string.Empty;
            int nameid = 0;

            // Make sure we have a name
            if (string.IsNullOrWhiteSpace(name))
                name = string.IsNullOrWhiteSpace(className) ? "unk" : className;

            // Make it lower case
            name = name.ToLower().Trim();

            // Look for an open slot
            while (Program.CurrentApp.SysObjects.ContainsKey(name + (nameid == 0 ? string.Empty : nameid.ToString())))
                nameid++;

            // Save it & return it
            result = name + (nameid == 0 ? string.Empty : nameid.ToString());
            Program.CurrentApp.SysObjects.Add(result, className);
            return result;
        }


        /// <summary>
        /// Looks for a file in the defaut path and path list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="fileName"></param>
        /// <returns>Path if file is found</returns>
        public static string FindPathForFile(string fileName)
        {
            string result = string.Empty;

            string path = JAXLib.JustFullPath(fileName);
            string fName = JAXLib.JustFName(fileName);

            try
            {
                if (File.Exists(fileName) == false)
                {
                    // Doing the search
                    string[] pathList = (Program.CurrentApp.CurrentDS.JaxSettings.Default + ";" + Program.CurrentApp.CurrentDS.JaxSettings.Path).Split(';');
                    for (int i = 0; i < pathList.Length; i++)
                    {
                        if (pathList[i].Length > 0)
                        {
                            path = JAXLib.Addbs(pathList[i]);
                            string fName2 = FixFileCase(path, fName, Program.CurrentApp.CurrentDS.JaxSettings.Naming, Program.CurrentApp.CurrentDS.JaxSettings.NamingAll);

                            if (File.Exists(fName2))
                            {
                                result = JAXLib.JustFullPath(fName2);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Already have a path so let's check it out
                    if (path[..2].Equals("..") || path[..1].Equals("/") || (path[..2].Equals(@"\\") == false && path[..1].Equals(@"\")))
                    {
                        // start of a legal relative path in linux or windows
                        result = GetFullResolvedPath(Program.CurrentApp.CurrentDS.JaxSettings.Default, path);

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false)
                        {
                            // All other OS's use forward slash so clean it up
                            result = result.Replace(@"\", "/");
                        }
                    }
                    else
                    {
                        // Not a legal relative path so test to see if it's a legal path
                        if (Directory.Exists(path))
                        {
                            result = path;

                            // We have a path, does the file live there?
                            if (File.Exists(result + fName) == false)
                                result = "";
                        }
                        else
                            result = "";
                    }
                }
            }
            catch
            {
                // Any error returns a blank string
                result = "";
            }

            return result;
        }


        /// <summary>
        /// Get the full path by combining the current path with a relative path
        /// </summary>
        /// <param name="defaultPath"></param>
        /// <param name="relPath"></param>
        /// <returns>Final full path</returns>
        public static string GetFullResolvedPath(string defaultPath, string relPath)
        {
            if (string.IsNullOrWhiteSpace(defaultPath))
                throw new ArgumentException("DefaultPath cannot be null or empty.", nameof(defaultPath));

            if (string.IsNullOrWhiteSpace(relPath))
                return Path.GetFullPath(defaultPath);

            string combined = Path.Combine(defaultPath, relPath);
            return Path.GetFullPath(combined);
        }



        /// <summary>
        /// Look for and return a user class based on name of the object
        /// </summary>
        /// <param name="app"></param>
        /// <param name="userClassName"></param>
        /// <returns>JAXObjectWrapper instance</returns>
        public static JAXObjectWrapper? FindUserClass(string userClassName)
        {
            JAXObjectWrapper? jow = null;

            for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
            {
                if (Program.CurrentApp.AppLevels[i].UserObjects.TryGetValue(userClassName, out jow))
                    break;
            }

            return jow;
        }


        /*-------------------------------------------------------------*
         * Convert a SimpleToken string to an object
         *-------------------------------------------------------------*/
        public static object? Convert2STValue(string val)
        {
            object? oResult = null;
            string ttype = val[..1];
            string tval = val[1..];

            switch (ttype)
            {
                case "C":
                    oResult = tval;
                    break;

                case "L":
                    oResult = tval.Equals(".T.");
                    break;

                case "N":
                    if (double.TryParse(tval, out double dval) == false) dval = 0;
                    oResult = dval;
                    break;

                case "D":
                    if (DateOnly.TryParse(tval, out DateOnly dto) == false) dto = DateOnly.MaxValue;
                    oResult = dto;
                    break;

                case "T":
                    if (DateTime.TryParse(tval, out DateTime dt) == false) dt = DateTime.MaxValue;
                    oResult = dt;
                    break;

                case "X":
                    break;

                default:
                    throw new Exception(string.Format("Invalid value type {0} passed", ttype));
            }

            return oResult;
        }


        /*
         * Parameter Stack Handling
         * ================================================================================================
         * 
         * Load a List<string> of RPN expressions as by value parameters for 
         * the next program/procedure called.
         * 
         * Parameter Class
         *      Type:   P - PrivateVars
         *              L - LocalVars
         *              M - Math string
         *              R - RPN Expr
         *              V - Var by Value
         *              
         *      Level:  0=Always use PrivateVars dictionary for Public (global) variables, otherwise 
         *              use LocalVars dictionary for Local variables
         *              
         *      RefVal: Types P & L holds variable expression for use by Reference
         *      
         *              Type M holds a match string to solve - must be prefixed with "="
         *              
         *              Type R holds RPN expression  - must be prefixed with AppClass.exprByte character
         *              
         *              Type V holds variable expression for use by Value and resolved after it is pulled 
         *              from list for use.  May start with an AppClass.literalStart character.
         *              
         * All validation is done after element is pulled from list
         * 
         */
        public static void ClearParameters()
        {
            Program.CurrentApp.ParameterClassList.Clear();
        }


        /// <summary>
        /// Break out the DO parameters and save them to the parameter list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="RPNString"></param>
        public static async Task LoadDOParameters(string RPNString)
        {
            // A DO/WITH always sends arrays and objects by reference and
            // everything else is sent by value.
            string[] pList = RPNString.Split(AppClass.expDelimiter);

            foreach (string p in pList)
            {
                string v = p.Trim(AppClass.expByte).Trim(AppClass.expEnd);

                // Is it a variable?
                if (p[0] == AppClass.literalStart)
                {
                    AppIO.DebugLog($"DO literal adding {p} to Parameter stack");
                    await LoadVarByRefToParameters(p);
                }
                else
                {
                    // It's an RPN expression
                    AppIO.DebugLog($"DO expression adding {p} to Parameter stack");
                    await LoadRPNStringToParameters(p);
                }
            }
        }

        /// <summary>
        /// Add one RPN expression to the parameters list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="rpnExpr"></param>
        public static async Task LoadRPNStringToParameters(string rpnExpr)
        {
            List<string> rpnList = [];
            rpnList.Add(rpnExpr);
            await LoadRPNListToParameters(rpnList, false);
        }

        /// <summary>
        /// Loads the token value into the parameter stack
        /// </summary>
        /// <param name="app"></param>
        /// <param name="tk"></param>
        public static void LoadTokenValToParameters(JAXObjects.Token tk)
        {
            LoadNamedTokenToParameters(tk, string.Empty);
        }

        /// <summary>
        /// Loads the token value as a named parameter to the parameter stack
        /// </summary>
        /// <param name="app"></param>
        /// <param name="tk"></param>
        /// <param name="ParamName"></param>
        public static void LoadNamedTokenToParameters(JAXObjects.Token tk, string ParamName)
        {
            ParameterClass p = new() { PName = ParamName };
            p.token.CopyFrom(tk);

            if (string.IsNullOrWhiteSpace(ParamName))
                AppIO.DebugLog($"adding value {tk.AsString()} to Parameter stack");
            else
                AppIO.DebugLog($"adding parameter {ParamName} with value {tk.AsString()} to Parameter stack");

            Program.CurrentApp.ParameterClassList.Add(p);
        }


        public static async Task LoadRPNToParameters(string rpnElement)
        {
            await LoadRPNToParameters(rpnElement, null);
        }

        public static async Task LoadRPNToParameters(string rpnElement, JAXObjectWrapper? jow)
        {
            ParameterClass parm = new();
            JAXObjects.Token tk = new();
            parm.Type = "T";

            if (rpnElement[0] == AppClass.expByte)
            {
                // RPN Expression
                tk = await Program.CurrentApp.SolveFromRPNString(rpnElement);
                parm.token.CopyFrom(tk);
            }
            else if (rpnElement[0] == AppClass.literalStart)
            {
                // It's a variable in a literal expression
                string var = rpnElement.Trim(AppClass.literalStart).Trim(AppClass.literalEnd);
                tk = await AppVars.GetVarFromExpression(rpnElement, jow);
                parm.token.CopyFrom(tk);
            }
            else if (rpnElement[0] == '=')
            {
                // It's a Math String that needs to be solved
                GenericClass gc = await Program.CurrentApp.JaxMath.SolveMath(rpnElement);
                parm.token.CopyFrom(gc.Value);
            }
            else
            {
                // Assuming it's a string that holds a variable
                tk = await AppVars.GetVarFromExpression(rpnElement, jow);
                parm.token.CopyFrom(tk);
            }

            AppIO.DebugLog($"adding type {parm.Type} from rpnElement {rpnElement} for value {tk.AsString()} to Parameter stack");
            Program.CurrentApp.ParameterClassList.Add(parm);

        }


        /// <summary>
        /// Add a list of RPN Expressions to the parametersclasslist
        /// </summary>
        /// <param name="app"></param>
        /// <param name="rpnList"></param>
        public static async Task LoadRPNListToParameters(List<string> rpnList, bool byRef)
        {
            try
            {
                // solve each RPN expression and add to App.ParameterList
                foreach (string rpnElement in rpnList)
                {
                    if (string.IsNullOrEmpty(rpnElement) == false)
                    {
                        if (byRef)
                        {
                            string vartest = rpnElement[1..^1];

                            if (vartest[0] == '_')
                            {
                                // Drop the leading underscore
                                vartest = vartest[1..];

                                // It's a var token - check to see if it's alone
                                if (JAXLib.ChrTran(vartest, "+-=[]/*^% $()`~@#&{}\\|?\";:',<>", string.Empty).Equals(vartest))
                                {
                                    // No special characters or spaces found, good start!
                                    if ("0123456789".Contains(vartest[0]) == false)
                                    {
                                        // Not started with a num, so definitely looks to be a var
                                        VarRef v = await AppVars.SolveVariableReference(vartest);

                                        if (v.row < 0 && v.col < 0)
                                        {
                                            // Double check to make sure it's just a var name
                                            JAXObjects.Token tk = await AppVars.GetVarToken(v.varName);
                                            if (tk.TType.Equals("A") || tk.Element.Type.Equals("O"))
                                            {
                                                // Loading ByRef
                                                await LoadVarByRefToParameters(v.varName);
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Dropped through to here?  Just send by value.
                        await LoadRPNToParameters(rpnElement);
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(9999, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }

        /// <summary>
        /// Solve a var type & location and save by value or by ref to the parameters list
        /// </summary>
        /// <param name="app"></param>
        /// <param name="rpnElement"></param>
        public static async Task LoadVarByRefToParameters(string rpnElement)
        {
            try
            {
                // Get the variable information
                string rpn = rpnElement.Trim(AppClass.literalStart).Trim(AppClass.literalEnd);
                VarRef vRef = await AppVars.SolveVariableReference(rpn);
                string v = vRef.varName;
                string refType = "S";   // Assume it's a simple token (send by value)
                int level = -1;

                if (JAXLib.InListC(v, ".null.", "null"))
                    throw new Exception("10||.null.");

                // Nulls jump this code
                if (v.Contains('.'))
                {
                    // Likely an Object reference so we
                    // need to do some extra work to see
                    // if it's a property or object
                    // TODO
                    string[] oref = v.Split('.');
                    v = oref[0];
                }
                List<string> varList = AppVars.BreakVar(v);
                level = AppHelper.GetVar(varList[0], out string varType);

                JAXObjects.Token vTest = new();
                if (varType.Equals("L"))
                    vTest = Program.CurrentApp.AppLevels[level].LocalVars.GetToken(v);
                else
                    vTest = Program.CurrentApp.AppLevels[level].PrivateVars.GetToken(v);

                if (vTest.TType.Equals('U'))
                    throw new Exception("12|" + v.ToUpper());
                else if (vTest.TType.Equals("A"))
                    refType = "A";
                else if (vTest.Element.Type.Equals("O"))
                    refType = "O";

                if ("AO".Contains(refType) == false)
                {
                    // Send by value
                    await LoadRPNStringToParameters(rpn);
                }
                else
                {
                    // Find the base of the variable expression and save the
                    // original expression to the class and add to the list
                    ParameterClass parm = new();
                    JAXObjects.Token tk = await AppVars.GetVarToken(rpn);

                    if (tk.Element.Type.Equals("X"))
                        tk.Element.MakeNull();
                    else
                    {
                        // TODO - get level and private or local!
                        parm.Type = varType;
                        parm.RefVal = rpn;
                        parm.Level = level;
                    }

                    AppIO.DebugLog($"adding {rpn} to Parameter stack by ref");
                    Program.CurrentApp.ParameterClassList.Add(parm);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(9999, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
        }

        // Also used by NAMING() function
        public static string FixFileCase(string path, string filename, int naming, bool allNaming)
        {
            // Make sure path & filename are actually separated
            path = JAXLib.JustFullPath(JAXLib.Addbs(path) + filename);
            filename = JAXLib.JustFName(filename);

            switch (naming)
            {
                case 1:
                    // Uppercase
                    filename = filename.ToUpper();
                    path = allNaming ? path.ToUpper() : path;
                    break;

                case 2:
                    // Lowercase
                    filename = filename.ToLower();
                    path = allNaming ? path.ToLower() : path;
                    break;

                case 3:
                    // Proper case
                    filename = JAXLib.Proper(filename);
                    path = allNaming ? JAXLib.Proper(path, allNaming) : path;
                    break;
            }

            return path + filename;
        }



        // Delete one or more elements from an array object
        public static void ADel(JAXObjects.Token tkArray, bool delCol, int idx)
        {
            if (tkArray.TType.Equals("A"))
            {
                if (delCol)
                {
                    // delete a column
                    if (idx < 1 || idx > tkArray.Col)
                        throw new Exception("1234|");

                    // Set values in column to false
                    for (int r = idx; r < tkArray.Row - 1; r++)
                    {
                        int destElement = (r - 1) * tkArray.Col + idx - 1;
                        tkArray._avalue[destElement].Value = false;
                    }

                    tkArray.SetElement(tkArray.Row, idx - 1);
                    tkArray.Element.Value = false;
                }
                else
                {
                    if (tkArray.Col > 1)
                    {
                        // Delete a row in a 2D column moving rows up
                        if (idx < 1 || idx > tkArray.Row)
                            throw new Exception("1234|");

                        // Move rows towards top and set last one to .F.
                        for (int r = idx; r < tkArray.Row - 1; r++)
                        {
                            for (int c = 0; c < tkArray.Col; c++)
                            {
                                int destElement = (r - 1) * tkArray.Col + c;
                                int sourceElement = r * tkArray.Col + c;
                                tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                            }
                        }

                        // Set the last row to false
                        for (int c = 0; c < tkArray.Col; c++)
                        {
                            tkArray.SetElement(tkArray.Row, c + 1);
                            tkArray.Element.Value = false;
                        }
                    }
                    else
                    {
                        // Move elements towards top and set last one to false
                        for (int i = idx - 1; i < tkArray.Row - 1; i++)
                            tkArray._avalue[i].Value = tkArray._avalue[i + 1].Value;

                        tkArray._avalue[tkArray.Row - 1].Value = false;
                    }
                }
            }
            else
                throw new Exception("232|");
        }


        // Initialize an array to the given dimensions and
        // toss an error if it's already an array.
        public static void ASetDimension(JAXObjects.Token tk, int r, int c)
        {
            if (tk.TType.Equals("A"))
                throw new Exception("233|");

            tk.TType = "A";
            tk.Row = r;
            tk.Col = c;

            while (tk._avalue.Count < r * c)
                tk._avalue.Add(new());
        }


        // Insert a row or column into an array token
        public static void AIns(JAXObjects.Token tkArray, bool insertCol, int count)
        {
            if (tkArray.TType.Equals("A"))
            {
                if (insertCol)
                {
                    // Inserting a column
                    if (count < 1 || count > tkArray.Col)
                        throw new Exception("1234|");

                    // Set values in column to .F.
                    for (int r = count; r < tkArray.Row - 1; r++)
                    {
                        int destElement = (r - 1) * tkArray.Col + count + 1;
                        int sourceElement = r * tkArray.Col + count;
                        tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                        tkArray._avalue[sourceElement].Value = false;
                    }
                }
                else
                {
                    // Adding a row to a 2D array?
                    if (count < 1 || count > tkArray.Row)
                        throw new Exception("1234|");

                    if (tkArray.Col > 1)
                    {

                        // Move rows towards down and set target row to false
                        for (int r = tkArray.Row - 1; r > count - 1; r--)
                        {
                            for (int c = 0; c < tkArray.Col; c++)
                            {
                                int destElement = (r - 1) * tkArray.Col + c;
                                int sourceElement = r * tkArray.Col + c;
                                tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                                tkArray._avalue[sourceElement].Value = false;
                            }
                        }

                        for (int c = 0; c < tkArray.Col; c++)
                        {
                            tkArray.SetElement(tkArray.Row, c + 1);
                            tkArray.Element.Value = false;
                        }
                    }
                    else
                    {
                        // Adding an element at the position
                        for (int r = tkArray.Row - 1; r > count - 1; r--)
                        {
                            tkArray._avalue[r].Value = tkArray._avalue[r - 1].Value;
                            tkArray._avalue[r - 1].Value = false;
                        }
                    }
                }
            }
            else
                throw new Exception("232|");
        }

        /*
         * Shrink an array by creating a new smaller, or same size, array.
         */
        /// <summary>
        /// Shrink an array by creating a new one and copying into it - extra elements are filled with .F.
        /// </summary>
        /// <param name="oldArray"></param>
        /// <param name="newrows"></param>
        /// <param name="newcols"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static JAXObjects.Token ACopyToNew(JAXObjects.Token oldArray, int newrows, int newcols)
        {
            if (oldArray.TType.Equals("A") == false)
                throw new Exception("232|");

            int oldrows = oldArray.Row;
            int oldcols = oldArray.Col;

            // If a negative is sent for either, use old values
            newrows = newrows < 0 ? oldrows : newrows;
            newcols = newcols < 0 ? oldcols : newcols;

            // Create the new array to desired size
            JAXObjects.Token newArray = new();
            AppHelper.ASetDimension(newArray, newrows, newcols);

            // Use the old array to fill in the new array
            for (int r = 0; r < oldrows; r++)
            {
                for (int c = 0; c < oldcols; c++)
                {
                    // Make sure we only try to update within 
                    // the bounds of the new array
                    if (r < newrows && c < newcols)
                    {
                        // We're inside the new array so it's ok to update it
                        int oldEelement = r * oldcols + c;
                        int newElement = r * newcols + c;
                        newArray._avalue[newElement].Value = oldArray._avalue[oldEelement].Value;
                    }
                }
            }

            // Now return it
            return newArray;
        }




        /*-------------------------------------------------------------*
         * JAXOBJECTS.TOKEN HANDLER
         * 
         * Returns  -1 if not found
         *          >0 = Level found
         *          
         * Out      P = Private
         *          L = Local
         *-------------------------------------------------------------*/
        public static int GetVar(string varName, out string varType)
        {
            int level = -1;
            JAXObjects.Token tk = new();
            varType = "U";

            try
            {
                // First check the current AppLevel becuase a local var
                // will have precidence over a public var of same name
                tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);

                if (tk.TType.Equals("U"))
                {
                    // Look for private var of this name
                    for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
                    {
                        tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                        level = 0;

                        if (tk.TType.Equals("U") == false)
                        {
                            level = i;
                            varType = "P";
                            break;
                        }
                    }
                }
                else
                {
                    level = Program.CurrentApp.AppLevels.Count - 1;
                    varType = "L";
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.SetError(9999, ex.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                tk.TType = "U";
                level = -1;
            }

            return level;
        }


        public static async Task<JAXObjects.Token> GetParameterClassToken(ParameterClass pc)
        {
            string rpn = pc.RefVal.ToLower();
            int level = pc.Level;
            string type = pc.Type;
            JAXObjects.Token tk = new();

            switch (type)
            {
                case "P":
                    tk = Program.CurrentApp.AppLevels[level].PrivateVars.jaxObject[rpn];
                    break;

                case "L":
                    tk = Program.CurrentApp.AppLevels[level].PrivateVars.jaxObject[rpn];
                    break;

                case "M":
                    GenericClass gc = await Program.CurrentApp.JaxMath.SolveMath(rpn);
                    tk.CopyFrom(gc.Value);
                    break;

                case "R":
                    tk = await Program.CurrentApp.SolveFromRPNString(rpn);
                    break;

                case "V":
                    tk = await AppVars.GetVarFromExpression(rpn, null);
                    break;

                case "T":
                    tk.CopyFrom(pc.token);
                    break;

                default:
                    throw new Exception($"9999||GetparameterClassValue - '{type}' HUH?");
            }

            return tk;
        }

        /*-------------------------------------------------------------*
         * Break an array or UDF into a list of parts
         *-------------------------------------------------------------*/
        public static List<string> BreakArrayOrUDF(string varName)
        {
            List<string> objList = [];

            int cpos = 0;

            if (")]".Contains(varName[^1]) == false) throw new Exception("10|End of varname is not ) or ]");

            char startQuote = varName[^1] == ')' ? '(' : '[';
            varName = varName[..^1];

            // Break the var name from the array or UDF
            while (varName.Length > 0 && cpos < varName.Length)
            {
                if (varName[cpos] == startQuote)
                {
                    // Found the left ( or [
                    objList.Add(varName[..cpos]);

                    // Is there anything left to parse?
                    if (cpos < varName.Length - 1)
                    {
                        cpos++;
                        varName = varName[cpos..];
                    }
                    else
                        varName = string.Empty;

                    break;  // we've got the name part
                }
                else
                {
                    if ("([".Contains(varName[cpos]))
                        throw new Exception("10|Mismatched start and end bracket/parens");
                    else
                        cpos++;
                }
            }

            // If it's a UDF, we're going to want to break up the rest of this
            cpos = 0;
            char endQuote = '\0';

            while (varName.Length > 0 && cpos < varName.Length)
            {
                char c = varName[cpos];

                if (endQuote == '\0')
                {
                    if ("(['\"".Contains(c))
                    {
                        if (c == '(')
                            endQuote = ')';
                        else if (c == '[')
                            endQuote = ']';
                        else
                            endQuote = c;
                    }
                    else if (c == ',')
                    {
                        // Found a comma for split
                        objList.Add(varName[..cpos].Trim(','));
                        if (cpos >= varName.Length) throw new Exception("10|");
                        varName = varName[cpos..].Trim(',');
                        cpos = 0;

                        if (varName.Contains(',') == false)
                        {
                            // Should be something left after the comma
                            if (string.IsNullOrWhiteSpace(varName)) throw new Exception("10|Expression missing after commad");

                            // Can't find a comma so add it to the list and we're done!
                            objList.Add(varName);
                            varName = string.Empty;
                            break;
                        }

                        // Skip the cpos++ so we're
                        // starting at pos 0
                        continue;
                    }
                }
                else if (endQuote == c)
                {
                    // Found the end quote
                    endQuote = '\0';
                }

                cpos++;
            }

            // Add anything left over into the list
            if (string.IsNullOrWhiteSpace(varName) == false)
                objList.Add(varName);

            return objList;
        }


        public static bool IsCompoundVar(string varName)
        {
            bool result = false;
            int cpos = 0;
            char endQuote = '\0';

            while (varName.Length > 0 && cpos < varName.Length)
            {
                char c = varName[cpos++];

                if (endQuote == '\0')
                {
                    if ("(['\"".Contains(c))
                    {
                        if (c == '(')
                            endQuote = ')';
                        else if (c == '[')
                            endQuote = ']';
                        else
                            endQuote = c;
                    }
                    else if (c == '.')
                    {
                        // Found a period outside of a quoted area
                        // so this is a compound variable (object.property)
                        result = true;
                        break;
                    }
                }
                else if (endQuote == c)
                {
                    // Found the end quote
                    endQuote = '\0';
                }
            }

            return result;
        }
    }
}
