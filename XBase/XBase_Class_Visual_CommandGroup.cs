/*------------------------------------------------------------------------------------------*
 * Command Group
 * 
 *  * TODO - ButtonLayout (add)
 *      3 - Vertical stack auto size borders
 *      4 - Horizontal stack auto size borders
 *------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_CommandGroup : XBase_Class_Avalonia
    {
        public Avalonia.Controls.Canvas cmdGroup => (Avalonia.Controls.Canvas)me.avaloniaObject!;

        public XBase_Class_Visual_CommandGroup(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Canvas(), "CommandGroup", "cmdgroup", true, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // Do we need to add buttons?
            if (InInit)
            {
                // ----------------------------------------
                // Final setup of properties
                // ----------------------------------------
                await SetProperty("height", 40, 0);
                await SetProperty("width", 174, 0);
                await SetProperty("borderstyle", 1, 0);   // Set up the border
                await SetProperty("borderwidth", 1, 0);   // Set up the border
                await SetProperty("bordercolor", "100,100,100", 0);
                await SetProperty("buttonlayout", 1, 0);  // Horizontal layout
                await SetProperty("buttoncount", 2, 0);   // Start with 2 buttons
                await FixSpacing();
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Add an object to the end of the objects array
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            if (value is not null && value.avaloniaObject is not null)
            {
                if (JAXLib.InListC(value.BaseClass, "commandbutton"))
                {
                    await base.MakeNextDefaultName(value);
                    value.SetParent(me);
                    cmdGroup.Children.Add((Avalonia.Controls.Button)value.avaloniaObject!);
                }
                else
                    err = 1903;

                if (err == 0)
                {
                    UserProperties["objects"].Add(value!);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                }
            }


            if (err > 0)
            {
                _AddError(err, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(err, $"{err}|{value?.BaseClass}", string.Empty);
            }

            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
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
         *      >0  - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;
            propertyName = propertyName.ToLower();
            int temp;

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (InInit == false)
                {
                    int iii = 0;
                }

                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName)
                    {
                        case "buttonlayout":
                            if (objtk.Element.Type.Equals("N") == false)
                                result = 11;
                            else if (JAXLib.Between(objtk.AsInt(), 0, 2))
                            {
                                temp = objtk.AsInt();
                                UserProperties[propertyName].Element.Value = temp;
                                await FixSpacing();
                                result = 9;
                            }
                            else
                                result = 41;

                            break;

                        // Intercept special handling of properties
                        case "buttoncount":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                await SetButtonCount(objtk.AsInt());
                                result = 9;
                                await FixSpacing();
                            }
                            else
                                result = 11;
                            break;

                        case "buttoncaptions":
                            // Allow quick adding of captions to buttons
                            if (objtk.Element.Type.Equals("C"))
                            {
                                string caps = objtk.AsString().Replace(',', ';');
                                UserProperties["buttoncaptions"].Element.Value = objtk.AsString();
                                await SetButtonCount(-1);
                                await FixSpacing();
                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "height":
                            // Make sure we have a valid value
                            if (objtk.Element.Type.Equals("N"))
                            {
                                temp = objtk.AsInt();

                                objValue = temp < 30 ? 30 : temp;
                                objtk.Element.Value = objValue;

                                result = await base.SetProperty(propertyName, objValue, objIdx);

                                if (result == 0)
                                {
                                    result = 9;
                                    await FixSpacing();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "spacing":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                temp = objtk.AsInt();

                                // Check value to make sure it's in the acceptable range and save it
                                temp = JAXLib.Between(temp, 0, 255) ? temp : throw new Exception("11|");
                                UserProperties[propertyName].Element.Value = temp;
                                result = 9;

                                // Fix button spacing
                                await FixSpacing();
                            }
                            else
                                result = 11;

                            break;

                        case "width":
                            // Make sure we have a valid value
                            if (objtk.Element.Type.Equals("N"))
                            {
                                temp = objtk.AsInt();
                                objValue = temp < 40 ? 40 : temp;
                                objtk.Element.Value = objValue;

                                result = await base.SetProperty(propertyName, objValue, objIdx);

                                // Update all buttons appropriately
                                if (result == 0)
                                {
                                    result = 9;
                                    await FixSpacing();
                                }
                            }
                            else
                                result = 11;

                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        // 9 & 10 skips further processing
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
                else
                    result = 1559;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);


                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            // Refresh everything
            cmdGroup.UpdateLayout();
            return result;
        }


        /*
         * Set the button count
         */
        private async Task SetButtonCount(int desiredButtonCount)
        {
            if (desiredButtonCount < 2)
            {
                // Somebody updated a property
                // before ButtonCount was updated
                return;
            }

            // Grab properties for setup
            string[] caps = UserProperties["buttoncaptions"].AsString().Replace(',', ';').Split(';');
            string[] names = UserProperties["buttonnames"].AsString().Replace(',', ';').Split(';');
            int spacing = UserProperties["spacing"].AsInt();

            // Get the current button count
            JAXObjects.Token bc = UserProperties["objects"];
            int currentButtonCount = bc._avalue.Count;

            // Do we need to knock some buttons out of Objects?
            int ii = bc.Count - 1;
            while (ii >= 0 && currentButtonCount > desiredButtonCount)
            {
                if (bc._avalue[ii].Value is JAXObjectWrapper)
                {
                    JAXObjectWrapper btn = (JAXObjectWrapper)bc._avalue[ii].Value;
                    if (btn.BaseClass.Equals("commandbutton", StringComparison.OrdinalIgnoreCase))
                    {
                        bc.RemoveAt(ii);
                        cmdGroup.Children.RemoveAt(cmdGroup.Children.Count - 1);
                        ii--;
                        currentButtonCount--;
                    }
                }
            }

            // Next, do we need to add buttons?
            while (currentButtonCount < desiredButtonCount)
            {
                JAXObjectWrapper obut = new(App, "commandbutton", $"command{bc.Count + 1}", []);
                await obut.SetProperty("autosize", true);
                await obut.SetProperty("caption", $"Command{bc.Count + 1}");
                await obut.SetProperty("visible", true);
                obut.SetParent(me);
                bc.Add(obut);
                cmdGroup.Children.Add(obut.avaloniaObject!);
                currentButtonCount = bc._avalue.Count;
            }

            UserProperties["controlcount"].Element.Value = bc.Col;
            UserProperties["buttoncount"].Element.Value = currentButtonCount;

            // Next we set the button names
            // An empty name skips renaming the button
            if (string.IsNullOrWhiteSpace(names[0]) == false || names.Length > 1)
            {
                int j = 0;

                for (int i = 0; i < bc.Col; i++)
                {
                    JAXObjectWrapper? btn = bc._avalue[i].Value as JAXObjectWrapper;
                    if (btn is not null)
                    {
                        string name = names[j++];
                        if (string.IsNullOrWhiteSpace(name) == false)
                        {
                            if (JAXUtilities.IsValidName(name))
                                await btn.SetProperty("name", name);
                            else
                                throw new Exception($"9105|{name}");
                        }
                    }

                    if (j >= caps.Length)
                        break;
                }
            }


            // Finally, are there captions to dispense?
            // Captions that are blank or have a value enable the button
            // A caption of "*" disables the button
            // If we run out of captions, nothing else happens
            if (string.IsNullOrWhiteSpace(caps[0]) == false || caps.Length > 1)
            {
                int j = 0;
                for (int i = 0; i < bc.Col; i++)
                {
                    JAXObjectWrapper? btn = bc._avalue[i].Value as JAXObjectWrapper;
                    if (btn is not null)
                    {
                        string cap = caps[j++];
                        if (string.IsNullOrWhiteSpace(cap))
                        {
                            // Enable the button if blank
                            await btn.SetProperty("enabled", true);
                        }
                        else
                        {
                            if (cap.Equals("*"))
                            {
                                // Disable the button
                                await btn.SetProperty("enabled", false);
                            }
                            else
                            {
                                // Enable and set the button's caption
                                await btn.SetProperty("enabled", true);
                                await btn.SetProperty("caption", cap);
                            }
                        }
                    }

                    if (j >= caps.Length)
                        break;
                }
            }
        }


        /*
         * Fix the spacing
         * ButtonLayout
         *      0 - Vertical stack - resize buttons to fit panel area
         *      1 - Horizontal stack - resize buttons to fit panel area
         *      2 -  - Program must set up everything
         */
        private async Task<int> FixSpacing()
        {
            int result = 0;
            string msg = "";

            // Don't do anything during init
            if (InInit)
                return 0;

            try
            {
                JAXObjects.Token otk = UserProperties["objects"];

                if (otk.Col < 2)
                    return 0;

                int spacing = UserProperties["spacing"].AsInt();
                int temp = spacing;

                // Current dimensions of panel
                int clft = UserProperties["left"].AsInt();
                int ctop = UserProperties["top"].AsInt();
                int cwid = UserProperties["width"].AsInt();
                int chgt = UserProperties["height"].AsInt();
                int blayout = UserProperties["buttonlayout"].AsInt();

                // Used for BL=2
                int top = 0;
                int lft = 0;

                // used for all 3 modes
                int hgt = 0;
                int wth = 0;
                int btncount = UserProperties["buttoncount"].AsInt();

                // If there are buttons...
                if (btncount > 0)
                {
                    // Skip this section if freeform
                    if (blayout == 2)
                    {
                        // set them way out there
                        hgt = 2147483647;
                        wth = 2147483647;
                    }
                    else
                    {
                        // Calculate the width and height of buttons
                        if (blayout == 0)
                        {
                            // Vertical
                            hgt = UserProperties["height"].AsInt();
                            int rsp = spacing * btncount * 2;
                            wth = (wth - rsp);
                            hgt = hgt / btncount;
                            hgt = (hgt < 23) ? 23 : hgt;
                            temp = (UserProperties["height"].AsInt() - rsp - wth * btncount) / 2 + temp;
                            wth = UserProperties["width"].AsInt() - spacing * 2;
                            wth = (wth < 25) ? 25 : wth;
                        }
                        else
                        {
                            // Horizontal
                            wth = UserProperties["width"].AsInt();
                            int rsp = spacing * btncount * 2;
                            wth = (wth - rsp);
                            wth = wth / btncount;
                            wth = (wth < 23) ? 23 : wth;
                            temp = (UserProperties["width"].AsInt() - rsp - wth * btncount) / 2 + temp;
                            hgt = UserProperties["height"].AsInt() - spacing * 2;
                            hgt = (hgt < 25) ? 25 : hgt;
                        }
                    }

                    // Temp now becomes the current top or left position for the next button
                    for (int i = 0; i < otk._avalue.Count; i++)
                    {
                        // Spacing between buttons is spacing * 2
                        if (i > 0) temp += spacing;

                        // Protection, but should always be true
                        if (otk._avalue[i].Value is JAXObjectWrapper)
                        {
                            // If it's a command button and autosize = .T.
                            JAXObjectWrapper itk = (JAXObjectWrapper)otk._avalue[i].Value;
                            if (blayout == 2)
                            {
                                // Freeform layout
                                // Get the current button location and dimensions
                                int t = itk.thisObject!.UserProperties["top"].AsInt();
                                int l = itk.thisObject!.UserProperties["left"].AsInt();
                                int h = itk.thisObject!.UserProperties["height"].AsInt();
                                int w = itk.thisObject!.UserProperties["width"].AsInt();

                                lft = (clft + lft) > clft + l ? l : clft;
                                top = (ctop + top) > ctop + t ? t : top;
                                wth = (clft + lft + wth) > (l + w) ? l + w : wth;
                                hgt = (ctop + chgt + hgt) > (t + h) ? t + h : hgt;
                            }
                            else
                            {
                                await itk.SetProperty("autosize", false);
                                if (blayout == 0)
                                {
                                    // Vertical layout
                                    await itk.SetProperty("top", temp);
                                    await itk.SetProperty("left", spacing);
                                    await itk.SetProperty("width", wth);
                                    await itk.SetProperty("height", hgt);

                                    //  advance temp for the next one
                                    temp += hgt + spacing;
                                }
                                else
                                {
                                    // Horizontal layout
                                    await itk.SetProperty("left", temp);
                                    await itk.SetProperty("top", spacing);
                                    await itk.SetProperty("width", wth);
                                    await itk.SetProperty("height", hgt);

                                    //  advance temp for the next one
                                    temp += wth + spacing;
                                }
                            }
                        }
                    }

                    // Now adjust the panel height/width and
                    // make sure the butten area is moved
                    // into the panel client area.
                    if (UserProperties["buttonlayout"].AsInt() == 2)
                    {
                        UserProperties["height"].Element.Value = hgt + spacing * 2;
                        UserProperties["width"].Element.Value = wth + spacing * 2;

                        // Set up the relative movement of all
                        // buttons to fit into the client area
                        int tfix = spacing - top;
                        int lfix = spacing - lft;

                        // Move all button tops and lefts so everything
                        // ends up in the client area.
                        for (int i = 0; i < otk._avalue.Count; i++)
                        {
                            // Protection, but should always be true
                            if (otk._avalue[i].Value is JAXObjectWrapper)
                            {
                                // If it's a command button and autosize = .T.
                                JAXObjectWrapper itk = (JAXObjectWrapper)otk._avalue[i].Value;
                                if (itk.BaseClass.Equals("commandbutton", StringComparison.OrdinalIgnoreCase))
                                {
                                    JAXObjects.Token t = await itk.GetProperty("top", 0);
                                    if (t.Element.Type.Equals("N"))
                                    {
                                        JAXObjects.Token l = await itk.GetProperty("left", 0);
                                        if (l.Element.Type.Equals("N"))
                                        {
                                            await itk.SetProperty("top", t.AsInt() + tfix);
                                            await itk.SetProperty("left", l.AsInt() + lfix);
                                        }
                                        else
                                        {
                                            // Remark on the problem
                                            result = 1559;
                                            App.SetError(result, $"1559|LEFT", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                        }
                                    }
                                    else
                                    {
                                        // Remark on the problem
                                        result = 1559;
                                        App.SetError(result, $"1559|TOP", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg= ex.Message;
            }

            if (result > 0)
                _AddError(result, 0, msg, App.AppLevels[^1].Procedure);

            return result;
        }

        /*
         * Resize actions depend on the buttonlayout property and autosize of buttons and container
         *  0 - Align all buttons vertically, adjusting their size and position within the border of the command group
         *  1 - Align all buttons horizontally, adjusting their size and position within the border of the command group.
         *  2 - Adjust the border of the command group to fit around the buttons if container is autosize.
         */
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 6501;

            if (Methods.ContainsKey(methodName))
            {
                result = 0;
                methodName = methodName.ToLower();
                JAXObjects.Token tk = new();

                switch (methodName)
                {
                    case "resize":
                        await FixSpacing();
                        break;

                    default:
                        result = await base.DoDefault(methodName);
                        break;
                }
            }

            return result;
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
                    case "buttoncount":
                        returnToken.Element.Value = UserProperties["objects"].Count;
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]);
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
               "addproperty", "addobject", "move", "readexpression", "readmethod", "refresh", "removeobject", "resettodefault",
               "saveasclass", "setall", "setfocus", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error","init","interactivechagnge","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","resize","rightclick","valid","visiblechanged","when"
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
                "anchor,n,0","alignment,n,2","autosize,l,true",
                "backcolor,R,15790320","backstyle,n,1","BaseClass,C!,commandgroup","bordercolor,R,100|100|100",
                "borderstyle,n,1","borderwidth,n,1","buttoncount,n,1","buttonlayout,n,0","buttonpictures,c,","buttontooltips,c,",
                "buttoncaptions,c,","buttonnames,c,",
                "Class,C!,commandgroup","ClassLibrary,C!,","Comment,C,","controlcount,n,0",
                "Enabled,L,true",
                "Height,N,40",
                "left,N,0",
                "name,c,",
                "objects,*,",
                "parent,o!,","parentclass,C!,",
                "righttoleft,L,false",
                "setoriginalwhen,n,0","spacing,n,6",
                "tabstop,L,true","tabindex,n,1","tag,C,","tooltiptext,c,","top,n,0",
                "value,n,1","visible,l,true","width,N,150"
                ];
        }
    }
}
