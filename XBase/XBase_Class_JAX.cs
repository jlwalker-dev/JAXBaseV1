using JAXBase.Core;
using JAXBase.Utilities;
using System.Diagnostics;
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

            UserProperties["classid"].Element.Value = App.MyInstance;

            JAXObjects.Token tk = await AppVars.GetVarToken("_jax");
            
            if (tk.Element.Type.Equals("L"))
                UserProperties["name"].Protected = true;

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
                        returnToken.Element.Value = File.Exists(App.ExeFolder + "jaxbase.ini") ? App.ExeFolder + "jaxbase.ini" : "";
                        break;

                    case "defaultpath":
                        returnToken.Element.Value = App.CurrentDS.JaxSettings.Default;
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

                    case "pathlist":
                        returnToken.Element.Value = App.CurrentDS.JaxSettings.Path;
                        break;

                    case "tempfolder":
                        returnToken.Element.Value = App.JaxVariables._TempPath;
                        break;

                    case "toolfolder":
                        returnToken.Element.Value = App.JaxVariables._ToolsPath;
                        break;

                    case "version":
                        returnToken.Element.Value = Program.Version;
                        break;

                    case "workfolder":
                        returnToken.Element.Value = App.AppWorkFolder;
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
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
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
                        default:
                            // Process standard properties
                            result = 1;
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
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
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
                "defaulpath,c!,",
                "fullname,c!,",
                "name,c,JAX",
                "pathlist,c!,",
                "tempfolder,c!,",
                "toolfolder,c!,",
                "version,n!,",
                "workfolder,c!,",
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
