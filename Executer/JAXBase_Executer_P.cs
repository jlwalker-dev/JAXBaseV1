using JAXBase.Core;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public class JAXBase_Executer_P
    {

        /*
         * 
         * PACK
         * 
         */
        public static async Task<string> Pack( ExecuterCodes eCodes)
        {
            try
            {
                int wa = Program.CurrentApp.CurrentDS.CurrentWorkArea();

                // Go to the desired workarea
                JAXObjects.Token workarea = new();
                workarea.Element.Value = string.IsNullOrWhiteSpace(eCodes.InExpr) ? wa : Program.CurrentApp.SolveFromRPNString(eCodes.InExpr);
                if (workarea.Element.Type.Equals("N"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.AsInt());
                else if (workarea.Element.Type.Equals("C"))
                    Program.CurrentApp.CurrentDS.SelectWorkArea(workarea.Element.ValueAsString);
                else
                    throw new Exception("11|");

                if (Program.CurrentApp.CurrentDS.CurrentWA is null || Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                    throw new Exception(string.Format("52|{0}", Program.CurrentApp.CurrentDS.CurrentWorkArea()));

                // now pack if it's a table
                if (Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.TableType.Equals("T"))
                    await Program.CurrentApp.CurrentDS.CurrentWA.DBFPack();
                else
                    throw new Exception("1115|");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /*
         * 
         * PARAMETERS
         * 
         */
        public static async Task<string> Parameters(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            try
            {
                // Is this the first executed command of the program?
                if (Program.CurrentApp.AppLevels.Count == 0) throw new Exception("2|");
                if (JAXLib.InList(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LastCommand, -1, jbe.CmdNum["procedure"], jbe.CmdNum["*sc"]) == false) throw new Exception("8|");

                // Break out the var expressions
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                    AppVars.SetVarOrMakePrivate(var.varName, var.row, var.col, true);

                    string type = eCodes.As[i];

                    // Set the var as this type
                    if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                        await AppVars.SetAsType(var.varName, type);

                    if (Program.CurrentApp.ParameterClassList.Count > 0)
                    {
                        JAXObjects.Token tk = await AppHelper.GetParameterToken(null);
                        if (string.IsNullOrWhiteSpace(type) || tk.Element.Type.Equals(type))
                            AppVars.SetVar(var.varName, tk);
                        else
                            throw new Exception("1732|");
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            Program.CurrentApp.ParameterClassList.Clear();
            return string.Empty;
        }


        /* TODO
         * 
         * PLAY
         * 
         */
        public static string Play(string cmdRest)
        {
            try
            {
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /*
         * 
         * PRIVATE var1 [AS Type1][, var2 AS Type...]
         *
         */
        public static async Task<string> Private( ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                        AppVars.SetVarOrMakePrivate(var.varName, var.row, var.col, true);

                        string type = eCodes.As[i];

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false) 
                            await AppVars.SetAsType(var.varName, type);
                    }
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
         * PUBLIC var1 [AS Type1][, var2 AS Type...]
         *
         */
        public static async Task<string> Public(ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        VarRef var = await AppVars.SolveVariableReference(answer.AsString());
                        await AppVars.MakePublicVar(var.varName, var.row, var.col, true);

                        string type = eCodes.As[i];

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                            await AppVars.SetAsType(var.varName, type);

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                            await AppVars.SetAsType(var.varName, type);
                    }
                    else
                        throw new Exception("11|");
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
