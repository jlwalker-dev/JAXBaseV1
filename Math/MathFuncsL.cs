using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.Math
{
    public class MathFuncsL
    {
        public static JAXObjects.Token L(AppClass App, string _rpn, List<string> pop)
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
                case "`JSONTOCURSOR":
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`JSONTOOBJ":
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`JUSTDRIVE":          // TODO NOW
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`JUSTEXT":
                    tAnswer._avalue[0].Value = JAXLib.JustExt(string1);
                    break;

                case "`JUSTFNAME":
                    tAnswer._avalue[0].Value = JAXLib.JustFName(string1);
                    break;

                case "`JUSTPATH":
                    tAnswer._avalue[0].Value = JAXLib.JustPath(string1);
                    break;

                case "`JUSTSTEM":
                    tAnswer._avalue[0].Value = JAXLib.JustStem(string1);
                    break;

                case "`KBBUFFER":
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`KEY":
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LASTKEY":
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LEFT":   // Return the left part of a string
                    if (stype1.Equals("C") && stype2.Equals("N"))
                    {
                        if (val2 == 0)
                            tAnswer._avalue[0].Value = string.Empty;
                        else if (val2 > string1.Length)
                            tAnswer._avalue[0].Value = string1;
                        else
                            tAnswer._avalue[0].Value = string1[..intval2];
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LEFTC":
                    // ---------------------------------------------------------------------------------
                    break;

                case "`LEN":  // Return the length of a string
                    if (stype1.Equals("C"))
                    {
                        intval1 = string1.Length;
                        tAnswer._avalue[0].Value = intval1;
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LENC":
                    // ---------------------------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LIKE": // TODO NOW
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LIKEC":
                    // ---------------------------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LOADPICTURE": // TODO NOW
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LOCK8":
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LOG":
                    if (stype1.Equals("N"))
                        tAnswer.Element.Value = System.Math.Log(val1);
                    else
                        App.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LOG10":
                    if (stype1.Equals("N"))
                        tAnswer.Element.Value = System.Math.Log10(val1);
                    else
                        App.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LOWER":  // Convert a string to lower case
                    if (stype1.Equals("C"))
                        tAnswer._avalue[0].Value = string1.ToLower();
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`LTRIM":
                    done = false;
                    pop[0] = " ";

                    while (!done && string1.Length > 0)
                    {
                        string2 = string1;
                        for (int i = 0; i < pop.Count; i++)
                            string1 = string1.TrimStart(pop[i][0]);

                        done = string2.Equals(string1);
                    }

                    tAnswer._avalue[0].Value = string1;
                    break;

                default:
                    throw new Exception("1999|" + _rpn);
            }

            return tAnswer;
        }
    }
}
