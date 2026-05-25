/*------------------------------------------------------------------------------------------*
 * MenuItem Class
 * 
 * 2025.11.19 - JLW
 *      Tool strips for JAXBase.  Add a nice looking tool strip to your form
 *      using any kind of graphic, though ico an png are probably best.
 *      
 * 2025.12.11 - JLW
 *      Learning about icon/pic use in C# so I can finish this up and have
 *      the grid left before getting serious with the form designer bootstrap
 *      project which will be the kick-off of Version 0.6 developement.
 *      
 *      The AppClass has the ImageLibrary class which handles registration and
 *      access of all images.  Images are stored using the lowercase "stem.ext"
 *      of it's file name.
 * 
 * 2026.02.24 - JLW
 *      Moving to Avalonia
 *      
 * 2026.03.15 - JLW
 *      Set up for asyc compatibity
 *      A separater is going to be a borderless, clear toolbar button.
 *      
 *------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ToolBar : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "ToolBar";
        public new string MyDefaultName { get; } = "toolbar";


        public Avalonia.Controls.Canvas Toolbar => (Avalonia.Controls.Canvas)me.avaloniaObject!;
        
        public XBase_Class_Visual_ToolBar(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Canvas(), "Toolbar", string.IsNullOrWhiteSpace(name) ? MyDefaultName : name, true, UserObject.URW);
            SetPrivateProperty("configured", false);
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
            if (value is not null && value.avaloniaObject is not null)
            {
                if (JAXLib.InListC(value.BaseClass, "toolbutton"))
                {
                    await base.MakeNextDefaultName(value);
                    value.SetParent(me);
                    Toolbar.Children.Add((Avalonia.Controls.Button)value.avaloniaObject!);
                }
                else
                    err = 1903;

                if (err == 0)
                {
                    UserProperties["objects"].Add(value!);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                    //await FixButtons(-2); // Request an toolbar refresh
                }
            }


            if (err > 0)
            {
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|{value?.BaseClass}", string.Empty);
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
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                // Get the property and fill in the value
                //returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    // Intercept special handling of properties
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

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
         *     -1   - Error encountered
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
                        case "buttoncount":
                            if (tk.Element.Type.Equals("N"))
                            {
                                UserProperties[propertyName].Element.Value = tk.AsInt();

                                if (InInit == false)
                                {
                                    SetPrivateProperty("configured", true);
                                    await SetButtonCount();
                                    await FixSpacing();
                                    //result = await FixButtons(tk.AsInt());
                                    result = result == 0 ? 9 : result;
                                }

                            }
                            else
                                result = 11;
                            break;

                        case "buttonpictures":
                        case "tooltiptext":
                            UserProperties[propertyName].Element.Value = tk.AsString();

                            if (InInit == false && PrivateProperties["configured"].AsBool())
                            {
                                // Go through each associated button and call the FixImage routine
                                // Now fix the buttons up
                                JAXObjects.Token objects = new();
                                objects = UserProperties["objects"];

                                bool vertical = UserProperties["vertical"].AsBool();
                                int width = UserProperties["width"].AsInt();
                                int height = UserProperties["height"].AsInt();
                                int spacing = UserProperties["spacing"].AsInt();

                                int btnSize = vertical ? width : height - spacing * 2;
                                int temp = spacing;

                                for (int i = 0; i < objects.Count; i++)
                                {
                                    if (i > 0) temp += spacing;

                                    JAXObjectWrapper? btn = objects._avalue[i].Value as JAXObjectWrapper;

                                    // It is, so fix tool tip and image
                                    FixToolTip(btn!, i);
                                    await FixImage(btn!, i);
                                }
                            }

                            break;


                        case "buttonnames":
                            // Set button names
                            UserProperties[propertyName].Element.Value = tk.AsString();

                            if (InInit == false)
                            {
                                SetPrivateProperty("configured", true);
                                //result = await FixButtons();
                                await SetButtonCount();
                                await FixSpacing();
                            }
                            break;

                        // Intercept special handling of properties
                        case "buttonlayout":
                            if (tk.Element.Type.Equals("N"))
                            {
                                int buttonLayout = tk.AsInt();

                                if (JAXLib.Between(buttonLayout, 0, 2))
                                {
                                    //if (buttonLayout == 0)
                                    //Toolbar.Orientation = Avalonia.Layout.Orientation.Vertical;
                                    //else
                                    //Toolbar.Orientation = Avalonia.Layout.Orientation.Horizontal;
                                    //result = await FixButtons(-1);
                                    await SetButtonCount();
                                    await FixSpacing();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "height":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() > 19)
                                {
                                    UserProperties[propertyName].Element.Value = tk.AsInt();
                                    Toolbar.Height = tk.AsInt();
                                    result = 9;
                                    await SetButtonCount();
                                    await FixSpacing();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "width":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() > 19)
                                {
                                    UserProperties[propertyName].Element.Value = tk.AsInt();
                                    Toolbar.Width = tk.AsInt();
                                    result = 9;
                                    await SetButtonCount();
                                    await FixSpacing();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // We don't save what we don't process. If result>10, then
                    // there was an error and we don't save the value.
                    if (JAXLib.Between(result, 0, 10))
                    {
                        // Result of 9 or 10 means it's been handled
                        // and we don't need to try to save it again
                        if (result < 9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;


            // If result>0 at this point, then there's been an error
            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
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
                "addproperty","addobject","move","readexpression","readmethod","refresh","removeobject","resettodefault",
                "saveasclass","setall","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","doubleclick","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","valid","visiblechanged","when"
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
                "anchor,n,0",
                "baseclass,C!,toolbar","backcolor,r,0","backstyle,n,1","bordercolor,R,0","borderwidth,N,0",
                "buttoncount,n,0","buttonlayout,n,0","buttonnames,c,","buttonpictures,c,","buttontooltips,c,",
                "class,C!,toolbar","classlibrary,C!,","comment,c,","controlcount,n!,0",
                "enabled,l,.t.",
                "forecolor,r,0",
                "height,n,40",
                "left,n,0",
                "name,c,",
                "objects,*,",
                "parent,o!,","parentclass,c!,",
                "spacing,n,1",
                "tabindex,n,1","tabstop,L,.T.","tag,c,","top,n,0","tooltiptext,c,",
                "value,n,0","visible,l,.t.",
                "width,n,200"
                ];
        }




        /*
         * Set the button count
         */
        private async Task SetButtonCount()
        {
            int desiredButtonCount = UserProperties["buttoncount"].AsInt();
            if (desiredButtonCount < 2)
            {
                // Somebody updated a property
                // before ButtonCount was updated
                return;
            }

            // Grab properties for setup
            //string[] caps = UserProperties["buttoncaptions"].AsString().Replace(',', ';').Split(';');
            string[] names = UserProperties["buttonnames"].AsString().Replace(',', ';').Split(';');
            int spacing = UserProperties["spacing"].AsInt();

            // Get the current button count
            JAXObjects.Token bc = UserProperties["objects"];
            int currentButtonCount = UserProperties["controlcount"].AsInt();

            // Do we need to knock some buttons out of Objects?
            int ii = bc.Count;
            while (ii >= 0 && currentButtonCount > desiredButtonCount)
            {
                if (bc._avalue[ii].Value is JAXObjectWrapper)
                {
                    JAXObjectWrapper btn = (JAXObjectWrapper)bc._avalue[ii].Value;
                    if (btn.BaseClass.Equals("toolbutton", StringComparison.OrdinalIgnoreCase))
                    {
                        bc.RemoveAt(ii);
                        Toolbar.Children.RemoveAt(Toolbar.Children.Count - 1);
                        ii--;
                        currentButtonCount--;
                    }
                }
            }

            // Next, do we need to add buttons?
            while (currentButtonCount < desiredButtonCount)
            {
                JAXObjectWrapper obut = new(Program.CurrentApp, "toolbutton", "", []);
                //await obut.SetProperty("autosize", true);
                await obut.SetProperty("name", $"Button{currentButtonCount + 1}");
                await obut.SetProperty("visible", true);
                obut.thisObject!.UserProperties["value"].Protected = false;
                obut.thisObject.UserProperties["value"].Element.Value = currentButtonCount + 1;
                obut.thisObject.UserProperties["value"].Protected = true;
                obut.thisObject.UserProperties["tabindex"].Element.Value = currentButtonCount + 1;
                obut.SetParent(me);

                // Check to see if _avalue[0] needs to be inserted
                bc.Add(obut);   // Otherwise just add

                Toolbar.Children.Add(obut.avaloniaObject!);
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

                    if (j >= names.Length)
                        break;
                }
            }
        }


        /*
         * Fix the spacing
         * ButtonLayout
         *      0 - Vertical stack - resize buttons to fit panel area
         *      1 - Horizontal stack - resize buttons to fit panel area
         *      2 - Manual - Program must set up everything
         */
        private async Task<int> FixSpacing()
        {
            int result = 0;

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
                    if (blayout != 2)
                    {
                        // Calculate the size of buttons
                        if (blayout == 0)
                        {
                            // Vertical
                            wth = UserProperties["width"].AsInt();
                            wth = (wth < 20) ? 20 : wth;
                            hgt = wth;
                        }
                        else
                        {
                            // Horizontal
                            hgt = UserProperties["height"].AsInt() - spacing * 2;
                            hgt = (hgt < 20) ? 20 : hgt;
                            wth = hgt;
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
                                if (blayout != 2)
                                {
                                    //await itk.SetProperty("autosize", false);
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
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                AppErrorHandling.SetError(result, $"9999|FIXSPACING|{ex.Message}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return result;
        }



        /*    
         * Set up image for a specific button
         */
        public async Task FixImage(JAXObjectWrapper btn, int count)
        {
            string[] imgs = UserProperties["buttonpictures"].AsString().Replace(',', ';').Split(';');
            if (count < imgs.Length)
            {
                // set the image
                if (string.IsNullOrWhiteSpace(imgs[count]) == false)
                {
                    if (imgs[count].Trim().Equals("*"))
                    {
                        await btn.SetProperty("picture", "");
                        await btn.SetProperty("enabled", false);
                    }
                    else
                    {
                        string imgName = imgs[count].Trim().Equals("*") ? "" : imgs[count].Trim();

                        if (imgName.Contains('\\') || imgName.Contains('/'))
                            Program.CurrentApp.JaxImages!.RegisterImage(imgName, "", out imgName);

                        await btn.SetProperty("picture", imgName);
                        await btn.SetProperty("enabled", true);
                    }
                }
                else
                {
                    JAXObjects.Token tk = btn.GetPrivateProperty("picname");
                    await btn.SetProperty("picture", tk.AsString());
                }
            }
        }

        /*
         * Set up tooltip for a specific button
         */
        public async void FixToolTip(JAXObjectWrapper btn, int count)
        {
            string[] tips = UserProperties["buttontooltips"].AsString().Replace(',', ';').Split(';');
            if (count < tips.Length)
            {
                // Set the tool tip
                await btn.SetProperty("tooltiptext", tips[count]);
            }
        }
    }
}
