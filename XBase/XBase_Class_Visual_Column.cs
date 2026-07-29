/****************************************************************************************************
 * 2026-07-22 - JLW
 *      New attempt at column support
 *      
 * Column Types
 *   0 - Text column (default)
 *   1 - Checkbox
 *   2 - Image
 *   3 - Combo
 *   4 - Link
 *   5 - Button
 *   6 - Spinner
 *   
 *   
 * TODO - Add header template
 * 
 * var column = myDataGrid.Columns[0] as DataGridTextColumn;
 * 
 * column.HeaderTemplate = new FuncDataTemplate<object>((_, _) =>
 * {
 *     return new TextBlock
 *     {
 *         Text = "Custom Header",
 *         FontWeight = FontWeight.Bold,
 *         // add more controls if needed
 *     };
 * });
 * 
 ****************************************************************************************************/

using Avalonia.Controls.Templates;
using Avalonia.Styling;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Column : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Column";
        public new string MyDefaultName { get; } = "column";

        public Avalonia.Controls.DataGridColumn Column => (Avalonia.Controls.DataGridColumn)me.nvObject!;

        private string temp = "";

        public XBase_Class_Visual_Column(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(null, "column", string.IsNullOrWhiteSpace(name) ? MyDefaultName : name, false, UserObject.URW);
            temp = name;
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            UserProperties["name"].Element.Value = temp;

            bool result = await base.PostInit(callBack, parameterList);

            // Get the columntype value
            int colTypeInt = UserProperties["columntype"].AsInt();
            string initialBinding = UserProperties["recordsource"].AsString();

            // Create column based on type
            Avalonia.Controls.DataGridColumn col = new Avalonia.Controls.DataGridTemplateColumn(); // Use template for all to unify

            if (colTypeInt == 0)
            {
                col = new Avalonia.Controls.DataGridTextColumn();

                if (col is Avalonia.Controls.DataGridTextColumn textCol && !string.IsNullOrEmpty(initialBinding))
                {
                    textCol.Binding = new Avalonia.Data.Binding(initialBinding);
                    AppIO.DebugLog($"Inital binding = {initialBinding}");
                }
            }
            else if (col is Avalonia.Controls.DataGridTemplateColumn templateCol)
            {
                templateCol.CellTemplate = await CreateCellTemplate(colTypeInt, initialBinding);
                templateCol.CellEditingTemplate = await CreateCellEditingTemplate(colTypeInt, initialBinding);
            }

            me.nvObject = col;

            // Set up the header
            JAXObjectWrapper header = new(Program.CurrentApp, "header", "header1", []);
            await AddObject(header);
            await header.SetProperty("caption", "Header", 0);

            return result;
        }

        /*
         * Add a header to the column
         */
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 0;
            if (CanUseObjects == false) throw new Exception("3019|");

            if (value.BaseClass.Equals("header"))
            {
                UserProperties[me.cPropObjects].Add(value);
                UserProperties[me.cPropControlCount].Element.Value = UserProperties[me.cPropObjects].Col;
                value.SetParent(me);
            }
            else
                err = 3016;

            return err > 0 ? -1 : UserProperties[me.cPropObjects]._avalue.Count;
        }



        // Helper to apply common properties (font, colors, etc.) to per-cell controls
        private void ApplyCommonProperties(Avalonia.Controls.Control control, bool isLink = false)
        {
            // Get common values once
            int backColorInt = UserProperties.TryGetValue("backcolor", out var backColorTok) && backColorTok.Element.Value is int bc ? bc : 0;
            int foreColorInt = UserProperties.TryGetValue("forecolor", out var foreColorTok) && foreColorTok.Element.Value is int fc ? fc : 0;
            string fontName = UserProperties["fontname"].AsString() ?? "Default";
            double fontSize = UserProperties["fontsize"].AsDouble();
            bool fontBold = UserProperties["fontbold"].AsBool();
            bool fontItalic = UserProperties["fontitalic"].AsBool();
            bool fontStrike = UserProperties.TryGetValue("fontstrikethrough", out var strikeTok) && strikeTok.Element.Value is bool s && s;
            bool fontUnderline = UserProperties.TryGetValue("fontunderline", out var underlineTok) && underlineTok.Element.Value is bool u && u;

            // Prepare foreground brush
            Avalonia.Media.IBrush foreBrush = isLink
                ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(0, 0, 255))
                : new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(foreColorInt));

            // Prepare decorations
            Avalonia.Media.TextDecorationCollection decorations = new Avalonia.Media.TextDecorationCollection();
            if (fontStrike) decorations.Add(new Avalonia.Media.TextDecoration { Location = Avalonia.Media.TextDecorationLocation.Strikethrough });
            if (fontUnderline || isLink) decorations.Add(new Avalonia.Media.TextDecoration { Location = Avalonia.Media.TextDecorationLocation.Underline });

            // Type-specific application
            if (control is Avalonia.Controls.Primitives.TemplatedControl tCtrl)
            {
                // For Button, ComboBox, NumericUpDown, CheckBox (all TemplatedControl)
                tCtrl.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(backColorInt));
                tCtrl.Foreground = foreBrush;
                tCtrl.FontFamily = new Avalonia.Media.FontFamily(fontName);
                tCtrl.FontSize = fontSize;
                tCtrl.FontWeight = fontBold ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                tCtrl.FontStyle = fontItalic ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;

                // Decorations via style for internal text (TextBlock)
                if (decorations.Count > 0)
                {
                    tCtrl.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<Avalonia.Controls.TextBlock>())
                    {
                        Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.TextBlock.TextDecorationsProperty, decorations) }
                    });
                }
            }
            else if (control is Avalonia.Controls.TextBox textBox)
            {
                // For TextBox (for editing templates)
                textBox.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(backColorInt));
                textBox.Foreground = foreBrush;
                textBox.FontFamily = new Avalonia.Media.FontFamily(fontName);
                textBox.FontSize = fontSize;
                textBox.FontWeight = fontBold ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                textBox.FontStyle = fontItalic ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;

                if (decorations.Count > 0)
                {
                    textBox.Template = new FuncControlTemplate<Avalonia.Controls.TextBox>((parent, scope) =>
                    {
                        var scrollViewer = new Avalonia.Controls.ScrollViewer
                        {
                            Name = "PART_ContentHost"
                        };
                        scrollViewer.Template = new FuncControlTemplate<Avalonia.Controls.ScrollViewer>((sv, svScope) =>
                        {
                            var textPresenter = new Avalonia.Controls.Presenters.TextPresenter
                            {
                                [!Avalonia.Controls.Presenters.TextPresenter.TextProperty] = new Avalonia.Data.Binding("Text")
                            };
                            // Apply decorations via style
                            textPresenter.Styles.Add(new Avalonia.Styling.Style(x => x.Is<Avalonia.Controls.Presenters.TextPresenter>())
                            {
                                Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.TextBlock.TextDecorationsProperty, decorations) }
                            });
                            return textPresenter;
                        });
                        return scrollViewer;
                    });
                }
                else
                {
                    textBox.Template = null; // Reset to default
                }
            }
            else if (control is Avalonia.Controls.TextBlock textBlock)
            {
                // For TextBlock (used in display templates)
                textBlock.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(backColorInt));
                textBlock.Foreground = foreBrush;
                textBlock.FontFamily = new Avalonia.Media.FontFamily(fontName);
                textBlock.FontSize = fontSize;
                textBlock.FontWeight = fontBold ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                textBlock.FontStyle = fontItalic ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                if (decorations.Count > 0)
                {
                    textBlock.TextDecorations = decorations;
                }
            }
            // No handling for Image
        }


        // New helper method to create CellTemplate based on type
        private async Task<Avalonia.Controls.Templates.IDataTemplate?> CreateCellTemplate(int colTypeInt, string bindingPath)
        {
            // Implement based on type, similar to PostInit, but using the new bindingPath
            switch (colTypeInt)
            {
                case 1: // CheckBox
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var checkBox = new Avalonia.Controls.CheckBox();
                        checkBox[!Avalonia.Controls.CheckBox.IsCheckedProperty] = new Avalonia.Data.Binding(bindingPath) { Mode = Avalonia.Data.BindingMode.TwoWay };
                        // Bubble checked changed
                        checkBox.IsCheckedChanged += async (s, e) => await _CallMethod("click");
                        ApplyCommonProperties(checkBox);
                        return checkBox;
                    });

                case 2: // Image
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var image = new Avalonia.Controls.Image();
                        if (!string.IsNullOrEmpty(bindingPath) && item is Dictionary<string, object> dict &&
                            dict.TryGetValue(bindingPath, out var value) &&
                            value is string path &&
                            !string.IsNullOrEmpty(path))
                        {
                            Program.CurrentApp.JaxImages!.RegisterImage(path, "", out string imageName);
                            int maxW = UserProperties["maximagewidth"].AsInt();
                            int maxH = UserProperties["maximageheight"].AsInt();
                            image.Source = Program.CurrentApp.JaxImages.GetImage(imageName, maxW, maxH);
                        }
                        ApplyCommonProperties(image);
                        return image;
                    });

                case 3: // ComboBox - Typically only editing template, so null for cell template
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) => new Avalonia.Controls.TextBlock
                    {
                        [!Avalonia.Controls.TextBlock.TextProperty] = new Avalonia.Data.Binding(bindingPath)
                    });

                case 4: // Link (as styled button)
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var button = new Avalonia.Controls.Button();
                        button[!Avalonia.Controls.Button.ContentProperty] = new Avalonia.Data.Binding(bindingPath);
                        button.Background = Avalonia.Media.Brushes.Transparent;
                        button.BorderThickness = new Avalonia.Thickness(0);
                        button.Padding = new Avalonia.Thickness(0);
                        button.Cursor = Avalonia.Input.Cursor.Parse("Hand");
                        button.Styles.Add(new Avalonia.Styling.Style(x => x.Is<Avalonia.Controls.Button>())
                        {
                            Setters =
                            {
                                new Avalonia.Styling.Setter(Avalonia.Controls.TextBlock.ForegroundProperty, Avalonia.Media.Brushes.Blue),
                                new Avalonia.Styling.Setter(Avalonia.Controls.TextBlock.TextDecorationsProperty, Avalonia.Media.TextDecorations.Underline)
                            }
                        });
                        button.Click += async (s, e) => await _CallMethod("click");
                        ApplyCommonProperties(button, true);
                        return button;
                    });

                case 5: // Button
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var button = new Avalonia.Controls.Button();
                        button[!Avalonia.Controls.Button.ContentProperty] = new Avalonia.Data.Binding(bindingPath);
                        button.Click += async (s, e) => await _CallMethod("click");
                        ApplyCommonProperties(button);
                        return button;
                    });

                case 6: // NumericUpDown - Display as TextBlock
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) => new Avalonia.Controls.TextBlock
                    {
                        [!Avalonia.Controls.TextBlock.TextProperty] = new Avalonia.Data.Binding(bindingPath)
                    });

                default:
                    return null;
            }
        }

        // New helper method to create CellEditingTemplate based on type
        private async Task<Avalonia.Controls.Templates.IDataTemplate?> CreateCellEditingTemplate(int colTypeInt, string bindingPath)
        {
            // Implement based on type, similar to PostInit
            switch (colTypeInt)
            {
                case 1: // CheckBox - Editing same as display
                    return await CreateCellTemplate(1, bindingPath); // Reuse cell template for editing
                case 2: // Image - Typically no editing, null or custom
                    return null;
                case 3: // ComboBox
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var combo = new Avalonia.Controls.ComboBox();
                        combo.ItemsSource = (IEnumerable<object>)UserProperties["itemssource"].Element.Value;
                        combo.DisplayMemberBinding = new Avalonia.Data.Binding(UserProperties["displaymember"].AsString());
                        combo.SelectedValueBinding = new Avalonia.Data.Binding(UserProperties["valuemember"].AsString());
                        combo[!Avalonia.Controls.ComboBox.SelectedItemProperty] = new Avalonia.Data.Binding(bindingPath) { Mode = Avalonia.Data.BindingMode.TwoWay };
                        combo.SelectionChanged += async (s, e) => await _CallMethod("click");
                        ApplyCommonProperties(combo);
                        return combo;
                    });
                case 4: // Link - Typically no editing, null or custom
                    return null;
                case 5: // Button - Typically no editing, null or custom
                    return null;
                case 6: // NumericUpDown
                    return new Avalonia.Controls.Templates.FuncDataTemplate<object>((item, namescope) =>
                    {
                        var numUpDown = new Avalonia.Controls.NumericUpDown();
                        numUpDown.Minimum = UserProperties["minimum"].AsDecimal();
                        numUpDown.Maximum = UserProperties["maximum"].AsDecimal();
                        numUpDown.Increment = UserProperties["increment"].AsDecimal();
                        numUpDown.FormatString = $"N{UserProperties["decimalplaces"].AsInt()}";
                        numUpDown[!Avalonia.Controls.NumericUpDown.ValueProperty] = new Avalonia.Data.Binding(bindingPath) { Mode = Avalonia.Data.BindingMode.TwoWay };
                        numUpDown.ValueChanged += async (s, e) => await _CallMethod("click");
                        ApplyCommonProperties(numUpDown);
                        return numUpDown;
                    });
                default:
                    return null;
            }
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
                        case "backcolor":
                        case "forecolor":
                            if (tk.Element.Type.Equals("N"))
                            {
                                var brush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(tk.AsInt()));

                                UserProperties[propertyName].Element.Value = tk.AsInt();

                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "columntype":
                            if (InInit)
                            {
                                // Can only changed during initialization
                                if (tk.Element.Type.Equals("N"))
                                {
                                    // 0 - TextBox
                                    // 1 - CheckBox
                                    // 2 - Image
                                    // 3 - ComboBox
                                    // 4 - Link
                                    // 5 - Button
                                    // 6 - Spinner
                                    if (JAXLib.Between(tk.AsInt(), 0, 6) == false)
                                        result = 41;
                                    else
                                        objValue = tk.AsInt();
                                }
                                else
                                    result = 11;
                            }
                            break;

                        case "fontbold":
                        case "fontitalic":
                        case "fontstrikethrough":
                        case "fontunderline":
                            if (tk.Element.Type.Equals("L"))
                            {
                                UserProperties[propertyName].Element.Value = tk.AsBool();

                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "fontname":
                            if (tk.Element.Type.Equals("C"))
                            {
                                UserProperties[propertyName].Element.Value = tk.AsString();

                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "fontsize":
                            if (tk.Element.Type.Equals("N"))
                            {
                                UserProperties[propertyName].Element.Value = tk.AsInt();

                                result = 9;
                            }
                            else
                                result = 11;
                            break;


                        case "name":
                            if (tk.Element.Type.Equals("C"))
                            {
                                // Handled internally
                                UserProperties["name"].Element.Value = tk.AsString();
                                result = 9;
                            }
                            else
                                result = 11;
                            break;

                        case "readonly":
                            if (tk.Element.Type.Equals("L"))
                            {
                                if (Column is not null)
                                    Column.IsReadOnly = tk.AsBool();
                            }
                            else
                                result = 11;
                            break;

                        case "recordsource":
                            if (tk.Element.Type.Equals("C"))
                            {
                                if (Column is Avalonia.Controls.DataGridTextColumn textCol)
                                {
                                    textCol.Binding = new Avalonia.Data.Binding(tk.AsString());
                                }
                                else if (Column is Avalonia.Controls.DataGridTemplateColumn templateCol)
                                {
                                    // Recreate templates with new binding path
                                    string newBinding = tk.AsString();
                                    int colTypeInt = UserProperties["columntype"].AsInt();  // Get stored column type

                                    // Recreate CellTemplate if applicable (e.g., for display)
                                    templateCol.CellTemplate = await CreateCellTemplate(colTypeInt, newBinding); // Use a new method to create template

                                    // Recreate CellEditingTemplate if applicable
                                    templateCol.CellEditingTemplate = await CreateCellEditingTemplate(colTypeInt, newBinding); // Use a new method to create editing template
                                }
                                if (grd != null)
                                {
                                    grd.InvalidateVisual();
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "value":
                            isProgrammaticChange = true;
                            if (grd is not null)
                                XBase_Class_Visual_Grid.Grid_SetSelectedCellValue(grd, objValue);
                            isProgrammaticChange = false;
                            break;

                        // Added: Handlers for new properties
                        case "maximagewidth":
                        case "maximageheight":
                            if (tk.Element.Type.Equals("N"))
                            {
                                // Applied in template factory; store for use
                            }
                            else
                                result = 11;
                            break;

                        case "itemssource":
                            if (tk.Element.Type.Equals("A"))
                            {
                                // Applied in combo template
                            }
                            else
                                result = 11;
                            break;

                        case "displaymember":
                        case "valuemember":
                        case "urltemplate":
                        case "command":
                            if (tk.Element.Type.Equals("C"))
                            {
                                // Stored for template use
                            }
                            else
                                result = 11;
                            break;

                        case "minimum":
                        case "maximum":
                        case "increment":
                        case "decimalplaces":
                            if (tk.Element.Type.Equals("N"))
                            {
                                // Applied in numeric template
                            }
                            else
                                result = 11;
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
                    case "value":
                        Avalonia.Controls.DataGrid grd = (Avalonia.Controls.DataGrid)me.Parent!.avaloniaObject!;

                        var (rowidx, colIdx, value) = XBase_Class_Visual_Grid.Grid_GetSelectedCellInfo(grd);
                        if (value is null)
                            returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = value;
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
         * Methods list
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
                [
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * Events list
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","doubleclick","error","errormessage","mouseenter","mousehover","mouseleave","visiblechanged","when"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * property data types
         * C = Character
         * N = Numeric I=Integer R=Color
         * D = Date
         * T = DateTime
         * L = Logical LY = Yes/No logical
         * Attributes
         * ! Protected - can't change after initialization
         * $ Special Handling - do not auto process
         *
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
                [
                "baseclass,C,column","backcolor,r,255|255|255",
                "class,C,column","classlibrary,C,","columnnumber,n,0","columntype,n!,0","comment,c,","controlcount,n,0",
                "enabled,l,.t.",
                "FontBold,L,","FontCondense,L,","FontItalic,L,false","FontName,C,Arial",
                "FontSize,N,9","FontStrikeThrough,L,","FontUnderline,L,","forecolor,r,0",
                "height,n,32",
                "name,c,",
                "objects,*,",
                "parent,o,","parentclass,c,",
                "readonly,L,","righttoleft,L,", "recordsource,c,",
                "tag,c,","tooltiptext,c,",
                "value,,","visible,L,.t.",
                "width,n,32"
                ];
        }
    }
}