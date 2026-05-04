using JAXBase.Core;
using JAXBase.Math;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Windows.Annotations;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_M
    {
        /*
         * Takes a string and converts it to an answer via the full math routine
         * The string can contain a value, like "hello", 10, or .F. or a full expression
         * like A+B*3
         * 
         */
        public static async Task<JAXObjects.Token> RawMath(AppClass app, string expression)
        {
            JAXMath jaxMath = new(app);
            GenericClass gc = await jaxMath.SolveMath(expression);
            return gc.Value;
        }



        /* ---------------------------------------------------------------------------------------------------*
         * PURPOSE:
         *      This routine is used to grab an expression from the command string and return the remaining
         *      command string along with the expression value as a token.  This command is expected to
         *      be used in cases where a literal is expected but may be replaced by an expression in
         *      parenthisis.
         * 
         *      Source examples:
         * 
         *          USE (tablename)
         *      
         *          AVERAGE (exprString) ALL TO ARRAY (arrayName)
         *      
         *      This allows us to extend the XBase language by putting in (experession) instead of
         *      having to perform marco substituion all the time, which will be faster since we you
         *      need to compile macro supstitution results during execution.
         * 
         * 
         * 
         * PROCESS DESCRIPTION:
         *      Get the next expression value from the command and send out the
         *      value found as an object token and return the rest of the string
         * 
         *      Literals are in the form of:
         *          <literalStart>literalstring<literalEnd>
         *      
         *      Expressions are in the form:
         *          <expByte>expstring1<expParam>exprstring2<exprParam>exprstring3...<expEnd>
         * 
         *      Grab the string between the start and end then process accordingly.  A literal
         *      is passed back as a string, while an expression is broken into a list by <expParam> 
         *      byte and returned, typically, as a string.
         * 
         * ---------------------------------------------------------------------------------------------------*/
        public static async Task<GenericClass> SolveFromRPNString(AppClass app, string Command)
        {
            GenericClass gc = new();
            List<string> rpnList = [];

            try
            {
                if (Command[0] == AppClass.literalStart)
                {
                    // Process a literal, returning as a string
                    int f = Command.IndexOf(AppClass.literalEnd);
                    if (f < 0)
                        throw new Exception("10|SyntaxError|Mismatched literal expression");

                    gc.Value.Element.Value = Command[1..f];

                    // Remove the literal
                    if (f < Command.Length - 1)
                        gc.cmdRest = Command[++f..];
                    else
                        gc.cmdRest = string.Empty;
                }
                else if (Command[0] == AppClass.expByte)
                {
                    // Process the next expression
                    int f = Command.IndexOf(AppClass.expEnd);

                    if (f < 0) throw new Exception("10|SyntaxError|Mismatched literal expression");
                    if (f < 1) throw new Exception("10|SyntaxError|Missing expression");

                    // Break out the expressions
                    string[] r = Command[1..f].Split(AppClass.expParam);
                    for (int i = 0; i < r.Length; i++)
                    {
                        if (r[i].Length > 0)
                            rpnList.Add(r[i]);
                    }

                    gc.cmdRest = Command[++f..];

                    if (rpnList.Count == 0)
                        throw new Exception("10||Empty expression List");

                    JAXMath jaxMath = new(app);
                    gc.Value = await jaxMath.MathSolve(rpnList);
                }
                else
                    throw new Exception(string.Format("10||Unknown command byte {0}", Command[0]));
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // If there is more to the expression, then there better be
            // an expression delimiter in the next byte
            if (gc.cmdRest.Length > 0)
            {
                if (gc.cmdRest[0] == AppClass.expDelimiter)
                    gc.cmdRest = gc.cmdRest[1..];
                else
                    throw new Exception(string.Format("10||Unexpected byte '{0}'", gc.cmdRest[0]));
            }

            return gc;
        }


        /* TODO
         * 
         * MD
         * 
         */
        public static async Task<string> MD(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (eCodes.Expressions.Count > 0)
                {
                    JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                    if (answer.Element.Type.Equals("C") == false)
                        throw new Exception("10|");

                    string path = JAXLib.AddBackSlash(answer.AsString().Trim());

                    if (Directory.Exists(path))
                        result = "Path " + path + " does not exists";
                    else
                    {
                        Directory.CreateDirectory(path);
                        result = "Created folder " + path;
                    }
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
         * MODIFY CLASS ClassName [OF ClassLibraryName1] | ?
         * MODIFY CLASSLIB ClassLibrary | ?
         * MODIFY COMMAND [FileName | ?] 
         * MODIFY CONNECTION [ConnectionName | ?]
         * MODIFY DATABASE [DatabaseName | ?]
         * MODIFY FILE [FileName | ?] 
         * MODIFY FORM [FormName | ?]
         * MODIFY GENERAL GeneralField
         * MODIFY LABEL [FileName | ?] 
         * MODIFY MEMO MemoField
         * MODIFY MENU [FileName | ?] 
         * MODIFY PROCEDURE [NOWAIT]
         * MODIFY PROJECT [FileName | ?]
         * MODIFY QUERY [FileName | ?]
         * MODIFY REPORT [FileName | ?]
         * MODIFY STRUCTURE
         * MODIFY VIEW ViewName
         * 
         */
        public static async Task<string> Modify(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            string result = string.Empty;
            string editor, fPath, fName, fExt, name;
            List<ParameterClass> pList;
            ParameterClass p;

            try
            {
                JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (answer.Element.Type.Equals("C"))
                    name = answer.AsString();
                else
                    throw new Exception("11|");

                fPath = JAXLib.JustFullPath(name);
                fName = JAXLib.JustStem(name);
                fExt = JAXLib.JustExt(name);

                fPath = string.IsNullOrWhiteSpace(fPath) ? jbe.App.CurrentDS.JaxSettings.Default : fPath;

                if (string.IsNullOrWhiteSpace(fExt))
                {
                    fExt = eCodes.SUBCMD switch
                    {
                        "C" => "clx",
                        "V" => "vcx",
                        "P" => "prg",
                        "M" => "scx",
                        "L" => "lbx",
                        "U" => "mnx",
                        "J" => "pjx",
                        "Q" => "qpr",
                        "R" => "rpx",
                        _ => ""
                    };
                }

                // Set up the startcommand for the editor
                pList = [];
                p = new() { PName = "startcommand", token = new("open," + name) };
                pList.Add(p);


                switch (eCodes.SUBCMD.ToUpper())
                {
                    case "C":
                        // Look for the JAX Class editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.classeditor");
                        break;

                    case "V":
                        // Look for the JAX Class libary editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.libraryeditor");

                        if (answer.Element.Type.Equals("C") && File.Exists(answer.AsString()))
                        {
                            // TODO - Call the ClassLib editor
                        }
                        break;

                    case "P":
                        answer = await AppVars.GetVarToken("_JAX.programeditor");
                        name = fPath + fName + "." + fExt;

                        // Send the File parameter to the editor
                        pList = [];
                        p = new() { PName = "startcommand", token = new("open," + name) };
                        pList.Add(p);

                        if (File.Exists(answer.AsString()))
                        {
                            // TODO - Call this (assumed) JAXBase program
                        }
                        else
                        {
                            JAXObjectWrapper prgEditor = new(jbe.App, "jaxedit", "", pList);
                            await prgEditor.MethodCall("show");
                        }
                        break;

                    case "F":
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.programeditor");
                        fPath = JAXLib.JustFullPath(name);
                        fName = JAXLib.JustStem(name);
                        fExt = JAXLib.JustExt(name);

                        fPath = string.IsNullOrWhiteSpace(fPath) ? jbe.App.CurrentDS.JaxSettings.Default : fPath;

                        name = fPath + fName + "." + fExt;

                        if (File.Exists(answer.AsString()))
                        {
                            // TODO - Call this (assumed) JAXBase program
                        }
                        else
                        {
                            JAXObjectWrapper prgEditor = new(jbe.App, "jaxedit", "", pList);
                            await prgEditor.MethodCall("show");
                        }
                        break;

                    case "M":
                        // Look for the JAX Form Editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_formeditor");
                        break;

                    case "L":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.labeleditor");
                        break;

                    case "U":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.menueditor");
                        break;

                    case "J":
                        // Look for the JAX Project Editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.projecteditor");
                        break;

                    case "Q":
                        // Look for the JAX Query Editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.queryeditor");
                        break;

                    case "R":
                        // Look for the JAX Report Editor application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.reporteditor");
                        break;

                    case "S":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = jbe.App.JAXPrtObj.GetValue("_JAX.tableeditor");
                        break;

                    default:
                        throw new Exception($"1099||modify type {eCodes.SUBCMD.ToUpper()}");
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
         * MOUSE
         * 
         */
        public static string Mouse(AppClass app, string cmdRest)
        {
            string result = string.Empty;

            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


    }
}
