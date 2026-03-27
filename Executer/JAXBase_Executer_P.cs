using JAXBase.Core;
using static JAXBase.Core.AppClass;
using JAXBase.Utilities.Utilities;
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
        public static async Task<string> Pack(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            try
            {
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

                // now pack if it's a table
                if (jbe.App.CurrentDS.CurrentWA.DbfInfo.TableType.Equals("T"))
                    await jbe.App.CurrentDS.CurrentWA.DBFPack();
                else
                    throw new Exception("1115|");
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
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
                if (jbe.App.AppLevels.Count == 0) throw new Exception("2|");
                if (JAXLib.InList(jbe.App.AppLevels[^1].LastCommand, -1, jbe.CmdNum["procedure"], jbe.CmdNum["*sc"]) == false) throw new Exception("8|");

                // Break out the var expressions
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    JAXObjects.Token answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    VarRef var = await jbe.App.SolveVariableReference(answer.AsString());
                    jbe.App.SetVarOrMakePrivate(var.varName, var.row, var.col, true);

                    string type = eCodes.As[i];

                    // Set the var as this type
                    if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                        await jbe.App.SetAsType(var.varName, type);

                    if (jbe.App.ParameterClassList.Count > 0)
                    {
                        JAXObjects.Token tk = await jbe.App.GetParameterToken(null);
                        if (string.IsNullOrWhiteSpace(type) || tk.Element.Type.Equals(type))
                            jbe.App.SetVar(var.varName, tk);
                        else
                            throw new Exception("1732|");
                    }
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            jbe.App.ParameterClassList.Clear();
            return string.Empty;
        }


        /* TODO
         * 
         * PLAY
         * 
         */
        public static string Play(AppClass app, string cmdRest)
        {
            try
            {
            }
            catch (Exception ex)
            {
                app.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return string.Empty;
        }


        /*
         * 
         * PRIVATE var1 [AS Type1][, var2 AS Type...]
         *
         */
        public static async Task<string> Private(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        VarRef var = await jbe.App.SolveVariableReference(answer.AsString());
                        jbe.App.SetVarOrMakePrivate(var.varName, var.row, var.col, true);

                        string type = eCodes.As[i];

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false) 
                            await jbe.App.SetAsType(var.varName, type);
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
         * PUBLIC var1 [AS Type1][, var2 AS Type...]
         *
         */
        public static async Task<string> Public(JAXBase_Executer jbe, ExecuterCodes eCodes)
        {
            JAXObjects.Token answer = new();
            string result = string.Empty;

            try
            {
                for (int i = 0; i < eCodes.Expressions.Count; i++)
                {
                    answer = await jbe.App.SolveFromRPNString(eCodes.Expressions[i].RNPExpr);

                    if (answer.Element.Type.Equals("C"))
                    {
                        VarRef var = await jbe.App.SolveVariableReference(answer.AsString());
                        await jbe.App.MakePublicVar(var.varName, var.row, var.col, true);

                        string type = eCodes.As[i];

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                            await jbe.App.SetAsType(var.varName, type);

                        // Set the var as this type
                        if (string.IsNullOrWhiteSpace(eCodes.As[i]) == false)
                            await jbe.App.SetAsType(var.varName, type);
                    }
                    else
                        throw new Exception("11|");
                }
            }
            catch (Exception ex)
            {
                jbe.App.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

    }
}
