using JAXBase.Core;
using JAXBase.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.Math
{
    public class MathFuncsR
    {
        private static Random rnd = new();

        public static async Task<JAXObjects.Token> R(AppClass App, string _rpn, List<string> pop)
        {
            bool done;
            JAXObjects.Token tAnswer = new();
            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);
            string stype4 = (pop.Count > 3 ? pop[3][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);
            string string4 = (pop.Count > 3 ? pop[3][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;
            if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;
            if (double.TryParse(string3.Trim(), out double val4) == false) val4 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;
            int intval4 = (int)val4;

            switch (_rpn)
            {
                case "`RAND":
                    if (string.IsNullOrEmpty(string1) == false)
                        rnd = new(intval1);
                    tAnswer._avalue[0].Value = rnd.NextDouble();
                    break;

                case "`RAT":
                    if (stype1.Equals("C") && stype2.Equals("C"))
                    {
                        Match theMatch = Regex.Match(string2, string1, RegexOptions.RightToLeft);
                        tAnswer._avalue[0].Value = theMatch.Index + 1;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RATC":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RATLINE":
                    tAnswer.Element.Value = 0;
                    int occurence = 0;
                    StringComparison comp = StringComparison.Ordinal;

                    if (stype3.Equals("N"))
                        occurence = intval3;
                    else
                    {
                        if (string.IsNullOrWhiteSpace(stype3) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (stype4.Equals("N"))
                    {
                        comp = intval4 switch
                        {
                            0 => StringComparison.Ordinal,
                            _ => StringComparison.OrdinalIgnoreCase
                        };
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(stype3) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (stype1.Equals("C") && stype2.Equals("C"))
                    {
                        List<string> list = [];
                        list.Add(string1);
                        list = MathFuncsA.GetALinesList(App, []);
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            if (list[i].Contains(string2, comp))
                            {
                                if (occurence > 0)
                                    occurence--;
                                else
                                {
                                    tAnswer.Element.Value = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`RELATION":  // TODO
                    // ---------------------------------------------------------------
                    tAnswer._avalue[0].Value = string.Empty;
                    break;

                case "`RECCOUNT":
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.RecCount;
                    break;

                case "`RECNO":
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.RecNo;
                    break;

                case "`RECSIZE":
                    tAnswer._avalue[0].Value = thisDS.CurrentWA.DbfInfo.RecordLen;
                    break;

                case "`REMOVEPROPERTY":                         // object name, property name
                    if (stype1.Equals("_"))
                    {
                        // Is it an object?
                        JAXObjects.Token tk = await AppVars.GetVarToken(string1);
                        if (tk.TType.Equals("O"))
                        {
                            List<string> pop2 = await JAXMathAux.ProcessPops(App, pop, 2);
                            if (pop2.Count > 0 && pop2[0][0].Equals('C'))
                            {
                                tAnswer._avalue[0].Value = tk.RemoveElement(pop2[0][1..]);
                                AppVars.SetVar(string1, tk);
                            }
                            else
                                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }
                        else
                            AppErrorHandling.SetError(1924, string1.ToUpper(), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`REPLICATE":
                    if ((stype1 + stype2).Equals("CN"))
                    {

                        if (intval2 < 1)
                            tAnswer._avalue[0].Value = string.Empty;
                        else
                            tAnswer._avalue[0].Value = new StringBuilder().Insert(0, string1, intval2).ToString();
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`REQUERY":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RGB":
                    tAnswer._avalue[0].Value = intval3 * 65536 + intval2 * 256 + intval1;
                    break;

                case "`RIGHT":  // Return the right part of a string
                    if ((stype1 + stype2).Equals("CN"))
                    {
                        if (intval2 > string1.Length)
                            tAnswer._avalue[0].Value = string1;
                        else
                            tAnswer._avalue[0].Value = string1[^intval1..];
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RIGHTC":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RLOCK":
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ROLLBACK":
                    // ---------------------------------------------------------------
                    break;

                case "`ROUND":  // Round a number
                    if ((stype1 + stype2).Equals("NN"))
                    {
                        val1 = System.Math.Round(val1, intval2, MidpointRounding.AwayFromZero);
                        tAnswer._avalue[0].Value = val1;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`RTOD":
                    tAnswer._avalue[0].Value = 180 / System.Math.PI * val1;
                    break;

                case "`RTRIM":
                    done = false;
                    pop[0] = " ";

                    while (!done && string1.Length > 0)
                    {
                        string2 = string1;
                        for (int i = 0; i < pop.Count; i++)
                            string1 = string1.TrimEnd(pop[i][0]);

                        done = string2.Equals(string1);
                    }

                    tAnswer._avalue[0].Value = string1;
                    break;

                default:
                    throw new Exception("1999|" + _rpn[1..]);
            }

            return tAnswer;
        }
    }
}
