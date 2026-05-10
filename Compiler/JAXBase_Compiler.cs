/* -------------------------------------------------------------------------------------------------*
 * JAXBase Compiler
 * 
 * Accepts a line or block of code and processes it down to a tokenized form.
 * Returns the tokenized block for storing or execution.
 * 
 * Since I know nothing about lexers, I expect this code could probably be 
 * optimized quite a bit.
 * 
 * -------------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.Math;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.Compiler
{
    public class JAXBase_Compiler
    {
        public readonly Dictionary<string, string> CompilerCodes = [];      // Compiler Code / Dictionary Code translation
        public readonly List<string> CodeDictionary = [];                   // Compiler Code Dictionary
        public readonly Dictionary<string, char> CompilerXRef = [];         // Convert compiler code to byte
        public readonly Dictionary<string, string> KeyLabels = [];          // Holds the code to execute for active key handlers

        private int lineNo = 0;

        /* -----------------------------------------------------------------------------------------*
         * Instantiate the compiler class
         * -----------------------------------------------------------------------------------------*/
        public JAXBase_Compiler(AppClass app)
        {
            // TODO - Move this all to AppClass
            for (int i = 0; i < JAXLanguageLists.JAXCompilerDictionary.Length; i++)
            {
                string[] jcp = JAXLanguageLists.JAXCompilerDictionary[i].Split('|');
                CompilerCodes.Add(jcp[0], jcp[1]);
                CodeDictionary.Add(jcp[1]);


                char k = Convert.ToChar(Convert.ToInt32(jcp[2], 16));

                CompilerXRef.Add(jcp[0], k);            // Use these codes building runtime statements
                app.XRef4Runtime.Add(k, jcp[0]);        // Convert statementcodes back to human readable codes
                app.RunTimeCodes.Add(jcp[0], jcp[1]);   // Human readable runtime statement elements
            }
        }

        /* -----------------------------------------------------------------------------------------*
         * Parse the rest of a normal command
         *
         * 000 - Empty location
         * AS0 - AS expr
         * AS1 - AS Lit/Expr
         * AT0 - AT row,col
         * BK0 - Blank
         * BR0 - BAR name
         * CL0 - CLASS lit/(expr)
         * CM0 - command
         * CO0 - COLLATE Lit/Expr
         * CP0 - CODEPAGE Lit/Expr
         * DA0 - DATA expr
         * DB0 - DATABASE Lit/(expr)
         * DF0 - DEFAULT expr
         * DG0 - DRAG TO row2,col2,row3,col3...
         * FG0 - FLAGS expr
         * FG1 - Flags place holder
         * FM0 - FROM Lit/(Expr)
         * FM1 - FROM [Array name][Memvar][Name objName][JSON var]
         * FM2 - FROM AT row,col
         * FR0 - FOR lExpression
         * FR1 - start expr of FOR after =
 	     * FR* - FOR "expression as string"
         * FV0 - FIELD Var Literal
         * FV1 - FIELDS Var Literal List
         * FV2 - FIELDS Var Literal List starting with (
         * FV3 - FIELDS Literal list, FIELDS LIKE skeleton | FIELDS EXCEPT skeleton
         * IN0 - IN WorkArea | Alias
         * IT0 - INTO filename
         * IT1 - 
         * LK0 - LIKE expression
         * LK1 - LIKE Lit/(Expr)
         * LK2 - LIKE varlist
         * MS0 - MESSAGE expression
         * NM0 - NAME lit/(expr)
         * OF0 - OF filename
         * ON0 - ON expression4
         * ON1 - ON Lit/Expr
         * OR0 - ORDER Lit/(Expr)
         * PD0 - PAD name
         * PT0 - PRETEXT expr
         * RC0 - RECORD expr
         * SI0 - SIZE AT row,col
         * SC0 - Scope (All/Rest/Record/Next/Top)
         * SS0 - SESSION nSession
         * SH0 - SHEET lit/(expr)
         * TG0 - 
         * TG3 - TO/TAG Lit/(Expr)
         * TB0 - table expression (field x(n,n),...)
         * TI0 - TIMEOUT expr
         * TI1 - TIME expr
         * TO0 - TO VarLiteral
         * TO1 - TO VarList
         * TO2 - TO [ARRAY] VarLiteral
         * TO3 - TO Literal | (expr)
         * TO4 - TO nExpression
         * TO5 - TO PRINTER [PROMPT] | FILE Lit/(Expr)
         * TO6 - TO MEMVAR | NAME objName | ARRAY varname
         * TO7 - 
         * TO8 - TO DEBUG | TO FILE filename
         * TO9 - TO row,col
         * TO* - TO expr,expr
         * TO# - TO literal to EOL
         * TY0 - TYPE C|D|T|N|F|I|B|Y|L
         * TY1 - TYPE XLS|XL5|XLSX|CALC
         * TY2 - TYPE XLS|XL5|XLSX|CALC or SDF|CSV DELIMITED WITH Delimiter | WITH blank | WITH TAB | WITH CHARACTER literal
         * VL0 - Values (varlist)
         * WI0 - WINDOW [AT rowExpr, colExpr]
         * WI1 - WINDOW Name
         * WI2 - WINDOW namelist
         * WH0 - When lExpression
         * WL0 - While lExpression
         * WT0 - With prop=expr
         * WT1 - With litExpr
         * WT2 - WITH litExpr1, litExpr2,...
         * XX0 - Lit/(Expr)
         * XX1 - Expression list for Calc command
         * XX2 - TOP|BOTTOM|expression
         * XX3 - Expression list
         * XX4 - Expression list starting with (
         * XX5 - Var array list with AS option
         * XX6 - List of Literals 
         * XX7 - Var Literal
         * XX8 - Var & Var Array list with AS option
         * XX9 - Literal to End of Line
         * XX* - Expression
         * XX# - Var list starting with (
         * XX@ - Lit/Expr List
         * XX! - FOR variable literal
         * XX: - Directory literal or expression
         * XX$ - Get entire expression
         * XF0 - Path template
         * 
         * -----------------------------------------------------------------------------------------*/
        private string ParseRest(string cmd, string cmdRest)
        {
            //Function and EndFunction are the same as Procedure and EndProcedure
            switch (cmd.ToLower())
            {
                case "function":
                case "func":
                    cmd = "procedure";
                    break;

                case "endfunction":
                case "endfunc":
                    cmd = "endprocedure";
                    break;
            }

            if (cmdRest.Contains("strtofile", StringComparison.OrdinalIgnoreCase))
                cmd = cmd.ToLower();

            string result = cmd.ToLower() switch
            {
                "activate" => JAXBase_Compiler_A.Activate(this, cmdRest),
                "add" => JAXBase_Compiler_A.Add(this, cmdRest),
                "alter" => JAXBase_Compiler_A.Alter(cmdRest),
                "aparameters" => Generic_Parser(cmdRest, "XX7", []),
                "append" => JAXBase_Compiler_A.Append(this, cmdRest),
                "assert" => Generic_Parser(cmdRest, "XX*,MS0,TO8", ["nodialog"]),
                "average" => Generic_Parser(cmdRest, "XX3,SC0,FR0,WL0,TO1", ["nooptimize"]),
                "begin" => Generic_Parser(cmdRest, string.Empty, ["TRANSACTION"]),
                "blank" => Generic_Parser(cmdRest, "FV1,SC0,FR0,WL0,IN0", ["default", "autoinc"]),
                "browse" => Generic_Parser(cmdRest, "FV1,SC0,FR0", ["noappend", "nodelete", "noshow", "nowait"]),
                "build" => throw new Exception("1999|BUILD"),
                "calculate" => Generic_Parser(cmdRest, "XX1,SC0,FR0,WL0,TO1", []),
                "cancel" => string.Empty,
                "cd" => Generic_Parser(cmdRest, "XF0", []),
                "case" => Struct_Parser(cmdRest, "CS", "XX*", []),
                "catch" => Struct_Parser(cmdRest, "TC", "TO0,WH0", []),
                "clear" => JAXBase_Compiler_C.Clear(this, cmdRest),
                "close" => Key_Parser(cmdRest, ["alternate", "database", "debugger", "format", "index", "memo", "procedure", "table", ""], "XX0", ["all"]),
                "compile" => Key_Parser(cmdRest, ["form", "classlib", "label", "report", "program", string.Empty], "XX:,CP0", ["all", "encrypt", "nodebug"]),
                "continue" => string.Empty,
                "copy" => JAXBase_Compiler_C.Copy(this, cmdRest),
                "count" => Generic_Parser(cmdRest, "XX3,SC0,FR0,WL0,TO1", ["nooptimize"]),
                "create" => JAXBase_Compiler_C.Create(this, cmdRest),
                "deactivate" => Key_Parser(cmdRest, ["menu", "popup", "window"], "XX0", ["ALL"]),
                "debug" => string.Empty,
                "debugout" => Generic_Parser(cmdRest, "XX3", []),
                "define" => JAXBase_Compiler_D.Define(this, cmdRest),
                "delete" => JAXBase_Compiler_D.Delete(this, cmdRest),
                "dimension" => Generic_Parser(cmdRest, "XX5", []),
                "directory" => Generic_Parser(cmdRest, "XX:,TO5", []),
                "display" => JAXBase_Compiler_D.Display(this, cmdRest),
                "do" => JAXBase_Compiler_D.Do(this, cmdRest),
                "doevents" => Generic_Parser(cmdRest, string.Empty, ["force"]),
                "dodefault" => string.Empty,
                "drop" => Key_Parser(cmdRest, ["table", "view"], "XX0", []),
                "edit" => StrictBreak(cmdRest, "FV1,SC0,FR0,WH0,NM0,TM0,TT0", ["noappend", "nodelete", "nomodify"], string.Empty),
                "eject" => Generic_Parser(cmdRest, string.Empty, ["page"]),
                "else" => Struct_Parser(cmdRest, "IL", string.Empty, []),
                "elseif" => Struct_Parser(cmdRest, "IS", string.Empty, []),
                "endcase" => Struct_Parser(cmdRest, "CE", string.Empty, []),
                "enddefine" => string.Empty,
                "enddo" => Struct_Parser(cmdRest, "WE", string.Empty, []),
                "endfor" => Struct_Parser(cmdRest, "FE", string.Empty, []),
                "endif" => Struct_Parser(cmdRest, "IE", string.Empty, []),
                "endprocedure" => string.Empty,
                "endscan" => Struct_Parser(cmdRest, "SE", string.Empty, []),
                "endtext" => string.Empty,
                "endtransaction" => string.Empty,
                "endtry" => Struct_Parser(cmdRest, "TE", string.Empty, []),
                "endwith" => Struct_Parser(cmdRest, "HE", string.Empty, []),
                "erase" => Generic_Parser(cmdRest, "XX0", ["recycle"]),
                "error" => Generic_Parser(cmdRest, "XX3", []),
                "exit" => string.Empty,
                "export" => Generic_Parser(cmdRest, "TO3,DB0,NM0,SH0,FR0,AS0", ["type|", "calc|xlsx|tab|csv|sdf"]),
                "external" => Key_Parser(cmdRest, ["file", "array", "class", "form", "label", "library", "menu", "procedure", "query", "report", "screen", "table"], "XX0", []),
                "finally" => Struct_Parser(cmdRest, "TF", string.Empty, []),
                "for" => Struct_Parser(cmdRest, "FR", "XX!,FR1,TO4,ST0", []),
                "foreach" => Struct_Parser(cmdRest, "EA", "XX7,AS0,OF0,IN0", []),
                "gather" => Generic_Parser(cmdRest, "FM1,FV3", ["memo", "blank", "default"]),
                "getexpr" => Generic_Parser(cmdRest, "XX*,TO1,TY0,MS0,DF0", []),
                "goto" => Generic_Parser(cmdRest, "XX2,IN0,SS0", []),
                "help" => Generic_Parser(cmdRest, "XX*", []),
                "if" => Struct_Parser(cmdRest, "IF", "XX$", []),
                "import" => Generic_Parser(cmdRest, "FM0,DB1,NM0,AS0", ["type|", "calc|xlsx|tab|csv|sdf"]),
                "index" => StrictBreak(cmdRest, "ON*,TO3,CO0,FR*", ["ascending|descending", "unique|candidate", "nocase"], "ON*,TO3"),
                "insert" => JAXBase_Compiler_I.Insert(this, cmdRest),
                "keyboard" => Generic_Parser(cmdRest, "XX0", ["plain", "clear"]),
                "list" => JAXBase_Compiler_D.Display(this, cmdRest),
                "local" => Generic_Parser(cmdRest, "XX8", []),
                "locate" => StrictBreak(cmdRest, "FR0,SC0,WH0,IN0,SS0", ["nooptimize"], string.Empty),
                "loop" => string.Empty,
                "lparameters" => Generic_Parser(cmdRest, "XX8", []),
                "lprocedure" => string.Empty,
                "md" => Generic_Parser(cmdRest, "XX0", []),
                "modify" => JAXBase_Compiler_M.Modify(this, cmdRest),
                "mouse" => Key_Parser(cmdRest, ["click", "dblclick"], "AT0,DG0,WI1", ["pixels", "left", "middle", "right", "shift", "control", "alt"]),
                "move" => Key_Parser(cmdRest, ["popup", "window"], "XX0,TO0,BY0", []),
                "nodefault" => string.Empty,
                "on" => ON_Parser(cmdRest).TrimStart(AppClass.expByte).TrimEnd(AppClass.expEnd),    // need to remove bytes as it's part of a command, not a command of it's own.
                "open" => Key_Parser(cmdRest, ["database"], "XX0", ["exclusive", "shared", "noupdate", "validate"]),
                "otherwise" => Struct_Parser(cmdRest, "CO", string.Empty, []),
                "pack" => Generic_Parser(cmdRest, "IN0", ["memo", "dbf"]),
                "parameters" => Generic_Parser(cmdRest, "XX8", []),
                "play" => Key_Parser(cmdRest, ["macro"], "XX0,TI1", []),
                "pop" => Key_Parser(cmdRest, ["key", "menu", "popup"], "XX0", ["all"]),
                "private" => Generic_Parser(cmdRest, "XX8", []),
                "procedure" => Generic_Parser(cmdRest, "XX9", []),
                "protected" => Generic_Parser(cmdRest, "XX0", []),
                "public" => Generic_Parser(cmdRest, "XX8", []),
                "push" => Key_Parser(cmdRest, ["key", "menu", "popup"], "XX0", ["clear"]),
                "quit" => string.Empty,
                "rd" => Generic_Parser(cmdRest, "XX0", []),
                "read" => Generic_Parser(cmdRest, string.Empty, ["events"]),
                "recall" => Generic_Parser(cmdRest, "SC0,FR0,WH0,IN0", ["nooptimize"]),
                "register" => Key_Parser(cmdRest, ["", "image", "sound", "video"], "XX*,AS0", []),
                "reindex" => string.Empty,
                "release" => Key_Parser(cmdRest, ["", "classlib", "console", "procedure"], "XX7", ["all"]),
                "rename" => JAXBase_Compiler_R.Rename(this, cmdRest),
                "remove" => Key_Parser(cmdRest, ["classlib", "table"], "XX0,OF0", ["all"]),
                "replace" => JAXBase_Compiler_R.Replace(this, cmdRest),
                "restore" => Key_Parser(cmdRest, ["", "macros"], "FM0,AL0", ["additive"]),
                "resume" => string.Empty,
                "retry" => string.Empty,
                "return" => Generic_Parser(cmdRest, "XX*,TO3", []),
                "rollback" => string.Empty,
                "run" => Generic_Parser(cmdRest, "XX9", ["/N", "/M"]),
                "save" => Key_Parser(cmdRest, ["", "macros"], "FM0,AL0", ["additive"]),
                "scan" => Struct_Parser(cmdRest, "SC", "SC0,FF0,WH0", ["nooptimize"]),
                "scatter" => Generic_Parser(cmdRest, "FV3,TO6", ["memo", "blank", "default"]),
                "seek" => Generic_Parser(cmdRest, "XX*,OR0,IN0,SS0", ["ascending|descending"]),
                "select" => JAXBase_Compiler_S.Select(this, cmdRest),
                "set" => Key_Parser(cmdRest, JAXLanguageLists.SetCommands, "XX0,SI0,IN0,SS0,AT0,TO3,WT2", []),
                "skip" => Generic_Parser(cmdRest, "XX*,IN0,SS0", []),
                "sort" => JAXBase_Compiler_S.Sort(this, cmdRest),
                "store" => BreakStoreStatement(cmdRest),
                "sum" => Generic_Parser(cmdRest, "XX3,SC0,FR0,WL0,TO1", []),
                "suspend" => string.Empty,
                "text" => Generic_Parser(cmdRest, "TO0,FG0,PT0", ["additive", "textmerge", "noshow"]),
                "throw" => Generic_Parser(cmdRest, "XX*", []),
                "total" => Generic_Parser(cmdRest, "TO0,FV1,SC0,FR0,WH0", ["nooptimize"]),
                "try" => Struct_Parser(cmdRest, "TR", string.Empty, []),
                "unlock" => Generic_Parser(cmdRest, "RC0,IN0", ["all"]),
                "until" => JAXBase_Compiler_U.Until(this, cmdRest),
                "update" => JAXBase_Compiler_U.Update(this, cmdRest),
                "use" => Generic_Parser(cmdRest, "XX0,IN0,AL0,IX1", ["again", "shared|exclusive", "noupdate"]),
                "wait" => Key_Parser(cmdRest, ["window", string.Empty], "XX*,TO0,TI0,WI0", ["nowait", "clear|noclear"]),
                "with" => Struct_Parser(cmdRest, "WH", "XX7", []),
                "zap" => Generic_Parser(cmdRest, "IN0,SS0", []),
                "?" => Generic_Parser(cmdRest, "XX3", []),
                "??" => Generic_Parser(cmdRest, "XX3", []),
                "~~~" => CompilerXRef["XX"].ToString() + AppClass.literalStart + cmdRest + AppClass.literalEnd,    // The object call will parse it out
                _ => Generic_Parser(cmdRest, "XX*", [])
            };

            AppIO.DebugLog($"adding {result.Length} bytes");
            return result;
        }


        /// <summary>
        /// Compile one or more lines of JAXCode source
        /// </summary>
        /// <param name="cmdBlock"></param>
        /// <param name="inCompile"></param>
        /// <param name="errorCount"></param>
        /// <returns>string containing PCode of source</returns>
        public string CompileBlock(string cmdBlock, bool inCompile, out int errorCount)
        {
            StringBuilder cmpBlock = new();
            StringBuilder cmpLine = new();
            errorCount = 0;
            lineNo = 0;
            bool includeSource = Program.CurrentApp.CurrentDS.JaxSettings.IncludeSource && Program.CurrentApp.InCompile;


            string[] block = cmdBlock.Replace("\n", "").Split('\r');
            AppIO.DebugLog($"Compiling {block.Length} lines");

            for (int i = 0; i < block.Length; i++)
            {
                lineNo++;
                string blockLine = block[i].Trim();

                if (blockLine.Contains("console", StringComparison.OrdinalIgnoreCase))
                {
                    int iii = 0;
                }

                if (blockLine.Length > 0)
                {
                    string ln;
                    if (blockLine.Contains("&&"))
                    {
                        // There is a comment at the end of the line... lop it off!
                        ln = blockLine[..blockLine.IndexOf("&&")].Trim();
                    }
                    else
                        ln = blockLine;

                    if (ln.Length > 0 && ln[^1] == ';')
                    {
                        // Add this line and look for another
                        cmpLine.Append(ln.TrimEnd(';').TrimEnd() + " ");
                    }
                    else
                    {
                        cmpLine.Append(ln);
                        AppIO.DebugLog($"{lineNo}: {ln}");

                        string cLine = cmpLine.ToString().Trim();
                        string cmdLine = CompileLine(cLine, inCompile).Trim();

                        Program.CurrentApp.utl.Conv64(lineNo, 2, out string lnNo);

                        // Process the command
                        if (Program.CurrentApp.InCompile && includeSource && cLine.Length > 0)
                            cmpBlock.Append(AppClass.cmdByte + Program.CurrentApp.MiscInfo["sourcecode"] + CompilerXRef["CM"].ToString() + AppClass.literalStart.ToString() + cLine + AppClass.literalEnd.ToString() + AppClass.cmdEnd + lnNo);

                        // Only append line number if there is something returned
                        if (cmdLine.Length > 0) cmpBlock.Append(cmdLine + lnNo);

                        cmpLine = new();
                    }
                }
            }

            //AppIO.DebugLog($"{cmpBlock.Length} bytes in block");

            // If there is something in the loop stack, we have a problem
            string lp = AppLoop.GetLoopStack();

            while (lp.Length > 0)
            {
                AppIO.DebugLog($"LoopStack is not empty");
                AppLoop.PopLoopStack();

                switch (lp[0])
                {
                    case 'C':   // DO CASE
                        AppErrorHandling.SetError(1939, "1939|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'H':   // WITH
                        AppErrorHandling.SetError(1939, "1939|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'I':   // IF
                        AppErrorHandling.SetError(1211, "1211|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'S':   // SCAN
                        AppErrorHandling.SetError(1939, "1939|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'T':   // TRY
                        AppErrorHandling.SetError(2058, "2058|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'W':   // DO WHILE
                        AppErrorHandling.SetError(1939, "1939|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case 'X':   // TEXT
                        AppErrorHandling.SetError(1939, "1939|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;
                }

                // anything else?
                lp = AppLoop.GetLoopStack();
            }

            AppIO.DebugLog("CompileBlock complete");
            return cmpBlock.ToString();
        }


        /// <summary>
        /// Compile a single line of JAXBase source code
        /// </summary>
        /// <param name="cmdLine"></param>
        /// <param name="inCompile"></param>
        /// <returns>string of resulting PCode</returns>
        /// <exception cref="Exception"></exception>
        public string CompileLine(string cmdLine, bool inCompile)
        {
            string result = string.Empty;
            int iCmd = -1;
            string b64 = string.Empty;
            string cmdRest = string.Empty;
            string cmdHold = cmdLine;

            if (string.IsNullOrWhiteSpace(cmdLine) == false)
            {
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode = cmdLine;

                // Get the leading token or variable/object name
                string c = GetNextToken(cmdLine, " )]=", out string cmd);

                // Special case for something like a[b[1]]=4 as
                // we actually want the last paren/bracket to
                // be part of the command
                if (c.Length > 0 && ")]".Contains(c[0]))
                {
                    cmd += c[..1];
                    c = c[1..];
                }

                // clean out leading/trailing spaces from rest of the command line
                cmdRest = c.Trim();

                // cmdRest should now have the rest of the line of code
                // and cmd should have the command token
                if (JAXLib.InListC(cmd, "~~~", "*sc"))
                {
                    throw new Exception("10||Internal commands are not allowed in source code: " + cmd);
                }

                if ((cmd.Length > 1 && cmd[..2] == "&&") || (cmd.Length > 0 && cmd[0] == '*'))
                {
                    // Remark
                    iCmd = 10000;
                }
                else if (cmd == "&")
                {
                    // Macro Expansion
                    cmdRest = cmdLine[1..];
                }
                else if (cmdRest.Length > 0 && cmdRest[0] == '=')
                {
                    // If there is an equals sign following the token, assume
                    // it's a variable and make it into a store command
                    // The variable could also be an object.property,
                    // object.object.property, and so on.
                    iCmd = Program.CurrentApp.CmdList.IndexOf("store");

                    if (cmd.Length > 0)
                        cmdRest = cmdRest[1..].Trim() + " to " + cmd;
                }
                else if (cmd.Contains("."))
                {
                    /*
                     * Expecting a legal object call like Frm.Show but could be more complex with
                     * something like FRM.PGFRAME1.PAGE1.TEXTBOX.REFRESH
                     * 
                     */
                    cmdRest = cmd + cmdRest; // cmdRest may hold a parameter list, empty or otherwise

                    if (cmdRest[0] == '.')
                    {
                        // Definitely an object call and should be inside a with
                        if (Program.CurrentApp.WithHold.Count == 0)
                            throw new Exception("1940||Empty WITH stack");
                    }

                    cmd = "~~~";    // Command for an object.method call, but is unpublished and won't parse from source code
                    iCmd = Program.CurrentApp.CmdList.FindIndex(a => a.StartsWith(cmd, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    switch (cmd.ToLower())
                    {
                        case "aparameter": cmd = "aparameters"; break;
                        case "brow": cmd = "browse"; break;
                        case "calc": cmd = "calculate"; break;
                        case "comp": cmd = "compile"; break;
                        case "def": cmd = "define"; break;
                        case "del": cmd = "delete"; break;
                        case "dim": cmd = "dimension"; break;
                        case "dir": cmd = "directory"; break;
                        case "doevent": cmd = "doevents"; break;
                        case "enddef": cmd = "enddefine"; break;
                        case "endfunc": cmd = "endfunction"; break;
                        case "endproc": cmd = "endprocedure"; break;
                        case "endtran": cmd = "endtransaction"; break;
                        case "func": cmd = "function"; break;
                        case "gath": cmd = "gather"; break;
                        case "keyb": cmd = "keyboard"; break;
                        case "loc": cmd = "locate"; break;
                        case "lparameter": cmd = "lparameters"; break;
                        case "lproc": cmd = "lprocedure"; break;
                        case "modi": cmd = "modify"; break;
                        case "parameter": cmd = "parameters"; break;
                        case "proc": cmd = "procedure"; break;
                        case "repl": cmd = "replace"; break;
                        case "scat": cmd = "scatter"; break;
                    }

                    // Expecting a command to parse here
                    iCmd = Program.CurrentApp.CmdList.IndexOf(cmd.ToLower());
                    if (iCmd < 0 && cmd.Length > 2)
                    {
                        iCmd = Program.CurrentApp.CmdList.FindIndex(a => a.StartsWith(cmd, StringComparison.OrdinalIgnoreCase)); // TODO - Needs work to deal with (
                    }

                    if (iCmd < 0)
                    {
                        // assuming that it's an expression that needs to
                        // begin with an equals sign.  Will fail if not.
                        result = CompileLine("=" + cmdHold, inCompile);
                        iCmd = 1000;
                    }
                }

                if (iCmd >= 0)
                {
                    // iCmd>998 is a "just skip this" flag
                    if (iCmd < 999)
                    {
                        // Got the command, so convert to 2 char token!
                        Program.CurrentApp.utl.Conv64(iCmd, 2, out b64);

                        // Process the command
                        result += AppClass.cmdByte + b64 + ParseRest(JAXLanguageLists.JAXCommands[iCmd], cmdRest) + AppClass.cmdEnd;
                    }
                }
                else
                {
                    if (iCmd < 0)
                    {
                        // TODO throw an error
                        throw new Exception("20|");
                    }
                }
            }

            // Command is in format <command begin byte><2 digit Base36>[<parameter1 string><parameter end byte>[<parameter2 string>...]<command end byte>
            return result;
        }


        public string Generic_Parser(string cmdRest, string ParseInfo, string[] Flags)
        {
            string result = string.Empty;
            List<string> thisParse = [];

            try
            {
                string[] ParsingParts = ParseInfo.Split(',');

                // Get the CompilerCodes translated to the CompilerDictionary Keys
                // The FW code is ignored as it's a combination of FIELDS and WITH
                if (ParsingParts[0].Length > 0)
                {
                    for (int i = 0; i < ParsingParts.Length; i++)
                    {
                        if (JAXLib.InListC(ParsingParts[i][..2], "fw") == false)
                            thisParse.Add(CompilerCodes[ParsingParts[i][..2]]);

                        if (JAXLib.InListC(ParsingParts[i], "XX8", "XX5"))
                            thisParse.Add(CompilerCodes["AS"]);
                    }

                    Dictionary<string, string> code = StatementBreak(cmdRest, ParseInfo, Flags);

                    // Put the codes that have information in them
                    // together with their XREF code into the statement
                    foreach (string pCode in ParsingParts)
                    {
                        string p = pCode[..2].ToString();
                        if (JAXLib.InListC(pCode, "XX8", "XX5"))
                        {
                            result += CompilerXRef["XX"].ToString() + code["expressions"] + AppClass.stmtDelimiter + CompilerXRef["AS"].ToString() + code["as"] + AppClass.stmtDelimiter;
                        }
                        else if (JAXLib.InListC(p, "fw"))
                        {
                            // FW is FIELDS and WITH
                            result += CompilerXRef["FV"].ToString() + code["fields"] + AppClass.stmtDelimiter + CompilerXRef["WT"].ToString() + code["with"] + AppClass.stmtDelimiter;
                        }
                        else
                        {
                            if (code[CompilerCodes[p]].Length > 0)
                                result += CompilerXRef[p].ToString() + code[CompilerCodes[p]] + AppClass.stmtDelimiter;
                        }
                    }

                    if (code["flags"].Length > 0)
                        result += CompilerXRef["FG"].ToString() + AppClass.literalStart + code["flags"].ToString() + AppClass.literalEnd + AppClass.stmtDelimiter;

                    result = result.TrimEnd(AppClass.stmtDelimiter);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }


        /* -----------------------------------------------------------------------------------------*
         * This parser handles the commands related to DO While, Do Until, Do Case, If, Try,
         * Scan, and PrintJob to provide nesting control & quick movement when an Exit
         * or Loop command is encountered.
         * 
         * Do Case, If, Try, and PrintJob commands are no loops, so the structure is pushed
         * to the loop stack during compile but not execution.  Once compiled, the loop code
         * has been imbedded into these and related commands.
         * 
         * Flag Description
         * ---- -------------------------------------
         * BE   ENDTRANSACTION
         * BT   BEGIN TRANSACTION
         * CC   CASE <expression>
         * CD   DO CASE
         * CE   CASE END
         * CO   CASE OTHERWISE
         * HE   ENDWITH
         * EA   FOREACH
         * FR   FOR
         * FE   ENDFOR
         * IF   IF
         * IE   ENDIF
         * IL   ELSE
         * PE   ENDPRINT
         * PJ   PRINTJOB
         * SC   SCAN
         * SE   ENDSCAN
         * TC   CATCH
         * TE   ENDTRY
         * TF   FINALLY
         * TR   TRY
         * UE   UNTIL end of loop
         * UD   DO for until loop
         * WD   DO WHILE
         * WE   ENDDO for while
         * WH   WITH start
         * 
         * -----------------------------------------------------------------------------------------*/
        public string Struct_Parser(string cmdRest, string Type, string ParseInfo, string[] Flags)
        {
            string result = string.Empty;
            List<string> thisParse = [];

            try
            {
                if (JAXLib.InListC(Type, "WH", "HE"))
                {
                    if (Type.Equals("WH"))
                    {
                        if (string.IsNullOrWhiteSpace(cmdRest))
                            throw new Exception("10||Missing WITH variable");

                        // Add a WITH statement - save the object name in
                        // case we need it in the future
                        Program.CurrentApp.WithHold.Add(cmdRest);
                        cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out string withExpr);
                        result = CompilerXRef["XX"].ToString() + withExpr;
                    }
                    else
                    {
                        if (Program.CurrentApp.WithHold.Count > 0)
                        {
                            // Pop the most recent one off.  We do this just to make
                            // sure there is an EndWith for every With statement
                            Program.CurrentApp.WithHold.RemoveAt(Program.CurrentApp.WithHold.Count - 1);
                            result = "  ";
                        }
                    }
                }
                else if (JAXLib.InListC(Type, "CD", "WD", "UD", "BT", "TR", "PJ", "IF", "FR", "EA", "SC"))
                {
                    // If it's a start code then push the loop type to the loop stack
                    // Do Case, Do While, Do Until, Transaction, Try, PrintJob, If, For, Scan
                    result = CompilerXRef["CS"].ToString() + AppLoop.AddLoop(Type[0].ToString()) + AppClass.stmtDelimiter;
                }
                else if (JAXLib.InListC(Type, "IS"))
                {
                    // ELSEIF
                    result = AppLoop.GetLoopStack();
                    if (result[0] == Type[0])
                        result = CompilerXRef["CS"].ToString() + result + AppClass.stmtDelimiter;
                    else
                        result = string.Empty;
                }
                else if (JAXLib.InListC(Type, "CS", "CO", "TC", "TF", "IL"))
                {
                    // Case, Otherwise, Catch, Finally, Else
                    result = AppLoop.GetLoopStack();
                    if (result[0] == Type[0])
                        result = CompilerXRef["CS"].ToString() + result + AppClass.stmtDelimiter;
                    else
                        result = string.Empty;
                }
                else if (JAXLib.InListC(Type, "CE", "WE", "UE", "BE", "TE", "PE", "IE", "FE", "SE"))
                {
                    // End Case, End DoWhile, Until End, End BeginTransaction, End Try, End PrintJob, End If, EndFor, End Scan
                    result = AppLoop.PopLoopStack();

                    if (result[0] == Type[0])
                        result = CompilerXRef["CS"].ToString() + result + AppClass.stmtDelimiter;
                    else
                        result = string.Empty;
                }

                // Was an error tossed?
                if (result.Length < 2)
                {
                    switch (Type[0])
                    {
                        case 'C':       // End Case
                            throw new Exception("1213|");

                        case 'F':       // End For
                            throw new Exception("1207|");

                        case 'I':       // End If
                            throw new Exception("1211|");

                        case 'R':       // End Transaction
                            throw new Exception("1591|");

                        case 'S':       // End Scan
                            throw new Exception("1203|");

                        case 'T':       // EndTry
                            throw new Exception("2058|");

                        case 'U':       // End Until
                            throw new Exception("1210|");

                        case 'W':       // End While
                            throw new Exception("1209|");

                        case 'H':
                            throw new Exception("1939|");

                        default:        // Unknown - system error
                            throw new Exception(string.Format("1999|{0}|Unimplemented structure command", result));
                    }
                }

                // Is there something to parse?
                if (ParseInfo.Length > 0 && cmdRest.Length > 0)
                {
                    // Break everything up
                    string[] ParsingParts = ParseInfo.Split(',');

                    // Get the CompilerCodes translated to the CompilerDictionary Keys
                    //for (int i = 0; i < ParsingParts.Length; i++)
                    //    thisParse.Add(CompilerCodes[ParsingParts[i][..2]]);

                    Dictionary<string, string> code;

                    switch (Type)
                    {
                        case "FR":
                            result += StrictBreak(cmdRest, ParseInfo, Flags, "XX!,FR1,TO4");
                            break;

                        case "EA":
                            result += StrictBreak(cmdRest, ParseInfo, Flags, "XX7,IN0");
                            break;

                        default:
                            code = StatementBreak(cmdRest, ParseInfo, Flags);
                            foreach (string pCode in ParsingParts)
                            {
                                string p = pCode[..2];
                                if (code[CompilerCodes[p]].Length > 0)
                                    result += CompilerXRef[p].ToString() + code[CompilerCodes[p]] + AppClass.stmtDelimiter;
                            }

                            result = result.TrimEnd(AppClass.stmtDelimiter);  // get rid of trailing statement delimiters
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }



        /* -----------------------------------------------------------------------------------------*
         * The ON command requires it's own parsing routine
         * -----------------------------------------------------------------------------------------*/
        public string ON_Parser(string cmdRest)
        {
            string result = "";

            try
            {
                if (string.IsNullOrWhiteSpace(cmdRest) == false)
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out string onCmd);

                    switch (onCmd.ToLower())
                    {
                        case "error":
                            // Set up the ON ERROR command
                            result = CompilerXRef["CS"].ToString() + "E" + AppClass.stmtDelimiter;
                            break;

                        case "key":
                            // There has to be a LABEL command next
                            cmdRest = GetNextToken(cmdRest, string.Empty, out onCmd);
                            if (onCmd.Equals("label", StringComparison.OrdinalIgnoreCase))
                            {
                                if (string.IsNullOrEmpty(cmdRest))
                                    throw new Exception("10|");
                                else
                                {
                                    cmdRest = GetNextLiteralOrExpression(cmdRest, "", out onCmd);

                                    // Get the key label
                                    KeyClass key = AppIO.KeyLabel(onCmd);

                                    // Build the dictionary for the command to execute
                                    string keylabel = key.CTRL ? "CTRL+" : "";
                                    keylabel += key.ALT ? "ALT+" : "";
                                    keylabel += key.SHIFT ? "SHIFT+" : "";
                                    keylabel += key.keyLabel;

                                    result = CompilerXRef["CS"].ToString() + "L" + AppClass.expDelimiter + CompilerXRef["ON"].ToString() + keylabel;
                                }

                                // Set up the ON KEY LABEL command
                                ;
                            }
                            else
                                throw new Exception("10|");
                            break;

                        case "shutdown":
                            // Set up the ON SHUTDOWN command
                            result = CompilerXRef["CS"].ToString() + "S" + AppClass.stmtDelimiter;
                            break;

                        default:
                            throw new Exception("10|");
                    }

                    // Add the command source - syntax is checked if the ON command is executed
                    result += CompilerXRef["CM"] + cmdRest + AppClass.stmtDelimiter;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }

        /* -----------------------------------------------------------------------------------------*
         * The keyword is expected to be the first token and then the rest is passed onto the 
         * generic parser. The key array must hold lower case keys.
         * -----------------------------------------------------------------------------------------*/
        public string Key_Parser(string cmdRest, string[] keys, string ParseInfo, string[] Flags)
        {
            string result = string.Empty;
            string hold = cmdRest;

            try
            {
                int f = -1;

                if (string.IsNullOrWhiteSpace(cmdRest) == false)
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out string key);

                    // Some commands assume an empty key means something
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(keys[i]))
                        {
                            // The empty string has to be last in the list
                            // for this to work corectly
                            cmdRest = (key + " " + cmdRest).Trim();
                            key = string.Empty;
                            f = i;
                            break;
                        }
                        else
                        {
                            if (key.Equals(keys[i], StringComparison.OrdinalIgnoreCase))
                            {
                                // Found a matching key
                                f = i;
                                key = keys[i];
                                break;
                            }
                            else if (keys.Length == 1 && key.Length > 2 && keys[i].StartsWith(key, StringComparison.OrdinalIgnoreCase))
                            {
                                // Found a matching key based on first several letters matched
                                // against just the one key element.  This must only happen
                                // under controlled circumstances, otherwise you may just end
                                // up making a hash of it.
                                f = i;
                                key = keys[i];
                                break;
                            }
                        }
                    }

                    if (f < 0) throw new Exception("10||Invalid key " + key);

                    if (key.Length > 0)
                        result = CompilerXRef["CS"].ToString() + key.ToUpper() + AppClass.stmtDelimiter + Generic_Parser(cmdRest, ParseInfo, Flags);
                    else
                        result = Generic_Parser(cmdRest, ParseInfo, Flags);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result = string.Empty;
            }

            return result;
        }


        /* -----------------------------------------------------------------------------------------*
         * This routine is used to grab the next name or expression, where the next thing
         * is a literal, such as a work area or table name, or can be substituted
         * with an expression in () such as in the statements:
         * 
         *      Statement           Returns
         *      SKIP 1 IN A         Literal A
         *      SKIP 1 IN (A+3)     Expression (A+3)
         *      SKIP 1 IN 5         Expression 5
         * 
         * After IN can be a name, a number, or an expression in parenthesis and this routine will
         * return a name as a literal and everything else as an expression string.
         * 
         * Returns remainder of command
         * Sends out expression string
         * -----------------------------------------------------------------------------------------*/
        /// <summary>
        /// Return an expression or named literal.  AB+3 is an expression, but AB is a literal.
        /// </summary>
        /// <param name="command">Command string to parse</param>
        /// <param name="tokens">Terminating tokens (space assumed)</param>
        /// <param name="exprResult">Compiled RPN string</param>
        /// <returns>Returns the remainder of the command string</returns>
        public string GetNextLiteralOrExpression(string command, string tokens, out string exprResult)
        {
            string cmd = command.Trim();
            string expr = string.Empty;

            // It's an (expression)
            cmd = GetNextExpression(command, tokens, out exprResult);

            if (exprResult.Contains(AppClass.expParam) == false)
            {
                // Strip off the start/end bytes to see if it is a name or a number
                string cmdTest = exprResult.TrimStart(AppClass.expByte);
                cmdTest = cmdTest.TrimEnd(AppClass.expEnd);

                // Does it start with a letter or underscore and is it only
                // letters and numbers?
                if (cmdTest[0] == '_' && Regex.IsMatch(cmdTest[1..], @"^[a-zA-Z_$][a-zA-Z_$0-9]*$"))
                {
                    // It came back as just a var name so make it int a literal
                    exprResult = AppClass.literalStart + cmdTest[1..] + AppClass.literalEnd;
                }
            }

            return cmd;
        }

        /// <summary>
        /// Return a literal name string unless surrounded by () which indicates an expression
        /// </summary>
        /// <param name="command">Command string to parse</param>
        /// <param name="tokens">Terminating tokens (space assumed)</param>
        /// <param name="exprResult">Compiled RPN string</param>
        /// <returns>Returns the remainder of the command string</returns>
        public string GetNameLiteralOrExpression(string command, string tokens, out string exprResult)
        {
            string cmd = command.Trim();
            string expr = string.Empty;

            if (cmd[..1].Equals("(") || "0123456789".Contains(cmd[..1]))
            {
                // It's an (expression)
                cmd = GetNextExpression(command, tokens, out exprResult);
            }
            else
            {
                // Grab the next expression
                cmd = GetNextToken(command, tokens, out exprResult);

                // Now make sure it's just letters, numbers, and underscores
                if (Regex.IsMatch(exprResult[..1], @"^[a-zA-Z_]+$"))
                    exprResult = AppClass.literalStart + exprResult + AppClass.literalEnd; // Wrap it up as a literal
                else
                    throw new Exception("11|"); // Toss an error
            }

            return cmd;
        }


        /// <summary>
        /// Return a literal path+file string unless surrounded by () which indicates an expression
        /// </summary>
        /// <param name="command">Command string to parse</param>
        /// <param name="tokens">Terminating tokens (space assumed)</param>
        /// <param name="exprResult">Compiled RPN string</param>
        /// <returns>Returns the remainder of the command string</returns>
        public string GetFileLiteralOrExpression(string command, string tokens, out string exprResult)
        {
            string cmd = command.Trim();
            string expr = string.Empty;
            exprResult = "";

            if (cmd.Length > 0)
            {
                if (cmd[..1].Equals("("))
                {
                    // It's an (expression)
                    cmd = GetNextExpression(command, tokens, out exprResult);
                }
                else
                {
                    // Grab the next expression
                    cmd = GetNextToken(command, tokens, out exprResult);

                    // Now make sure it's just letters, numbers, and underscores
                    if (Regex.IsMatch(exprResult[..1], @"^[a-zA-Z0-9_\.\\*?]+$"))
                        exprResult = AppClass.literalStart + exprResult + AppClass.literalEnd; // Wrap it up as a literal
                    else
                        throw new Exception("11|"); // Toss an error
                }
            }

            return cmd;
        }


        /// <summary>
        /// Return a directory literal unless surrounded by () which indicates an expression
        /// </summary>
        /// <param name="command">Command string to parse</param>
        /// <param name="tokens">Terminating tokens (space assumed)</param>
        /// <param name="exprResult">Compiled RPN string</param>
        /// <returns>Returns the remainder of the command string</returns>
        public string GetDirectoryLiteralOrExpression(string command, string tokens, out string exprResult)
        {
            string cmd = command.Trim();
            string expr = string.Empty;

            if (cmd[..1].Equals("("))
            {
                // It's an (expression)
                cmd = GetNextExpression(command, tokens, out exprResult);
            }
            else
            {
                // Grab the next literal
                cmd = GetNextToken(command, string.Empty, out exprResult);

                // Now confirm it's a valid path\file
                if (JAXLib.IsValidPathFile(exprResult) > 0)
                    throw new Exception("202|cmd");
                else
                    exprResult = AppClass.literalStart + exprResult + AppClass.literalEnd; // Wrap it up as a literal
            }

            return cmd;
        }


        /// <summary>
        /// Looks for a string in a string that is not inside an expression (defined as 
        /// between quotes, brackets, or parentheses)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="findToken"></param>
        /// <returns>int of location, -1 if not found</returns>
        public int FindCommandString(string command, string findToken)
        {
            int f = -1;
            char inQuote = '\0';
            findToken = " " + findToken.Trim() + " ";

            for (int i = 0; i < command.Length; i++)
            {
                if (inQuote == '\0')
                {
                    if (i + findToken.Length < command.Length)
                    {
                        // Is it in-line and not at end of statement
                        if (command.Substring(i, findToken.Length).Equals(findToken, StringComparison.OrdinalIgnoreCase))
                        {
                            f = i + 1;
                            break;
                        }
                    }

                    if (i + findToken.Length - 1 == command.Length - 1)
                    {
                        // In case it's at the end of the statement
                        if (command.Substring(i, findToken.Length - 1).Equals(findToken.TrimEnd(), StringComparison.OrdinalIgnoreCase))
                        {
                            f = i + 1;
                            break;
                        }
                    }

                    // look for a quote or expression character
                    inQuote = command[i] switch
                    {
                        '"' => '"',
                        '\'' => '\'',
                        '[' => ']',
                        '(' => ')',
                        _ => '\0'
                    };
                }
                else
                    inQuote = command[i].Equals(inQuote) ? '\0' : inQuote;  // Clear flag because we found end of the  quoted material
            }

            return f;
        }

        /*
         * Simple STORE statement breaker
         * 
         */
        public string BreakStoreStatement(string command)
        {
            string result = string.Empty;
            string exprResult = string.Empty;

            try
            {
                // TODO - Last Index isn't good enough
                // What if we get just MESSAGEBOX("Welcome to hell?")
                string formula = string.Empty;
                string target = string.Empty;
                if (command[0] == '=')
                {
                    formula = command[1..].Trim();
                }
                else
                {
                    int f = command.ToLower().LastIndexOf(" to ");
                    if (f > 0)
                    {
                        target = command[(f + 3)..].Trim();
                        formula = command[..f].Trim();
                    }
                    else
                    {
                        formula = command;
                    }
                }

                if (formula.Length > 0)
                {
                    StringBuilder sb = new();
                    JAXMathAux aux = new();
                    List<string> parsed = aux.MathParse(formula);
                    List<string> rpn = aux.MathMakeRPN(parsed);

                    sb.Append(rpn[0]);

                    for (int ii = 1; ii < rpn.Count; ii++)
                        if (rpn[ii].Equals("(") || rpn[ii].Equals(")"))
                        {
                            // Skip this as RPN should not have parens
                        }
                        else
                            sb.Append(AppClass.expParam + rpn[ii]);

                    exprResult = AppClass.expByte + sb.ToString() + AppClass.expEnd;
                }
                else
                    throw new Exception("GROK FAILED!");

                result = CompilerXRef["XX"].ToString() + exprResult;

                if (string.IsNullOrWhiteSpace(target) == false)
                    result += AppClass.stmtDelimiter + CompilerXRef["TO"].ToString() + AppClass.literalStart + target + AppClass.literalEnd;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return result;
        }


        /* -----------------------------------------------------------------------------------------*
         * Get an expression from the string.
         * 
         * Start at the begining of the string and consume tokens until the
         * expression ends.  It may have spaces, so we have to intelligently
         * look for other signs.
         * 
         * (A[1,2]+B(3) *CalcSum.balance) /17+linkVal1 TO Field1
         *                                             ^ end of expression
         *                                               
         * You will continue to the end of the line unless you run into
         * the breaking of an expression string.  Expression items are
         * held together with:
         *      () [] + - * / ^ % = <> != > < >= <= " ' and or not
         * 
         * Variables/function names can't have spaces in them (see above)
         * 
         * 
         * Default processing occurs if tokens is string.Empty
         * 
         * UNDER REVIEW:
         * , but you can
         * provide a pipe delimited string and if any of those substrings
         * are found using a case-insensitive match, then if they are not
         * part of an enclosing structure () [] "" '' then that is the
         * assumed end of an expression.
         * 
         * Example:
         * 
         * a=GetNextExpression("23+97/BG1 to Balance"," TO ",out ...
         *      The expression will be everything up to " TO " (case insensitive)
         *      regardless of any other consideration
         * 
         * 
         * NOTE:
         *      The expession can be a complete mess, syntax wise, but as long as
         *      it follows the rules, we'll let it go through here and catch the syntax
         *      problems in the math parsing routines
         *      
         * Return the remaining line info and send OUT the expression
         * 
         * -----------------------------------------------------------------------------------------*/
        /// <summary>
        /// Extract an expression from the beginning of a command string, sending it out to
        /// the exprResut as a compiled RPN string
        /// </summary>
        /// <param name="command">Command string to parse</param>
        /// <param name="tokens">Terminating tokens (space assumed)</param>
        /// <param name="exprResult">Compiled RPN string</param>
        /// <returns>Remainder of command string</returns>
        public string GetNextExpression(string command, string? tokens, out string exprResult)
        {
            return GetNextExpression(command, tokens, false, out exprResult);
        }

        public string GetNextExpression(string command, string? tokens, bool ReturnAsIs, out string exprResult)
        {
            string cmd = string.Empty;
            exprResult = string.Empty;

            try
            {

                // TODO - make sure these commands are not in quoted text
                string cmdLine = command.Trim().
                    Replace(".and.", " and ", StringComparison.OrdinalIgnoreCase).
                    Replace(".or.", " or ", StringComparison.OrdinalIgnoreCase).
                    Replace(".not.", " not ", StringComparison.OrdinalIgnoreCase);    // clean up leading and trailing spaces

                string hold = cmdLine;

                char inQuote = '\0';
                int i = 0;
                int quoteCount = 0;

                bool keepLooping = cmdLine.Length > 0;

                // Special case - if null, just grab everything to end of the command
                if (tokens is null)
                {
                    exprResult = command;
                    keepLooping = false;
                }

                while (keepLooping)
                {
                    // Did we find a token that ends the expression?                    
                    if (inQuote == '\0' && tokens!.Length > 0 && tokens.Contains(cmdLine[i]))
                    {
                        keepLooping = false;
                    }

                    // Look for the beginning of a quote or enclosing structure
                    if ("(['\"".Contains(cmdLine[i]) && inQuote == '\0')
                    {
                        // Found an enclosing structure, set up what to
                        // look for to close the structure
                        inQuote = cmdLine[i] switch
                        {
                            '[' => ']',
                            '(' => ')',
                            _ => cmdLine[i]
                        };
                        quoteCount++;
                    }
                    else if (inQuote == '\0' && cmdLine[i] == ',')
                    {
                        // found a comma outside of quote so it's an automatic expression delimiter
                        keepLooping = false;
                    }
                    else if (cmdLine[i] == inQuote)
                    {
                        // Decrement the quote count
                        quoteCount--;
                        if (inQuote != '\0' && quoteCount == 0) inQuote = '\0'; // We found the end of the quote
                    }
                    else if (inQuote == ')' || inQuote == ']')
                    {
                        // Keep track of other parens and brackets
                        if (inQuote == ')' && cmdLine[i] == '(') quoteCount++;
                        if (inQuote == ')' && cmdLine[i] == ')') quoteCount--;

                        if (inQuote == ']' && cmdLine[i] == '[') quoteCount++;
                        if (inQuote == ']' && cmdLine[i] == ']') quoteCount--;

                        // If in quote and count = 0, clear the inQuote flag
                        if (inQuote != '\0' && quoteCount == 0) inQuote = '\0'; // We found the end of the quote
                    }

                    if (keepLooping == false)
                    {
                        // We found the end of the expression in the string
                        exprResult = cmdLine[..i].Trim();
                        cmd = cmdLine[i..];
                    }
                    else if (i >= cmdLine.Length - 1)
                    {
                        // We found the end of the string
                        exprResult = cmdLine;
                        cmd = string.Empty;
                        keepLooping = false;
                    }

                    // Won't execute when on the last character of the expression
                    if (keepLooping && i < cmdLine.Length - 1)
                        i++;
                }

                if (exprResult.Length > 0)
                {
                    // Run it through to get the RPN string which will
                    // also validate the expression as not having any
                    // obvious syntax errors
                    StringBuilder sb = new();
                    JAXMathAux aux = new();

                    if (exprResult.Contains("seek", StringComparison.OrdinalIgnoreCase))
                    {
                        int iii = 0;
                    }
                    List<string> parsed = aux.MathParse(exprResult);
                    List<string> rpn = aux.MathMakeRPN(parsed);

                    sb.Append(rpn[0]);

                    // Create the RPN string
                    for (int ii = 1; ii < rpn.Count; ii++)
                    {
                        if (rpn[ii].Equals("(") || rpn[ii].Equals(")"))
                        {
                            // Skip this as RPN should not have parens
                        }
                        else
                            sb.Append(AppClass.expParam + rpn[ii]);
                    }

                    if (ReturnAsIs == false)
                    {
                        // Return as an RPN string
                        exprResult = AppClass.expByte + sb.ToString() + AppClass.expEnd;
                    }
                    else
                    {
                        // Return as-is as a literal expression string
                        exprResult = AppClass.literalStart + exprResult + AppClass.literalEnd;
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return cmd;
        }


        /// <summary>
        /// Find the next command/variable starting at the beginning of the string and ending with a 
        /// space, comma, or other punctuation.  Include quoted material.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="tokens"></param>
        /// <param name="cmdToken"></param>
        /// <returns>OUT: string of command found.  RETURNS: remainder of command string.</returns>
        public string GetNextToken(string command, string delimiterTokens, out string cmdToken)
        {
            cmdToken = string.Empty;
            string cmdLine = command.Trim();    // clean up leading and trailing spaces
            string cmd = string.Empty;
            char inQuote = '\0';
            char quoteType = '\0';

            int quoteCount = 0;
            int i = 0;
            delimiterTokens = delimiterTokens.Length == 0 ? " " : delimiterTokens;

            while (cmdLine.Length > 0)
            {
                // Have we found a delimiter token?
                if (delimiterTokens.Contains(cmdLine[i]))
                {
                    // If we're not in a quoted section
                    if (inQuote == '\0')
                    {
                        cmdToken = cmdLine[..i].Trim();
                        cmd = cmdLine[i..].Trim();
                        break;
                    }
                }

                // Are we in a quoted section?
                if (inQuote == '\0')
                {
                    // No, so are we on a quoted sections start character?
                    if ("(['\"".Contains(cmdLine[i]))
                    {
                        quoteType = cmdLine[i];
                        inQuote = cmdLine[i].Equals('(') ? ')' : cmdLine[i].Equals('[') ? ']' : cmdLine[i];
                        quoteCount++;
                    }
                }
                else
                {
                    // Need to be able to handle nested parens & brakets
                    if (cmdLine[i] == quoteType) quoteCount++;

                    if (cmdLine[i] == inQuote)
                    {
                        inQuote = '\0';
                        quoteCount--;
                    }
                }

                // Move to the next character
                i++;
                if (i >= cmdLine.Length)
                {
                    // We're done
                    cmdToken = cmdLine;
                    cmd = string.Empty;
                    break;
                }
            }

            return cmd;
        }

        /* -----------------------------------------------------------------------------------------*
         * No computation takng place beyond putting tokens into
         * the correct order and returning the string
         * 
         * -----------------------------------------------------------------------------------------*/
        /// <summary>
        /// Compile an expression into an RPN string where each token is separated by AppClass.expParam
        /// </summary>
        /// <param name="app"></param>
        /// <param name="cmdRest"></param>
        /// <returns>RPN expression string</returns>
        public string GetRPNString(AppClass app, string cmdRest)
        {
            List<string> result = [];
            StringBuilder sb = new();
            sb.Append(AppClass.expByte);

            try
            {
                JAXMath jaxMath = new();
                result = jaxMath.ReturnRPN(cmdRest);
                for (int i = 0; i < result.Count; i++)
                    sb.Append(result[i] + AppClass.expParam);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return sb.ToString().TrimEnd(AppClass.expParam) + AppClass.expEnd;
        }


        /* -----------------------------------------------------------------------------------------*
         * TODO
         * Break the statement up using the allowed string as a guide to the order the keywords
         * must appear with a list of what must be filled in
         * -----------------------------------------------------------------------------------------*/
        public string StrictBreak(string cmdRest, string ParseInfo, string[] flags, string mustFill)
        {
            string result = string.Empty;
            string[] ParsingParts = ParseInfo.Split(',');

            int cmdRestLen = cmdRest.Length;

            // Create the codes dictionary
            Dictionary<string, string> codes = [];
            for (int i = 0; i < JAXLanguageLists.JAXCompilerDictionary.Length; i++)
                codes.Add(CodeDictionary[i], string.Empty);

            // Split the parsing information
            string[] codeOrder = ParseInfo.Split(',');
            int[] codePos = new int[codeOrder.Length];

            for (int i = 0; i < codeOrder.Length; i++)
                codePos[i] = -1;

            // get the position of each code keyword
            for (int i = 0; i < codeOrder.Length; i++)
            {
                string nextcode = codeOrder[i][..2].ToUpper();
                int f = -1;

                if (nextcode.Equals("00"))
                {
                    // skip this one
                }
                else if (codeOrder[i].Equals("FR1"))
                {
                    // Special case for FOR XX = FR1 to 
                    f = JAXLib.FindKeyword("=", cmdRest, 0);
                    codePos[i] = f;
                }
                else if (JAXLib.InList(nextcode, "XX", "FW"))
                {
                    // Special handling - always the first thing in
                    // the statement if it exists at all
                    codePos[i] = 0;
                }
                else
                {
                    string kw = CompilerCodes[nextcode];
                    f = JAXLib.FindKeyword(kw, cmdRest, 3);

                    if (f >= 0)
                    {
                        if (codePos[i] < 0)
                            codePos[i] = f;
                        else
                            throw new Exception($"10||keyword {kw} found twice");
                    }
                }
            }

            // Now go through the keyword list and break things out
            int lastPos = -1;
            for (int i = 0; i < codeOrder.Length; i++)
            {
                string code = codeOrder[i][..2].ToUpper();
                int getInfo = -1;
                string expr = string.Empty;

                if (codePos[i] >= 0)
                {
                    if (codePos[i] >= lastPos)
                    {
                        for (int j = i + 1; j < codeOrder.Length; j++)
                        {
                            if (codePos[j] > 0)
                            {
                                lastPos = lastPos < 0 ? 0 : lastPos;
                                getInfo = codePos[j] - lastPos;
                                lastPos = codePos[j];
                                break;
                            }
                        }

                        // Handle if last keyword in statement
                        if (getInfo < 0)
                        {
                            getInfo = cmdRest.Length;
                            lastPos = cmdRestLen;
                        }
                    }
                    else
                        throw new Exception($"10||Out of order {codeOrder[i]}");
                }


                if (getInfo >= 0)
                {
                    expr = cmdRest[..getInfo];
                    cmdRest = cmdRest[getInfo..];

                    // process!
                    Dictionary<string, string> retcodes = StatementBreak(expr, codeOrder[i], flags);

                    foreach (KeyValuePair<string, string> pair in retcodes)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Value) == false)
                        {
                            if (codes[pair.Key].Length == 0)
                                codes[pair.Key] = pair.Value;
                            else
                                throw new Exception($"10||can't reassign code {pair.Key}");
                        }
                    }
                }
            }

            // Now make sure the required pieces are filled in
            foreach (string pCode in ParsingParts)
            {
                string p = pCode[..2];
                if (p.Equals("FW", StringComparison.OrdinalIgnoreCase))
                {
                    // This is always required
                    if (string.IsNullOrWhiteSpace(codes[CompilerCodes["FV"]]) || string.IsNullOrWhiteSpace(codes[CompilerCodes["WT"]]))
                        throw new Exception("|");
                    else
                        result += CompilerXRef["FV"].ToString() + codes[CompilerCodes["FV"]] + AppClass.stmtDelimiter + CompilerXRef["WT"].ToString() + codes[CompilerCodes["WT"]] + AppClass.stmtDelimiter;
                }
                else
                {
                    if (codes[CompilerCodes[p]].Length > 0)
                        result += CompilerXRef[p].ToString() + codes[CompilerCodes[p]] + AppClass.stmtDelimiter;
                }
            }

            result = result.TrimEnd(AppClass.stmtDelimiter);
            return result;
        }


        /* -----------------------------------------------------------------------------------------*
         * Break the statement up using the allowed string as a guide to the keywords that are allowed
         * to be in the command.  The flagsSent array represents the flags that are allowed to
         * show up.
         * 
         * CmdRest is the rest of the command to parse (the main command has already been stripped.
         *                                                                  
         * -----------------------------------------------------------------------------------------*/
        public Dictionary<string, string> StatementBreak(string cmdRest, string allowed, string[] flagsSent)
        {
            string HoldMe = cmdRest;

            Dictionary<string, string> code = [];
            for (int i = 0; i < JAXLanguageLists.JAXCompilerDictionary.Length; i++)
                code.Add(CodeDictionary[i], string.Empty);

            List<string> Flags = [];
            List<string> FlagChecks = [];

            for (int i = 0; i < flagsSent.Length; i++)
            {
                if (flagsSent[i].Contains('|'))
                {
                    FlagChecks.Add("|" + flagsSent[i].ToLower() + "|");

                    // Fix for allowed abriviations
                    if (FlagChecks[^1].Contains("descending", StringComparison.OrdinalIgnoreCase))
                        FlagChecks[^1] += "asc|des|";

                    string[] fsent = flagsSent[i].Split('|');
                    for (int j = 0; j < fsent.Length; j++)
                    {
                        Flags.Add(fsent[j].ToLower());

                        // Add in any allowed abriviations
                        if (JAXLib.InListC(fsent[i], "ascending", "descending", "exclusive", "shared", "noupdate",
                        "noappend", "nomodify", "nodelete", "unique", "candidate", "nooptimioze", "validate", "additive", "again"))
                            FlagChecks.Add(flagsSent[i][..3].ToLower());
                    }
                }
                else
                    Flags.Add(flagsSent[i].ToLower());
            }

            string cmdOut = string.Empty;
            code["flags"] = AppClass.expDelimiter.ToString();

            try
            {
                while (cmdRest.Length > 0)
                {
                    string s;

                    if (allowed.Equals("CM0"))
                    {
                        // ON x Command Exception
                        string cmd = Program.CurrentApp.JaxCompiler.CompileLine(cmdRest, false);
                        code["command"] = cmd.TrimStart(AppClass.cmdByte).TrimEnd(AppClass.cmdEnd);
                        cmdRest = string.Empty;
                        continue;
                    }
                    else if (allowed.StartsWith("XX!,"))
                    {
                        // For Statement Exception
                        if (code["expressions"].Length == 0)
                        {
                            // Start of for expression, get up to the =
                            cmdRest = GetNextToken(cmdRest, "=", out s);
                        }
                        else if (code["for"].Length == 0)
                        {
                            // Get the starting increment
                            if (cmdRest[0] == '=')
                            {
                                cmdRest = cmdRest[1..].Trim();
                                s = "=";
                            }
                            else
                                throw new Exception("10||Improper FOR statement structure");
                        }
                        else
                        {
                            // TO and STEP process is done below
                            cmdRest = GetNextToken(cmdRest, string.Empty, out s);
                        }
                    }
                    else
                    {
                        // Get next token to decide what to do with it
                        cmdRest = GetNextToken(cmdRest, string.Empty, out s);
                    }

                    // ------------------------------------------------------------------------------
                    // Process the next token by stepping through this massive
                    // IF/ELSE IF statement that holds all of the rest of the
                    // language's key words
                    // ------------------------------------------------------------------------------
                    if (s[0] == '(' && allowed.Contains("TB0") && code["expressions"].Length > 0)
                    {
                        if (code["table"].Length > 0) throw new Exception("10||Cannot redefine table definition");
                        cmdRest = StatementBreak_TableFields(allowed, Flags, s + " " + cmdRest, out cmdOut);
                        code["table"] = cmdOut;
                    }
                    else if (s.Equals("datasession", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["session"].Length > 0) throw new Exception("10||Cannot redefine DATASESSION definition");
                        if (allowed.Contains("SS") == false) throw new Exception("10||Unexpected DATASESSION clause");
                    }
                    else if (s.Equals("alias", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["alias"].Length > 0) throw new Exception("10||Cannot redefine ALIAS definition");
                        if (allowed.Contains("AL") == false) throw new Exception("10||Unexpected ALIAS clause");
                    }
                    else if (s.Equals("index", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["index"].Length > 0) throw new Exception("10||Cannot redefine INDEX definition");
                        if (allowed.Contains("IX") == false) throw new Exception("10||Unexpected INDEX clause");
                    }
                    else if (s.Equals("at", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["at"].Length > 0) throw new Exception("10||Cannot redefine AT definition");
                        if (allowed.Contains("AT") == false) throw new Exception("10||Unexpected AT clause");
                        cmdRest = GetNextExpression(cmdRest, ",", out cmdOut);
                        if (cmdRest.Length > 0 && cmdRest[0] == ',')
                        {
                            cmdRest = cmdRest[1..].Trim();
                            code["at"] = cmdOut + AppClass.expParam;
                            cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                            code["at"] += cmdOut;
                        }
                        else
                            throw new Exception("10||Invalid AT expression");
                    }
                    else if (s.Equals("size", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["size"].Length > 0) throw new Exception("10||Cannot redefine SIZE definition");
                        if (allowed.Contains("SI") == false) throw new Exception("10||Unexpected SIZE clause");
                        cmdRest = GetNextExpression(cmdRest, ",", out cmdOut);
                        if (cmdRest.Length > 0 && cmdRest[0] == ',')
                        {
                            cmdRest = cmdRest[1..].Trim();
                            code["size"] = cmdOut + AppClass.expParam;
                            cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                            code["size"] += cmdOut;
                        }
                        else
                            throw new Exception("10||Invalid SIZE expression");
                    }
                    else if (s.Equals("for", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // FOR lExpression
                        // ---------------------------------------------
                        if (code["for"].Length > 0) throw new Exception("10||Cannot redefine FOR clause");
                        if (allowed.Contains("FR") == false) throw new Exception("10||Unexpected FOR keyword");
                        cmdRest = StatementBreak_For(allowed, Flags, cmdRest, out cmdOut);
                        code["for"] = cmdOut;
                    }
                    else if (s.Equals("as", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // AS 
                        // ---------------------------------------------
                        if (code["as"].Length > 0) throw new Exception("10||Cannot redefine AS clause");
                        if (allowed.Contains("AS") == false) throw new Exception("10||Unexpected AS keyword");
                        cmdRest = StatementBreak_As(allowed, Flags, cmdRest, out cmdOut);
                        code["as"] = cmdOut;
                    }
                    else if (s.Equals("codepage", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // CODEPAGE
                        // ---------------------------------------------
                        if (allowed.Contains("CP") == false) throw new Exception("10||Unexpected CODEPAGE keyword");
                        if (code["codepage"].Length > 0) throw new Exception("10||Cannot redfine CODEPAGE clause");

                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["codepage"] = cmdOut;
                    }
                    else if (s.Equals("collate", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // COLLATE
                        // ---------------------------------------------
                        if (allowed.Contains("CO") == false) throw new Exception("10||Unexpected COLLATE keyword");
                        if (code["collote"].Length > 0) throw new Exception("10||Cannot redfine COLLATE clause");

                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["collate"] = cmdOut;
                    }
                    else if (s.Equals("field", StringComparison.OrdinalIgnoreCase))
                    {
                        cmdRest = StatementBreak_Field(allowed, [], cmdRest, out cmdOut);
                        code["fields"] = "V" + AppClass.expDelimiter + cmdOut;
                    }
                    else if (s.Equals("like", StringComparison.OrdinalIgnoreCase))
                    {
                        if (allowed.Contains("LK") == false) throw new Exception("10|");
                        if (code["like"].Length > 0) throw new Exception("10||You cannot redefine the LIKE list");

                        if (allowed.Contains("LK2"))
                        {
                            // Get the comma delimited list of like names
                            cmdRest = StatementBreak_VarList(allowed, Flags, cmdRest, out cmdOut);
                            code["like"] = cmdOut;
                        }
                    }
                    else if (s.Equals("fields", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // Filed varName / varNameList
                        // LL = Field
                        // L0 = Field List
                        // ---------------------------------------------
                        if (allowed.Contains("FV") == false) throw new Exception("10|");
                        if (code["fields"].Length > 0) throw new Exception("10||You cannot redefine the field list");

                        if (cmdRest.Length > 5 && cmdRest[..5].Equals("like ", StringComparison.OrdinalIgnoreCase))
                        {
                            if (allowed.Contains("FV3"))
                            {
                                cmdRest = cmdRest[5..].Trim();
                                code["fields"] = "L" + AppClass.expDelimiter + cmdRest;
                            }
                            else
                                throw new Exception("10}");
                        }
                        else if (cmdRest.Length > 6 && cmdRest[..6].Equals("except", StringComparison.OrdinalIgnoreCase))
                        {
                            if (allowed.Contains("FV3"))
                            {
                                cmdRest = cmdRest[7..].Trim();
                                code["fields"] = "X" + AppClass.expDelimiter + cmdRest;
                            }
                            else
                                throw new Exception("10}");
                        }
                        if (cmdRest[0] == '(' && allowed.Contains("FV2"))
                        {
                            cmdRest = GetNextToken(cmdRest, ")", out cmdOut);
                            cmdOut = cmdOut[1..];
                            cmdOut = cmdOut[..^1];
                            cmdOut = cmdOut.Trim();
                            StatementBreak_FieldList(allowed, [], cmdRest, out cmdOut);
                            code["fields"] = "L" + AppClass.expDelimiter + cmdRest;
                        }

                    }
                    else if (s[0] == '=' && allowed.Contains("FR1"))
                    {
                        if (code["for"].Length > 0) throw new Exception("10||Cannot redefine FOR clause");

                        // If the expression got attached to the =
                        if (s.Length > 0)
                            cmdRest = s[1..] + " " + cmdRest;

                        cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                        code["for"] = cmdOut;
                    }
                    else if (s.Equals("step", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["step"].Length > 0) throw new Exception("10||You cannot redfine STEP clause");
                        if (allowed.Contains("ST") == false) throw new Exception("10||Unexpected STEP clause");
                        cmdRest = StatementBreak_ExpressionList(allowed, [], cmdRest, out cmdOut);
                        code["step"] = cmdOut;
                    }
                    else if (s.Equals("from", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // FROM
                        // ---------------------------------------------
                        if (code["from"].Length > 0) throw new Exception("10||Cannot redefine FROM clause");
                        if (allowed.Contains("FM") == false) throw new Exception("10||Unexpected FROM keyword");
                        cmdRest = StatementBreak_From(allowed, Flags, cmdRest, out cmdOut);
                        code["from"] = cmdOut;
                    }
                    else if (s.Equals("in", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // IN workarea | alias
                        // ---------------------------------------------
                        if (allowed.Contains("IN") == false) throw new Exception("10||Unexpected IN keyword");
                        if (code["in"].Length > 0) throw new Exception("10||Cannot redfine IN clause");

                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["in"] = cmdOut;
                    }
                    else if (s.Equals("session", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // SESSION nSession
                        // ---------------------------------------------
                        if (allowed.Contains("SS") == false) throw new Exception("10||Unexpected SESSION keyword");
                        if (code["session"].Length > 0) throw new Exception("10||Cannot redfine SESSION clause");
                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["session"] = cmdOut;
                    }
                    else if (s.Equals("into", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // INTO workarea | alias
                        // ---------------------------------------------
                        if (allowed.Contains("IT") == false) throw new Exception("10||Unexpected INTO keyword");
                        if (code["into"].Length > 0) throw new Exception("10||Cannot redfine INTO clause");

                        cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["into"] = cmdOut;
                    }
                    else if (s.Equals("message", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // MESSAGE cExpr
                        // ---------------------------------------------
                        if (allowed.Contains("MS") == false) throw new Exception("10||Unexpected MESSAGE keyword");
                        if (code["message"].Length > 0) throw new Exception("10||Cannot redfine MESSAGE clause");

                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["message"] = cmdOut;
                    }
                    else if (s.Equals("of", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // OF 
                        // ---------------------------------------------
                        if (allowed.Contains("OF") == false) throw new Exception("10||Unexpected OF keyword");
                        if (code["of"].Length > 0) throw new Exception("10||Cannot redfine OF clause");

                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["of"] = cmdOut;
                    }
                    else if (s.Equals("on", StringComparison.OrdinalIgnoreCase) && allowed.Contains("on", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // ON expression
                        // ---------------------------------------------
                        if (allowed.Contains("ON") == false) throw new Exception("10||Unexpected ON keyword");
                        if (code["on"].Length > 0) throw new Exception("10||Cannot redfine IN clause");

                        if (allowed.Contains("ON*"))
                        {
                            // THIS HAS TO BE CALLED BY STRICTBREAK()
                            // as we're potentially returning an infix formula
                            cmdOut = AppClass.literalStart + cmdRest + AppClass.literalEnd;
                            cmdRest = string.Empty;
                        }
                        else
                            cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out cmdOut);

                        code["on"] = cmdOut;
                    }
                    else if (JAXLib.InListC(s, "all", "rest", "next", "top", "record") && allowed.Contains("SC"))
                    {
                        // ---------------------------------------------
                        // Scope commands
                        // ---------------------------------------------
                        if (allowed.Contains("SC") == false) throw new Exception("10||Unexpected SCOPE keyword");
                        if (code["scope"].Length > 0) throw new Exception("10||Cannot redefine scope");
                        cmdRest = StatementBreak_Scope(allowed, Flags, s, cmdRest, out cmdOut);
                        code["scope"] = cmdOut;
                    }
                    else if (s.Equals("while", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // WHILE lExpression
                        // ---------------------------------------------
                        if (allowed.Contains("WL") == false) throw new Exception("10||Unexpected WHILE");
                        if (code["while"].Length > 0) throw new Exception("10||Cannot redefine while");

                        cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                        code["while"] = cmdOut;
                    }
                    else if (s.Equals("when", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // WHEN lExpression
                        // ---------------------------------------------
                        if (allowed.Contains("WH") == false) throw new Exception("10||Unexpected WHEN");
                        if (code["when"].Length > 0) throw new Exception("10||Cannot redefine when");

                        cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                        code["when"] = cmdOut;
                    }
                    else if (s.Equals("with", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // WITH 
                        // ---------------------------------------------
                        if (allowed.Contains("WT") == false) throw new Exception("10||Unexpected WHILE");
                        if (code["with"].Length > 0) throw new Exception("10||Cannot redefine while");

                        if (allowed.Contains("WT0"))
                        {
                            while (cmdRest.Length > 0)
                            {
                                // Expecting Property=Value,Property...
                                cmdRest = GetNextToken(cmdRest, ",", out cmdOut);
                                code["with"] += "P" + AppClass.expDelimiter + cmdOut + AppClass.stmtDelimiter;

                                if (cmdRest.Length == 0 || cmdRest[0] != ',')
                                    break;

                                cmdRest = cmdRest[1..].Trim();
                            }
                        }
                        if (allowed.Contains("WT1"))
                        {
                            // Set WITH
                            throw new Exception("WT1 not supported");

                        }
                        if (allowed.Contains("WT3"))
                        {
                            string cCmd = string.Empty;

                            // Expression list after WITH
                            cmdRest = GetNextExpression(cmdRest, ",", out string exp);
                            cmdOut = "X" + AppClass.expDelimiter + exp + AppClass.expDelimiter;
                            cmdRest = cmdRest.Trim();

                            // We are looking for an expression list
                            while (cmdRest.Length > 0 && cmdRest[..1].Equals(","))
                            {
                                cmdRest = cmdRest[1..].Trim();  // strip the comma
                                cmdRest = GetNextExpression(cmdRest, ",", out exp);
                                cmdOut += "X" + AppClass.expDelimiter + exp + AppClass.expDelimiter;
                            }
                        }
                        else
                        {
                            // Expecting varName (SET - WT2)
                            cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);
                            code["with"] = "X" + AppClass.expDelimiter + cmdOut + AppClass.stmtDelimiter;
                        }

                        code["with"] = cmdOut;
                    }
                    else if (s.Equals("order", StringComparison.OrdinalIgnoreCase) || (s.Equals("tag", StringComparison.OrdinalIgnoreCase) && allowed.Contains("OR0")))
                    {
                        // ---------------------------------------------
                        // ORDER/TAG
                        // ---------------------------------------------
                        if (allowed.Contains("OR") == false) throw new Exception("10|");
                        if (code["order"].Length > 0) throw new Exception("10||Cannot redefine ORDER clause");
                        cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                        code["order"] = (s.Equals("tag", StringComparison.OrdinalIgnoreCase) ? "G" : "O") + cmdOut;
                    }
                    else if (s.Equals("to", StringComparison.OrdinalIgnoreCase) || s.Equals("tag", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // TO/TAG
                        // ---------------------------------------------
                        if (allowed.Contains("TO") == false && allowed.Contains("TG") == false) throw new Exception("10|");
                        if (code["to"].Length > 0) throw new Exception("10||Cannot redefine TO list");

                        cmdRest = StatementBreak_To(allowed, Flags, cmdRest, out cmdOut);

                        if (s.Equals("to", StringComparison.OrdinalIgnoreCase))
                            code["to"] = cmdOut;
                        else
                            code["tag"] = cmdOut;
                    }
                    else if (s.Equals("type", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // TYPE
                        // ---------------------------------------------
                        cmdOut = string.Empty;

                        if (code["type"].Length > 0) throw new Exception("10||Cannot redefine TYPE clause");
                        if (allowed.Contains("TY") == false) throw new Exception("10||Unexpected TYPE keyword");

                        cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);

                        if (allowed.Contains("TY0") && JAXLib.InListC(cmdOut, "XLS", "XLSX", "CALC") == false) throw new Exception("10||Unkown TYPE " + cmdOut);

                        if (cmdOut.Length == 0) throw new Exception("10||Unknown TYPE code");
                        code["type"] = cmdOut;
                    }
                    else if (s.Equals("values", StringComparison.OrdinalIgnoreCase))
                    {
                        // ---------------------------------------------
                        // Values
                        // ---------------------------------------------
                        cmdOut = string.Empty;

                        if (code["values"].Length > 0) throw new Exception("10||Cannot redefine VALUES clause");
                        if (allowed.Contains("VL") == false) throw new Exception("10||Unexpected VALUES keyword");

                        cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);

                        if (cmdOut[0] == '(' && cmdOut[^1] == ')')
                        {
                            cmdOut = cmdOut[1..];
                            cmdOut = cmdOut[..^1];
                            string cmdLeft = StatementBreak_ExpressionList(allowed, Flags, cmdOut, out cmdOut);
                            if (cmdLeft.Length > 0) throw new Exception("10|");

                            code["values"] = cmdOut;
                        }
                    }
                    else if (s.Equals("timeout", StringComparison.OrdinalIgnoreCase))
                    {
                        if (code["timeout"].Length > 0) throw new Exception("10||Cannot redefine TIMEOUT clause");
                        if (allowed.Contains("TI") == false) throw new Exception("10||Unexpected TIMEOUT keyword");
                        cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
                        code["timeout"] = cmdOut;
                    }
                    else if (Flags.Contains(s.ToLower()))
                    {
                        // ---------------------------------------------
                        // Flag handling
                        // ---------------------------------------------
                        s = s.ToLower();

                        string saveAs = JAXLib.InListC(s, "descending", "ascending", "ascending", "descending",
                                "exclusive", "shared", "noupdate", "unique", "candidate", "nooptimioze", "validate",
                                "additive", "again") ? s[..3] : s;

                        // Did we repeat this flag?
                        if (code["flags"].Contains(AppClass.expDelimiter.ToString() + saveAs + AppClass.expDelimiter.ToString(), StringComparison.OrdinalIgnoreCase))
                            throw new Exception("10||Repeating flag " + s);

                        // Make sure this new flag does not appear with a 
                        // flag that is in conflict with one already set
                        for (int i = 0; i < FlagChecks.Count; i++)
                        {
                            if (FlagChecks.Contains("|" + s + "|"))
                            {
                                string[] fcheck = FlagChecks[i].Trim('|').Split('|');
                                for (int j = 0; j < fcheck.Length; j++)
                                {
                                    if (code["flags"].Contains(AppClass.expDelimiter.ToString() + fcheck[j] + AppClass.expDelimiter.ToString()))
                                        throw new Exception(string.Format("10||Cannot set flag {0} with {1}", s, fcheck[j]));
                                }
                                break;
                            }
                        }

                        // Everything checks, add it in
                        code["flags"] += saveAs + AppClass.expDelimiter;
                    }
                    else if (allowed.Contains("FW0"))
                    {
                        // ---------------------------------------------
                        // Field WITH expression [, ...]
                        // Returns Fields & With code blocks
                        // ---------------------------------------------
                        if (code["fields"].Length > 0) throw new Exception("10||Cannot redefine fields");
                        if (code["with"].Length > 0) throw new Exception("10||Cannot redefine with clause");

                        // Put it back together
                        cmdRest = s + " " + cmdRest;

                        while (string.IsNullOrWhiteSpace(cmdRest) == false)
                        {
                            // Get the field literal/expression
                            bool paren = cmdRest[0] == '(';
                            cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out string field);

                            // Is there a WITH?
                            if (cmdRest.Length > 5 && cmdRest[..5].Equals("with ", StringComparison.OrdinalIgnoreCase))
                            {
                                // Eat the WITH
                                cmdRest = cmdRest[5..].Trim();

                                // Get the value expression
                                cmdRest = GetNextExpression(cmdRest, ",", out string expr);

                                // Make sure we've got values for both
                                if (string.IsNullOrWhiteSpace(expr) || string.IsNullOrWhiteSpace(field)) throw new Exception("10|");

                                // Include them into the correct code elements
                                if (paren || field[0] == AppClass.literalStart)
                                {
                                    code["fields"] += (paren ? "X" : "L") + AppClass.expDelimiter + field + AppClass.expDelimiter;
                                    code["with"] += "X" + AppClass.expDelimiter + expr + AppClass.expDelimiter;
                                }
                                else
                                    throw new Exception("10|");
                            }
                            else
                                throw new Exception("10|");

                            // If no comma left then get out
                            if (string.IsNullOrWhiteSpace(cmdRest) || cmdRest[0] != ',')
                            {
                                cmdRest = cmdRest.Trim();
                                break;
                            }

                            // Trim off leading commas and spaces
                            while (cmdRest.Length > 0 && " ,".Contains(cmdRest[0]))
                            {
                                cmdRest = cmdRest.TrimStart(',');
                                cmdRest = cmdRest.TrimStart();
                            }
                        }
                    }
                    else if (allowed.Contains("XF"))
                    {
                        cmdRest = (s + " " + cmdRest).Trim();

                        if (allowed.Contains("XF0"))
                        {
                            // FilePath literal or expression
                            cmdRest = GetFileLiteralOrExpression(cmdRest, ", ", out cmdOut);

                            code["fileexpr"] = cmdOut;
                        }
                        else
                            throw new Exception("1999||XF issue in{allowed}");
                    }
                    else if (allowed.Contains("XX"))
                    {
                        // ---------------------------------------------
                        // Expression or  Expression List
                        // ---------------------------------------------
                        if (code["expressions"].Length > 0) throw new Exception("10||Cannot redefine expression");

                        cmdRest = (s + " " + cmdRest).Trim();

                        if (allowed.Contains("XX$"))
                        {
                            cmdRest = GetNextExpression(cmdRest, null, out cmdOut);
                        }
                        else if (allowed.Contains("XX*"))
                        {
                            // Get an expression
                            cmdRest = GetNextExpression(cmdRest, ", ", out cmdOut);
                        }
                        else if (allowed.Contains("XX:"))
                        {
                            // Directory literal or expression
                            cmdRest = GetDirectoryLiteralOrExpression(cmdRest, ", ", out cmdOut);
                        }
                        else if (allowed.Contains("XX0"))
                        {
                            // Name literal or expression
                            cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out cmdOut);
                        }
                        else
                        {
                            if (allowed.Contains("XX!"))
                            {
                                // FOR Expression
                                cmdRest = GetNextToken(cmdRest, "=", out cmdOut);
                                cmdOut = AppClass.literalStart + cmdOut + AppClass.literalEnd;
                            }
                            else if (allowed.Contains("XX#"))
                            {
                                if (cmdRest[0] == '(')
                                {
                                    // Pull out (varlist) section
                                    cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);

                                    if (cmdOut[0] == '(' && cmdOut[^1] == ')')
                                    {
                                        cmdOut = cmdOut[1..];
                                        cmdOut = cmdOut[..^1];
                                        string cmdLeft = StatementBreak_VarList(allowed, Flags, cmdOut, out cmdOut);
                                        if (cmdLeft.Length > 0) throw new Exception("10|");
                                    }
                                }
                                else
                                    throw new Exception("10|"); // Expecting a (expression list)
                            }
                            else if (allowed.Contains("XX4"))
                            {
                                if (cmdRest[0] == '(')
                                {
                                    // Pull out () section
                                    cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);

                                    if (cmdOut[0] == '(' && cmdOut[^1] == ')')
                                    {
                                        cmdOut = cmdOut[1..];
                                        cmdOut = cmdOut[..^1];
                                        string cmdLeft = StatementBreak_ExpressionList(allowed, Flags, cmdOut, out cmdOut);
                                        if (cmdLeft.Length > 0) throw new Exception("10|");
                                    }
                                }
                                else
                                    throw new Exception("10|"); // Expecting a (expression list)
                            }
                            else if (allowed.Contains("XX5"))
                            {
                                // Get the variable array list with AS option
                                cmdRest = StatementBreak_ArrayList(allowed, Flags, cmdRest, out cmdOut, out string asType);
                                if (cmdRest.Length > 0) throw new Exception("10|");
                                if (code["as"].Length > 0) throw new Exception("10||Cannot redefine AS clause");
                                code["as"] = asType;
                            }
                            else if (allowed.Contains("XX6"))
                            {
                                // Get the variable list
                                cmdRest = StatementBreak_VarList(allowed, Flags, cmdRest, out cmdOut);
                                if (cmdRest.Length > 0) throw new Exception("10|");
                            }
                            else if (allowed.Contains("XX2"))
                            {
                                // Get TOP, BOTTOM, or an expression
                                if (cmdRest.Equals("top", StringComparison.OrdinalIgnoreCase))
                                {
                                    cmdOut = AppClass.literalStart + "TOP" + AppClass.literalEnd;
                                    cmdRest = string.Empty;
                                }
                                else if ("bottom".StartsWith(cmdRest, StringComparison.OrdinalIgnoreCase))
                                {
                                    cmdOut = AppClass.literalStart + "BOTTOM" + AppClass.literalEnd;
                                    cmdRest = string.Empty;
                                }
                                else
                                    cmdRest = GetNextExpression(cmdRest, ", ", out cmdOut);
                            }
                            else if (allowed.Contains("XX3") || allowed.Contains("XX1"))
                            {
                                // Expecting an expression list
                                cmdRest = StatementBreak_ExpressionList(allowed, Flags, cmdRest, out cmdOut);
                            }
                            else if (allowed.Contains("XX7"))
                            {
                                // Variable literal only
                                cmdRest = GetNextToken(cmdRest, string.Empty, out cmdOut);
                                cmdOut = AppClass.literalStart + cmdOut + AppClass.literalEnd;
                            }
                            else if (allowed.Contains("XX8"))
                            {
                                // Expecting a variable list for PARAMETERS, LPARAMETERS, PUBLIC, PRIVATE, and LOCAL
                                cmdRest = StatementBreak_VarList8(allowed, Flags, cmdRest, out cmdOut, out string cmdAs);
                                if (code["as"].Length > 0) throw new Exception("10||Cannot redefine AS clause");
                                code["as"] = cmdAs;
                            }
                            else if (allowed.Contains("XX9"))
                            {
                                // Literal to end of line
                                cmdOut = AppClass.literalStart + cmdRest.Trim() + AppClass.literalEnd;
                                cmdRest = string.Empty;
                            }
                        }


                        if (cmdOut.Length < 1)
                            throw new Exception("10||Invalid or missing expression");
                        else
                            code["expressions"] = cmdOut;
                    }
                    else
                        throw new Exception("10||Unexpected token in command: " + s);
                }

                // Clean up the flags
                code["flags"] = code["flags"].Trim(AppClass.expDelimiter);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return code;
        }




        /* -----------------------------------------------------------------------------------------*
         * TO
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_To(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            // break out one or more variables to where you are storing the results
            string cmdPrefix = string.Empty;

            if (allowed.Contains("T02"))
            {
                GetNextToken(cmdRest, ", ", out cmdOut);
                if (cmdOut.ToLower().Equals("array"))
                {
                    cmdRest = GetNextToken(cmdRest, ", ", out _);    // Eat the ARRAY token
                    cmdPrefix = "A";
                }
                else
                {
                    cmdPrefix = "V";
                }

                cmdRest = GetNextToken(cmdRest, ", ", out cmdOut);  // TO2 VarLiteral
                cmdOut = cmdPrefix + AppClass.expDelimiter + cmdOut;
            }
            else if (allowed.Contains("TO1"))
                cmdRest = StatementBreak_VarList(allowed, flags, cmdRest, out cmdOut);
            else if (allowed.Contains("TG3") || allowed.Contains("TO3") || allowed.Contains("TO0"))
                cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out cmdOut);
            else if (allowed.Contains("TO4"))
            {
                cmdRest = GetNextExpression(cmdRest, ", ", out cmdOut);
            }
            else if (allowed.Contains("TO5"))
            {
                // TO Printer | TO File filename
                cmdRest = cmdPrefix + GetNextToken(cmdRest, ", ", out cmdPrefix);
                if (cmdPrefix.Equals("printer", StringComparison.OrdinalIgnoreCase))
                    cmdOut = "P" + AppClass.expDelimiter;
                else if (cmdPrefix.Equals("file", StringComparison.OrdinalIgnoreCase))
                {
                    cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                    cmdOut = "F" + AppClass.expDelimiter + cmdOut;
                }
                else
                    throw new Exception("10|");
            }
            else if (allowed.Contains("TO6"))
            {
                GetNextToken(cmdRest, string.Empty, out string type);

                if (JAXLib.InListC(type, "array", "name", "json"))
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out type);
                    cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                    cmdOut = type[0].ToString().ToUpper() + AppClass.expDelimiter + cmdOut;
                }
                else if (type.Equals("memvar", StringComparison.OrdinalIgnoreCase))
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out type);
                    cmdOut = "M";
                }
                else
                    throw new Exception("10||Unexpected FROM " + type);
            }
            else if (allowed.Contains("TO8"))
            {
                cmdRest = GetNextToken(cmdRest, string.Empty, out string type);
                string lit = string.Empty;

                if (JAXLib.InListC(type, "file", "debug", "var"))
                {
                    cmdOut = string.Empty;
                    if (string.IsNullOrWhiteSpace(cmdRest) == false)
                        cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                    else if (type.Equals("debug", StringComparison.OrdinalIgnoreCase) == false)
                        throw new Exception($"10||Missing TO {type.ToUpper()} literal/expression");

                    cmdOut = type.ToString().ToUpper() + AppClass.expDelimiter + cmdOut;
                }
                else
                    throw new Exception("10||Unexpected TO " + type);
            }
            else if (allowed.Contains("TO#"))
            {
                // Get rest to end of line
                cmdOut = AppClass.literalStart + cmdRest.Trim() + AppClass.literalEnd;
                cmdRest = string.Empty;
            }
            else
                throw new Exception("1999|");

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * FF - FOR
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_For(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            if (allowed.Contains("FR0"))
                cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);         // Get the expression as a RPN string
            else if (allowed.Contains("FR*"))
                cmdRest = GetNextExpression(cmdRest, string.Empty, true, out cmdOut);   // Get the expression as an infix notation string
            else
                throw new Exception("10|");

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * FM - FROM
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_From(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            if (allowed.Contains("FM0"))
                cmdRest = GetNextExpression(cmdRest, string.Empty, out cmdOut);
            else if (allowed.Contains("FM1"))
            {
                GetNextToken(cmdRest, string.Empty, out string type);

                if (JAXLib.InListC(type, "array", "name", "json"))
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out type);
                    cmdRest = GetNameLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
                    cmdOut = AppClass.literalStart.ToString() + type[0].ToString().ToUpper() + AppClass.literalEnd.ToString() + AppClass.expDelimiter.ToString() + cmdOut;
                }
                else if (type.Equals("memvar", StringComparison.OrdinalIgnoreCase))
                {
                    cmdRest = GetNextToken(cmdRest, string.Empty, out type);
                    cmdOut = AppClass.literalStart + "M" + AppClass.literalEnd;
                }
                else
                    throw new Exception("10||Unexpected FROM " + type);
            }
            else
                throw new Exception("10||Unexpected FROM (FRx) code in: " + allowed);

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * AA - AS
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_As(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            if (allowed.Contains("AS0"))
                cmdRest = GetNextLiteralOrExpression(cmdRest, string.Empty, out cmdOut);
            else throw new Exception("10|");

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * Expresions
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_ExpressionList(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            // EXPRESSION LISTS
            // -----------------------------------
            string result = string.Empty;
            string cCmd = string.Empty;

            if (allowed.Contains("XX1")) cmdRest = StripCMD(cmdRest, out cCmd);  // Calculate command
            cmdRest = GetNextExpression(cmdRest, ",", out string exp);
            cmdOut = (cCmd.Length > 0 ? AppClass.literalStart.ToString() + cCmd + AppClass.literalEnd + AppClass.expDelimiter : string.Empty) + exp;

            // We are looking for an expression list
            while (cmdRest.Length > 0 && cmdRest.Trim()[..1].Equals(","))
            {
                cmdRest = cmdRest[1..].Trim();  // strip the comma

                cCmd = string.Empty;
                if (allowed.Contains("XX1")) cmdRest = StripCMD(cmdRest, out cCmd);  // Calculate command code

                cmdRest = GetNextExpression(cmdRest, ",", out exp);
                cmdOut += AppClass.expDelimiter.ToString() + (cCmd.Length > 0 ? cCmd : string.Empty) + exp;
            }

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * Variable List (simple)  var1, var2, etc
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_VarList(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            string var;
            cmdOut = string.Empty;

            // Get the var name(s)
            if (allowed.Contains("XX@") || allowed.Contains("LK2"))
                cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out cmdOut);
            else
            {
                cmdRest = GetNextToken(cmdRest, ", ", out var);
                if (var.Length == 0) throw new Exception("10||Missing var list");
                cmdOut = AppClass.literalStart + var + AppClass.literalEnd;
            }

            // It may multiple expressions delimited with commas
            while (cmdRest.Length > 0 && cmdRest[0] == ',')
            {
                cmdRest = cmdRest[1..].Trim();

                // Get the var name(s)
                if (allowed.Contains("XX@") || allowed.Contains("LK2"))
                    cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out var);
                else
                {
                    cmdRest = GetNextToken(cmdRest, ", ", out var);
                    var = AppClass.literalStart + var + AppClass.literalEnd;
                    if (var.Length == 0) throw new Exception("10||Missing var list");
                }

                cmdOut += AppClass.expDelimiter.ToString() + var;
            }

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * Variable List var AS type - ?Array List?  TODO - Check this out
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_ArrayList(string allowed, List<string> flags, string cmdRest, out string cmdOut, out string asType)
        {
            string var;
            string astype = string.Empty;
            cmdOut = string.Empty;
            asType = string.Empty;

            // Get the var name(s)
            cmdRest = GetNextToken(cmdRest, ", ", out var);
            if (var.Length == 0) throw new Exception("10||Missing var list");
            List<string> aTest = AppHelper.BreakArrayOrUDF(var);
            if (aTest.Count < 2) throw new Exception($"10||Invalid dimension of {var.ToUpper()}");
            cmdOut = AppClass.literalStart + var + AppClass.literalEnd;

            // Check for AS <type>
            if (cmdRest.Length > 3 && cmdRest.StartsWith("AS ", StringComparison.OrdinalIgnoreCase))
            {
                // Handle the AS
                cmdRest = cmdRest[3..].Trim();
                cmdRest = GetNextToken(cmdRest, ", ", out astype);
            }

            asType = AppClass.literalStart + astype + AppClass.literalEnd;


            // Does it have a comma-delimited list?
            while (cmdRest.Length > 0 && cmdRest[0] == ',')
            {
                cmdRest = cmdRest[1..].Trim();

                // Get the dimension expression
                cmdRest = GetNextToken(cmdRest, ", ", out var);
                if (var.Length == 0) throw new Exception("10||Missing var list");
                cmdOut += AppClass.expDelimiter.ToString() + AppClass.literalStart.ToString() + var + AppClass.literalEnd.ToString();

                // Check for AS
                astype = string.Empty;
                if (cmdRest.Length > 3 && cmdRest.StartsWith("AS ", StringComparison.OrdinalIgnoreCase))
                {
                    // Handle the AS
                    cmdRest = cmdRest[3..].Trim();
                    cmdRest = GetNextToken(cmdRest, ", ", out astype);
                }

                asType += AppClass.expDelimiter.ToString() + (astype.Length > 0 ? AppClass.literalStart.ToString() + astype + AppClass.literalEnd.ToString() : string.Empty);
            }

            return cmdRest;
        }

        /* -----------------------------------------------------------------------------------------*
         * Variable List for LOCAL, PRIVATE, PUBLIC
         * var1, var2 as <type>, var3...
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_VarList8(string allowed, List<string> flags, string cmdRest, out string cmdOut, out string cmdAs)
        {
            string var = string.Empty;
            cmdOut = string.Empty;

            cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out var);
            cmdOut = var;

            // Must include a matching AS entry, even if blank
            var = string.Empty;
            if (cmdRest.Length > 3 && cmdRest[..3].Equals("AS ", StringComparison.OrdinalIgnoreCase))
            {
                cmdRest = cmdRest[3..].Trim();
                cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out var);
            }

            cmdAs = var;

            // It may multiple expressions delimited with commas
            while (cmdRest.Length > 0 && cmdRest[0] == ',')
            {
                cmdRest = cmdRest[1..].Trim();

                // Get the var name(s)
                cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out var);
                cmdOut += AppClass.expDelimiter.ToString() + var;

                // Must include a matching AS entry, even if blank
                var = string.Empty;
                if (cmdRest.Length > 3 && cmdRest[..3].Equals("AS ", StringComparison.OrdinalIgnoreCase))
                {
                    cmdRest = cmdRest[3..].Trim();
                    cmdRest = GetNameLiteralOrExpression(cmdRest, ", ", out var);
                }

                cmdAs += AppClass.expDelimiter.ToString() + var;
            }

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * FIELD
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_Field(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            string result = string.Empty;

            cmdRest = GetNextLiteralOrExpression(cmdRest, ", ", out cmdOut);
            if (cmdRest.Length < 3) throw new Exception("10||Missing field name");
            if (result[0] == '(') throw new Exception("10||Field expressions only");

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * FieldList
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_FieldList(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            cmdRest = GetNextLiteralOrExpression(cmdRest, ", ", out cmdOut);
            if (cmdOut.Length < 3) throw new Exception("10||Missing field(s)");
            if (cmdOut[0] == AppClass.expByte) throw new Exception("10||Field expressions only");

            // Multiple field expressions delimited with commas
            while (cmdRest[0] == ',')
            {
                cmdRest = cmdRest[1..].Trim();
                if (cmdRest.Length > 0)
                {
                    cmdRest = GetNextLiteralOrExpression(cmdRest, ", ", out string var);
                    if (var.Length < 3) throw new Exception("10||Missing field expression");
                    if (var[0] == AppClass.expByte) throw new Exception("10||Field expressions only");
                    cmdOut += AppClass.expDelimiter.ToString() + var;

                }
                else
                    throw new Exception("10|");
            }

            return cmdRest;
        }


        /* -----------------------------------------------------------------------------------------*
         * SCOPE
         * -----------------------------------------------------------------------------------------*/
        public string StatementBreak_Scope(string allowed, List<string> flags, string cmd, string cmdRest, out string cmdOut)
        {
            if (cmd.Equals("all", StringComparison.OrdinalIgnoreCase) || cmd.Equals("rest", StringComparison.OrdinalIgnoreCase))
            {
                if (allowed.Contains("SC0") == false) throw new Exception("10|");

                // SCOPE all|rest
                // ----------------------------------------
                cmdOut = cmd[..1].Equals("a", StringComparison.OrdinalIgnoreCase) ? "A" : "S";
            }
            else
            {
                // SCOPE expressions Record, Top, & Next
                // ----------------------------------------
                cmdRest = GetNextExpression(cmdRest, string.Empty, out string x);

                if (x.Length > 0)
                    cmdOut = cmd[..1].ToUpper() + AppClass.expDelimiter + x;
                else
                    throw new Exception("10||Invalid scope");
            }

            return cmdRest;
        }

        /* -----------------------------------------------------------------------------------------*
         * The Calculate command has an expression list in 
         * the format of:
         * 
         *      Commandd(field),Command(field),...
         *      
         * The commands are as follows, but I need to learn more 
         * about them to understand which should stay here, which 
         * should become math functions, or be in both places.
         * 
         *          AVG() - Average
         *          CNT() - Count
         *          COR() - Correlation - New
         *          COV() - Covariance - New
         *          IRR() - Internal Rate of Return - New
         *          MAX() - Maximum value
         *          MED() - Median Value - New
         *          MIN() - Minimum value
         *          NPV() - Net Present Value
         *          NFV() - Net Future Value - New
         *          NOP() - Number Of Payments - New
         *          PPP() - Payment Per Period - New
         *          STD() - Standard Deviation
         *          TIP() - Total Interest Paid - New
         *          VAR() - Variance
         * -----------------------------------------------------------------------------------------*/
        private string StripCMD(string cmdRest, out string cCmd)
        {
            cCmd = string.Empty;

            if (cmdRest.Length > 5)
            {
                if (cmdRest[..6].Equals("count(", StringComparison.OrdinalIgnoreCase))
                    cmdRest = "cmd(" + cmdRest[6..];

                // Get the Command code for CALCULATE
                cCmd = cmdRest[..4].ToLower() switch
                {
                    "avg(" => "A",
                    "cnt(" => "C",
                    "max(" => "X",
                    "min(" => "M",
                    "npv(" => "N",
                    "std(" => "T",
                    "sum(" => "S",
                    "var(" => "V",
                    _ => throw new Exception(string.Format("10||Unknown CALCULATE expression: {0}", cmdRest))
                };

                // expose the expression
                cmdRest = cmdRest[3..];
            }
            else
                throw new Exception(string.Format("10||Unknown CALCULATE expression: {0}", cmdRest));

            return cmdRest;
        }

        public string StatementBreak_TableFields(string allowed, List<string> flags, string cmdRest, out string cmdOut)
        {
            cmdOut = string.Empty;

            // Is it a () or FROM ARRAY command?
            if (cmdRest[0] == '(')
            {
                cmdRest = cmdRest[1..];

                while (cmdRest[0] != ')')
                {
                    if (cmdRest[0] == ',') cmdRest = cmdRest[1..].Trim();

                    // Get the () area
                    cmdRest = GetNextToken(cmdRest, ",)", out string field);

                    // Break it down
                    string fNull = string.Empty;
                    string fAutoVal = string.Empty;
                    string fAutoStep = string.Empty;
                    string fColl = string.Empty;

                    // Zero fill width and decimals
                    GetNextExpression("0", string.Empty, out string zero);
                    string fWidth = zero;
                    string fDec = zero;

                    field = GetNextToken(field, string.Empty, out string fName);
                    field = GetNextToken(field, " (", out string fType);

                    if (field.Length > 0 && field[0] == '(')
                    {
                        field = GetNextToken(field[1..], ",)", out fWidth);
                        if (field.Length > 0 && field[0] == ',') field = field[1..];
                        field = GetNextToken(field, ")", out fDec);
                        fWidth = GetRPNString(Program.CurrentApp, fWidth);
                        fDec = GetRPNString(Program.CurrentApp, fDec.Length > 0 ? fDec : "0");

                        if (fWidth[1] != 'N' || fDec[1] != 'N')
                            throw new Exception("10|");

                        // Eat the remaining )
                        field = field[1..];
                    }

                    // [NULL | NOT NULL] [AUTOINC [NEXTVALUE NextValue [STEP StepValue]]] [COLLATE cCollateSequence]
                    if (field.Length > 0)
                    {
                        string fRest = field;
                        while (fRest.Length > 0)
                        {
                            fRest = GetNextToken(field, string.Empty, out string fInfo);
                            fInfo = fInfo.ToLower();

                            switch (fInfo)
                            {
                                case "null":
                                    fNull = "null";
                                    break;

                                case "not":
                                    fRest = GetNextToken(field, string.Empty, out fInfo);
                                    if (fInfo.Equals("null", StringComparison.OrdinalIgnoreCase) == false)
                                        throw new Exception("10|");
                                    break;

                                case "autoinc":
                                    fRest = GetNextToken(field, string.Empty, out fInfo);
                                    if (fInfo.Equals("nextvalue"))
                                    {
                                        fRest = GetNextToken(field, string.Empty, out fAutoVal);
                                        fAutoVal = GetRPNString(Program.CurrentApp, fAutoVal);
                                        if (fAutoVal[1] != 'N')
                                            throw new Exception("10|");
                                    }
                                    else
                                        throw new Exception("10|");

                                    GetNextToken(field, string.Empty, out fInfo);
                                    if (fInfo.Equals("nextvalue"))
                                    {
                                        // Eat STEP
                                        fRest = GetNextToken(field, string.Empty, out _);
                                        fRest = GetNextToken(field, string.Empty, out fAutoStep);
                                        fAutoStep = GetRPNString(Program.CurrentApp, fAutoStep);
                                        if (fAutoStep[1] != 'N')
                                            throw new Exception("10|");
                                    }
                                    else
                                        throw new Exception("10|");

                                    break;

                                case "collate":
                                    fRest = GetNextToken(field, string.Empty, out fColl);
                                    fColl = GetRPNString(Program.CurrentApp, fColl);
                                    if (fColl[1] != 'N')
                                        throw new Exception("10|");
                                    break;

                                default:
                                    throw new Exception("10||Unkown field component " + fInfo);
                            }
                        }
                    }

                    cmdOut += AppClass.literalStart + fName + AppClass.literalEnd + AppClass.expParam
                        + AppClass.literalStart + fType + AppClass.literalEnd + AppClass.expParam
                        + fWidth + AppClass.expParam
                        + fDec + AppClass.expParam
                        + fNull + AppClass.expParam
                        + fAutoVal + AppClass.expParam
                        + fAutoStep + AppClass.expParam
                        + fColl + AppClass.expDelimiter;

                    if (cmdRest.Length < 1) throw new Exception("10||Missing closing parentheses of table definition ')'");
                }

                // Get rid of the closing parentheses
                if (cmdRest.Length > 0 && cmdRest[0] == ')') cmdRest = cmdRest[1..].Trim();

                // Remove the last expression delimiter
                cmdOut = cmdOut.TrimEnd(AppClass.expDelimiter);

            }

            return cmdRest;
        }
    }
}
