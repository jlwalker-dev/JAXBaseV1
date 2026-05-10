using JAXBase.Core;
using JAXBase.Data;
using JAXBase.XBase;
using JAXBase.Utilities;
using System.Globalization;

namespace JAXBase.Math
{
    public class MathFuncsD
    {
        public static async Task<JAXObjects.Token> D(AppClass App, string _rpn, List<string> pop)
        {
            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            DateTime dtVal;
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
                case "`DATE":       // TODO NOW - fix this up so BUILDDATE is not needed
                    tAnswer._avalue[0].Value = DateOnly.FromDateTime(DateTime.Now);
                    break;

                case "`DATETIME":  // TODO NOW
                    tAnswer._avalue[0].Value = DateTime.Now;
                    break;

                case "`DAY":                        // Day of month
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = 0;
                        else
                            tAnswer._avalue[0].Value = dtVal.Day.ToString();
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DBC":
                    tAnswer._avalue[0].Value = thisDS.CurrentDatabase.Equals("*default", StringComparison.OrdinalIgnoreCase) ? string.Empty : thisDS.CurrentDatabase;
                    break;

                case "`DBF":
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.FQFN;
                    break;

                case "`DBGETPROP":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DBSETPROP":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DBUSED":                         // TODO - Is this database open?
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`DEFAULTEXT":                     // Make sure a file has an extension as given if it doesn't already
                    string path = JAXLib.JustFullPath(string1);
                    string stem = JAXLib.JustStem(string1);
                    string ext = JAXLib.JustExt(string1);
                    ext = string.IsNullOrWhiteSpace(ext) ? string2 : ext;
                    ext = string.IsNullOrWhiteSpace(ext) ? string.Empty : "." + ext;

                    tAnswer._avalue[0].Value = path + stem + ext;
                    break;

                case "`DELETED":
                    JAXDirectDBF.DBFInfo DbfInfo = thisDS.CurrentWA.DbfInfo;
                    tAnswer._avalue[0].Value = (bool)DbfInfo.CurrentRow.Rows[DbfInfo.CurrentRecNo - 1][0];
                    break;

                case "`DESCENDING":
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DIFFERENCE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DIRECTORY":  // TODO NOW
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DISKSPACE":
                    DriveInfo[] allDrives = DriveInfo.GetDrives();

                    foreach (DriveInfo d in allDrives)
                    {
                        if (d.IsReady && d.Name.Equals(string1))
                        {
                            tAnswer._avalue[0].Value = d.TotalFreeSpace;
                            break;
                        }
                    }
                    break;

                case "DOY":
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dt) == false) dt = DateTime.MinValue;

                        if (dt.Year < 1752)
                            tAnswer.Element.Value = 0;
                        else
                            tAnswer.Element.Value = dt.DayOfYear;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`DMY":                    // Returns date as dd MMM YYYY
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = string.Empty;
                        else
                            tAnswer._avalue[0].Value = dtVal.ToString("dd MMMM yyyy", CultureInfo.CreateSpecificCulture("en-US"));
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DODEFAULT":
                    JAXObjectWrapper? wrapper = App.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObject;

                    if (wrapper is not null)
                    {
                        await wrapper.MethodCall(App.AppLevels[Program.CurrentApp.CurrentAppLevel].ThisObjectMethod);
                    }
                    else
                    {
                        // TODO - ERROR!
                    }
                    break;

                case "`DOW":                    // Day of week
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = 0;
                        else
                            tAnswer._avalue[0].Value = dtVal.DayOfWeek;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DRIVETYPE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DTOC":                   // TODO - Convert date to string MM/dd/yyyy
                    if ("DT".Contains(stype1))
                        tAnswer._avalue[0].Value = string.Concat("C", string1.AsSpan(1, 10));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DTOR":                       // Degrees to Radians
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = (System.Math.PI / 180) * val1;
                    break;

                case "`DTOS":                       // Convert date to string format YYYYMMDD
                    if ("DT".Contains(stype1))
                        tAnswer._avalue[0].Value = string1[..10].Replace("-", "");
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`DTOT":                       // Convert a date to a date/time
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out dtVal) == false) dtVal = DateTime.MinValue;
                        tAnswer._avalue[0].Value = dtVal;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`EMPTY":                      // Is the value empty string or 0
                    if (string1.Trim().Length == 0 ||
                        (stype1.Equals("N") && val1 == 0) ||
                        ("DT".Contains(stype1) && string1.Equals(DateTime.MinValue.ToString("yyyy-MM-ddT00:00:00"))) ||
                        (stype1.Equals("L") && string1.Equals(".F.")))
                        tAnswer._avalue[0].Value = true;
                    else
                        tAnswer._avalue[0].Value = false;
                    break;

                case "`EOF":
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.DBFEOF;
                    break;

                case "`ERROR":
                    tAnswer._avalue[0].Value = AppErrorHandling.LastErrorNo();
                    break;

                case "`EVALUATE":                   // Evaluate string
                    JAXMath jxm = new();
                    GenericClass gc = await jxm.SolveMath(string1);
                    if (gc.Value.TType.Equals("A"))
                    {
                        // we're returning an array
                        tAnswer.SetDimension(gc.Value.Row, gc.Value.Col, true);
                        for (int i = 0; i < gc.Value._avalue.Count; i++)
                            tAnswer._avalue[i].Value = gc.Value._avalue[i].Value;
                    }
                    else
                        tAnswer.Element.Value = gc.Value.Element.Value;
                    break;

                case "`EVL":                        // return non-empty value from 2 values
                    switch (stype1)
                    {
                        case "C":
                            tAnswer._avalue[0].Value = string.IsNullOrEmpty(string1) ? string2 : string1;
                            break;

                        case "N":
                            tAnswer._avalue[0].Value = val1 == 0 ? val2 : val1;
                            break;

                        case "L":
                            tAnswer._avalue[0].Value = string.IsNullOrEmpty(string1) || string1.Equals(".F.") ? string2 == ".T." : string1 == ".T.";
                            break;

                        case "D":
                        case "T":
                            if (DateTime.TryParse(string1, out dtVal) == false) dtVal = DateTime.MinValue;
                            if (DateTime.TryParse(string1, out DateTime dtVal2) == false) dtVal2 = DateTime.MinValue;
                            tAnswer._avalue[0].Value = dtVal == DateTime.MinValue ? dtVal2 : dtVal;
                            break;

                        default:
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            break;
                    }
                    break;

                case "`EXECSCRIPT":
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`EXP":
                    tAnswer.Element.Value = System.Math.Exp(val1);
                    break;
            }

            return tAnswer;
        }
    }
}
