using JAXBase.Core;
using JAXBase.Utilities;
using System.Text;

namespace JAXBase.Math
{
    public class MathFuncsB
    {
        public static JAXObjects.Token B(AppClass App, string _rpn, List<string> pop)
        {
            JAXObjects.Token tAnswer = new();

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;

            switch (_rpn)
            {
                case "`BETWEEN":                           // Value between values
                    tAnswer._avalue[0].Value = Between(App, pop);
                    break;

                case "`BINDEVENT":  // Bind to an event
                    // ---------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BINTOC":     // Binary to Character
                    bool reverse = false;
                    byte[] rc = [];
                    string Field = string.Empty;

                    if (stype1.Equals("N"))
                    {
                        if (stype2.Equals("C"))
                        {
                            string2 = string2.Trim().ToUpper();

                            if (string2.Length == 1)
                                string2 += "I";

                            reverse = string2.Length > 2 && string2[2] == 'R';
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(stype2))
                                string2 = "4I";
                            else
                                AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }

                        switch (string2[0])
                        {
                            case '1':
                                if (string2[1] == 'I')
                                {
                                    if (JAXLib.Between(intval1, 0, 255))
                                    {
                                        rc = new byte[1];
                                        rc[0] = (byte)intval1;
                                    }

                                    else
                                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name); // TODO ***********
                                }
                                else
                                    AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name); // TODO ***********

                                break;

                            case '2':
                                if (string2[1] == 'I')
                                {
                                    if (JAXLib.Between(intval1, 0, 65535))
                                    {
                                        rc = new byte[2];
                                        rc[1] = (byte)(intval1 / 256);
                                        rc[0] = (byte)(intval1 % 256);
                                    }

                                    Field = App.utl.MKI(intval1);
                                    rc = Convert.FromBase64String(Field);
                                }
                                else
                                    AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name); // TODO ***********

                                break;

                            case '4':
                                if (string2[1] == 'I')
                                {
                                    Field = App.utl.MKI(intval1);
                                    rc = Convert.FromBase64String(Field);
                                }
                                else
                                {
                                    Field = App.utl.MKS(Convert.ToSingle(val2));
                                    rc = Convert.FromBase64String(Field);
                                }
                                break;

                            case '8':
                                if (string2[1] == 'I')
                                {
                                    long lval=(long)val1;
                                    Field = App.utl.MKD(Convert.ToDouble(lval));
                                    rc = Convert.FromBase64String(Field);
                                }
                                else
                                {
                                    Field = App.utl.MKD(val1);
                                    rc = Convert.FromBase64String(Field);
                                }
                                break;

                            default:
                                AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name); // TODO ***********
                                break;
                        }

                        if (rc.Length > 0)
                        {
                            if (reverse) rc= (byte[])rc.Reverse();
                            tAnswer.Element.Value = Encoding.ASCII.GetString(rc);
                        }
                        else
                            tAnswer.Element.Value = string.Empty;
                    }

                    break;

                case "`BITTEST":                            // Value of bit
                    if ((stype1 + stype2).Equals("NN"))
                        tAnswer._avalue[0].Value = (intval1 & (1 << intval2)) > 0;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITAND":                             // Bit And
                case "`BITCLEAR":                           // Bit to 0 
                case "`BITLSHIFT":                          // Bit left shift
                case "`BITOR":                              // Bit or
                case "`BITRSHIFT":                          // Bit right shift
                case "`BITSET":                             // Bit to 1
                case "`BITXOR":                             // Bit XOR
                    tAnswer._avalue[0].Value = BitMath(App, _rpn, stype1, stype2, intval1, intval2, pop);
                    break;

                case "`BOF":                                // Beginning of table flag
                    tAnswer._avalue[0].Value = App.CurrentDS.CurrentWA.DbfInfo.DBFBOF;
                    break;

            }

            return tAnswer;
        }

        // Compare a value to a lower and upper range and return
        // if between those two values (inclusive)
        public static bool Between(AppClass App, List<string> pop)
        {
            bool result = false;

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            if ((stype1.Equals(stype2) && stype1.Equals(stype3)) == false)
                AppErrorHandling.SetError(9, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            switch (stype1)
            {
                case "N":
                    if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
                    if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;
                    if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;
                    result = JAXLib.Between(val1, val2, val3);
                    break;

                case "C":
                    result = JAXLib.Between(string1, string2, string3);
                    break;

                case "D":
                case "T":
                    if (DateTime.TryParse(string1.Trim(), out DateTime dval1) == false) dval1 = DateTime.MinValue;
                    if (DateTime.TryParse(string2.Trim(), out DateTime dval2) == false) dval2 = DateTime.MinValue;
                    if (DateTime.TryParse(string3.Trim(), out DateTime dval3) == false) dval3 = DateTime.MinValue;
                    result = JAXLib.Between(dval1, dval2, dval3);
                    break;

                case "L":
                    bool bval1 = string1.Equals(".T.");
                    bool bval2 = string1.Equals(".T.");
                    bool bval3 = string1.Equals(".T.");
                    result = JAXLib.Between(bval1, bval2, bval3);
                    break;
            }

            return result;
        }

        // Bitwise math routines
        public static int BitMath(AppClass App, string cmd, string stype1, string stype2, int intval1, int intval2, List<string> pop)
        {
            int result = 0;

            switch (cmd)
            {
                case "`BITAND":                             // Bit And
                    if ((stype1 + stype2).Equals("NN"))
                        result = intval1 & intval2;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITCLEAR":                           // Bit to 0 - TODO
                    if ((stype1 + stype2).Equals("NN"))
                    {
                        intval1 &= ~(1 << intval2);
                        result = intval1;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITLSHIFT":                          // Bit left shift
                    if ((stype1 + stype2).Equals("NN"))
                        result = intval1 << intval2;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITOR":                             // Bit or - TODO
                    if (stype1.Equals("N"))
                    {
                        for (int i = 1; i < pop.Count; i++)
                        {
                            if (pop[i][..1].Equals('N'))
                            {
                                if (int.TryParse(pop[i][1..], out intval2) == false)
                                    intval2 = 0;

                                result = intval1 | intval2;
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITRSHIFT":                          // Bit right shift
                    if ((stype1 + stype2).Equals("NN"))
                        result = intval1 >> intval2;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITSET":                             // Bit to 1 - TODO
                    if ((stype1 + stype2).Equals("NN"))
                    {
                        intval1 &= intval1 | (1 << intval2);
                        result = intval1;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`BITXOR":                             // Bit XOR  - TODO
                    if (stype1.Equals("N"))
                    {
                        for (int i = 1; i < pop.Count; i++)
                        {
                            if (pop[i][..1].Equals('N'))
                            {
                                if (int.TryParse(pop[i][1..], out intval2) == false)
                                    intval2 = 0;

                                result = intval1 ^ intval2;
                            }
                        }
                    }
                    AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;
            }

            return result;
        }
    }
}