/*
 * 2026.07.26 - JLW
 *      Header object for a grid column
 *      
 *      
 */
using Avalonia.Controls.Templates;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Header : XBase_Class_Avalonia
    {
        public new string MyBaseClass = "Header";
        public new string MyDefaultName = "header";

        public XBase_Class_Visual_Header(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "header", string.IsNullOrWhiteSpace(name) ? MyDefaultName : name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);
            SetHeader();

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * GetProperty method returns
         * 0 = Successfully returning value
         * -1 = Error code
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            int result = 0;
            JAXObjects.Token returnToken = new();
            propertyName = propertyName.ToLower();

            // Column is a special case and won't call Base.GetProperty()
            // First, we double check to make sure that the property exists
            if (UserProperties.ContainsKey(propertyName))
            {
                // Get the property and fill in the value
                returnToken.CopyFrom(UserProperties[propertyName]);
                // Visual object common property handler
                switch (propertyName)
                {
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
         * Intercept property assignments, otherwise rely on the base
         *
         * Return result from XBase_Visual_Class
         * 0  - Successfully proccessed
         * 1  - Did not process
         * 2  - Requires special processing
         * 9  - Success, perform no more processing
         * 10 - ???Failure, perform no more processing???
         * 
         * >10 - Error code
         *
         *
         * Return from here
         * 0 - Successfully processed
         * >0 - Error Code
         *
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;
            Avalonia.Controls.DataGrid? grd = me.Parent is null || me.Parent.avaloniaObject is null ? null : (Avalonia.Controls.DataGrid)me.Parent.avaloniaObject;
            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    // Visual object common property handler
                    switch (propertyName)
                    {
                        case "alignment":
                            if (tk.Element.Type.Equals("N") == false)
                                result = 11;
                            else if (JAXLib.Between(tk.AsInt(), 0, 9) == false)
                                result = 41;
                            else
                            {
                                UserProperties[propertyName].Element.Value = tk.AsInt();
                                result = 9;
                                SetHeader();
                            }
                            break;

                        case "caption":
                        case "fontname":
                            if (tk.Element.Type.Equals("C") == false)
                                result = 11;
                            else
                            {
                                UserProperties[propertyName].Element.Value = tk.AsString()+"!!";
                                result = 9;
                                SetHeader();
                            }
                            break;

                        case "name":
                            if (tk.Element.Type.Equals("C") == false)
                                result = 11;
                            else if (AppHelper.IsLegalObjectName(tk.AsString()) == false)
                                result = 41;
                            break;

                        case "comment":
                        case "tag":
                        case "tooltiptext":
                            if (tk.Element.Type.Equals("C") == false)
                                result = 11;
                            break;


                        case "fontbold":
                        case "fontcondense":
                        case "fontitalic":
                        case "fontstrikethrough":
                        case "fontunderline":
                            if (tk.Element.Type.Equals("L") == false)
                                result = 11;
                            else
                            {
                                UserProperties[propertyName].Element.Value = tk.AsBool();
                                result = 9;
                                SetHeader();
                            }
                            break;

                        case "fontsize":
                            if (tk.Element.Type.Equals("N") == false)
                                result = 11;
                            else if (JAXLib.Between(tk.AsInt(), 1, 5000)==false)
                                result = 41;
                            else
                            {
                                UserProperties[propertyName].Element.Value = tk.AsInt();
                                result = 9;
                                SetHeader();
                            }
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
            if (result > 0)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }
            return result;
        }


        // Changes made to the header simply recreate the header
        private void SetHeader()
        {
            if (me.parent is null || me.parent.nvObject is null) return;

            Avalonia.Media.TextDecorationCollection? decorations = null;
            bool UL = UserProperties["fontunderline"].AsBool();
            bool ST = UserProperties["fontstrikethrough"].AsBool();

            if (UL || ST)
            {
                decorations = new();
                if (UL) decorations.Add(new Avalonia.Media.TextDecoration { Location = Avalonia.Media.TextDecorationLocation.Underline });
                if (ST) decorations.Add(new Avalonia.Media.TextDecoration { Location = Avalonia.Media.TextDecorationLocation.Strikethrough });
            }

            /*
             * ------------------------
             * Set up Alignment
             * ------------------------
             *  0 - Middle Left
             *  1 - Middle Right
             *  2 - Middle Center
             *  3 - Automatic
             *  4 - Top Left
             *  5 - Top Right
             *  6 - Top Center
             *  7 - Bottom Left
             *  8 - Bottom Right
             *  9 - Bottom Center
             */

            Avalonia.Layout.HorizontalAlignment HA = new();
            Avalonia.Layout.VerticalAlignment VA = new();

            switch (UserProperties["alignment"].AsInt())
            {
                case 1:
                    VA = Avalonia.Layout.VerticalAlignment.Top;
                    HA = Avalonia.Layout.HorizontalAlignment.Right;
                    break;

                case 2:
                    VA = Avalonia.Layout.VerticalAlignment.Top;
                    HA = Avalonia.Layout.HorizontalAlignment.Center;
                    break;

                case 3:
                    VA = Avalonia.Layout.VerticalAlignment.Stretch;
                    HA = Avalonia.Layout.HorizontalAlignment.Stretch;
                    break;

                case 4:
                    VA = Avalonia.Layout.VerticalAlignment.Center;
                    HA = Avalonia.Layout.HorizontalAlignment.Left;
                    break;

                case 5:
                    VA = Avalonia.Layout.VerticalAlignment.Center;
                    HA = Avalonia.Layout.HorizontalAlignment.Right;
                    break;

                case 6:
                    VA = Avalonia.Layout.VerticalAlignment.Center;
                    HA = Avalonia.Layout.HorizontalAlignment.Center;
                    break;

                case 7:
                    VA = Avalonia.Layout.VerticalAlignment.Bottom;
                    HA = Avalonia.Layout.HorizontalAlignment.Left;
                    break;

                case 8:
                    VA = Avalonia.Layout.VerticalAlignment.Bottom;
                    HA = Avalonia.Layout.HorizontalAlignment.Right;
                    break;

                case 9:
                    VA = Avalonia.Layout.VerticalAlignment.Bottom;
                    HA = Avalonia.Layout.HorizontalAlignment.Right;
                    break;

                default:
                    VA = Avalonia.Layout.VerticalAlignment.Top;
                    HA = Avalonia.Layout.HorizontalAlignment.Left;
                    break;
            }


            // Refernce the parent column
            Avalonia.Controls.DataGridColumn col = (Avalonia.Controls.DataGridColumn)me.parent!.nvObject!;

            // Set up a new header template
            col.HeaderTemplate = new FuncDataTemplate<object>((_, _) =>
            {
                return new Avalonia.Controls.Border
                {
                    Background = Avalonia.Media.Brushes.White,
                    Padding = new Avalonia.Thickness(6, 4),
                    Child = new Avalonia.Controls.TextBlock
                    {
                        Text = UserProperties["caption"].AsString(),
                        FontSize = UserProperties["fontsize"].AsInt() * 96.0 / 72.0,
                        FontWeight = UserProperties["fontbold"].AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal,
                        FontStyle = UserProperties["fontitalic"].AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal,
                        FontStretch = UserProperties["fontcondense"].AsBool() ? Avalonia.Media.FontStretch.Condensed : Avalonia.Media.FontStretch.Normal,
                        TextDecorations = decorations,
                        Foreground = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(UserProperties["foreground"].AsInt())),
                        Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(UserProperties["background"].AsInt())),
                        HorizontalAlignment = HA,
                        VerticalAlignment = VA
                    }
                };
            });
        }

        /*------------------------------------------------------------------------------------------*
         * Methods list
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods() => ["init", "load", "destroy"];

        /*------------------------------------------------------------------------------------------*
         * Events list
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents() =>
                [
                "click", "doubleclick", "error", "errormessage",
                "mouseenter", "mousehover", "mouseleave",
                "visiblechanged", "when"
                ];

        public override string[] JAXProperties() =>
                [
                "alignment,n,0",
                "backcolor,r,0",
                "caption,c,Header","comment,c,",
                "enabled,l,.t.",
                "FontBold,L,","FontCondense,L,","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,","FontUnderline,L,","forecolor,r,0",
                "name,c,header1",
                "parent,o,","parentclass,c,",
                "tag,c,","tooltiptext,c,"
                ];
    }
}
