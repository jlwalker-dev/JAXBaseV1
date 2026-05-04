/*------------------------------------------------------------------------------------------*
 * OptionGroup class
 * 
 * 2026-03-08 - JLW
 *      Switching to Avalonia
 *      
 *------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_OptionGroup : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "OptionGroup";
        public new string MyDefaultName { get; } = "optiongroup";


        public Avalonia.Controls.Canvas oGrp => (Avalonia.Controls.Canvas)me.avaloniaObject!;

        public XBase_Class_Visual_OptionGroup(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Canvas(), "OptionGroup", "optgroup", true, UserObject.URW);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            if (InInit)
            {
                // ----------------------------------------
                // Final setup of properties
                // ----------------------------------------
                //SetProperty("height", 40, 0);
                //SetProperty("width", 174, 0);
                //SetProperty("borderstyle", 1, 0);   // Set up the border
                //SetProperty("borderwidth", 1, 0);   // Set up the border
                //SetProperty("bordercolor", "100,100,100", 0);
                //SetProperty("buttonlayout", 0, 0);  // Vertical layout
                //SetProperty("buttoncount", 2, 0);   // Start with 2 buttons
                //FixSpacing();
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
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
            int spacing;
            int temp;

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "bordercolor":
                            UserProperties[propertyName].Element.Value = JAXUtilities.ReturnColorInt(objValue);
                            result = 9;
                            break;

                        case "borderwidth":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 64))
                                {
                                    // Set the border width - TODO
                                    UserProperties[propertyName].Element.Value = objtk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "borderstyle":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 6))
                                {
                                    UserProperties[propertyName].Element.Value = objtk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "buttonlayout":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 2))
                                    UserProperties[propertyName].Element.Value = objValue;
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            await FixSpacing();
                            break;

                        // Intercept special handling of properties
                        case "buttoncount":
                            if (InInit == false)
                            {
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    JAXObjects.Token bc = UserProperties["objects"];
                                    if (objtk.AsInt() < 1)
                                        result = 41;
                                    else
                                    {
                                        int desiredButtonCount = objtk.AsInt();

                                        // Do we need to knock some buttons out of Objects?
                                        int ii = bc.Count - 1;
                                        while (ii >= 0 && oGrp.Children.Count > desiredButtonCount)
                                        {
                                            bc.RemoveAt(ii);
                                            oGrp.Children.RemoveAt(oGrp.Children.Count - 1);
                                            ii--;
                                        }

                                        spacing = UserProperties["spacing"].AsInt();

                                        // Finallyu, do we need to add some to the end?
                                        while (oGrp.Children.Count < desiredButtonCount)
                                        {
                                            JAXObjectWrapper obut = new(App, "optionbutton", $"option{bc.Count + 1}", []);
                                            obut.SetParent(me);
                                            bc.Add(obut);
                                            oGrp.Children.Add(obut.avaloniaObject!);

                                            await obut.SetProperty("autosize", true);
                                            await obut.SetProperty("caption", $"Option{bc.Count}");
                                            await obut.SetProperty("value", bc.Count);
                                            await obut.SetProperty("visible", true);
                                        }

                                        UserProperties["controlcount"].Element.Value = bc.Col;
                                        UserProperties["buttoncount"].Element.Value = oGrp.Children.Count;
                                        result = 9;
                                        await FixSpacing();
                                    }
                                }
                                else
                                    result = 11;
                            }

                            break;

                        case "columns":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (objtk.AsInt() < 1)
                                    result = 41;
                                else
                                    UserProperties[propertyName].Element.Value = objValue;

                                await FixSpacing();
                            }
                            else
                                result = 11;
                            break;

                        case "height":
                        case "width":
                            // Make sure we have a valid value
                            if (objtk.Element.Type.Equals("N"))
                            {
                                temp = objtk.AsInt();
                                objValue = temp < 30 ? 30 : temp;
                                objtk.Element.Value = objValue;

                                result = await base.SetProperty(propertyName, objValue, objIdx);

                                // Update all buttons appropriately
                                await FixSpacing();
                            }
                            else
                                result = 41;

                            break;

                        case "spacing":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                temp = objtk.AsInt();

                                // Check value to make sure it's in the acceptable range and save it
                                temp = JAXLib.Between(temp, 0, 255) ? temp : throw new Exception("11|");
                                UserProperties[propertyName].Element.Value = temp;

                                // Fix button spacing
                                await FixSpacing();
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
                        // Did we process it?
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName} - {objtk.AsString()}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
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
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }

        /*
         * Fix the spacing
         * ButtonLayout
         *      0 - Vertical stack - resize buttons to fit panel area
         *      1 - Horizontal stack - resize buttons to fit panel area
         *      2 - FreeForm - resize panel to fit around the buttons
         *                     leaving them exactly where they are.
         *      
         */
        private async Task<int> FixSpacing()
        {
            int result = 0;
            if (InInit) return result;

            try
            {
                JAXObjects.Token otk = UserProperties["objects"];
                int spacing = UserProperties["spacing"].AsInt();
                int temp = spacing;

                // Current dimensions of panel
                int clft = UserProperties["left"].AsInt();
                int ctop = UserProperties["top"].AsInt();
                int cwid = UserProperties["width"].AsInt();
                int chgt = UserProperties["height"].AsInt();

                // Used for BL=2
                int top = 0;
                int lft = 0;

                // used for all 3 modes
                int hgt = 0;
                int wth = 0;
                int btncount = UserProperties["buttoncount"].AsInt();
                int btnlayout = UserProperties["buttonlayout"].AsInt();

                // If there are option buttons...
                if (btncount > 1)
                {
                    // Skip this section if freeform
                    if (btnlayout == 2)
                    {
                        // set them way out there
                        hgt = 2147483647;
                        wth = 2147483647;
                    }
                    else
                    {
                        if (btnlayout == 0)
                        {
                            // Vertical
                            hgt = UserProperties["height"].AsInt();
                            hgt = (hgt - 6 * btncount - spacing * 2) / btncount;
                            hgt = (hgt < 23) ? 23 : hgt;
                            wth = UserProperties["width"].AsInt() - spacing * 2;
                            wth = (wth < 25) ? 25 : wth;
                        }
                        else
                        {
                            // Horizontal
                            wth = UserProperties["width"].AsInt();
                            wth = (wth - 6 * btncount - spacing * 2) / btncount;
                            wth = (wth < 23) ? 23 : wth;
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
                            // If it's a option button and autosize = .T.
                            JAXObjectWrapper obtn = (JAXObjectWrapper)otk._avalue[i].Value;
                            if (btnlayout == 2)
                            {
                                // Freeform layout
                                // Get the current button location and dimensions
                                int t = obtn.thisObject!.UserProperties["top"].AsInt();
                                int l = obtn.thisObject!.UserProperties["left"].AsInt();
                                int h = obtn.thisObject!.UserProperties["height"].AsInt();
                                int w = obtn.thisObject!.UserProperties["width"].AsInt();

                                lft = (clft + lft) > clft + l ? l : clft;
                                top = (ctop + top) > ctop + t ? t : top;
                                wth = (clft + lft + wth) > (l + w) ? l + w : wth;
                                hgt = (ctop + chgt + hgt) > (t + h) ? t + h : hgt;
                            }
                            else
                            {
                                await obtn.SetProperty("autosize", false);
                                if (UserProperties["buttonlayout"].AsInt() == 0)
                                {
                                    // Vertical layout
                                    await obtn.SetProperty("top", temp);
                                    await obtn.SetProperty("left", spacing);
                                    await obtn.SetProperty("wordwrap", true);
                                    await obtn.SetProperty("width", wth);

                                    // Deal with word wrap height changes
                                    JAXObjects.Token tkhgt = await obtn.GetProperty("height");
                                    hgt = tkhgt.AsInt();

                                    //  advance temp for the next one
                                    temp += hgt + spacing;
                                }
                                else
                                {
                                    // Horizontal layout
                                    await obtn.SetProperty("left", temp);
                                    await obtn.SetProperty("top", spacing);
                                    await obtn.SetProperty("wordwrap", false);
                                    await obtn.SetProperty("width", wth);
                                    await obtn.SetProperty("height", hgt);

                                    //  advance temp for the next one
                                    temp += wth + spacing;
                                }
                            }
                        }
                    }

                    if (btnlayout == 2)
                    {
                        // Adjust the panel height/width and make
                        // sure the client can hold the buttons.
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
                            if (otk._avalue[i].Value is JAXObjectWrapper itk)
                            {
                                JAXObjects.Token t = await itk.GetProperty("top", 0);

                                if (t.Element.Type.Equals("N"))
                                { 
                                    JAXObjects.Token l = await itk.GetProperty("left", 0);
                                    if (t.Element.Type.Equals("N"))
                                    {
                                        await itk.SetProperty("top", t.AsInt() + tfix);
                                        await itk.SetProperty("left", l.AsInt() + lfix);
                                    }
                                    else
                                    {
                                        // Remark on the problem
                                        result = 1559;
                                        AppErrorHandling.SetError(result, $"1559|LEFT", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                    }
                                }
                                else
                                {
                                    // Remark on the problem
                                    result = 1559;
                                    AppErrorHandling.SetError(result, $"1559|TOP", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                AppErrorHandling.SetError(result, $"9999|{ex.Message}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }

        /*
         * Resize actions depend on the buttonlayout property and autosize of buttons and container
         *  0 - Align all buttons vertically, adjusting their size and position within the border of the option group
         *  1 - Align all buttons horizontally, adjusting their size and position within the border of the option group.
         *  2 - Adjust the border of the option group to fit around the buttons if container is autosize.
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
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty", "drag", "move", "readexpression", "readmethod", "refresh", "resettodefault",
                "saveasclass", "settooriginalvalue", "setfocus", "showwhatsthis", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus",
                "init","interactivechagnge","keypress","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rangehigh","rangelow","rightclick","uienable","valid","visiblechanged","when"
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
                "anchor,n,0","autosize,l,false","backcolor,R,255|255|255","backstyle,n,1",
                "baseclass,C!,optiongroup","bordercolor,R,0|0|0","borderstyle,n,1","borderwidth,n,0",
                "buttoncount,n,0","buttonlayout,n,0","buttoncaptions,c,","buttonnames,c,",
                "Class,C!,OptionGroup","ClassLibrary,C!,","Comment,C,","controlcount,n!,0","controlsource,c,",
                "disabledbackcolor,R,128|128|128","disabledforecolor,R,64|64|64",
                "Enabled,L,true","forecolor,R,0|0|0",
                "Height,N,66",
                "left,N,0",
                "name,c,optiongroup",
                "objects,*,","originalvalue,,",
                "parent,o!,","parentclass,C!,",
                "spacing,n,5",
                "tabindex,n,1","tag,C,","top,N,0","tooltiptext,c,",
                "value,n,1","visible,l,true","width,N,175"
                ];
        }
    }
}
