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
        public static string[] MathFunctions = ["ABS(", "ACLASS(", "ACOPY(", "ACOS(", "ADATABASES(", "ADBOBJECTS(", "ADDBS(",
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
        public static string[] JAXCommands = [
            "ACTIVATE","ADD","ALTER","APARAMETERS","APPEND",
            "ASSERT","AVERAGE","BEGIN","BLANK","BROWSE","CALCULATE",
            "CANCEL","CASE","CATCH","CD","CLEAR","CLOSE","COMPILE","CONTINUE",
            "COPY", "COUNT", "CREATE", "DEBUG","DEBUGOUT","DEFINE","DELETE",
            "DIMENSION","DIRECTORY","DISPLAY","DO","DOEVENTS","DODEFAULT",
            "DROP","EDIT","ELSE","ELSEIF","END","ENDCASE","ENDDEFINE", "ENDDO", "ENDFOR","ENDFUNCTION",
            "ENDIF","ENDPROCEDURE","ENDSCAN","ENDTEXT","ENDTRANSACTION","ENDTRY", "ENDWITH",
            "ERASE","ERROR","EXIT","EXPORT","EXTERNAL","FINALLY", "FOR","FOREACH","FUNCTION",
            "GATHER","GETEXPR","GOTO","HELP","IF", "IMPORT","INDEX","INSERT","KEYBOARD",
            "LIST","LOCATE","LOCAL","LOOP","LPARAMETERS","LPROCEDURE",
            "MD","MENU","MODIFY","MOUSE","NODEFAULT","ON","OPEN",
            "OTHERWISE","PACK", "PARAMETERS","PLAY","PRIVATE","PROCEDURE","PUBLIC",
            "QUIT","RD", "READ","RECALL","REGISTER","REINDEX","RELEASE","RENAME", "REPLACE",
            "RESTORE","RESUME","RETRY","RETURN","ROLLBACK","RUN","SAVE","SCAN","SCATTER",
            "SEEK","SELECT","SET", "SKIP","SORT","STORE","SUM","SUSPEND","TEXT",
            "THROW","TOTAL", "TRY","UNLOCK","UNTIL","UPDATE","USE","WAIT","WITH","ZAP",
            "?","??","!", "=","~~~","*sc"];


        /// <summary>
        /// List of all supported SET commands - TODO: Validate against JAXBase_Executor_settings
        /// </summary>
        // Settings have caps because those letters are used to identify each setting with a 3 char mnmonic
        public static string[] SetCommands =
            [
            "AIaGent", "ALTernate", "ANSi", "APPinit", "ASserTs", "AutoIncError","AutoSaVe",
            "BELl", "BLocKsize",
            "CaRrY","CENtury", "CLassliB", "CoLlaTe","ConFirM","CONsole","COVerage","CPCompile","CPDialog","CURrency","CurSoR",
            "DataBaSe","DataSessioM","DATe","DEBug", "DeBugOut","DECimals","DEFault","DELeted","DEVelopment","DeViCe",
            "EChO","ESCape","EVentList","EVentTracking","eXaCT","eXCLusive",
            "FDoW","FieLDs","FiLTer","FIXed","FullPaTh","FWeeK",
            "HEaDings","HeLP","HouRs",
            "InDeX",
            "KBMinput",
            "LIBrary","LoCK","LOGging",
            "MaCKey","MemoWiDth","MeSsaGe","MultiLocKs",
            "NAMing","NEaR","NoCPtrans","NOTify","NULl","NuLlDisplay",
            "ODoMeter","ORDer",
            "PaTH","PoiNT","PRImary","PROcedure",
            "REFresh","RELation","REProcess","RESource",
            "SAFety","SEConds","SeCuRity","SEParator","SKiP","SPaCe","SQlBuffering","SqlCoNnection","SqlLoaD","STeP","StrictDaTe","SySFormats","SYSmenu",
            "TaBlePrompt","TaBleValidate","TaLK","TextMerGe","TextDeLimiters","ToPiC","TopicID","TRBetween","TyPeaHead","TyPeConvert",
            "VarCharMapping","UDFparms"
            ];

        /// <summary>
        /// Array of Language code | Language component | Language byte code Elements
        /// </summary>
        // Language code is abreviated in the lexxer AS0, AT3, etc
        // Language component is the name used for the dictionary
        // Byte code is what's written into the tokenized code identifying the statement component
        public static string[] JAXCompilerDictionary = 
            [
            "AL|alias|0x80","AS|as|0x82", "AT|at|0x84", "CS|subcmd|0x86", "CM|command|0x88", "CO|collate|0x8A", "CP|codepage|0x8C", 
            "DB|database|0x90", "FG|flags|0x92", "FM|from|0x94", "FN|fname|0x96", "FR|for|0x98", "FV|fields|0x9A", 
            "IN|in|0xA0", "IT|into|0xA2","IX|index|0xA4", "LK|like|0xa6", "MS|message|0xA8","xx|xxxx|0xAC","NM|name|0xAD",
            "OF|of|0xB0", "ON|on|0xB4", "OR|order|0xB8","PT|pretext|0xB9", "RC|record|0xBA", 
            "SC|scope|0xC0", "SH|sheet|0xC2", "SI|size|0xC4", "SS|session|0xC6", "ST|step|0xC8", 
            "TB|table|0xD0", "TG|tag|0xD2", "TI|timeout|0xD4", "TO|to|0xD6", "TY|type|0xD8", "TX|text|0xDA",
            "VL|values|0xE0", "WL|while|0xE2", "WH|when|0xE4", "WI||0xE6", "WT|with|0xE8", 
            "XF|fileexpr|0xF0", "XX|expressions|0xF2"
            ];

        /// <summary>
        /// Array containing all valid JAXBase object types
        /// </summary>
        public static string[] JAXObjects = ["barcode","browser","checkbox","codebox","collection","column","combobox","commandbutton","commandgroup","container",
            "custom","editbox","empty","file","form","formset","ftp","grid","httpclient","hyperlink","image","jax","jaxedit","ipc","ircclient","label","line","listbox",
            "menu","menuitem","mqttclient","nostrclient","optionbutton","optiongroup","page","pageframe","pgp","pipe","pop3","printer","screen","separator","shape","sms",
            "smtp","sound","spinner","sql","textbox","toolbar","toolbutton","tcpclient","tcpserver","timer","udp","video"];


        /// <summary>
        /// Debug array that is used to translate characters under x20 with their related AppClass code abbreviations or hex values
        /// </summary>
        public static string[] PRGByteCodes = ["x00", "x01", " <ls>", "<le> ", "x04", "x05", " <HS>", "<HE> ", " <Hms>", "<Hme> ", "x0A", "x0B", "x0C","x0D",
            " <Xb>","<Xp>","<Xe> ","<Xd>","<pe>"," x13"," <Stmt> ","x15","x16","x17","x18"," <Ab>","<Ae> ","x1B"," <Cb>","<Ce> ","x1E","x1F"];

        /// <summary>
        /// Source filename extensions
        /// </summary>
        public static string[] SourceExtensions = ["scx", "vcx", "def", "mnu", "prg", "qry"];

        /// <summary>
        /// Run time filename extensions
        /// </summary>
        public static string[] RunTimeExtensions = ["jxs", "jxv", "jxd", "jxm", "jxp", "jxq"];


        public static List<string> SpecialKeys = ["TAB","BACKTAB","LBRACE","RBRACE","ENTER","SPACEBAR","ESC","DEL",
            "LEFTARROW","RIGHTARROW","UPARROW","DNARROW","HOME","END","PGUP","PGDN",
           "INS","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12","BACKSPACE",
        "RIGHTMOUSE","LEFTMOUSE","MOUSE"];

        /*-------------------------------------------------------------------------------------------*
         * DEBUG ROUTINE
         *-------------------------------------------------------------------------------------------*/
        public static void Decompile(string fileStem, string block)
        {
            int f;

            // Clear the file
            JAXLib.StrToFile(string.Empty, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 0);

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
                    JAXLib.StrToFile("      " + c, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile("      " + d, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile(bt.ToString("D4") + ": " + h, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile(string.Empty, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

                    c = string.Empty;
                    d = string.Empty;
                    h = string.Empty;
                    bt = i;
                }

                h += " " + b.ToString("X2") + " ";
                d += JAXLib.Right("    " + b.ToString("D3").TrimStart('0', ' ') + " ", 4);
                c += b > 32 && b < 127 ? " " + (char)b + "  " : "    ";
            }

            JAXLib.StrToFile(string.Empty, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
            JAXLib.StrToFile(string.Empty, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
            JAXLib.StrToFile(string.Empty, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

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
                    JAXLib.StrToFile(stmt, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);

                    cmdLine = cmdLine.TrimStart(AppClass.cmdByte); // get rid of the leading/trailing Statement Delimiters
                    string[] stmts = cmdLine.Split(AppClass.stmtDelimiter, StringSplitOptions.RemoveEmptyEntries);
                    string[] estmt = stmts[^1].Split(AppClass.cmdEnd);
                    stmts[^1] = estmt[0];

                    int cmdIdx = Program.CurrentApp.utl.Conv64ToInt(stmts[0][..2]);
                    stmts[0] = stmts[0][2..];

                    int lineNo = Program.CurrentApp.utl.Conv64ToInt(estmt[1]);
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

                            if (Program.CurrentApp.XRef4Runtime.ContainsKey(stmtCode))
                                cmd += Program.CurrentApp.XRef4Runtime[stmtCode] + " ";
                            else
                                cmd += "?" + stmtCode + "? ";


                            stmt = stmt.Replace(AppClass.expByte.ToString(), "<xs>").Replace(AppClass.expEnd.ToString(), "<xe>")
                                       .Replace(AppClass.expParam.ToString(), "<xp>").Replace(AppClass.expDelimiter.ToString(), "<XD>" + Environment.NewLine + "               ")
                                       .Replace(AppClass.literalStart.ToString(), "<ls>").Replace(AppClass.literalEnd.ToString(), "<le>") + " ";

                            cmd += stmt + Environment.NewLine + "            ";
                        }
                    }

                    // Write the line
                    JAXLib.StrToFile(lineNo.ToString("D5") + ": " + cmd, Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                    JAXLib.StrToFile("", Program.CurrentApp.JaxVariables._WorkPath + fileStem + "_cdf.txt", 3);
                }
                else
                    throw new Exception(string.Format("Unknown command byte {0}", (int)cmdByte));
            }
        }
    }
}
