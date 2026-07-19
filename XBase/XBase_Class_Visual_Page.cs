/*
 * Required in order to make the PageFrame work with the system
 */
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    internal class XBase_Class_Visual_Page : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Page";
        public new string MyDefaultName { get; } = "page";


        public Avalonia.Controls.TabItem pfPage => (Avalonia.Controls.TabItem)me.avaloniaObject!;
        public Avalonia.Controls.TabControl? pgFrame => me.parent is null ? null : (Avalonia.Controls.TabControl?)me.parent.avaloniaObject;
        public Avalonia.Controls.Canvas InnerCanvas;

        public XBase_Class_Visual_Page(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.TabItem(), "Page", "page", true, UserObject.URW);

            InnerCanvas = new Avalonia.Controls.Canvas
            {
                Background = Avalonia.Media.Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
        }

        /* ----------------------------------------*
         * Final setup of properties
         * ----------------------------------------*/
        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            pfPage.Content = InnerCanvas;

            if (InInit)
            {
                base.ClearEvents(false);
                pfPage.Tapped += PfPage_Tapped;
            }

            bool result = await base.PostInit(callBack, parameterList);


            return result;
        }


        /*
         * Intercept the click, update the active page, and select it
         */
        private void PfPage_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            int thisPg = UserProperties["pagenumber"].AsInt();

            if (thisPg > 0)
            {
                if (me.parent is not null)
                {
                    me.parent.thisObject!.UserProperties["activepage"].Element.Value = thisPg;

                    if (pgFrame is not null)
                        pgFrame.SelectedIndex = thisPg - 1;
                }
            }

            e.Handled = true;
        }


        /*
         * Handle any cases that need special processing when
         * adding a new object to the form
         */
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            string msg = "";

            try
            {
                // Add valid controls to the canvas
                if (value.avaloniaObject is not null)
                    InnerCanvas.Children.Add(value.avaloniaObject!);
                else if (value.nvObject is Avalonia.Controls.Shapes.Path)
                    InnerCanvas.Children.Add((Avalonia.Controls.Shapes.Path)value.nvObject!);
                else if (value.nvObject is not null)
                {
                    // It's something else then add it to the form's objects collection
                    UserProperties["objects"].Add(value);
                    UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;
                    value.SetParent(me);
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                err = 1980;
            }

            if (err == 0)
            {
                // If everything is ok, add it to the Objects array
                UserProperties["objects"].Add(value);
                UserProperties["controlcount"].Element.Value = UserProperties["controlcount"].AsInt() + 1;

                // now update the parent
                if (value.thisObject is not null)
                    value.SetParent(me);
            }
            else
            {
                // Something went wrong
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|{value.JOWName}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
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
            propertyName = propertyName.ToLower();
            JAXObjects.Token tkObj = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "backcolor":
                            InnerCanvas.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tkObj.AsString())));
                            objValue = JAXUtilities.ReturnColorInt(tkObj.AsString());
                            break;

                        case "caption":
                            if (tkObj.Element.Type.Equals("C"))
                                pfPage.Header = tkObj.AsString();
                            else
                                result = 11;
                            break;

                        case "foreground":
                            break;

                        case "picture":
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
                    else
                        result = 1559;
                }


                if (result > 0)
                {
                    _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                    if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                        AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    result = -1;
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
                //returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    // Intercept special handling of properties
                    case "caption":
                        returnToken.Element.Value = pfPage.Header ?? "";
                        break;

                    case "pagenumber":
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;

                    result = 0;
                }

            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}|{propertyName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * 
         * May decide to restore the addobject and removeobject methods to remain consistent.
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                "addproperty","addobject","move","readexpression","readmethod","refresh","resettodefault",
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
                "activate","click","dblclick","destroy","error","gotfocus","init","keypress","load","lostfocus",
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
         *          ! Protected - user can't change
         *          $ Special Handling - do not auto process
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return [
                "activecontrol,N!,0",
                "backcolor,R,255|255|255","backstyle,n,1",
                "baseclass,C!,page",
                "caption,c,","class,C!,page","classlibrary,C!,","comment,C,","controlcount,N!,0",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","fontsize,N,9",
                "FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0|0|0",
                "keypreview,L,false",
                "name,C,page",
                "objects,*,",
                "pagenumber,n!,0","parent,o!,","parentclass,C!,","picture,C,",
                "righttoleft,l,.f.",
                "tag,C,","tooltiptext,c,",
                "visible,L,true"
                ];
        }
    }
}
