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
        public static async Task<JAXObjects.Token> RawMath( string expression)
        {
            JAXMath jaxMath = new();
            GenericClass gc = await jaxMath.SolveMath(expression);
            return gc.Value;
        }



        /* TODO
         * 
         * MD
         * 
         */
        public static async Task<string> MD( ExecuterCodes eCodes)
        {
            string result = string.Empty;

            try
            {
                if (eCodes.Expressions.Count > 0)
                {
                    JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

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
        public static async Task<string> Modify( ExecuterCodes eCodes)
        {
            string result = string.Empty;
            string editor, fPath, fName, fExt, name;
            List<ParameterClass> pList;
            ParameterClass p;

            try
            {
                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);
                if (answer.Element.Type.Equals("C"))
                    name = answer.AsString();
                else
                    throw new Exception("11|");

                fPath = JAXLib.JustFullPath(name);
                fName = JAXLib.JustStem(name);
                fExt = JAXLib.JustExt(name);

                fPath = string.IsNullOrWhiteSpace(fPath) ? Program.CurrentApp.CurrentDS.JaxSettings.Default : fPath;

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
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.classeditor");
                        break;

                    case "V":
                        // Look for the JAX Class libary editor application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.libraryeditor");

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
                            JAXObjectWrapper prgEditor = new(Program.CurrentApp, "jaxedit", "", pList);
                            await prgEditor.MethodCall("show");
                        }
                        break;

                    case "F":
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.programeditor");
                        fPath = JAXLib.JustFullPath(name);
                        fName = JAXLib.JustStem(name);
                        fExt = JAXLib.JustExt(name);

                        fPath = string.IsNullOrWhiteSpace(fPath) ? Program.CurrentApp.CurrentDS.JaxSettings.Default : fPath;

                        name = fPath + fName + "." + fExt;

                        if (File.Exists(answer.AsString()))
                        {
                            // TODO - Call this (assumed) JAXBase program
                        }
                        else
                        {
                            JAXObjectWrapper prgEditor = new(Program.CurrentApp, "jaxedit", "", pList);
                            await prgEditor.MethodCall("show");
                        }
                        break;

                    case "M":
                        // Look for the JAX Form Editor application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_formeditor");
                        break;

                    case "L":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.labeleditor");
                        break;

                    case "U":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.menueditor");
                        break;

                    case "J":
                        // Look for the JAX Project Editor application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.projecteditor");
                        break;

                    case "Q":
                        // Look for the JAX Query Editor application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.queryeditor");
                        break;

                    case "R":
                        // Look for the JAX Report Editor application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.reporteditor");
                        break;

                    case "S":
                        // Look for the JAXTableDesigner application and
                        // load it if you find it, else give error
                        editor = Program.CurrentApp.JAXPrtObj.GetValue("_JAX.tableeditor");
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
        public static string Mouse(string cmdRest)
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
