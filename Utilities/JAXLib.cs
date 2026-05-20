using Avalonia.Threading;
using JAXBase.Core;
using JAXBase.UI.Dialogs;
using Microsoft.VisualBasic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace JAXBase.Utilities
{
    public class JAXLib
    {

        // Error setting & logging
        private static void SetError(int nErrNo, string sMessage)
        {
            throw new Exception(string.Format("{0}|{1}", nErrNo, sMessage));
        }



        // ---------------------------------------------------------------------
        // Some common time related functions
        //      Seconds() is seconds from midnight (local machine time)
        //      CtoT() tries to parse a string into a date, just not as well.
        //      DtoS() returns the date in a string like YYYYMMDD
        //      TtoC() returns similar values like VFP.  Expanded so 4 returns
        //          a SQL compatible datetime string, and 5 returns just the
        //          date in the format yyyy-MM-dd
        //
        // These functions should only be used on code running on a local
        // workstation, otherwise you'll be picking up the time from a
        // machine that may have a different timezone than the user.  The
        // routines do NOT adjust for timezones.
        // ---------------------------------------------------------------------
        public static int Seconds() { return TimeLib.SecondsSinceMidnightLocal(); }
        public static int SecondsUTC() { return TimeLib.SecondsSinceMidnightUTC(); }

        public static DateTime CtoT(string str)
        {
            DateTime? dt = TimeLib.CToT(str, true);
            return dt is null ? DateTime.MinValue : (DateTime)dt;
        }

        public static string DtoS(DateTime dt) { return TimeLib.DToS(dt); }
        public static string TtoC(DateTime dt, int flag)
        {
            string slReturn = flag switch
            {
                // Time only
                2 => dt.ToString("HH:mm:ss"),
                // Indexable datetime
                3 => dt.ToString("yyyy-MM-ddTHH:mm:ss"),
                // SQL compatible datetime
                4 => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK"),
                // Just the date
                5 => dt.ToString("yyyy-MM-dd"),
                // all numbers
                _ => dt.ToString("yyyyMMddHHmmss"),
            };

            return slReturn;
        }

        public static string ChrTran(string source, string charsFrom, string charsTo)
        {
            string sResult = source;
            for (int i = 0; i < charsFrom.Length; i++)
            {
                char c = charsTo.Length > i ? charsFrom[i] : '\0';
                sResult = sResult.Replace(charsFrom[i].ToString(), c == '\0' ? string.Empty : c.ToString());
            }

            return sResult;
        }

        // Search string1 for string2 [replace with string3] [at starting occurence] [for number of occurences] [with flags]
        // Might be able to do this with Regex but the docs aren't meant for mere mortals
        public static string StrTran(string sourceText, string findText, string replaceWithText, int startOccurence, int numberOfOccurences, int flags)
        {
            string sResult = sourceText;
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;   // used to get proper/title case

            startOccurence = startOccurence < 1 ? 1 : startOccurence;   // always going to do at least one
            numberOfOccurences = numberOfOccurences < 1 ? sourceText.Length : numberOfOccurences;

            int startIdx = 0;

            while (startOccurence-- > 1)
                startIdx = ((flags == 1 || flags == 3) ? sourceText.IndexOf(findText, startIdx, StringComparison.OrdinalIgnoreCase) : sourceText.IndexOf(findText, startIdx)) + 1;

            if (startIdx >= 0)
            {
                while (numberOfOccurences > 0)
                {
                    // look for this occurence
                    startIdx = (flags == 1 || flags == 3) ? sResult.IndexOf(findText, startIdx, StringComparison.OrdinalIgnoreCase) : sResult.IndexOf(findText, startIdx);

                    if (startIdx >= 0)
                    {
                        // 0 or higher is found at this index
                        if (flags > 1)
                        {
                            // Replacement is performed with the case of cReplacement changed to match the case of the string found. 
                            string chkString = sResult.Substring(startIdx, findText.Length);

                            if (chkString.ToUpper() == chkString)
                                replaceWithText = replaceWithText.ToUpper();            // make upper case
                            else if (chkString.ToLower() == chkString)
                                replaceWithText = replaceWithText.ToLower();            // make lower case
                            else if (chkString.Equals(myTI.ToTitleCase(chkString)))
                                replaceWithText = myTI.ToTitleCase(replaceWithText);    // make US proper/title case
                        }

                        // do the replacement
                        sResult = sResult[..startIdx] + replaceWithText + sResult[(startIdx + findText.Length)..];
                    }
                    else
                        break;  // no more occurences

                    numberOfOccurences--;
                }
            }

            return sResult;
        }


        // Perform the StrTran replacement on the nth occurence
        // if it exists.  If not, just return the original text.
        public static string StrTran2(string sourceText, string findText, string replaceWithText, int startAt, int flags)
        {
            string sResult = sourceText;
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;   // used to get proper/title case

            // Do the search based on comparison choice
            int idx = flags < 2 ? sourceText.IndexOf(findText, startAt, sourceText.Length - startAt, StringComparison.OrdinalIgnoreCase) : sourceText.IndexOf(findText, 0, startAt);

            // 0 or higher is found at this index
            if (idx >= 0)
            {
                if (flags > 1)
                {
                    // Replacement is performed with the case of cReplacement changed to match the case of the string found. 
                    string chkString = sourceText.Substring(idx, findText.Length);

                    if (chkString.ToUpper() == chkString)
                        replaceWithText = replaceWithText.ToUpper();            // make upper case
                    else if (chkString.ToLower() == chkString)
                        replaceWithText = replaceWithText.ToLower();            // make lower case
                    else if (chkString.Equals(myTI.ToTitleCase(chkString)))
                        replaceWithText = myTI.ToTitleCase(replaceWithText);    // make US proper/title case
                }

                // do the replacement
                sResult = sourceText[..idx] + replaceWithText + sourceText[(idx + findText.Length + 1)..];
            }

            return sResult;
        }



        // ---------------------------------------------------------------------
        // These left/right functions are similar but are 0 based so that they
        // are c# compatible.  They simply add range checkign and return
        // expected responses if you are out of bounds.
        //
        // Your length value must be 1 or greater else you will get an
        // empty string as the return value
        //
        // Left returns the full string if you greater than the length
        // Right returns the full string if you send a longer length value
        // ---------------------------------------------------------------------
        public static string Left(string str, int maxLength) { return maxLength < 0 ? string.Empty : str?[0..System.Math.Min(str.Length, maxLength)] ?? string.Empty; }
        public static string Right(string sValue, int iMaxLength)
        {
            //Check if the value is valid
            if (string.IsNullOrEmpty(sValue) || iMaxLength < 1)
            {
                //Set valid empty string as string could be null
                sValue = string.Empty;
            }
            else if (sValue.Length > iMaxLength)
            {
                //Make the string no longer than the max length
                sValue = sValue.Substring(sValue.Length - iMaxLength, iMaxLength);
            }

            //Return the string
            return sValue;
        }

        // Add a backslash to the path if it needs it
        public static string Addbs(string path)
        {
            string sResult = path.Trim();
            return sResult.Length > 0 ? (sResult[^1].Equals('\\') ? sResult : sResult + "\\") : string.Empty;
        }

        // ---------------------------------------------------------------------
        // Alltrim function trims a string of white space and optionally
        // of other strings front and back
        // ---------------------------------------------------------------------
        public static string Alltrim(params string[] args)
        {
            string slReturn = args[0].Trim();

            bool fixing = true;
            while (fixing)
            {
                fixing = false;

                for (int i = 1; i < args.Length; i++)
                {
                    if (slReturn.StartsWith(args[i]))
                    {
                        slReturn = slReturn[args[i].Length..].Trim();
                        fixing = true;
                    }

                    if (slReturn.EndsWith(args[i]))
                    {
                        slReturn = slReturn[..^args[i].Length].Trim();
                        fixing = true;
                    }
                }
            }

            return slReturn;
        }


        // ---------------------------------------------------------------------
        // Trim function trims a string of white space and optionally
        // of other strings on the back end
        // ---------------------------------------------------------------------
        public static string Trim(params string[] args)
        {
            string slReturn = args[0].TrimEnd();

            bool fixing = true;
            while (fixing)
            {
                fixing = false;

                for (int i = 1; i < args.Length; i++)
                {
                    if (slReturn.EndsWith(args[i]))
                    {
                        slReturn = slReturn[..^args[i].Length].TrimEnd();
                        fixing = true;
                    }
                }
            }

            return slReturn;
        }


        // ---------------------------------------------------------------------
        // ---------------------------------------------------------------------
        public static string Ltrim(params string[] args)
        {
            string slReturn = args[0].TrimStart();

            bool fixing = true;
            while (fixing)
            {
                fixing = false;

                for (int i = 1; i < args.Length; i++)
                {
                    if (slReturn.StartsWith(args[i]))
                    {
                        slReturn = slReturn[args[i].Length..].TrimStart();
                        fixing = true;
                    }
                }
            }

            return slReturn;
        }


        // ---------------------------------------------------------------------
        // InList function returns true if first parameter is found
        // in the list of the following parameters.
        // Included variations for string, int, double, float, and long
        // ---------------------------------------------------------------------
        public static bool InList(params object[] args)
        {
            bool llReturn = false;
            object args0 = args[0];

            try
            {
                if (args0 != null)
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        if (args0.GetType() == typeof(string))
                            llReturn = args0.ToString()!.Equals(args[i].ToString()!);
                        else if (args[0].GetType() == typeof(double))
                            llReturn = (double)args[0] == (double)args[i];
                        else if (args[0].GetType() == typeof(bool))
                            llReturn = (bool)args[0] == (bool)args[i];
                        else if (args[0].GetType() == typeof(DateTime))
                            llReturn = (DateTime)args[0] == (DateTime)args[i];
                        else if (args[0].GetType() == typeof(char))
                            llReturn = (char)args[0] == (char)args[i];
                        else if (args[0].GetType() == typeof(int))
                            llReturn = (int)args[0] == (int)args[i];
                        else if (args[0].GetType() == typeof(long))
                            llReturn = (long)args[0] == (long)args[i];
                        else if (args[0].GetType() == typeof(float))
                            llReturn = (float)args[0] == (float)args[i];
                        else if (args[0].GetType() == typeof(decimal))
                            llReturn = (decimal)args[0] == (decimal)args[i];

                        if (llReturn) break;
                    }
                }
            }
            catch (Exception ex)
            {
                llReturn = false;
                SetError(11, "|" + ex.Message);
            }

            return llReturn;
        }


        // ---------------------------------------------------------------------
        // Enhanced InList function to ignore case of string comparison
        // ---------------------------------------------------------------------
        public static bool InListC(params string[] args)
        {
            bool llReturn = false;
            string slSearchFor = args[0].ToString();

            for (int i = 1; i < args.Length; i++)
            {
                if (slSearchFor.Equals(args[i].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    llReturn = true;
                    break;
                }
            }

            return llReturn;
        }



        // ---------------------------------------------------------------------
        // I always felt this was a handy function since it would handle 
        // the different types transparently
        //
        // Returns true if the obj is between lower and upper inclusively
        // ---------------------------------------------------------------------
        public static bool Between(object obj, object lower, object upper)
        {
            bool llReturn = false;

            try
            {
                if (obj.GetType() == typeof(string))
                    llReturn = (string.Compare(obj.ToString(), lower.ToString()) >= 0 && string.Compare(obj.ToString(), upper.ToString()) <= 0);

                if (obj.GetType() == typeof(char))
                    llReturn = (char)obj >= (char)lower && (char)obj <= (char)upper;

                if (obj.GetType() == typeof(int))
                    llReturn = ((int)obj >= (int)lower && (int)obj <= (int)upper);

                if (obj.GetType() == typeof(long))
                    llReturn = ((long)obj >= (long)lower && (long)obj <= (long)upper);

                if (obj.GetType() == typeof(float))
                    llReturn = ((float)obj >= (float)lower && (float)obj <= (float)upper);

                if (obj.GetType() == typeof(double))
                    llReturn = ((double)obj >= (double)lower && (double)obj <= (double)upper);

                if (obj.GetType() == typeof(decimal))
                    llReturn = ((decimal)obj >= (decimal)lower && (decimal)obj <= (decimal)upper);

                if (obj.GetType() == typeof(DateTime))
                    llReturn = (DateTime)obj >= (DateTime)lower && (DateTime)obj <= (DateTime)upper;

                if (obj.GetType() == typeof(DateTimeOffset))
                    llReturn = (DateTimeOffset)obj >= (DateTimeOffset)lower && (DateTimeOffset)obj <= (DateTimeOffset)upper;

                if (obj.GetType() == typeof(DateOnly))
                    llReturn = (DateOnly)obj >= (DateOnly)lower && (DateOnly)obj <= (DateOnly)upper;

                if (obj.GetType() == typeof(TimeSpan))
                    llReturn = (TimeSpan)obj >= (TimeSpan)lower && (TimeSpan)obj <= (TimeSpan)upper;
            }
            catch (Exception ex)
            {
                llReturn = false;
                SetError(9999, ex.Message);
            }

            return llReturn;
        }


        // Standard XBase Proper function
        public static string Proper(string str)
        {
            return Proper(str, false);
        }

        // Enhanced Proper function with FileProper option
        public static string Proper(string str, bool FileProper)
        {
            bool wasspace = true;
            Regex rg = new Regex(@"^[a-zA-Z0-9\s,]*$");

            string slReturn = string.Empty;
            for (int i = 0; i < str.Length; i++)
            {

                // Are we at a space or non-alphanumeric character?
                if (str[i].Equals(' ') || (FileProper && rg.IsMatch(str[i].ToString()) == false))
                {
                    wasspace = true;    // Remember so we can deal with the next alphanumeric character
                    slReturn += str[i].ToString();
                }
                else
                {
                    // If last character was a space, capitalize this one
                    if (wasspace)
                        slReturn += str[i].ToString().ToUpper();
                    else
                        slReturn += str[i].ToString();

                    wasspace = false;
                }
            }

            return slReturn;
        }

        /* ---------------------------------------------------------------------
         * Look for searchFor in  the searchIn string but it cannot be between
         * quotes, brackets, or parens.  Return position (0 based) or -1
         * if not found.
         * 
         * Keywords are space delimited, no exceptions.
         * 
         * If the current search position is 0 then you add a space to the
         * end of the keyword, otherwise a space needs to be added to the
         * front and back of the keyword.
         * ---------------------------------------------------------------------*/
        public static int FindKeyword(string searchFor, string searchIn, int padMe)
        {
            int result = -1;
            char inQuote = '\0';
            int parenCount = 0;

            for (int i = 0; i < searchIn.Length; i++)
            {
                if (parenCount == 0)
                {
                    if (inQuote == searchIn[i])
                    {
                        inQuote = '\0';
                    }
                    else if ("['\"".Contains(searchIn[i]))
                    {
                        switch (searchIn[i])
                        {
                            case '[':
                                inQuote = ']';
                                break;

                            default:
                                inQuote = searchIn[i];
                                break;
                        }
                    }
                }

                if (inQuote == '\0')
                {
                    if (searchIn[i] == '(') parenCount++;
                    if (searchIn[i] == ')') parenCount--;

                    if (parenCount == 0)
                    {
                        // create the string to search for with appropriate spacing
                        // where padMe=1 or 3 for before and padMe=2 or 3 for after
                        string srch4 = searchFor + ((padMe & 1) > 0 ? " " : string.Empty);

                        // Pad after?
                        if (i > 0 && padMe > 1)
                            srch4 = " " + srch4;

                        if (i + srch4.Length < searchIn.Length)
                        {
                            if (searchIn[i..(i + srch4.Length)].Equals(srch4, StringComparison.OrdinalIgnoreCase))
                            {
                                result = i;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        // ---------------------------------------------------------------------
        // Strextract function
        //
        // StrExtract("FileNo_DocID_FileName","_") = "FileNo"
        // StrExtract("FileNo_DocID_FileName","_","_") = "DocID"
        // StrExtract("FileNo_DocID_FileName","_","",2) = "FileNo"
        //
        // ---------------------------------------------------------------------
        public static string StrExtract(string sourceText, string Token1)
        {
            return StrExtract(sourceText, Token1, string.Empty, 1);
        }

        public static string StrExtract(string sourceText, string Token1, string Token2)
        {
            return StrExtract(sourceText, Token1, Token2, 1);
        }

        public static string StrExtract(string sourceText, string Token1, string Token2, int occurance)
        {
            int Token1Start = 0;

            if (string.IsNullOrEmpty(Token1))
                Token1Start = 0;
            else
            {
                if (occurance == 0)
                    Token1Start = sourceText.IndexOf(Token1, Token1Start) + 1;
                else
                {
                    for (int i = 0; i < occurance; i++)
                        Token1Start = sourceText.IndexOf(Token1, Token1Start) + 1;
                }
            }

            int Token2Start = sourceText.Length;
            if (!string.IsNullOrEmpty(Token2))
                Token2Start = sourceText.IndexOf(Token2, Token1Start);

            // break out the substring
            string sub;

            if (Token2Start < 0)
            {
                if (Token1Start >= 0)
                    sub = sourceText[Token1Start..];
                else
                    sub = string.Empty;
            }
            else
            {
                if (Token1Start < 0)
                    sub = string.Empty;
                else
                    sub = sourceText[Token1Start..Token2Start];
            }

            return sub;
        }


        public static string AddBackSlash(string path)
        {
            path = path.Trim().TrimEnd('\\');
            return path + "\\";
        }

        // ****************************************************************************
        // ****************************************************************************
        // GetPath equivalent & GetFullPath adds \ to end of path
        // ============================================================================
        public static string JustFullPath(string fileName)
        {
            string sPath = JustPath(fileName);
            return sPath + (sPath == string.Empty ? string.Empty : @"\");
        }

        public static string JustPath(string fileName)
        {
            string? sTryPath = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName) == false)
            {
                try { sTryPath = System.IO.Path.GetDirectoryName(fileName); }
                catch (ArgumentException ex) { SetError(1220, ex.Message); }
                catch (PathTooLongException ex) { SetError(202, ex.Message); }
                catch (Exception ex) { SetError(9999, ex.Message); }
            }
            return sTryPath ?? string.Empty;
        }


        // ****************************************************************************
        // ****************************************************************************
        // GetFName equivalent
        // ============================================================================
        public static string JustFName(string fileName)
        {
            string? sTryName = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName) == false)
            {
                try { sTryName = System.IO.Path.GetFileName(fileName); }
                catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
                catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
                catch (NotSupportedException ex) { SetError(333, ex.Message); }
                catch (ArgumentException ex) { SetError(1220, ex.Message); }
                catch (PathTooLongException ex) { SetError(202, ex.Message); }
                catch (Exception ex) { SetError(9999, ex.Message); }
            }

            return sTryName ?? string.Empty;
        }


        // ****************************************************************************
        // ****************************************************************************
        // GetExt equivalent
        // ============================================================================
        public static string JustExt(string fileName)
        {
            string? sTryExt = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName) == false)
            {
                try { sTryExt = System.IO.Path.GetExtension(fileName); }
                catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
                catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
                catch (NotSupportedException ex) { SetError(333, ex.Message); }
                catch (ArgumentException ex) { SetError(1220, ex.Message); }
                catch (PathTooLongException ex) { SetError(202, ex.Message); }
                catch (Exception ex) { SetError(9999, ex.Message); }
            }

            return string.IsNullOrEmpty(sTryExt) ? string.Empty : (sTryExt[0] == '.' ? sTryExt[1..] : sTryExt);
        }


        /******************************************************************************
         ******************************************************************************
         * GetStem equivalent 
         *============================================================================*/
        public static string JustStem(string fileName)
        {
            string? sTryStem = string.Empty;

            if (string.IsNullOrWhiteSpace(fileName) == false)
            {
                try { sTryStem = System.IO.Path.GetFileNameWithoutExtension(fileName); }
                catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
                catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
                catch (NotSupportedException ex) { SetError(333, ex.Message); }
                catch (ArgumentException ex) { SetError(1220, ex.Message); }
                catch (PathTooLongException ex) { SetError(202, ex.Message); }
                catch (Exception ex) { SetError(9999, ex.Message); }
            }
            return sTryStem ?? "";
        }

        /*
         * Used to make sure a file being written out has a fully
         * qualified path by either using the FQP it aleady has
         * or adding the default path to it.
         */
        public static string FixFilePath(string fileName, string defPath)
        {
            string result = string.Empty;
            string path = JustPath(fileName);

            switch (path.Length)
            {
                case 0:
                    result = defPath + fileName;
                    break;

                case 1:
                    if (path == "\\")
                    {
                        if (defPath.Length > 2 && defPath[1] == ':')
                            result = defPath[..2] + fileName;
                        else
                        {
                            if (defPath.Length > 2 && defPath[..2] == "\\\\")
                            {
                                // Find the third backslash
                                int f = defPath.IndexOf('\\', 3);
                                if (f > 0)
                                    result = defPath[..f] + fileName;
                                else
                                    throw new Exception($"9999||Default path is {defPath}");
                            }
                            else
                                throw new Exception($"9999||Default path is {defPath}");
                        }
                    }
                    break;

                default:
                    // there is a path to deal with
                    if (path[0] == '.')
                    {
                        // Expecting path to start with .\ or ..\
                        result = defPath + fileName;
                    }
                    else if ((path[..2] == "\\\\" || path[1] == ':') == false)
                    {
                        // It's not a stand-alone path, so if it starts with 
                        // a backslash, we get the root of the default path
                        if (path[0] == '\\')
                        {
                            if (defPath.Length > 2 && defPath[1] == ':')
                                result = defPath[..2] + fileName;
                            else
                            {
                                if (defPath.Length > 2 && defPath[..2] == "\\\\")
                                {
                                    // Find the third backslash
                                    int f = defPath.IndexOf('\\', 3);
                                    if (f > 0)
                                        result = defPath[..f] + fileName;
                                    else
                                        throw new Exception($"9999||Default path is {defPath}");
                                }
                                else
                                    throw new Exception($"9999||Default path is {defPath}");
                            }
                        }
                    }
                    else
                        result = fileName;  // It's fine as-is
                    break;
            }

            return result;
        }


        /******************************************************************************
         ******************************************************************************
         * StrToFile equivalent with FLAG enhancement:
         *  0 = Overwrite file
         *  1 = Append to file
         *  2 = Overwrite but add NewLine to end of text
         *  3 = Append to file but add NewLine to end of text
         *
         * This allows easier control for creating log files and other internal
         * text files created and used by JAXBase.
         *============================================================================*/
        public static int StrToFile(string text, string fileName, int flag)
        {
            int result = text.Length;

            try
            {
                text += (flag > 1 ? Environment.NewLine : string.Empty);

                if (flag == 0 || flag == 2 || !File.Exists(fileName))
                    File.WriteAllText(fileName, text);
                else
                    using (StreamWriter sw = File.AppendText(fileName)) { sw.Write(text); }
            }
            catch (DirectoryNotFoundException ex) { result = -1; SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { result = -1; SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { result = -1; SetError(333, ex.Message); }
            catch (ArgumentException ex) { result = -1; SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { result = -1; SetError(202, ex.Message); }
            catch (Exception ex) { result = -1; SetError(9999, ex.Message); }

            return result;
        }


        // ****************************************************************************
        // ****************************************************************************
        // FileToStr equivalent
        // ============================================================================
        public static string FileToStr(string fileName)
        {
            string readText = string.Empty;

            try { readText = File.ReadAllText(fileName); }
            catch (DirectoryNotFoundException ex) { SetError(1963, ex.Message); }
            catch (UnauthorizedAccessException ex) { SetError(1705, ex.Message); }
            catch (NotSupportedException ex) { SetError(333, ex.Message); }
            catch (ArgumentException ex) { SetError(1220, ex.Message); }
            catch (PathTooLongException ex) { SetError(202, ex.Message); }
            catch (Exception ex) { SetError(9999, ex.Message); }

            return readText;
        }

        // Transform a tokenized value to a string
        // Not overly sophisticated and will not work exactly like VFP if you
        // mess around with weird combinations
        //
        public static string JAXFormatToNet_ConvertNumber(object nVal, string jaxTransformString)
        {
            if (double.TryParse(nVal.ToString(), out double dval) == false) dval = 0.00;
            string netString = string.Empty;
            char[] cTrans = jaxTransformString.ToCharArray();
            string leadChars = string.Empty;

            // look for leading @ codes
            // Format must be "@x@x@x" else it skips
            bool leftJustify = false;
            bool padZero = false;
            bool zeroToSpace = false;
            bool negativeDB = false;
            bool negativeInParen = false;
            bool toScientificNotation = false;
            bool toHex = false;
            bool addCurrency = false;

            try
            {
                int j = 0;
                while (true)
                {
                    if (j + 1 < cTrans.Length && cTrans[j].Equals('@') && "BLXZ(^0$".Contains(cTrans[j + 1].ToString().ToUpper()))
                    {
                        j++;

                        // @B left justifies
                        if (cTrans[j].ToString().Equals("B", StringComparison.OrdinalIgnoreCase)) leftJustify = true;

                        // @L pads numeric with leading zeros
                        if (cTrans[j].ToString().Equals("L", StringComparison.OrdinalIgnoreCase)) padZero = true;

                        // @X appends DB to negative values
                        if (cTrans[j].ToString().Equals("X", StringComparison.OrdinalIgnoreCase)) negativeDB = true;

                        // @Z convert 0 value to spaces
                        if (cTrans[j].ToString().Equals("Z", StringComparison.OrdinalIgnoreCase)) zeroToSpace = true;

                        // @( enclose negative values with ()
                        if (cTrans[j].ToString().Equals("(")) negativeInParen = true;

                        // @^ convert to scientific notation
                        if (cTrans[j].ToString().Equals("^")) toScientificNotation = true;

                        // @0 convert to hex
                        if (cTrans[j].ToString().Equals("0")) toHex = true;

                        // @$ add set currency character to value
                        if (cTrans[j].ToString().Equals("$")) addCurrency = true;

                        j++;
                    }
                    else
                        break;
                }

                // if a @ code was found, skip spaces
                while (j > 0 && j < cTrans.Length && cTrans[j].Equals(' '))
                    j++;

                // look for leading non-format characters
                for (int i = j; i < cTrans.Length; i++)
                {
                    if ("9X!Y.".Contains(cTrans[j]))
                    {
                        j = i;
                        break;
                    }
                    else
                    {
                        if (cTrans[j].Equals(',') == false)
                            leadChars += cTrans[j];
                    }
                }

                /*                // look for leading 0 format and change char 0 to space
                                // until the first non-zero result in each
                                for (int i = j; i < cTrans.Length; i++)
                                {
                                    if (cTrans[i].Equals('0') && cResult[i].Equals('0'))
                                        cResult[i] = ' ';
                                    else
                                        break;
                                }
                */

                // Extract the format string
                string format = string.Empty;
                for (int i = j; i < cTrans.Length; i++)
                    format += cTrans[i];

                // Change the 9's to 0's
                format = format.Replace("9", "0");

                // Translate the number to a string
                netString = dval.ToString(format);
                cTrans = netString.ToCharArray();

                // Blank out leading commas and zeros
                for (int i = 0; i < cTrans.Length-1; i++)
                {
                    if (" ,0".Contains(cTrans[i]))
                        cTrans[i] = ' ';
                    else
                        break;
                }

                netString = new string(cTrans);

                // Process @ codes
                if (dval < 0 && negativeDB) netString += "DB";
                if (dval < 0 && negativeInParen) netString = string.Format("({0})", netString);
                if (dval == 0 && zeroToSpace) netString = new string(' ', netString.Length);

                if (addCurrency)
                {
                    netString = netString.Trim();
                    netString = (new string(' ', 15) + "$" + netString)[^13..];
                }


                if (padZero)
                {
                    // Zero padding
                    netString = ((int)dval).ToString("0000000000") + dval.ToString("0.00")[^3..];
                }
                else
                {
                    // kill leading zeros
                    cTrans = netString.ToCharArray();
                    for (int i = 0; i < cTrans.Length-1; i++)
                    {
                        if (cTrans[i].Equals('0'))
                            cTrans[i] = ' ';
                        else
                            break;
                    }
                    netString = new string(cTrans);

                }

                if (leftJustify)
                {
                    int i = netString.Length;
                    netString = (Ltrim(netString) + new string(' ', i))[..i];
                }

                if (toScientificNotation)
                {
                    // Scientific Notation overrides everything above
                    netString = dval.ToString("E7");
                    if (int.TryParse(netString[12..], out int i) == false) i = 0;
                    netString = netString[..11] + i.ToString();
                }

                if (toHex)
                {
                    // Hex conversion overrides everything else
                    int i = (int)dval;
                    netString = i.ToString("X");
                    switch (8.CompareTo(netString.Length))
                    {
                        case 0: // 8 chars
                            netString = "0x" + netString;
                            break;

                        case 1:
                            netString = "0x" + new string('0', 8 - netString.Length) + netString;
                            break;

                        default:
                            netString = "0x00000000";
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                // Something went weird
                leadChars = "";
                netString = "ERROR: " + ex.Message;
            }

            return leadChars + netString;
        }


        public static string JAXFormatToNet_ConvertString(string sVal, string jaxTransformString)
        {
            string netString = string.Empty;
            char[] cTrans = jaxTransformString.ToCharArray();
            string leadChars = string.Empty;

            // look for leading @ codes
            // Format must be "@x@x@x" else it skips
            bool leftJustify = false;

            try
            {
                int j = 0;
                while (true)
                {
                    if (j + 1 < cTrans.Length && cTrans[j].Equals('@') && "L".Contains(cTrans[j + 1].ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        j++;

                        // @B left justifies
                        if (cTrans[j].ToString().Equals("B", StringComparison.OrdinalIgnoreCase)) leftJustify = true;

                        j++;
                    }
                    else
                        break;
                }

                // if a @ code was found, skip spaces
                while (j > 0 && j < cTrans.Length && cTrans[j].Equals(' '))
                    j++;

                // look for leading non-format characters
                for (int i = j; i < cTrans.Length; i++)
                {
                    if ("WUA9X!.".Contains(cTrans[j]))
                    {
                        j = i;
                        break;
                    }
                    else
                    {
                        // Add the non-format character to the lead character string
                        leadChars += cTrans[j];
                    }
                }

                // Extract the format string and process sVal into it
                string format = string.Empty;
                char[] cval = sVal.ToCharArray();

                int k = 0;
                for (int i = j; i < cTrans.Length; i++)
                {
                    char c = k < cval.Length ? cval[k] : ' ';

                    switch (cTrans[i])
                    {
                        case 'W':
                            if (Between(c.ToString().ToUpper(), 'A', 'Z'))
                                netString += c.ToString().ToLower();
                            break;

                        case 'U':
                            if (Between(c.ToString().ToUpper(), 'A', 'Z'))
                                netString += c.ToString().ToUpper();
                            break;

                        case 'A':
                            if (Between(c.ToString().ToUpper(), 'A', 'Z'))
                                netString += c.ToString();
                            break;

                        case '9':
                            k++;
                            if ("0123456789+-".Contains(c))
                                netString += c.ToString();
                            break;

                        case 'X':
                            k++;
                            netString += c.ToString();
                            break;

                        case '!':
                            k++;
                            netString += c.ToString().ToUpper();
                            break;

                        default:
                            netString += cTrans[i].ToString();
                            break;
                    }
                }

                if (leftJustify)
                {
                    int i = netString.Length;
                    netString = (Ltrim(netString) + new string(' ', i))[..i];
                }
            }
            catch (Exception ex)
            {
                // Something went weird
                leadChars = "";
                netString = "ERROR: " + ex.Message;
            }

            return leadChars + netString;
        }

        public static string TextMerge(string sourceText, bool recurse, string lDelim, string rDelim, AppClass app)
        {
            string sResult = string.Empty;
            int i = 1;

            sourceText = sourceText.Replace(((char)10).ToString(), "");
            string[] aText = sourceText.Split((char)13);
            string line1 = aText[0].ToLower().Replace("  ", " ");
            int pretext = 0;
            string assing2var = string.Empty;

            if (line1.Contains(" to "))
                assing2var = StrExtract(line1, " to ", " ");

            if (line1.Contains(" pretext "))
                if (int.TryParse(StrExtract(line1, " pretext ", ""), out pretext) == false) pretext = 0;

            for (int j = 1; j < aText.Length; j++)
            {
                if ((pretext & 4) == 0 || string.IsNullOrEmpty(aText[j].Trim()) == false)  // No blank lines check
                {
                    if ((pretext & 2) > 0) // Eliminate leading tabs
                    {
                        string a = string.Empty;
                        for (int k = 0; k < aText[j].Length; k++)
                        {
                            if (aText[j][k].Equals("\t") == false)       // Toss the tabs
                            {
                                if (aText[j][k].Equals(" "))   // Keep the spaces
                                    a += aText[j][k];
                                else
                                {
                                    // Get the rest of the line
                                    for (int l = k; l < aText[j].Length; l++)
                                        a += aText[j][l];
                                    break;
                                }
                            }
                        }

                        aText[j] = a;
                    }

                    if ((pretext & 1) > 0) // Eliminate leading spaces
                    {
                        string a = string.Empty;
                        for (int k = 0; k < aText[j].Length; k++)
                        {
                            if (aText[j][k].Equals(" ") == false)   // Toss the spaces
                            {
                                if (aText[j][k].Equals("\t"))       // Keep the tabs
                                    a += aText[j][k];
                                else
                                {
                                    // Get the rest of the line
                                    for (int l = k; l < aText[j].Length; l++)
                                        a += aText[j][l];
                                    break;
                                }
                            }
                        }

                        aText[j] = a;
                    }

                    i = 1;
                    while (aText[j].Contains(lDelim) && aText[j].Contains(rDelim))
                    {
                        sResult = StrExtract(aText[j], "", lDelim, i);
                        string var = StrExtract(aText[j], lDelim, rDelim, i++);
                        sResult += AppVars.GetVarToken(var[1..]);
                    }

                    if (i > 1)
                        sResult += StrExtract(aText[j], rDelim, "", --i);
                    else
                        sResult = aText[j] + "\r" + ((pretext & 8) > 0 ? string.Empty : "\n");
                }
            }

            if (string.IsNullOrEmpty(assing2var) == false)
                AppVars.SetVar(assing2var, sResult, 1, 1);

            return sResult;
        }

        /*
         * Third time's a charm with Grok.  Just have to keep asking.
         */
        public static class InputBox
        {
            /// <summary>
            /// Shows a modal input box that emulates Visual FoxPro's INPUTBOX() exactly
            /// </summary>
            /// <param name="prompt">Message displayed to the user</param>
            /// <param name="title">Dialog title (if empty, uses Application.ProductName)</param>
            /// <param name="defaultValue">Default text, or "*" to show password input (no echo)</param>
            /// <param name="timeout">Timeout in milliseconds (0 = no timeout)</param>
            /// <param name="timeoutValue">Value returned on timeout</param>
            /// <param name="cancelValue">Value returned on Cancel/close</param>
            /// <returns>User input, timeoutValue or cancelValue</returns>
            public static string Show(string prompt, string title, string defaultValue, int timeout, string timeoutValue, string cancelValue)
            {
                string result = cancelValue; // Default = cancel value (VFP returns "" on cancel unless overridden)

                var form = new Form();
                var label = new Label();
                var textBox = new TextBox();
                var btnOk = new Button();
                var btnCancel = new Button();

                try
                {
                    // Form setup - clean and VFP-like
                    form.Text = string.IsNullOrEmpty(title) ? Application.ProductName : title;
                    form.FormBorderStyle = FormBorderStyle.FixedDialog;
                    form.MaximizeBox = false;
                    form.MinimizeBox = false;
                    form.StartPosition = FormStartPosition.CenterScreen;
                    form.AcceptButton = btnOk;
                    form.CancelButton = btnCancel;
                    form.TopMost = true;
                    form.Width = 420;
                    form.Height = 160;
                    form.Font = SystemFonts.MessageBoxFont;
                    form.Padding = new Padding(10);

                    // Prompt
                    label.Text = prompt ?? "";
                    label.AutoSize = false;
                    label.Dock = DockStyle.Top;
                    label.Height = 50;
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    form.Controls.Add(label);

                    // TextBox - with VFP password star support
                    textBox.Dock = DockStyle.Top;
                    textBox.Height = 28;
                    textBox.Margin = new Padding(0, 5, 0, 10);

                    if (defaultValue == "*")
                    {
                        textBox.PasswordChar = '*';  // Mask input like VFP
                        textBox.Text = "";           // No default text in password mode
                        textBox.UseSystemPasswordChar = false; // Ensures '*' is used, not bullet
                    }
                    else
                    {
                        textBox.Text = defaultValue ?? "";
                    }

                    textBox.SelectAll();
                    form.Controls.Add(textBox);

                    // Buttons panel
                    var panel = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        Dock = DockStyle.Bottom,
                        Height = 45,
                        Padding = new Padding(0, 5, 10, 10)
                    };

                    btnCancel.Text = "&Cancel";
                    btnCancel.DialogResult = DialogResult.Cancel;
                    btnCancel.Width = 90;

                    btnOk.Text = "&OK";
                    btnOk.DialogResult = DialogResult.OK;
                    btnOk.Width = 90;

                    panel.Controls.Add(btnCancel);
                    panel.Controls.Add(btnOk);
                    form.Controls.Add(panel);

                    // Button actions
                    btnOk.Click += (s, e) =>
                    {
                        result = textBox.Text;
                        form.Close();
                    };

                    btnCancel.Click += (s, e) =>
                    {
                        result = cancelValue;
                        form.Close();
                    };

                    form.FormClosing += (s, e) => { result = cancelValue; }; // Esc or X button

                    // Timeout handling
                    System.Windows.Forms.Timer? timer = null;
                    if (timeout > 0)
                    {
                        timer = new System.Windows.Forms.Timer { Interval = timeout };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            result = timeoutValue;
                            if (!form.IsDisposed)
                                form.Close();
                        };
                        timer.Start();

                        form.FormClosed += (s, e) => timer?.Dispose();
                    }

                    // Focus the textbox when shown
                    form.Shown += (s, e) => textBox.Focus();

                    // Run on STA thread and wait
                    var thread = new Thread(() =>
                    {
                        Application.Run(form);
                    });

                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();

                    return result;
                }
                finally
                {
                    form?.Dispose();
                }
            }

            // Overloads for convenience (just like VFP)
            public static string Show(string prompt) => Show(prompt, "", "", 0, "", "");
            public static string Show(string prompt, string title) => Show(prompt, title, "", 0, "", "");
            public static string Show(string prompt, string title, string defaultValue) => Show(prompt, title, defaultValue, 0, "", "");
            public static string Show(string prompt, string title, string defaultValue, int timeout) => Show(prompt, title, defaultValue, timeout, "", "");
            public static string Show(string prompt, string title, string defaultValue, int timeout, string timeoutvalue) => Show(prompt, title, defaultValue, timeout, timeoutvalue, "");
        }

        /*----------------------------------------------------------------------------*
         * Look for a file in the supplied path list array
         *----------------------------------------------------------------------------*/
        public static string FindFileInPathList(string[] pathList, string fileName)
        {
            string FQFN = string.Empty;

            foreach (string path in pathList)
            {
                FQFN = Addbs(path) + fileName;

                if (File.Exists(FQFN))
                    break;

                fileName = string.Empty;
            }

            return FQFN;
        }


        /*
         * Expanded upon code found at 
         *      https://stackoverflow.com/questions/14522540/how-to-close-a-messagebox-after-several-seconds
         *      Posted by DmitryG, modified by community. See post 'Timeline' for change history
         *      Retrieved 2025-12-08, License - CC BY-SA 4.0
         *
         */
        public class JAXMessageBox
        {
            System.Threading.Timer _timeoutTimer;
            string _caption;
            DialogResult _result = DialogResult.None;

            JAXMessageBox(string text, int boxType, string caption, int timeout)
            {

                MessageBoxButtons nButtons = (boxType % 16) switch
                {
                    1 => MessageBoxButtons.OKCancel,
                    2 => MessageBoxButtons.AbortRetryIgnore,
                    3 => MessageBoxButtons.YesNoCancel,
                    4 => MessageBoxButtons.YesNo,
                    5 => MessageBoxButtons.RetryCancel,
                    _ => MessageBoxButtons.OK
                };

                MessageBoxIcon nIcon = ((boxType % 256) / 16) switch
                {
                    2 => MessageBoxIcon.Question,
                    3 => MessageBoxIcon.Exclamation,
                    4 => MessageBoxIcon.Information,
                    _ => MessageBoxIcon.Stop
                };

                MessageBoxDefaultButton defaultButton = (boxType / 256) switch
                {
                    1 => MessageBoxDefaultButton.Button2,
                    2 => MessageBoxDefaultButton.Button3,
                    _ => MessageBoxDefaultButton.Button1
                };


                _caption = caption;
                _timeoutTimer = new System.Threading.Timer(OnTimerElapsed!, null, timeout, System.Threading.Timeout.Infinite);
                using (_timeoutTimer)
                    _result = MessageBox.Show(text, caption, nButtons, nIcon, defaultButton);
            }

            public static DialogResult Show(string text, int boxType, string caption, int timeout)
            {
                return new JAXMessageBox(text, boxType, caption, timeout)._result;
            }

            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow("#32770", _caption); // lpClassName is #32770 for MessageBox
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
                _result = DialogResult.None;
            }

            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }



        /// <summary>
        /// Exact emulation of Visual FoxPro PUTFILE()
        /// Syntax: PUTFILE([cCustomText] [, cFileName] [, cFileExtensions])
        /// Returns full path or "" on Cancel — exactly like VFP
        /// Works on Windows and Linux (.NET 6+ WinForms)
        /// </summary>
        public static string PutFile(string customText, string fileName, string fileExtensions)
        {
            using (var dlg = new SaveFileDialog())
            {
                // 1. Dialog title (cCustomText)
                if (!string.IsNullOrWhiteSpace(customText))
                    dlg.Title = customText.Trim();
                else
                    dlg.Title = "Save As";  // VFP default

                // 2. Pre-filled filename (cFileName)
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    string cleanName = fileName.Trim();
                    // VFP behavior: if full path, use filename only but set directory
                    dlg.FileName = System.IO.Path.GetFileName(cleanName);

                    string? dir = System.IO.Path.GetDirectoryName(cleanName);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        dlg.InitialDirectory = dir;
                }

                // 3. File extensions / filter (cFileExtensions)
                string filter = "All files (*.*)|*.*";
                string? defaultExt = "";
                if (!string.IsNullOrWhiteSpace(fileExtensions))
                {
                    string ext = fileExtensions.Trim();
                    if (ext.Contains('|') || ext.Contains(';'))
                    {
                        // Full filter string like "Text Files (*.txt)|*.txt|All Files (*.*)|*.*"
                        filter = ext.Replace(';', '|');  // Standardize to |
                    }
                    else
                    {
                        // Simple extension(s) like "txt" or "txt;doc"
                        filter = BuildFilter(ext);
                    }

                    // Set DefaultExt to first extension in filter
                    var firstExt = filter.Split('|').Skip(1).FirstOrDefault()?.TrimStart('*', '.');
                    if (!string.IsNullOrEmpty(firstExt))
                        defaultExt = firstExt.Split(';').FirstOrDefault()?.TrimStart('*', '.');
                }
                dlg.Filter = filter;
                dlg.FilterIndex = 1;
                if (!string.IsNullOrEmpty(defaultExt))
                    dlg.DefaultExt = defaultExt;

                // VFP always prompts on overwrite
                dlg.OverwritePrompt = true;
                dlg.CheckPathExists = true;
                dlg.RestoreDirectory = true;

                // Show dialog
                DialogResult result = dlg.ShowDialog();

                return (result == DialogResult.OK) ? dlg.FileName : "";
            }
        }

        // Builds WinForms filter string from VFP-style cFileExtensions
        private static string BuildFilter(string ext)
        {
            // If contains "|", treat as full filter string (desc|mask|desc|mask)
            // Standardize ";" to "|" if needed
            if (ext.Contains('|'))
            {
                return ext.Replace(';', '|');
            }

            // Else, extensions like "jpg;png;gif" — auto-generate descriptions
            var parts = ext.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(e => e.Trim().TrimStart('*', '.'))
                           .Where(e => !string.IsNullOrEmpty(e))
                           .Distinct()
                           .ToArray();

            if (parts.Length == 0)
                return "All files (*.*)|*.*";

            var filters = parts.Select(e =>
                $"{e.ToUpper()} files (*.{e})|*.{e}").ToList();

            // Add "All supported files" first
            string allPattern = string.Join(";", parts.Select(e => "*." + e));
            filters.Insert(0, $"All supported files|{allPattern}");

            // Add "All files" last — matches VFP behavior
            filters.Add("All files (*.*)|*.*");

            return string.Join("|", filters);
        }

        /// <summary>
        /// Exact Visual FoxPro GETDIR() emulation — .NET 6+
        /// Syntax: GETDIR([cDirectory] [, cText] [, cCaption] [, nFlags] [, lRootOnly])
        /// Returns folder path with trailing separator (e.g. "C:\MyFolder\" or "/home/user/Documents/")
        /// Returns "" on Cancel — exactly like VFP
        /// </summary>
        public static string GetDir(
            string cDirectory,
            string cText,
            string cCaption,
            int nFlags,
            bool lRootOnly)
        {
            // Linux / macOS: Use Ookii.Dialogs for beautiful native folder picker
            return GetDirCrossPlatform(cDirectory, cText ?? cCaption);
        }


        // TODO - Cross-platform directory picker
        private static string GetDirCrossPlatform(string initialDir, string title)
        {
            try
            {
                //using var dlg = new VistaFolderBrowserDialog
                //{
                //    Description = string.IsNullOrWhiteSpace(title) ? "Select a folder" : title.Trim(),
                //    ShowNewFolderButton = true
                //};

                //if (!string.IsNullOrWhiteSpace(initialDir))
                //{
                //    string path = initialDir.TrimEnd(System.IO.Path.DirectorySeparatorChar);
                //    if (Directory.Exists(path))
                //        dlg.SelectedPath = path;
                //}

                //if (dlg.ShowDialog() == DialogResult.OK && Directory.Exists(dlg.SelectedPath))
                //{
                //    string result = dlg.SelectedPath;
                //    return result.EndsWith(System.IO.Path.DirectorySeparatorChar)
                //        ? result
                //        : result + System.IO.Path.DirectorySeparatorChar;
                //}
            }
            catch
            {
                // Graceful fallback: simple input dialog
                string defaultPath = initialDir ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string input = Interaction.InputBox(
                    title ?? "Enter folder path:",
                    "Select Directory",
                    defaultPath);

                if (string.IsNullOrWhiteSpace(input) || !Directory.Exists(input))
                    return "";

                return input.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            }

            return "";
        }


        /// <summary>
        /// VFP-style GETDATE([dDate|tDateTime]) — Windows/Linux/macOS compatible (.NET 6+)
        /// - No parameter or DateTime → shows Date + Time picker
        /// - Date parameter → shows only Date picker (time fixed to 00:00:00)
        /// - Returns DateTime with time = midnight if input was Date type
        /// - Returns selected DateTime (with time) if DateTime or no param
        /// - Returns DateTime.MinValue on Cancel
        /// </summary>
        /// 

        /*
         * Linux/Mac version is ugly as hell - find something better!
         */
        public static DateTime GetDate(DateTime initial, bool isDateOnly)
        {
            bool isWindows = OperatingSystem.IsWindows();

            using var form = new Form
            {
                Text = isDateOnly ? "Select Date" : "Select Date and Time",
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(isWindows ? 225 : 280, isWindows ? (isDateOnly ? 260 : 260) : 200),
                Font = new Font("Segoe UI", 9F)
            };

            // Date selection control
            Control dateControl;
            if (isWindows)
            {
                // Windows: Use MonthCalendar for VFP-like date picker
                var monthCalendar = new MonthCalendar
                {
                    Location = new Point(15, 15),
                    MaxSelectionCount = 1
                };
                dateControl = monthCalendar;
            }
            else
            {
                // Linux/macOS: Use DateTimePicker with date-only format
                var datePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "MMMM dd, yyyy",
                    Width = 250,
                    Location = new Point(15, 20)
                };
                dateControl = datePicker;
            }

            // Time selection (hidden for date-only mode)
            DateTimePicker? timePicker = null;
            Label? lblTime = null;
            if (!isDateOnly)
            {
                timePicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Time,
                    ShowUpDown = true,
                    Width = isWindows ? 140 : 200,
                    Location = new Point(isWindows ? 15 : 15, isWindows ? dateControl.Bottom + 22 : 80)
                };
                lblTime = new Label
                {
                    Text = "Time:",
                    AutoSize = true,
                    Location = new Point(isWindows ? 165 : 15, timePicker.Top + 3)
                };
            }

            // Buttons
            //int btnTop = isWindows ? (isDateOnly ? dateControl.Bottom + 20 : timePicker!.Bottom + 20) : 140;
            int btnTop = isWindows ? dateControl.Bottom + 50 : 140;

            var btnOK = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(45, btnTop),
                Width = 75
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(130, btnTop),
                Width = 75
            };

            // Add controls
            form.Controls.Add(dateControl);
            if (timePicker != null)
            {
                form.Controls.Add(timePicker);
                form.Controls.Add(lblTime);
            }
            form.Controls.Add(btnOK);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOK;
            form.CancelButton = btnCancel;

            // Set initial value
            DateTime now = DateTime.Now;
            DateTime selected = initial;
            if (isWindows)
            {
                ((MonthCalendar)dateControl).SetDate(selected.Date);
            }
            else
            {
                ((DateTimePicker)dateControl).Value = selected.Date;
            }
            if (timePicker != null)
            {
                timePicker.Value = selected;
            }

            // If Date-only mode, force time to midnight
            if (isDateOnly && timePicker != null)
            {
                timePicker.Value = selected.Date;
                timePicker.Visible = false;

                if (lblTime is not null)
                    lblTime.Visible = false;

                form.ClientSize = new Size(form.ClientSize.Width, dateControl.Bottom + 80);
            }

            form.Shown += (s, e) =>
            {
                if (isWindows)
                    ((MonthCalendar)dateControl).Focus();
                else
                    ((DateTimePicker)dateControl).Focus();
            };

            if (form.ShowDialog() != DialogResult.OK)
                return DateTime.MinValue; // Cancel

            DateTime result;
            if (isWindows)
            {
                result = ((MonthCalendar)dateControl).SelectionStart.Date;
            }
            else
            {
                result = ((DateTimePicker)dateControl).Value.Date;
            }

            if (!isDateOnly && timePicker != null)
            {
                result = result.Add(timePicker.Value.TimeOfDay);
            }

            // Final rule: if input was Date type → return with time = 00:00:00
            return result;
        }

        // More GROK supplied routines that I modified to make it work right.
        // Validates directory/file names for relative and absolute paths in
        // both Linux and Windows.
        /// <summary>
        /// Returns 0 if the path or path/file is a valid on Windows or Linux. 1 = not a valid path\file, 2 = other error
        /// </summary>
        /// <param name="path">The path to validate</param>
        /// <returns>0 if valid, 1 if not, 2 if error</returns>
        public static int IsValidPathFile(string path)
        {
            int result = 2;

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                try
                {
                    // Path.GetFullPath will throw if the path contains invalid characters
                    // or is malformed for the current OS - discard results
                    _ = Path.GetFullPath(path);

                    // Additional check: disallow paths that resolve to device names like CON, PRN, etc. on Windows
                    string fileName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    result = IsWindowsReservedName(fileName) == false ? 0 : 1;
                }
                catch
                {
                    // send back the error code
                    result = 2;
                }
            }

            return result;
        }

        // Windows reserved names (case-insensitive)
        private static readonly string[] ReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static bool IsWindowsReservedName(string name)
        {
            return OperatingSystem.IsWindows() && !string.IsNullOrEmpty(name) && ReservedNames.Contains(name.TrimEnd('.').ToUpperInvariant());
        }

        /// <summary>
        /// Distinguish between absolute and relative - returns 0 if absolute, 1 if it's not, 2 if an error
        /// </summary>
        /// <param name="path">the path to validate</param>
        /// <returns>0 if absolute, 1 if relative, 2 if error</returns>
        public static int IsAbsolutePath(string path)
        {
            int result = 1;

            if (string.IsNullOrWhiteSpace(path) == false)
            {
                try
                {
                    // .NET 5+ (works cross-platform)
                    result = Path.IsPathFullyQualified(path) ? 0 : 1;
                }
                catch
                {
                    // Send back the error code
                    result = 2;
                }
            }

            return result;
        }


        /*
         * Converted a portion of the code from stack overflow
         *  https://stackoverflow.com/questions/577411/how-can-i-find-the-state-of-numlock-capslock-and-scrolllock-in-net
         * 
         * TODO - TEST THIS
         */
        public static int NetInKeyToVFP(string key, int nKeyCode, System.Windows.Forms.Keys modifiers)
        {
            int result = 0;
            bool Shift = modifiers == Keys.Shift;
            bool Alt = modifiers == Keys.Alt;
            bool Ctrl = modifiers == Keys.Control;
            bool CapsLock= modifiers == Keys.CapsLock;

            // Keydown and KeyUp fire the events.  Only listen if key is down
            if (nKeyCode != 0)
            {
                // Is it Alt+A-Z or Alt+F1-12?
                if (Alt && !Shift && !Ctrl)
                {
                    if (JAXLib.Between(nKeyCode, 65, 90))
                    {
                        // A - Z
                        result = -(nKeyCode - 64);
                    }
                    else
                    {
                        // F1 - F12
                        result = nKeyCode switch
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
                
                if (result == 0)
                {
                    // Not a special alt key or one of the keypads, so try to figure it out
                    result = nKeyCode switch
                    {
                        8 =>Shift ? 127 : (Ctrl ? 127 : (Alt ? 14 : 127)),    // Backspace
                        9 =>Shift ? 15 : (Ctrl ? 148 : (Alt ? 0 : 9)),        // Tab
                        13 =>Alt ? 166 : (Ctrl ? 10 : 13),                    // Enter
                        27 =>Shift ? 50 : (Ctrl ? 27 : (Alt ? 1 : 27)),       // Esc
                        32 =>Shift ? 32 : (Ctrl ? 32 : (Alt ? 57 : 32)),      // Spacebar
                        48 =>Shift ? 41 : 48,                                 // 0
                        49 =>Shift ? 33 : 49,                                 // 1
                        50 =>Shift ? 64 : 50,                                 // 2
                        51 =>Shift ? 35 : 51,                                 // 3
                        52 =>Shift ? 36 : 52,                                 // 4
                        53 =>Shift ? 37 : 53,                                 // 5
                        54 =>Shift ? 94 : 54,                                 // 6
                        55 =>Shift ? 38 : 55,                                 // 7
                        56 =>Shift ? 42 : 56,                                 // 8
                        57 =>Shift ? 40 : 57,                                 // 9
                        112 =>Shift ? 84 : (Ctrl ? 94 : (Alt ? 104 : 28)),    // F1
                        113 =>Shift ? 86 : (Ctrl ? 95 : (Alt ? 104 : -1)),    // F2
                        114 =>Shift ? 87 : (Ctrl ? 96 : (Alt ? 105 : -2)),    // F3
                        115 =>Shift ? 87 : (Ctrl ? 97 : (Alt ? 106 : -3)),    // F4
                        116 =>Shift ? 88 : (Ctrl ? 98 : (Alt ? 107 : -4)),    // F5
                        117 =>Shift ? 89 : (Ctrl ? 99 : (Alt ? 108 : -5)),    // F6
                        118 =>Shift ? 90 : (Ctrl ? 100 : (Alt ? 109 : -6)),   // F7
                        119 =>Shift ? 91 : (Ctrl ? 101 : (Alt ? 110 : -7)),   // F8
                        120 =>Shift ? 92 : (Ctrl ? 102 : (Alt ? 111 : -8)),   // F9
                        121 =>Shift ? 93 : (Ctrl ? 103 : (Alt ? 112 : -9)),   // F10
                        122 =>Shift ? 135 : (Ctrl ? 137 : (Alt ? 139 : 133)), // F11
                        123 =>Shift ? 136 : (Ctrl ? 138 : (Alt ? 140 : 134)), // F12
                        186 =>Shift ? 59 : 58,                                // ;
                        187 =>Shift ? 61 : 43,                                // =
                        188 =>Shift ? 44 : 60,                                // ,
                        189 =>Shift ? 45 : 95,                                // _
                        190 =>Shift ? 46 : 62,                                // .
                        191 =>Shift ? 47 : 63,                                // /
                        192 =>Shift ? 96 : 126,                               // `
                        222 =>Shift ? 39 : 34,                                // '
                        219 =>Shift ? 91 : 123,                               // [
                        220 =>Shift ? 92 : 124,                               // \
                        221 =>Shift ? 93 : 125,                               // ]
                        _ => 0 // did not translate
                    };

                    // Perhaps it's not a special key?
                    if (result == 0 && JAXLib.Between(nKeyCode, 32, 127))
                    {
                        string akey =Shift ? ((char)nKeyCode).ToString().ToUpper() : ((char)nKeyCode).ToString().ToLower();

                        if (Shift || CapsLock)
                            result = akey.ToUpper()[0];
                        else
                            result = akey.ToLower()[0];
                    }
                }
            }

            return result;
        }

        /*
         * Translates .NET keypress event key code (from WinForm controls) to 
         * the VFP equivalent value for nKey
         * 
         * To get the correct nShiftAltCtrl value, use the following:
         *      // Key modifiers converted for VFP
         *      int keymods = e.Modifiers == Keys.Shift ? 1 : 0;
         *      keymods += e.Modifiers == Keys.Control ? 2 : 0;
         *      keymods += e.Modifiers == Keys.Alt ? 4 : 0;
         *
         */
        public static int FormsVFPKeyPress(string key, int nKeyCode)
        {
            int result = nKeyCode;

            if (JAXLib.InListC(key, "down", "up", "left", "right", "home", "End", "pageup", "pagedown", "next"))
            {
                result = nKeyCode switch
                {
                    38 => 5,
                    40 => 24,
                    37 => 19,
                    39 => 4,
                    36 => 1,
                    35 => 6,
                    33 => 18,
                    34 => 3,
                    _ => nKeyCode
                };
            }
            else if (JAXLib.InListC(key, "controlkey", "menu", "shiftkey", "lwin", "apps", "scroll", "pause"))
            {
                result = nKeyCode switch
                {
                    16 => 256,      // Shift
                    17 => 512,      // Control
                    18 => 1024,     // Alt
                    19 => 201,      // pause
                    91 => 202,      // LWin
                    93 => 203,      // apps
                    145 => 204,     // scroll
                    _ => nKeyCode
                };
            }
            else if (JAXLib.InListC(key, "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12"))
            {
                result = nKeyCode switch
                {
                    112 => 28,      // F1
                    113 => -1,      // F2
                    114 => -2,      // F3
                    115 => -3,      // F4
                    116 => -4,      // F5
                    117 => -5,      // F6
                    118 => -6,      // F7
                    119 => -7,      // F8
                    120 => -8,      // F9
                    121 => -9,      // F10
                    122 => 133,     // F11
                    123 => 134,     // F12
                    _ => nKeyCode
                };
            }
            else
                result = nKeyCode;

            return result;
        }



        //public static Avalonia.Controls.Window? WaitWindow(
        //    Core.AppClass app,
        //    string msgText,
        //    int row,
        //    int col,
        //    bool clear,
        //    bool wait,
        //    int timeout,
        //    out string retval)
        //{
        //    retval = string.Empty;

        //    if (clear)
        //    {
        //        CloseExistingWaitWindow();
        //        return null;
        //    }

        //    CloseExistingWaitWindow();

        //    var waitWin = new AvaloniaWaitWindow(msgText, row, col, timeout);
        //    ActiveWaitWindow = waitWin;
        //    waitWin.Closed += (s, e) => ActiveWaitWindow = null;

        //    if (!wait)
        //    {
        //        waitWin.ShowNonBlocking();
        //        return waitWin;
        //    }

        //    // Blocking WAIT on UI thread (no deadlock)
        //    string result = string.Empty;
        //    Dispatcher.UIThread.Invoke(() =>
        //    {
        //        result = waitWin.ShowAndWait();
        //    });

        //    retval = result;
        //    return null;
        //}

        //private static Avalonia.Controls.Window? ActiveWaitWindow = null;

        //private static void CloseExistingWaitWindow()
        //{
        //    if (ActiveWaitWindow == null) return;

        //    try
        //    {
        //        if (Dispatcher.UIThread.CheckAccess())
        //            ActiveWaitWindow.Close();
        //        else
        //            Dispatcher.UIThread.InvokeAsync(() => ActiveWaitWindow?.Close());
        //    }
        //    catch { }
        //    finally
        //    {
        //        ActiveWaitWindow = null;
        //    }
        //}

        /// <summary>
        /// Returns the window immediately. Blocking is handled by the caller (Executer) via await.
        /// </summary>
        public static Avalonia.Controls.Window? WaitWindow(
            Core.AppClass app,
            string msgText,
            int row,
            int col,
            bool clear,
            bool wait,
            int timeout,
            out string retval)
        {
            retval = string.Empty;

            if (clear)
            {
                CloseExistingWaitWindow();
                return null;
            }

            CloseExistingWaitWindow();

            var waitWin = new AvaloniaWaitWindow(msgText, row, col, timeout);
            ActiveWaitWindow = waitWin;
            waitWin.Closed += (s, e) => ActiveWaitWindow = null;

            if (!wait)
            {
                waitWin.ShowNonBlocking();
                return waitWin;
            }

            // For wait=true we return the window and let caller await the task
            return waitWin;
        }

        private static Avalonia.Controls.Window? ActiveWaitWindow = null;

        private static void CloseExistingWaitWindow()
        {
            if (ActiveWaitWindow == null) return;

            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                    ActiveWaitWindow.Close();
                else
                    Dispatcher.UIThread.InvokeAsync(() => ActiveWaitWindow?.Close());
            }
            catch { }
            finally
            {
                ActiveWaitWindow = null;
            }
        }
    }
}
