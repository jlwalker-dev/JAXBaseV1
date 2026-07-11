using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.Compiler
{
    public static class JAXBase_Compiler_D
    {

        /* TODO
         * 
         * Decide which DEFINE statement is being called
         * and then send control to that process
         *      
         */
        public static string Define(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, string.Empty, out string token);

                if (jbc.lang!.Abreviations.TryGetValue(token.ToLower(), out string? abbr))
                    token = abbr;

                if (jbc.lang!.CommandParts.TryGetValue(token.ToLower(), out string? cmdPart))
                    token = cmdPart;

                switch (token.ToLower())
                {
                    case "class": result = DefineClass(jbc, cmdLine); break;
                    default:
                        throw new Exception("systax error");
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
         * DEFINE CLASS ClassName1 AS ParentClass
         * 
         * 
         * 
         */
        public static string DefineClass(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                if (Program.CurrentApp.InDefine.Length > 0)
                    throw new Exception("1926||");

                Program.CurrentApp.InDefine = "C";

                result = jbc.Key_Parser(cmdLine, ["Class"], "XX0,AS1", []);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                Program.CurrentApp.InDefine = string.Empty;
            }
            return AppErrorHandling.ErrorCount() > 0 ? string.Empty : result;
        }




        /*
         * 
         * DELETE [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias]
         *
         * DELETE DATABASE DatabaseName
         * 
         * DELETE TABLE tablename 
         * 
         * DELETE FILE FileName
         * 
         */
        public static string Delete(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, string.Empty, out string token);

                if (jbc.lang!.Abreviations.TryGetValue(token.ToLower(), out string? abbr))
                    token = abbr;

                if (jbc.lang!.CommandParts.TryGetValue(token.ToLower(), out string? cmdPart))
                    token = cmdPart;

                switch (token.ToLower())
                {
                    case "database":
                    case "table":
                    case "file":
                        result = jbc.Key_Parser(JAXLib.Proper(cmdLine), [token], "XX0", []);
                        break;

                    default:
                        result = jbc.Generic_Parser(cmdLine, "SC0,FR0,WH0,IN0", []);
                        break;
                }
            }
            catch (Exception ex)
            {
                result = string.Empty;
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;

        }




        /* 
         * 
         * DISPLAY DATABASE [cDataBase]  [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY PROCEDURES [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         *
         * DISPLAY STATUS     [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY TABLES     [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY VIEWS      [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY FILES   litExpr [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]]
         *
         * DISPLAY MEMORY  litExpr [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY OBJECTS litExpr [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         * DISPLAY [FIELDS Literal list, FIELDS LIKE skeleton | FIELDS EXCEPT skeleton] [IN nWorkArea | cTableAlias][Scope] [FOR lExpression1] [WHILE lExpression2] [OFF] [NOCONSOLE] [NOOPTIMIZE] [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]]
         * 
         * DISPLAY STRUCTURE [IN nWorkArea | cTableAlias] [TO PRINTER [PROMPT] | TO FILE FileName [ADDITIVE]] [NOCONSOLE]
         * 
         */
        public static string Display(JAXBase_Compiler jbc, string cmdLine)
        {
            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, " ", out string token);

                if (jbc.lang!.Abreviations.TryGetValue(token.ToLower(), out string? abbr))
                    token = abbr;

                if (jbc.lang!.CommandParts.TryGetValue(token.ToLower(), out string? cmdPart))
                    token = cmdPart;

                token = token.ToLower();

                switch (token)
                {
                    case "database":
                    case "data":
                        result = jbc.Key_Parser(cmdLine, ["Database"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "procedures":
                    case "proc":
                        result = jbc.Key_Parser(cmdLine, ["Procedures"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "status":
                    case "stat":
                        result = jbc.Key_Parser(cmdLine, ["Status"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "tables":
                    case "tabl":
                        result = jbc.Key_Parser(cmdLine, ["Tables"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "views":
                    case "view":
                        result = jbc.Key_Parser(cmdLine, ["Views"], "TO5", ["additive", "noconsole"]);
                        break;

                    case "fields":
                    case "field":
                    case "fiel":
                        result = jbc.Generic_Parser(cmdLine, "FV3,SC0,FR0,WH0,TO5", ["additive", "noconsole", "off"]);
                        break;

                    case "files":
                    case "file":
                        result = jbc.Key_Parser(cmdLine, ["Files"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "memory":
                    case "memo":
                        result = jbc.Key_Parser(cmdLine, ["Memory"], "LK2,TO5", ["additive", "noconsole"]);
                        break;

                    case "objects":
                    case "obj":
                    case "object":
                        result = jbc.Key_Parser(cmdLine, ["Objects"], "XX0,TO5", ["additive", "noconsole"]);
                        break;

                    case "structure":
                    case "stru":
                        result = jbc.Key_Parser(cmdLine, ["Structure"], "IN0,TO5", ["additive", "noconsole"]);
                        break;

                    default:
                        throw new Exception("1999|" + token);
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }
            return result;

        }


        /*
         *  DO ProgramName1 | ProcedureName [IN ProgramName2] [WITH ParameterList]
         *  
         *  DO FORM FormName [NAME VarName [LINKED]] [WITH cParameterList] [TO VarName] [NOREAD] [NOSHOW]
         *
         *  DO REPORT reportName [WITH reportObject]
         *  
         *  DO LABEL labelName [WITH reportObject]
         *
         *  DO WHILE lExpression 
         *      Commands 
         *      [LOOP]
         *      [EXIT]
         *  ENDDO
         *  
         *  DO CASE
         *      [CASE lExpression1
         *          [Commands]]
         *      [CASE lExpression2 
         *          [Commands]]
         *      ...
         *      [CASE lExpressionN
         *          [Commands]]
         *      [OTHERWISE 
         *          [Commands]]
         *  ENDCASE
         *  
         *  DO 
         *  UNTIL lExpression
         *  
         *  <cmdcode>P|R|F|W|C[<expressionlist>]
         *  P - program
         *  R - procedure in program
         *  F - Form
         *  W - While
         *  C - Case
         *  U - Until
         * 
         */
        public static string Do(JAXBase_Compiler jbc, string cmdLine)
        {
            cmdLine = cmdLine.Trim();
            string expression = string.Empty;
            string parentPrg = string.Empty;
            string procedure = string.Empty;
            string formname = string.Empty;
            string parmlist = string.Empty;
            string linkvar = string.Empty;
            string toVar = string.Empty;

            string result = string.Empty;

            try
            {
                jbc.GetNextToken(cmdLine, " (", out string token);

                if (jbc.lang!.Abreviations.TryGetValue(token.ToLower(), out string? abbr))
                    token = abbr;

                if (jbc.lang!.CommandParts.TryGetValue(token.ToLower(), out string? cmdPart))
                    token = cmdPart;

                switch (token.ToLower())
                {
                    case "label":
                    case "report":
                        int f1 = cmdLine.ToLower().IndexOf(" with ");
                        int f2 = f1 > 0 ? cmdLine.ToLower().IndexOf('=', f1) : -1;

                        result = Program.CurrentApp.CompilerXRef["CS"].ToString() + token[..1].ToUpper() + AppClass.stmtDelimiter;

                        if (f2 < 0)
                            result += jbc.Key_Parser(cmdLine, [token], "XX0,WT1", ["noconsole"]);    // WITH reportObj
                        else
                            result += jbc.Key_Parser(cmdLine, [token], "XX0,WT0", ["noconsole"]);    // WITH paramList
                        break;

                    case "form":    // DO FORM FormName | ? [NAME VarName [LINKED]] [WITH cParameterList] [TO VarName] [NOREAD] [NOSHOW]
                        result = Program.CurrentApp.CompilerXRef["CS"].ToString() + "F" + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdLine, "NM0,WT0,TO0", ["noread", "noshow", "linked"]);
                        break;

                    case "case":    // DO CASE
                        result = Program.CurrentApp.CompilerXRef["CS"].ToString() + AppLoop.AddLoop("C");
                        break;

                    case "while":   // DO WHILE lExpression 
                        cmdLine = jbc.GetNextToken(cmdLine, string.Empty, out token);
                        expression = jbc.GetRPNString(Program.CurrentApp, cmdLine);
                        result = Program.CurrentApp.CompilerXRef["CS"].ToString() + AppLoop.AddLoop("W") + AppClass.stmtDelimiter +Program.CurrentApp.CompilerXRef["XX"].ToString()+ expression;
                        break;

                    default:        // DO ProgramName1 | ProcedureName [IN ProgramName2] [WITH ParameterList]
                        if (cmdLine.Length == 0)
                        {
                            // Nothing following the DO assumes an UNTIL is coming
                            result = Program.CurrentApp.CompilerXRef["CS"].ToString() + AppLoop.AddLoop("U");
                        }
                        else
                            result = Program.CurrentApp.CompilerXRef["CS"].ToString() + "P" + AppClass.stmtDelimiter + jbc.Generic_Parser(cmdLine, "XX0,IN0,WT3", ["noread", "noshow", "linked"]); // Just expressions after with

                        break;
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
