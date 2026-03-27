/*
 * 2026.03.10 - JLW
 *      Pageframe is showing and reacting as expected, but testing needs to be done.
 *      
 */
using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_PageFrame : XBase_Class_Avalonia
    {
        public Avalonia.Controls.TabControl pgFrame => (Avalonia.Controls.TabControl)me.avaloniaObject!;

        public XBase_Class_Visual_PageFrame(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            Avalonia.Controls.TabControl tc = new Avalonia.Controls.TabControl
            {
                TabStripPlacement = Avalonia.Controls.Dock.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };

            SetVisualObject(tc, "PageFrame", "pgFrame", true, UserObject.URW);
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
            propertyName = propertyName.ToLower();
            JAXObjects.Token tkobj = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "tabcaptions":
                            if (tkobj.Element.Type.Equals("C"))
                            {
                                // Set up the captions
                                UserProperties[propertyName].Element.Value = objValue;
                                result = 9;
                                FixPages();
                            }
                            else
                                result = 11;

                            break;

                        case "pagecount":
                            if (InInit == false)
                            {
                                if (tkobj.Element.Type.Equals("N"))
                                {
                                    JAXObjects.Token bc = UserProperties["objects"];
                                    if (tkobj.AsInt() < 1)
                                        result = 41;
                                    else
                                    {
                                        int desiredButtonCount = tkobj.AsInt();

                                        // Do we need to knock some buttons out of Objects?
                                        int ii = bc.Count - 1;
                                        while (ii >= 0 && pgFrame.Items.Count > desiredButtonCount)
                                        {
                                            bc.RemoveAt(ii);
                                            pgFrame.Items.RemoveAt(pgFrame.Items.Count - 1);
                                            ii--;
                                        }

                                        // Finallyu, do we need to add some to the end?
                                        while (pgFrame.Items.Count < desiredButtonCount)
                                        {
                                            JAXObjectWrapper oPg = new(App, "page", $"page{bc.Count + 1}", []);
                                            oPg.SetParent(me);

                                            oPg.thisObject!.UserProperties["pagenumber"].Element.Value = bc.Count + 1;
                                            await oPg.SetProperty("caption", $"Page{bc.Count + 1}");
                                            await oPg.SetProperty("visible", true);

                                            pgFrame.Items.Add(oPg.avaloniaObject!);
                                            bc.Add(oPg);
                                        }

                                        UserProperties["controlcount"].Element.Value = bc.Col;
                                        UserProperties["pagecount"].Element.Value = pgFrame.Items.Count;
                                        result = 9;
                                        FixPages();

                                        if (UserProperties["activepage"].AsInt() < 1)
                                            UserProperties["activepage"].Element.Value = 1;

                                        pgFrame.SelectedIndex = UserProperties["activepage"].AsInt() - 1;
                                    }
                                }
                                else
                                    result = 11;
                            }

                            break;


                        case "taborientation":
                            if (tkobj.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tkobj.AsInt(), 0, 2))
                                {
                                    // Set up the layout
                                    UserProperties[propertyName].Element.Value = tkobj.AsInt();
                                    result = 9;
                                    FixPages();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;

                        case "tabnames":
                            if (tkobj.Element.Type.Equals("C"))
                            {
                                // Set up the names - this property is destructive
                                UserProperties[propertyName].Element.Value = objValue;
                                result = 9;
                                FixPages();
                            }
                            else
                                result = 11;

                            break;

                        case "tabstretch":
                            if (tkobj.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tkobj.AsInt(), 0, 2))
                                {
                                    // Set up the layout
                                    UserProperties[propertyName].Element.Value = tkobj.AsInt();
                                    result = 9;
                                    FixPages();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;

                        case "tabstyle":
                            if (tkobj.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tkobj.AsInt(), 0, 2))
                                {
                                    // Set up the layout
                                    UserProperties[propertyName].Element.Value = tkobj.AsInt();
                                    result = 9;
                                    FixPages();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;

                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 9 ? 0 : result;
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
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
                // Visual object common property handler
                switch (propertyName.ToLower())
                {
                    case "activepage":
                    case "pagecount":
                    case "taborientation":
                    case "tabstretch":
                    case "tabstyle":
                    case "tabs":
                        returnToken.Element.Value = UserProperties[propertyName].Element.Value;
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
                "addproperty", "addobject", "move", "readexpression", "readmethod", "refresh", "resettodefault",
                "saveasclass", "settooriginalvalue", "setfocus", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "rightclick","visiblechanged","when"
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
            return [
                "activepage,N!,0","anchor,n,0",
                "backcolor,R,255|255|255","bordercolor,R,100|100|100","borderwidth,N,1",
                "baseclass,C!,pageframe",
                "class,C!,pageframe","classlibrary,C!,","comment,C,","controlcount,N!,0",
                "Enabled,L,true",
                "forecolor,R,0",
                "Height,N,200",
                "keypreview,L,false",
                "left,N,0",
                "name,C,pageframe",
                "objects,*,",
                "pagecount,n,1","parent,o!,","parentclass,C!,","picture,C,",
                "righttoleft,L,",
                "tag,C,","tabcaptions,c,","tabindex,N,1","tabnames,c,","taborientation,N,0","tabstop,L,true",
                "tabstretch,N,1","tabstyle,n,0","tabs,L,.T.","tooltiptext,c,","top,N,0",
                "visible,L,true",
                "width,N,200"
                ];
        }


        // Fix up the pages and canvas
        private void FixPages()
        {

        }

    }
}
