/******************************************************************************************************************************************
 ******************************************************************************************************************************************/
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;

namespace JAXBase.Math
{
    public class MathFuncsF
    {
        public static JAXObjects.Token F(AppClass App, string _rpn, List<string> pop)
        {
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
            if (double.TryParse(string2.Trim(), out double val3) == false) val3 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;
            int cwa;

            switch (_rpn)
            {
                case "`FCOUNT":                     // Number of fields in table
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.FieldCount;
                    break;

                case "`FDATE":
                    if (File.Exists(string1))
                        tAnswer.Element.Value = File.GetLastWriteTime(string1).Date;
                    else
                        AppErrorHandling.SetError(1, string1, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FIELD":                          // Get field name for field #
                    if (stype1.Equals("N"))
                    {
                        cwa = App.CurrentDS.CurrentWorkArea();

                        if (stype2.Equals("N"))
                        {
                            if (intval2 > 0)
                                App.CurrentDS.SelectWorkArea(intval2);
                        }
                        else if (stype2.Equals("C"))
                        {
                            if (string.IsNullOrWhiteSpace(string2))
                                App.CurrentDS.SelectWorkArea(string2);
                        }
                        else
                            AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                        if (JAXLib.Between(intval1, 1, App.CurrentDS.CurrentWA.DbfInfo.FieldCount))
                            tAnswer.Element.Value = App.CurrentDS.CurrentWA.DbfInfo.Fields[intval1].FieldName;
                        else
                            tAnswer.Element.Value = string.Empty;
                    }

                    break;

                case "`FILE":                           // File exists?
                    tAnswer._avalue[0].Value = (File.Exists(App.CurrentDS.JaxSettings.Default + string1) || File.Exists(string1));
                    break;

                case "`FILETOSTR":                      // Put file into string
                    tAnswer._avalue[0].Value = JAXLib.FileToStr(string1);
                    break;

                case "`FILTER":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FLOCK":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FLOOR":  // Get the next lowest int
                    if (stype1.Equals("N"))
                    {
                        intval1 = (int)val1 - (val1 < 0 ? 1 : (int)val1 == intval1 ? 1 : 0);
                        tAnswer._avalue[0].Value = intval1;
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FONTMETRIC":                         // https://learn.microsoft.com/en-us/dotnet/desktop/winforms/advanced/how-to-obtain-font-metrics?view=netframeworkdesktop-4.8
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FOR":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FORCEEXT":
                    tAnswer._avalue[0].Value = JAXLib.JustFullPath(string1) + JAXLib.JustStem(string1) + "." + string2;
                    break;

                case "`FORCEPATH":
                    tAnswer._avalue[0].Value = JAXLib.Addbs(string2) + JAXLib.JustFName(string1);
                    break;

                case "`FOUND":
                    cwa = App.CurrentDS.CurrentWorkArea();

                    if (stype1.Equals("N"))
                    {
                        if (intval1 > 0)
                            App.CurrentDS.SelectWorkArea(intval2);
                    }
                    else if (stype1.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string1) == false)
                            App.CurrentDS.SelectWorkArea(string1);
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    tAnswer.Element.Value = App.CurrentDS.CurrentWA.DbfInfo.Found;
                    App.CurrentDS.SelectWorkArea(cwa);
                    break;

                case "`FSIZE":
                    // --------------------------------------------------------------------------------- TODO
                    try
                    {
                        // TODO If table/field name
                        if (false)
                        {
                            tAnswer._avalue[0].Value = 0;
                        }
                        else
                        {
                            // If file name
                            long length = new System.IO.FileInfo(string1).Length;
                            tAnswer._avalue[0].Value = length;
                        }
                    }
                    catch (Exception e)
                    {
                        AppErrorHandling.SetError(9999, e.Message, "MathFuncsF.FSize");
                        tAnswer._avalue[0].Value = 0;
                    }
                    break;

                case "`FTIME":
                    if (File.Exists(string1))
                        tAnswer.Element.Value = File.GetLastWriteTime(string1).ToString("HH:mm:ss");
                    else
                        AppErrorHandling.SetError(1, string1, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`FULLPATH":   // TODO - parameter 2 holds logical - if .T., search subfolders
                    if (stype1.Equals("C"))
                    {
                        string file = AppHelper.FindPathForFile(string1);
                        if (string.IsNullOrEmpty(file))
                            tAnswer._avalue[0].Value = Program.CurrentApp.CurrentDS.JaxSettings.Default + string1;
                        else
                            tAnswer._avalue[0].Value = file;
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`FV":
                    if ((stype1 + stype2 + stype3).Equals("NNN"))
                    {
                        if (val3 == 0)
                            tAnswer.Element.Value = 0D;
                        else if (val2 == 0.0)
                            tAnswer.Element.Value = val1 * val3;
                        else
                        {

                            // VFP formula: FV = payment * ((1 + r)^n - 1) / r
                            double r = val2;
                            double power = System.Math.Pow(1.0 + r, val3);
                            double fv = val1 * (power - 1.0) / r;

                            // Round to the current set decimals places
                            tAnswer.Element.Value = System.Math.Round(fv, App.CurrentDS.JaxSettings.Decimals);
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string1, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                default:
                    throw new Exception("1999|" + _rpn);
            }

            return tAnswer;
        }
    }
}
