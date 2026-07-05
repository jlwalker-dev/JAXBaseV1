/*------------------------------------------------------------------------------------------*
 * BASE CLASS FOR ALL JAX CLASSES
 * 
 * 2026-02-15 - JLW
 *      Realized that if put the source to GitHub before converting to Avalonia that
 *      it may never get converted.   V0.6 will have a cross-platform GUI.
 * 
 * 2026-04-01 - JLW
 *      Futher research is convincing me that it would be better to allow the
 *      classes to only define the properties and methods before the form renders.
 *      Then when it renders, it can create eaach control based on the properties.
 *      Otherwise, too many things get lost when we copy from the build canvas to 
 *      the display canvas.
 * 
 *      This is going to take more than a weekend and a couple of pizzas.
 *
 *      If I don't do it, I'm going to have to keep kludeging the 
 *      XBase_Class_Avalonia.ReapplyPosition() method to try to fix the
 *      lost properties and breaks to event handling after the fact.
 * 
 * 2026-06-15 - JLW
 *      Working on direction of travel between visual controls i preperation for getting
 *      the When event working correctly.  Also need to add LastControl property set up
 *      for the When event.
 *      
 *      If a control's When returns a false, then if there is a direction of control the
 *      next control in that direction should get focus.  If there is no direction of control,
 *      then the focus should return to the control that lost focus (LastControl).
 *      
 *      Tried to get the project to allow me to use the LosingFocus event, but it seems
 *      to be a problem.  I can use the LostFocus event, but I can't get direction of
 *      travel through that event.  So I'm going to have to see if I can use the
 *      GotFocus travel direction for the same purpose.
 *      
 * 
 *------------------------------------------------------------------------------------------*/
using Avalonia.Controls;
using Avalonia.Input;
using JAXBase.Core;
using JAXBase.Language;
using JAXBase.Utilities;
using System.Windows.Controls;
using static JAXBase.XBase.JAXObjectsAux;

namespace JAXBase.XBase
{
    public class XBase_Avalonia : IJAXAvaClass, IDisposable
    {
        // Control flag for the object array indicating if the JAXCode can work with the array.  Caps mean
        // the code is allowed to Read, Write, or Update.  All lower case prohibits all object array access.
        public enum UserObject { urw, Urw, uRw, URw, UrW, URW }

        public JAXObjectWrapper me;
        public JAXObjectWrapper? Parent = null;
        public Dictionary<string, JAXObjects.Token> UserProperties { get; private set; } = new Dictionary<string, JAXObjects.Token>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, JAXObjects.Token> PrivateProperties { get; private set; } = new Dictionary<string, JAXObjects.Token>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, MethodClass> Methods { get; private set; } = new Dictionary<string, MethodClass>(StringComparer.OrdinalIgnoreCase);

        public double DIPScaling = 1D;
        public int MyIDX = -1;
        readonly private int nextMove = 0;
        public int GetNextMove() { return nextMove; }

        public int SetObjectIDX(int idx)
        {
            MyIDX = idx;
            return 0;
        }

        public int GetObjectIDX() { return MyIDX; }

        public bool CanUseObjects = false;          // Can external code use the objects array
        public bool CanReadObjects = false;         // Can external code read the objects array
        public bool CanWriteObjects = false;        // Can external code write the objects array

        public bool InInit = true;
        public bool isProgrammaticChange = false;

        public virtual bool VisualClass { get; set; } = false;
        public virtual string MyDefaultName { get; } = string.Empty;
        public virtual string MyBaseClass { get; } = string.Empty;
        public virtual bool Register { get; } = true;

        public XBase_Avalonia(JAXObjectWrapper jow, string name)
        {
            me = jow;
            me.BaseClass = MyBaseClass;
            //MyDefaultName = string.Empty;
            //MyBaseClass = string.Empty;

            // Make sure there's a name in the name property
            if (UserProperties.ContainsKey("name"))
            {
                if (string.IsNullOrWhiteSpace(UserProperties["name"].AsString()))
                    UserProperties["name"].Element.Value = me.Class;
            }
        }




        /*------------------------------------------------------------------------------------------*
         * Set up if a visual object and the user object access settings
         *------------------------------------------------------------------------------------------*/
        protected virtual void SetVisualObject(Avalonia.Controls.Control? MyObj = null, string myBaseClass = "", string MyDefaultName = "", bool VisualClass = false, UserObject uobj = UserObject.urw)
        {
            me.VisualClass = VisualClass;
            me.BaseClass = myBaseClass;

            if (VisualClass && MyObj is not null)
                me.avaloniaObject = MyObj;

            me.SetName(MyDefaultName);

            int userObject = (int)uobj;

            CanUseObjects = userObject > 0;
            CanReadObjects = JAXLib.InList(userObject, 2, 3, 5);
            CanWriteObjects = userObject > 3;

            /*
             * Give it a default name and set up the common events
             */
            if (me.VisualClass && me.avaloniaObject is not null)
            {
                me.SetName(MyDefaultName);
                SuspendEvents();
                SetEvents();
            }
        }



        /*------------------------------------------------------------------------------------------*
         * Post init setting up the Parent object and parent class
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // We don't need this code
            Parent = callBack;
            if (Parent is not null)
            {
                // Formset can't have a parent (what about _Screen?)
                if (me.BaseClass.Equals("formset", StringComparison.OrdinalIgnoreCase)) // What about _Screen as parent????
                    throw new Exception($"3300|{me.Class}/{Parent.BaseClass}");

                // Optionbutton can only have an Optiongroup parent
                if (me.BaseClass.Equals("optionbutton", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Equals("optiongroup", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{me.Class}/{Parent.BaseClass}");

                // Menuitem can only have a menu parent
                if (me.BaseClass.Equals("menuitem", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Contains("menu", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{me.Class}/{Parent.BaseClass}");

                // Toolbutton can only have a toolbar parent
                if (me.BaseClass.Equals("toolbutton", StringComparison.OrdinalIgnoreCase) && Parent.BaseClass.Equals("toolbar", StringComparison.OrdinalIgnoreCase) == false)
                    throw new Exception($"3300|{me.Class}/{Parent.BaseClass}");

                // Form can only have a form or formset parent (what about _Screen?)
                if (me.BaseClass.Equals("form", StringComparison.OrdinalIgnoreCase) && JAXLib.InListC(Parent.BaseClass, "form", "formset") == false)
                    throw new Exception($"3300|{me.Class}/{Parent.BaseClass}");

                // Form on a formset is the exception to this rule
                if (Parent.BaseClass.Equals("formset") == false && me.BaseClass.Equals("form", StringComparison.OrdinalIgnoreCase) == false)
                {
                    if (VisualClass && Parent.VisualClass == false)
                        throw new Exception($"3301|{me.Class}/{Parent.BaseClass}");
                }

                JAXObjects.Token tk = await Parent.GetProperty("name");
                UserProperties["parentclass"].Element.Value = tk.AsString();
                AppIO.DebugLog($"Setting parent of {me.JOWName} to {Parent.JOWName}", false);
            }

            // Update the properties of this object
            // Remember to watch out for triggered properties that
            // may need to be run after all of the other properties
            // have been processed
            foreach (ParameterClass p in parameterList)
            {
                string Name = p.PName.ToLower();

                if (UserProperties.ContainsKey(Name) == false)
                    AddProperty(Name);

                SetProperty(Name, p.token.Element.Value, 0).Wait();
            }

            InInit = false;
            return true;
        }

        /*------------------------------------------------------------------------------------------*
         * Some classes may have things that need to happen after their init has executed
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<bool> PostClassInit() { return true; }

        /*------------------------------------------------------------------------------------------*
         * Update the AError array
         *------------------------------------------------------------------------------------------*/
        public virtual void _AddError(int errorNo, int lineNo, string message, string procedure)
        {
            if (UserProperties.TryGetValue("aerror", out JAXObjects.Token? aerr))
            {
                AppIO.DebugLog($">>> ERROR: Class {me.JOWName} ({me.BaseClass}): {errorNo} @ {lineNo} in {procedure} - {message}");

                // Check to make sure we really have an array with 4 columns
                if (aerr.TType.Equals("A") && aerr.Col == 4)
                {
                    int i = 0;

                    // If _avalue[0] is not zero then we have to add another
                    // row to the array and position the strting point on
                    // the new row before saving the error
                    if (aerr._avalue[0].ValueAsInt != 0)
                    {
                        i = aerr._avalue.Count;
                        for (int j = 0; j < 4; j++)
                            aerr._avalue.Add(new());
                    }

                    // Add the error to the array
                    aerr._avalue[i + 0].Value = errorNo;
                    aerr._avalue[i + 1].Value = lineNo;
                    aerr._avalue[i + 2].Value = errorNo < 9999 ? JAXError.JAXErrMsg(errorNo, message) : message;
                    aerr._avalue[i + 3].Value = procedure;
                }
            }
        }

        /*------------------------------------------------------------------------------------------*
         * Get a property
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<JAXObjects.Token> GetProperty(string propertyName)
        {
            return await GetProperty(propertyName, 0);
        }


        /*------------------------------------------------------------------------------------------*
         * Add an object to the end of the objects array
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            if (CanUseObjects == false) throw new Exception("3019|");

            if (CanWriteObjects)
            {
                if (value.VisualClass)
                {
                    if (value.avaloniaObject is not null)
                    {
                        if (value.avaloniaObject is Avalonia.Controls.Canvas canvas)
                            canvas.Children.Add(value.avaloniaObject);
                        else if (value.avaloniaObject is Avalonia.Controls.Panel panel)
                            panel.Children.Add(value.avaloniaObject);
                    }
                    else
                        err = 1901;
                }

                if (err == 0)
                {
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["objects"].Col;
                    value.thisObject?.PostInit(me, []).Wait();
                }
            }
            else
                err = 3019;

            if (err > 0)
            {
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            }
            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }

        /*------------------------------------------------------------------------------------------*
         * Insert an object in the objects array at a specific location
         * moving the rest of the objects down and expanding the array
         * by one element
         *------------------------------------------------------------------------------------------*/
        public virtual int InsertObjectAt(JAXObjectWrapper obj, int moveIDX)
        {
            int result = 0;

            if (CanUseObjects == false)
                result = 3019;
            else if (CanWriteObjects)
            {

            }
            else
                result = 3109;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Remove an object from the specified index in the objects array
         *------------------------------------------------------------------------------------------*/
        public int RemoveObject(int idx)
        {
            int result = 0;

            if (CanUseObjects == false)
                result = 3019;
            else if (CanWriteObjects)
            {
                if (idx >= UserProperties["objects"].Col)
                    result = 3003;
                else
                {
                    JAXObjectWrapper jow = (JAXObjectWrapper)UserProperties["objects"].Element.Value;

                    if (jow is not null && jow.thisObject is not null)
                    {
                        if (jow.Protected == JAXObjectWrapper.Protection.URD)
                            UserProperties["objects"].RemoveAt(idx);
                        else
                            result = 3042;
                    }
                    else
                        UserProperties["objects"].RemoveAt(idx);  // Remove nulled obejct

                    if (result == 0)
                        UserProperties["controlcount"].Element.Value = UserProperties["objects"].Col;
                }
            }
            else
                result = 3019;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{me.JOWName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Get an object by index from the objects array
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<JAXObjectWrapper?> GetObject(int idx)
        {
            JAXObjectWrapper? jaxClass = null;

            if (CanUseObjects == false) throw new Exception("3019|");
            if (CanReadObjects)
            {
                if (idx >= 0 && UserProperties["objects"].Count > idx)
                {
                    UserProperties["objects"].ElementNumber = idx;
                    jaxClass = (JAXObjectWrapper)UserProperties["objects"].Element.Value;
                }
            }

            if (jaxClass is null)
                throw new Exception(string.Format("Object index ({0}) is out of bounds", idx));

            return jaxClass;
        }

        /*------------------------------------------------------------------------------------------*
         * Get an object by name from the objects array, returning the object and index
         * in the objects array
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<JAXObjectWrapper?> GetObject(string objectname)
        {
            JAXObjectWrapper? jaxClass = null;

            if (CanReadObjects)
            {
                int count = UserProperties["objects"].Count;

                for (int i = 0; i < count; i++)
                {
                    UserProperties["objects"].ElementNumber = i;
                    JAXObjectWrapper o = (JAXObjectWrapper)UserProperties["objects"].Element.Value;
                    JAXObjects.Token tk = await o.GetProperty("name");
                    string name = tk.Element.Type.Equals("C") ? tk.AsString().ToUpper() : string.Empty;
                    if (name.Equals(objectname.ToUpper()))
                    {
                        jaxClass = o;
                        jaxClass.IDX = i;
                        break;
                    }
                }
            }

            return jaxClass;
        }


        /*------------------------------------------------------------------------------------------*
         * Get a list trio (Name,Type,Tag) of all Properties, Methods, and Events in the class
         * along with Tag = (U)ser or (S)ystem
         *------------------------------------------------------------------------------------------*/
        public virtual List<GenericClass> GetPEMList()
        {
            List<GenericClass> results = [];
            foreach (KeyValuePair<string, JAXObjects.Token> tk in UserProperties)
            {
                GenericClass listItem = new()
                {
                    Name = tk.Key,
                    Type = "P",
                    Tag = tk.Value.Tag
                };

                results.Add(listItem);
            }

            foreach (KeyValuePair<string, MethodClass> tk in Methods)
            {
                GenericClass listItem = new()
                {
                    Name = tk.Key,
                    Type = tk.Value.Type,
                    Tag = tk.Value.Tag
                };

                results.Add(listItem);
            }

            return results;
        }


        /*- Virtual method -------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here to get the value of the 
         * property from the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token? returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (CanReadObjects || propertyName.Equals("objects", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    if (JAXLib.Between(idx, 0, UserProperties[propertyName]._avalue.Count - 1))
                        returnToken = new(UserProperties[propertyName]._avalue[idx].Value);
                    else
                        result = 3028;
                }
                else
                    result = 1559;
            }
            else
            {
                if (CanUseObjects)
                    result = 3023;
                else
                    result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                returnToken.Element.IsNull();
            }

            return returnToken;
        }

        /*------------------------------------------------------------------------------------------*
         * Get an object from the OBJECTS array
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<JAXObjects.Token> GetObjectProperty(int idx, string propertyName)
        {
            int result = 0;
            JAXObjects.Token? objToken = new();
            propertyName = propertyName.ToLower();

            if (CanReadObjects && UserProperties.TryGetValue("objects", out JAXObjects.Token? value))
            {
                if (idx < 0 || idx >= value.Row)
                {
                    // Out of the bounds of the array's index
                    result = 3003;
                }
                else
                {
                    // Found the object array, return the correct index
                    // Objects are always JAXObjectWrappers types
                    value.SetElement(idx, 1);
                    JAXObjectWrapper o = (JAXObjectWrapper)value.Element.Value;
                    objToken = await o.GetProperty(propertyName);
                }
            }
            else
            {
                // Object not found or not available
                result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                objToken.Element.MakeNull();
            }

            return objToken;
        }

        /*------------------------------------------------------------------------------------------*
         * Set the default value for this control
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<int> SetDefault(string cmd)
        {
            int result = 0;
            XClass_AuxCode.SetDefault(me, cmd).Wait();
            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Set an object[] property
         *------------------------------------------------------------------------------------------*/
        public virtual int SetObjectProperty(int idx, string propertyName, JAXObjects.Token value)
        {
            int result = 0;
            propertyName = propertyName.ToLower();

            if (CanWriteObjects)
            {
                // TODO - set the object property
                JAXObjectWrapper jow = (JAXObjectWrapper)UserProperties["objects"]._avalue[idx].Value;
                jow.SetProperty(propertyName, value).Wait();
            }
            else
            {
                if (IsMember("objects").Equals("P"))
                    result = 3025;
                else
                    result = 3019;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*- Virtual Method -------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here for basic storing of the 
         * property to the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;

            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;

            if (InInit == false)
                AppIO.DebugLog($"MyObj={me.JOWName} BASE.{propertyName}={objtk.AsString()}");

            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        default:
                            if (string.IsNullOrWhiteSpace(UserProperties[propertyName].Element._setAsType) || UserProperties[propertyName].Element._setAsType.Equals(objtk.Element.Type))
                                UserProperties[propertyName].Element.Value = objValue;
                            else
                                result = 9;
                            break;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Add a property to the class with a default .F. value
         *------------------------------------------------------------------------------------------*/
        public virtual int AddProperty(string propertyName)
        {
            JAXObjects.Token token = new();
            return AddProperty(propertyName, token);
        }

        /*------------------------------------------------------------------------------------------*
         * Add a property object to the class passed as a JAX Object Wrapper
         *------------------------------------------------------------------------------------------*/
        public virtual int AddProperty(string propertyName, JAXObjectWrapper token)
        {
            JAXObjects.Token tk = new();
            tk.Element.Value = token;
            return AddProperty(propertyName, tk);
        }

        /*------------------------------------------------------------------------------------------*
         * Add a property to the class with a value passed as a var token
         *------------------------------------------------------------------------------------------*/
        public virtual int AddProperty(string propertyName, JAXObjects.Token token)
        {
            int result = 0;

            propertyName = propertyName.ToLower();

            // Not a form object, so try to add as a user property
            if (UserProperties.ContainsKey(propertyName) || Methods.ContainsKey(propertyName))
                result = 1771;
            else
                UserProperties.Add(propertyName, token);

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * Set the method's code.  If sent source and not compiled code, then compile
         * the source.  If sent compiled code, save it and ignore sourece if sent.
         * If sent neither, clear out the compiled code.
         * 
         * First time through?  Need to set the type to (M)ethod, (E)vent, or (U)ser
         * defined method.
         *------------------------------------------------------------------------------------------*/
        public virtual int _SetMethod(string methodName, string SourceCode, bool createOK, string methodType)
        {

            MethodClass? mc = null;
            methodName = methodName.ToLower();

            string CompCode = "";
            int result = 0;

            if (me.BaseClass.Equals("empty", StringComparison.OrdinalIgnoreCase))
                result = 6598;
            else if (UserProperties.ContainsKey(methodName))    // Is this trying to overwrite a property?
                result = 1738;
            else
            {
                // Does the method already exist?
                if (Methods.TryGetValue(methodName, out MethodClass? value))
                {
                    // Get the current method definition and update the source code
                    mc = value;
                    mc.PrgCall = SourceCode;
                }
                else
                {
                    if (createOK)
                    {
                        // Create a new method definition
                        mc = GetMethod(methodName);
                        mc.PrgCall = SourceCode;

                        if (string.IsNullOrWhiteSpace(methodType) == false)
                        {
                            mc.Type = methodType[..1].ToUpper();
                            //mc.Tag = Type.Contains('!') ? "N" : "U";    // Finding a ! means it's a native method
                            mc.Inherited = methodType.Contains('#'); // Inherited 
                            if ("MEU".Contains(mc.Type) == false) throw new Exception("Invalid method type: " + mc.Type);
                        }
                    }
                    else
                        result = 6501;
                }
            }

            // Is there some source code to compile?
            if (result == 0 && SourceCode.Length > 0)
            {
                CompCode = Program.CurrentApp.JaxCompiler.CompileBlock(SourceCode, true, out int errorCount);

                if (errorCount > 0)
                    result = 9997;
            }

            if (result == 0)
            {
                // Update the compiled code
                mc!.CompiledCode = CompCode;

                // Store the method definition
                if (!Methods.TryAdd(methodName, mc))
                    Methods[methodName] = mc;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * Call the JAXCode for a method
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<int> _CallMethod(string methodName)
        {
            int results = 0;
            string msg = "";

            Program.CurrentApp.ReturnValue.Element.Value = true;

            try
            {
                if (Methods.ContainsKey(methodName.ToLower()))
                {
                    string cCode = Methods[methodName.ToLower()].CompiledCode;

                    // Create a new App.Levels and execute the code
                    if (cCode.Length > 0)
                    {
                        //AppIO.DebugLog($"_CallMethod for {methodName} start ─ this: {this.GetHashCode()}  me: {me?.GetHashCode() ?? -1}  me.Name: {me?.Name ?? "?"}", false);

                        //// Call the routine to compile and execute a block of code
                        _ = Program.CurrentApp.JaxExecutor.ExecuteCodeBlock(me!, methodName, cCode);

                    }
                    else
                        results = await DoDefault(methodName);
                }
                else
                    results = 6501;

            }
            catch (Exception ex)
            {
                msg = ex.Message;
                results = 9999;
            }

            if (results > 0)
            {
                _AddError(results, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(results, $"{results}|{methodName}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                results = -1;
            }

            return results;
        }

        public virtual async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower();

            if (Methods.ContainsKey(methodName))
            {
                //string mcall = Methods[methodName].PrgCall;
                //AppIO.DebugLog($"DODEFAULT - Method {methodName} for class {UserProperties["class"].AsString()} - {me.Name} - {UserProperties["classid"].AsString()} - Source {mcall}");

                switch (methodName)
                {
                    case "addobject":
                        // TODO - Only certain classes can have objects added to them
                        if (Program.CurrentApp.ParameterClassList.Count == 1 && Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("O"))
                        {
                            // JAXBase can accept an object in ADDOBJECT()
                            AddObject((JAXObjectWrapper)Program.CurrentApp.ParameterClassList[0].token.Element.Value).Wait();
                        }
                        else
                        {
                            // we're expecting cName, cClass [,aInit1, aInit2...]
                            if (Program.CurrentApp.ParameterClassList.Count == 0)
                                result = 1229;
                            else
                            {
                                List<JAXObjects.Token> ParameterList = [];
                                foreach (ParameterClass p in Program.CurrentApp.ParameterClassList)
                                {
                                    JAXObjects.Token t = new();
                                    object? obj = AppHelper.GetParameterValue(p);
                                    if (obj is null)
                                        t.Element.MakeNull();
                                    else
                                        t.Element.Value = obj;

                                    ParameterList.Add(t);
                                }

                                me.AddObjectUsingParameters(ParameterList).Wait();
                            }
                        }
                        break;

                    case "error":
                        if (Program.CurrentApp.CurrentDS.JaxSettings.ErrorClassReporting)
                        {
                            // We are supposed to report the error
                            if (UserProperties.TryGetValue("aerror", out JAXObjects.Token? tk))
                            {
                                if (tk._avalue[0].Type.Equals("N") && tk._avalue[0].ValueAsInt > 0)
                                {
                                    // we have an actual error to report!
                                    AppErrorHandling.SetError(tk._avalue[0].ValueAsInt, tk._avalue[2].ValueAsString, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                }
                            }
                        }
                        break;

                    case "errormessage":
                        // Show an Invalid Input message in upper right
                        Program.CurrentApp.WaitWindow = JAXLib.WaitWindow(Program.CurrentApp, "Invalid Input", -1, -1, false, false, 3, out _);
                        break;


                    case "lostfocus":
                        // Does this control have a validation clause and has it been validated?
                        if (Program.CurrentApp.ReturnValue.AsBool() && Methods.ContainsKey("valid") && me.Validated == false)
                        {
                            // Yes, so call that and decide if we can leave the control
                            // and in what direction
                            me.MethodCall("valid").Wait();

                            if (Program.CurrentApp.ReturnValue.Element.Type.Equals("L"))
                            {
                                if (Program.CurrentApp.ReturnValue.AsBool() == false)
                                {
                                    // Don't leave this control
                                }
                            }
                            else if (Program.CurrentApp.ReturnValue.Element.Type.Equals("N"))
                            {
                                // Potentially moving forward or backward
                            }
                            else
                                result = 11;
                        }
                        break;

                    case "refresh":
                        me.avaloniaObject?.InvalidateVisual();
                        break;

                    case "resettodefault":
                        // Only certain objects can reset to default
                        if (Program.CurrentApp.ParameterClassList.Count == 1 && Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                            result = await ResetPropertyToDefault(Program.CurrentApp.ParameterClassList[0].token.AsString());
                        else
                            result = 1559;

                        break;

                    case "setfocus":
                        //AppIO.DebugLog($"Setfocus default code start ─ this: {this.GetHashCode()}  me: {me?.GetHashCode() ?? -1}  me.Name: {me?.Name ?? "?"}", false);
                        if (me.avaloniaObject is not null)
                            me.avaloniaObject.Focus();
                        break;

                    case "show":
                        if (JAXLib.InListC(me.BaseClass, "form", "formset", "browser"))
                            result = await me.Show();
                        break;

                    case "writemethod":
                        // Only some classes allow method code to be written at runtime
                        string cMethodName = (Program.CurrentApp.ParameterClassList.Count > 0) ? Program.CurrentApp.ParameterClassList[0].token.AsString() : string.Empty;
                        string cSourceCode = (Program.CurrentApp.ParameterClassList.Count > 1) ? Program.CurrentApp.ParameterClassList[1].token.AsString() : string.Empty;
                        bool lWriteNew = (Program.CurrentApp.ParameterClassList.Count > 2) && Program.CurrentApp.ParameterClassList[2].token.AsBool();

                        result = me.SetMethod(cMethodName, cSourceCode, lWriteNew);
                        break;

                    default:
                        break;
                }

                if (result > 0)
                {
                    _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                    if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                        AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                }
            }

            return result;
        }

        public virtual async Task MakeNextDefaultName(JAXObjectWrapper value)
        {
            JAXObjects.Token? tk = await value.GetProperty("name");

            if (tk is not null)
            {
                string name = tk.AsString();
                if (name.Equals(value.DefaultName(), StringComparison.OrdinalIgnoreCase))
                {
                    JAXObjects.Token objects = UserProperties["objects"];
                    int icount = objects.Row * objects.Col;
                    int ncount = 1;

                    if (icount == 0)
                        name += "1";
                    else
                    {
                        // Find the highest default name in the objects list
                        for (int i = 0; i < icount; i++)
                        {
                            JAXObjectWrapper jow = (JAXObjectWrapper)objects._avalue[i].Value;
                            if ((tk = await jow.GetProperty("name")) is not null)
                            {
                                string tname = tk.AsString();
                                if (tname.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    // found a default name match so look for the highest
                                    // default name number out of the list
                                    while (tname.CompareTo($"{name}{ncount}") >= 0)
                                        ncount++;
                                }
                            }
                        }

                        // finalize the name
                        name += $"{ncount}";
                    }

                    value.SetProperty("name", name).Wait();
                }
            }
        }


        /*------------------------------------------------------------------------------------------*
         *------------------------------------------------------------------------------------------*/
        public virtual string DefaultName() { return MyDefaultName; }

        /*------------------------------------------------------------------------------------------*
         * List of properties registered to the class
         *------------------------------------------------------------------------------------------*/
        public virtual string[] JAXProperties() { return []; }

        /*------------------------------------------------------------------------------------------*
         * List of methods registered to the class
         *------------------------------------------------------------------------------------------*/
        public virtual string[] JAXMethods() { return []; }

        /*------------------------------------------------------------------------------------------*
         * List of events registered to the class
         *------------------------------------------------------------------------------------------*/
        public virtual string[] JAXEvents() { return []; }


        /*------------------------------------------------------------------------------------------*
         * Resets a property to it's default value
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<int> ResetPropertyToDefault(string property)
        {
            int result = 0;
            XClass_AuxCode.ResetPropertyToDefault(me, property).Wait();
            return result;
        }


        /*
         * Set a property of all like classes
         * Deep dive through all child objects
         */
        public virtual void SetAllOfClass(string Class, string propertyName, JAXObjects.Token objtk)
        {
            if (UserProperties.TryGetValue("objects", out JAXObjects.Token? otk))
            {
                for (int i = 0; i < otk._avalue.Count; i++)
                {
                    // If it's a button then adjust the property
                    // Protection, but should always be true
                    if (otk._avalue[i].Value is JAXObjectWrapper itk)
                    {
                        if (itk.Class.Equals(Class, StringComparison.OrdinalIgnoreCase))
                            if (UserProperties.ContainsKey(propertyName))
                                SetObjectProperty(i, propertyName, objtk);

                        // Deep dive
                        itk.thisObject!.SetAllOfClass(Class, propertyName, objtk);
                    }
                }

            }
        }


        /*
         * Set a property of all like baseclasses
         * Deep dive through all child objects
         */
        public virtual void SetAllOfBaseClass(string BaseClass, string propertyName, JAXObjects.Token objtk)
        {
            if (UserProperties.TryGetValue("objects", out JAXObjects.Token? otk))
            {
                // Deep dive first
                SetAllOfClass(BaseClass, propertyName, objtk);
                for (int i = 0; i < otk._avalue.Count; i++)
                {
                    // If it's a button then adjust the property
                    // Protection, but should always be true
                    if (otk._avalue[i].Value is JAXObjectWrapper itk)
                    {
                        if (itk.Class.Equals(BaseClass, StringComparison.OrdinalIgnoreCase))
                            if (UserProperties.ContainsKey(propertyName))
                                SetObjectProperty(i, propertyName, objtk);

                        // Deep dive
                        itk.thisObject!.SetAllOfClass(BaseClass, propertyName, objtk);
                    }
                }
            }
        }


        /*------------------------------------------------------------------------------------------*
         * Add a property, with value, and locked to a specific var type
         *------------------------------------------------------------------------------------------*/
        public virtual int AddProperty(string propertyName, string lockType, string lockValue)
        {
            int result = 0;
            XClass_AuxCode.AddLockedProperty(me, propertyName, lockType, lockValue);
            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Returns a bool on whether the provided name is a property in the object
         *------------------------------------------------------------------------------------------*/
        public virtual bool HasProperty(string propertyName) { return UserProperties.ContainsKey(propertyName.ToLower().Trim()); }

        /*------------------------------------------------------------------------------------------*
         * Returns a string determining if the name is a member of the control
         * M - Method/Event
         * P - Property
         * O - Object
         * U - Unknown
         *------------------------------------------------------------------------------------------*/
        public virtual async Task<string> IsMember(string name)
        {
            string isMember = "U";

            if (name.Equals("exec",StringComparison.OrdinalIgnoreCase))
            {
                int iii = 0;
            }

            if (UserProperties.ContainsKey(name.ToLower())) // Is it a property?
                isMember = "P";
            else if (Methods.ContainsKey(name.ToLower()))   // Is it a method/event?
                isMember = "M";
            else
            {
                if (UserProperties.TryGetValue("objects", out JAXObjects.Token? Objs))
                {
                    // Is it an object?
                    int cCount = UserProperties["controlcount"].AsInt();
                    for (int i = 0; i < cCount; i++)
                    {
                        Objs.ElementNumber = i;
                        JAXObjectWrapper oname = (JAXObjectWrapper)Objs.Element.Value;
                        JAXObjects.Token tk;

                        if ((tk = await oname.GetProperty("name", 0)) is not null)
                        {
                            string nam = tk.Element.Type.Equals("C") ? tk.AsString() : string.Empty;
                            if (nam.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                isMember = "O";
                                break;
                            }
                        }
                    }
                }
            }

            return isMember;
        }

        public virtual int GetPrivateProperty(string propertyName, out JAXObjects.Token value)
        {
            int result = 0;
            value = new();

            try
            {
                propertyName = propertyName.ToLower();
                if (PrivateProperties.TryGetValue(propertyName, out JAXObjects.Token? value1))
                    value.CopyFrom(value1);
                else
                {
                    value.Element.MakeNull();
                    result = 9902;
                }
            }
            catch (Exception ex)
            {
                result = 9901;
                value.Element.MakeNull();
                AppIO.DebugLog($"Error in GetPrivateProperty for class {UserProperties["name"]} - {ex.Message}");
            }

            return result;

        }

        public virtual int SetPrivateProperty(string propertyName, object? value)
        {
            int result = 0;

            try
            {
                propertyName = propertyName.ToLower();
                if (PrivateProperties.ContainsKey(propertyName) == false)
                    PrivateProperties.Add(propertyName, new());

                if (value is null)
                    PrivateProperties[propertyName].Element.MakeNull();
                else
                    PrivateProperties[propertyName].Element.Value = value;
            }
            catch (Exception ex)
            {
                result = 9901;
                AppIO.DebugLog($"Error in SetPrivateProperty for class {UserProperties["name"]} - {ex.Message}");
            }

            return result;
        }



        /*------------------------------------------------------------------------------------------*
         *------------------------------------------------------------------------------------------*
         * JAXBase Visual Class events
         * 
         * Look at C:\Users\jlw61\OneDrive\Desktop\Grok\CSharp\FormGetLostFocus for more info
         * on how to track movement between controls.  Will need to create a link to THISFORM,
         * and it's about time I did that anyway.  Add in THISFORMSET!
         *------------------------------------------------------------------------------------------*
         *------------------------------------------------------------------------------------------*/
        public virtual void MyObj_LostFocus(object? sender, EventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("valid"))
            {
                _CallMethod("valid").Wait();

                if (Program.CurrentApp.ReturnValue.Element.Type.Equals("L") && Program.CurrentApp.ReturnValue.AsBool())
                    _CallMethod("LostFocus").Wait();
            }
            else if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("lostfocus"))
                _CallMethod("lostfocus").Wait();

            // If by tab/shift+tab then set direction of travel
        }

        public virtual void MyObj_GotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive)
            {
                bool OK2Enter = true;

                // If there is a when event
                if (Methods.ContainsKey("when"))
                {
                    _CallMethod("when").Wait();

                    // OK to enter?
                    if (Program.CurrentApp.ReturnValue.Element.Type.Equals("L"))
                        OK2Enter = Program.CurrentApp.ReturnValue.AsBool();
                    else
                        throw new Exception("11|");
                }

                if (OK2Enter)
                {
                    // Update so validation works correctly in each control
                    me.Validated = false;
                    me.MoveDirection = 1;

                    if (e.NavigationMethod == NavigationMethod.Tab)
                    {
                        // Shift key pressed = backward (previous control, lower in hierarchy/tab order)
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            me.MoveDirection = -1;
                    }


                    // Entering this control
                    if (Methods.ContainsKey("gotfocus"))
                        _CallMethod("gotfocus").Wait();

                    // Update what control we're in
                }
                else
                {
                    // If move was based on TAB/SHIFT+TAB then go to the
                    // next control in direction of travel.  Otherwise go
                    // back to the control we were in.
                    switch (e.NavigationMethod)
                    {
                        case NavigationMethod.Pointer:
                            // Mouse Click
                            break;

                        case NavigationMethod.Tab:
                            // Check if shift is pressed
                            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                            {
                                // SHIFT+TAB
                            }
                            else
                            {
                                // TAB
                            }
                            break;


                        case NavigationMethod.Directional:
                            // Arrow keys so is it back or forward?
                            if (Program.CurrentApp.LastKeyPressed is not null)
                            {
                                if (Program.CurrentApp.LastKeyPressed.Value == Key.Up || Program.CurrentApp.LastKeyPressed.Value == Key.Left)
                                {
                                    // back
                                }
                                else
                                {
                                    // forward
                                }
                            }
                            break;

                        default:
                            // unknown
                            break;
                    }
                }
            }
        }

        public virtual void MyObj_MouseWheel(object? sender, PointerWheelEventArgs e)
        {
            // Should only be called when over an Avalonia object
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mousewheel") && me.THISFORM is not null)
            {
                //Avalonia.Controls.Canvas _canvas = (Avalonia.Controls.Canvas)me.THISFORM.avaloniaObject!;
                double deltaY = e.Delta.Y;     // This is the important value

                MouseButtonAction("mousewheel", false, e, deltaY > 1 ? 1 : -1);
            }
        }

        public virtual void MyObj_MouseMove(object? sender, PointerEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mousemove"))
                MouseButtonAction("mousemove", e);
        }

        public virtual void MyObj_MouseUp(object? sender, PointerEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mouseup"))
                MouseButtonAction("mouseup", e);
        }

        public virtual void MyObj_MouseDown(object? sender, PointerEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mousedown"))
                MouseButtonAction("mousedown", e);
        }

        public virtual void MyObj_MouseEnter(object? sender, PointerEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mouseenter"))
                MouseButtonAction("mouseenter", false, e);
        }

        private void MouseButtonAction(string cMethod, PointerEventArgs e)
        {
            MouseButtonAction(cMethod, true, e);
        }

        private void MouseButtonAction(string cMethod, bool checkButton, PointerEventArgs e)
        {
            MouseButtonAction(cMethod, checkButton, e, 0);
        }

        public virtual void MyObj_MouseLeave(object? sender, PointerEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("mouseleave"))
                MouseButtonAction("mouseleave", false, e);
        }

        public virtual void MyObj_DoubleClick(object? sender, EventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("doubleclick"))
                _CallMethod("doubleclick").Wait();
        }

        //public virtual void MyObj_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        //{
        //    if (App.EventsAreActive)
        //    {
        //        AppIO.DebugLog($"XBASE - {me.JOWName}.click - {UserProperties["name"].AsString()} - {UserProperties["classid"].AsString()}");

        //        if (Methods.ContainsKey("click"))
        //        {
        //            App.EventsAreActive = false;
        //            me.MethodCall("click").Wait();
        //            App.EventsAreActive = true;
        //        }
        //    }
        //}

        public virtual void MyObj_Move(object? sender, EventArgs? e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("moved"))
                _CallMethod("moved").Wait();
        }

        public virtual void MyObj_KeyPress(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive)
            {
                Program.CurrentApp.LastKeyPressed = e.Key;

                // Set parameters nKeyCode, nShiftAltCtrl
                if (Methods.ContainsKey("keypress"))
                    _CallMethod("keypress").Wait();
            }
        }

        public virtual void HandleTapped(object? sender, TappedEventArgs e)
        {
            e.Handled = true;

            if (Program.CurrentApp.EventsAreActive)
            {
                AppIO.DebugLog($"XBASE - {me.JOWName}.click - {UserProperties["name"].AsString()} - {UserProperties["classid"].AsString()}");

                if (Methods.ContainsKey("click"))
                {
                    Program.CurrentApp.EventsAreActive = false;
                    me.MethodCall("click").Wait();
                    Program.CurrentApp.EventsAreActive = true;
                }
            }
        }


        /*
         * Everything comes here in order to call a mouse event
         * 
         * And here's something you can do in avalonia.  Don't know
         * if there's going to be a need for it, but it's there.
         * 
         * var visuals = this.GetVisualsAt(mousePos);
         * var topControl = visuals.FirstOrDefault();
         * 
         * if (topControl != null)
         * {
         *     Console.WriteLine($"Mouse is over: {topControl.GetType().Name}");
         * }
         * 
         */
        public virtual void MouseButtonAction(string cMethod, bool checkButton, PointerEventArgs e, int wheelDelta)
        {
            int nShift = 0;
            int nButton = 0;

            // any control keys pressed?
            var modifiers = e.KeyModifiers;
            if (modifiers.HasFlag(KeyModifiers.Shift)) nShift += 1;
            if (modifiers.HasFlag(KeyModifiers.Control)) nShift += 2;
            if (modifiers.HasFlag(KeyModifiers.Alt)) nShift += 4;

            double xCoord;
            double yCoord;

            Avalonia.Visual? obj = (me.THISFORM is null || me.THISFORM.avaloniaObject is null) ? null : (Avalonia.Controls.Canvas)me.THISFORM!.avaloniaObject!;
            Avalonia.Point pnt = e.GetPosition(obj);
            xCoord = pnt.X;
            yCoord = pnt.Y;

            Program.CurrentApp.ParameterClassList.Clear();

            // Is this a button or wheel event?
            if (checkButton)
            {
                Avalonia.Input.PointerPoint p = e.GetCurrentPoint(obj);
                if (p.Properties.IsLeftButtonPressed) nButton += 1;
                if (p.Properties.IsRightButtonPressed) nButton += 2;
                if (p.Properties.IsMiddleButtonPressed) nButton += 4;

                Program.CurrentApp.ParameterClassList.Add(new(nButton));
            }
            else if (wheelDelta != 0)
                Program.CurrentApp.ParameterClassList.Add(new(wheelDelta));

            // All mouse actions have these parameters
            Program.CurrentApp.ParameterClassList.Add(new(nShift));
            Program.CurrentApp.ParameterClassList.Add(new(xCoord));
            Program.CurrentApp.ParameterClassList.Add(new(yCoord));

            _CallMethod(cMethod).Wait();
        }


        public virtual void MyObj_Resize(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            AppIO.DebugLog($">>>>>MyObj_Resize: {me.JOWName} - NewSize=({e.NewSize.Width}, {e.NewSize.Height}), PreviousSize=({e.PreviousSize.Width}, {e.PreviousSize.Height})");
            //me.MethodCall("resize").Wait();
        }

        public virtual void MyObj_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //me.MethodCall("init");
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected disposal
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                me.Release();   // JAXObjectWraper release logic
                CleanUp(disposing);
            }
        }

        // Finalizer as fallback.
        ~XBase_Avalonia()
        {
            Dispose(false);
        }


        // We're shutting things down
        public virtual void CleanUp(bool disposing)
        {
            ClearEvents(disposing);
        }

        // Didn't want to rename this yet in case I needed
        // some more code during the destroy event
        public virtual void ClearEvents(bool disposing)
        {
            SuspendEvents();
        }

        public virtual void SuspendEvents()
        {
            if (me.avaloniaObject is not null)
            {
                me.avaloniaObject.Tapped -= HandleTapped;
                me.avaloniaObject.DoubleTapped -= MyObj_DoubleClick;
                me.avaloniaObject.PointerEntered -= MyObj_MouseEnter;
                me.avaloniaObject.PointerExited -= MyObj_MouseLeave;
                me.avaloniaObject.PointerMoved -= MyObj_MouseMove;
                me.avaloniaObject.PointerPressed -= MyObj_MouseDown;
                me.avaloniaObject.PointerReleased -= MyObj_MouseUp;
                me.avaloniaObject.PointerWheelChanged -= MyObj_MouseWheel;

                me.avaloniaObject.GotFocus -= MyObj_GotFocus;
                me.avaloniaObject.LostFocus -= MyObj_LostFocus;
                me.avaloniaObject.Loaded -= MyObj_Loaded;

                if (JAXLib.InListC(me.BaseClass, "form", "container", "page"))
                    me.avaloniaObject.SizeChanged -= CanvasResized;
                else
                    me.avaloniaObject.SizeChanged -= MyObj_Resize;

                me.avaloniaObject.KeyDown -= MyObj_KeyPress;
            }
        }

        public virtual void SetEvents()
        {
            if (me.avaloniaObject is not null)
            {
                me.avaloniaObject.Tapped += HandleTapped;
                //me.avaloniaObject.Tapped += MyObj_Click;
                me.avaloniaObject.DoubleTapped += MyObj_DoubleClick;
                me.avaloniaObject.PointerEntered += MyObj_MouseEnter;
                me.avaloniaObject.PointerExited += MyObj_MouseLeave;
                me.avaloniaObject.PointerMoved += MyObj_MouseMove;
                me.avaloniaObject.PointerPressed += MyObj_MouseDown;
                me.avaloniaObject.PointerReleased += MyObj_MouseUp;
                me.avaloniaObject.PointerWheelChanged += MyObj_MouseWheel;

                me.avaloniaObject.GotFocus += MyObj_GotFocus;
                me.avaloniaObject.LostFocus += MyObj_LostFocus;

                me.avaloniaObject.Loaded += MyObj_Loaded;

                if (JAXLib.InListC(me.BaseClass, "form", "container", "page"))
                    me.avaloniaObject.SizeChanged += CanvasResized;
                else
                    me.avaloniaObject.SizeChanged += MyObj_Resize;

                me.avaloniaObject.KeyDown += MyObj_KeyPress;
            }
        }

        // Add these two virtual methods for easy overriding in derived classes
        public virtual void MyObj_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("closing"))
            {
                // You can expose e.Cancel to JAX code via ReturnValue or a property
                _CallMethod("queryunload").Wait();

                // Example: allow JAX code to cancel by returning .F.
                if (Program.CurrentApp.ReturnValue.Element.Type.Equals("L") && Program.CurrentApp.ReturnValue.AsBool() == false)
                {
                    e.Cancel = true;
                }
            }
        }

        public virtual void MyObj_Closed(object? sender, EventArgs e)
        {
            if (Program.CurrentApp.EventsAreActive && Methods.ContainsKey("closed"))
            {
                _CallMethod("destroy").Wait();
            }

            // Optional: auto-dispose after window is fully closed
            Dispose();
        }

        public virtual void ApplyVFPAnchor(double DeltaX, double DeltaY) { }

        public virtual void CanvasResized(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            // Dummy handler which will be overridden in XBase_Avalonia_Class
            AppIO.DebugLog($">>>>>WRONG CanvasResized: {me.JOWName} - NewSize=({e.NewSize.Width}, {e.NewSize.Height}), PreviousSize=({e.PreviousSize.Width}, {e.PreviousSize.Height})");
        }
    }
}
