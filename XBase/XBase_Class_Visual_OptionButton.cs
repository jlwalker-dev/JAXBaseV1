/*
 * Option Button
 */
using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    internal class XBase_Class_Visual_OptionButton : XBase_Class_Avalonia
    {
        public Avalonia.Controls.RadioButton optBtn => (Avalonia.Controls.RadioButton)me.avaloniaObject!;

        public XBase_Class_Visual_OptionButton(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.RadioButton(), "OptionButton", "option", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            if (InInit)
            {
                optBtn.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                optBtn.Tapped += OptBtn_Tapped;
                optBtn.IsCheckedChanged += OptBtn_CheckedChanged;
            }

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }


        // Don't allow events to bubble up
        private void OptBtn_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            e.Handled = true;
        }


        // If there is a change, and it's allowed to go through, then
        // if checked, call the parent's valid method
        private async void OptBtn_CheckedChanged(object? sender, EventArgs e)
        {
            if (isProgrammaticChange)
                await _CallMethod("programmaticchange");
            else
                await _CallMethod("interactivechange");

            if (App.ReturnValue.AsBool() && (me.parent is not null) && (optBtn.IsChecked ?? false))
            {
                await me.parent.SetProperty("value", UserProperties["value"].AsInt());
                await me.parent.MethodCall("valid");
            }
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
            int h, w;
            bool a, b;
            propertyName = propertyName.ToLower();
            JAXObjects.Token objtk = new();
            objtk.Element.Value = objValue;

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        // Intercept special handling of properties
                        case "autosize":
                            a = (bool)objValue;
                            h = UserProperties["height"].AsInt();
                            b = UserProperties["wordwrap"].AsBool();
                            w = UserProperties["width"].AsInt();

                            SetWordWrap(a, b, h, w);
                            break;

                        case "height":
                            a = UserProperties["autosize"].AsBool();
                            h = Convert.ToInt32(objValue);
                            b = UserProperties["wordwrap"].AsBool();
                            w = UserProperties["width"].AsInt();

                            if (b)
                                SetWordWrap(a, b, h, w);
                            else
                                optBtn.Height = h;
                            break;

                        case "width":
                            a = UserProperties["autosize"].AsBool();
                            b = UserProperties["wordwrap"].AsBool();
                            h = UserProperties["height"].AsInt();
                            w = Convert.ToInt32(objValue);

                            if (b)
                                SetWordWrap(a, b, h, w);
                            else
                                optBtn.Width = w;
                            break;

                        case "wordwrap":
                            a = UserProperties["autosize"].AsBool();
                            b = (bool)objValue;
                            h = UserProperties["height"].AsInt();
                            w = UserProperties["width"].AsInt();

                            SetWordWrap(a, b, h, w);
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            if ("N".Contains(objtk.Element.Type) == false)
                                result = 11;
                            isProgrammaticChange = false;
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
                        // Do we skip?
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
                    App.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
                //returnToken.CopyFrom(UserProperties[propertyName]);

                switch (propertyName)
                {
                    case "height":
                        result = 1; // Can't rely on height of control due to word wrap
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
                "addproperty","drag","move","readexpression","readmethod","refresh","resettodefault",
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
                "click","dblclick","destroy","error","gotfocus",
                "init","interactivechange","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rightclick","valid","visiblechanged","when"
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
                "alignment,n,0","anchor,n,0","autosize,l,false","backcolor,R,255|255|255","backstyle,n,1",
                "BaseClass,C,commandbutton",
                "caption,c,Option1",
                "Class,C,Grid","ClassLibrary,C,",
                "Comment,C,",
                "disabledbackcolor,R!,140|140|140","disabledforecolor,R!,64|64|64","disabledpicture,c,","downpicture,c,",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forcolor,R,0",
                "Height,N,30",
                "left,N,0",
                "name,c,command",
                "originalvalue,,",
                "parent,o,","parentclass,C,","picture,c,","picturemargin,n,0","pictureposition,n,13","picturespacing,n,0",
                "righttoleft,L,false",
                "setoriginalwhen,n,0",
                "tabindex,n,1","tabstop,l,true","tag,C,","top,N,0","tooltiptext,c,",
                "value,n,1","visible,l,true",
                "width,N,100","wordwrap,l,false"
                ];
        }

        /*
         * The autosize occurs by injecting a TextBlock into the content.
         * Since the option button is always confined by the optiongroup,
         * autosize isn't really useful.
         */
        public void SetWordWrap(bool autosize, bool wrap, int h, int w)
        {
            if (InInit)
                return;

            if (wrap)
            {
                optBtn.ClearValue(Avalonia.Controls.TextBlock.HeightProperty);
                optBtn.Width = w;
            }
            else
            {
                optBtn.Height = h;
                optBtn.Width = w;
            }

            var textBlock = new Avalonia.Controls.TextBlock
            {
                Text = UserProperties["caption"].AsString(),  // "Option 2"
                TextWrapping = wrap ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap,
                MaxWidth = w,  // 100
                Margin = new Avalonia.Thickness(3)
            };

            // Measure the TextBlock with constrained width and infinite height
            textBlock.Measure(new Avalonia.Size(w, System.Double.PositiveInfinity));

            double textHeight = textBlock.DesiredSize.Height;

            // Calculate the RadioButton height based on Fluent theme MinHeight=32
            UserProperties["height"].Element.Value = System.Math.Max(32, textHeight);

            // Now set the content (actualHeight can be used for positioning before adding to canvas)
            optBtn.Content = textBlock;
        }

    }
}