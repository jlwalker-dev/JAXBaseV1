/* --------------------------------------------------------------------------------------------------*
 * --------------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Text;

namespace JAXBase.Math
{
    public partial class MathFuncsC
    {
        //[LibraryImport("user32.dll")]
        //private static partial short GetKeyState(int keyCode);

        public static async Task<JAXObjects.Token> C(AppClass App, string _rpn, List<string> pop)
        {
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
            if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;

            int intval1 = (int)val1;
            int intval2 = (int)val2;
            int intval3 = (int)val3;

            switch (_rpn)
            {
                case "`CANDIDATE":                          // Is index a candidate index?
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CAPSLOCK":                           // Caps key is on
                    tAnswer.Element.Value = Control.IsKeyLocked(Keys.CapsLock);
                    break;

                case "`CAST":                               // Cast one type to another, usually in a SQL statement
                    tAnswer._avalue[0].Value = Cast(App, pop);
                    break;

                case "`CD":
                    tAnswer._avalue[0].Value = Program.CurrentApp.CurrentDS.JaxSettings.Default;
                    break;

                case "`CDOW":                               // Calendar Day of Week
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = string.Empty;
                        else
                            tAnswer._avalue[0].Value = dtVal.DayOfWeek.ToString();
                    }
                    else
                        AppErrorHandling.SetError(9996, "11|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CEILING":                            // Get next highest integer
                    if (stype1.Equals("N"))
                    {
                        intval1 = (int)val1 + (val1 > 0 && (val1 - intval1 == 0) ? 0 : 1);
                        tAnswer._avalue[0].Value = intval1;
                    }
                    else
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CHR":                // Return the ascii character of a number
                    tAnswer._avalue[0].Value = "" + ((char)intval1);
                    break;

                case "`CHRSAW":             // is this character in the keyboard buffer?
                    // --------------------------------------------------------------------------------- TODO
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CHRTRAN": // Change each characer in string1 with string2
                                 // defaulting to "" for replacement if string2
                                 // is shorter than string1
                    if ((stype1 + stype2).Equals("CC") && (stype3.Equals("C") || string.IsNullOrEmpty(string3)))
                    {
                        string result = string1;
                        for (int i = 0; i < string2.Length; i++)
                        {
                            string string4 = string3.Length > i ? string3.Substring(i, 1) : string.Empty;
                            result = result.Replace(string2.Substring(i, 1), string4);
                        }

                        tAnswer._avalue[0].Value = result;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CHRTRANC":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CMONTH":
                    if ("TD".Contains(stype1))
                    {
                        if ((DateTime.TryParse(string1, out dtVal) == false) || dtVal == DateTime.MinValue)
                            tAnswer._avalue[0].Value = string.Empty;
                        else
                            tAnswer._avalue[0].Value = dtVal.Month.ToString();
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`COMMIT":
                    // --------------------------------------------------------------------------------- TODO
                    break;

                case "`COMPILE":
                    string1 = string1.Trim();

                    if (string.IsNullOrWhiteSpace(string1) == false)
                    {
                        string1 = App.JaxCompiler.CompileBlock(string1, false, out int errCount);
                        string1 = errCount > 0 ? string.Empty : string1;
                    }

                    tAnswer.Element.Value = string1;
                    break;

                case "`COMPOBJ":
                    throw new Exception("1999|");

                case "`COS":                            // Cosine
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = System.Math.Acos(val1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CPCONVERT":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CPCURRENT":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CPDBF":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CREATEBINARY":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CREATEOBJECT":                   // Create an object of specified class
                    string1 = string1.ToLower().Trim();

                    var cType = Array.Find(App.lists.JAXObjects, s => s.Equals(string1, StringComparison.OrdinalIgnoreCase));
                    if (cType is not null)
                    {
                        List<ParameterClass>? plist=new();

                        if (string.IsNullOrWhiteSpace(string2))
                            plist = null;   // Empty parameter string
                        else
                        {
                            // Parameter 2 is a delimited string that contains property
                            // assignment, such as: Name="MyObj";Top=10;Left=55;Height=30;AutoSize=.T."
                            // which is parsed and sent to the JAXObjectWrapper class

                            // TODO - Can't split on ',' until I ignore them in quoted material
                            string[] pl= string2.Split(';');
                            string2 = "";

                            for (int i = 0; i < pl.Length; i++)
                            {
                                string[] par = pl[i].Split('=');

                                if (par.Length == 2) 
                                {
                                    ParameterClass p = new();
                                    p.Type = "N";
                                    p.PName = par[0].Trim();
                                    GenericClass gc = await App.JaxMath.SolveMath(par[1].Trim());
                                    p.token.Element.Value = gc.Value.Element.Value;
                                    plist.Add(p);
                                }
                                else
                                {
                                    // Was not in the format NAME=VALUE
                                    AppErrorHandling.SetError(9999,$"1905|{pl[i]}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                }
                            }
                        }

                        try
                        {
                            tAnswer._avalue[0].Value = new JAXObjectWrapper(App, string1, string2, plist);
                        }
                        catch (Exception ex)
                        {
                            string a = ex.Message;
                        }
                    }
                    else
                    {
                        JAXObjectWrapper? jow = AppHelper.FindUserClass(App, string1);
                        if (jow is not null)
                            tAnswer._avalue[0].Value = JAXUtilities.CloneJson(jow)!;
                        else
                            AppErrorHandling.SetError(9999, "1733|CREATEOBJECT()|" + string1.ToUpper(), System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    break;

                case "`CTOBIN":
                    // Convert the base64 string to a numeric value
                    byte[] rc = [];
                    string Field = string.Empty;
                    double DVal = 0L;

                    if (stype1.Equals("C"))
                    {
                        if (stype2.Equals("C"))
                        {
                            string2 = string2.Trim().ToUpper();

                            if (string2.Length == 1)
                                string2 += "I";
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(stype2))
                                string2 = "4I";
                            else
                                AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }

                        // First, how long is it?
                        switch (string1.Length)
                        {
                            case 1:
                                rc = Convert.FromBase64String(string1);
                                DVal = rc[0];
                                break;

                            case 2:
                                rc = Convert.FromBase64String(string1);
                                DVal = rc[0] + rc[1] * 256;
                                break;

                            case 4:
                                rc = Encoding.UTF8.GetBytes(string1);
                                string c = Convert.ToBase64String(rc);
                                DVal = App.utl.CVF4(c);
                                break;

                            case 8:
                                rc = Encoding.UTF8.GetBytes(string1);
                                c = Convert.ToBase64String(rc);
                                DVal = App.utl.CVD(c);
                                break;

                            default:
                                throw new Exception("11|");
                        }
                    }

                    // Fix up the answer for integer & decimal length as needed
                    tAnswer.Element.Value = string2[0] == 'I' ? (long)DVal : System.Math.Round(DVal, App.CurrentDS.JaxSettings.Decimals);
                    tAnswer.Element.Dec = string2[0] == 'I' ? 0 : App.CurrentDS.JaxSettings.Decimals;
                    break;

                case "`CTOD":                           // Char to date
                    DateTime? dt2 = TimeLib.CToT(string1);
                    DateTime dtd = (DateTime)((dt2 is null) ? DateTime.MinValue : dt2!);
                    dtd!.AddSeconds(-dtd.Hour * 3600 - dtd.Minute * 60 - dtd.Second);
                    tAnswer._avalue[0].Value = dtd;
                    break;

                case "`CTOT":                           // Char to time
                    dt2 = TimeLib.CToT(string1);
                    dtd = (DateTime)((dt2 is null) ? DateTime.MinValue : dt2!);
                    tAnswer._avalue[0].Value = dtd;
                    break;

                case "`CURSORGETPROP":
                    // ---------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`CURSORSETPROP":
                    // ---------------------------------------------------------------------------------
                    break;

                case "CURSORTOJSON":
                    // ---------------------------------------------------------------------------------
                    //TODO - tAnswer.Element.Value=JsonConvert.SerializeObject(dt, Formatting.Indented);
                    break;

                case "`CURSORTOXML":                    // Create a VFP compatible XML from a table
                    // ---------------------------------------------------------------------------------
                    break;

                case "`CURREC":
                    // TODO - Like CURVAL but grab entire record with _DELETED
                    int cwa = App.CurrentDS.CurrentWorkArea();

                    if (stype2.Equals("N"))
                    {
                        if (intval2 > 0)
                            App.CurrentDS.SelectWorkArea(intval2);
                    }
                    else if (stype2.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string2) == false)
                            App.CurrentDS.SelectWorkArea(string2.Trim());
                    }
                    else if (string.IsNullOrWhiteSpace(string2) == false)
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    if (App.CurrentDS.CurrentWA is null || App.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                        AppErrorHandling.SetError(52, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    else
                    {
                        // Now get the current field value directly from the table
                        JAXDirectDBF.DBFInfo dbf = App.CurrentDS.CurrentWA.DbfInfo;

                        List<JAXTables.FieldInfo> fields = [];
                        for (int i = 0; i < dbf.Fields.Count; i++)
                        {
                            if (dbf.Fields[i].FieldName.Equals(string1, StringComparison.OrdinalIgnoreCase))
                            {
                                fields.Add(dbf.Fields[i]);
                                break;
                            }
                        }

                        List<JAXObjects.Token> valueList = await App.CurrentDS.CurrentWA.FastFieldValue(App.CurrentDS.CurrentWA.DbfInfo.RecNo, fields);

                        App.CurrentDS.SelectWorkArea(cwa);

                        if (valueList.Count == 0)
                            AppErrorHandling.SetError(9999, "9999|CURREC()|"+_rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        else
                        {
                            // Create the object and save the answer
                            JAXObjectWrapper emptyRec = new(App, "empty", "", []);
                            for (int i = 0; i < valueList.Count; i++)
                                await emptyRec.AddProperty(fields[i].FieldName, valueList[i], 0, string.Empty);

                            tAnswer.Element.Value = emptyRec;
                        }
                    }

                    break;

                case "`CURVAL":
                    cwa = App.CurrentDS.CurrentWorkArea();

                    if (stype2.Equals("N"))
                    {
                        if (intval2 > 0)
                            App.CurrentDS.SelectWorkArea(intval2);
                    }
                    else if (stype2.Equals("C"))
                    {
                        if (string.IsNullOrWhiteSpace(string2) == false)
                            App.CurrentDS.SelectWorkArea(string2.Trim());
                    }
                    else if (string.IsNullOrWhiteSpace(string2) == false)
                        AppErrorHandling.SetError(11, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    if (App.CurrentDS.CurrentWA is null || App.CurrentDS.CurrentWA.DbfInfo.DBFStream is null)
                        AppErrorHandling.SetError(52, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    else
                    {

                        // Now get the current field value directly from the table
                        JAXDirectDBF.DBFInfo dbf = App.CurrentDS.CurrentWA.DbfInfo;

                        List<JAXTables.FieldInfo> fields = [];
                        for (int i = 1; i < dbf.Fields.Count; i++)
                        {
                            if (dbf.Fields[i].FieldName.Equals(string1, StringComparison.OrdinalIgnoreCase))
                            {
                                fields.Add(dbf.Fields[i]);
                                break;
                            }
                        }

                        List<JAXObjects.Token> valueList = await App.CurrentDS.CurrentWA.FastFieldValue(App.CurrentDS.CurrentWA.DbfInfo.RecNo, fields);

                        App.CurrentDS.SelectWorkArea(cwa);

                        if (valueList.Count == 1)
                            tAnswer.Element.Value = valueList[0].Element.Value;
                        else
                            AppErrorHandling.SetError(9999, "9999|CURVAL()"+_rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    }
                    break;
            }
            return tAnswer;
        }

        public static JAXObjects.Token Cast(AppClass App, List<string> pop)
        {
            JAXObjects.Token result = new()
            {
                TType = "U"
            };

            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (int.TryParse(string1.Trim(), out int intval1) == false) intval1 = 0;
            if (int.TryParse(string3.Trim(), out int intval3) == false) intval3 = 0;

            if (stype2.Equals("C"))
            {
                switch (string2.ToUpper())
                {
                    case "C":
                    case "CHAR":
                    case "CHARACTER":
                        if (stype3.Equals(""))
                            intval3 = 1;
                        result._avalue[0].Value = string1[..1].PadLeft(intval3);
                        break;

                    case "B":
                    case "DOUBLE":
                    case "F":
                    case "FLOAT":
                    case "N":
                    case "NUM":
                    case "NUMERIC":
                        if (pop.Count > 3)
                        {
                            switch (stype1)
                            {
                                case "N":
                                case "C":
                                    break;

                                case "L":
                                    result._avalue[0].Value = string1 switch
                                    {
                                        ".F." => 0,
                                        _ => 1,
                                    };
                                    break;

                                default:
                                    AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                    break;
                            }

                            if (int.TryParse(string3, out intval3) == false)
                                intval3 = 10;

                            if (int.TryParse(pop[3][1..], out int intval4) == false)
                                intval4 = 0;

                            if (intval3 < 1 || intval4 < 0)
                                result._avalue[0].Value = 0;
                            else
                                result._avalue[0].Value = val1;
                        }
                        else
                            AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);  // Invalid number of parameters
                        break;

                    case "D":
                    case "DATE":
                        if ("DTC".Contains(stype1))
                        {
                            if (DateTime.TryParse(string1, out DateTime dtVal) == false)
                                dtVal = DateTime.MinValue;

                            result._avalue[0].Value = dtVal.AddSeconds(-dtVal.Hour * 3600 - dtVal.Minute * 60 - dtVal.Second);
                        }
                        else
                            AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case "I":
                    case "INT":
                    case "INTEGER":
                        switch (stype1)
                        {
                            case "N":
                            case "C":
                                result._avalue[0].Value = intval1;
                                break;

                            case "L":
                                result._avalue[0].Value = string1 switch
                                {
                                    ".F." => 0,
                                    _ => 1,
                                };
                                break;

                            default:
                                AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                break;
                        }
                        break;

                    case "T":
                    case "DATETIME":
                        if ("DTC".Contains(stype1))
                        {
                            if (DateTime.TryParse(string1, out DateTime dtVal) == false)
                                dtVal = DateTime.MinValue;

                            result._avalue[0].Value = dtVal;
                        }
                        else
                            AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        break;

                    case "L":
                    case "LOGICAL":
                        switch (stype1)
                        {
                            case "C":
                                result._avalue[0].Value = string1.ToUpper() switch
                                {
                                    ".T." => true,
                                    _ => false,
                                };
                                break;

                            case "N":
                                result._avalue[0].Value = val1 switch
                                {
                                    0 => false,
                                    _ => true,
                                };
                                break;

                            default:
                                AppErrorHandling.SetError(9999, "9999|CAST()|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                break;
                        }
                        break;

                    case "U":
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);  // TODO
                        break;

                    case "X":
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);  // TODO
                        break;
                }
            }
            else
                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            return result;
        }

        /* --------------------------------------------------------------------------------------------------*
         * Compare two objects and return a boolean indicating if they are excactly the same except
         * for object order, and the following properties:
         *      aerror, classid, hwnd, and name
         *      top & left are also excluded if Parent is null
         * --------------------------------------------------------------------------------------------------*/
        public static async Task<bool> CompObj(JAXObjectWrapper obj1, JAXObjectWrapper obj2)
        {
            // First, are they the same class and base class?
            bool result = obj1.Class.Equals(obj2.Class) && obj1.BaseClass.Equals(obj2.BaseClass);

            int ccount1 = 0;
            int ccount2 = 0;

            // Next, check out child object counts an if the same and greater
            // than zero, recurse through each object.
            // Child objects are searched for by name, meaning they do not
            // need to be in the same order to be considered a match.
            if (result && (await obj1.IsMember("controlcount")).Equals("P"))
            {
                if ((await obj1.IsMember("controlcount")).Equals("P"))
                    ccount1 = (await obj1.GetProperty("controlcount")).AsInt();

                if ((await obj2.IsMember("controlcount")).Equals("P"))
                    ccount2 = (await obj2.GetProperty("controlcount")).AsInt();

                // Same number of objects attached to this object?
                result = result && ccount1 == ccount2;

                if (result && ccount1 > 0)
                {
                    // Check out each object
                    for (int i = 0; i < ccount1; i++)
                    {
                        JAXObjectWrapper? ochild1=await obj1.GetObject(0);

                        if (ochild1 is not null)
                        {
                            int idx2 = await obj2.FindObjectByName(ochild1.JOWName);
                            if (idx2 >= 0)
                            {
                                JAXObjectWrapper? ochild2 = await obj2.GetObject(idx2);
                                if (ochild2 is not null)
                                    result = await CompObj(ochild1, ochild2);
                                else
                                    result = false;
                            }
                            else
                                result = false;
                        }
                        else
                            result = false;

                        // break out early?
                        if (result == false)
                            break;
                    }
                }
            }

            // Check out the properties
            if (result)
            {
                List<string> list1 = obj1.GetPropertyList();

                for (int i = 0; i < list1.Count; i++)
                {
                    if (JAXLib.InListC(list1[i], "name", "controlcount", "objects", "classid", "aerror", "hwnd") == false && (obj1.Parent is null && JAXLib.InListC(list1[i], "top", "left") == false))
                    {
                        // Check this object
                        if ((await obj2.IsMember(list1[i])).Equals("P"))
                        {
                            // check the values of both
                            JAXObjects.Token prop1 = new();
                            JAXObjects.Token prop2 = new();

                            prop1 = await obj1.GetProperty(list1[i]);
                            prop2 = await obj2.GetProperty(list1[i]);

                            if (prop1.TType.Equals(prop2.TType))
                            {
                                if (prop1.TType.Equals("O"))
                                {
                                    // Compare these properties
                                    result = await CompObj((JAXObjectWrapper)prop1.Element.Value, (JAXObjectWrapper)prop2.Element.Value);
                                }
                                else if (prop1.TType.Equals("A"))
                                {
                                    // Compare these arrays
                                    if (prop1._avalue.Count == prop2._avalue.Count)
                                    {
                                        // check each array element and compare as needed
                                        for (int j = 0; j < prop2._avalue.Count; j++)
                                        {
                                            if (prop1._avalue[j].Type.Equals(prop2._avalue[j].Type))
                                            {
                                                if (prop1._avalue[j].Type.Equals("O"))
                                                    result = await CompObj((JAXObjectWrapper)prop1._avalue[j].Value, (JAXObjectWrapper)prop2._avalue[j].Value);
                                                else
                                                {
                                                    if (prop1._avalue[j].IsNull() || prop2._avalue[j].IsNull())
                                                        result = prop1._avalue[j].IsNull() && prop2._avalue[j].IsNull();
                                                    else
                                                        result = prop1.AsString().Equals(prop2.AsString());
                                                }
                                            }
                                            else
                                                result = false;

                                            if (result == false)
                                                break;
                                        }
                                    }
                                    else
                                        result = false;
                                }
                                else if (prop1.Element.Type.Equals(prop2.Element.Type))
                                {
                                    // Compare these properties
                                    if (prop1.Element.IsNull() || prop2.Element.IsNull())
                                        result = prop1.Element.IsNull() && prop2.Element.IsNull();
                                    else
                                        result = prop1.AsString().Equals(prop2.AsString());
                                }
                                else
                                    result = false;
                            }
                            else
                                result = false;
                        }
                        else
                            result = false;
                    }

                    if (result == false)
                        break;
                }
            }

            return result;
        }
    }
}
