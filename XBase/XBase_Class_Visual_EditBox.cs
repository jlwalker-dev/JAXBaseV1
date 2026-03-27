using JAXBase.Core;
using JAXBase.Utilities.Utilities;
using System.Security.Permissions;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_EditBox : XBase_Class_Avalonia
    {

        public Avalonia.Controls.TextBox edtBox => (Avalonia.Controls.TextBox)me.avaloniaObject!;

        public XBase_Class_Visual_EditBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.TextBox(), "EditBox", "edit", true, UserObject.urw);
            edtBox.AcceptsReturn = true;
            edtBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;

            // TextBox does NOT have Vertical/HorizontalScrollBarVisibility properties directly.
            // You must use ScrollViewer's attached properties instead (Avalonia standard pattern).
            Avalonia.Controls.ScrollViewer.SetVerticalScrollBarVisibility(edtBox, Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);      // Show when needed
            Avalonia.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(edtBox, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled); // Usually disable horizontal
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
        public new virtual async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "bordercolor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                int clrint = JAXUtilities.ReturnColorInt(tk.AsString());
                                edtBox.BorderBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(clrint));
                                objValue = clrint;
                            }
                            break;

                        case "borderstyle":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() == 0)
                                    edtBox.BorderThickness = new Avalonia.Thickness(0);
                                else
                                    edtBox.BorderThickness = new Avalonia.Thickness(UserProperties["borderwidth"].AsInt());
                            }
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = JAXLib.InList(result, 0, 9) ? 9 : result;
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 9))
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
                    App.SetError(result, $"{result}|", string.Empty);

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
                switch (propertyName)
                {
                    case "value":
                        returnToken.Element.Value = edtBox.Text??string.Empty;
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
                "addproperty", "move", "readexpression", "readmethod", "refresh", "resettodefault",
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
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus",
                "init","keypress","load","lostfocus",
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
                "addlinefeeds,l,.T.","alignment,n,3","allowtabs,l,.F.","anchor,n,0",
                "backcolor,R,255|255|255","backstyle,n,1","bordercolor,R,0","borderstyle,n,1","borderwidth,n,1","baseclass,C!,editbox",
                "class,C!,editbox","classlibrary,C!,","Comment,C,","controlsource,c,",
                "enablehyperlinks,l,false","Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "fontsize,N,9","forecolor,R,0","format,c,",
                "height,n,21",
                "left,N,0",
                "margin,n,2","maxlength,n,0","name,c,text1",
                "originalvalue,,",
                "parent,o!,","parentclass,C!,","passwordchar,c,",
                "readonly,l,false","righttoleft,L,false",
                "scrollbars,n,2","sellength,n,0","selstart,n,0","seltext,n,0","selectonentry,l,f","setoriginalwhen,n,0",
                "tabindex,n,1","tabstop,l,true","tag,C,","text,c,","top,N,0","tooltiptext,c,",
                "value,C,","visible,l,true",
                "width,N,0"
                ];
        }
    }
}

