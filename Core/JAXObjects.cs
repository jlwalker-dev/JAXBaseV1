/******************************************************************************************************************************************jaxDataSession
 * This is the variable manager for simple variables, arrays, and objectsjaxDataSession
 * A variable can hold any kind of data (or be null) and array elements
 * or object properties can each individually be of any type (or null).
 * 
 *  2024-05-25 - JLW
 *      I have simple tokens (Logical, Character, Bool, Number, Date, DateTime, Null) and
 *      now Arrays of SimpleTokens.  At some point I'm going to add Objects to the
 *      simmple tokens list and fear that will require a bit of work.
 *      
 *      Objects are a dictionary of simple tokens
 *      
 *      TODO - Figure out an elegant way to pass back a code error OR we'll be forced to
 *      put exception handling around all variable handling processes.  It will save a lot
 *      of coding if we can figure it out here.  DO NOT pass the APP class in, we need to
 *      flag an exception coming out.
 *      
 *  2024.07.11 - JLW
 *      Working on implementing objects
 *      TODO: Still waiting on Record objects
 *                   
 *  2024.07.15 - JLW
 *      TODO - Null hadling is weak to non-existant - will take some time to figure out 
 *      before attempting it.  Getting a null in appears to be very straight foward, but 
 *      I'm not a C# programmer so there are questions remaining on how to handle nulls 
 *      coming back out.
 *
 *  2024.07.19 - JLW
 *      Have come up with a better way to handle objects by creating actual classes for
 *      the various objects and attatching a Dictionary<string, Token> to it for
 *      user defined properties.  I should refresh myself on Interfaces and subclassing
 *      but quite frankly, I'm not interested.  I'll leave that to a later time or for 
 *      someone who understands C# much better than me.  Right now I'll just be glad to 
 *      get it working.  Object definitions will be in JAXObjectsAux.cs
 *      
 *  2025.04.07 - JLW
 *      Have placed this into the JAXBase environment and am adding a decimal
 *      width property to simple token definition so that it will act more like
 *      other xBase flavors when it's displaying numbers.  The match system will 
 *      need an update to correctly set the dec value of all answers.  The dec 
 *      field is updated when a value is placed into the simple token or manually 
 *      from the math system.
 *      
 *      I'm also changing all updating references to JAX, and started a language
 *      reference manual.  Need to start a technical reference manual too.  There
 *      are things that need to be kept straight and I'm constantly going back
 *      to my coded comments to know what I need to do next.
 *      
 *      I've found several logic errors so far since I have an interpreter, allowing
 *      me to probe memory and test commands as we go along.  I'm really sad that
 *      C# doesn't have a Windows form that acts like other xBase flavors where 
 *      you can just print to it and it acts like a screen.  This means that IDE is 
 *      going to have to bet converted, along with everything else, to C, C++, RUST, 
 *      or some other very robust (and faster than C#) language.
 *      
 *      I could open up a console or a (better) teminal window, and deal with the
 *      screen manipulation that way, but I'm not ready to go that route.
 *      
 * 
 ******************************************************************************************************************************************/

using JAXBase.XBase;
using JAXBase.Utilities.Utilities;

namespace JAXBase.Core
{
    public class JAXObjects
    {
        // This dictionary contains all token names and values for this object
        public readonly Dictionary<string, Token> jaxObject = [];
        private bool AllowNew = true;

        public List<string> GetVarNames()
        {
            List<string> varNames = [];

            foreach (KeyValuePair<string, Token> kvp in jaxObject)
                varNames.Add(kvp.Key);

            varNames.Sort();
            return varNames;
        }

        public List<string> GetObjectNames()
        {
            List<string> varNames = [];

            foreach (KeyValuePair<string, Token> kvp in jaxObject)
            {
                if (kvp.Value.Element.Type.Equals("O"))
                    varNames.Add(kvp.Key);
            }

            varNames.Sort();
            return varNames;
        }

        /*
         * The simple token is the basic building block that holds one value
         * so an array is a List of simple tokens and an object is a
         * Dictionary of simple tokens
         * 
         * VFP Variable Limits vs JAXBase                                                   JAXBase
         *      Highest/Lowest decimal without loss:        +/-99,999,999,999,999.99        9,999,999,999,999.99 (double)
         *      Highest/Lowest whole number without loss:   +/-999,999,999,999,999          999,999,999,999,999 (double)    9,223,372,036,854,775,807 (long)
         *      
         *      Highest Date:   12/31/9999                                                  
         *      Lowest Date:    01/01/0001                                                   
         * 
        */
        public class SimpleToken
        {
            private object _value = false;          // Simple objects (string, numeric, bool, date, datetime)
            public string _setAsType { get; private set; } = string.Empty;

            public bool Instantiated { get; private set; } = true;

            public string PropertyName { get; private set; } = string.Empty;
            public string PropertyType { get; private set; } = string.Empty;        // P=property, M=method, E=Event, p=user property, m=user method
            public string PropertyProtection { get; private set; } = string.Empty;  // H=hidden, P=protected, U=public
            public string Type { get; private set; } = "L";


            public bool ReadOnly { get; private set; } = false;
            public int Dec { get; set; } = 0;                               // Number of decimal places for float/numeric/double/currency

            public string DevDebugMsg = string.Empty;

            public object? DefaultValue { get; private set; } = null;
            public bool HasChanged { get; private set; } = false;

            // Set up a simple token with read only and user property values
            public SimpleToken(string propertyName, bool readOnly, object? value, bool userProperty)
            {
                propertyName = propertyName.ToLower().Trim();

                if (!ReadOnly)
                {
                    PropertyType = userProperty ? "p" : "P";

                    if (value is null)
                        MakeNull();
                    else
                        Value = value;

                    ReadOnly = readOnly;

                    if (ReadOnly == false && DefaultValue is not null)
                        HasChanged = true;
                }
            }


            // Put a value into the simple token
            public SimpleToken(object? value)
            {
                if (!ReadOnly)
                {
                    if (value is null)
                        MakeNull();
                    else
                    {
                        Value = value;
                    }

                    if (DefaultValue is not null)
                        HasChanged = true;
                }
            }


            // Update with value and readonly flag
            public SimpleToken(bool readOnly, object? value)
            {
                if (!ReadOnly)
                {
                    if (value is null)
                        MakeNull();
                    else
                        Value = value;

                    ReadOnly = readOnly;
                }
            }

            public SimpleToken() { }


            // Used to set to a proper null token
            public void MakeNull()
            {
                if (!ReadOnly)
                {
                    Value = ".NULL.";
                    Type = "X";

                    if (DefaultValue is not null)
                        HasChanged = true;
                }
            }


            /*
             * Locks the element to a specific primitive data type.  Works with arrays!
             * Objects are JAXObjectWrapper and you can't lock in a specific type
             * of JAXObjectWrapper.
             */
            public void SetAsType(string type)
            {
                if (_setAsType.Length == 0)
                {
                    type = type.ToUpper() == "I" ? "N" : type.ToUpper();
                    _setAsType = type;

                    switch (type)
                    {
                        case "N": Value = 0; break;
                        case "D": Value = DateOnly.MinValue; break;
                        case "I": { Value = 0; Dec = 0; break; }
                        case "T": Value = DateTime.MinValue; break;
                        case "L": Value = false; break;
                        case "C": Value = string.Empty; break;
                        case "O":
                        case "*": Value = string.Empty; _setAsType = "O"; MakeNull(); break;
                        default: throw new Exception("1662|" + type);
                    }

                    HasChanged = false;
                }
            }


            // We want to have the ability to set default/original values
            // and to be able to track them as needed
            public void SetDefaultValue(object defaultValue) { SetDefaultValue(defaultValue, false); }

            public void SetDefaultValue(object? defaultValue, bool ok2Reset)
            {
                if (DefaultValue is null || ok2Reset == true)
                {
                    //Value = defaultValue;
                    DefaultValue = defaultValue;
                    HasChanged = false;
                }
                else
                    throw new Exception("Cannot reset default value");
            }


            // Replaces the value with the existing non-null default
            // value, otherwise it replaces with an empty value
            public void SetToDefault()
            {
                if (DefaultValue is not null)
                {
                    Value = DefaultValue;
                    HasChanged = false;
                }
                else
                {
                    switch (Type)
                    {
                        case "N": Value = 0; Dec = 0; break;
                        case "C": Value = ""; break;
                        case "D": Value = DateOnly.MinValue; break;
                        case "T": Value = DateTime.MinValue; break;
                        default: Value = false; break;
                    }
                }
            }


            // Returns an empty value for current value type
            // or null and receiving end needs to handle it
            // correctly, such as:
            //
            //  if (v.ValueAsEmpty() is null)
            //      v.MakeNull()
            //  else
            //      v.Element.Value=v.ValueAsEmpty();
            //
            public object? ValueAsEmpty()
            {
                return Type switch
                {
                    "N" => 0D,
                    "I" => 0,
                    "C" => string.Empty,
                    "D" => DateOnly.MinValue,
                    "T" => DateTime.MinValue,
                    "X" => null,
                    _ => false
                };
            }


            // Quick way to know an element is null
            public bool IsNull() { return Type.Equals("X"); }

            // Return the value as an integer
            public int ValueAsInt
            {
                get
                {
                    int ival;

                    if (Type.Equals("N"))
                        ival = Convert.ToInt32(_value);
                    else
                        if (int.TryParse(_value.ToString(), out ival) == false) ival = 0;

                    return ival;
                }
            }

            // Return the value as a double
            public double ValueAsDouble
            {
                get
                {
                    double ival;

                    if (Type.Equals("N"))
                        ival = Convert.ToDouble(_value);
                    else
                        if (double.TryParse(_value.ToString(), out ival) == false) ival = 0;

                    if (_setAsType.Equals("I")) ival = Convert.ToInt32(ival);
                    return ival;
                }
            }

            // Return the value as a DateTime
            public DateTime ValueAsDateTime
            {
                get
                {
                    if (DateTime.TryParse(_value.ToString(), out DateTime ival) == false) ival = DateTime.MinValue;
                    return ival;
                }
            }

            // Return the value as a DataOnly
            public DateOnly ValueAsDateOnly
            {
                get
                {
                    DateOnly dto;
                    if (Type.Equals("D"))
                        dto = (DateOnly)_value;
                    else if (Type.Equals("T"))
                        dto = DateOnly.FromDateTime((DateTime)_value);
                    else
                        if (DateOnly.TryParse(_value.ToString(), out dto) == false) dto = DateOnly.MinValue;
                    return dto;
                }
            }

            // Return the value as a bool
            public bool ValueAsBool
            {
                get
                {
                    bool ibool;
                    if (Type.Equals("L"))
                        ibool = (bool)_value;
                    else if (Type.Equals("N"))
                        ibool = (double)_value > 0;
                    else if (Type.Equals("C"))
                        ibool = ((string)_value).Equals(".T.");
                    else
                        ibool = false;

                    return ibool;
                }
            }

            // Return the value as a string
            public string ValueAsString
            {
                get
                {
                    string istring;

                    if (Type.Equals("X"))
                        istring = ".NULL.";
                    if (Type.Equals("L"))
                        istring = _value.ToString() ?? ".F.";
                    else if (Type.Equals("N"))
                        istring = _value.ToString() ?? "0";
                    else if (Type.Equals("D"))
                        istring = ((DateOnly)_value).ToString("dd-MM-yyyy");
                    else if (Type.Equals("T"))
                        istring = ((DateOnly)_value).ToString("dd-MM-yyyy HH:mm:ss");
                    else if (Type.Equals("C"))
                        istring = _value.ToString() ?? ".null.";
                    else
                        istring = string.Empty;

                    return istring;
                }
            }

            // Put in a Simple Token rather than an object
            public void SetWithSimpleToken(SimpleToken st)
            {
                if (st.Value == null)
                    MakeNull();
                else
                {
                    _value = st.Value.ToString() ?? string.Empty;
                    Type = st.Type;

                    // Set as Type integer sets Dec to 0 while
                    // all other numeric sets Dec to 2 allowing
                    // it to change as needed.
                    if (_setAsType.Equals("I") == false)
                        Dec = st.Dec > Dec ? st.Dec : Dec;
                }
            }

            // Get the value as it is meant to be
            public object Value
            {
                get
                {
                    object val = Type switch
                    {
                        "N" => ValueAsDouble,
                        "L" => ValueAsBool,
                        "D" => ValueAsDateOnly,
                        "T" => ValueAsDateTime,
                        _ => _value,
                    };

                    return val;
                }

                set
                {
                    try
                    {
                        DevDebugMsg = string.Empty;

                        if (value is null)
                        {
                            if (_setAsType.Length > 0 && _setAsType.Equals("O") == false) throw new Exception("1732|");
                            _value = ".NULL.";
                            Type = "X";
                        }
                        else
                        {
                            var v = value.GetType();
                            string sVarType = value.GetType().Name.ToLower();

                            if (_setAsType.Length > 0 && "*O".Contains(_setAsType))
                            {
                                sVarType = "O";
                            }

                            // Now handle what was given
                            switch (sVarType.ToLower())
                            {
                                case "char":
                                case "string":
                                    if (_setAsType.Length > 0 && _setAsType != "C") throw new Exception("1732|");
                                    Type = "C";    // String & Numeric
                                    _value = value;
                                    break;

                                case "int32":
                                    if (_setAsType.Length > 0 && "NI".Contains(_setAsType) == false) throw new Exception("1732|");
                                    Type = "N";
                                    _value = Convert.ToInt32(value);
                                    Dec = 0;
                                    break;

                                case "int64":   // Future LONG support
                                    if (_setAsType.Length > 0 && "NI".Contains(_setAsType) == false) throw new Exception("1732|");
                                    //if (setAsType.Length > 0 && setAsType != "N" && setAsType != "K") throw new Exception("1732|");
                                    Type = "N";
                                    _value = Convert.ToInt32(value);
                                    //_value = Convert.ToInt64(value);
                                    Dec = 0;
                                    break;

                                case "float":
                                case "decimal":
                                case "single":
                                    if (_setAsType.Length > 0 && "NI".Contains(_setAsType) == false) throw new Exception("1732|");
                                    Type = "N";
                                    if (_setAsType == "I")
                                        _value = Convert.ToInt64(_value);
                                    else
                                    {
                                        _value = Convert.ToDouble(value);

                                        // Get dec value
                                        string vs = string.Format("{0}", Convert.ToDouble(value) - System.Math.Truncate(Convert.ToDouble(value)) * 1000000000).TrimStart('.').TrimEnd('0');
                                        Dec = vs.Length;
                                    }
                                    break;

                                case "currency":
                                case "double":
                                    if (_setAsType.Length > 0 && "NI".Contains(_setAsType) == false) throw new Exception("1732|");
                                    Type = "N";
                                    if (_setAsType == "I")
                                        _value = Convert.ToInt32(_value);
                                    else
                                    {
                                        _value = Convert.ToDouble(value);

                                        // Get dec value
                                        string vs = string.Format("{0}", Convert.ToDouble(value) - System.Math.Truncate(Convert.ToDouble(value)) * 1000000000).TrimStart('.').TrimEnd('0');
                                        Dec = vs.Length;
                                    }
                                    break;

                                case "boolean":
                                    if (_setAsType.Length > 0 && _setAsType != "L") throw new Exception("1732|");
                                    _value = value;
                                    Type = "L";
                                    break;

                                case "dateonly":
                                    if (_setAsType.Length > 0 && _setAsType != "D") throw new Exception("1732|");
                                    _value = value;
                                    Type = "D";    // DateTime in format yyyy-MM-ddT00:00:00
                                    break;

                                case "datetime":
                                    if (_setAsType.Length > 0 && _setAsType != "T") throw new Exception("1732|");
                                    _value = value;
                                    Type = "T";    // DateTime in format yyyy-MM-ddTHH:mm:ss
                                    break;

                                // Support for objects
                                default:
                                    if (_setAsType.Length > 0 && "O*".Contains(_setAsType) == false) throw new Exception("1732|");

                                    if (sVarType.Equals("*"))
                                        MakeNull();
                                    else
                                        _value = value;

                                    Type = "O";
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _value = ".NULL.";
                        DevDebugMsg = ex.ToString();
                    }
                }
            }
        }


        // ======================================================================================
        // The token is the value holder and can contain a single simple token, a list
        // of simple tokens (an array), or a dictionary of simple tokens (an object).
        //
        // Not so hard to create an Array variable when you think about how to do it.
        // I'm pretty sure that in VFP, all arrays are one dimensional, but you can set
        // up two dimensions which then calculates which element you are referencing, then
        // just work with that element.  VFP may do a bit more than what is done here, but
        // this is designed to work with syntactically correct code so we don't need to
        // add all of the extra code to catch issues.
        //
        // ======================================================================================
        public class Token
        {
            public readonly List<SimpleToken> _avalue = [];                  // Needed for variables and Arrays

            // S - Simple Token
            // A - Array Token
            // O - Object Token
            // U - Unknown Token
            public string TType = "O";
            public string Alias = string.Empty;
            public int AppLevel = 0;            // 0=global
            public string Tag = "U";            // [U]ser (default), [N]ative, [I]nherited, in[H]erited user

            public int Row = 1;                 // row * col = total number of elements
            public int Col = 1;
            public int ElementNumber = 0;
            public string elementName = string.Empty;
            public string DevDebugMsg = string.Empty;

            // Return -1 if not an array, otherewise row*col
            public int Count { get { return TType.Equals("A") ? (Row == 0 ? 1 : Row) * Col : -1; } }
            public string BaseClass { get; private set; } = string.Empty;

            // ------------------------------------------------------------
            // These are for properties
            public string Assign = string.Empty;
            public string Access = string.Empty;
            public bool Changed = false;
            public bool ClassProperty = false;
            public bool Hidden = false;
            public string Info = string.Empty;
            public bool Inherited = false;
            public bool JAXObjectProperty = false;
            public string PropType = string.Empty;
            public bool Protected = false;
            public bool SpecialHandling = false;

            // Shortcut to create a Token with a specific value
            // and lock it as that type.
            public Token(string val, string setOnlyAsType)
            {
                SimpleToken tk = new();

                if ("CDLNTOP*".Contains(setOnlyAsType) == false) throw new Exception("1732|");
                if (setOnlyAsType.Equals("P") == false)
                    tk.SetAsType(setOnlyAsType);
                TType = "S";

                switch (setOnlyAsType)
                {
                    case "C":   // Character
                        tk.Value = val;
                        break;

                    case "D":   // DateOnly
                        if (DateOnly.TryParse(val, out DateOnly ddo) == false) ddo = DateOnly.MinValue;
                        tk.Value = ddo;
                        break;

                    case "I":   // Integer
                        if (int.TryParse(val, out int ii) == false) ii = 0;
                        tk.Dec = 0;
                        tk.Value = ii;
                        break;

                    case "L":   // Logical
                        tk.Value = JAXLib.InListC(val, ".t.", "true");
                        break;

                    case "N":   // Numeric
                        if (double.TryParse(val, out double dd) == false) dd = 0D;
                        tk.Dec = 2;
                        tk.Value = dd;
                        break;

                    case "P":   // Protected - set the value and prevent any changes
                        tk.Value = val;
                        setOnlyAsType = tk.Type;
                        Protected = true;
                        break;

                    case "T":   // DateTime
                        if (DateTime.TryParse(val, out DateTime dt) == false) dt = DateTime.MinValue;
                        tk.Value = dt;
                        break;

                    case "O":   // JAXObjectWrapper - defaulted to O=.NULL. and *= empty array.
                        JAXObjectProperty = true;
                        break;

                    case "*":   // JAXObjectWrapper array must addressed using AddObject, SetObject, and RemoveObject
                        JAXObjectProperty = true;
                        TType = "A";
                        Row = 0;
                        Col = 0;
                        tk.MakeNull();
                        break;
                }

                _avalue.Add(tk);
            }
            // End of properties section ----------------------------------


            // Update an existing var as a type and initialize the value - all array elements are affected
            // *** Should only be used by DIMENSION, PUBLIC, LOCAL, and PRIVATE statements ***
            public void SetAsType(string setAsType)
            {
                for (int i = 0; i < _avalue.Count; i++)
                {
                    _avalue[i].SetAsType(setAsType);

                    switch (setAsType)
                    {
                        case "C":   // Character
                            _avalue[i].Value = string.Empty;
                            break;

                        case "D":   // DateOnly
                            _avalue[i].Value = DateOnly.MinValue;
                            break;

                        case "I":   // Integer
                        case "N":   // Numeric
                            _avalue[i].Value = 0;
                            break;

                        case "L":   // Logical
                            _avalue[i].Value = false;
                            break;

                        case "T":   // DateTime
                            _avalue[i].Value = DateTime.MinValue;
                            break;

                        case "O":   // JAXObjectWrapper - defaulted to O=.NULL. and *= empty array.
                            _avalue[i].MakeNull();
                            break;
                    }
                }
            }

            // Initially set up the Array with one element
            // because the first element is used by
            // non-array variables. An array of SimpleTokens
            // is controlled in this class.
            public Token()
            {
                // Token element value defaults to FALSE
                SimpleToken tk = new();
                _avalue.Add(tk);
                TType = "S";
            }

            // Shortcut to set up a token with a value
            public Token(object? val)
            {
                SimpleToken stk = new();
                if (val != null)
                    stk.Value = val;
                else
                    stk.MakeNull();

                _avalue.Add(stk);
                TType = "S";
            }

            public void SetDimension(int row, int col, bool makeArray)
            {
                if (TType.Equals("A") || makeArray)
                {
                    Row = row;
                    Col = col;
                    while (_avalue.Count < (row < 1 ? 1 : Row) * col)
                        _avalue.Add(new SimpleToken());

                    while (_avalue.Count > (row < 1 ? 1 : Row) * col)
                        _avalue.RemoveAt(_avalue.Count - 1);

                    TType = "A";
                }
            }

            public void CopyFrom(JAXObjects.Token sourceTK)
            {
                if (Protected)
                    throw new Exception("3027|");   // Can't overwrite
                else
                {
                    // Copy the value(s)
                    if (sourceTK.TType.Equals("A"))
                    {
                        SetDimension(sourceTK.Row, sourceTK.Col, true);
                        for (int i = 0; i < _avalue.Count; i++)
                            _avalue[i].Value = sourceTK._avalue[i].Value;
                    }
                    else
                    {
                        TType = sourceTK.TType;
                        _avalue[0].Value = sourceTK._avalue[0].Value;
                    }

                    // Copy the properties
                    Alias = sourceTK.Alias;
                    ClassProperty = sourceTK.ClassProperty;
                    Changed = sourceTK.Changed;
                    Hidden = sourceTK.Hidden;
                    Info = sourceTK.Info;
                    Protected = sourceTK.Protected;
                    PropType = sourceTK.PropType;
                    Tag = sourceTK.Tag;
                }
            }

            // Element setter/getter based on element number
            public SimpleToken Element
            {
                get
                {
                    SimpleToken? tResult = null;

                    try
                    {
                        DevDebugMsg = string.Empty;

                        //if ("SA".Contains(TType))
                        //{
                        //    if (elementNumber < _avalue.Count)
                        //        tResult = _avalue[elementNumber];
                        //}
                        //else
                        if (TType.Equals("O"))
                        {
                            // Does the propertyname exist?
                            for (int i = 0; i < _avalue.Count; i++)
                            {
                                if (_avalue[i].PropertyName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                                {
                                    tResult = _avalue[i];
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (ElementNumber < _avalue.Count)
                                tResult = _avalue[ElementNumber];
                        }
                    }
                    catch (Exception ex)
                    {
                        DevDebugMsg = ex.Message;
                    }

                    tResult ??= new();

                    return tResult;
                }

                set
                {
                    try
                    {
                        DevDebugMsg = string.Empty;
                        bool lSet = false;

                        if ("SA".Contains(TType))
                        {
                            if (_avalue.Count == 0)
                                _avalue.Add(new SimpleToken());
                            _avalue[ElementNumber] = value;
                            lSet = true;
                        }
                        else if (TType.Equals("O"))
                        {
                            // Does the propertyname exist?
                            for (int i = 0; i < _avalue.Count; i++)
                            {
                                if (_avalue[i].PropertyName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (_avalue[i].ReadOnly)
                                        throw new Exception("JAXERR:1757");
                                    else
                                    {
                                        _avalue[i] = value;
                                        lSet = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (lSet == false)
                        {
                            throw new Exception("Did not set");
                        }
                    }
                    catch (Exception ex)
                    {
                        DevDebugMsg = ex.Message;
                    }

                }
            }

            // Special internal methods for 1D object arrays
            // used by the system, such as the Objects property
            public void Add(JAXObjectWrapper value)
            {
                if (TType.Equals("A") && Row < 2)
                {
                    SimpleToken newst = new(value);

                    if (_avalue.Count == 1 && Col == 0)
                        _avalue[0] = newst;
                    else
                        _avalue.Add(newst);

                    Row = 1;
                    Col = _avalue.Count;
                }
                else
                    throw new Exception("1921");
            }

            public void RemoveAt(int idx)
            {
                if (TType.Equals("A") && Row == 1 && idx < _avalue.Count && _avalue[idx].Type.Equals("O"))
                {
                    _avalue.RemoveAt(idx);
                    Col = _avalue.Count;
                }
                else
                    throw new Exception("1921|");
            }
            // End object special methods for object arrays

            // Return token value as a string
            public string AsString()
            {
                string sVal = Element.Value.ToString() ?? "";
                string sType = Element.Type;

                string sResult = sVal;

                if (TType.Equals("O"))
                    sResult = "{Object}";
                else if (TType.Equals("R"))
                    sResult = "{Object:DataRow}";
                else
                {
                    if (sType.Equals("L"))
                        sResult = AsBool() ? ".T." : ".F.";
                    else if ("DT".Contains(sType))
                    {
                        if (DateTime.TryParse(sVal, out DateTime dtVal) == false)
                            dtVal = DateTime.MinValue;

                        if (dtVal == DateTime.MinValue)
                            sResult = sType.Equals("D") ? "{  /  /  }" : "{// ::}";
                        else
                            sResult = sType.Equals("D") ? "{" + dtVal.ToString()[..10] + "}" : "{" + dtVal.ToString() + "}";
                    }
                }

                return sResult;
            }

            // Return the token value as a bool
            public bool AsBool()
            {
                bool bResult = false;
                string sVal;

                if (Element.Value is null)
                    sVal = string.Empty;
                else
                    sVal = Element.Value.ToString() ?? string.Empty;

                switch (Element.Type)
                {
                    case "C":
                        bResult = sVal.Equals("0") || sVal.Equals(".T.");
                        break;

                    case "N":
                        if (double.TryParse(sVal, out double dVal))
                            bResult = dVal != 0;
                        else
                            bResult = false;
                        break;

                    case "L":
                        bResult = sVal.Equals("True");
                        break;

                    default:
                        Console.WriteLine(string.Format("Runtime error #99903: Value type '{0}' will not convert to a boolean value", Element.Type));
                        break;
                }

                return bResult;
            }


            // 
            public void CreateJAXObject(string baseClass)
            {
                AddElement("baseclass", baseClass, true, false);
                TType = "O";
            }

            public void SetElement(string propertyName)
            {
                if (TType.Equals("O"))
                {
                    // Does the propertyname exist?
                    elementName = string.Empty;

                    for (int i = 0; i < _avalue.Count; i++)
                    {
                        if (_avalue[i].PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            elementName = propertyName.ToLower();
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(elementName))
                        throw new Exception(string.Format("JAXERR:1734,{0}", propertyName));
                }
                else
                    throw new Exception("JAXERR:1924");
            }

            public void AddElement(string propertyName, object? val, bool readOnly, bool userProperty)
            {
                propertyName = propertyName.ToLower();

                if (TType.Equals("O") == false)
                    TType = "O";

                for (int i = 0; i < _avalue.Count; i++)
                {
                    if (_avalue[i].PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        throw new Exception(string.Format("JAXERR:1763,Property {0} already exists", propertyName));
                }

                SimpleToken newst = new(propertyName, readOnly, val, userProperty);         // If read only, value will not be changeable from this point on
                _avalue.Add(newst);
            }

            public bool RemoveElement(string propertyName)
            {
                bool lSet = false;
                propertyName = propertyName.ToLower();

                if (TType.Equals("O"))
                {
                    for (int i = 0; i < _avalue.Count; i++)
                    {
                        if (_avalue[i].PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (_avalue[i].PropertyType.Equals("p"))
                            {
                                // Remove user property or method
                                _avalue.RemoveAt(i);
                                lSet = true;
                                break;
                            }
                            else
                            {
                                if (_avalue[i].PropertyType.Equals("P"))
                                    throw new Exception("Cannot remove base property");
                                else
                                    throw new Exception("Cannot remove a method or event");
                            }
                        }
                    }
                }
                else
                    throw new Exception("JAXERR:1924");         // not an object

                return lSet;
            }


            // This is 1 based so that it's easier to implement with JAX
            public void SetElement(int r, int c)
            {
                //int iElement = TType.Equals("A") || r * c == 1 ? (r - 1) * Col + c : r;
                int iElement = TType.Equals("A") ? ((r == 0 ? 0 : r - 1) * Col + c) : 1;
                if (iElement < 1 || iElement > (Row < 1 ? 1 : Row) * Col)
                    throw new Exception("31|");
                else
                    ElementNumber = iElement - 1;
            }

            public int AsInt()
            {
                int iResult = 0;

                string temp = Element.Value.ToString() ?? string.Empty;
                if (double.TryParse(temp, out double dResult) == false)
                    iResult = 0;
                else
                    iResult = Convert.ToInt32(dResult);
                return iResult;
            }

            public long AsLong()
            {
                if (long.TryParse(Element.Value.ToString(), out long lResult) == false)
                    lResult = 0;
                return lResult;
            }

            public float AsFloat()
            {
                if (float.TryParse(Element.Value.ToString(), out float fResult) == false)
                    fResult = 0;
                return fResult;
            }

            public decimal AsDecimal()
            {
                if (decimal.TryParse(Element.Value.ToString(), out decimal fResult) == false)
                    fResult = 0;
                return fResult;
            }

            public double AsDouble()
            {
                if (double.TryParse(Element.Value.ToString(), out double dResult) == false)
                    dResult = 0;
                return dResult;
            }

            public DateOnly AsDate()
            {
                string sVal = Element.Value.ToString() ?? "";
                if (DateOnly.TryParse(sVal[..10], out DateOnly doResult))
                    doResult = DateOnly.MinValue;
                return doResult;
            }

            public DateTime AsDateTime()
            {
                string sVal = Element.Value.ToString() ?? "";
                if (DateTime.TryParse(sVal[..10], out DateTime dtResult))
                    dtResult = DateTime.MinValue;
                return dtResult;
            }
        }


        public void SetAllowNew(bool allow)
        {
            AllowNew = allow;
        }


        public void SetToken(string varName, Token tk)
        {
            if (jaxObject.ContainsKey(varName.ToLower()))
                jaxObject[varName.ToLower()] = tk;
            else
                throw new Exception(string.Format("Variable {0} does not exists", varName.ToUpper()));
        }


        // Add a variable to the dictionary
        private void AddVarWithSimpleToken(string varName, SimpleToken value)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false && AllowNew)
            {
                SimpleToken st = new();
                if (value is null)
                    st.MakeNull();
                else
                    st.Value = value;

                Token tk = new();
                tk.Element.SetWithSimpleToken(st);
                jaxObject.Add(varName.ToLower(), tk);
            }
        }

        // Add a variable to the dictionary
        private void AddVar(string varName, object? value)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false && AllowNew)
            {
                SimpleToken st = new();
                if (value is null)
                    st.MakeNull();
                else
                    st.Value = value;

                Token tk = new();
                if (value is null)
                    tk.Element.MakeNull();
                else
                    tk.Element.Value = value;

                jaxObject.Add(varName.ToLower(), tk);
            }
        }

        // Add a variable to the dictionary
        private void AddSystemVar(string varName, object? value)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false && AllowNew)
            {
                SimpleToken st = new();
                if (value is null)
                    st.MakeNull();
                else
                    st.Value = value;

                Token tk = new();
                if (value is null)
                    tk.Element.MakeNull();
                else
                    tk.Element.Value = value;

                jaxObject.Add(varName.ToLower(), tk);
            }
        }

        public void SetDimension(string varName, int rows, int col, bool alterArray)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false)
            {
                if (AllowNew)
                    AddVar(varName.ToLower(), false);
                else
                    throw new Exception("9999,Invalid Set setting");
            }

            if (alterArray)
            {
                SetDimension(varName.ToLower(), rows, col);
                jaxObject[varName.ToLower()].TType = (rows < 1 ? 1 : rows) * col > 1 ? "A" : "S";
            }
        }

        // Set two dimensions to the array
        public void SetDimension(string varName, int r, int c)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false)
            {
                if (AllowNew)
                    AddVar(varName.ToLower(), false);
                else
                    throw new Exception($"3029|{varName}");
            }

            SetDimension(jaxObject[varName.ToLower()], r, c);
        }

        // Initialize the dimensions for the array
        public void SetDimension(Token tk, int r, int c)
        {
            tk.SetDimension(r, c, true);
        }



        // Set the value of the current element
        public void SetValue(string varName, object? value)
        {
            if (jaxObject.ContainsKey(varName.ToLower()) == false)
            {
                if (AllowNew)
                    AddVar(varName.ToLower(), value);
                else
                    throw new Exception("3029,{varName}");
            }

            if (value is null)
                jaxObject[varName.ToLower()].Element.MakeNull();
            else
                jaxObject[varName.ToLower()].Element.Value = value;
        }

        public void SetValueWithSimpleToken(string varName, SimpleToken stoken, int e)
        {
            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], e);
                jaxObject[varName.ToLower()].Element.SetWithSimpleToken(stoken);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9909 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }
        }

        // Set the value of an array element
        public void SetValue(string varName, object? value, int e)
        {
            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], e);
                SetValue(varName.ToLower(), value);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9909 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }
        }

        // Set the value of an 2 dimensioned element
        public void SetValueWithSimpleToken(string varName, SimpleToken stoken, int row, int col)
        {
            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], row, col);
                jaxObject[varName.ToLower()].Element.SetWithSimpleToken(stoken);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9909 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }
        }


        // Set the value of an 2 dimensioned element
        public void SetValue(string varName, object? value, int row, int col)
        {
            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], row, col);
                SetValue(varName.ToLower(), value);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9909 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }
        }

        // Get the value of element 1
        public string GetValue(string varName) { return GetValue(varName, 1); }

        // Get the value of a 2D element
        public string GetValue(string varName, int row, int col)
        {
            string sResult = string.Empty;

            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], row, col);
                sResult = jaxObject[varName.ToLower()].Element.Value.ToString() ?? string.Empty;
            }

            return sResult;
        }

        // Get the value of a 1D element
        public string GetValue(string varName, int e)
        {
            string sResult = string.Empty;

            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], e);
                sResult = jaxObject[varName.ToLower()].Element.Value.ToString() ?? string.Empty;
            }

            return sResult;
        }

        // Set up which element is going to be referenced based on 1 dimension
        public void SetElement(Token tk, int e)
        {
            if (tk.TType.Equals("A") || e == 1)
            {
                if (e > 0 && e <= (tk.Row < 1 ? 1 : tk.Row) * tk.Col)
                {
                    tk.ElementNumber = e - 1;
                }
                else
                    throw new Exception(string.Format("Runtime Error #9910 - Invalid array element"));
            }
            else
            {
                // Not an array
                throw new Exception(string.Format("Runtime Error #9911 - Not an array"));
            }
        }


        // Set up which element is going to be referenced based on 2 dimenions
        // R & C are 1 based so you need to subtract 1 from the result to 
        // get the right element number
        public void SetElement(Token tk, int r, int c)
        {
            tk.ElementNumber = 0;

            if (r > 0 && c < 1)
            {
                // Set for 1D
                int i = r;
                r = c;
                c = i;
            }

            if (tk.TType.Equals("A") || ((r < 1 ? 1 : r) * c == 1))
            {
                tk.ElementNumber = 0;

                if (r == 0 && c > 0)
                {
                    // By element reference
                    if (r <= (tk.Row < 1 ? 1 : tk.Row) * tk.Col)
                        tk.ElementNumber = c - 1;
                    else
                    {
                        throw new Exception(string.Format("Runtime Error #9913 - Referencing past end of array"));
                    }
                }
                else if (r > 0 && c > 0)
                {
                    // 2 dimension array reference
                    if (r * c > 0 || r * c <= tk.Row * tk.Col)
                    {
                        // Valid location
                        tk.ElementNumber = (r - 1) * tk.Col + c - 1;
                    }
                    else
                    {
                        throw new Exception(string.Format("Runtime Error #9912 - Invalid column dimension"));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Runtime Error #9912 - Invalid column dimension"));
                }
            }
            else
            {
                // Not an array
                throw new Exception(string.Format("Runtime Error #9914 - Not an array"));
            }
        }


        // Get the simple token of the current element
        public SimpleToken GetElement(string varName)
        {
            SimpleToken oResult;

            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                if ("SA".Contains(jaxObject[varName.ToLower()].TType))
                {
                    // SimpleToken or Array Element
                    oResult = jaxObject[varName.ToLower()].Element;
                }
                else
                {
                    // Object handling - TODO
                    oResult = jaxObject[varName.ToLower()].Element;
                }
            }
            else
            {
                //throw new Exception(string.Format("Runtime Error #9915 - Variable or Object '{0}' does not exist", varName.ToLower()));
                oResult = new SimpleToken(varName);
            }

            return oResult;
        }

        // Get the simple token of a element in 1 dimension
        public SimpleToken GetElement(string varName, int e)
        {
            SimpleToken oResult;

            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], e);
                oResult = GetElement(varName);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9915 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }

            return oResult;
        }

        // Get the simple token of a element in 2 dimensions
        public SimpleToken GetElement(string varName, int r, int c)
        {
            SimpleToken oResult;

            if (jaxObject.ContainsKey(varName.ToLower()))
            {
                SetElement(jaxObject[varName.ToLower()], r, c);
                oResult = GetElement(varName);
            }
            else
            {
                throw new Exception(string.Format("Runtime Error #9915 - Variable or Object '{0}' does not exist", varName.ToLower()));
            }

            return oResult;
        }


        // Get the token of a variable name
        public Token GetToken(string varName)
        {
            // Assume we don't find it
            Token oToken = new()
            {
                TType = "U",
                Row = 0,
                Col = 0
            };

            string var = varName.ToLower();
            string property = string.Empty;

            if (var.Contains('.'))
            {
                // Dealing with possible object
                string[] varParts = var.Split('.');
                var = varParts[0];
                property = varParts[1];
            }

            if (jaxObject.ContainsKey(var))
            {
                jaxObject[var].ElementNumber = 0;
                Token oToken1 = jaxObject[var];

                if (string.IsNullOrEmpty(property) == false)
                {
                    // definitely looking for an object
                    if (oToken1.TType.Equals("O"))
                    {
                        for (int i = 0; i < oToken1._avalue.Count; i++)
                        {
                            if (oToken1._avalue[i].PropertyName.Equals(property, StringComparison.OrdinalIgnoreCase))
                            {
                                oToken = oToken1;
                                oToken.elementName = property;
                                oToken.ElementNumber = i;
                                break;
                            }
                        }
                    }
                }
                else
                    oToken = jaxObject[var];
            }

            return oToken;
        }


        // ---------------------------------------------------------------------
        // Get the VarType of this named object
        // ---------------------------------------------------------------------
        public string VarType(string varName)
        {
            string sType = string.Empty;

            if (string.IsNullOrEmpty(varName))
            {
                // Toss a runtime error
                Console.WriteLine("Runtime error #1221: No variable name provided");
            }
            else
            {
                if (jaxObject.ContainsKey(varName.ToLower()))
                {
                    // Found it, return the type
                    sType = jaxObject[varName.ToLower()].Element.Type;
                }
                else
                {
                    // Unknown variable
                    sType = "U";
                }
            }

            return sType;
        }


        // Release a variable from the list
        public void Release(string varName)
        {
            if (string.IsNullOrEmpty(varName))
            {
                // Toss a runtime error
                Console.WriteLine("Runtime error #1221: No variable name provided");
            }
            else
            {
                // Found it, return the type
                jaxObject.Remove(varName.ToLower());
            }
        }
    }
}
