using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_B
    {

        /* TODO
         * 
         * BEGIN [TRANSACTION]
         * 
         */
        public static string Begin(AppClass app, string cmdLine)
        {
            app.ClearErrors();
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


        /* TODO NOW
         * 
         * BLANK [FIELDS FieldList] [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [SESSION nSession]
         * 
         */
        public static string Blank(AppClass app, string cmdLine)
        {
            app.ClearErrors();
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


        /* TODO
         * 
         * BROWSE [FIELDS FieldList] [TITLE cTitleText]
         *      SIZE height,width
         *      [LOCATION  UPPERLEFT | UPPERCENTER | UPPERRIGHT | LOWERLEFT | LOWERCENTER | LOWERRIGHT |CENTERLEFT | CENTER | CENTERRIGHT] 
         *      [NAME ObjectName] [FOR lExpression1 [REST]] [NOAPPEND] 
         *      [NOEDIT | NOMODIFY] [NOCAPTIONS] [NODELETE] [NOMENU] [NOOPTIMIZE] [NOREFRESH] [NORMAL] [NOWAIT] [NOSHOW]
         * 
         * 
         *  FieldList | TitleExpr | Height | Width | LocationExpr | NameExpr | ForExpr | Flags
         * 
         */
        public static async Task<string> Browse(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            jbe.App.ClearErrors();
            string result = string.Empty;

            try
            {
                string FieldList = string.Empty;
                string TitleExpr = string.Empty;
                int HeightExpr = 600;
                int WidthExpr = 800;
                string LocExpr = string.Empty;
                string NameExpr = string.Empty;
                string ForExpr = string.Empty;
                string Flags = string.Empty;

                JAXObjects.Token tok = new();
                // Break out the Flags
                bool Rest = Flags.Contains("R");
                bool NoAppend = Flags.Contains("A");
                bool NoModify = Flags.Contains("E");
                bool NoCaptions = Flags.Contains("D");
                bool NoDelete = Flags.Contains("D");
                bool NoMenu = Flags.Contains("M");
                bool NoOptimize = Flags.Contains("O");
                bool NoRefresh = Flags.Contains("F");
                bool Normal = Flags.Contains("N");
                bool NoWait = Flags.Contains("W");
                bool NoShow = Flags.Contains("S");

                // Now build the JAX BrowseWindow using these parameters
                JAXObjectWrapper jow = new(jbe.App, "browser", NameExpr, null);
                NameExpr = AppHelper.RegisterObject(jbe.App, "browser", "browser");

                await jow.SetProperty("height", HeightExpr);
                await jow.SetProperty("width", WidthExpr);

                JAXObjects.Token bwin = new();
                bwin.Element.Value = jow;
                jbe.App.SetVarOrMakePrivate(NameExpr, bwin);
                await jow.MethodCall("show");
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * BUILD
         * 
         */
        public static string Build(AppClass app, string cmdLine)
        {
            app.ClearErrors();
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

    }
}
