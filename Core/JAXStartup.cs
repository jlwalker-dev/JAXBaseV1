using JAXBase.Language;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace JAXBase.Core
{
    public class JAXStartup
    {
        public static void AppStartup(AppClass app)
        {
            // -------------------------------------------------------------------------
            // Create the Command List
            // -------------------------------------------------------------------------
            app.CmdList = [];

            for (int i = 0; i < JAXLanguageLists.JAXCommands.Length; i++)
                app.CmdList.Add(JAXLanguageLists.JAXCommands[i].ToLower());

            // -------------------------------------------------------------------------
            // Create the jump point markers
            // -------------------------------------------------------------------------
            if (app.MiscInfo.ContainsKey("endifcmd") == false)
            {
                int ibyte;
                string b64;

                ibyte = app.CmdList.IndexOf("case");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("casecmd", b64);

                ibyte = app.CmdList.IndexOf("otherwise");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("otherwisecmd", b64);

                ibyte = app.CmdList.IndexOf("endcase");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endcasecmd", b64);

                ibyte = app.CmdList.IndexOf("try");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("trycmd", b64);

                ibyte = app.CmdList.IndexOf("catch");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("catchcmd", b64);

                ibyte = app.CmdList.IndexOf("endtry");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endtrycmd", b64);

                ibyte = app.CmdList.IndexOf("finally");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("finallycmd", b64);

                ibyte = app.CmdList.IndexOf("do");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("docmd", b64);

                ibyte = app.CmdList.IndexOf("enddo");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("enddocmd", b64);

                ibyte = app.CmdList.IndexOf("until");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("untilcmd", b64);

                ibyte = app.CmdList.IndexOf("else");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("elsecmd", b64);

                ibyte = app.CmdList.IndexOf("elseif");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("elseifcmd", b64);

                ibyte = app.CmdList.IndexOf("endif");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endifcmd", b64);

                ibyte = app.CmdList.IndexOf("enddefine");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("enddefinecmd", b64);

                ibyte = app.CmdList.IndexOf("endfor");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endforcmd", b64);

                ibyte = app.CmdList.IndexOf("endprocedure");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endproccmd", b64);

                ibyte = app.CmdList.IndexOf("endscan");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("endscancmd", b64);

                ibyte = app.CmdList.IndexOf("for");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("forcmd", b64);

                ibyte = app.CmdList.IndexOf("procedure");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("procedurecmd", b64);

                ibyte = app.CmdList.IndexOf("nodefault");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("nodefaultcmd", AppClass.cmdByte.ToString()+b64+AppClass.cmdEnd.ToString());

                ibyte = app.CmdList.IndexOf("*sc");
                app.utl.Conv64(ibyte, 2, out b64);
                app.MiscInfo.Add("sourcecode", b64);

            }

            // -------------------------------------------------------------------------
            // Create the JAX paths
            // -------------------------------------------------------------------------
            app.JaxVariables._AppPath = AppContext.BaseDirectory;
            app.JaxVariables._BaseFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            app.JaxVariables._WorkPath = app.JaxVariables._BaseFolder + @"JAXBase\Work\";
            app.JaxVariables._TempPath = app.JaxVariables._BaseFolder + @"JAXBase\Temp\";
            app.JaxVariables._LogPath = app.JaxVariables._BaseFolder + @"JAXBase\Logs\";
            // -------------------------------------------------------------------------
            // Create the environment variables
            // -------------------------------------------------------------------------
            
        }
    }
}
