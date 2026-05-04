/*
 * This is a central location to hold the language elements and other lists that 
 * may be needed throught the code.  They are especially important when you want to
 * expand an abbriviation to the full length.
 * 
 * USE:
 *      var f=Array.Find(app.lists.<ArrayName>,s=>s.StartsWith(setting.AsString().ToUpper()));
 *      if (f is null)
 *          throw new Exception("36|");
 *
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.Language
{
    public class JAXLanguageLists
    {
        /// <summary>
        /// Array containing all valid JAXBase functions
        /// </summary>
        public string[] MathFunctions = ["ABS(", "ACLASS(", "ACOPY(", "ACOS(", "ADATABASES(", "ADBOBJECTS(", "ADDBS(",
                "ADDPROPERTY(", "ADEL(", "ADIR(", "ADLLS(", "ADOCKSTATE(", "AELEMENT(", "AERROR(", "AEVENTS(",
                "AFIELDS(", "AFONT(", "AGETCLASS(", "AGETFILEVERSION(", "AINS(", "AINSTANCE(", "ALEN(",
                "ALIAS(", "ALINE(", "ALLTRIM(", "AMEMBERS(",  "ANETRECOURCES(",
                "APROCINFO(", "ASC(", "ASCAN(", "ASELOBJ(", "ASESSIONS(", "ASIN(", "ASORT(",
                "ASTACKINFO(", "ASUBSCRIPT(", "AT(", "AT_C(", "ATAGINFO(", "ATAN(", "ATC(", "ATCC(", "ATCLINE(",
                "ATLINE(", "ATN2(", "AUSED(", "AVCXCLASSES(",
                "BARCODE(", // Return BMP image string of barcode(type,content[,size?]) - size = 1=100x100, 2=150x150, 3=200x200, 4=300,300, 5=450x450, 6=600,600, 7=1200x1200, 8=1800x1800, 9=2400x2400
                "BETWEEN(", "BINDEVENT(", "BINTOC(", "BITAND(", "BITCLEAR(",
                "BITLSHIFT(", "BITNOT(", "BITOR(", "BITRSHIFT(", "BITSET(", "BITTEST(", "BITXOR(", "BOF(",
                "CANDIDATE(", "CAPSLOCK(", "CAST(", "CD(", "CDOW(", "CEILING(", "CHR(", "CHRSAW(", "CHRTRAN(",
                "CHRTRANC(", "CLEARRESULTSET(", "CMONTH(", "CNTBAR(", "CNTPAD(", "COL(", "COM(", "COMARRAY(",
                "COMCLASSINFO(", "COMPOBJ(", "COMPROP(", "COMRETURNERROR(", "COS(", "CPCONVERT(", "CPCURRENT(",
                "CONVERT(", // TODO - Convert integer to Base xx
                "CPDBF(", "CREATEBINARY(", "CREATEOBJECT(", "CREATEOBJECTEX(", "CREATOFFLINE(", "CTOBIN(", "CTOD(",
                "CTOT(", "CURSORGETPROP(", "CURSORSETPROP(", "CURSORTOJSON(", "CURSORTOXML(", "CURVAL(",
                "DATE(", "DATETIME(", "DAY(", "DBC(", "DBF(", "DBGETPROP(", "DBUSED(", "DEFAULTEXT(", "DELETED(",
                "DESCENDING(", "DIFFERENCE(", "DIRECTORY(", "DISKSPACE(", "DISPLAYPATH(", "DMY(", "DODEFAULT(",
                "DOW(", "DRIVETYPE(", "DROPOFFLINE(", "DTOC(", "DTOR(", "DTOS(", "DTOT(",
                "EDITSOURCE(", "EMPTY(", "EOF(", "ERROR(", "EVALUATE(", "EVENTHANDLER(", "EV(", "EVL(", "EXECSCRIPT(", "EXP(",
                "FCHSIZE(", "FCLOSE(", "FCOUNT(", "FCREATE(", "FDATE(", "FEOF(", "FERROR(", "FFLUSH(", "FGETS(", "FIELD(",
                "FILE(", "FILETOSTR(", "FILTER(", "FKLABEL(", "FKMAX(", "FLDLIST(", "FLOCK(", "FLOOR(", "FONTMETRIC(", "FOPEN(",
                "FOR(", "FORCEEXT(", "FORCEPATH(", "FOUND(", "FPUTS(", "FREAD(", "FSEEK(", "FSIZE(", "FTIME(", "FULLPATH(", "FWRITE(",
                "GETAUTOINCVALUE(", "GETCP(", "GETDATE(", "GETDIR(", "GETENV(", "GETFILE(", "GETFLDSTATE(", "GETFONT(",
                "GETNEXTMODIFIED(", "GETJSON(", "GETOBJECT(", "GETPICT(", "GETPRINTER(", "GETWORDCOUNT(", "GETWORDNUM(", "GETCURSORADAPTER(",
                "GOMONTH(", "GUID(",
                "HEADER(", "HOME(", "HOUR(",
                "ICASE(", "IDXCOLLATE(", "IIF(", "INDBC(", "INDEXSEEK(", "INKEY(", "INLIST(", "INLISTC(", "INPUTBOX(", "INSMODE(",
                "INT(", "ISALPHA(", "ISBLANK(", "ISCOLOR(", "ISDIGIT(", "ISEXCUSIVE(", "ISFLOCKED(", "ISLEADBYTE(", "ISLOWER(",
                "ISNULL(", "ISODD(", "ISPEN(", "ISREADONLY(", "ISRLOCKED(", "ISUPPER(",
                "JAX(", "JUSTDRIVE(", "JUSTEXT(", "JUSTFNAME(", "JUSTPATH(", "JUSTSTEM(", "JSONTOCURSOR(","JSONTOOBJ(",
                "KEY(", "KEYMATCH(",
                "LASTKEY(", "LEFT(", "LEFTC(", "LEN(", "LIKE(", "LIKEC(", "LINENO(", "LOADPICTURE(",
                "LOCFILE(", "LOCK(", "LOG(", "LOG10(", "LOOKUP(", "LOWER(", "LTRIM(", "LUPDATE(",
                "MAX(", "MCOL(", "MDOWN(", "MDX(", "MDY(", "MEMLINES(", "MEMORY(", "MESSAGE(", "MESSAGEBOX(",
                "MIN(", "MINUTE(", "MLINE(", "MOD(", "MONTH(", "MRKBAR(", "MRKPAD(", "MROW(", "MTON(", "MWINDOW(",
                "NAMING(", "NDX(", "NEWOBJECT(", "NODA(", "NORMALIZE(", "NTOM(", "NUMLOCK(", "NVL(",
                "OBJNUM(", "OBJTOCLIENT(", "OBJTOJSON(", "OBJVAR(", "OCCURS(", "OEMTOANSI(", "OLDVAL(", "ONKEY(", "ORDER(", "OS(",
                "PADL(", "PADR(", "PADC(", "PARAMETERS(", "PAYMENT(", "PCOL(", "PCOUNT(",
                "PEMSTATUS(", "PI(","PIXELPOS(", "PRIMARY(", "PROGRAM(", "PROMPT(", "PROPER(", "PUTFILE(", "PUTJSON(", "PV(",
                "QUARTER(",
                "RAISEEVENT(", "RAND(", "RAT(", "RATC(", "RATLINE(", "RDLEVEL(", "READKEY(", "RECCOUNT(",
                "RECNO(", "RECSIZE(", "REFRESH(", "RELATION(", "REMOVEPROPERTY(", "REPLICATE(", "REQURY(", "RGB(",
                "RIGHT(", "RIGHTC(", "RLOCK(", "ROUND(", "ROW(", "RTOD(", "RTRIM(",
                "SAVEPICTURE(", "SCHEME(", "SCOLS(", "SEC(", "SECONDS(", "SEEK(", "SELECT(", "SET(", "SETFLDSTATE(",
                "SETRESULTSET(", "SIGN(", "SIN(", "SKPBAR(", "SKPPAD(", "SOUNDEX(", "SPACE(", "SQLCANCEL(",
                "SQLCOLUMNS(", "SQLCOMMIT(", "SQLCONNECT(", "SQLDISCONNECT(", "SQLEXEC(", "SQLGETPROP(",
                "SQLIDLEDISCONNECT(", "SQLMERGERESULTS(", "SQLPREPARE(", "SQLROLLBACK(", "SQLSETPROP(",
                "SQLSTRINGCONNECT(", "SQLTABLES(", "SORT(", "STR(", "STRCONV(", "STREXTRACT(", "STRFORMAT(", "STRTOFILE(",
                "STRTRAN(", "STUFF(", "STUFFC(", "SUBSTR(", "SUBTRC(", "SYSMETRIC(","SYSID(",
                "TAN(", "TARGET(","TEXTPOS(", "TEXTMERGE(", "TIME(", "TIMEZONE(", "TOSEC(",
                "TRANSFORM(", "TRIM(", "TTOC(", "TTOD(", "TXNLEVEL(", "TXTWIDTH(", "TYPE(",
                "UNBINDEVENTS(", "UNIQUE(", "UPDATED(", "UPPER(", "USED(",
                "VAL(", "VARREAD(", "VARTYPE(", "VERSION(",
                "WEEK(",
                "XMLTOCURSOR(",
                "YEAR("];

        /// <summary>
        /// Array containing all valid JAXBase commands including hidden commands
        /// </summary>
        public string[] JAXCommands = [
            "ACTIVATE","ADD","ALTER","APARAMETERS","APPEND",
            "ASSERT","AVERAGE","BEGIN","BLANK","BROWSE","CALCULATE",
            "CANCEL","CASE","CATCH","CD","CLEAR","CLOSE","COMPILE","CONTINUE",
            "COPY", "COUNT", "CREATE", "DEACTIVATE","DEBUG","DEBUGOUT","DEFINE","DELETE",
            "DIMENSION","DIRECTORY","DISPLAY","DO","DOEVENTS","DODEFAULT",
            "DROP","EDIT","ELSE","ELSEIF","END","ENDCASE","ENDDEFINE", "ENDDO", "ENDFOR","ENDFUNCTION",
            "ENDIF","ENDPROCEDURE","ENDSCAN","ENDTEXT","ENDTRANSACTION","ENDTRY", "ENDWITH",
            "ERASE","ERROR","EXIT","EXPORT","EXTERNAL","FINALLY", "FOR","FOREACH","FUNCTION",
            "GATHER","GETEXPR","GOTO","HELP","IF", "IMPORT","INDEX","INSERT","KEYBOARD",
            "LIST","LOCATE","LOCAL","LOOP","LPARAMETERS","LPROCEDURE",
            "MD","MENU","MODIFY","MOUSE","MOVE","NODEFAULT","ON","OPEN",
            "OTHERWISE","PACK", "PARAMETERS","PLAY","PRIVATE","PROCEDURE","PUBLIC",
            "QUIT","RD", "READ","RECALL","REGISTER","REINDEX","RELEASE","RENAME", "REPLACE",
            "RESTORE","RESUME","RETRY","RETURN","ROLLBACK","RUN","SAVE","SCAN","SCATTER",
            "SEEK","SELECT","SET", "SKIP","SORT","STORE","SUM","SUSPEND","TEXT",
            "THROW","TOTAL", "TRY","UNLOCK","UNTIL","UPDATE","USE","WAIT","WITH","ZAP",
            "?","??","!", "=","~~~","*sc"];


        /// <summary>
        /// List of all supported SET commands
        /// </summary>
        public string[] SetCommands = [
            "alternate", "appinit", "asserts", "autoincerror","autosave",
            "bell", "blocksize",
            "carry","century", "classlib", "collate","confirm","console","coverage","cpcompile","cpialog","currency","cursor",
            "database","datasession","date","debug", "debugout","decimals","default","deleted","development",
            "echo","escape","eventlist","eventtracking","exeact","exclusive",
            "fdow","fields","filter","fixed","fullpath","fweek",
            "headings","help","hour",
            "index",
            "kbminput",
            "lock","logging",
            "mackey","memowidth","message","multilocks",
            "naming","near","nocptrans","notify","null","nulldisplay",
            "odometer","order",
            "path","point","primary","procedure",
            "refresh","relation","reprocess","resource",
            "safety","seconds","security","separator","skip","space","sqlconnection","sqlload","step","strictdate","sysmenu",
            "tableprompt","tablevalidate","talk","textmerge","textdelimiters","topic","topicid","trbetween","typeahead","typeconvert",
            "includesource"];   // Deprecated as of V1.0

        /// <summary>
        /// Array of Language code | Language component | Language byte code Elements
        /// </summary>
        // Language code is abreviated in the lexxer AS0, AT3, etc
        // Language component is the name used for the dictionary
        // Byte code is what's written into the tokenized code identifying the statement component
        public string[] JAXCompilerDictionary = ["AL|alias|0x80","AS|as|0x82", "AT|at|0x84", "CS|subcmd|0x88", "CM|command|0x8A", "CO|collate|0x8C",
            "CP|codepage|0x90", "DB|database|0x94", "FG|flags|0x96", "FM|from|0x98", "FN|fname|0x9C",
            "FR|for|0xA0", "FV|fields|0xA4", "IN|in|0xA8", "IT|into|0xAC","IX|index|0xAE",
            "LK|like|0xB0", "MS|message|0xB4","xx|xxxx|0xB8","NM|name|0xBC",
            "OF|of|0xC0", "ON|on|0xC4", "OR|order|0xC8", "RC|record|0xCA", "SC|scope|0xCC", "SH|sheet|0xCE",
            "SI|size|0xD0", "SS|session|0xD1", "ST|step|0xD2", "TB|table|0xD4", "TG|tag|0xD6","TI|timeout|0xD8", "TO|to|0xDC",
            "TY|type|0xE0", "VL|values|0xE4", "WL|while|0xE8", "WH|when|0xEC",
            "WI||0xF0", "WT|with|0xF4", "XF|fileexpr|0xF6", "XX|expressions|0xF8"];

        /// <summary>
        /// Array containing all valid JAXBase object types
        /// </summary>
        public string[] JAXObjects = ["barcode","browser","checkbox","codebox","collection","column","combobox","commandbutton","commandgroup","container",
            "custom","editbox","empty","file","form","formset","ftp","grid","http","hyperlink","image","jax","jaxedit","ipc","irc","label","line","listbox",
            "menu","menuitem","optionbutton","optiongroup","page","pageframe","pgp","pipe","pop3","printer","screen","separator","shape","sms",
            "smtp","sound","spinner","sql","textbox","toolbar","toolbutton","tcp","timer","udp","video"];

        /// <summary>
        /// Debug array that is used to translate characters under x20 with their related AppClass code abbreviations or hex values
        /// </summary>
        public string[] PRGByteCodes = ["x00", "x01", " <ls>", "<le> ", "x04", "x05", " <HS>", "<HE> ", " <Hms>", "<Hme> ", "x0A", "x0B", "x0C","x0D",
            " <Xb>","<Xp>","<Xe> ","<Xd>","<pe>"," x13"," <Stmt> ","x15","x16","x17","x18"," <Ab>","<Ae> ","x1B"," <Cb>","<Ce> ","x1E","x1F"];

        /// <summary>
        /// Source filename extensions
        /// </summary>
        public string[] SourceExtensions = ["scx", "vcx", "def", "mnu", "prg", "qry"];

        /// <summary>
        /// Run time filename extensions
        /// </summary>
        public string[] RunTimeExtensions = ["jxs", "jxv", "jxd", "jxm", "jxp", "jxq"];


        public List<string> SpecialKeys = ["TAB","BACKTAB","LBRACE","RBRACE","ENTER","SPACEBAR","ESC","DEL",
            "LEFTARROW","RIGHTARROW","UPARROW","DNARROW","HOME","END","PGUP","PGDN",
           "INS","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12","BACKSPACE",
        "RIGHTMOUSE","LEFTMOUSE","MOUSE"];

        /*-------------------------------------------------------------------------------------------*
         * DEBUG ROUTINE
         *-------------------------------------------------------------------------------------------*/
        public void Decompile(AppClass app, string fileStem, string block)
        {
            int f;

            // Clear the file
            JAXLib.StrToFile(string.Empty, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 0);

            // Strip the header and map
            char cmdByte = block[0];

            // Is it a header?
            if (cmdByte == AppClass.headerStartByte)
            {
                f = block.IndexOf(AppClass.headerEndByte);
                if (f < 0) throw new Exception("Missing header end byte");
                f++;

                if (block.Length > f)
                    block = block[f..];
                else
                    block = string.Empty;
            }

            cmdByte = block[0];
            if (cmdByte == AppClass.headerMapStartByte)
            {
                f = block.IndexOf(AppClass.headerMapEndByte);
                if (f < 0) throw new Exception("Missing header map end byte");
                f++;

                if (block.Length > f)
                    block = block[f..];
                else
                    block = string.Empty;
            }

            // Create the dump
            string c = string.Empty;
            string d = string.Empty;
            string h = string.Empty;
            int bt = 0;

            for (int i = 0; i < block.Length; i++)
            {
                int b = block[i];

                // Start of a new command?cd c:\
                if (b == AppClass.cmdByte && i > 0)
                {
                    JAXLib.StrToFile("      " + c, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile("      " + d, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile(bt.ToString("D4") + ": " + h, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile(string.Empty, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

                    c = string.Empty;
                    d = string.Empty;
                    h = string.Empty;
                    bt = i;
                }

                h += " " + b.ToString("X2") + " ";
                d += JAXLib.Right("    " + b.ToString("D3").TrimStart('0', ' ') + " ", 4);
                c += b > 32 && b < 127 ? " " + (char)b + "  " : "    ";
            }

            JAXLib.StrToFile(string.Empty, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
            JAXLib.StrToFile(string.Empty, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
            JAXLib.StrToFile(string.Empty, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

            // Line by line decompilation
            while (block.Length > 0)
            {
                // Get the start byte
                cmdByte = block[0];

                if (cmdByte == AppClass.cmdByte)
                {
                    f = block.IndexOf(AppClass.cmdEnd);
                    while (block[f] == AppClass.cmdEnd) f++;

                    if (f + 2 < 0) throw new Exception("Missing End bytes");
                    f += 2;
                    string cmdLine = block[..f];

                    if (block.Length > f)
                        block = block[f..];
                    else
                        block = string.Empty;

                    string stmt = string.Empty;
                    for (int i = 0; i < cmdLine.Length; i++)
                    {
                        stmt += (((int)cmdLine[i]).ToString("X2") + "/" + ((int)cmdLine[i]).ToString("D3").TrimStart('0', ' ') + "      ")[..8];
                        if (i > 0 && i % 10 == 0) stmt += Environment.NewLine;
                    }

                    // Write the line
                    JAXLib.StrToFile(stmt, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

                    cmdLine = cmdLine.TrimStart(AppClass.cmdByte); // get rid of the leading/trailing Statement Delimiters
                    string[] stmts = cmdLine.Split(AppClass.stmtDelimiter, StringSplitOptions.RemoveEmptyEntries);
                    string[] estmt = stmts[^1].Split(AppClass.cmdEnd);
                    stmts[^1] = estmt[0];

                    int cmdIdx = app.utl.Conv64ToInt(stmts[0][..2]);
                    stmts[0] = stmts[0][2..];

                    int lineNo = app.utl.Conv64ToInt(estmt[1]);
                    string cmd = JAXCommands[cmdIdx].ToString() + " ";

                    if (lineNo == 49) lineNo = lineNo - 0;

                    // Build on the command
                    for (int i = 0; i < stmts.Length; i++)
                    {
                        if (stmts[i].Length > 0)
                        {
                            stmt = stmts[i];
                            char stmtCode = stmt[0];
                            stmt = stmt[1..];

                            if (app.XRef4Runtime.ContainsKey(stmtCode))
                                cmd += app.XRef4Runtime[stmtCode] + " ";
                            else
                                cmd += "?" + stmtCode + "? ";


                            stmt = stmt.Replace(AppClass.expByte.ToString(), "<xs>").Replace(AppClass.expEnd.ToString(), "<xe>")
                                       .Replace(AppClass.expParam.ToString(), "<xp>").Replace(AppClass.expDelimiter.ToString(), "<XD>" + Environment.NewLine + "               ")
                                       .Replace(AppClass.literalStart.ToString(), "<ls>").Replace(AppClass.literalEnd.ToString(), "<le>") + " ";

                            cmd += stmt + Environment.NewLine + "            ";
                        }
                    }

                    // Write the line
                    JAXLib.StrToFile(lineNo.ToString("D5") + ": " + cmd, app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile("", app.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                }
                else
                    throw new Exception(string.Format("Unknown command byte {0}", (int)cmdByte));
            }
        }
    }
}
