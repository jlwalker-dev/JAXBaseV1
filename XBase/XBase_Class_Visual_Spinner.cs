using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Spinner : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Spinner";
        public new string MyDefaultName { get; } = "spinner";


        public Avalonia.Controls.NumericUpDown spn => (Avalonia.Controls.NumericUpDown)me.avaloniaObject!;

        int LastInputType = -1; // -1 = none, 0=spinner, 1=user key
        public XBase_Class_Visual_Spinner(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.NumericUpDown(), "Spinner", "spinner", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            if (InInit)
            {
                //result = base.PostInit(callBack, parameterList);
                //spn.InterceptArrowKeys = true;
                //spn.UpDownAlign = LeftRightAlignment.Right;
                //spn.InnerNumericUpDown.ValueChanged -= Spn_ValueChanged;
                //spn.InnerNumericUpDown.ValueChanged += Spn_ValueChanged;
                //spn.InnerNumericUpDown.TextChanged -= Spn_TextChanged;
                //spn.InnerNumericUpDown.TextChanged += Spn_TextChanged;
                //spn.InnerNumericUpDown.Validating -= Spn_Valid;
                //spn.InnerNumericUpDown.Validating += Spn_Valid;
                //spn.BorderWidth = 0;
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
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {

                JAXObjects.Token objtk = new();
                objtk.Element.Value = objValue;

                if (UserProperties.ContainsKey(propertyName))
                {
                    // Intercept special handling of properties
                    switch (propertyName)
                    {
                        case "alignment":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                int av = objtk.AsInt();
                                //spn.TextAlign = av switch
                                //{
                                //    0 => HorizontalAlignment.Left,
                                //    2 => HorizontalAlignment.Center,
                                //    3 => HorizontalAlignment.Right,   // TODO - automatic
                                //    _ => HorizontalAlignment.Right
                                //};

                                UserProperties["alignment"].Element.Value = JAXLib.Between(av, 0, 3) ? av : 1;
                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objValue);
                            //spn.BorderColor = XClass_AuxCode.IntToColor((int)objValue);
                            UserProperties["bordercolor"].Element.Value = objValue;
                            result = 9; // do nothing else
                            break;

                        case "borderwidth":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                //if (JAXLib.Between(objtk.AsInt(), 0, 15))
                                //    spn.BorderWidth = objtk.AsInt();
                                //else
                                //    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "hex":
                            //if (objtk.Element.Type.Equals("L"))
                            //    spn.Hexadecimal = objtk.AsBool();
                            //else
                            //    result = 11;
                            //break;

                        case "increment":
                            //if (objtk.Element.Type.Equals("N"))
                            //    spn.Increment = Convert.ToDecimal(objtk.AsFloat());
                            //else
                            //    result = 11;
                            break;

                        case "precision":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                //if (JAXLib.Between(objtk.AsInt(), 0, 10))
                                //    spn.DecimalPlaces = objtk.AsInt();
                                //else
                                //    result = 11;
                            }
                            else
                                result = 11;
                            break;

                        case "keyboardhighvalue":
                        case "keyboardlowvalue":
                        case "spinnerhighvalue":
                        case "spinnerlowvalue":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                // Update the property
                                UserProperties[propertyName].Element.Value = objtk.AsFloat();

                                decimal khv = UserProperties["keyboardhighvalue"].AsDecimal();
                                decimal shv = UserProperties["spinnerhighvalue"].AsDecimal();

                                decimal klv = UserProperties["keyboardlowvalue"].AsDecimal();
                                decimal slv = UserProperties["spinnerlowvalue"].AsDecimal();

                                // Update the min/max values
                                //spn.Maximum = System.Math.Max(khv, shv);
                                //spn.Minimum = System.Math.Max(klv, slv);

                                // Mark it as handled
                                result = 9;
                            }
                            else
                                result = 11;
                            break;


                        case "thousandsseparator":
                            //if (objtk.Element.Type.Equals("L"))
                            //    spn.ThousandsSeparator = objtk.AsBool();
                            //else
                            //    result = 11;
                            break;

                        case "text":
                            result = 1533;
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            //if (objtk.Element.Type.Equals("N"))
                            //{
                            //    isProgrammaticChange = true;
                            //    spn.ThousandsSeparator = objtk.AsBool();
                            //    isProgrammaticChange = false;
                            //}
                            //else
                            //    result = 11;
                            isProgrammaticChange = false;
                            break;
                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 0, 10))
                    {
                        if (result < 9)
                        {
                            result = 0;

                            // Last chance for processing
                            switch (propertyName)
                            {
                                case "***":
                                    break;
                            }

                            // Did we process it?
                            if (result == 0)
                            {
                                // We processed it or just need to save the property (perhaps again)
                                // Ignore the CA1854 as it won't put the value into the property
                                if (UserProperties.ContainsKey(propertyName))
                                    UserProperties[propertyName].Element.Value = objValue;
                                else
                                    result = 1559;
                            }
                        }
                        else
                            result = 0;
                    }
                }
                else
                    result = 1559;
            }

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
                int dp;

                switch (propertyName)
                {
                    case "text":
                        dp = UserProperties["precistion"].AsInt();
                        bool tp = UserProperties["thousandsseparator"].AsBool();
                        //returnToken.Element.Value = tp ? spn.Value.ToString($"N{dp}", CultureInfo.InvariantCulture) : spn.Value.ToString($"N{dp}");
                        result = 9;
                        break;

                    case "value":
                        // Integer fix
                        dp = UserProperties["precision"].AsInt();
                        returnToken.Element.Value = System.Math.Round(returnToken.AsDouble(), dp);
                        returnToken.Element.Dec = dp;
                        result = 9;
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
                "init","keypress","load","lostfocus","interactivechange",
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
            return [
                "alignment,n,1","anchor,n,0",
                "backcolor,R,255|255|255","BaseClass,C!,spinner","borderstyle,n,1","boundcolumn,n,1","boundto,l,.F.",
                "bordercolor,R,0","borderwidth,n,0",
                "Class,C!,spinner","ClassLibrary,C!,","Comment,C,","controlsource,c,","comment,c,","controlsource,c,",
                "disabledbackcolor,R,240|240|240","disabledforecolor,R,100|100|100",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0","format,c,",
                "height,n,23","hex,l,.F.",
                "increment,N,1",
                "keyboardhighvalue,n,2147483647.00","keyboardlowvalue,n,-2147483647.00",
                "left,N,0",
                "margin,n,2",
                "name,c,spinner",
                "originalvalue,N,0",
                "parent,o!,","parentclass,C!,","precision,N,0",
                "readonly,l,false","righttoleft,L,false",
                "sellength,n,0","selstart,n,0","seltext,c,","selectonentry,l,f",
                "spinnerhighvalue,n,2147483647.00","spinnerlowvalue,n,-2147483647.00",
                "tabindex,n,1","tabstop,l,true","tag,C,","thousandsseparator,l,.T.","top,N,0","tooltiptext,c,",
                "value,n,0","visible,l,true","width,N,120"
                ];
        }


        /* TODO - Using these two routines, we may be able to
         * have different keyboard/spinner values like VFP.  
         * The spinner Max/Min values will be the highest/lowest
         * between the two.  Then spinner will then be limited  
         * to SpinnerHigh and SpinnerLow values while the user
         * can type in values limited by keyboardhigh/keyboardlow.
         * 
         * Decide if programmatic or interactive change
         * The Spinner calls this as does the loss of focus
         */
        private void Spn_ValueChanged(object? sender, EventArgs e)
        {
            //if (spn.InnerNumericUpDown.LastChangeWasFromTyping == false)
            //{
            //    if (spn.Value > UserProperties["spinnerhighvalue"].AsDecimal())
            //        spn.Value = UserProperties["spinnerhighvalue"].AsDecimal();
            //    else if (spn.Value < UserProperties["spinnerlowvalue"].AsDecimal())
            //        spn.Value = UserProperties["spinnerlowvalue"].AsDecimal();

            //    if (isProgrammaticChange)
            //        _CallMethod("programmaticchange");
            //    else
            //        _CallMethod("interactivechange");
            //}
        }

        /*
         * Only fires when user is typing
         */
        private async void Spn_TextChanged(object? sender, EventArgs e)
        {
            await _CallMethod("interactivechange");
        }

        // here is where we do final decisions on the value.
        private async void Spn_Valid(object? sender, EventArgs e)
        {
            //if (spn.InnerNumericUpDown.LastChangeWasFromTyping)
            //{
            //    // keyboard high/low
            //    if (spn.Value > UserProperties["keyboardhighvalue"].AsDecimal())
            //        spn.Value = UserProperties["keyboardhighvalue"].AsDecimal();
            //    else if (spn.Value < UserProperties["keyboardlowvalue"].AsDecimal())
            //        spn.Value = UserProperties["keyboardlowvalue"].AsDecimal();
            //}
            //else
            //{
            //    // Spinner high/low
            //    if (spn.Value > UserProperties["spinnerhighvalue"].AsDecimal())
            //        spn.Value = UserProperties["spinnerhighvalue"].AsDecimal();
            //    else if (spn.Value < UserProperties["spinnerlowvalue"].AsDecimal())
            //        spn.Value = UserProperties["spinnerlowvalue"].AsDecimal();
            //}

            await _CallMethod("valid");
        }
    }
}
