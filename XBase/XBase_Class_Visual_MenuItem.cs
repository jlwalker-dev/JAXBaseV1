/*------------------------------------------------------------------------------------------*
 * MenuItem Class
 * 
 * 2025.11.17 - JLW
 *      Created this component of the menu class.  You add menu items to a menu
 *      by creating a object of this class and adding it to the menu object.
 *      You can also add menuitem objects to this object to create sub-menus.
 *      
 *      Not actually a visual object, but a component of the menu which is
 *      a visual object.
 *      
 *      Limited properties, events and methods at this time as I'm just looking 
 *      for basic functionality.
 *      
 * 2025.12.11 - JLW
 *      First full success with building and use a menu!
 *      Took a few days and google searches to learn more about events in C#
 *      than I really thought I would ever need to learn.  It's so much more
 *      complicated than XBase!  While .Net seems to have more capabilities, 
 *      XBase works just fine with the simpler interface.
 *      
 * 2025.02.24 - JLW
 *      Converting to Avalonia.  Unlike WinForms, the menu item is a visual
 *      object and thus less monkeying around to make it work.
 *      
 *------------------------------------------------------------------------------------------*/
using Avalonia.Input;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_MenuItem : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "MenuItem";
        public new string MyDefaultName { get; } = "menuitem";


        public Avalonia.Controls.MenuItem Menuitem => (Avalonia.Controls.MenuItem)me.avaloniaObject!;
        
        public XBase_Class_Visual_MenuItem(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.MenuItem(), "MenuItem", "menuitem", true, UserObject.URW);

            // Detach the generic Tapped event to prevent it from firing on non-leaf items
            //Menuitem.Tapped -= MyObj_Click;

            // Attach a handler to mark Tapped as handled to prevent bubbling
            //Menuitem.Tapped += HandleTapped;

            // Attach to the MenuItem.Click event for leaf items
            //Menuitem.Click += MyObj_Click;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        /*------------------------------------------------------------------------------------------*
         * Add an object to the end of the objects array
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            if (CanUseObjects == false) err = 3019;

            if (err == 0 && CanWriteObjects)
            {
                if (me.avaloniaObject is not null)
                {
                    // Add the menu item to the menu
                    if (value.avaloniaObject is null)
                        err = 1901;
                    else if (JAXLib.InListC(value.BaseClass, "menuitem", "separator"))
                    {
                        value.SetParent(me);
                        await value.SetProperty("tag", me.ClassID);
                        await base.MakeNextDefaultName(value);

                        if (value.BaseClass.ToLower() == "separator")
                            Menuitem.Items.Add((Avalonia.Controls.Separator)value.avaloniaObject);
                        else
                        {
                            Avalonia.Controls.MenuItem obj = (Avalonia.Controls.MenuItem)value.avaloniaObject;
                            Menuitem.Items.Add(obj);
                        }
                    }
                    else
                        err = 1903;
                }

                if (err == 0)
                {
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                }
            }
            else
                err = 3019;

            if (err > 0)
            {
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", string.Empty);

            }
            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }



        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns 
         *      0 = Successfully returning value
         *     -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                // Get the property and fill in the value
                returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    // Intercept special handling of properties
                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 0, 10))
                {
                    result = 0;
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]);
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *      -1  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    // Intercept special handling of properties
                    switch (propertyName)
                    {
                        case "caption":
                            if (tk.Element.Type.Equals("C"))
                                Menuitem.Header = (objValue.ToString() ?? string.Empty).Replace("\\<","_");
                            else
                                result = 11;
                            break;

                        case "hotkey":
                            if (tk.Element.Type.Equals("C"))
                            {
                                string keyinfo = tk.AsString().ToUpper().Trim();
                                if (keyinfo.Length > 0)
                                {
                                    string akeyinfo = keyinfo.Replace("CTRL", "Control").Replace("ALT", "Alt").Replace("SHIFT", "Shift").Replace("ESC", "Escape");
                                    akeyinfo=akeyinfo.Replace("UPARROW", "Up").Replace("DNARROW", "Down").Replace("LEFTARROW", "Left").Replace("RIGHTARROW", "Right");

                                    try
                                    {
                                        AppIO.DebugLog($"Parsing hotkey: {akeyinfo}");
                                        Menuitem.HotKey = Avalonia.Input.KeyGesture.Parse(akeyinfo);
                                        Menuitem.InputGesture = Avalonia.Input.KeyGesture.Parse(akeyinfo);
                                        objValue = keyinfo;
                                    }
                                    catch
                                    {
                                        result = 9860; // Invalid hotkey expression
                                    }
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "icon":
                            if (tk.Element.Type.Equals("C"))
                            {
                                // Registered Icon or file name
                                //Menuitem.Icon = JAXImages();
                            }
                            else
                                result = 11;
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // We don't save what we don't process
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;


            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }
            else
                result = 0;

            return result;
        }



        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","addobject","readexpression","readmethod","refresh","removeobject",
                "saveasclass","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","error","gotfocus","lostfocus",
                "mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","visiblechanged","writemethod","when"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - can't change after initialization
         *          $ Special Handling - do not auto process
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
                [
                "baseclass,C!,menuitem","backcolor,r,255|255|255","backstyle,n,0",
                "caption,c,","class,C!,menuitem","classlibrary,C!,","comment,c,","controlcount,n,0",
                "enabled,l,.t.",
                "fontBold,L,false","fontitalic,L,false","fontname,C,Arial","fontsize,N,9","forecolor,r,0",
                "hotkey,c,",
                "icon,c,",
                "name,c,",
                "objects,*,",
                "parent,o!,","parentclass,c!,","picture,c,",
                "righttoleft,L,false",
                "tag,c,","tooltiptext,c,",
                "visible,l,.t.",
                ];
        }

        //private void HandleTapped(object? sender, TappedEventArgs e)
        //{
        //    e.Handled = true;
        //}
    }
}
