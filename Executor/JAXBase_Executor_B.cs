using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Math;
using JAXBase.XBase;

namespace JAXBase.Executor
{
    public class JAXBase_Executor_B
    {

        /* TODO
         * 
         * BEGIN [TRANSACTION]
         * 
         */
        public static string Begin(string cmdLine)
        {
            AppErrorHandling.ClearErrors();
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


        /*
         * 
         * BLANK [FIELDS FieldList] [Scope] [FOR lExpression1] [WHILE lExpression2] [IN nWorkArea | cTableAlias] [SESSION nSession]
         * 
         */
        public static async Task<string> Blank(ExecutorCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
            string result = string.Empty;

            try
            {
                string ForExpr = eCodes.ForExpr;
                string WhileExpr = eCodes.WhileExpr;
                List<int> Fields = [];

                JAXObjects.Token answer = new();

                // ---------------------------------------------------------------------
                // Extract the field names from the fields list
                // ---------------------------------------------------------------------
                for (int i = 0; i < eCodes.Fields.Count; i++)
                {
                    Fields.Add(0);

                    for (int j = 0; j < Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields.Count; j++)
                    {
                        if (eCodes.Fields[i].Name.Equals(Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Fields[j].FieldName, StringComparison.OrdinalIgnoreCase))
                        {
                            Fields[^1] = j;
                            break;
                        }
                    }

                    if (Fields[^1] < 1)
                        throw new Exception($"4012|{eCodes.Fields[i]}|Blank");
                }

                // ---------------------------------------------------------------------
                // Current datasession and workarea
                // ---------------------------------------------------------------------
                int session = Program.CurrentApp.CurrentDataSession;
                int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

                // ---------------------------------------------------------------------
                // Go to the desired session
                // ---------------------------------------------------------------------
                if (eCodes.SESSION > 0)
                    Program.CurrentApp.SetDataSession(eCodes.SESSION);

                // ---------------------------------------------------------------------
                // Go to the desired workarea
                // ---------------------------------------------------------------------
                answer = new();
                if (string.IsNullOrWhiteSpace(eCodes.InExpr))
                    answer.Element.Value = wa;
                else
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);

                if (answer.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(answer.AsInt());
                else if (answer.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(answer.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (Program.CurrentApp.CurrentDS.CurrentWA is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", Program.CurrentApp.CurrentDS.CurrentWorkArea()));

                // ---------------------------------------------------------------------
                // Prep record position and set the scope
                // ---------------------------------------------------------------------
                JAXDirectDBF Table = Program.CurrentApp.CurrentDS.CurrentWA;
                JAXScope jaxScope = new();
                await jaxScope.Setup(eCodes.Scope, Table, true);

                // ---------------------------------------------------------------------
                // first record is already in the buffer and continue working on
                // records until we reach EOF.  We use goto top/bottom and skip
                // because we want to play nice with indexes
                // ---------------------------------------------------------------------
                while (Table.DbfInfo.DBFEOF == false && Table.DbfInfo.RecCount > 0)
                {
                    // The For expression says if we use this record
                    JAXObjects.Token temp = await Program.CurrentApp.SolveFromRPNString(ForExpr);
                    if (ForExpr.Length == 0 || temp.Element.ValueAsBool)
                    {
                        // No FOR or it evaluated to true
                        // Now check the WHILE which says if we continue to process records
                        temp = await Program.CurrentApp.SolveFromRPNString(WhileExpr);
                        if (WhileExpr.Length == 0 || temp.Element.ValueAsBool)
                        {
                            // Blank out these fields
                            if (Fields.Count==0 || (Fields.Count==1 && Fields[0].Equals("*")))
                            {
                                // Blank all fields
                                await Table.DBFWriteRecord(Table.DbfInfo.EmptyRow.Rows[0],false);
                            }
                            else
                            {
                                // Blank only the specified fields
                                for (int i = 0; i < Fields.Count; i++)
                                {
                                    JAXObjects.Token value = new();
                                    value.Element.Value = Table.DbfInfo.Fields[Fields[i]].FieldType switch
                                    {
                                        "C" => "",
                                        "V" => "",
                                        "D" => DateOnly.MinValue,
                                        "L" => false,
                                        "T" => DateTime.MinValue,
                                        "N" => 0.00,
                                        "B" => 0.00D,
                                        "F" => 0.00F,
                                        "Y" => 0.00F,
                                        "M" => "",
                                        "G" => "",
                                        "W" => "",
                                        _ => throw new Exception($"Unsupported field type {Table.DbfInfo.Fields[Fields[i]].FieldType} for field {Table.DbfInfo.Fields[Fields[i]].FieldName}")
                                    };

                                    await Table.DBFReplaceField(Table.DbfInfo.Fields[Fields[i]].FieldName,value, false);
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
                // Make sure we get back to starting workarea
                // ---------------------------------------------------------------------
                Program.CurrentApp.SetDataSession(session);
                Program.CurrentApp.CurrentDS.SelectWorkArea(wa);
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
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
        public static async Task<string> Browse(ExecutorCodes eCodes)
        {
            AppErrorHandling.ClearErrors();
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
                JAXObjectWrapper jow = new(Program.CurrentApp, "browser", NameExpr, null);
                NameExpr = AppHelper.RegisterObject("browser", "browser");

                await jow.SetProperty("height", HeightExpr);
                await jow.SetProperty("width", WidthExpr);

                JAXObjects.Token bwin = new();
                bwin.Element.Value = jow;
                AppVars.SetVarOrMakePrivate(NameExpr, bwin);
                await jow.MethodCall("show");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        /* TODO
         * 
         * BUILD
         * 
         */
        public static string Build(string cmdLine)
        {
            AppErrorHandling.ClearErrors();
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
