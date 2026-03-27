/*
 * TODO - get rid of JAXLabel and use Avalonia.Label correctly
 * 
 */
using Avalonia.Controls;
using Avalonia.Media;
using JAXBase.Core;
using JAXBase.Utilities.Utilities;
using System.Globalization;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Label : XBase_Class_Avalonia
    {
        public JAXLabel lbl => (JAXLabel)me.avaloniaObject!;

        public XBase_Class_Visual_Label(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new JAXLabel(), "Label", "label", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            lbl.SizeChanged += Resize;

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }


        /*
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
         *     -1   - Error Code
         * 
         */
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            int w, h;
            bool a, b;

            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;    // Now we can type it easily!
            propertyName = propertyName.ToLower();

            App.DebugLog($"Label: {me.JOWName.ToUpper()}.{propertyName}={tk.AsString()}");

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    // Visual object common property handler
                    switch (propertyName.ToLower())
                    {
                        // Intercept special handling of properties
                        case "autosize":
                            if (tk.Element.Type.Equals("L"))
                            {
                                a = tk.AsBool();
                                h = UserProperties["height"].AsInt();
                                b = UserProperties["wordwrap"].AsBool();
                                w = UserProperties["width"].AsInt();

                                if (b)
                                    SetWordWrap(a, b, h, w);
                                else
                                {
                                    lbl.ClearValue(Avalonia.Controls.TextBlock.WidthProperty);
                                    lbl.ClearValue(Avalonia.Controls.TextBlock.HeightProperty);
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "backcolor":
                            lbl.Background = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            objValue = tk.AsInt();
                            break;

                        case "bordercolor":
                            lbl.BorderBrush = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            objValue = JAXUtilities.ReturnColorInt(tk.AsString());
                            break;

                        case "borderwidth":
                            lbl.BorderThickness = new Avalonia.Thickness(tk.AsDouble());
                            break;

                        case "caption":
                            objValue = tk.AsString();
                            lbl.Text = tk.AsString();
                            GetWidth();
                            break;

                        case "forecolor":
                            lbl.Foreground = new SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(tk.AsString())));
                            objValue = tk.AsInt();
                            break;

                        case "height":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    a = UserProperties["autosize"].AsBool();
                                    h = tk.AsInt();
                                    b = UserProperties["wordwrap"].AsBool();
                                    w = UserProperties["width"].AsInt();

                                    if (b)
                                        SetWordWrap(a, b, h, w);
                                    else
                                        lbl.Height = h;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "width":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                {
                                    a = UserProperties["autosize"].AsBool();
                                    b = UserProperties["wordwrap"].AsBool();
                                    h = UserProperties["height"].AsInt();
                                    w = tk.AsInt();

                                    if (b)
                                        SetWordWrap(a, b, h, w);
                                    else
                                        lbl.Width = w;

                                    objValue = lbl.Width;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "wordwrap":
                            if (tk.Element.Type.Equals("L"))
                            {
                                a = UserProperties["autosize"].AsBool();
                                b = tk.AsBool();
                                h = UserProperties["height"].AsInt();
                                w = Convert.ToInt32(objValue);

                                SetWordWrap(a, b, h, w);
                            }
                            else
                                result = 11;
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
                            break;
                    }

                    // Did we process it?
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
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }
            else
                result = 0;

            return result;
        }

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
                    case "autosize":
                    case "height":
                    case "wordwrap":
                        returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                        break;

                    case "width":
                        returnToken.Element.Value = GetWidth();

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
                    App.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        public override string[] JAXMethods()
        {
            return [
                "addproperty", "drag", "move", "readexpression", "readmethod", "refresh",
                "resettodefault", "saveasclass", "setfocus", "showwhatsthis", "writeexpression", "writemethod", "zorder"
                ];
        }

        public override string[] JAXEvents()
        {
            return [
                "click","dblclick","destroy","dragdrop","dragover","error","gotfocus",
                "init","interactivechagnge","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rangehigh","rangelow","rightclick","uienable","valid","visiblechanged","when"
            ];
        }

        public override string[] JAXProperties()
        {
            return [
                "alignment,n,0","anchor,n,0","autosize,l,false",
                "backcolor,R,255|255|255","backstyle,n,1","BaseClass,C!,label","bordercolor,R,0|0|0","borderwidth,n,0","borderstyle,n,0",
                "caption,c,","Class,C!,label","ClassLibrary,C!,","Comment,C,",
                "disabledbackcolor,R,220|220|220","disabledforecolor,R,128|128|128",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false",
                "FontName,C,Arial","FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false",
                "forecolor,R,0|0|0",
                "height,n,21",
                "left,N,0",
                "name,c,label1",
                "parent,o!,","parentclass,C!,",
                "righttoleft,L,false",
                "tabindex,n,1","tabstop,l,false","tag,C,","top,N,0","tooltiptext,c,",
                "visible,l,true","width,N,100","wordwrap,l,false"
                ];
        }


        /*
         * Word wrap handler for avalonia
         */
        public void SetWordWrap(bool autosize, bool wrap, int h, int w)
        {
            if (wrap)
            {
                // Wrap in the specified area
                lbl.Height = h;
                lbl.Width = w;
                lbl.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                lbl.ClearValue(Avalonia.Controls.TextBlock.WidthProperty);
                lbl.ClearValue(Avalonia.Controls.TextBlock.HeightProperty);
            }
            else
            {
                if (autosize)
                {
                    lbl.ClearValue(Avalonia.Controls.TextBlock.HeightProperty);
                    lbl.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                }
                else
                {
                    lbl.TextWrapping = Avalonia.Media.TextWrapping.NoWrap;
                    lbl.Height = h;
                    lbl.Width = w;
                }
            }

            // Update the properties
            GetWidth();
        }

        private double GetWidth()
        {
            double result = 0;

            if (UserProperties["autosize"].AsBool())
            {
                bool rendered = false;

                if (me.parent is not null)
                {
                    JAXObjects.Token answer = me.parent.GetPrivateProperty("rendered");
                    if (answer.Element.IsNull() == false)
                    {
                        if (answer.AsBool())
                        {
                            // It's rendered
                            rendered = true;
                        }
                    }
                }

                if (rendered)
                    result = lbl.Bounds.Width;
                else
                {
                    var typeface = new Typeface(lbl.FontFamily, lbl.FontStyle, lbl.FontWeight, lbl.FontStretch);
                    var formattedText = new FormattedText(
                        lbl.Text ?? string.Empty,
                        CultureInfo.CurrentUICulture,
                        Avalonia.Media.FlowDirection.LeftToRight,
                        typeface,
                        lbl.FontSize,
                        lbl.Foreground ?? Avalonia.Media.Brushes.Black
                    );

                    double expectedExtra = lbl.Padding.Left + lbl.Padding.Right;
                    Console.WriteLine($"extra={expectedExtra}");

                    //// Apply properties that affect layout/wrapping (these trigger internal re-calc)
                    //formattedText.TextAlignment = lbl.TextAlignment;

                    //// Apply constraints
                    //formattedText.MaxTextWidth = int.MaxValue;
                    //formattedText.MaxTextHeight = int.MaxValue;
                    //formattedText.MaxLineCount = int.MaxValue;  // 0 = unlimited (or set to e.g. 3 to limit lines)

                    // Now read the measured results
                    result = formattedText.WidthIncludingTrailingWhitespace * App.DefaultScaling;  // Usually the one you want
                    UserProperties["width"].Element.Value = result; // Set the current width
                }
            }
            else
                result = UserProperties["width"].AsDouble();

            return result;
        }


        /* -----------------------------------------------------------------------------------------*
         * Events for the control
         * -----------------------------------------------------------------------------------------*/
        private void Resize(object? sender, SizeChangedEventArgs e)
        {
            Avalonia.Size size = e.NewSize;
            UserProperties["width"].Element.Value = size.Width;
            UserProperties["height"].Element.Value = size.Height;
        }
    }
}
