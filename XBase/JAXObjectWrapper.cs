/*--------------------------------------------------------------------------------------------------*
 * 2025.08.01 - JLW
 *      This class is used to create an object with the events needed to 
 *      make all classes act the same and be able to interface with the
 *      code in the same manner.
 *      
 * 2025.11.05 - JLW
 *      Coming down to the home stretch.  The only event that will be a
 *      problem right now is the destroy event which I expect can be
 *      solved using idispose, but I'll need to read up and test that
 *      out extensively before attempting it.
 *      
 * 2025.11.06 - JLW
 *      Converted over to new classes.  Lots of testing to do but they
 *      should be more bulletproof and will have a lot less code to break;
 *
 * 2025.11.07 - JLW
 *      Needed a few GROK sessions to help me figure things out.  But I've
 *      got a better grip on sub classing. 
 *      
 *--------------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    /*----------------------------------------------------------------------------------------------*
     * This wrapper provides the functionality to make all objects appear to work the
     * same, execute code in the events and methods, and otherwise make the classes
     * a unified object type that can be used across the system.  Just like you'd 
     * expect in an xBase language.
     *----------------------------------------------------------------------------------------------*/
    public class JAXObjectWrapper
    {
        public enum Protection { urd, Urd, uRd, URd, URD }


        // Tie in the parent Avalonia Window
        public Avalonia.Visual? ParentAvaloniaWindow = null;

        // Access to the global App class
        public readonly AppClass App;

        // The Visual/Nonvisual class object
        public readonly IJAXAvaClass? thisObject;

        // Quick references so we don't have to do a GetProperty
        // and so we can still handle the EMPTY baseclass correctly
        public JAXObjectWrapper? parent = null;
        public JAXObjectWrapper? Parent
        {
            get { return parent; }
            set
            {
                // Set up the parent related references
                parent = value;

                if (parent is not null)
                {
                    ParentClass = parent.Class;
                    THISFORM = parent.THISFORM;
                    THISFORMSET = parent.THISFORMSET;
                }
                else
                {
                    ParentClass = string.Empty;
                    THISFORM = null; ;
                    THISFORMSET = null;
                }
            }
        }

        public string ParentClass { get; private set; } = string.Empty;


        private string jaxclass = string.Empty;
        public string Class
        {
            get { return jaxclass; }
            set
            {
                // You can't reset the class type
                if (string.IsNullOrWhiteSpace(jaxclass)) jaxclass = value;
            }
        }
        public string JOWName { get; private set; } = string.Empty;

        private string baseclass = string.Empty;
        public string BaseClass
        {
            get { return baseclass; }
            set
            {
                // You can't reset a base class name
                if (string.IsNullOrWhiteSpace(baseclass)) baseclass = value;
            }
        }

        public int IDX = 0;

        private string classid = string.Empty;
        public string ClassID
        {
            get { return classid; }
            set
            {
                // You can't reset the classid
                if (string.IsNullOrWhiteSpace(classid)) classid = App.SystemCounter();
            }
        }

        // Visual class flag and object
        public bool VisualClass = false;
        //public System.Windows.Forms.Control? visualObject = null;
        public Avalonia.Controls.Control? avaloniaObject = null;
        //public Avalonia.Controls.Primitives.TemplatedControl? avaloniaObject = null;
        public object? nvObject = null;

        public int AnchorValue = 0;

        public JAXObjectWrapper THIS;
        public JAXObjectWrapper? THISFORM = null;
        public JAXObjectWrapper? THISFORMSET = null;

        // Used to prevent incorrect clearing of AError array by
        // indicating that we are in a multi-method transaction.
        public bool InTransaction = false;

        public Protection Protected = Protection.URD;


        // This section is for flags and properies that are used
        // by the visual classes to track changes and update the
        // visual objects as needed.
        // ---------------------------------------------------------

        // Used for controls that don't have normal validation events
        // such as comboboxes and listboxes.
        public bool Validated = false;
        public int ValidMoveDirection = 0;

        // TODO - Move direction tracking for visual classes.
        // Used to track move direction for visual classes that
        // don't have built in move direction tracking, such as grids.
        public int MoveDirection = 0;

        // Original position and size properties for visual classes
        public double originalHeight = 0;
        public double originalLeft = 0;
        public double originalTop = 0;
        public double originalWidth = 0;



        public JAXObjectWrapper(AppClass app, string cClass, string cName, List<ParameterClass>? parameterList)
        {
            App = app;
            Class = cClass;
            THIS = this;
            string lastProp;

            int CurrentErrCount = Program.CurrentApp.Errors.Count;
            int Err = 0;
            string msg = string.Empty;

            // ClearErrors() must not cleared when in a transaction except for 
            // the transaction that sets the value to true.
            InTransaction = true;

            if (Array.IndexOf(JAXLanguageLists.JAXObjects, cClass.ToLower()) < 0)
            {
                //-------------------------------------------------------------
                // TODO - DEAL WITH USER DEFINED CLASSES 
                //-------------------------------------------------------------
                Err = 1999; // Not implemented

                // Class name is not a base class so must be defined in app.ClassDefinitions list
                // TODO - Do we add class library name support or just search for the first name match?
                //int f = app.ClassDefinitions.FindIndex(x => x.Name.Equals(cClass, StringComparison.OrdinalIgnoreCase));
                //if (f < 0) Err = 1733; // No class definition

                // TODO - how do we define parent class?
                // create the parent object
                //JAXObjectWrapper jow = new(app, app.ClassDefinitions[f].ParentClass, string.Empty, parameterList);

                // Create a new base class and copy the properties from the parent
                //thisObject = JAXObjectsAux.GetClass(this, jow.BaseClass, cName);

                // Get the properties from the jow object

                // Update the properties in this object

                // Now execute the property code from app.ClassDefinitions

                // Now load the methods from app.ClassDefinitions[f]
            }
            else
            {
                // Class name is a base class
                if (Class.Equals("screen", StringComparison.OrdinalIgnoreCase))
                {
                    int iii = 0;

                }
                thisObject = JAXObjectsAux.GetClass(this, cClass, cName);
            }


            // Check initialization progress
            if (Err == 0)
                Err = thisObject is null ? 1901 : 0;

            if (Err == 0)
            {
                // All visual objects get this property
                if (VisualClass)
                    SetPrivateProperty("rendered", false);

                string[] JAXProperties = thisObject!.JAXProperties();
                for (int i = 0; i < JAXProperties.Length; i++)
                {

                    string[] prop = JAXProperties[i].Split(',');

                    if (prop.Length == 3)
                    {
                        string p0 = prop[0].ToLower().Trim();
                        string p1 = prop[1].Replace("!", "").ToUpper().Trim();
                        JAXObjects.Token tk = new();

                        if (p0.Contains("backst", StringComparison.OrdinalIgnoreCase))
                        {
                            int iii = 0;
                        }

                        try
                        {
                            //AppIO.DebugLog($"Adding property {p0}");
                            lastProp = p0;

                            // Some properties are already assigned in some classes
                            // so check first before trying to create it
                            if (thisObject.HasProperty(prop[0]) == false)
                            {
                                switch ((p1 + "*")[..1])
                                {
                                    case "F":   // Numeric float
                                        thisObject.AddProperty(p0, "N", prop[2]);
                                        thisObject.UserProperties[p0].Info = "F";
                                        break;

                                    case "Y":  // Logical using Y/N
                                        thisObject.AddProperty(p0, "L", prop[2]);
                                        thisObject.UserProperties[p0].Info = "Y";
                                        break;

                                    case "N":   // Numeric Integer
                                        thisObject.AddProperty(p0, "N", prop[2]);
                                        thisObject.UserProperties[p0].Info = "I";
                                        break;

                                    case "P":   // Points
                                        JAXObjects.Token pp = new();
                                        AppHelper.ASetDimension(pp, 1, 1);
                                        thisObject.AddProperty(p0, pp);
                                        thisObject.UserProperties[p0].Info = "P";
                                        break;

                                    case "R":   // RGB color value
                                        if (prop[2].Contains('|'))
                                        {
                                            string[] rparts = prop[2].Split('|');
                                            if (rparts.Length == 3)
                                            {
                                                if (int.TryParse(rparts[0], out int rp0) == false) rp0 = 0;
                                                if (int.TryParse(rparts[1], out int rp1) == false) rp1 = 0;
                                                if (int.TryParse(rparts[2], out int rp2) == false) rp2 = 0;

                                                prop[2] = (rp2 + rp1 * 256 + rp0 * 65536).ToString();
                                            }
                                            else
                                                throw new Exception("9999|JAXOBJECTWRAPPER|Color error");
                                        }
                                        else
                                        {
                                            // Expecting a single number value
                                            if (int.TryParse(prop[2], out int test) == false) throw new Exception("1732|");
                                            if (JAXLib.Between(test, 0, 16777215) == false) throw new Exception("41|");
                                            prop[2] = test.ToString();
                                        }

                                        thisObject.AddProperty(p0, "N", prop[2]);
                                        thisObject.UserProperties[p0].Info = "RGB";
                                        break;

                                    case "#":   // Simple Array with no members
                                        tk.SetDimension(0, 1, true);
                                        thisObject.AddProperty(p0, tk);
                                        break;

                                    default:    // Rest of normal types (C,D,T,L)
                                        if (p1.Length == 0)
                                        {
                                            // No type, so make the property mutable with a string
                                            thisObject.AddProperty(p0);
                                            thisObject.SetProperty(p0, string.Empty, 0);
                                        }
                                        else
                                            thisObject.AddProperty(p0, p1[..1], prop[2]);
                                        break;
                                }
                            }
                            else
                            {
                                // Update the property
                                tk = new();

                                // Converting to type C, D, L, N, O, or T
                                tk = AppHelper.ReturnStringAsTokenOfType(prop[2], p1);

                                thisObject.SetProperty(p0, tk.Element.Value, 0);
                            }

                            // Update the property attributes
                            thisObject.UserProperties[p0].Protected = prop[1].Contains('!');
                            thisObject.UserProperties[p0].SpecialHandling = prop[1].Contains('$');
                            thisObject.UserProperties[p0].ClassProperty = true;
                            thisObject.UserProperties[p0].Tag = "N";    // Native/User
                        }
                        catch (Exception ex)
                        {
                            msg = ex.Message;
                            Err = 9999;
                        }
                    }
                    else
                        AppIO.DebugLog($"Property error {JAXProperties[i]}");
                }

                // Make sure the name gets set
                if (thisObject is not null && thisObject.UserProperties.ContainsKey("name"))
                    if ((VisualClass && avaloniaObject is not null) || (VisualClass == false && nvObject is not null))
                        thisObject.SetProperty("name", string.IsNullOrWhiteSpace(cName) ? cClass : cName, 0);
            }

            if (Err == 0)
            {
                try
                {
                    // ----------------------------------------------------------------------------------
                    // The following is for all classes except the EMPTY class
                    // ----------------------------------------------------------------------------------
                    if (cClass.Equals("empty", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        // ------------------------------------------------------------------------------
                        // The ID, AError, and Locked properties need to be added
                        // to all properties except the EMPTY property.
                        // ------------------------------------------------------------------------------
                        if (thisObject!.HasProperty("classid") == false)
                        {
                            ClassID = app.SystemCounter();
                            thisObject.AddProperty("classid", "P", ClassID);
                            AppIO.DebugLog($"Adding classid property with value {ClassID}");
                        }

                        if (thisObject!.HasProperty("aerror") == false)
                        {
                            thisObject.AddProperty("aerror");
                            ClearErrors();
                        }

                        if (thisObject!.HasProperty("locked") == false)
                            thisObject.AddProperty("locked", "L", "false");

                        thisObject.UserProperties["classid"].Tag = "N";
                        thisObject.UserProperties["aerror"].Tag = "N";
                        thisObject.UserProperties["locked"].Tag = "N";

                        // Set the AError array to its empty setting
                        ClearErrors();

                        // Now force the object to be updated by running through all the properties
                        // via SetProperty except for those that are arrays or protected
                        foreach (KeyValuePair<string, JAXObjects.Token> tok in thisObject.UserProperties)
                        {
                            //AppIO.DebugLog($"Upating {tok.Key} = {tok.Value.AsString()}");
                            if (tok.Value.Protected == false && tok.Value.TType.Equals("A") == false && JAXLib.InListC(tok.Key, "datasession") == false)
                                thisObject.SetProperty(tok.Key, tok.Value.Element.Value, 0);
                        }

                        // Load the methods
                        string[] JAXMethods = thisObject.JAXMethods();
                        for (int i = 0; i < JAXMethods.Length; i++)
                        {
                            //AppIO.DebugLog($"Adding method {JAXMethods[i]}");
                            thisObject._SetMethod(JAXMethods[i], string.Empty, true, "M!");
                            thisObject.Methods[JAXMethods[i]].Tag = "N";
                        }

                        // Load the events
                        string[] JAXEvents = thisObject.JAXEvents();
                        for (int i = 0; i < JAXEvents.Length; i++)
                        {
                            //AppIO.DebugLog($"Adding event {JAXEvents[i]}");
                            thisObject._SetMethod(JAXEvents[i], string.Empty, true, "E!");
                            thisObject.Methods[JAXEvents[i]].Tag = "N";
                        }

                        // ------------------------------------------------------------------------------
                        // The following methods need to be added to all objects
                        // automatically if they have not already been added
                        // ------------------------------------------------------------------------------


                        // ------------------------------------------------------------------------------
                        // Now call the load method, if it exists
                        // ------------------------------------------------------------------------------
                        if (thisObject.Methods.ContainsKey("load"))
                            thisObject._CallMethod("load");
                    }

                    // ------------------------------------------------------------------------------
                    // Perform post cleanup
                    // ------------------------------------------------------------------------------
                    if (parameterList is not null)
                        thisObject!.PostInit(parent, parameterList);
                    else
                        thisObject!.PostInit(null, []);

                    if (thisObject is not null)
                    {
                        // Now process the JAX init method
                        if (thisObject.Methods.ContainsKey("init"))
                        {
                            thisObject._CallMethod("init");

                            if ("NL".Contains(App.ReturnValue.Element.Type) == false)
                                AppErrorHandling.SetError(11, $"11||{BaseClass}.INIT returned value of type {App.ReturnValue.Element.Type}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            else
                            {
                                // The init only accepts a bool or number for return
                                // If .F. or 0, then the init fails
                                if (App.ReturnValue.AsBool() == false || App.ReturnValue.AsInt() != 0)
                                {
                                    // Kill the class.  This doesn't cause an error unless AError has something
                                    // in it, which we'll then push them to the Errors list
                                    JAXObjects.Token err = thisObject.UserProperties["aerror"];

                                    // Check the first column of the first row for an error number
                                    err.SetElement(1, 1);
                                    if (err.AsInt() > 0)
                                    {
                                        for (int r = 0; r < err.Row; r++)
                                        {
                                            int errNo = err._avalue[r * err.Col].ValueAsInt;

                                            // If the error number > 0
                                            if (errNo > 0)
                                            {
                                                // Update CurrentError pointer
                                                if (r == 0) App.CurrentError = Program.CurrentApp.Errors.Count;

                                                string errMsg = err._avalue[r * err.Col + 2].ValueAsString;
                                                string jaxErrMsg = JAXErrorList.JAXErrMsg(errNo, errMsg);

                                                // Push them to the App error silently
                                                JAXErrors e = new()
                                                {
                                                    ErrorNo = errNo,
                                                    ErrorMessage = jaxErrMsg,
                                                    ErrorProcedure = err._avalue[r * err.Col + 3].ValueAsString,
                                                    ErrorLine = err._avalue[r * err.Col + 1].ValueAsInt
                                                };

                                                Program.CurrentApp.Errors.Add(e);
                                            }
                                        }

                                        thisObject = null;
                                    }
                                }
                            }
                        }

                        // Various classes have other methods that need
                        // to be called after their init method completes
                        thisObject?.PostClassInit();
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    Err = 9999;
                }
            }

            // Did the object initialize correctly?
            // If not, null out thisObject to signal a failure
            if (Err == 0 && Program.CurrentApp.Errors.Count > CurrentErrCount)
                Err = AppErrorHandling.LastErrorNo();

            if (Err > 0)
            {
                thisObject = null;
                AppErrorHandling.SetError(Err, $"{Err}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            else
            {
                if (baseclass.Contains("form"))
                    App._screenClass.AddForm(this);

                thisObject!.PostClassInit().Wait();
            }
        }


        // All objects should call this when shutting down
        public void Release()
        {
            if (baseclass.Contains("form"))
                App._screenClass.RemoveForm(ClassID);
        }

        public void SetParent(JAXObjectWrapper parent)
        {
            if (Parent is not null)
            {
                if (baseclass.Equals("formset", StringComparison.OrdinalIgnoreCase))
                    throw new Exception($"3300|{Class}/{Parent.BaseClass}");

                if (baseclass.Equals("optionbutton", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Equals("optiongroup", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{Class}/{Parent.BaseClass}");

                if (baseclass.Equals("menuitem", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Contains("menu", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{Class}/{Parent.BaseClass}");

                if (baseclass.Equals("toolbutton", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Equals("toolbar", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{Class}/{Parent.BaseClass}");

                if (baseclass.Equals("form", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Equals("formset", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{Class}/{Parent.BaseClass}");

                if (VisualClass && Parent.VisualClass == false)
                    throw new Exception($"3301|{Class}/{Parent.BaseClass}");
            }

            Parent = parent;
        }

        public void SetName(string name)
        {
            JOWName = name.Trim().ToLower();

            if (thisObject is not null && thisObject.UserProperties.TryGetValue("name", out JAXObjects.Token? value))
                value.Element.Value = JOWName;
        }


        /*
         * Clear the aError array by creating it from scratch
         */
        public void ClearErrors()
        {
            if (thisObject is not null && thisObject.HasProperty("aerror"))
            {
                JAXObjects.Token tk = new();
                tk._avalue[0].Value = 0;
                tk._avalue.Add(new());
                tk._avalue.Add(new());
                tk._avalue.Add(new());
                tk.Row = 1;
                tk.Col = 4;
                tk.TType = "A";

                // Have to set the property directly so that the
                // property is the array.  Otherwise SetProperty()
                // will put the array in the element, like an object.
                thisObject.UserProperties["aerror"] = tk;
            }
        }

        public async Task<int> GetErrorNo()
        {
            int result;

            if (thisObject is null)
                result = 1901;
            else
            {
                JAXObjects.Token? tk = await thisObject.GetProperty("aerror", 0);

                if (tk is not null)
                    result = tk._avalue[0].ValueAsInt;
                else
                    result = 9601;
            }

            return result;
        }

        public async Task<int> AddError(int errorNo, int lineNo, string message, string procedure)
        {
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    result = 1901;
                else
                {
                    thisObject._AddError(errorNo, lineNo, message, procedure);

                    if (thisObject.Methods.ContainsKey("error"))
                        result = await MethodCall("error");
                }

            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
            {
                AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }

            return result;
        }

        public async Task<string> IsMember(string memTest)
        {
            string result = "X";

            try
            {
                if (thisObject is not null)
                    result = await thisObject.IsMember(memTest);
                else
                    AppErrorHandling.SetError(1901, "1901|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }
            catch (Exception ex)
            {
                result = "X";
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }

        public string DefaultName()
        {
            string result = string.Empty;

            try
            {
                if (thisObject is not null)
                    result = thisObject.DefaultName();
                else
                    AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, "");
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            return result;
        }


        // Add a user defined method or update an existing method/event
        public int SetMethod(string methodName, string sourceCode, bool createOK)
        {
            int result;
            string msg = string.Empty;

            try
            {
                if (!InTransaction) ClearErrors();

                if (thisObject is null)
                {
                    result = 1901;
                    AppErrorHandling.SetError(1901, "1901|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
                else
                    result = thisObject._SetMethod(methodName, sourceCode, createOK, "U");
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = 9999;
            }

            if (result > 0)
            {
                AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }

            return result;
        }


        /*
         * When a method is called, if there is code in that method, it is executed, otherwise
         * the DoDefault() is executed.  A method with code in it must call DoDefault if it
         * wants the underlying action/code to execute.
         * 
         * Parameters are expected to already be in the stack.
         * 
         */
        public async Task<int> MethodCall(string methodName)
        {
            int result;
            string msg = string.Empty;
            methodName = methodName.ToLower();

            // Certain events may be called before the methods dictionary is initialized.
            // If it's not an Empty class, then just return if no methods are listed.
            if (baseclass.Equals("empty"))
                return 1738;
            else
                if (thisObject is not null && thisObject.Methods.Count == 0)
                    return 0;

            try
            {
                // Clear the aError array before calling a method
                // TODO - this isn't right
                if (!InTransaction) ClearErrors();

                if (thisObject is null)
                    result = 1901;
                else
                {
                    // Check to see if the method exists before trying to call it
                    if ((await thisObject.IsMember(methodName)).Equals("M"))
                    {
                        if (methodName.Contains("add"))
                        {
                            int iii = 0;
                        }


                        if (thisObject.Methods[methodName].CompiledCode.Length > 0)
                        {
                            // Execute the coded method
                            result = await thisObject._CallMethod(methodName);

                            if (methodName.Equals("error", StringComparison.OrdinalIgnoreCase) && result != 0)
                            {
                                result = 3099;
                                AppErrorHandling.SetError(3099, $"Name (BaseClass: {BaseClass}, ID:{ClassID})", string.Empty);
                            }
                        }
                        else
                            result = await thisObject.DoDefault(methodName);
                    }
                    else
                        result = 1738;

                    // Everything ok?
                    //if (result == 0 && string.IsNullOrWhiteSpace(methodName) == false)
                    //    result = thisObject._CallMethod(methodName);
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            // Clear the parameter list no matter what
            App.ParameterClassList.Clear();

            if (result > 0)
            {
                if (thisObject is not null)
                {
                    thisObject._AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                    if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                        AppErrorHandling.SetError(result, $"{result}|{methodName}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
                else
                    AppErrorHandling.SetError(result, $"{result}|{methodName}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            // Send back the return value
            return result;
        }

        // Standard xBase addobject is stored in a list of tokens
        //      ADDOBJECT(cName, cClass [,OLEClass] [,aInit1 ,aInit2...]
        //
        // TODO - Look this over!  It doesn't do anything, yet!
        public async Task<int> AddObjectUsingParameters(List<JAXObjects.Token> ObjectParameters)
        {
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (!InTransaction) ClearErrors();

                if (ObjectParameters.Count < 1)
                    result = 11;
                else
                {
                    string cClass = ObjectParameters[0].AsString();
                    string cName = ObjectParameters.Count > 1 ? ObjectParameters[1].AsString() : string.Empty;

                    // Set up app parameters to send
                    //App.ParameterList.Clear();
                    List<ParameterClass> cParams = [];

                    if (ObjectParameters.Count > 2)
                    {
                        for (int i = 2; i < ObjectParameters.Count; i++)
                        {
                            ParameterClass p = new();
                            p.token.Element.Value = ObjectParameters[i].Element.Value;
                            cParams.Add(p);
                        }
                    }

                    // CHECK NAME

                    // Everything is ok, so create the object
                    JAXObjectWrapper jow = new(App, cClass, cName, cParams);

                    if (jow.thisObject is not null)
                    {
                        // Successful! Set the parent and add it
                        // to the objects array
                        jow.SetParent(this);
                        int i = await AddObject(jow);

                        if (i >= 0)
                        {
                            jow.thisObject.SetObjectIDX(i);
                        }
                        else
                            result = 1904;
                    }
                    else
                    {
                        // Failed to instantiate
                        result = 1902;
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
            {
                await AddError(result, -1, msg, string.Empty);
                result = -1;
            }

            return result;
        }


        // Sometimes we have an object to which we want to add to another object
        public async Task<int> AddObject(JAXObjectWrapper eClass)
        {
            int result;
            int ccount = -1;
            JAXObjects.Token tk;
            int objIdx;
            string msg = string.Empty;

            if (!InTransaction) ClearErrors();

            if (thisObject is null)
                result = 1901;
            else
            {

                // Does this object support the objects array?
                tk = await thisObject.GetProperty("controlcount");
                if (tk.Element.IsNull() == false)
                    ccount = tk.AsInt();

                try
                {
                    string className = string.Empty;
                    tk = await thisObject.GetProperty("class");
                    if (tk.Element.IsNull() == false)
                        className = tk.AsString();

                    if (ccount < 0)
                        result = 3016;
                    else
                    {
                        // Can't add an object with no name or class defined as an object
                        // must be defined as a property to be included in this class
                        if ((await eClass.IsMember("name")).Equals("P") && (await eClass.IsMember("class")).Equals("P"))
                        {
                            result = 0;// TODO - fix error detection
                            tk = await eClass.GetProperty("name");

                            if (result == 0)
                            {
                                string name = tk.AsString();

                                if (name.Length == 0)
                                {
                                    // Need to create a name so loop through all objects looking for
                                    // the same base class and comparing eClass default name plus a
                                    // counter to existing names until we find the highest matching
                                    // name.  If no matches, then it's number 1.
                                    int nameCount = 1;
                                    string nameTemplate = eClass.DefaultName() + "{0}";

                                    for (int i = 0; i < ccount; i++)
                                    {
                                        string nameTry = string.Format(nameTemplate, i);
                                        result = 0;  // TODO - Fix error detection
                                        tk = await eClass.GetProperty("baseclass");

                                        if (result == 0)
                                        {
                                            result = 0;  // TODO - here too
                                            JAXObjectWrapper? obj = await thisObject.GetObject(i);
                                            JAXObjects.Token tk2 = await obj!.GetProperty("baseclass");

                                            if (tk2.Element.Type.Equals("C"))
                                            {
                                                if (tk.AsString().Equals(tk2.AsString(), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    tk = await obj.GetProperty("name");
                                                    if (tk.Element.Type.Equals("C"))
                                                    {
                                                        if (nameTry.Equals(tk.AsString(), StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            nameCount++;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                        break;
                                                }
                                            }
                                            else
                                                break;
                                        }
                                        else
                                            break;
                                    }

                                    if (result == 0)
                                        await eClass.SetProperty("name", string.Format(nameTemplate, nameCount));
                                }


                                // We've been given a name - make sure it's not alreay used
                                if (result == 0 && ccount >= 0)
                                {
                                    // Get the object name
                                    result = 0;// fix error detection
                                    tk = await eClass.GetProperty("name");

                                    if (result == 0)
                                    {
                                        string cName = tk.AsString();

                                        for (int i = 0; i < ccount; i++)
                                        {
                                            // Check this object name with all others
                                            result = 0;// fix error detection
                                            JAXObjectWrapper? obj = await thisObject.GetObject(i);
                                            tk = await obj!.GetProperty("name");

                                            if (tk.Element.Type.Equals("C"))
                                            {
                                                if (cName.Equals(tk.AsString(), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    // Same name alreay in use
                                                    result = 3014;
                                                    break;
                                                }
                                            }
                                            else
                                                break;
                                        }
                                    }
                                }
                            }

                            if (result == 0)
                            {
                                // Everything is fine, so set the parent property
                                // and add it to the objects array
                                eClass.SetParent(this);
                                objIdx = await thisObject.AddObject(eClass);

                                if (objIdx >= 0)
                                {
                                    result = thisObject.SetObjectIDX(objIdx);

                                    if (result == 0)
                                        result = await thisObject._CallMethod("AddObject");
                                }
                            }
                        }
                        else
                            result = 3015;
                    }
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    result = 9999;
                }
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }


        // Other times we want to create a new object and add it to an object
        public async Task<int> AddObject(string cName, string cClass)
        {
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (!InTransaction) ClearErrors();

                if (thisObject is null)
                    result = 1901;
                else
                {
                    JAXObjectWrapper eClass = new(App, cClass, "", null);
                    JAXObjects.Token tk;

                    int ccount = -1;

                    // Does this object support the objects array?
                    tk = await thisObject.GetProperty("controlcount");
                    if (tk.Element.IsNull() == false)
                        ccount = tk.AsInt();

                    string className = string.Empty;
                    tk = await thisObject.GetProperty("class");
                    if (tk.Element.IsNull() == false)
                        className = tk.AsString();

                    if ((await eClass.IsMember("baseclass")).Equals("P") && (await eClass.IsMember("name")).Equals("P"))
                    {
                        if (ccount < 0)
                        {
                            // This object doesn't allow objects to be added
                            result = 3016;
                        }
                        else
                        {
                            if (ccount >= 0)
                            {
                                string name = eClass.DefaultName();
                                int HighestVal = 1;

                                if (cName.Length == 0)
                                {
                                    for (int i = 0; i < ccount; i++)
                                    {
                                        result = 0;// fix error detection
                                        JAXObjectWrapper? obj = await thisObject.GetObject(i);
                                        tk = await obj!.GetProperty("name");

                                        if (tk.Element.Type.Equals("C"))
                                        {
                                            string objName = tk.AsString()[..name.Length];
                                            if (objName.Equals(name[..name.Length]))
                                            {
                                                if (int.TryParse(objName[name.Length..], out int testVal) == false) testVal = 0;
                                                HighestVal = HighestVal < testVal ? testVal : HighestVal;
                                            }
                                        }
                                        else
                                            break;
                                    }

                                    result = await eClass.SetProperty("name", string.Format(name + "{0}", HighestVal + 1));
                                }
                            }

                            // If the new object was created, try to add it to this object
                            if (result == 0)
                                result = await AddObject(eClass);
                        }
                    }
                    else
                    {
                        // Can't add an empty object
                        result = 3015;
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = 9999;
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Add a property directly.  Used internally.
        /// </summary>
        /// <param name="cPropertyName"></param>
        /// <param name="eNewValue"></param>
        public int AddPropertyDirect(string cPropertyName, JAXObjects.Token eNewValue)
        {
            int result;
            string msg = string.Empty;

            if (thisObject is null)
                result = 1901;
            else
                result = thisObject.AddProperty(cPropertyName, eNewValue);

            if (result > 0)
            {
                AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }

            return result;
        }


        /// <summary>
        /// Add a property directly.  Used internally.
        /// </summary>
        /// <param name="cPropertyName"></param>
        /// <param name="eNewValue"></param>
        public int AddPropertyValue(string cPropertyName, object? eNewValue)
        {
            int result;
            string msg = string.Empty;

            if (thisObject is null)
                result = 1901;
            else
            {
                JAXObjects.Token tk = new();
                if (eNewValue is null)
                    tk.Element.MakeNull();
                else
                    tk.Element.Value = eNewValue;

                result = thisObject.AddProperty(cPropertyName, tk);
            }

            if (result > 0)
            {
                AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Add a proprty and value to the object and trigger the JAX Method
        /// </summary>
        /// <param name="cPropertyName"></param>
        /// <param name="eNewValue"></param>
        /// <param name="nVisiblity"></param>
        /// <param name="cDescription"></param>
        /// <returns></returns>
        /// 
        // TODO - add visibility & description support
        public async Task<int> AddProperty(string cPropertyName, JAXObjects.Token eNewValue, int nVisiblity, string cDescription)
        {
            // nVisibility & cDescription are not supported at this time
            int result;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (!InTransaction) ClearErrors();

                    result = thisObject.AddProperty(cPropertyName, eNewValue);
                    thisObject.UserProperties[cPropertyName].PropType = "U";   // User defined

                    // TODO - set up parameter list
                    if (result == 0)
                        result = await MethodCall("AddProperty");
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = 9999;
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }


        /// <summary>
        /// Returns the JAXObjectsAux.MethodClass for the specified method.  If not found, Type will be empty.
        /// </summary>
        /// <param name="meth"></param>
        /// <returns></returns>
        public JAXObjectsAux.MethodClass MethodInfo(string meth)
        {
            JAXObjectsAux.MethodClass result = new();
            meth = meth.ToLower();

            if (thisObject is not null && thisObject.Methods.TryGetValue(meth, out JAXObjectsAux.MethodClass? value))
                result = value;

            return result;
        }


        /// <summary>
        /// Uncontrolled access for getting the method/event list. String returned as Name+Type (Ex: CLICKE)
        /// </summary>
        /// <returns></returns>
        public List<string> GetMethodList()
        {
            List<string> props = [];
            int err = 0;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    err = 1901;
                else
                {
                    // Populate the list
                    foreach (KeyValuePair<string, JAXObjectsAux.MethodClass> ky in thisObject.Methods)
                        props.Add(ky.Key.ToUpper());

                    // Sort the list
                    props.Sort();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                err = 9999;
            }

            if (err > 0)
                AppErrorHandling.SetError(err, $"{err}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            // Return the list
            return props;
        }

        /// <summary>
        /// Uncontrolled access for getting the property list.
        /// </summary>
        /// <returns></returns>
        public List<string> GetPropertyList()
        {
            List<string> props = [];
            int err = 0;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    err = 1901;
                else
                {
                    // Populate the list
                    foreach (KeyValuePair<string, JAXObjects.Token> ky in thisObject.UserProperties)
                        props.Add(ky.Key.ToUpper());

                    // Sort the list
                    props.Sort();
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                err = 9999;
            }

            if (err > 0)
                AppErrorHandling.SetError(err, $"{err}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            // Return the list
            return props;
        }

        /// <summary>
        /// Get a property from this class
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<JAXObjects.Token> GetProperty(string name)    // TODO - register error
        {
            JAXObjects.Token tk = new();
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (!InTransaction) ClearErrors();

                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (name.Equals("anchor"))
                    {
                        int iii = 0;
                    }
                    JAXObjects.Token tk1 = await thisObject.GetProperty(name);
                    if (tk.Element.IsNull())
                        tk.Element.MakeNull();  // Property is not a member
                    else
                        tk = tk1;
                }
            }
            catch (Exception ex)
            {
                tk.Element.MakeNull();
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);
            }

            return tk;
        }

        /// <summary>
        /// Get an property from the class with an element index
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public async Task<JAXObjects.Token> GetProperty(string name, int idx)
        {
            JAXObjects.Token tk = new();
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (!InTransaction) ClearErrors();

                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (name.Equals("listitem", StringComparison.OrdinalIgnoreCase))
                    {
                        int iii = 0;
                    }
                    JAXObjects.Token tk1 = await thisObject.GetProperty(name, idx);

                    if (tk1.Element.IsNull())
                        tk.Element.MakeNull();
                    else
                        tk = tk1;
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                tk.Element.MakeNull();
                result = 9999;
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);
            }

            return tk;
        }

        /// <summary>
        /// Return the object index for the supplied name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<int> FindObjectByName(string name)
        {
            int result = -1;
            int err = 0;
            string msg = string.Empty;

            try
            {
                name = name.Trim();
                int objCount = -1;

                if (thisObject is null)
                    err = 1901;
                else
                {
                    JAXObjects.Token tk = await thisObject.GetProperty("controlcount");
                    if (tk.Element.IsNull() == false)
                        objCount = tk.AsInt();

                    for (int i = 0; i < objCount; i++)
                    {
                        JAXObjectWrapper? obj = await thisObject.GetObject(i);
                        string memb = await obj!.IsMember("name");    // Is there a name property?

                        if (memb.Equals("P"))
                        {
                            JAXObjects.Token tk1 = new();
                            err = 0; // fix error detection
                            tk1 = await obj.GetProperty("name");

                            if (err == 0)
                            {
                                if (tk1.AsString().Equals(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    result = i;
                                    break;
                                }
                            }
                            else
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = -1;
                err = 9999;
                msg = ex.Message;
            }

            // Handle any error we find
            if (err > 0)
            {
                if (err == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }


        /// <summary>
        /// Set the property of an element in the Objects array.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public async Task<int> SetObject(int idx, string property, JAXObjects.Token value)
        {
            int result;
            string msg = string.Empty;

            if (!InTransaction) ClearErrors();
            if (thisObject is null)
                result = 1901;
            else
            {
                // TODO - Check to see if ccount>=0 and check to see ccount>idx
                result = thisObject.SetObjectProperty(idx, property, value);
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Get an element from the Objects array by name.  Sends out index and returns a JOW.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public async Task<JAXObjectWrapper?> GetObject(string name)
        {
            int idx = -1;
            JAXObjectWrapper? jow = null;

            if (!InTransaction) ClearErrors();

            if (thisObject is not null)
            {
                jow = await thisObject.GetObject(name);

                if (jow is not null)
                    jow.IDX = idx;
            }

            return jow;
        }

        /// <summary>
        /// Get an element from the Objects array by index.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public async Task<JAXObjectWrapper?> GetObject(int idx)
        {
            int result = 0;
            string msg = string.Empty;
            JAXObjectWrapper? jow = null;

            if (!InTransaction) ClearErrors();

            if (thisObject is null)
                result = 1901;
            else
            {
                jow = await thisObject.GetObject(idx);
            }

            // TODO - Check to see if ccount>=0 and check to see ccount>idx
            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);
            }

            return jow;
        }


        /// <summary>
        /// Special case
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<int> SetProperty(string name, object value, int idx)
        {
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (!InTransaction) ClearErrors();

                    if ((await thisObject.IsMember(name)).Equals("P"))
                    {
                        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
                            JOWName = value.ToString() ?? string.Empty;

                        await thisObject.SetProperty(name, value, idx);

                        AppIO.DebugLog($"Updated {this.JOWName}.{name} -> {value}");
                    }
                    else
                    {
                        // Property is not a member
                        result = 1559;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO - Add error support
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
            {
                if (result != 9999)
                    AppErrorHandling.SetError(result, $"{result}|{name}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }
        /// <summary>
        /// Set a class property by name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<int> SetProperty(string name, object value)
        {
            int result = 0;
            string msg = string.Empty;

            try
            {
                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (!InTransaction) ClearErrors();

                    if ((await thisObject.IsMember(name)).Equals("P"))
                    {
                        if (name.Equals("name", StringComparison.OrdinalIgnoreCase))
                            JOWName = value.ToString() ?? string.Empty;

                        await thisObject.SetProperty(name, value, 0);

                        AppIO.DebugLog($"Updated {this.JOWName}.{name} -> {value}");
                    }
                    else
                    {
                        // Property is not a member
                        result = 1559;
                    }
                }
            }
            catch (Exception ex)
            {
                // TODO - Add error support
                result = 9999;
                msg = ex.Message;
            }

            if (result > 0)
            {
                if (result != 9999)
                    AppErrorHandling.SetError(result, $"{result}|{name}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Remove an element from the Objects array by index.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public async Task<int> RemoveObject(int x)
        {
            int result;
            string msg = string.Empty;

            if (!InTransaction) ClearErrors();

            if (thisObject is null)
                result = 1901;
            else
            {
                // TODO - get ccount and compare to x
                result = thisObject.RemoveObject(x);
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Remove an element from the Objects array by name.
        /// </summary>
        /// <param name="cName"></param>
        /// <returns></returns>
        public async Task<int> RemoveObject(string cName)
        {
            if (!InTransaction) ClearErrors();

            int result = -1;
            int ccount = -1;
            int idx = -1;
            string msg = string.Empty;

            if (thisObject is null)
                result = 1901;
            else
            {
                JAXObjects.Token tk = await thisObject.GetProperty("controlcount");
                if (tk.Element.IsNull() == false)
                    ccount = tk.AsInt();

                for (int i = 0; i < ccount; i++)
                {
                    JAXObjectWrapper? obj = await thisObject.GetObject(i);
                    tk = await obj!.GetProperty("name");

                    if (tk.Element.Type.Equals("C") && cName.Equals(tk.AsString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Found it, so going to remove it
                        thisObject.RemoveObject(i);
                        idx = i;
                        break;
                    }
                }
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }
            else
                result = idx;

            return result;
        }


        //---------------------------------------------------------
        // I think this needs to go to the base class
        //---------------------------------------------------------
        /// <summary>
        /// Make the form visible.
        /// </summary>
        /// <returns></returns>
        public async Task<int> Show()
        {
            if (!InTransaction) ClearErrors();

            int result = 0;
            string msg = "SHOW";

            try
            {
                if (thisObject is null)
                    result = 1901;
                else
                {
                    if (thisObject.Methods.ContainsKey("show"))
                    {
                        if (BaseClass.Equals("formset", StringComparison.OrdinalIgnoreCase))
                        {
                            // Formset calls top Form
                            int i = -1;
                            JAXObjects.Token tk = await thisObject.GetProperty("controlcount");
                            if (tk.Element.Type.Equals("N"))
                            {
                                i = tk.AsInt();

                                if (i >= 0)
                                {
                                    JAXObjectWrapper? obj = await thisObject.GetObject(i);
                                    await obj!.MethodCall("show");
                                }
                            }
                        }
                        else
                        {
                            thisObject!._CallMethod("show").Wait();
                        }
                    }
                    else
                        result = 6501;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }


            if (result > 0)
            {
                msg = JAXErrorList.JAXErrMsg(result, msg);
                thisObject!._AddError(result, App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine, msg, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|SHOW|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Set the ZOrder of the object. JAXBase allows you to order objects explicity.  nOrder>ordercount = bottom of order while nOrder >=0 and < nOrderCount = insert at element.
        /// </summary>
        /// <param name="nOrder"></param>
        public async Task<int> ZOrder(int nOrder)
        {
            int result = 0;
            string msg = string.Empty;

            if (!InTransaction) ClearErrors();
            if (thisObject is null)
                result = 1901;
            else
            {
                JAXObjects.Token tk = await thisObject.GetProperty("baseclass");

                if (tk.Element.Type.Equals("C"))
                {
                    if ((await thisObject.IsMember("zorder")).Equals("P"))
                    {
                        JAXObjects.Token par = await thisObject.GetProperty("parent");
                        if (par.TType.Equals("O"))
                        {
                            JAXObjectWrapper parent = (JAXObjectWrapper)par.Element.Value;
                            tk = await parent.GetProperty("controlcount");

                            if (result == 0)
                            {
                                int cCount = tk.AsInt();

                                int myIDX = thisObject.GetObjectIDX();

                                // TODO - Change it's order
                                JAXObjectWrapper? obj = await parent.GetObject(myIDX);

                                if (obj is not null)
                                {
                                    if (nOrder <= 0)
                                    {
                                        // Top of order
                                        await parent.RemoveObject(myIDX);
                                    }
                                    else if (nOrder >= cCount)
                                    {
                                        // Bottom of order
                                        await parent.RemoveObject(myIDX);
                                        await parent.AddObject(obj!);
                                    }
                                    else
                                    {
                                        // Place it here
                                        await parent.RemoveObject(myIDX);
                                    }
                                }
                            }
                        }
                        else
                            result = 9999;
                    }
                    else
                        result = 3018;  // ZOrder isn't a property
                }
                else
                    result = 3018; // TODO - it's an empty class
            }

            if (result > 0)
            {
                if (result == 1901)
                    AppErrorHandling.SetError(result, $"{result}||{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                else
                    await AddError(result, -1, msg, string.Empty);

                result = -1;
            }

            return result;
        }

        public JAXObjects.Token GetPrivateProperty(string name)
        {
            JAXObjects.Token result = new();

            if (thisObject is null || thisObject.GetPrivateProperty(name.ToLower(), out result) != 0)
                result.Element.MakeNull();

            return result;
        }

        public int SetPrivateProperty(string name, object? value)
        {
            int result;

            if (thisObject is not null)
                result = thisObject.SetPrivateProperty(name.ToLower(), value);
            else
                result = 1901;

            return result;
        }

        public void ApplyVFPAnchor(double width, double height)
        {
            if (thisObject is not null && AnchorValue > 0)
                thisObject.ApplyVFPAnchor(width, height);
        }
    }
}
