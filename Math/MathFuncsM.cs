using JAXBase.Core;
using JAXBase.Data;
using NodaTime;
using NodaTime.TimeZones;
using JAXBase.Utilities.Utilities;
using JAXBase.XBase;

namespace JAXBase.Math
{
    public class MathFuncsM
    {
        public static async Task<JAXObjects.Token> M(AppClass App, string _rpn, List<string> pop)
        {
            DateTime dtVal;
            JAXObjects.Token tAnswer = new();

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
            if (double.TryParse(string4.Trim(), out double val4) == false) val3 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;
            int intval4 = (int)val4;

            //int col = 0;
            int start = 0;
            int end = 0;

            JAXDirectDBF.DBFInfo dbf;
            int cwa;

            switch (_rpn)
            {
                case "`MAX":
                    //          MAX(expr1, expr2, expr3...) returns max value from list of similar expression types
                    //
                    //  In the following example, all values in the array must be of similar type - if logical is encountered, the comparison ends
                    //          MAX("ARRAY") returns max value from array (must all be same type)
                    //
                    //  In the following examples, if the array is one dimensional, then the values following the name of the array point at elements, but if the array
                    //  is two dimensional, then the second value is the starting row number.  If the logical value is encountered, then the comparison ends
                    //          MAX("ARRAY,2") returns max value from array column 2 (must all be same type)
                    //          MAX("ARRAY,2,7") returns max value from array column 2 (must all be same type) starting at cell 7 and going to end of array
                    //          MAX("ARRAY,2,7,10") returns max value from a two dimensional array for column 2 (must all be same type) starting at cell 7 and going to cell 10
                    //
                    switch (pop.Count)
                    {
                        case 0:
                            throw new Exception("11|");

                        case 1: // Must be a string holding array information
                            if (stype1.Equals("C"))
                            {
                                string[] arrayParts = string1.Split(',');
                                JAXObjects.Token aVar = await App.GetVarToken(arrayParts[0]);

                                if (aVar.TType.Equals("A") == false)
                                    throw new Exception("Not an array");
                                else
                                {
                                    if (aVar.Col < 2)
                                    {
                                        end = aVar.Row;

                                        // one dimensional array
                                        switch (arrayParts.Length)
                                        {
                                            case 1:     // Whole array
                                                break;

                                            case 2:     // Start at element
                                                start = intval2;
                                                break;

                                            case 3:     // Start at and end at element
                                                start = intval2;
                                                end = intval3;
                                                break;

                                            case 4:     // Not a 2D array
                                                throw new Exception("11|");

                                            default:
                                                throw new Exception("11|");
                                        }

                                        if (start > end) throw new Exception("Start > End");
                                        if (start < 1) throw new Exception("Start < 1");
                                        if (end > aVar.Col * aVar.Row) throw new Exception("End > number of cells");

                                        JAXObjects.Token saItem = new();
                                        JAXObjects.Token saItem2 = new();
                                        aVar.SetElement(start, 1);
                                        saItem.Element.Value = aVar.Element.Value;

                                        for (int i = start; i <= end; i++)
                                        {
                                            aVar.SetElement(i, 1);
                                            saItem2.Element.Value = aVar.Element.Value;
                                            if (saItem.Element.Type.Equals(saItem2.Element.Type))
                                            {
                                                switch (saItem.Element.Type)
                                                {
                                                    case "N":
                                                        if (saItem2.AsDouble() > saItem.AsDouble()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    case "L":
                                                        if (saItem2.AsBool()) saItem.Element.Value = true;
                                                        break;

                                                    case "T":
                                                        if (saItem2.AsDateTime() > saItem.AsDateTime()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    case "D":
                                                        if (saItem2.AsDate() > saItem.AsDate()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    default:    // Character
                                                        if (saItem2.AsString().CompareTo(saItem.AsString()) > 0) saItem.Element.Value = saItem2.Element.Value;
                                                        break;
                                                }
                                            }
                                            else if (saItem2.Element.Type.Equals("L"))
                                                break;
                                            else
                                                throw new Exception("Type mismatch");
                                        }

                                        tAnswer._avalue[0].Value = saItem.Element.Value;
                                    }
                                    else
                                    {
                                        int colStart = 1;
                                        int colEnd = 1;

                                        // two dimensional array
                                        switch (arrayParts.Length)
                                        {
                                            case 1:     // Look at all elements
                                                colStart = 1;
                                                colEnd = aVar.Col;
                                                start = 1;
                                                end = aVar.Row;
                                                break;

                                            case 2:     // Look at Column x
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = 1;
                                                end = aVar.Row;
                                                break;

                                            case 3:     // Look at Column x starting at element y
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = intval3;
                                                end = aVar.Row;
                                                break;

                                            case 4:     // Look at Column x starting at element y ending at z
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = intval3;
                                                end = intval4;
                                                break;

                                            default:
                                                throw new Exception("11|");
                                        }

                                        if (colStart < 1) throw new Exception("Column start <1");
                                        if (colEnd < aVar.Col) throw new Exception("Column end > number of rows");
                                        if (colStart < colEnd) throw new Exception("Column start < Column end");
                                        if (start > end) throw new Exception("Start > End");
                                        if (start < 1) throw new Exception("Start < 1");
                                        if (end > aVar.Col * aVar.Row) throw new Exception("End > number of cells");


                                        JAXObjects.Token saItem = new();
                                        JAXObjects.Token saItem2 = new();
                                        aVar.SetElement(start, colStart);
                                        saItem.Element.Value = aVar.Element.Value;

                                        for (int r = start; r <= end; r++)
                                        {
                                            for (int c = colStart; c <= colEnd; c++)
                                            {
                                                aVar.SetElement(r, c);
                                                saItem2.Element.Value = aVar.Element.Value;
                                                if (saItem.Element.Type.Equals(saItem2.Element.Type))
                                                {
                                                    switch (saItem.Element.Type)
                                                    {
                                                        case "N":
                                                            if (saItem2.AsDouble() > saItem.AsDouble()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        case "L":
                                                            if (saItem2.AsBool()) saItem.Element.Value = true;
                                                            break;

                                                        case "T":
                                                            if (saItem2.AsDateTime() > saItem.AsDateTime()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        case "D":
                                                            if (saItem2.AsDate() > saItem.AsDate()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        default:    // Character
                                                            if (saItem2.AsString().CompareTo(saItem.AsString()) > 0) saItem.Element.Value = saItem2.Element.Value;
                                                            break;
                                                    }
                                                }
                                                else if (saItem2.Element.Type.Equals("L"))
                                                    break;
                                                else
                                                    throw new Exception("Type mismatch");
                                            }
                                        }

                                        tAnswer._avalue[0].Value = saItem.Element.Value;
                                    }
                                }
                            }
                            else
                                throw new Exception("11|");

                            break;

                        default:        // Expecting a list of similar value types
                            string objTyp = pop[0][..1];

                            object objVal = objTyp switch
                            {
                                "N" => 0D,
                                "D" => DateOnly.MinValue,
                                "T" => DateTime.MinValue,
                                "L" => false,
                                _ => string.Empty
                            };

                            for (int popCount = 0; popCount < pop.Count; popCount++)
                            {
                                string oType = pop[popCount][..1];
                                string oValue = pop[popCount][1..];

                                if (oType.Equals(objTyp) == false)
                                    throw new Exception("11|");
                                else
                                {
                                    switch (objTyp)
                                    {
                                        case "N":
                                            if (double.TryParse(oValue, out double dv) == false) dv = 0;
                                            objVal = popCount == 0 ? dv : ((double)objVal < dv ? dv : (double)objVal);
                                            break;

                                        case "L":
                                            bool lv = oValue.Equals(".T.");
                                            objVal = popCount == 0 ? lv : ((bool)objVal! || lv);
                                            break;

                                        case "T":
                                        case "D":
                                            if (DateTime.TryParse(oValue, out DateTime tv) == false) tv = DateTime.MinValue;
                                            objVal = popCount == 0 ? tv : ((DateTime)objVal! < tv ? tv : (DateTime)objVal);
                                            break;

                                        default:    // Character
                                            objVal = popCount == 0 ? oValue : (oValue.CompareTo(objVal!.ToString()) > 0 ? oValue : objVal);
                                            break;
                                    }
                                }
                            }

                            tAnswer._avalue[0].Value = objTyp switch
                            {
                                "N" => (double)objVal,
                                "L" => (bool)objVal,
                                "T" => (DateTime)objVal,
                                "D" => (DateOnly)objVal,
                                _ => objVal.ToString() ?? string.Empty
                            };
                            break;
                    }

                    break;

                case "`MDY":    // Returns January 1, [20]25 for {1/1/2025}     TODO - TEST ME!
                    if ("DT".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dch) == false) dch = DateTime.MinValue;
                        tAnswer._avalue[0].Value = App.CurrentDS.JaxSettings.Century ? dch.ToString("MMMM M, yyyy") : dch.ToString("MMMM M, yy");
                    }
                    else
                        throw new Exception("11|");
                    break;

                case "`MEMLINES":
                    if (stype1.Equals("C"))
                    {
                        // Get number of Carriage returns
                        int lCount = string1.Length - string1.Replace("\r", "").Length + 1;

                        // If only less than 2 characters then always 1
                        if (string1.Length < 2) lCount = 1;

                        // if last char is CR or last two are CRLF, subtract 1 from count
                        if ((string1.Length > 1 && string1[^1].Equals("\r\n")) || (string1.Length > 1 && string1.Substring(string1.Length - 3).Equals("\r\n"))) lCount--;

                        // Return the corrected count
                        tAnswer._avalue[0].Value = lCount;
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`MESSAGE":
                    JAXErrors e = App.GetLastError();
                    tAnswer._avalue[0].Value = intval1 == 1 ? e.ErrorSource : e.ErrorMessage;
                    break;

                case "`MESSAGEBOX":
                    string text = string1;
                    int boxType = intval2;
                    string caption = string3;
                    int timeout = intval4;

                    DialogResult dr = JAXLib.JAXMessageBox.Show(text, boxType, caption, timeout);

                    tAnswer._avalue[0].Value = dr switch
                    {
                        DialogResult.OK => 1,
                        DialogResult.Cancel => 2,
                        DialogResult.Abort => 3,
                        DialogResult.Retry => 4,
                        DialogResult.Ignore => 5,
                        DialogResult.Yes => 6,
                        DialogResult.No => 7,
                        _ => 0,
                    };

                    break;

                case "`MIN":
                    //          MIN(expr1, expr2, expr3...) returns min value from list of similar expression types
                    //
                    //  In the following example, all values in the array must be of similar type - if logical is encountered, the comparison ends
                    //  Arrays are read from column 1 to column n and from row 1 to row n
                    //          MIN("ARRAY") returns MIN value from array (must all be same type) 
                    //
                    //  In the following examples, if the array is one dimensional, then the values following the name of the array point at elements, but if the array
                    //  is two dimensional, then the second value is the starting row number.  If the logical value is encountered, then the comparison ends
                    //          MAX("ARRAY,2") returns min value from array column 2 (must all be same type)
                    //          MAX("ARRAY,2,7") returns min value from array column 2 (must all be same type) starting at cell 7 and going to end of array
                    //          MAX("ARRAY,2,7,10") returns min value from a two dimensional array for column 2 (must all be same type) starting at cell 7 and going to cell 10
                    //

                    switch (pop.Count)
                    {
                        case 0:
                            throw new Exception("11|");

                        case 1: // Must be a string holding array information
                            if (stype1.Equals("C"))
                            {
                                string[] arrayParts = string1.Split(',');
                                JAXObjects.Token aVar = await App.GetVarToken(arrayParts[0]);

                                if (aVar.TType.Equals("A") == false)
                                    throw new Exception("Not an array");
                                else
                                {
                                    if (aVar.Col < 2)
                                    {
                                        end = aVar.Row;

                                        // one dimensional array
                                        switch (arrayParts.Length)
                                        {
                                            case 1:     // Whole array
                                                break;

                                            case 2:     // Start at element
                                                start = intval2;
                                                break;

                                            case 3:     // Start at and end at element
                                                start = intval2;
                                                end = intval3;
                                                break;

                                            case 4:     // Not a 2D array
                                                throw new Exception("11|");

                                            default:
                                                throw new Exception("11|");
                                        }

                                        if (start > end) throw new Exception("Start > End");
                                        if (start < 1) throw new Exception("Start < 1");
                                        if (end > aVar.Col * aVar.Row) throw new Exception("End > number of cells");

                                        JAXObjects.Token saItem = new();
                                        JAXObjects.Token saItem2 = new();
                                        aVar.SetElement(start, 1);
                                        saItem.Element.Value = aVar.Element.Value;

                                        for (int i = start; i <= end; i++)
                                        {
                                            aVar.SetElement(i, 1);
                                            saItem2.Element.Value = aVar.Element.Value;
                                            if (saItem.Element.Type.Equals(saItem2.Element.Type))
                                            {
                                                switch (saItem.Element.Type)
                                                {
                                                    case "N":
                                                        if (saItem2.AsDouble() < saItem.AsDouble()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    case "L":
                                                        if (saItem2.AsBool() == false) saItem.Element.Value = false;
                                                        break;

                                                    case "T":
                                                        if (saItem2.AsDateTime() < saItem.AsDateTime()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    case "D":
                                                        if (saItem2.AsDate() < saItem.AsDate()) saItem.Element.Value = saItem2.Element.Value;
                                                        break;

                                                    default:    // Character
                                                        if (saItem2.AsString().CompareTo(saItem.AsString()) < 0) saItem.Element.Value = saItem2.Element.Value;
                                                        break;
                                                }
                                            }
                                            else if (saItem2.Element.Type.Equals("L"))
                                                break;
                                            else
                                                throw new Exception("Type mismatch");
                                        }

                                        tAnswer._avalue[0].Value = saItem.Element.Value;
                                    }
                                    else
                                    {
                                        int colStart = 1;
                                        int colEnd = 1;

                                        // two dimensional array
                                        switch (arrayParts.Length)
                                        {
                                            case 1:     // Look at all elements
                                                colStart = 1;
                                                colEnd = aVar.Col;
                                                start = 1;
                                                end = aVar.Row;
                                                break;

                                            case 2:     // Look at Column x
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = 1;
                                                end = aVar.Row;
                                                break;

                                            case 3:     // Look at Column x starting at element y
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = intval3;
                                                end = aVar.Row;
                                                break;

                                            case 4:     // Look at Column x starting at element y ending at z
                                                colStart = intval2;
                                                colEnd = intval2;
                                                start = intval3;
                                                end = intval4;
                                                break;

                                            default:
                                                throw new Exception("11|");
                                        }

                                        if (colStart < 1) throw new Exception("Column start <1");
                                        if (colEnd < aVar.Col) throw new Exception("Column end > number of rows");
                                        if (colStart < colEnd) throw new Exception("Column start < Column end");
                                        if (start > end) throw new Exception("Start > End");
                                        if (start < 1) throw new Exception("Start < 1");
                                        if (end > aVar.Col * aVar.Row) throw new Exception("End > number of cells");


                                        JAXObjects.Token saItem = new();
                                        JAXObjects.Token saItem2 = new();
                                        aVar.SetElement(start, colStart);
                                        saItem.Element.Value = aVar.Element.Value;

                                        for (int r = start; r <= end; r++)
                                        {
                                            for (int c = colStart; c <= colEnd; c++)
                                            {
                                                aVar.SetElement(r, c);
                                                saItem2.Element.Value = aVar.Element.Value;
                                                if (saItem.Element.Type.Equals(saItem2.Element.Type))
                                                {
                                                    switch (saItem.Element.Type)
                                                    {
                                                        case "N":
                                                            if (saItem2.AsDouble() < saItem.AsDouble()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        case "L":
                                                            if (saItem2.AsBool() == false) saItem.Element.Value = false;
                                                            break;

                                                        case "T":
                                                            if (saItem2.AsDateTime() < saItem.AsDateTime()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        case "D":
                                                            if (saItem2.AsDate() < saItem.AsDate()) saItem.Element.Value = saItem2.Element.Value;
                                                            break;

                                                        default:    // Character
                                                            if (saItem2.AsString().CompareTo(saItem.AsString()) < 0) saItem.Element.Value = saItem2.Element.Value;
                                                            break;
                                                    }
                                                }
                                                else if (saItem2.Element.Type.Equals("L"))
                                                    break;
                                                else
                                                    throw new Exception("Type mismatch");
                                            }
                                        }

                                        tAnswer._avalue[0].Value = saItem.Element.Value;
                                    }
                                }
                            }
                            else
                                throw new Exception("11|");

                            break;

                        default:        // Expecting a list of similar value types
                            string objTyp = pop[0][..1];

                            object objVal = objTyp switch
                            {
                                "N" => 0D,
                                "D" => DateOnly.MinValue,
                                "T" => DateTime.MinValue,
                                "L" => false,
                                _ => string.Empty
                            };

                            for (int popCount = 0; popCount < pop.Count; popCount++)
                            {
                                string oType = pop[popCount][..1];
                                string oValue = pop[popCount][1..];

                                if (oType.Equals(objTyp) == false)
                                    throw new Exception("11|");
                                else
                                {
                                    switch (objTyp)
                                    {
                                        case "N":
                                            if (double.TryParse(oValue, out double dv) == false) dv = 0;
                                            objVal = popCount == 0 ? dv : ((double)objVal > dv ? dv : (double)objVal);
                                            break;

                                        case "L":
                                            bool lv = oValue.Equals(".F.");
                                            objVal = popCount == 0 ? lv : ((bool)objVal! || lv);
                                            break;

                                        case "T":
                                        case "D":
                                            if (DateTime.TryParse(oValue, out DateTime tv) == false) tv = DateTime.MinValue;
                                            objVal = popCount == 0 ? tv : ((DateTime)objVal! > tv ? tv : (DateTime)objVal);
                                            break;

                                        default:    // Character
                                            objVal = popCount == 0 ? oValue : (oValue.CompareTo(objVal!.ToString()) < 0 ? oValue : objVal);
                                            break;
                                    }
                                }
                            }

                            tAnswer._avalue[0].Value = objTyp switch
                            {
                                "N" => (double)objVal,
                                "L" => (bool)objVal,
                                "T" => (DateTime)objVal,
                                "D" => (DateOnly)objVal,
                                _ => objVal.ToString() ?? string.Empty
                            };
                            break;
                    }

                    break;

                case "`MINUTE":
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = 0;
                        else
                            tAnswer._avalue[0].Value = dtVal.Minute;
                    }
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`MLINE":  // Return the nth row of a chr(13) delimited string
                    if (pop.Count > 1)  // must be at lest 2 parameters
                    {
                        if (stype1.Equals("C") && stype2.Equals("N"))
                        {
                            if (string1.Length > 0)
                            {
                                if (!int.TryParse(string2, out intval1)) intval1 = 0; // which one?
                                intval1 = (intval1 > 0 ? --intval1 : 1);

                                if (pop.Count > 2)
                                {
                                    // string3 holds number of characters?
                                    if (!int.TryParse(string3, out intval2)) intval2 = 0;

                                    if (intval2 > 0) string1 = string1[intval2..];
                                }

                                string[] split2 = string1.Split((char)13, StringSplitOptions.None);

                                if (split2.Length > intval1)
                                    string1 = split2[intval1];
                                else
                                    string1 = "";
                            }

                            tAnswer._avalue[0].Value = string1;
                        }
                        else
                            App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                        App.SetError(1229, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`MOD":
                    if ((stype1 + stype2).Equals("NN"))
                        tAnswer.Element.Value = val1 % val2;
                    else
                        App.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`MONTH":
                    if ("TD".Contains(stype1))
                    {
                        if (DateTime.TryParse(string1, out DateTime dtm))
                            tAnswer.Element.Value = dtm.Month;
                        else
                            App.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                        App.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`NDX":  // TODO NOW
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
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
                    }

                    App.CurrentDS.SelectWorkArea(cwa);
                    break;

                case "`NEWOBJECT":  // TODO NOW ????
                    // --------------------------------------------------------------------------------- TODO
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`NODA":
                    IDateTimeZoneProvider provider = DateTimeZoneProviders.Tzdb;
                    Instant now = SystemClock.Instance.GetCurrentInstant();
                    tAnswer.Element.Value = string.Empty;

                    var tzdbSource = TzdbDateTimeZoneSource.Default;
                    IEnumerable<string> zoneIds = tzdbSource.GetIds();

                    if (stype1.Equals("N"))
                    {
                        if (intval1 > 0 && intval1<=zoneIds.Count())
                        {
                            foreach (string zoneId in zoneIds)
                            {
                                intval1--;

                                if (intval1==0)
                                {
                                    tAnswer.Element.Value = zoneId;
                                    DateTimeZone zone = provider[zoneId];
                                    Offset offset = zone.GetUtcOffset(now);
                                    tAnswer.Element.Value = zoneId + "|" + offset.Seconds.ToString();
                                    break;
                                }
                            }
                        }

                    }
                    else if (stype1.Equals("C") == false)
                        throw new Exception("11|");
                    else if (string.IsNullOrWhiteSpace(stype1))
                    {
                        // empty string means get current
                        Offset offset = App.TimeZone.GetUtcOffset(now);
                        tAnswer.Element.Value = App.TimeZone.Id + "|" + offset.Seconds.ToString();
                    }
                    else
                    {
                        try
                        {
                            // Clean up the timezone string
                            while (string1.Contains("  "))
                                string1 = string1.Replace("  ", " ");

                            string1 = string1.Trim().Replace(" ", "_");

                            // Now find the zone
                            tAnswer.Element.Value = string.Empty;
                            foreach (string zoneId in zoneIds)
                            {
                                if (zoneId.Contains(string1, StringComparison.OrdinalIgnoreCase))
                                {
                                    tAnswer.Element.Value = zoneId;
                                    DateTimeZone zone = provider[zoneId];
                                    Offset offset = zone.GetUtcOffset(now);
                                    tAnswer.Element.Value = zoneId + "|" + offset.Seconds.ToString();
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            tAnswer.Element.Value = string.Empty;
                        }
                    }
                    break;

                case "`NORMALIZE":
                    // ---------------------------------------------------------------------------------
                    App.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`NUMLOCK":
                    tAnswer.Element.Value = Control.IsKeyLocked(Keys.NumLock);
                    break;

                case "`NVL":
                    tAnswer._avalue[0].Value = stype1.Equals("X") ? JAXMathAux.SovleSimpleTokenString(stype2 + string2) : JAXMathAux.SovleSimpleTokenString(stype1 + string1);
                    break;

            }

            return tAnswer;
        }
    }
}
