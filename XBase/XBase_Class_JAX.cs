using JAXBase.Core;
using JAXBase.Utilities;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;

namespace JAXBase.XBase
{
    public class XBase_Class_JAX : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "JAX";
        public new string MyDefaultName = "jax";
        public new bool Register = false;

        // This list holds the row source array followed by important related values
        public ObservableSortedDictionary<int, JAXObjects.Token> Screens = [];

        public XBase_Class_JAX(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, MyBaseClass, string.IsNullOrWhiteSpace(name) ? "_jax" : name, false, UserObject.URW);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            UserProperties["classid"].Element.Value = Program.CurrentApp.MyInstance;

            JAXObjects.Token tk = await AppVars.GetVarToken("_jax");

            if (tk.Element.Type.Equals("L"))
                UserProperties["name"].Protected = true;

            string path = Assembly.GetExecutingAssembly().Location;
            UserProperties["homefolder"].Element.Value = JAXLib.AddBackSlash(path);

            if (Directory.Exists(path + @"tools\"))
                await SetProperty("toolfolder", JAXLib.AddBackSlash(path) + @"tools\", 0);

            path = JAXLib.AddBackSlash(Program.CurrentApp.UserFolder);
            if (Directory.Exists(path + @"jaxbase") == false) Directory.CreateDirectory(path + @"jaxbase");
            if (Directory.Exists(path + @"jaxbase\temp") == false) Directory.CreateDirectory(path + @"jaxbase\temp\");
            if (Directory.Exists(path + @"jaxbase\work") == false) Directory.CreateDirectory(path + @"jaxbase\work\");

            await SetProperty("userfolder", path, 0);
            await SetProperty("tempfolder", path + @"jaxbase\temp\", 0);
            await SetProperty("workfolder", path + @"jaxbase\work\", 0);

            return result;
        }

        /* ------------------------------------------------------------------------------------------*
         * GetProperty
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {

                switch (propertyName)
                {
                    case "activeform":
                        returnToken = await AppVars.GetVarFromExpression("_screen.activeform", null);
                        break;

                    case "activemonitor":
                        returnToken.Element.MakeNull();
                        break;

                    case "config":
                        returnToken.Element.Value = File.Exists(Program.CurrentApp.ExeFolder + "jaxbase.ini") ? Program.CurrentApp.ExeFolder + "jaxbase.ini" : "";
                        break;

                    case "defaultpath":
                        returnToken.Element.Value = Program.CurrentApp.CurrentDS.JaxSettings.Default;
                        break;

                    case "fullname":
                        string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

                        if (!string.IsNullOrEmpty(exePath))
                        {
                            string fileName = Path.GetFileName(exePath);                    // e.g. MyApp.exe
                            string directory = Path.GetDirectoryName(exePath) ?? "";        // e.g. C:\Program Files\MyApp
                            returnToken.Element.Value = JAXLib.Addbs(directory) + fileName; // full path + filename
                        }
                        else
                            returnToken.Element.Value = exePath!;

                        break;

                    case "logfolder":
                        returnToken.Element.Value = JAXLib.JustFullPath(Program.CurrentApp.AppLogFile);
                        break;

                    case "pathlist":
                        returnToken.Element.Value = Program.CurrentApp.JaxVariables._LogPath;
                        break;

                    case "tempfolder":
                        returnToken.Element.Value = Program.CurrentApp.JaxVariables._TempPath;
                        break;

                    case "toolfolder":
                        returnToken.Element.Value = Program.CurrentApp.JaxVariables._ToolsPath;
                        break;

                    case "userfolder":
                        returnToken.Element.Value = Program.CurrentApp.UserFolder;
                        break;

                    case "version":
                        returnToken.Element.Value = Program.Version;
                        break;

                    case "workfolder":
                        returnToken.Element.Value = Program.CurrentApp.JaxVariables._WorkPath;
                        break;

                    case "x64":
                        returnToken.Element.Value = System.Environment.Is64BitProcess;
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /* ------------------------------------------------------------------------------------------*
         * SetProperty
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      9   - Success, do no further processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);
            string file = tk.AsString();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                {
                    result = 3026;
                }
                else
                {
                    switch (propertyName)
                    {
                        case "defaultpath":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.CurrentDS.JaxSettings.Default = file;
                            else
                                result = 11;
                            break;

                        case "logfolder":
                            if (tk.Element.Type.Equals("C"))
                            {
                                file = JAXLib.AddBackSlash(file) + JAXLib.JustFName(Program.CurrentApp.AppLogFile);

                                if (File.Exists(file))
                                    Program.CurrentApp.AppLogFile = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "pathlist":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.JaxVariables._LogPath = CheckFolder(file);
                            else
                                result = 11;
                            break;

                        case "tempfolder":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.JaxVariables._TempPath = CheckFolder(file);
                            else
                                result = 11;
                            break;

                        case "toolfolder":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.JaxVariables._ToolsPath = CheckFolder(file);
                            else
                                result = 11;
                            break;

                        case "userfolder":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.UserFolder = CheckFolder(file);
                            else
                                result = 11;
                            break;

                        case "workfolder":
                            if (tk.Element.Type.Equals("C"))
                                Program.CurrentApp.JaxVariables._WorkPath = CheckFolder(file);
                            else
                                result = 11;
                            break;

                        case "classeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._ClassEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "fileeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._EditPRG = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "formeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._FormEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "imageeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._ImageEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "labeleditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._LabelEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "libraryeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._ClassEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "menueditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._MenuEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "projecteditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._ProjectEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "programeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._PrgEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "queryeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._QueryEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "reporteditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._ReportEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "tableeditor":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (File.Exists(tk.AsString()))
                                    Program.CurrentApp.JaxVariables._TableEditor = file;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        default:
                            // skip everything else
                            result = 9;
                            break;
                    }

                    // Was the property retrieved?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }


        private string CheckFolder(string path)
        {
            if (Directory.Exists(path) == false)
            {
                int err = 0;
                string msg = "";

                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (ArgumentNullException ex) { err = 1886; msg = ex.Message; }
                catch (ArgumentException ex) { err = 202; msg = ex.Message; }
                catch (PathTooLongException ex) { err = 2022; msg = ex.Message; }
                catch (UnauthorizedAccessException ex) { err = 1705; msg = ex.Message; }
                catch (IOException ex) { err = 0; msg = ex.Message; }
                catch (Exception ex) { err = 9999; msg = ex.Message; }

                if (err > 0)
                    throw new Exception($"{err}||{msg}");
            }

            return path;
        }

        public override string[] JAXMethods()
        {
            return [];
        }

        public override string[] JAXEvents()
        {
            return [];
        }

        public override string[] JAXProperties()
        {
            return
            [
                "activeform,n!,0",
                "activemonitor,n!,0",
                "activeproject,o!,",
                "baseclass,C!,jax",
                "class,C!,screen",
                "classlibrary,C!,",
                "config,c!,",
                "defaulpath,c,",
                "fullname,c!,",
                "homefolder,c!,",
                "logfolder,c,",
                "name,c,JAX",
                "pathlist,c,",
                "tempfolder,c,",
                "toolfolder,c,",
                "userfolder,c,",
                "workfolder,c,",
                "version,n!,",
                "x64,l!,.F.",
                "classeditor,c,EDIT_CLX.APP",
                "fileeditor,c,",
                "formeditor,c,EDIT_SCX.APP",
                "imageeditor,c,EDIT_IMG.APP",
                "labeleditor,c,EDIT_LBX.APP",
                "libraryeditor,c,EDIT_VCX.APP",
                "menueditor,c,EDIT_MNU.APP",
                "projecteditor,c,EDIT_PJX.APP",
                "programeditor,c,",
                "queryeditor,c,EDIT_QPR.APP",
                "reporteditor,c,EDIT_RPX.APP",
                "tableditor,c,EDIT_DBF.APP"
            ];
        }
    }

}
