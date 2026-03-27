using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Math;
using JAXBase.XBase;
using JAXBase.Utilities.Utilities;
using JAXBase.UI.Dialogs;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_A
    {
        /* 
         * ACTIVATE CONSOLE cName
         */
        public static string Activate(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {

                // Activate DEFAULT console if no name specified
                string name = string.IsNullOrWhiteSpace(eCodes.NAME) ? "default" : eCodes.NAME.ToLower();

                switch (eCodes.SUBCMD.ToLower())
                {
                    case "console":
                        //jbe.App.JAXConsoles[name].Active(true);
                        break;

                    default:
                        throw new Exception($"1099||Unknown device {eCodes.SUBCMD.ToUpper()}");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * 
         * ADD OBJECT [PROTECTED] ObjectName AS ClassName2 [NOINIT] [WITH cPropertylist]
         * 
         * ADD CLASS ClassName [OF ClassLibraryName1] TO ClassLibraryName2 [OVERWRITE]
         *
         * ADD TABLE TableName | ?   [NAME LongTableName]
         * 
         */
        public static async Task<string> Add(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                switch (eCodes.SUBCMD)
                {
                    case "class":   // Class
                        result = AddClass(jbe, eCodes);
                        break;

                    case "object":   // Object
                        // "cla"|objName|ClassName|flags|parameters
                        result = await AddObject(jbe, eCodes);
                        break;

                    case "table":   // Table
                        result = AddTable(jbe, eCodes);
                        break;

                    default:
                        throw new Exception($"1999||Unsupported add type {eCodes.SUBCMD}");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        /* 
         * Add Object in definition
         * 
         */
        private static async Task<string> AddObject(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            JAXObjectWrapper addObject;
            List<ParameterClass> xParameters = [];

            if (jbe.App.InDefineObject is null)
                throw new Exception("1928|");

            if (string.IsNullOrWhiteSpace(eCodes.NAME) || eCodes.As.Count != 1 || string.IsNullOrWhiteSpace(eCodes.As[0]))
                throw new Exception("10|");

            JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.CLASS);
            string className = answer.AsString();

            answer = await jbe.App.SolveFromRPNString(eCodes.As[0]);
            string asClass = answer.AsString();

            for (int i = 0; i < eCodes.With.Count; i++)
            {
                string property = eCodes.With[i].RNPExpr;
                int f = property.IndexOf('=');
                if (f < 0) throw new Exception("10|");
                int e = f + 1;

                string propname = property[..f].Trim();
                string propName = answer.AsString();
                answer = await jbe.App.SolveFromRPNString(property[e..].Trim());
                ParameterClass xProp = new() { PName = property[..f] };
                xProp.token.Element.Value = answer;
                xParameters.Add(xProp);
            }

            // Define the object
            addObject = new(jbe.App, asClass, className, xParameters);

            await jbe.App.InDefineObject.AddObject(addObject);
            return result;
        }

        /* 
         * TODO - ADD Class
         */
        private static string AddClass(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            throw new Exception("1999|ADD CLASS");
            //return string.Empty;
        }

        /* 
         * TODO - ADD Table
         */
        private static string AddTable(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            throw new Exception("1999|ADD TABLE");
            //return string.Empty;
        }


        /* TODO
         * 
         * ALTER TABLE TableName1 ADD | ALTER [COLUMN] FieldName1 FieldType [( nFieldWidth [, nPrecision])] 
         *      [NULL | NOT NULL] [CHECK lExpression1 [ERROR cMessageText1]] 
         *      [AUTOINC [NEXTVALUE NextValue [STEP StepValue]]] [DEFAULT eExpression1] 
         *      [PRIMARY KEY | UNIQUE [COLLATE cCollateSequence]] 
         *      [REFERENCES TableName2 [TAG TagName1]] [NOCPTRANS] [NOVALIDATE]
         *      
         * ALTER TABLE TableName1 ALTER [COLUMN] FieldName2 [NULL | NOT NULL] [SET DEFAULT eExpression2] 
         *      [SET CHECK lExpression2 [ERROR cMessageText2]] [ DROP DEFAULT ] [ DROP CHECK ] [ NOVALIDATE ]
         *      
         * ALTER TABLE TableName1 [DROP [COLUMN] FieldName3] 
         *      [SET CHECK lExpression3 [ERRORcMessageText3]] [DROP CHECK] 
         *      [ADD PRIMARY KEY eExpression3 [FOR lExpression4] TAG TagName2
         *      [COLLATE cCollateSequence]] [DROP PRIMARY KEY] 
         *      [ADD UNIQUE eExpression4 [[FOR lExpression5] TAG TagName3 
         *      [COLLATE cCollateSequence]]] [DROP UNIQUE TAG TagName4] 
         *      [ADD FOREIGN KEY [eExpression5] [FOR lExpression6] TAG TagName4 
         *          REFERENCES TableName4 [TAG TagName4][COLLATE cCollateSequence]
         *          REFERENCES TableName2 [TAG TagName5]] 
         *      [DROP FOREIGN KEY TAG TagName6 [SAVE]] 
         *      [RENAME COLUMN FieldName4 TO FieldName5] [NOVALIDATE]
         *      
         *
         */
        public static string Alter(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }



        /*
         * APARAMETERS aVar
         * 
         * Places all parameters from the stack into a private array
         * 
         */
        public static async Task<string> AParameters(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            if (jbe.App.AppLevels.Count == 0) throw new Exception("2|");
            if (JAXLib.InList(jbe.App.AppLevels[^1].LastCommand, -1, jbe.CmdNum["procedure"], jbe.CmdNum["*sc"]) == false) throw new Exception("8|");

            // Get the parameter name
            JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
            string aVar = string.Empty;

            try
            {
                if (answer.Element.Type.Equals("C"))
                {
                    aVar = answer.AsString();

                    // Get the parameter count
                    int j = jbe.App.ParameterClassList.Count;

                    // Create the array if it does not exist
                    jbe.App.SetVarOrMakePrivate(aVar, 1, j, true);

                    // Get the array pointer
                    JAXObjects.Token destArray = await jbe.App.GetVarToken(aVar);

                    // Fill in the array
                    for (int i = 0; i < j; i++)
                        destArray._avalue[i].Value = jbe.App.ParameterClassList[i].token.Element.Value;

                    // Clear the parameter list
                    jbe.App.ParameterClassList.Clear();
                }
                else
                    throw new Exception("11|");
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Format("Private array {0} created with {1} elements", aVar, jbe.App.ParameterClassList.Count);
        }




        /*
         * APPEND [BLANK] [IN nWorkArea | cTableAlias] [NOMENU]
         * 
         * TODO 
         * -------------
         * APPEND FROM ARRAY ArrayName [FOR lExpression] [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         * 
         * APPEND FROM FileName [FIELDS FieldList] [FOR lExpression] [[TYPE] [DELIMITED [WITH Delimiter | WITH BLANK | WITH TAB | WITH CHARACTER Delimiter] |  SDF | CSV | XLS | XL5 | XL8 | XLX [SHEET cSheetName]]] [AS nCodePage]
         * 
         * APPEND FROM JSON jsonstring [FOR lExpression] [FIELDS FieldList | FIELDS LIKE Skeleton | FIELDS EXCEPT Skeleton]
         * 
         *      
         */
        public static async Task<string> Append(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            int wa = jbe.App.CurrentDS.CurrentWorkArea();
            int ds = jbe.App.CurrentDataSession;

            try
            {
                JAXObjects.Token workarea = new();

                if (eCodes.SESSION > 0)
                    jbe.App.SetDataSession(eCodes.SESSION);

                if (string.IsNullOrWhiteSpace(eCodes.InExpr) == false)
                {
                    workarea = await jbe.App.SolveFromRPNString(eCodes.InExpr);

                    if (workarea.Element.Type.Equals("N"))
                        jbe.App.CurrentDS.SelectWorkArea(workarea.AsInt());
                    else if (workarea.Element.Type.Equals("C"))
                        jbe.App.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                    else
                        throw new Exception("11|");
                }

                // Is the workarea valid?
                if (jbe.App.CurrentDS.CurrentWA is null || jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", jbe.App.CurrentDS.CurrentWorkArea()));

                if (eCodes.SUBCMD.Equals("blank", StringComparison.OrdinalIgnoreCase))
                {
                    // Append a blank record
                    await jbe.App.CurrentDS.CurrentWA.DBFAppendRecord(null);
                }
                else if (eCodes.SUBCMD.Equals("array", StringComparison.OrdinalIgnoreCase))
                {
                    // APPEND FROM ARRAY
                    // result = JAXDataHandler.AppendArray(jbe, eCodes)
                    throw new Exception("1999|APPEND FROM ARRAY");
                }
                else if (eCodes.SUBCMD.Equals("file", StringComparison.OrdinalIgnoreCase))
                {
                    // APPEND FROM filename
                    // result = JAXDataHandler.AppendFile(jbe, eCodes)
                    throw new Exception("1999|APPEND FROM FILE");
                }
                else if (eCodes.SUBCMD.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    // APPEND FROM JSON
                    // result = JAXDataHandler.AppendJSON(jbe, eCodes)
                    throw new Exception("1999|APPEND FROM JSON");
                }
                else
                {
                    // Bring up the APPEND form
                    throw new Exception("1999|APPEND");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // Get back to the starting data session & work area
            jbe.App.SetDataSession(ds);
            jbe.App.CurrentDS.SelectWorkArea(wa);
            return string.Empty;
        }


        /*
         * 
         * ASSERT lExpression [MESSAGE cMessageText] [TO DEBUG | VAR variable | TO FILE filename]
         * 
         */
        public static async Task<string> Assert(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                // Call the Assert dialog with the message
                string msg = string.IsNullOrWhiteSpace(eCodes.MESSAGE) ? "Assert: " + eCodes.COMMAND : eCodes.MESSAGE;

                int res = 3;
                if (Array.IndexOf(eCodes.Flags, "noconsole") < 0)
                {
                    JAXDialogs MsgBox = new();
                    res = MsgBox.AssertDialog(msg);
                }

                // Once the user choses, write the assert and choice 
                // to the debug console, a variable, or append to a text file
                msg = msg + Environment.NewLine + "User selected " + res switch
                {
                    2 => "Cancel",
                    3 => "Ingore",
                    4 => "Ignore All",
                    _ => "Debug"
                };

                string toType = string.Empty;
                string toName = string.Empty;
                JAXObjects.Token answer = new();

                if (eCodes.To.Count > 0)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.To[0].Type);
                    if (answer.Element.Type.Equals("C"))
                        toType = answer.AsString();
                    else
                        throw new Exception("11|");

                    answer = await jbe.App.SolveFromRPNString(eCodes.To[0].Name);
                    if (answer.Element.Type.Equals("C"))
                        toName = answer.AsString();
                    else
                        throw new Exception("11|");
                }

                jbe.App.DebugLog("ASSERT: " + result);

                // Always write to debug file if DEBUG is ON
                if (jbe.App.CurrentDS.JaxSettings.Debug && string.IsNullOrWhiteSpace(jbe.App.CurrentDS.JaxSettings.DebugOut) == false)
                {
                    string filestr = "ASSERT:" + msg + Environment.NewLine + "RESPONSE: " + res.ToString() + Environment.NewLine;
                    JAXLib.StrToFile(filestr, jbe.App.CurrentDS.JaxSettings.DebugOut, 1);
                }

                // Optionally write to the specified destination
                if (eCodes.To.Count > 0)
                {
                    if (toType.Equals("console", StringComparison.OrdinalIgnoreCase))
                    {
                        // TO DEBUG
                        //if (string.IsNullOrWhiteSpace(toName))
                        //    toName = jbe.App.ActiveConsole;

                        if (string.IsNullOrWhiteSpace(toName) == false)
                        {
                            string filestr = "ASSERT:" + msg + Environment.NewLine + "RESPONSE: " + res.ToString();
                            JAXLib.StrToFile(filestr, toName, 3);
                            //jbe.App.JAXConsoles[toName].WriteLine(filestr);
                        }
                    }
                    else if (toType.Equals("file", StringComparison.OrdinalIgnoreCase))
                    {
                        // TO FILE filename
                        if (string.IsNullOrWhiteSpace(toName))
                            throw new Exception("10|");

                        string filestr = "ASSERT:" + msg + Environment.NewLine + "RESPONSE: " + res.ToString() + Environment.NewLine;
                        JAXLib.StrToFile(filestr, toName, 1);
                    }
                    else if (toType.Equals("var", StringComparison.OrdinalIgnoreCase))
                    {
                        // TO VAR variable
                        if (string.IsNullOrWhiteSpace(toName))
                            throw new Exception("10|");

                        string filestr = msg + ((char)13).ToString() + res.ToString();
                        jbe.App.SetVarOrMakePrivate(toName, 1, 1, false);
                        jbe.App.SetVar(toName, filestr, 1, 1);
                    }
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* 
         * 
         * AVERAGE   [ExpressionList] [Scope] [FOR lExpression1] [WHILE lExpression2] [TO VarList | TO ARRAY ArrayName] [NOOPTIMIZE] [IN nWorkArea | cTableAlias]
         * 
         * SUM       [ExpressionList] [Scope] [FOR lExpression1] [WHILE lExpression2] [TO VarList | TO ARRAY ArrayName] [NOOPTIMIZE] [IN nWorkArea | cTableAlias]
         * 
         * 
         *          Code: CS,XX,SC,FR,WL,TO,FG
         *              subcmd      A - Average, C - Calculate, S - Sume
         *              expressions
         *              scope
         *              for
         *              while
         *              to
         *              flags       N - NoOptimize (ignored in Version 1)
         *   
         */
        public static async Task<string> Average(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                string cmd = string.IsNullOrWhiteSpace(eCodes.SUBCMD) ? throw new Exception("10|") : eCodes.SUBCMD;
                List<ExCodeRPN> Expr = eCodes.Expressions.Count > 0 ? eCodes.Expressions : throw new Exception("10|");
                List<ExCodeName> To = eCodes.To.Count > 0 ? eCodes.To : throw new Exception("10|");
                //ExCodeScope Scope = eCodes.Scope;
                string ForExpr = eCodes.ForExpr;
                string WhileExpr = eCodes.WhileExpr;
                string[] Flags = eCodes.Flags;

                JAXObjects.Token answer = new();

                bool noOptimize = false;

                JAXMath jaxMath = new(jbe.App);

                if (cmd.Length != 1)
                    throw new Exception("Missing command expression list");

                // ---------------------------------------------------------------------
                // Flags
                // ---------------------------------------------------------------------
                noOptimize = Flags.Length > 0 && Flags.Contains("N");

                // ---------------------------------------------------------------------
                // WorkArea|Alias
                // ---------------------------------------------------------------------
                int wa = jbe.App.CurrentDS.CurrentWorkArea();

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : jbe.App.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    jbe.App.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    jbe.App.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (jbe.App.CurrentDS.CurrentWA is null || jbe.App.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", jbe.App.CurrentDS.CurrentWorkArea()));

                // ---------------------------------------------------------------------
                // Prep record position and set the scope
                // ---------------------------------------------------------------------
                JAXDirectDBF Table = jbe.App.CurrentDS.CurrentWA;
                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, Table, true);

                // ---------------------------------------------------------------------
                // 5 - ToType (V|A)
                // ---------------------------------------------------------------------
                if (To.Count == 0)
                    throw new Exception("10||Empty To Expression");

                // ---------------------------------------------------------------------
                // 6 - VarList
                // ---------------------------------------------------------------------
                if (eCodes.Expressions.Count > eCodes.To.Count)
                    throw new Exception("1230|");                                               // Too many arguments

                if (eCodes.Expressions.Count < eCodes.To.Count)
                    throw new Exception("94|");                                                 // Must specify additional parameters

                List<JAXObjects.Token> sums = [];

                // ---------------------------------------------------------------------
                // Create the sums list for the results
                // ---------------------------------------------------------------------
                for (int j = 0; j < Expr.Count; j++)
                {
                    answer = new();
                    answer.Element.Value = 0;
                    sums.Add(answer);
                }

                // ---------------------------------------------------------------------
                // first record is already in the buffer and continue working on
                // records until we reach EOF.  We use goto top/bottom and skip
                // because we want to play nice with indexes
                // ---------------------------------------------------------------------
                while (Table.DbfInfo.DBFEOF == false && Table.DbfInfo.RecCount > 0)
                {
                    // The For expression says if we use this record
                    JAXObjects.Token temp = await jbe.App.SolveFromRPNString(ForExpr);
                    if (ForExpr.Length == 0 || temp.Element.ValueAsBool)
                    {
                        // No FOR or it evaluated to true
                        // Now check the WHILE which says if we continue to process records
                        temp = await jbe.App.SolveFromRPNString(WhileExpr);
                        if (WhileExpr.Length == 0 || temp.Element.ValueAsBool)
                        {
                            // Either no while or it evaluated to true
                            // Add the values of this record to the sum values for each expression
                            for (int j = 0; j < Expr.Count; j++)
                            {
                                answer = await jbe.App.SolveFromRPNString(Expr[j].RNPExpr);

                                if (answer.Element.Type.Equals("N"))
                                {
                                    double dval = sums[j].Element.ValueAsDouble;
                                    sums[j].Element.Value = dval + answer.AsDouble();
                                }
                            }

                            // Have we reached the end of the until flag scope?
                            if (jaxScope.IsDone()) break;
                        }
                        else
                        {
                            // break out of the loop because the
                            // while statement is false
                            break;
                        }

                        // Otherwise try to read in the next record
                        await Table.DBFSkipRecord(1);
                    }
                }

                // ---------------------------------------------------------------------
                // Now finalize the results and place them into the requested vars
                // ---------------------------------------------------------------------
                bool NotArray = true;

                if (To[0].Type.Equals("A"))
                {
                    jbe.App.SetVarOrMakePrivate(To[0].Name, 1, Expr.Count, false);
                    NotArray = false;
                }
                else
                    jbe.App.SetVarOrMakePrivate(To[0].Name, 1, 1, false);

                for (int i = 0; i < Expr.Count; i++)
                {
                    // Get the sum or ave
                    double dval = sums[i].Element.ValueAsDouble;
                    if (cmd.Equals("A")) dval = dval / jaxScope.RecordsRead;

                    if (NotArray)
                    {
                        answer = await jbe.App.GetVarFromExpression(To[i].Name, null);
                        //jbe.App.GetVar(To[i].Name, out answer);

                        if (answer.TType.Equals("U"))
                            jbe.App.SetVarOrMakePrivate(To[i].Name, 1, 1, true);
                    }

                    // Set the value
                    if (NotArray)
                        jbe.App.SetVar(To[i].Name, dval, 1, 1);
                    else
                        jbe.App.SetVar(To[i].Name, dval, 1, i);
                }

                // ---------------------------------------------------------------------
                // Make sure we get back to starting workarea
                // ---------------------------------------------------------------------
                jbe.App.CurrentDS.SelectWorkArea(wa);
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }
    }
}
