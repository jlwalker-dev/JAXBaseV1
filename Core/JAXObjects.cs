/*****************************************************************************************************************************************
 * 
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
 * 2025.04.10 - JLW
 *      First major upgrade to this code which will allow the support of the
 *      LIST and LISTITEM properties in ListBox and ComboBox classes.  In
 *      addition, making it possible to very straight forward to support the 
 *      JAXBase Collection class.
 *      
 *      New var types are S for SortedDictionary (Collection class support) and
 *      M for MappedList (for ListItem property).  The ObservableSortedDictioary
 *      is required in the ComboBox and ListBox classes to support the ListItem
 *      property.
 *      
 *      Removed a lot of extra code that I thought would be needed when I first
 *      built this class, but in the long run found I over engineered by a country
 *      mile, as I have been known to do from time to time.
 *      
 ******************************************************************************************************************************************/

using JAXBase.Utilities;
using JAXBase.XBase;

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
            public object? oldValue { get; private set; } = false;

            // Fired whenever the Value property is successfully written to (after type handling)
            public event EventHandler? ValueChanged;

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
                        case "E": Value = 0; Dec = 0; break;
                        case "I": { Value = 0; Dec = 0; break; }
                        case "T": Value = DateTime.MinValue; break;
                        case "L": Value = false; break;
                        case "M": Value = 0; Dec = 0; break;
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
                        oldValue = (IsNull()) ? null : _value;   // capture before change (optional but useful)

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

                        // === MINIMAL ADDITION: Fire the event after successful assignment ===
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                    catch (Exception ex)
                    {
                        _value = ".NULL.";
                        DevDebugMsg = ex.ToString();
                    }
                }
            }
        }


        /* ======================================================================================
         * The token is the value holder and can contain a single simple token, a list
         * of simple tokens (an array), or a dictionary of simple tokens (an object).
         *
         * Not so hard to create an Array variable when you think about how to do it.
         * I'm pretty sure that in VFP, all arrays are one dimensional, but you can set
         * up two dimensions which then calculates which element you are referencing, then
         * just work with that element.  VFP may do a bit more than what is done here, but
         * this is designed to work with syntactically correct code so we don't need to
         * add all of the extra code to catch issues.
         * ====================================================================================== */
        public class Token
        {
            public readonly List<SimpleToken> _avalue = [];                     // Needed for variables and Arrays
            public ObservableSortedDictionary<int, Token>? _dictionary = null;
            public List<int>? _mappedList = null;

            // used for the dictionary key if not provided
            public int ListItemID { get; private set; } = 0;

            // S - Simple Token
            // A - Array Token
            // D - Dictionary (MAP) variable
            // O - Object Token
            // U - Unknown Token
            public string TType = "O";
            public string Alias = string.Empty;
            public int AppLevel = 0;            // 0=global
            public string Tag = "U";            // [U]ser (default), [N]ative, [I]nherited, in[H]erited user

            public int Row = 1;                 // row * col = total number of elements
            public int Col = 1;
            public int ElementNumber = 0;
            public int ListNumber = 0;
            public int KeyNumber = 0;
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
            public Token(object val, string setOnlyAsType)
            {
                SimpleToken tk = new();

                if ("CDELMNTOP*".Contains(setOnlyAsType) == false) throw new Exception("1732|");
                if (setOnlyAsType.Equals("P") == false)
                    tk.SetAsType(setOnlyAsType);
                TType = "S";


                string sval = (val is Dictionary<int, Token>) ? "" : val.ToString() ?? "";

                switch (setOnlyAsType)
                {
                    case "C":   // Character
                        tk.Value = sval;
                        break;

                    case "D":   // DateOnly
                        if (DateOnly.TryParse(sval, out DateOnly ddo) == false) ddo = DateOnly.MinValue;
                        tk.Value = ddo;
                        break;

                    case "I":   // Integer
                        if (int.TryParse(sval, out int ii) == false) ii = 0;
                        tk.Dec = 0;
                        tk.Value = ii;
                        break;

                    case "L":   // Logical
                        tk.Value = JAXLib.InListC(sval, ".t.", "true");
                        break;

                    case "M":   // Mapped List tied to ObservableSortedDictionary
                        if (val is ObservableSortedDictionary<int, Token>)
                        {
                            _dictionary = (ObservableSortedDictionary<int, Token>)val;

                            _mappedList = [];
                            foreach (KeyValuePair<int, Token> dic in _dictionary)
                                _mappedList.Add(dic.Key);

                            // Not sure we need both of these
                            _dictionary.CollectionChanged += (sender, e) => { UpdateList(); };
                            TType = "M";
                        }
                        else
                            throw new Exception("8010|");
                        break;

                    case "N":   // Numeric
                        if (double.TryParse(sval, out double dd) == false) dd = 0D;
                        tk.Dec = 2;
                        tk.Value = dd;
                        break;

                    case "P":   // Protected - set the value and prevent any changes
                        tk.Value = val;
                        setOnlyAsType = tk.Type;
                        Protected = true;
                        break;

                    case "E":   // Sorted Dictionary
                        TType = "E";
                        Row = 0;
                        Col = 0;
                        _dictionary = val is ObservableSortedDictionary<int, Token> ? (ObservableSortedDictionary<int, Token>)val : [];   // A dictionary uses a lot more RAM, so only instantiate when needed
                        break;

                    case "T":   // DateTime
                        if (DateTime.TryParse(sval, out DateTime dt) == false) dt = DateTime.MinValue;
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

                        case "E":   // Sorted Dictionary
                            if (_avalue[i].Value is not JAXObjectWrapper)
                                _avalue[i].MakeNull();
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

            /// <summary>
            /// When a change to the observable dictionary occurs, control is sent
            /// here to update the list, making as few changes as possible.
            /// </summary>
            private void UpdateList()
            {
                if (_dictionary is null || _mappedList is null)
                {
                    // create a new list
                    _mappedList = [];
                    _dictionary = [];

                    foreach (KeyValuePair<int, Token> dic in _dictionary)
                        _mappedList.Add(dic.Key);
                }
                else
                {
                    // reorganize the list
                    int i = 0;
                    foreach (KeyValuePair<int, Token> dic in _dictionary)
                    {
                        if (i == _mappedList.Count)         // Need to add a key
                        {
                            _mappedList.Add(dic.Key);
                            i++;
                        }
                        else if (_mappedList[i] > dic.Key)  // Need to insert the key
                            _mappedList.Insert(i, dic.Key);
                        else if (_mappedList[i] < dic.Key)  // Need to remove obsolete keys
                        {
                            while (_mappedList[i] < dic.Key)
                                _mappedList.Remove(i);
                        }
                        else
                            i++;    // MappedList == key    // Same so do nothing
                    }
                }
            }

            /*
             * Initially set up the Array with one element because the 
             * first element is used by non-array variables. An array 
             * of SimpleTokens is controlled in this class.
             */
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


            /*
             * Remove a key from the sorted dictionarly
             */
            public void RemoveItem(int rowKey)
            {
                if (TType.Equals("E"))
                {
                    // Do we have a valid dictionary object?
                    if (_dictionary is null)
                        throw new Exception("8000|");

                    // Does this key already exist?
                    if (_dictionary.ContainsKey(rowKey))
                        _dictionary.Remove(rowKey);
                    else
                        throw new Exception("8001|");    // Key does not exist
                }
                else
                    throw new Exception("8102|");        // Not a sorted dictionary
            }


            /*
             * Clear the TOKEN depending on type
             */
            public void Clear()
            {
                if ("EM".Contains(TType))
                {
                    _dictionary!.Clear();
                    _mappedList!.Clear();
                    ListItemID = 0;
                }
                else if (TType.Equals("S"))
                {
                    switch (Element.Type)
                    {
                        case "N": Element.Value = 0; break;
                        case "D": Element.Value = DateOnly.MinValue; break;
                        case "E": Element.Value = 0; Element.Dec = 0; break;
                        case "I": { Element.Value = 0; Element.Dec = 0; break; }
                        case "T": Element.Value = DateTime.MinValue; break;
                        case "L": Element.Value = false; break;
                        case "M": Element.Value = 0; Element.Dec = 0; break;
                        case "C": Element.Value = ""; break;
                        case "O": Element.MakeNull(); break;
                    }
                }
                else if (TType.Equals("A"))
                {
                    for (int i = 0; i < _avalue.Count; i++)
                        _avalue[i].Value = false;
                }
            }


            /*
             * Add a new itemID to the dictionary and set the token value as an array if appropriate
             */
            public void AddItemID(string cItem, int rowKey, int Column)
            {
                if (TType.Equals("E"))
                {
                    // If the row key is not provided, create the next available
                    if (rowKey < 1)
                    {
                        while (_dictionary!.ContainsKey(++ListItemID)) ;
                        rowKey = ListItemID;
                    }

                    // Do we have a valid dictionary object?
                    if (_dictionary is null)
                        throw new Exception("8100|");

                    // Does this key already exist?
                    if (_dictionary.ContainsKey(rowKey))
                        throw new Exception("8101|");

                    // Create the token with dimension if appropriate
                    JAXObjects.Token tk = new();

                    //if (Column != Col)
                    //    tk.SetDimension(1, Col, true);

                    if (Column > Col)
                        tk.SetDimension(1, Column, true);

                    if (_dictionary.ContainsKey(rowKey))
                    {
                        // Update the key/token pair
                        _dictionary[rowKey].SetElement(1, Column);
                        _dictionary[rowKey].Element.Value = cItem;
                    }
                    else
                    {
                        // Add the new key/token pair
                        tk.SetElement(1, Column);
                        tk.Element.Value = cItem;
                        _dictionary.Add(rowKey, tk);
                    }
                }
                else
                    throw new Exception("8102|");    // Not a sorted dictionary
            }



            /*
             * Add or insert a new item to the dictionary and array. If index==0 or is greater than the 
             * count, it adds to the end of the list, otherwise it inserts at the index and moves the 
             * rest of the items down. Column indicates which column of the token value to set.
             */
            public void AddItem(string cItem, int Index, int Column, int rowKey)
            {
                if (TType.Equals("M"))
                {
                    // If the row key is not provided, create the next available
                    if (rowKey < 1)
                    {
                        while (_dictionary!.ContainsKey(++ListItemID)) ;
                        rowKey = ListItemID;
                    }

                    // Do we have a valid dictionary object?
                    if (_dictionary is null)
                        throw new Exception("8100|");

                    // Does this key already exist?
                    if (_dictionary.ContainsKey(rowKey))
                        throw new Exception("8101|");

                    // Create the token with dimension if appropriate
                    JAXObjects.Token tk = new();

                    if (Column != Col)
                        tk.SetDimension(1, Col, true);

                    // Add the new key/token pair
                    tk.SetElement(1, Column);
                    tk.Element.Value = cItem;
                    _dictionary.Add(rowKey, tk);

                    // If index < count, insert the new key into the mapped list at the appropriate
                    // spot, otherwise leave it at the end of the array
                    if (Index > 0 && Index < _mappedList!.Count)
                    {
                        int pos = _mappedList[^1];
                        _mappedList.RemoveAt(_mappedList.Count - 1);
                        _mappedList.Insert(Index, pos);
                    }
                }
                else
                    throw new Exception("8102|");    // Not a sorted dictionary
            }

            public void InsertItemAt(int pos, int rowkey, JAXObjects.Token tk)
            {
                _dictionary!.Add(rowkey, tk);
                _mappedList!.RemoveAt(_mappedList.Count - 1);
                _mappedList.Insert(pos, rowkey);
            }

            public void SetDimension(int row, int col, bool makeArray)
            {
                if (TType.Equals("M"))
                {
                    // Type M (Mapped List) is completely dependent on
                    // the dictionary, so changes only occur when the
                    // dictionary is updated
                    throw new Exception("8111|");
                }
                else if (TType.Equals("E"))
                {
                    // Dictionary
                    if (_dictionary is null)
                        throw new Exception("8100|");

                    // We're changing all elements of the dictionary
                    // to a 1D array with Col elements
                    Col = col;
                    foreach (KeyValuePair<int, Token> tk in _dictionary)
                        tk.Value.SetDimension(1, col, true);
                }
                else if (TType.Equals("A") || makeArray)
                {
                    Row = row;
                    Col = col;

                    if (row < 0 || col < 0)
                        throw new Exception("31|");

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
                        // Get the array
                        SetDimension(sourceTK.Row, sourceTK.Col, true);
                        for (int i = 0; i < _avalue.Count; i++)
                            _avalue[i].Value = sourceTK._avalue[i].Value;
                    }
                    else if (sourceTK.TType.Equals("E"))
                    {
                        // Get the dictionary
                        _dictionary = [];

                        if (sourceTK._dictionary is not null)
                        {
                            foreach (KeyValuePair<int, Token> source in sourceTK._dictionary)
                                _dictionary.Add(source.Key, source.Value);
                        }
                    }
                    else if (sourceTK.TType.Equals("M"))
                    {
                        _dictionary = [];

                        // Copy the mapped list
                        if (sourceTK._mappedList is not null && sourceTK._dictionary is not null)
                        {
                            _mappedList = [.. sourceTK._mappedList!];
                            foreach (KeyValuePair<int, Token> source in sourceTK._dictionary)
                                _dictionary.Add(source.Key, source.Value);
                        }
                        else
                            _mappedList = [];
                    }
                    else
                    {
                        // Get the 1 simple token
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
                        else
                        {
                            // Optional: propagate ValueChanged from the inner SimpleToken
                            if (value != null)
                            {
                                value.ValueChanged += (sender, args) =>
                                this.ElementValueChanged?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DevDebugMsg = ex.Message;
                    }

                }
            }

            // Optional event on Token that fires when its current Element's Value changes
            public event EventHandler? ElementValueChanged;

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
                string sVal = "EM".Contains(TType) ? "" : Element.Value.ToString() ?? "";
                string sType = "EM".Contains(TType) ? "C" : Element.Type;

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
                    throw new Exception("1924|");         // not an object

                return lSet;
            }


            // This is 1 based so that it's easier to implement with JAX
            public void SetElement(int r, int c)
            {
                if ("ME".Contains(TType))
                {
                    // Mapped List?
                    if (TType.Equals("M"))
                    {
                        if (_mappedList is null)
                            throw new Exception("8112|");
                        else
                        {
                            if (_mappedList.Count <= r)
                            {
                                ListNumber = r;
                                r = _mappedList[r]; // Set the dictionary key
                            }
                            else
                                throw new Exception("31|");
                        }
                    }

                    // Sorted Dictionary
                    if (_dictionary is null)
                        throw new Exception(TType == "E" ? "8100|" : "8112");
                    else
                    {
                        if (_dictionary.ContainsKey(r))
                        {
                            // Found key, now check columns
                            if (c > 0 && _dictionary[r].Col <= c)
                            {
                                KeyNumber = r;
                                ElementNumber = c - 1;
                            }
                        }
                        else
                            throw new Exception("8101|");
                    }
                }
                else
                {
                    // Var or Array
                    int iElement = TType.Equals("A") ? ((r == 0 ? 0 : r - 1) * Col + c) : 1;
                    if (iElement < 1 || iElement > (Row < 1 ? 1 : Row) * Col)
                        throw new Exception("31|");
                    else
                        ElementNumber = iElement - 1;
                }
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
        // ----- End of Token definition ------------------------------------------------------------

        /* ------------------------------------------------------------------------------------------
         * This section is used for Private/Public variable addressing
         * 
         * Public and Private tokens do not support type M or S variables
         * ------------------------------------------------------------------------------------------*/
        public void SetAllowNew(bool allow)
        {
            AllowNew = allow;
        }


        // Set an existing variable with a token
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


        // Position the pointer in the variable
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

                if (jaxObject[varName.ToLower()].TType.Equals("E") == false)
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

        // Get the value of current element
        public string GetValue(string varName)
        {
            return jaxObject[varName.ToLower()].Element.Value.ToString() ?? string.Empty;
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
