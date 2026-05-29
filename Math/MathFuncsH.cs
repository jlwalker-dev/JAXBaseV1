using JAXBase.Core;
using JAXBase.Data;
using JAXBase.XBase;
using JAXBase.Utilities;
using JAXBase.Language;

namespace JAXBase.Math
{
    public class MathFuncsH
    {
        public static JAXObjects.Token H(AppClass App, string _rpn, List<string> pop)
        {
            DateTime dtVal;
            bool done;
            JAXObjects.Token tAnswer = new();

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);
            string stype4 = (pop.Count > 3 ? pop[3][..1] : string.Empty);
            string stype5 = (pop.Count > 4 ? pop[4][..1] : string.Empty);
            string stype6 = (pop.Count > 5 ? pop[5][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);
            string string4 = (pop.Count > 3 ? pop[3][1..] : string.Empty);
            string string5 = (pop.Count > 4 ? pop[4][1..] : string.Empty);
            string string6 = (pop.Count > 5 ? pop[5][1..] : string.Empty);

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
            int cwa = 0;

            switch (_rpn)
            {
                case "`HEADER":
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;
                    JAXObjectWrapper header = new(App, "header", "header_" + dbf.Alias, []);

                    App.CurrentDS.SelectWorkArea(cwa);
                    break;

                case "`HEXTOINT":
                    if (stype1.Equals("C"))
                    {
                        string1 = string1.Trim().ToUpper();
                        tAnswer.Element.Value = -1;

                        // Remove the 0X if it exists
                        if (string1.Length > 2 && string1[..2].Equals("0X"))
                            string1 = string1[..2];

                        if (string.IsNullOrWhiteSpace(string1) == false)
                        {
                            if (JAXLib.ChrTran(string1, "0123456789ABCDEF", "").Length == 0)
                            {
                                // It's a hex
                                tAnswer.Element.Value = Convert.ToInt64(string1, 16);
                            }
                            else
                            {
                                // It's not a hex value - TODO
                                AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`HOUR":
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = 0;
                        else
                            tAnswer._avalue[0].Value = dtVal.Hour;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ICASE":
                    done = false;
                    string2 = string.Empty;
                    intval2 = 0;

                    // Going left to right
                    foreach (string popped in pop)
                    {
                        if (++intval2 % 2 == 1) // get condition
                        {
                            // test the results of the condition
                            if (popped.Equals("L.T.")) intval1 = 1;
                            done = (intval1 != 0);
                        }
                        else // get response
                        {
                            if (done)
                            {
                                // grab the answer to the successful condition
                                string2 = popped;
                                break;
                            }
                        }
                    }

                    if (pop.Count % 2 == 1) // odd number of parameters
                    {
                        // was there a succesful condition?
                        if (string2 == string.Empty)
                        {
                            // no, use the default response
                            tAnswer._avalue[0].Value = AppHelper.Convert2STValue(pop[^1])!;
                        }
                    }
                    else
                    {
                        // even number of parameters returns a null
                        tAnswer._avalue[0].MakeNull();
                    }

                    break;

                case "`IDXCOLLATE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`IIF":  // Perform an IIF 
                    if (stype1.Equals("L"))
                        tAnswer._avalue[0].Value = string1.Equals(".T.") ? AppHelper.Convert2STValue(pop[1])! : AppHelper.Convert2STValue(pop[3])!;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`IMESTATUS":
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`INDBC":                          // Is this object in a database
                    // ---------------------------------------------------------------------------------
                    tAnswer._avalue[0].Value = false;
                    break;

                case "`INDEXSEEK":
                    // --------------------------------------------------------------------------------- TODO
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;

                    App.CurrentDS.SelectWorkArea(cwa);
                    break;

                case "`INKEY":                      // Calls the routine that mimics the INKEY function
                    // ---------------------------------------------------------------------------------
                    tAnswer._avalue[0].Value = JAXLanguage.InKey(intval2);
                    break;

                case "`INLISTC":
                case "`INLIST":
                    if (pop.Count < 2)
                        throw new Exception("10||Too few arguments");

                    List<Object> iPop = [];
                    bool hasNull = false;
                    JAXObjects.Token tk = new();

                    if (pop[0].Length > 0)
                    {
                        if (pop[0][0] == 'X')
                        {
                            tk.Element.MakeNull();
                            hasNull = true;
                        }
                        else
                        {
                            object? tko = AppHelper.Convert2STValue(pop[0]);
                            if (tko is null)
                                tk.Element.MakeNull();
                            else
                                tk.Element.Value = tko;

                            string popType = tk.Element.Type;

                            // Make sure INLISTC is comparing character types
                            if (_rpn.Equals("`INLISTC") && tk.Element.Type.Equals("C") == false)
                                throw new Exception("11|");

                            // Load the list
                            for (int i = 0; i < pop.Count; i++)
                            {
                                tko = AppHelper.Convert2STValue(pop[i]);
                                if (tko is null)
                                    hasNull = true;
                                else
                                {
                                    tk.Element.Value = tko;

                                    // correct for Date/Time comparisons
                                    if ((popType + tk.Element.Type).Equals("DT"))
                                        tk.Element.Value = tk.AsDate();
                                    else if ((popType + tk.Element.Type).Equals("TD"))
                                        tk.Element.Value = tk.AsDateTime();

                                    // Add the element to the list
                                    if (tk.Element.Type.Equals(popType))
                                        iPop.Add(tko);
                                    else
                                        throw new Exception("11|");
                                }
                            }
                        }
                    }
                    else
                        throw new Exception("1229|");

                    if (hasNull)
                    {
                        // A null anywhere in the process returns .NULL.
                        tAnswer.Element.MakeNull();
                    }
                    else
                    {
                        if (_rpn.Equals("`INLIST"))
                        {
                            // Convert list to array and send to InList
                            Object[] aPop = iPop.ToArray();
                            tAnswer.Element.Value = JAXLib.InList(iPop);
                        }
                        else
                        {
                            // Convert list to array and send to InListC
                            string[] aPop = new string[iPop.Count];
                            for (int i = 0; i < aPop.Length; i++)
                                aPop[i] = iPop[i].ToString() ?? string.Empty;

                            tAnswer.Element.Value = JAXLib.InListC(aPop);
                        }
                    }
                    break;


                case "`INPUTBOX":

                    if (string.IsNullOrWhiteSpace(stype1))  // Prompt
                    {
                        stype1 = "C";
                        string1 = "Input: ";
                    }

                    stype2 = string.IsNullOrWhiteSpace(stype2) ? "C" : stype2;  // Title
                    stype3 = string.IsNullOrWhiteSpace(stype3) ? "C" : stype3;  // Default value

                    if ((stype1 + stype2 + stype3).Equals("CCC"))
                        tAnswer.Element.Value = JAXLib.InputBox.Show(string1, string2, string3, intval4, string5, string6);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`INSMODE":
                    tAnswer.Element.Value = Control.IsKeyLocked(Keys.Insert);
                    break;

                case "`INT":
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = intval1;
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`INTTOHEX":
                    if (stype1.Equals("N"))
                    {
                        if (intval1 >= 0)
                            tAnswer.Element.Value = intval1.ToString("X");
                        else
                            AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);  // TODO
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISALPHA":
                    if (stype1.Equals("C") && string1.Length > 0)
                        tAnswer.Element.Value = JAXLib.Between((string1.ToUpper() + " ")[0], 'A', 'Z');
                    else
                        tAnswer.Element.Value = false;
                    break;

                case "`ISBLANK":                // returns if a value is blank (uninitialized fields, especially)
                    if (stype1.Equals("C"))
                        tAnswer.Element.Value = string.IsNullOrEmpty(string1);
                    else
                        tAnswer.Element.Value = false;
                    break;

                case "`ISDIGIT":
                    if (stype1.Equals("C") && string1.Length > 0)
                        tAnswer.Element.Value = "0123456789".Contains((string1 + " ")[0]);
                    else
                        tAnswer.Element.Value = false;
                    break;

                case "`ISEXCLUSIVE":
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;

                    App.CurrentDS.SelectWorkArea(cwa);
                    break;

                case "`ISFLOCKED":
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;

                    App.CurrentDS.SelectWorkArea(cwa);
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISLEADBYTE":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISLOWER":
                    if (stype1.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string1))
                            string2 = string1;
                        else
                            string2 = JAXLib.ChrTran(string1, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", "");

                        tAnswer.Element.Value = string1.Equals(string2);
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`ISNULL":
                    tAnswer.Element.Value = stype1.Equals('X');
                    break;

                case "`ISODD":
                    if (stype1.Equals("N"))
                        tAnswer.Element.Value = intval1 % 1 == 1;
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`ISPEN":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISREADONLY":
                    // --------------------------------------------------------------------------------- TODO
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;
                    App.CurrentDS.SelectWorkArea(cwa);

                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISRLOCKED":
                    // ---------------------------------------------------------------------------------
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

                    dbf = App.CurrentDS.CurrentWA.DbfInfo;

                    App.CurrentDS.SelectWorkArea(cwa);
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ISUPPER":
                    if (stype1.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string1))
                            string2 = string1;
                        else
                            string2 = JAXLib.ChrTran(string1, "abcdefghijklmnopqrstuvwxyz", "");

                        tAnswer.Element.Value = string1.Equals(string2);
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                default:
                    throw new Exception("1999|" + _rpn);
            }

            return tAnswer;
        }
    }
}
