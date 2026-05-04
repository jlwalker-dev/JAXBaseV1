/*------------------------------------------------------------------------------------------*
 * Menu class
 * 
 * 2025.11.17 - JLW  
 *      Created and started working on it.  Not a difficult class since most of the work
 *      is just adding MenuItems and Separators.  This is a visual class but doesn't
 *      support a lot of events or methods as it's more of a pallette than a full visual
 *      control.
 *      
 * 2025.02.24 - JLW
 *      Converting basic functinality to Avalonia.  It will only dock to the top
 *      of the form, but that's done in the window control and will be addressed
 *      at a later date.
 *      
 *      The menu is a text only object.
 *------------------------------------------------------------------------------------------*/
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Menu : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Menu";
        public new string MyDefaultName { get; } = "menu";


        public Avalonia.Controls.Menu MenuObj => (Avalonia.Controls.Menu)me.avaloniaObject!;

        public XBase_Class_Visual_Menu(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.Menu(), "Menu", "menu", true, UserObject.URW);
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
                            MenuObj.Items.Add((Avalonia.Controls.Separator)value.avaloniaObject);
                        else
                        {
                            Avalonia.Controls.MenuItem obj = (Avalonia.Controls.MenuItem)value.avaloniaObject;
                            MenuObj.Items.Add(obj);
                        }
                    }
                    else
                        err = 1903;
                }

                if (err == 0)
                {
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    await PostInit(me, []);
                }
            }
            else
                err = 3019;

            if (err > 0)
            {
                _AddError(err, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", string.Empty);

            }
            return err > 0 ? -1 : UserProperties["objects"]._avalue.Count;
        }


        /*------------------------------------------------------------------------------------------*
            * GetProperty method returns 
            *      0 = Successfully returning value
            *      1 = Not processed, returning .F.
            *      
            *    >10 = Error code
        *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    default:
                        // Process standard properties
                        returnToken =await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0; 
                        break;
                }

                if (JAXLib.Between(result,0,9))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]);

                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }

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
         *     -1   - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *     -1   - Error Code
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
                    result = await base.SetProperty(propertyName, objValue, objIdx);
                    result = result == 0 ? 9 : result;

                    // We don't save what we don't process
                    if (JAXLib.Between(result,0,10))
                    {
                        if(result<9)
                            UserProperties[propertyName].Element.Value = objValue;

                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
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
                "addobject","addproperty","move","readexpression","readmethod","refresh","resettodefault",
                "saveasclass","settooriginalvalue","setfocus","writeexpression","writemethod","zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "destroy","error","init","load",
                "mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "visiblechanged","when"
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
                "anchor,n,0","autosize,l,.t.",
                "baseclass,C!,menu","backcolor,r,255|255|255","backstyle,n,0",
                "class,C!,menu","classlibrary,C!,","comment,c,","controlcount,n,0",
                "enabled,l,.t.",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,r,0",
                "left,n,0",
                "name,c,",
                "objects,*,",
                "parent,o!,","parentclass,c!,",
                "righttoleft,L,false",
                "tag,c,","tooltiptext,c,","top,n,0",
                "visible,l,.t.",
                ];
        }
    }
}
