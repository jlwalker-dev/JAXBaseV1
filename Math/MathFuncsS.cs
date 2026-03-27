/*
 * 2025.03.29 - JLW
 *      I'm thnking of stripping the VFP SYS() functions from the system and
 *      replace them with a SYS.PRG included that handles the VFP functionality
 *      along with adding some new commands/functions that handle the internal
 *      needs for printing, table manipulation, etc.
 *
 *      I think reports will be generated with 100% JAX code, included with the system.
 *      Form, menu, table, database, and report creation apps will be 100% PRG source.
 *      Printer control will probably be 100% JxBase PRG code using new commands, 
 *      system variables, and functions.
 *      
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Executer;
using JAXBase.XBase;
using JAXBase.Utilities.Utilities;

namespace JAXBase.Math
{
    internal class MathFuncsS
    {
        public static async Task<JAXObjects.Token> S(AppClass App, string _rpn, List<string> pop)
        {
            DateTime dtVal;
            JAXObjects.Token tAnswer = new();
            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;
            if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;

            switch (_rpn)
            {
                case "`SAVEPICTURE":        // TODO NOW
                    // --------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SEC":
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = 0;
                        else
                            tAnswer._avalue[0].Value = dtVal.Second;
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SECONDS":                                    // Since midnight
                    tAnswer._avalue[0].Value = (DateTime.Now.Hour * 3600 + +DateTime.Now.Minute * 60 + DateTime.Now.Second) + DateTime.Now.Millisecond / 1000D;
                    break;

                case "`SEEK":
                    // SEEK(eExpression [, nWorkArea | cTableAlias [, nIndexNumber | cIDXIndexFileName]])
                    int wa = App.CurrentDS.CurrentWorkArea();
                    int idx = -1;

                    if (string.IsNullOrEmpty(string2) == false)
                    {
                        if (stype2.Equals("N"))
                        {
                            if (intval2 < 0)
                                throw new Exception("3021|");
                            else if (intval2 > 0)
                                App.CurrentDS.SelectWorkArea(intval2);
                        }
                        else if (stype2.Equals("C"))
                        {
                            if (string.IsNullOrWhiteSpace(string2) == false)
                                App.CurrentDS.SelectWorkArea(string2);
                        }
                        else
                            throw new Exception("11|");
                    }

                    // Set the IDX to use
                    if (string.IsNullOrEmpty(string3) == false)
                    {
                        if (intval3 > 0)
                            idx = intval3;
                        else
                        {
                            if (string.IsNullOrWhiteSpace(string3) == false)
                            {
                                List<JAXDirectDBF.IDXInfo> list = await App.CurrentDS.CurrentWA.IDXGetInfoList(string2, string.Empty);
                                if (list.Count == 0) throw new Exception("1683||Cannot find index " + string2);
                                idx = list[0].IDXListPos;
                            }
                        }
                    }
                    else    
                        idx = App.CurrentDS.CurrentWA.DbfInfo.ControllingIDX;

                    if (idx < 0)
                        throw new Exception("1683``|");

                    switch (stype1)
                    {
                        case "N":
                            tAnswer.Element.Value = val1;
                            break;

                        case "D":
                            if (DateOnly.TryParse(string1, out DateOnly dv1)) dv1 = DateOnly.MinValue;
                            tAnswer.Element.Value = dv1;
                            break;

                        case "T":
                            if (DateTime.TryParse(string1, out DateTime dt1)) dt1 = DateTime.MinValue;
                            tAnswer.Element.Value = dt1;
                            break;

                        case "L":
                            tAnswer.Element.Value = string1.Contains('T');
                            break;

                        default:
                            tAnswer.Element.Value = string1;
                            break;
                    }

                    JAXDirectDBF.IDXCommand cmd = await App.CurrentDS.CurrentWA.IDXSearch(idx, tAnswer.Element.Value, 0, true, true);
                    tAnswer.Element.Value = cmd.Command == 1;
                    App.CurrentDS.SelectWorkArea(wa);
                    break;

                case "`SELECT":                         // if no parameter or 0, returns current workarea, if 1, returns highest unused
                    if (intval1 == 0)
                        tAnswer._avalue[0].Value = thisDS.CurrentWorkArea();
                    else
                        tAnswer._avalue[0].Value = App.jaxDataSession[App.CurrentDataSession].GetHighestOpenWorkArea();

                    break;

                case "`SET":                            // Get a setting value

                    if (string1.Equals("console", StringComparison.OrdinalIgnoreCase))
                    {
                        // TODO - WHAT AM I DOING HERE?
                        JAXObjectWrapper jow = new(App, "empty", string.Empty, []);

                        //if (App.JAXConsoles.ContainsKey(string2.ToLower()))
                        //{
                        //    jow.AddProperty("rows", new(), 1, string.Empty);
                        //    //jow.InTransaction = true;
                        //    jow.AddProperty("columns", new(), 1, string.Empty);
                        //    jow.AddProperty("name", new(), 1, string.Empty);
                        //    jow.AddProperty("visible", new(), 1, string.Empty);
                        //    jow.AddProperty("active", new(), 1, string.Empty);
                        //    jow.AddProperty("usecolors", new(), 1, string.Empty);
                        //    jow.AddProperty("foregroundcolor", new(), 1, string.Empty);
                        //    jow.AddProperty("backgroundcolor", new(), 1, string.Empty);
                        //    //jow.InTransaction = false;

                        //    JAXConsoleSettings con = App.JAXConsoles[string2.ToLower()].ReportSettings();
                        //    jow.SetProperty("rows", con.Rows);
                        //    jow.SetProperty("columns", con.Columns);
                        //    jow.SetProperty("name", string2.ToLower());
                        //    jow.SetProperty("visible", con.IsVisible);
                        //    jow.SetProperty("active", con.IsActive);
                        //    jow.SetProperty("usecolors", con.UseColors);
                        //    jow.SetProperty("foregroundcolor", con.ForegroundColor);
                        //    jow.SetProperty("backgroundcolor", con.BackgroundColor);
                        //}
                        //else
                        //    throw new Exception("4013|" + string2.ToLower());

                        tAnswer.Element.Value = jow;
                    }
                    else
                        tAnswer.Element.Value = JAXBase_Executer_Settings.GetSettings(App, string1, intval2).Element.Value;
                    break;

                case "`SETFLDSTATE":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SIGN":                            // Returns -1 if negative, 0 if zero, 1 if positive
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = val1.CompareTo(0);
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SIN":
                    if (stype1.Equals("N"))
                        tAnswer.Element.Value = System.Math.Sin(val1);
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SOUNDEX":
                    if (stype1.Equals("C"))
                        tAnswer.Element.Value = JAXUtilities.Soundex(string1);
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`SPACE": // Return x number of spaces
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = new string(' ', intval1);
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SQRT":                               // Square root
                    tAnswer._avalue[0].Value = System.Math.Sqrt(val1);
                    break;

                case "`STR":  // Convert from a number to a string
                    if ((stype1.Equals("N")))
                    {
                        if (pop.Count > 2)
                        {
                            tAnswer._avalue[0].Value = val1.ToString(new string('#', intval2) + "." + new string('#', intval3));
                        }
                        else if (pop.Count > 1)
                        {
                            tAnswer._avalue[0].Value = val1.ToString(new string('#', intval2));
                        }
                        else
                            tAnswer._avalue[0].Value = val1.ToString();
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`STRCONV":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`STREXTRACT": // Extract substrings using tokens
                    if ((stype1 + stype2 + stype3).Equals("CCC"))
                    {
                        if (int.TryParse(pop.Count > 3 ? pop[3][1..] : "0", out int intval4) == false) intval4 = 0;
                        if (int.TryParse(pop.Count > 4 ? pop[4][1..] : "0", out int intval5) == false) intval5 = 0; // TODO
                        tAnswer._avalue[0].Value = JAXLib.StrExtract(string1, string2, string3, intval4).Trim();
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`STRFORMAT":
                    //tAnswer.Element.Value = JAXUtilities.StrFormat(App, string1, JAXMathAux.ProcessPops(App, pop, 1).ToArray());
                    tAnswer.Element.Value = JAXUtilities.StrFormat(App, [..pop]);
                    break;

                case "`STRTOFILE":
                    if ("UX".Contains(stype1) || stype2.Equals("C") == false || (string.IsNullOrWhiteSpace(stype3) == false && "UXDTLC".Contains(stype3)))
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    else
                    {
                        string2 = JAXLib.FixFilePath(string2, App.CurrentDS.JaxSettings.Default);
                        string2 = AppHelper.FixFileCase(string.Empty, string2, App.CurrentDS.JaxSettings.Naming, App.CurrentDS.JaxSettings.NamingAll);
                        JAXLib.StrToFile(string1, string2, intval3);
                        tAnswer._avalue[0].Value = string1.Length;
                    }

                    break;

                case "`STRTRAN": // Search string1 for string2 [replace with string3] [at starting occurrence] [for number of occurences] [with flags]
                    if (true)   // protect the intval4 - 6 variables
                    {
                        if (int.TryParse(pop.Count > 3 ? pop[3][1..] : "0", out int intval4) == false) intval4 = 0;
                        if (int.TryParse(pop.Count > 4 ? pop[4][1..] : "0", out int intval5) == false) intval5 = 0;
                        if (int.TryParse(pop.Count > 5 ? pop[5][1..] : "0", out int intval6) == false) intval6 = 0;
                        tAnswer._avalue[0].Value = JAXLib.StrTran(string1, string2, string3, intval4, intval5, intval6);
                    }
                    break;

                case "`STUFF":  // TODO
                                // --------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`STUFFC":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SUBSTR":  // Get a substring from a string
                    if ((stype1 + stype2).Equals("CN"))
                    {
                        if (pop.Count > 2)
                        {
                            if (stype3.Equals("N") == false)
                                App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }
                        else
                            intval3 = string1.Length;

                        if (intval2 < 1 || intval2 > string1.Length || intval3 < 0)  // Invalid starting point
                            tAnswer._avalue[0].Value = string.Empty;
                        else if (intval2 + intval3 >= string1.Length)
                            tAnswer._avalue[0].Value = string1[(intval2 - 1)..]; // grab rest of string
                        else
                            tAnswer._avalue[0].Value = string1.Substring(intval2 - 1, intval3); // grab just part
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SUBSTRC":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`SYSID":
                    tAnswer.Element.Value = App.SystemCounter();
                    break;
            }

            return tAnswer;
        }
    }
}
