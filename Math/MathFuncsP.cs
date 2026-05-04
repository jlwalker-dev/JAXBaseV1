/******************************************************************************************************************************************
 * "O" and "P" functions
 * 
 * 2024.08.12
 *      Only a few functions are supported at this time (PADx, PROMPT, PROGRAM, PCOUNT, PI)
 *      
 ******************************************************************************************************************************************/
using JAXBase.Core;
using JAXBase.Data;
using System.Globalization;
using JAXBase.Utilities;
using static JAXBase.Core.AppClass;
using static System.Net.Mime.MediaTypeNames;

namespace JAXBase.Math
{
    internal class MathFuncsP
    {
        public static JAXObjects.Token P(AppClass App, string _rpn, List<string> pop)
        {
            JAXObjects.Token tAnswer = new();

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);
            string stype4 = (pop.Count > 3 ? pop[3][..1] : string.Empty);
            string stype5 = (pop.Count > 4 ? pop[4][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);
            string string4 = (pop.Count > 3 ? pop[3][1..] : string.Empty);
            string string5 = (pop.Count > 4 ? pop[4][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;
            if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;
            if (double.TryParse(string4.Trim(), out double val4) == false) val4 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;
            int intval4 = (int)val4;

            JAXDirectDBF.DBFInfo dbf;
            int cwa;

            switch (_rpn)
            {
                case "`OBJTOCLIENT@":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`OBJTOJSON@":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`OCCURS":
                    if (string1.Equals("C") && string2.Equals("C"))
                    {
                        int counter = 0;
                        int idx = 0;
                        while ((idx = string2.IndexOf(string1, idx)) >= 0)
                        {
                            idx++;
                            counter++;
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`OLDVAL":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ONKEY":  // TODO NOW
                    if (stype1.Equals("C"))
                    {
                        // TODO - Return the On key label setting for this key
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ORDER":
                    cwa = App.CurrentDS.CurrentWorkArea();

                    if (stype1.Equals("N"))
                    {
                        if (intval1 > 0)
                            App.CurrentDS.SelectWorkArea(intval1);
                    }
                    else if (stype2.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string1))
                            App.CurrentDS.SelectWorkArea(string1);
                    }
                    else
                        throw new Exception("11|");

                    if (App.CurrentDS.CurrentWA is not null && App.CurrentDS.CurrentWA.DbfInfo.DBFStream is not null)
                    {
                        dbf = App.CurrentDS.CurrentWA.DbfInfo;

                        int controllingIDX = dbf.ControllingIDX;

                        if (controllingIDX < 0)
                            tAnswer.Element.Value = intval2 < 2 ? string.Empty : intval2 < 3 ? 0 : false;
                        else
                        {
                            tAnswer.Element.Value = intval2 switch
                            {
                                1 => dbf.IDX[controllingIDX].FileName,
                                2 => controllingIDX,
                                3 => dbf.IDX[controllingIDX].Descending,
                                _ => dbf.IDX[controllingIDX].Name
                            };
                        }

                    }
                    break;

                case "`OS":
                    tAnswer.Element.Value = App.OS switch
                    {
                        OSType.Linux => "LINUX",
                        OSType.Mac => "MAC",
                        OSType.FreeBSD => "FREEBSD",
                        OSType.Windows => "WINDOWS",
                        _ => "UNKNOWN"
                    };
                    break;

                case "`PADC":
                    tAnswer._avalue[0].Value = PadC(App, pop);
                    break;

                case ("`PADL"): // Pad on left side
                    tAnswer._avalue[0].Value = PadL(App, pop);
                    break;

                case ("`PADR"): // Pad on right side - TODO support other characters than space
                    tAnswer._avalue[0].Value = PadR(App, pop);
                    break;

                case "`PARAMETERS":
                    tAnswer._avalue[0].Value = App.JAXSysObj.GetElement("parameters");
                    break;

                case "`PAYMENT":
                    double principal = val1;
                    double interestrate = val2;
                    double totalpayments = val3;

                    if (interestrate == 0D)
                        tAnswer.Element.Value = principal / totalpayments;
                    else
                    {
                        double numerator = interestrate * System.Math.Pow(1D + interestrate, totalpayments);
                        double denominator = System.Math.Pow(1D + interestrate, totalpayments) - 1D;
                        tAnswer.Element.Value = principal * (numerator / denominator);
                    }
                    break;

                case "`PCOUNT":
                    tAnswer._avalue[0].Value = App.JAXSysObj.GetElement("pcount");
                    break;

                case "`PEMSTATUS":   // TODO NOW???
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`PI":
                    tAnswer._avalue[0].Value = System.Math.PI;
                    break;

                case "PIXELPOS@":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1096, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`PROGRAM":        // TODO
                    if (intval1 > App.AppLevels.Count)
                        tAnswer._avalue[0].Value = string.Empty;            // Invalid value
                    else if (intval1 < 0)
                        tAnswer._avalue[0].Value = App.AppLevels.Count;     // Number of levels
                    else if (intval1 < 2)
                        tAnswer._avalue[0].Value = App.AppLevels[0];        // Master program
                    else
                        tAnswer._avalue[0].Value = App.AppLevels[^intval1]; // Program at this level
                    break;

                case "`PROPER":
                    if (stype1.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string1))
                            tAnswer.Element.Value = string.Empty;
                        else
                        {
                            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
                            tAnswer.Element.Value = textInfo.ToTitleCase(string1.ToLower());
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`PUTFILE":
                    if (stype1.Equals("C"))
                    {
                        if ((stype2.Equals("C") || string.IsNullOrWhiteSpace(stype2)) == false) throw new Exception("11|");
                        if ((stype3.Equals("C") || string.IsNullOrWhiteSpace(stype3)) == false) throw new Exception("11|");

                        string customText = string1.Trim();
                        string fileName = string2.Trim();
                        string fileExtensions = string3.Trim();

                        tAnswer.Element.Value = JAXLib.PutFile(customText, fileName, fileExtensions);
                    }
                    else if (string.IsNullOrWhiteSpace(stype1))
                        tAnswer.Element.Value = JAXLib.PutFile(string.Empty,string.Empty,string.Empty);
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`PUTJSON@":
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`PV":
                    double payment = val1;
                    double rate = val2;
                    double paymentnumber = val3;

                    if (rate == 0)
                        tAnswer.Element.Value = payment * paymentnumber;
                    else
                    {
                        double factor = 1D + rate;
                        tAnswer.Element.Value = payment * (1 - System.Math.Pow(factor, -paymentnumber)) / rate;
                    }
                    break;

                case "`QUARTER":
                    if ("DT".Contains(stype1))
                    {
                        if (string1.Length > 7)
                        {
                            string1 = string1.Substring(3, 2);
                            tAnswer.Element.Value = JAXLib.Between(string1, "01", "03") ? 1 : JAXLib.Between(string1, "04", "06") ? 2 : JAXLib.Between(string1, "07", "09") ? 3 : 4;
                        }
                        else
                            tAnswer.Element.Value = 0;
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                default:
                    throw new Exception("1999|" + _rpn[1..]);
            }

            return tAnswer;
        }

        public static string Pad(AppClass app)
        {
            string result = string.Empty;

            return result;
        }

        public static string PadC(AppClass App, List<string> pop)
        {
            string result = string.Empty;
            char char1 = ' ';

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            // translate into numbers for certain commands
            if (int.TryParse(string2.Trim(), out int intval2) == false) intval2 = 0;

            int intval1;

            if (pop.Count > 1)
            {
                if (stype1.Equals("C") && stype2.Equals("N"))
                {
                    if (pop.Count > 2)
                    {
                        if (stype3.Equals("C"))
                            char1 = string3[0];
                        else
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (string1.Length + 1 > intval2)
                        result = string1[..intval2];  // Doesn't need centering
                    else
                    {
                        intval1 = (intval2 - string1.Length) / 2 + string1.Length;
                        intval2 = intval2 - string1.Length - intval1;

                        if (intval2 < 1)
                            result = new string(char1, intval1) + string1;
                        else
                            result = new string(char1, intval1) + string1 + new string(char1, intval2);
                    }
                }
                else
                    AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            else
                AppErrorHandling.SetError(1229, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);


            return result;
        }

        public static string PadL(AppClass App, List<string> pop)
        {
            string result = string.Empty;
            char char1 = ' ';

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            // translate into numbers for certain commands
            if (int.TryParse(string2.Trim(), out int intval2) == false) intval2 = 0;

            if (pop.Count > 1)
            {
                if (stype1.Equals("C") && stype2.Equals("N"))
                {
                    if (pop.Count > 2)
                    {
                        if (stype3.Equals("C"))
                            char1 = string3[0];
                        else
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (string1.Length + 1 > intval2)
                        result = string1[..intval2];
                    else
                        result = string1.PadLeft(intval2, char1);
                }
                else
                    AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            else
                AppErrorHandling.SetError(1229, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);


            return result;
        }

        public static string PadR(AppClass App, List<string> pop)
        {
            string result = string.Empty;
            char char1 = ' ';

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            // translate into numbers for certain commands
            if (int.TryParse(string2.Trim(), out int intval2) == false) intval2 = 0;

            if (pop.Count > 1)
            {
                if (stype1.Equals("C") && stype2.Equals("N"))
                {
                    if (pop.Count > 2)
                    {
                        if (stype3.Equals("C"))
                            char1 = string3[0];
                        else
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (string1.Length + 1 > intval2)
                        result = string1[..intval2];
                    else
                        result = string1.PadRight(intval2, char1);
                }
                else
                    AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            else
                AppErrorHandling.SetError(1229, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            return result;
        }

    }
}
