using JAXBase.Core;
using JAXBase.Utilities;
using Newtonsoft.Json.Linq;


namespace JAXBase.Math
{
    public class MathFuncsG
    {
        public static async Task<JAXObjects.Token> G(AppClass App, string _rpn, List<string> pop)
        {
            DateTime dtVal;
            string[] delimiters;
            string[] words;
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

            switch (_rpn)
            {
                case "`GETAUTOINCVALUE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETDATE":
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dtm) == false) dtm = DateTime.Now;
                        dtm = JAXLib.GetDate(dtm, stype1.Equals("D"));
                        tAnswer.Element.Value = stype1.Equals("D") ? DateOnly.FromDateTime(dtm) : dtm;
                    }
                    else if (string.IsNullOrWhiteSpace(stype1))
                        tAnswer.Element.Value = JAXLib.GetDate(DateTime.Now, false);
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`GETCP":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETDIR":
                    if (stype1.Equals("C"))
                    {
                        if ((stype2.Equals("C") || string.IsNullOrWhiteSpace(stype2)) == false) throw new Exception("11|");
                        if ((stype3.Equals("C") || string.IsNullOrWhiteSpace(stype3)) == false) throw new Exception("11|");
                        if ((stype4.Equals("N") || string.IsNullOrWhiteSpace(stype4)) == false) throw new Exception("11|");
                        if ((stype5.Equals("L") || string.IsNullOrWhiteSpace(stype5)) == false) throw new Exception("11|");

                        string cDir = string1.Trim();
                        string cText = string2.Trim();
                        string cCaption = string3.Trim();
                        int nFlags = intval4;
                        bool lRootOnly = string5.Equals(".T.");

                        tAnswer.Element.Value = JAXLib.GetDir(cDir, cText, cCaption, nFlags, lRootOnly);
                    }
                    else if (string.IsNullOrWhiteSpace(stype1))
                        tAnswer.Element.Value = JAXLib.GetDir(string.Empty, string.Empty, string.Empty, 0, false);
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETENV":
                    if (stype1.Equals("C"))
                        tAnswer.Element.Value = Environment.GetEnvironmentVariable(string1) ?? string.Empty;
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETFILE":
                    if (stype1.Equals("C") || string.IsNullOrWhiteSpace(stype1))
                    {
                        string[] s1 = string1.Split(':');

                        string fileType;
                        string fileExt;
                        if (s1.Length > 1)
                        {
                            fileType = s1[0].Trim();
                            fileExt = s1[1].Trim();
                        }
                        else
                        {
                            fileType = "Open File(s)";
                            fileExt = s1[0].Trim();
                        }

                        fileExt = string.IsNullOrWhiteSpace(fileExt) ? "*.*" : fileExt;

                        fileExt = fileExt.Replace(',', ';');

                        await App.utl.ShowOpenFileDialog(fileType, fileExt);
                        tAnswer.Element.Value = App.ReturnValue.AsString();
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETFLDSTATE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETFONT":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETNEXTMODIFIED":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETJSON":
                    if (stype1.Equals("C") && stype2.Equals("C"))
                    {
                        object? obj = VFPUtilities.GetJsonValue(string1, string2, "");

                        if (obj is null)
                            tAnswer._avalue[0].MakeNull();
                        else
                            tAnswer._avalue[0].Value = obj;
                    }

                    break;

                case "`GETOBJ":
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETPICT": // CHECK DOCS!
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETPRINTER":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GETWORDCOUNT":
                    string2 = string.IsNullOrEmpty(string2) ? " \t\n\r" : string2;
                    delimiters = new string[string2.Length];
                    for (int k = 0; k < string2.Length; k++) delimiters[k] = string2.Substring(k, 1);
                    words = string1.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    tAnswer._avalue[0].Value = words.Length;
                    break;

                case "`GETWORDNUM":                 // get x word from string1
                    string3 = string.IsNullOrEmpty(string3) ? " \t\n\r" : string3;
                    delimiters = new string[string3.Length];
                    for (int k = 0; k < string3.Length; k++) delimiters[k] = string3.Substring(k, 1);
                    words = string1.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    tAnswer._avalue[0].Value = JAXLib.Between(intval2, 1, words.Length) ? words[intval2 - 1] : string.Empty;
                    break;

                case "`GETCURSORADAPTER":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`GOMONTH":                    // Returns the day of month x number of months away respecting end of month
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = DateTime.MinValue;
                        else
                            tAnswer._avalue[0].Value = dtVal.AddMonths(intval2);
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "GUID":
                    tAnswer._avalue[0].Value = new Guid();
                    break;
            }

            return tAnswer;
        }
    }
}
