/*
 * This is a central location to hold language elements and other lists that 
 * may be needed throughout the code.
 * 
 */
using JAXBase.Core;
using JAXBase.Core.Extensions;
using JAXBase.Language.es;

namespace JAXBase.Language
{
    public class JAXLanguageLists
    {
        /// <summary>
        /// Array containing all valid JAXBase functions
        /// </summary>
        public static string[] MathFunctions = 
            [
            "ABS(", "ACLASS(", "ACOPY(", "ACOS(", "ADATABASES(", "ADBOBJECTS(", "ADDBS(",
            "ADDPROPERTY(", "ADEL(", "ADIR(", "ADLLS(", "ADOCKSTATE(", "AELEMENT(", "AERROR(", "AEVENTS(",
            "AFIELDS(", "AFONT(", "AGETCLASS(", "AGETFILEVERSION(", "AINS(", "AINSTANCE(", "ALEN(",
            "ALIAS(", "ALINE(", "ALLTRIM(", "AMEMBERS(",  "ANETRECOURCES(", "APROCINFO(", "ASC(", "ASCAN(", "ASELOBJ(", 
            "ASESSIONS(", "ASIN(", "ASORT(", "ASTACKINFO(", "ASUBSCRIPT(", "AT(", "AT_C(", "ATAGINFO(", "ATAN(", "ATC(", 
            "ATCC(", "ATCLINE(", "ATLINE(", "ATN2(", "AUSED(", "AVCXCLASSES(",

            "BARCODE(", "BETWEEN(", "BINDEVENT(", "BINTOC(", "BITAND(", "BITCLEAR(", "BITLSHIFT(", "BITNOT(", "BITOR(", 
            "BITRSHIFT(", "BITSET(", "BITTEST(", "BITXOR(", "BOF(",

            "CANDIDATE(", "CAPSLOCK(", "CAST(", "CD(", "CDOW(", "CEILING(", "CHR(", "CHRSAW(", "CHRTRAN(", "CHRTRANC(", 
            "CLEARRESULTSET(", "CMONTH(", "CNTBAR(", "CNTPAD(", "COL(", "COM(", "COMARRAY(", "COMCLASSINFO(", "COMPOBJ(", 
            "COMPROP(", "COMRETURNERROR(", "COS(", "CPCONVERT(", "CPCURRENT(", "CPDBF(", "CREATEBINARY(", "CREATEOBJECT(", 
            "CREATEOBJECTEX(", "CREATOFFLINE(", "CTOBIN(", "CTOD(", "CTOT(", "CURSORGETPROP(", "CURSORSETPROP(", 
            "CURSORTOJSON(", "CURSORTOXML(", "CURVAL(",
            
            "DATE(", "DATETIME(", "DAY(", "DBC(", "DBF(", "DBGETPROP(", "DBUSED(", "DEFAULTEXT(", "DELETED(", "DESCENDING(", 
            "DIFFERENCE(", "DIRECTORY(", "DISKSPACE(", "DISPLAYPATH(", "DMY(", "DODEFAULT(", "DOW(", "DRIVETYPE(", 
            "DROPOFFLINE(", "DTOC(", "DTOR(", "DTOS(", "DTOT(",

            "EDITSOURCE(", "EMPTY(", "EOF(", "ERROR(", "EVALUATE(", "EVENTHANDLER(", "EV(", "EVL(", "EXECSCRIPT(", "EXP(",
            "FCHSIZE(", "FCLOSE(", "FCOUNT(", "FCREATE(", "FDATE(", "FEOF(", "FERROR(", "FFLUSH(", "FGETS(", "FIELD(",

            "FILE(", "FILETOSTR(", "FILTER(", "FKLABEL(", "FKMAX(", "FLDLIST(", "FLOCK(", "FLOOR(", "FONTMETRIC(", "FOPEN(",
            "FOR(", "FORCEEXT(", "FORCEPATH(", "FOUND(", "FPUTS(", "FREAD(", "FSEEK(", "FSIZE(", "FTIME(", "FULLPATH(", "FWRITE(",

            "GETAUTOINCVALUE(", "GETCP(", "GETDATE(", "GETDIR(", "GETENV(", "GETFILE(", "GETFLDSTATE(", "GETFONT(",
            "GETNEXTMODIFIED(", "GETJSON(", "GETOBJECT(", "GETPICT(", "GETPRINTER(", "GETWORDCOUNT(", "GETWORDNUM(", "GETCURSORADAPTER(",
            "GOMONTH(", "GUID(",

            "HEADER(", "HOME(", "HOUR(","HEX(",

            "ICASE(", "IDXCOLLATE(", "IIF(", "INDBC(", "INDEXSEEK(", "INKEY(", "INLIST(", "INLISTC(", "INPUTBOX(", "INSMODE(",
            "INT(", "ISALPHA(", "ISBLANK(", "ISCOLOR(", "ISDIGIT(", "ISEXCUSIVE(", "ISFLOCKED(", "ISLEADBYTE(", "ISLOWER(",
            "ISNULL(", "ISODD(", "ISPEN(", "ISREADONLY(", "ISRLOCKED(", "ISUPPER(",

            "JAX(", "JUSTDRIVE(", "JUSTEXT(", "JUSTFNAME(", "JUSTPATH(", "JUSTSTEM(", "JSONTOCURSOR(","JSONTOOBJ(",

            "KEY(", "KEYMATCH(",

            "LASTKEY(", "LEFT(", "LEFTC(", "LEN(", "LIKE(", "LIKEC(", "LINENO(", "LOADPICTURE(", "LOCFILE(", "LOCK(", "LOG(", 
            "LOG10(", "LOOKUP(", "LOWER(", "LTRIM(", "LUPDATE(",

            "MAX(", "MCOL(", "MDOWN(", "MDX(", "MDY(", "MEMLINES(", "MEMORY(", "MESSAGE(", "MESSAGEBOX(", "MIN(", "MINUTE(", 
            "MLINE(", "MOD(", "MONTH(", "MRKBAR(", "MRKPAD(", "MROW(", "MTON(", "MWINDOW(", "NAMING(", "NDX(", "NEWOBJECT(", 

            "NODA(", "NORMALIZE(", "NTOM(", "NUMLOCK(", "NVL(",

            "OBJNUM(", "OBJTOCLIENT(", "OBJTOJSON(", "OBJVAR(", "OCCURS(", "OEMTOANSI(", "OLDVAL(", "ONKEY(", "ORDER(", "OS(",

            "PADL(", "PADR(", "PADC(", "PARAMETERS(", "PAYMENT(", "PCOL(", "PCOUNT(",
            "PEMSTATUS(", "PI(","PIXELPOS(", "PRIMARY(", "PROGRAM(", "PROMPT(", "PROPER(", "PUTFILE(", "PUTJSON(", "PV(",

            "QUARTER(",

            "RAISEEVENT(", "RAND(", "RAT(", "RATC(", "RATLINE(", "RDLEVEL(", "READKEY(", "RECCOUNT(", "RECNO(", "RECSIZE(", 
            "REFRESH(", "RELATION(", "REMOVEPROPERTY(", "REPLICATE(", "REQURY(", "RGB(", "RIGHT(", "RIGHTC(", "RLOCK(", 
            "ROUND(", "ROW(", "RTOD(", "RTRIM(",

            "SAVEPICTURE(", "SCHEME(", "SCOLS(", "SEC(", "SECONDS(", "SEEK(", "SELECT(", "SET(", "SETFLDSTATE(",
            "SETRESULTSET(", "SIGN(", "SIN(", "SKPBAR(", "SKPPAD(", "SOUNDEX(", "SPACE(", "SQLCANCEL(", "SQLCOLUMNS(", 
            "SQLCOMMIT(", "SQLCONNECT(", "SQLDISCONNECT(", "SQLEXEC(", "SQLGETPROP(", "SQLIDLEDISCONNECT(", "SQLMERGERESULTS(", 
            "SQLPREPARE(", "SQLROLLBACK(", "SQLSETPROP(", "SQLSTRINGCONNECT(", "SQLTABLES(", "SORT(", "STR(", "STRCONV(", 
            "STREXTRACT(", "STRFORMAT(", "STRTOFILE(", "STRTRAN(", "STUFF(", "STUFFC(", "SUBSTR(", "SUBTRC(", "SYSMETRIC(","SYSID(",

            "TAN(", "TARGET(","TEXTPOS(", "TEXTMERGE(", "TIME(", "TIMEZONE(", "TOSEC(", "TRANSFORM(", "TRIM(", "TTOC(", "TTOD(", 
            "TXNLEVEL(", "TXTWIDTH(", "TYPE(",

            "UNBINDEVENTS(", "UNIQUE(", "UPDATED(", "UPPER(", "USED(",

            "VAL(", "VARREAD(", "VARTYPE(", "VERSION(",

            "WEEK(",

            "XMLTOCURSOR(",

            "YEAR("
            ];


        /// <summary>
        /// Array containing all valid JAXBase commands including hidden commands
        /// </summary>
        public static string[] JAXCommands = 
            [
            "ACTIVATE","ADD","ALTER","APARAMETERS","APPEND", "ASSERT","AVERAGE",
            "BEGIN","BLANK","BROWSE",
            "CALCULATE", "CANCEL","CASE","CATCH","CD","CLEAR","CLOSE","COMPILE","CONTINUE", "COPY", "COUNT", "CREATE", 
            "DEBUG","DEBUGOUT","DEFINE","DELETE", "DIMENSION","DIRECTORY","DISPLAY","DO","DOEVENTS","DODEFAULT", "DROP",
            "EDIT","ELSE","ELSEIF","END","ENDCASE","ENDDEFINE", "ENDDO", "ENDFOR","ENDFUNCTION", "ENDIF","ENDPROCEDURE",
            "ENDSCAN","ENDTEXT","ENDTRANSACTION","ENDTRY", "ENDWITH", "ERASE","ERROR","EXIT","EXPORT","EXTERNAL",
            "FINALLY", "FOR","FOREACH","FUNCTION",
            "GATHER","GETEXPR","GOTO",
            "HELP",
            "IF", "IMPORT","INDEX","INSERT",
            "KEYBOARD",
            "LIST","LOCATE","LOCAL","LOOP","LPARAMETERS","LPROCEDURE",
            "MD","MENU","MODIFY","MOUSE",
            "NODEFAULT",
            "ON","OPEN", "OTHERWISE",
            "PACK", "PARAMETERS","PLAY","PRIVATE","PROCEDURE","PUBLIC",
            "QUIT",
            "RD", "READ","RECALL","REGISTER","REINDEX","RELEASE","RENAME", "REPLACE", "RESTORE","RESUME","RETRY","RETURN",
            "ROLLBACK","RUN",
            "SAVE","SCAN","SCATTER","SEEK","SELECT","SET", "SKIP","SORT","STORE","SUM","SUSPEND",
            "TEXT","THROW","TOTAL", "TRY",
            "UNLOCK","UNTIL","UPDATE","USE",
            "WAIT","WITH",
            "ZAP",
            "?","??","!", "=","~~~"
            ];

       
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
            "UDFparms",
            "VarCharMapping",
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
            "IN|in|0xA0", "IT|into|0xA2","IX|index|0xA4", "LK|like|0xa6", "MS|message|0xA8","NM|name|0xAD",
            "OF|of|0xB0", "ON|on|0xB4", "OR|order|0xB8","PT|pretext|0xB9", "RC|record|0xBA",
            "SC|scope|0xC0", "SH|sheet|0xC2", "SI|size|0xC4", "SS|session|0xC6", "ST|step|0xC8",
            "TB|table|0xD0", "TG|tag|0xD2", "TI|timeout|0xD4", "TO|to|0xD6", "TY|type|0xD8", "TX|text|0xDA",
            "VL|values|0xE0", "WL|while|0xE2", "WH|when|0xE4", "WI||0xE6", "WT|with|0xE8",
            "XF|fileexpr|0xF0", "XX|expressions|0xF2"
            ];


        /// <summary>
        /// Array containing all valid JAXBase object types
        /// </summary>
        public static string[] JAXObjects = 
            [
            "barcode","browser",
            "checkbox","codebox","collection","column","combobox","commandbutton","commandgroup","container","custom",
            "editbox","empty",
            "file","form","formset","ftp",
            "grid","httpclient",
            "hyperlink",
            "image","ipc","ircclient",
            "jax","jaxedit",
            "label","line","listbox",
            "menu","menuitem","mqttclient",
            "nostrclient",
            "optionbutton","optiongroup",
            "page","pageframe","pgp","pipe","pop3","printer",
            "robrowser",
            "screen","separator","shape","sms","smtp","sound","spinner","sql",
            "textbox","toolbar","toolbutton","tcpclient","tcpserver","timer","tree","treeitem",
            "udp",
            "video"
            ];


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


        /// <summary>
        ///  English to ActiveLanguagePack
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetWord(string input, string dictionary)
        {
            var pack = dictionary.ToUpper() switch
            {
                "MATH" => Program.CurrentApp.ActiveLanguagePack.MathFunctions,
                "COMMAND" => Program.CurrentApp.ActiveLanguagePack.JAXCommands,
                "COMMANDPARTS" => Program.CurrentApp.ActiveLanguagePack.CommandParts,
                "SET" => Program.CurrentApp.ActiveLanguagePack.SetCommands,
                "OBJECT" => Program.CurrentApp.ActiveLanguagePack.JaxObjects,
                "REVCOMMANDPARTS" => Program.CurrentApp.ActiveLanguagePack.RevCommandParts,
                "REVOBJECTS" => Program.CurrentApp.ActiveLanguagePack.RevJaxObjects,
                "REVPEMS" => Program.CurrentApp.ActiveLanguagePack.RevPEMs,
                "KEY" => Program.CurrentApp.ActiveLanguagePack.SpecialKeys,
                _ => throw new ArgumentException(GetPhrase(28, dictionary))
            };

            if (pack.TryGetValue(input, out string? canonical))
                return canonical ?? input;

            return input;  // fallback
        }


        /// <summary>
        /// Grab and populate a phrase
        /// </summary>
        /// <param name="iphrase"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public static string GetPhrase(int iphrase, string arg1 = "", string arg2 = "", string arg3 = "")
        {
            string result = "";

            if (iphrase < Program.CurrentApp.ActiveLanguagePack.Phrase.Count && iphrase >= 0)
            {
                result = Program.CurrentApp.ActiveLanguagePack.Phrase[iphrase];


                switch (result.Length - result.Replace("{", "").Length)
                {
                    case 1:
                        result = string.Format(result, arg1);
                        break;

                    case 2:
                        result = string.Format(result, arg1, arg2);
                        break;

                    case 3:
                        result = string.Format(result, arg1, arg2, arg3);
                        break;
                }
            }
            else
                result = string.Format(Program.CurrentApp.ActiveLanguagePack.Phrase[27], iphrase);

            return result;
        }


        /// <summary>
        /// Load the desired language pack based on language code:
        ///     en - English
        ///     es - Spanish
        ///     fr - French
        ///     de - German
        ///     
        /// </summary>
        /// <param name="languageCode"></param>
        /// <returns></returns>
        public static ILanguagePack GetLanguagePack(string languageCode)
        {
            //bool found = File.Exists("LanguagePacks/JAXBase-Lang-" + languageCode + ".dll");

            ILanguagePack pack;

            try
            {
                pack = languageCode == "es" ? new SpanishLanguagePack() : new EnglishLanguagePack();
            }
            catch (Exception ex)
            {
                AppIO.DebugLog($"Language pack for '{languageCode}' could not be loaded. {ex.Message}");
                pack = new EnglishLanguagePack();
            }

            return pack;
        }
    }
}
