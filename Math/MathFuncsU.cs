using JAXBase.Core;
using JAXBase.Data;
using NodaTime;
using NodaTime.TimeZones;
using System.Data.Common;
using System.Globalization;

namespace JAXBase.Math
{
    public class MathFuncsU
    {
        public static JAXObjects.Token U(AppClass App, string _rpn, List<string> pop)
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

            switch (_rpn)
            {
                case "`UNBINDEVENTS":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`UPDATED":
                    // ---------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`UPPER":  // Upper case a string
                    tAnswer._avalue[0].Value = string1.ToUpper();
                    break;

                case "`USED":                               // Check current datasession for name/alias
                    tAnswer._avalue[0].Value = thisDS.TableUsed(string1)>0;
                    break;

                case "`VAL":  // Convert from string to number
                    if (stype1.Equals("C"))
                        tAnswer._avalue[0].Value = val1;
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`VARTYPE":
                    tAnswer._avalue[0].Value = stype1;
                    break;

                case "`WEEK":
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dtm))
                        {
                            Calendar cal = CultureInfo.InvariantCulture.Calendar;
                            tAnswer.Element.Value = cal.GetWeekOfYear(dtm, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
                        }
                        else
                            tAnswer.Element.Value = 0;
                    }
                    break;

                case "`XMLTOCURSOR":  // TODO
                    // --------------------------------------------------------------- 
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`YEAR":
                    if ("DT".Contains(stype1))
                    {
                        if (int.TryParse(string1[..4], out int iYear))
                            tAnswer._avalue[0].Value = iYear;
                        else
                            tAnswer._avalue[0].Value = 0;
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                default:
                    throw new Exception("1999|" + _rpn[1..]);
            }

            return tAnswer;
        }
    }
}
