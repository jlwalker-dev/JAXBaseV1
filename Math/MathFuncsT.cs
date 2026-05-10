using JAXBase.Core;
using NodaTime;
using JAXBase.Utilities;

namespace JAXBase.Math
{
    internal class MathFuncsT
    {
        public static async Task<JAXObjects.Token> T(AppClass App, string _rpn, List<string> pop)
        {
            bool done;
            JAXObjects.Token tAnswer = new();

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
            if (double.TryParse(string2.Trim(), out double val3) == false) val3 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;

            switch (_rpn)
            {
                case "`TAN":            // Tangent
                    if (stype1.Equals("N"))
                    {
                        intval2 = App.CurrentDS.JaxSettings.Decimals;
                        tAnswer.Element.Value = System.Math.Round(System.Math.Tan(val1),intval2);
                    }
                    else
                        throw new Exception("11|");

                    break;

                case "`TARGET":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;


                case "`TEXTMERGE":
                    JAXLib.TextMerge(string1, string.IsNullOrEmpty(string2) ? false : string2.Equals(".T."), string.IsNullOrEmpty(string3) ? "<<" : string3, pop.Count > 3 ? pop[3][1..] : ">>", App);
                    break;

                case "TEXTPOS@":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1096, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`TIME":                                   // Return current time as HH:mm:ss include .ff if an expression is sent
                    tAnswer.Element.Value = string.IsNullOrEmpty(string2) ? DateTime.Now.ToString("HH:mm:ss") : DateTime.Now.ToString("HH:mm:ss.ff");
                    break;

                case "`TIMEZONE":
                    if (stype1.Equals("N") || string.IsNullOrWhiteSpace(stype1))
                    {
                        switch (intval1)
                        {
                            case 0: // ID Name
                                tAnswer.Element.Value = App.TimeZone.Id;
                                break;

                            case 1: // Offset
                                Instant now = SystemClock.Instance.GetCurrentInstant();
                                Offset offset = App.TimeZone.GetUtcOffset(now);
                                tAnswer.Element.Value = offset.Seconds;
                                break;

                            default:
                                break;
                        }
                    }
                    else
                        throw new Exception("11|");

                    break;

                case "`TOSEC":  // Number of seconds for this datetime value since the midnight on that day
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dtm))
                        {
                            TimeSpan t = dtm - dtm.Date;
                            tAnswer.Element.Value = (int)t.TotalSeconds;
                        }
                        else
                            tAnswer.Element.Value = 0;
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`TRANSFORM":  // Transform a value to a string using foxpro format codes
                    tAnswer.Element.Value = Transform(App, pop);
                    break;

                case "`TRIM": // Trim character off back of string
                    done = false;
                    pop[0] = " ";

                    while (!done && string1.Length > 0)
                    {
                        string2 = string1;
                        for (int i = 0; i < pop.Count; i++)
                            string1 = string1.TrimEnd(pop[i][0]);

                        done = string2.Equals(string1);
                    }

                    tAnswer.Element.Value = string1;
                    break;

                case "`TTOC": // TODO - Return a string in format MM/dd/yyyy hh:mm:ss
                    tAnswer.Element.Value = TTOC(pop);
                    break;

                case "`TTOD": // Get the date from the datetime / string in format yyyyMMddT00:00:00
                    if (stype1.Equals("T"))
                    {
                        if (DateTime.TryParse(string1, out DateTime dtVal) == false) dtVal = DateTime.MinValue;
                        tAnswer.Element.Value = dtVal.AddSeconds(-dtVal.Hour * 3600 - dtVal.Minute * 60 - dtVal.Second);
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`TXNLEVEL":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`TXTWIDTH":  // TODO NOW
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`TYPE":                           // Get the "type" of what's in the string
                    tAnswer.Element.Value =await JAXType(App, pop);
                    break;

                default:
                    throw new Exception("1999|" + _rpn[1..]);
            }

            return tAnswer;
        }

        public static string Transform(AppClass app, List<string> pop)
        {
            string result = string.Empty;

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;

            // TODO - fix this, but for now it's either 0 or default decimals
            int dec = string1.Contains('.') ? app.CurrentDS.JaxSettings.Decimals : 0;

            if (string.IsNullOrEmpty(stype2))
            {
                // What are we converting?
                switch (stype1)
                {
                    case "N":   // Number
                        if (dec == 0)
                            string2 = "9";
                        else
                            string2 = "9." + new string('9', dec);
                        break;

                    case "D":   // Date only
                        string2 = "MM/dd/yyyy";
                        break;

                    case "T":   // Datetime
                        string2 = "MM/dd/yyyy hh:mm:ss";
                        break;

                    case "U":
                        break;

                    case "X":
                        break;

                    default:
                        break;
                }
            }

            // Now perform the transformation
            switch (stype1)
            {
                case "N":   // Number
                    result = JAXLib.JAXFormatToNet_ConvertNumber(val1, string2);
                    break;

                case "C":   // String
                    if (string.IsNullOrEmpty(string2))
                        result = string1;
                    else
                        result = JAXLib.JAXFormatToNet_ConvertString(string1, string2);
                    break;

                case "L":   // Logical
                    if (string2.Contains("@Y"))
                        string1 = string1.Equals(".T.") ? "Y" : "N";

                    result = string1;
                    break;

                case "D":   // Date only
                case "T":   // Datetime
                    if (DateTime.TryParse(string1, out DateTime dtVal) == false)
                        dtVal = DateTime.MinValue;

                    result = dtVal.ToString(string2);
                    break;
            }

            return result;
        }

        public static string TTOC(List<string> pop)
        {
            string result = string.Empty;

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);

            if (int.TryParse(string2, out int intval2) == false) intval2 = 0;

            if (stype1.Equals("T") && (string.IsNullOrEmpty(stype2) || stype2.Equals("N")))
            {
                result = "C" + intval2 switch
                {
                    3 => string1,
                    // am/pm time TODO- set hours observance needed
                    2 => string1[12..],
                    // As is
                    _ => string1.Replace("T", "").Replace(":", "").Replace(" ", ""),
                };
            }

            return result;
        }

        public static async Task<string> JAXType(AppClass App, List<string> pop)
        {
            string result;

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);

            if (int.TryParse(string2, out int intval2) == false) intval2 = 0;

            try
            {
                if (intval2 == 1)
                {
                    // Is the var an array, collecton, or unknown?
                    JAXObjects.Token tkA = await AppVars.GetVarToken(string1);
                    if (tkA.TType == "A")
                        result = "A";
                    else if (tkA.TType == "O")      // We really only support collections
                        result = "C";
                    else
                        result = "U";
                }
                else
                {
                    // Create a new Math process
                    JAXMath Math2 = new();

                    // Get the simple type of the result
                    JAXBase.XBase.GenericClass gc = await Math2.SolveMath(string1);
                    result = gc.Value.Element.Type;
                }
            }
            catch
            {
                // Some sort of error occured so it's definitely unknown
                result = "U";
            }

            return result;
        }
    }
}
