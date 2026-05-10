/* 
 * GridClass 2026 - 03 - 08
 * 
 * I started testing on 3/1 and for the first 6 days, I could not get the grid to display
 * Finally got it to show and sometimes, even though co-pilot and grok both say it's
 * legal and correct, the form fails to render and shows up as a very wide form
 * with a black background and nothing appears.
 * 
 * An exaple is in PostInit with the setters.  If I uncomment any of the commented
 * setters, the window fails to render.
 * 
 * Three days later, I'm still not showing data and I know the problem.  When
 * the recordsourcetype is set, that's when data is attached to the grid.
 * However, at that time, the columns don't have the correct bindings (they're null).
 * When the code, which seems to be what is needed is executed, again, the black
 * form of death.
 * 
 * At this point, I'm stopping with grids (with great disspointment) and moving on
 * to try to get the final visual classes done.
 * 
 */
using Avalonia.Input;
using Avalonia.Styling;
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Controls;
using static JAXBase.Core.AppClass;
namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Grid : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Grid";
        public new string MyDefaultName { get; } = "grid";


        public Avalonia.Controls.DataGrid grid => (Avalonia.Controls.DataGrid)me.avaloniaObject!;
        private ObservableCollection<Dictionary<string, object>> _gridRows = new ObservableCollection<Dictionary<string, object>>();

        private bool _allowUserToAddRows = false; // default = prevent adding
        public bool AllowUserToAddRows
        {
            get => _allowUserToAddRows;
            set
            {
                _allowUserToAddRows = value;
            }
        }
        bool doPostInitSetup = true;

        public XBase_Class_Visual_Grid(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            AppIO.DebugLog("Initializing grid", false);
            SetVisualObject(new Avalonia.Controls.DataGrid(), "Grid", "grid", true, UserObject.URW);
        }


        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {

            // Process named parameters
            foreach (var param in parameterList)
            {
                if (UserProperties.ContainsKey(param.PName.ToLower()))
                {
                    object? propValue = AppHelper.GetParameterValue(param);

                    if (propValue is not null)
                        await SetProperty(param.PName, propValue, 0);
                }
            }

            // ----------------------------------------
            // Set up events
            // ----------------------------------------
            if (doPostInitSetup)
            {
                // Added: Subscribe to grid events for bubbling from columns
                grid.RowEditEnding += DgvMain_BeforeCellChange;
                grid.CurrentCellChanged += DgvMain_AfterCellChanged;
                grid.KeyDown += DgvMain_KeyPress;

                // Added: General click handler for bubbled events (e.g., from button/link columns)
                grid.PointerPressed += Grid_PointerPressed;
                doPostInitSetup = false;
                grid.InvalidateVisual();
            }

            grid.ItemsSource = _gridRows;
            grid.AutoGenerateColumns = false;
            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All; // Default to show lines for visibility

            // Try to fix the header text and borders
            var customHeaderTheme = new ControlTheme
            {
                TargetType = typeof(Avalonia.Controls.DataGridColumnHeader),
                Setters =
                    {
                        //new Setter(Avalonia.Controls.DataGridColumnHeader.BackgroundProperty, Brushes.DarkGray),
                        //new Setter(Avalonia.Controls.DataGridColumnHeader.ForegroundProperty, Brushes.Black),
                        new Setter(Avalonia.Controls.DataGridColumnHeader.FontWeightProperty, Avalonia.Media.FontWeight.Bold),
                        //new Setter(Avalonia.Controls.DataGridColumnHeader.SeparatorBrushProperty, Brushes.Black),
                        //new Setter(Avalonia.Controls.DataGridColumnHeader.BorderBrushProperty, Brushes.DarkGray),
                        new Setter(Avalonia.Controls.DataGridColumnHeader.PaddingProperty, new Avalonia.Thickness(0)),
                        //new Setter(Avalonia.Controls.DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                        new Setter(Avalonia.Controls.DataGridColumnHeader.BorderThicknessProperty, new Avalonia.Thickness(1))
                    }
            };

            //// Add the style to the DataGrid's Styles collection
            grid.ColumnHeaderTheme = customHeaderTheme;

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }

        // Added: Handler for bubbled clicks (e.g., from button or link columns)
        private async void Grid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
            {
                await _CallMethod("click");
            }
        }

        /*
         * Manually add a new column to the form
         */
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 1903;

            // You can only add columns to a grid's objects list
            if (value.nvObject is not null && value.nvObject is DataGridColumn)
            {
                await value.SetProperty("columnnumber", grid.Columns.Count);

                // Add the created column from the wrapper
                Avalonia.Controls.DataGridColumn thiscol = (Avalonia.Controls.DataGridColumn)value.nvObject!;
                thiscol.Header = string.IsNullOrEmpty(value.thisObject!.UserProperties["caption"].ToString()) ? $"Column{grid.Columns.Count + 1}" : value.thisObject.UserProperties["caption"].ToString();

                // TODO - figure out binding
                //if (thiscol is Avalonia.Controls.DataGridBoundColumn boundCol && !string.IsNullOrEmpty(binding))
                //{
                //    boundCol.Binding = new Avalonia.Data.Binding(col!.ColumnName);
                // }

                thiscol.IsReadOnly = UserProperties["readonly"].AsBool();
                grid.Columns.Add(thiscol);

                AppIO.DebugLog("AddObject: " + value.JOWName);

                // Added: Refresh grid after addition
                grid.InvalidateVisual();

                // If everything is ok, add it to the Objects array
                UserProperties["objects"].Add(value);

                // Make sure controlcount stays up to date
                if (UserProperties["controlcount"].AsInt() < grid.Columns.Count)
                    UserProperties["controlcount"].Element.Value = grid.Columns.Count;

                // Make sure columncount stays up to date
                if (UserProperties["columncount"].AsInt() < grid.Columns.Count)
                    UserProperties["columncount"].Element.Value = grid.Columns.Count;

                value.SetParent(me);
                err = 0;
            }

            if (err > 0)
            {
                // Something went wrong
                _AddError(err, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return err > 0 ? -1 : grid.Columns.Count;
        }

        public async Task<int> AddColumn(int type, string header, string binding)
        {
            // Set the columntype parameter
            List<ParameterClass> p = new List<ParameterClass>();
            ParameterClass colType = new ParameterClass();
            colType.token.Element.Value = type;
            colType.Type = "N";
            colType.PName = "columntype";
            p.Add(colType);

            // Create the column based on type
            string colName = $"Column{grid.Columns.Count + 1}";
            JAXObjectWrapper colObj = new JAXObjectWrapper(App, "column", colName, p);

            await colObj.SetProperty("columnnumber", grid.Columns.Count);
            UserProperties["objects"].Add(colObj);

            // Add the created column from the wrapper
            Avalonia.Controls.DataGridColumn col = (Avalonia.Controls.DataGridColumn)colObj.nvObject!;
            col.Header = string.IsNullOrEmpty(header) ? colName : header;
            if (col is Avalonia.Controls.DataGridBoundColumn boundCol && !string.IsNullOrEmpty(binding))
            {
                boundCol.Binding = new Avalonia.Data.Binding(binding);
                AppIO.DebugLog($"Column bound to {binding}");
            }

            col.IsReadOnly = UserProperties["readonly"].AsBool();
            grid.Columns.Add(col);

            // If columncount<grid.columns.count then update it
            if (UserProperties["columncount"].AsInt() < grid.Columns.Count)
                UserProperties["columncount"].Element.Value = grid.Columns.Count;

            if (UserProperties["controlcount"].AsInt() < grid.Columns.Count)
                UserProperties["controlcount"].Element.Value = grid.Columns.Count;

            // Added: Refresh grid after addition
            grid.InvalidateVisual();

            AppIO.DebugLog("AddColumn: " + colName);
            return grid.Columns.Count;
        }

        /*
         * ADDCOLUMN(x)
         * Add a column where x is a numeric value indicating
         * the type of column to add. If columncount is 0
         * then it becomes Column1. Setting column count
         * to a higher number will simply add columns after.
         *
         */
        public override async Task<int> _CallMethod(string methodName)
        {
            int results = 0;
            string msg = string.Empty;
            methodName = methodName.ToLower();
            App.ReturnValue.Element.Value = true;
            try
            {
                if (Methods.ContainsKey(methodName))
                {
                    string cCode = Methods[methodName].CompiledCode;
                    // Create a new App.Levels and execute the code
                    if (cCode.Length > 0)
                        results = await base._CallMethod(methodName);
                    else
                    {
                        switch (methodName)
                        {
                            case "addcolumn":
                                // Should be just one parameter
                                if (App.ParameterClassList.Count > 1)
                                    results = 94;
                                else
                                {
                                    JAXObjects.Token tk = new();
                                    if (App.ParameterClassList.Count == 1)
                                    {
                                        object? obj = AppHelper.GetParameterValue(App.ParameterClassList[0]);
                                        if (obj is null)
                                            tk.Element.MakeNull();
                                        else
                                            tk.Element.Value = obj;
                                    }
                                    if (tk.Element.Type.Equals("N"))
                                    {
                                        // 0:Text, 1:Checkbox, 2:Image, 3:ComboBox, 4:Link, 5: Button, 6: Spinner
                                        // Masked TextBox LATER?-> 7: Numeric, 8:DateTime, 9:Date, 10:Masked, 11:Currency
                                        if (JAXLib.Between(tk.AsInt(), 0, 6))
                                        {
                                            await AddColumn(tk.AsInt(), string.Empty, string.Empty);
                                        }
                                        else
                                            results = 11;
                                    }
                                }
                                break;
                            default:
                                results = await base._CallMethod(methodName);
                                break;
                        }
                    }
                }
                else
                {
                    msg = methodName;
                    results = 6501;
                }
            }
            catch (Exception ex)
            {
                results = 9999;
                msg = ex.Message;
            }

            if (results > 0)
            {
                _AddError(results, 0, msg, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(results, $"{results}|{msg}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                App.ReturnValue.Element.Value = false;
                results = -1;
            }
            return results;
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         *
         * Return result from XBase_Visual_Class
         * 0 - Successfully proccessed
         * 1 - Did not process
         * 2 - Requires special processing
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
            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    result = 0;
                    // First, we check to make sure that the property exists
                    if (UserProperties.ContainsKey(propertyName))
                    {
                        // Visual object common property handler
                        switch (propertyName)
                        {
                            case "allowaddnew":
                                if (tk.Element.Type.Equals("L") == false)
                                    result = 11;
                                break;

                            case "allowautocolumnfit":
                                if (tk.Element.Type.Equals("N"))
                                    ApplyAutoSizeMode(tk.AsInt());
                                else
                                    result = 11;
                                break;

                            case "autocellselection":
                                if (tk.Element.Type.Equals("L") == false)
                                    result = 11;
                                break;

                            case "allowheaderresizing":
                                if (tk.Element.Type.Equals("L"))
                                    grid.CanUserResizeColumns = tk.AsBool();
                                else
                                    result = 11;
                                break;

                            case "allowrowresizing":
                                if (tk.Element.Type.Equals("L"))
                                    grid.RowHeight = tk.AsBool() ? Double.NaN : UserProperties["rowheight"].AsInt();
                                else
                                    result = 11;
                                break;

                            case "columncount":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    int newCount = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                    if (newCount < grid.Columns.Count)
                                    {
                                        // Subtract from the end
                                        int pos = grid.Columns.Count - 1;
                                        while (newCount < grid.Columns.Count)
                                        {
                                            // Find the reference JAXObjectWrapper
                                            JAXObjectWrapper? obj = await GetObject(pos);
                                            if (obj is null)
                                                result = 1901;
                                            else
                                            {
                                                if (obj.Class.Equals("column", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    // Remove them both
                                                    RemoveObject(pos--);
                                                    grid.Columns.RemoveAt(grid.Columns.Count - 1);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Add more if needed
                                        while (newCount > grid.Columns.Count)
                                        {
                                            await AddColumn(0, string.Empty, string.Empty);
                                        }
                                    }
                                }
                                else
                                    result = 11;
                                break;

                            case "deletemark":
                                if (tk.Element.Type.Equals("L"))
                                {
                                    if (tk.AsBool())
                                    {
                                        if (UserProperties["recordsource"].AsBool() == false)
                                        {
                                            grid.RowHeaderWidth = 10;
                                            grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.All;
                                        }
                                    }
                                    else
                                    {
                                        //if (UserProperties["recordsource"].AsBool() == false)
                                        // grid.RowHeadersVisible = false;
                                    }
                                }
                                else
                                    result = 11;
                                break;

                            case "gridlinecolor":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                    clr = clr > 16843008 ? 16843008 : clr;
                                    objValue = clr;
                                    grid.HorizontalGridLinesBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(clr)));
                                    grid.VerticalGridLinesBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(clr)));
                                }
                                else
                                    result = 11;
                                break;

                            case "gridlinewidth":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    //grid.CellPainting -= dataGridView1_CellPainting;
                                    int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                    clr = clr > 16 ? 16 : clr;
                                    //grid.GridLineWidth = clr;
                                    objValue = clr;
                                    switch (clr)
                                    {
                                        case 0:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.None;
                                            break;
                                        case 1:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.Horizontal;
                                            break;
                                        default:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All;
                                            break;
                                    }
                                    grid.InvalidateVisual();
                                }
                                else
                                    result = 11;
                                break;

                            case "gridlines":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    //grid.CellPainting -= dataGridView1_CellPainting;
                                    int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                    clr = clr > 3 ? 3 : clr;
                                    objValue = clr;
                                    switch (clr)
                                    {
                                        case 0:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.None;
                                            break;
                                        case 1:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.Horizontal;
                                            break;
                                        case 2:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.Vertical;
                                            break;
                                        default:
                                            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All;
                                            break;
                                    }
                                    grid.InvalidateVisual();
                                }
                                else
                                    result = 11;
                                break;

                            case "headerheight":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    int clr = tk.AsInt();
                                    clr = clr < 0 ? 0 : clr;
                                    clr = clr > 256 ? 256 : clr;
                                    objValue = clr;
                                    grid.ColumnHeaderHeight = clr;
                                }
                                else
                                    result = 11;
                                break;

                            case "highlight":
                                //if (tk.Element.Type.Equals("L"))
                                //{
                                // if (tk.AsBool())
                                // {
                                // grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(UserProperties["backcolor"].AsInt());
                                // grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(UserProperties["forecolor"].AsInt());
                                // }
                                // else
                                // {
                                // grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(UserProperties["highlightbackcolor"].AsInt());
                                // grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(UserProperties["highlightforecolor"].AsInt());
                                // }
                                //}
                                //else
                                // result = 11;
                                break;

                            case "hilightbackcolor":
                                //if (tk.Element.Type.Equals("N"))
                                //{
                                // int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                // clr = clr > 16843008 ? 16843008 : clr;
                                // objValue = clr;
                                // grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(clr);
                                //}
                                //else
                                // result = 11;
                                break;

                            case "hilightforecolor":
                                //if (tk.Element.Type.Equals("N"))
                                //{
                                // int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                // clr = clr > 16843008 ? 16843008 : clr;
                                // objValue = clr;
                                // grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(clr);
                                //}
                                //else
                                // result = 11;
                                break;

                            case "highlightrow":
                                //if (tk.Element.Type.Equals("L"))
                                //{
                                // if (tk.AsBool())
                                // grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                                // else
                                // grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
                                //}
                                //else
                                // result = 11;
                                break;

                            case "readonly":
                                if (tk.Element.Type.Equals("L"))
                                {
                                    for (int i = 0; i < grid.Columns.Count; i++)
                                    {
                                        if (tk.AsBool())
                                            grid.Columns[i].IsReadOnly = true;  // Set columns to readonly
                                        else
                                        {
                                            // Put readonly back to column setting
                                            JAXObjects.Token obj = UserProperties["objects"];
                                            JAXObjectWrapper jow = (JAXObjectWrapper)obj._avalue[i].Value;
                                            JAXObjects.Token tok = await jow.GetProperty("readonly");
                                            if (tok.Element.Type.Equals("L"))
                                                grid.Columns[i].IsReadOnly = tok.AsBool();
                                        }
                                    }
                                }
                                break;

                            case "recordmark":
                                if (tk.Element.Type.Equals("L"))
                                {
                                    if (tk.AsBool())
                                    {
                                        grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.All; // show both
                                        grid.RowHeaderWidth = 43;
                                    }
                                    else
                                    {
                                        if (UserProperties["deletemark"].AsBool())
                                        {
                                            grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.All; // show both
                                            grid.RowHeaderWidth = 10;
                                        }
                                        else
                                        {
                                            grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.Column; // hide row headers (recommended)
                                        }
                                    }
                                }
                                else
                                    result = 11;
                                break;

                            case "recordsourcetype":
                                // If there is a record source, then bind the data
                                // using the value provided to bind a
                                // <0 - No record source
                                // 0 - Table
                                // 1 - Alias
                                // 2 - Prompt
                                // 3 - Qry File
                                // 4 - SQL Select
                                // 5 - Array
                                if (tk.Element.Type.Equals("N"))
                                {
                                    if (JAXLib.Between(tk.AsInt(), 0, 5))
                                    {
                                        if (string.IsNullOrWhiteSpace(UserProperties["recordsource"].AsString()) == false)
                                        {
                                            switch (tk.AsInt())
                                            {
                                                case 0:
                                                    // Open a table and bind it
                                                    string tableName = UserProperties["recordsource"].AsString().Trim();
                                                    if (tableName.Contains('&'))
                                                    {
                                                        // Macro expansion needed
                                                    }
                                                    if (result == 0 && tableName.Contains("("))
                                                    {
                                                        // It's definitely a function or variable
                                                        // so math the answer
                                                    }
                                                    if (result == 0)
                                                    {
                                                        // Look for the table in the current folder list
                                                        // If not found, then check for it if there is a current database
                                                    }
                                                    if (result == 0 && GridDBF is not null)
                                                    {
                                                        await PrepDataGrid();
                                                        //LoadDataIntoGrid();
                                                    }
                                                    break;

                                                case 1:
                                                    // Look for the alias and bind it
                                                    string alias = UserProperties["recordsource"].AsString();
                                                    if (alias.Contains("&"))
                                                    {
                                                    }
                                                    if (alias.Contains('('))
                                                    {
                                                        // Expression, so math it out
                                                    }
                                                    if (result == 0 && string.IsNullOrWhiteSpace(alias))
                                                        result = 11;
                                                    if (result == 0)
                                                    {
                                                        // We may have an alias so try to open it up
                                                        GridDBF = App.CurrentDS.GetWorkAreaObject(alias);
                                                        if (result == 0 && GridDBF is not null)
                                                        {
                                                            await PrepDataGrid();
                                                            //LoadDataIntoGrid();
                                                        }
                                                        else
                                                            result = 13;
                                                    }
                                                    break;

                                                case 2:
                                                    // Prompt for a record source (great for a generic browse window)
                                                    break;

                                                case 3:
                                                    // Load the Query file and execute it then
                                                    // bind the data to the grid
                                                    break;

                                                case 4:
                                                    // We should have a SQL statement
                                                    string SQLSelect = UserProperties["recordsource"].AsString().Trim();
                                                    if (SQLSelect.Contains('&'))
                                                    {
                                                        // Handle a macro expansion
                                                    }
                                                    if (result == 0 && SQLSelect.Contains(" ") == false)
                                                    {
                                                        // look for a sql statement in a variable
                                                        JAXObjects.Token sql = await AppVars.GetVarFromExpression(SQLSelect, null);
                                                        if (sql.Element.Type.Equals("C"))
                                                            SQLSelect = sql.AsString();
                                                        else
                                                            result = 11;
                                                    }
                                                    if (result == 0 && SQLSelect[..7].Equals("SELECT ", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        // Execute the sql select and bind that data
                                                    }
                                                    else
                                                        result = 11;
                                                    break;

                                                case 5:
                                                    // Try to bind the record source to an array name
                                                    aGridData = await AppVars.GetVarToken(UserProperties["recordsource"].AsString());

                                                    // Is it an array variable name?
                                                    if (aGridData.TType.Equals("A") == false)
                                                    {
                                                        // No, so reset and give error
                                                        result = 234;
                                                        aGridData = new();
                                                    }
                                                    else
                                                    {
                                                        // Load the grid from the array
                                                        LoadArrayIntoGrid();

                                                        // Gives the black screen of death
                                                        //for (int i = 0; i < grid.Columns.Count; i++)
                                                        //{
                                                        //    if (grid.Columns[i] is Avalonia.Controls.DataGridBoundColumn boundCol && boundCol.Binding.Path is null)
                                                        //    {
                                                        //        boundCol.Binding = new Avalonia.Data.Binding(i.ToString());
                                                        //    }
                                                        //}
                                                    }
                                                    break;
                                            }

                                            grid.InvalidateVisual();
                                        }
                                    }
                                    else
                                    {
                                        if (tk.AsInt() < 0)
                                        {
                                            ResetGridToBlank();
                                            UserProperties["recordsource"].Element.Value = string.Empty;
                                        }
                                        else
                                            result = 3003; // Value or index out of range
                                    }
                                }
                                else
                                    result = 11;
                                if (result == 0)
                                    grid.UpdateLayout();
                                break;

                            case "rowheight":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    int clr = tk.AsInt();
                                    clr = clr < 0 ? 0 : clr;
                                    clr = clr > 256 ? 256 : clr;
                                    grid.RowHeight = clr;
                                    objValue = clr;
                                }
                                else
                                    result = 11;
                                break;

                            case "scrollbars":
                                if (tk.Element.Type.Equals("N"))
                                {
                                    int sb = tk.AsInt();
                                    sb = sb < 0 ? 0 : sb;
                                    switch (sb)
                                    {
                                        case 0:
                                            grid.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
                                            grid.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
                                            break;
                                        case 1:
                                            grid.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            grid.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
                                            break;
                                        case 2:
                                            grid.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
                                            grid.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            break;
                                        default:
                                            sb = 3;
                                            grid.HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            grid.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Visible;
                                            break;
                                    }
                                    objValue = sb;
                                    grid.InvalidateVisual();
                                }
                                else
                                    result = 11;
                                break;

                            case "selecteditembackcolor":
                                //if (tk.Element.Type.Equals("N"))
                                //{
                                // int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                // clr = clr > 16843008 ? 16843008 : clr;
                                // objValue = clr;
                                // grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(clr);
                                //}
                                //else
                                // result = 11;
                                break;

                            case "selecteditemforecolor":
                                //if (tk.Element.Type.Equals("N"))
                                //{
                                // int clr = tk.AsInt() < 0 ? 0 : tk.AsInt();
                                // clr = clr > 16843008 ? 16843008 : clr;
                                // objValue = clr;
                                // grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(clr);
                                //}
                                //else
                                // result = 11;
                                break;

                            case "value":
                                isProgrammaticChange = true;
                                isProgrammaticChange = false;
                                break;

                            default:
                                // Process standard properties
                                result = await base.SetProperty(propertyName, objValue, objIdx);
                                if (propertyName.Equals("visible"))
                                {
                                    grid.UpdateLayout();
                                    grid.InvalidateVisual();
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
                    else
                        result = 1559;
                }
            }
            else
                result = 1559;

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
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
            if (UserProperties.ContainsKey(propertyName))
            {
                // Visual object common property handler
                switch (propertyName)
                {
                    case "controlcount":
                    case "columncount":
                        returnToken.Element.Value = grid.Columns.Count;
                        result = 9;
                        break;
                    default:
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 0, 10))
                {
                    if (result < 9)
                        returnToken = UserProperties[propertyName];

                    result = 0;
                }
            }
            else
                result = 1559;
            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

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
            return new string[]
            {
            "activatecell","addcolumn","addobject","autofit","addproperty","deletecolumn","doscroll",
            "gridhittest","move", "readexpression", "readmethod","refresh","removeobject","resettodefault",
            "saveasclass","setall","setfocus","writeexpression","writemethod","zorder"
            };
        }

        /*------------------------------------------------------------------------------------------*
         *
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return new string[]
            {
            "afterrowcolchange","beforerowcolchange","click","dblclick","deleted","destroy","error","errormessage",
            "init","keypress","load","middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove",
            "mouseup","mousewheel","moved","resize","rightclick","scrolled","uienable","valid","visiblechanged"
            };
        }

        /*------------------------------------------------------------------------------------------*
         * property data types
         * C = Character
         * N = Numeric I=Integer R=Color
         * D = Date
         * T = DateTime
         * L = Logical LY = Yes/No logical
         *
         * Attributes
         * ! Protected - can't change after initialization
         * $ Special Handling - do not auto process
         *
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return new string[]
            {
            "activecolumn,n!,0","activerow,n!,0","allowaddnew,l,","allowautocolumnfit,n,0","allowcellselection,l,.T.",
            "allowheadersizing,l,.T.","allowrowsizing,l,.T.","anchor,n,0",
            "backcolor,R,255|255|255","baseclass,c!,grid",
            "caption,c,","class,c!,grid","classlibrary,c!,","columncount,n,0","controlcount,n,0","comment,c,",
            "deletemark,l,",
            "enabled,l,.T.",
            "FontBold,L,false",
            "FontItalic,L,false","fontname,C,arial","FontSize,N,9",
            "FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0",
            "gridlinecolor,n,0","gridlinewidth,n,1","gridlines,n,3",
            "headerheight,n,19","Height,N,200","highlight,l,.T.","highlightbackcolor,R,0|120|215",
            "highlightforecolor,R,255|255|255","highlightrow,l,.T.",
            "lastcol,n!,-1","lastrow,n!,-1","left,n,0","leftcolumn,n,1",
            "mousepointer,n,0",
            "name,c,grid",
            "objects,*,",
            "parent,o!,","parentclass,c!,","partition,n,0",
            "readonly,l,","recordmark,l,.T.","recordsource,c,","recordsourcetype,n,1","righttoleft,l,",
            "rowcolchange,n!,0","rowheight,n,18",
            "scrollbars,n,3","selecteditembackcolor,R,0|120|215","selecteditemforecolor,R,255|255|255","splitbar,l,.T.",
            "tabindex,n,1","tabstop,l,.T.","tag,c,","top,n,0","tooltiptext,c,",
            "value,,","view,n,0","visible,l,.T.",
            "width,n,200"
            };
        }

        /*------------------------------------------------------------------------------------------*
         * SHOW has an override which selects the table in the current
         * work area (if one exists) if the recordsourcetype is set to -1.
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> DoDefault(string methodName)
        {
            int results = 0;
            string msg = string.Empty;
            methodName = methodName.ToLower();
            try
            {
                if (Methods.ContainsKey(methodName))
                {
                    string cCode = Methods[methodName].CompiledCode;
                    // Create a new App.Levels and execute the code
                    if (cCode.Length > 0)
                        results = await base._CallMethod(methodName);
                    else
                    {
                        switch (methodName)
                        {
                            case "autofit":
                                //grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                                //grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                                //if (grid.Columns.Count > 0)
                                //{
                                // // First make all columns size to content
                                // foreach (DataGridViewColumn col in grid.Columns)
                                // col.MinimumWidth = 35;
                                // // Then override the last column (or whichever you prefer) to fill
                                // grid.Columns[grid.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                                //}
                                break;
                            case "refresh":
                                grid.InvalidateVisual();
                                break;
                            default:
                                results = await base.DoDefault(methodName);
                                break;
                        }
                    }
                }
                else
                    results = 6501;
            }
            catch (Exception ex)
            {
                results = 9999;
                msg = ex.Message;
            }
            if (results > 0)
            {
                _AddError(results, 0, msg, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(results, $"{results}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                results = -1;
            }
            return results;
        }

        // ────────────────────────────────────────────────────────────────
        // Start of Array binding
        // ────────────────────────────────────────────────────────────────
        // Reference the array in UserProperties
        public JAXObjects.Token aGridData = new();

        public void ResetGridToBlank()
        {
            //AppIO.DebugLog("ResetGridToBlank", false);
            //grid.Rows.Clear(); // Clear observable collection
        }

        // --------------------------------------------------------------------------------
        // Load the array into the grid
        private void LoadArrayIntoGrid()
        {
            AppIO.DebugLog("LoadArrayIntoGrid");

            //_gridRows.Clear();
            //grid.Columns.Clear();
            // Get the number of columns for the grid
            int acc = aGridData.Col; // columns in the source array
            int arc = aGridData.Row; // rows in the source array

            // Fix for 1D array (treat as single-column, many rows)
            if (arc < 1)
            {
                arc = acc;
                acc = 1;
            }

            int col = UserProperties["columncount"].AsInt();
            if (col < 1)
            {
                UserProperties["columncount"].Element.Value = acc;
                col = acc;
            }

            // Row fix in FOR statement for 1D arrays
            for (int r = 1; r <= arc; r++)
            {
                var rowDict = new Dictionary<string, object>();

                // Optional: store original row index (replaces row.Tag = r)
                rowDict["_RowIndex"] = r;

                // Fill columns of this row
                for (int c = 1; c <= col; c++)
                {
                    string key = (c - 1).ToString();
                    if (c <= acc)
                    {
                        aGridData.SetElement(r, c);
                        rowDict[key] = aGridData.Element.Value ?? string.Empty;
                        AppIO.DebugLog($"Row: {r} Col:{key} - {rowDict[key]}");
                    }
                    else
                    {
                        rowDict[key] = string.Empty; // outside source array → blank
                        AppIO.DebugLog($"Row: {r} Col:{key} - Empty");
                    }
                }

                _gridRows.Add(rowDict);
            }

            // Avalonia equivalent of grid.AutoResizeColumns()
            if (UserProperties["allowautocolumnfit"].AsInt() == 0)
            {
                foreach (var column in grid.Columns)
                    column.Width = Avalonia.Controls.DataGridLength.Auto;
            }
        }

        /*
         * Start of event handling
         */
        private async void DgvMain_KeyPress(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            //AppIO.DebugLog("Grid Keypress", false);
            // VFP nKeyCode translation
            ParameterClass nKeyCode = new();
            if (App.OS == OSType.Windows)
            {
                nKeyCode.token.Element.Value = JAXLib.FormsVFPKeyPress(e.Key.ToString(), (int)e.Key);
                if (nKeyCode.token.AsInt() > 200) return; // Don't want modifier keys here
            }
            else
            {
                // TODO - Linux translation?
            }
            // Key modifiers converted for VFP
            int keymods = e.KeyModifiers == KeyModifiers.Shift ? 1 : 0;
            keymods += e.KeyModifiers == KeyModifiers.Control ? 2 : 0;
            keymods += e.KeyModifiers == KeyModifiers.Alt ? 4 : 0;
            ParameterClass nShiftAltCtrl = new();
            nShiftAltCtrl.token.Element.Value = keymods;
            App.ParameterClassList.Add(nKeyCode);
            App.ParameterClassList.Add(nShiftAltCtrl);
            await _CallMethod("keypress");
        }

        // Deleted: DgvMain_CellValidating - Replaced with more generic validation in column
        private async void DgvMain_BeforeCellChange(object? sender, EventArgs e)
        {
            //AppIO.DebugLog("BeforeCellChanged", false);
            ParameterClass colIndex = new();
            colIndex.token.Element.Value = 0;
            ParameterClass rowIndex = new();
            rowIndex.token.Element.Value = 0;
            ParameterClass cellValue = new();
            var (rowidx, colIdx, value) = GetSelectedCellInfo(grid);
            colIndex.token.Element.Value = colIdx + 1;
            rowIndex.token.Element.Value = rowidx + 1;
            if (value is not null)
                cellValue.token.Element.Value = value;
            else
                cellValue.token.Element.MakeNull();
            App.ParameterClassList.Add(rowIndex);
            App.ParameterClassList.Add(colIndex);
            App.ParameterClassList.Add(cellValue);
            await _CallMethod("beforerowcolchange");
        }

        // ────────────────────────────────────────────────
        // Emulate the AfterRowColChange event
        // ────────────────────────────────────────────────
        private async void DgvMain_AfterCellChanged(object? sender, EventArgs e)
        {
            //AppIO.DebugLog("AfterCellChanged", false);
            var (rowidx, colIdx, value) = GetSelectedCellInfo(grid);
            if (value is null)
            {
                //AppIO.DebugLog("Null skips AfterRowColChange logic", false);
            }
            else
            {
                // Update back to aGridData if recordsourcetype=5
                if (UserProperties["recordsourcetype"].AsInt() == 5)
                {
                    aGridData.SetElement(rowidx + 1, colIdx + 1);
                    aGridData.Element.Value = value;
                }

                App.ParameterClassList.Clear();
                AppHelper.LoadTokenValToParameters(new(colIdx + 1));
                AppHelper.LoadTokenValToParameters(new(rowidx + 1));
                AppHelper.LoadTokenValToParameters(new(value));
                //AppIO.DebugLog("Calling AfterRowColChange logic", false);

                await _CallMethod("afterrowcolchange");
                UserProperties["lastcol"].Element.Value = colIdx + 1;
                UserProperties["lastrow"].Element.Value = rowidx + 1;
            }
        }

        public void RefreshGrid()
        {
            //AppIO.DebugLog("Refresh grid", false);
            LoadArrayIntoGrid();
        }

        /***************************************************************
         * Start of Lazy grid data binding 
         * TODO - Needs extensive rewrite for Avalonia
         ***************************************************************/
        JAXDirectDBF? GridDBF = null;

        private async Task PrepDataGrid()
        {
            AppIO.DebugLog("PrepDataGrid", false);
            //grid.Rows.Clear();
            await SetProperty("columncount", 0, 0);

            if (GridDBF is not null)
            {
                // We have an active work area
                if (GridDBF.DbfInfo.DBFStream is not null)
                {
                    // We have an active table!
                    grid.IsReadOnly = true; // For viewing only
                    AllowUserToAddRows = false; // No new row at the end

                    // Add columns based on DBF schema (from empty DBFRow)
                    // Fetch an empty row to get the structure
                    DataTable JBrow = await GridDBF.DBFGotoRecord("TOP");

                    foreach (DataColumn col in JBrow.Columns)
                    {
                        if (col.ColumnName[..1] != "$") // Skip the deleted flag column if you don't want to show it
                        {
                            // Set the column and return the object location
                            int c = await AddColumn(0, col.ColumnName, col.ColumnName);
                        }
                    }
                }
            }
        }

        private void ApplyAutoSizeMode(int mode)
        {
            if (grid.Columns.Count == 0) return;
            switch (mode)
            {
                case 2:
                    // Fixed widths (None)
                    for (int i = 0; i < grid.Columns.Count; i++)
                        grid.Columns[i].Width = new Avalonia.Controls.DataGridLength(150 + i * 50, Avalonia.Controls.DataGridLengthUnitType.Pixel);
                    break;
                case 0:
                case 1:
                    // Size to content (AllCells / DisplayedCells)
                    foreach (var column in grid.Columns)
                        column.Width = Avalonia.Controls.DataGridLength.Auto;
                    break;
            }
            grid.InvalidateVisual(); // Ensure visual update
        }

        /// <summary>
        /// Returns the currently focused/selected cell info (Avalonia equivalent of DataGridView.CurrentCell)
        /// Works with the Dictionary-based ItemsSource used in your LoadArrayIntoGrid()
        /// </summary>
        public static (int RowIndex, int ColumnIndex, object? CellValue) GetSelectedCellInfo(Avalonia.Controls.DataGrid grid)
        {
            if (grid == null || grid.SelectedIndex < 0 || grid.CurrentColumn == null)
            {
                return (-1, -1, null);
            }
            int rowIndex = grid.SelectedIndex;
            int columnIndex = grid.Columns.IndexOf(grid.CurrentColumn);
            if (columnIndex < 0)
                return (rowIndex, -1, null);
            // Get value from the Dictionary row (exact match to your dynamic grid)
            if (grid.SelectedItem is Dictionary<string, object> rowDict)
            {
                string key = columnIndex.ToString();
                object? value = rowDict.ContainsKey(key) ? rowDict[key] : null;
                return (rowIndex, columnIndex, value);
            }
            // Fallback for strongly-typed objects (e.g. Person class)
            return (rowIndex, columnIndex, "Strongly-typed value");
        }

        /// <summary>
        /// Gets the value from any specific cell by row and column index
        /// (Avalonia equivalent of grid.Rows[rowIndex].Cells[colIndex].Value)
        /// Works with your Dictionary-based ItemsSource from LoadArrayIntoGrid()
        /// </summary>
        public static object? GetCellValue(Avalonia.Controls.DataGrid grid, int rowIndex, int columnIndex)
        {
            if (grid == null ||
            rowIndex < 0 ||
            columnIndex < 0 ||
            grid.ItemsSource is not ObservableCollection<Dictionary<string, object>> rows)
            {
                return null;
            }
            if (rowIndex >= rows.Count)
                return null;
            var rowDict = rows[rowIndex];
            string key = columnIndex.ToString();
            return rowDict.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Sets a value into the currently selected cell
        /// (Avalonia equivalent of grid.CurrentCell.Value = newValue)
        /// Works with the Dictionary-based ItemsSource from your LoadArrayIntoGrid()
        /// </summary>
        public static void SetSelectedCellValue(Avalonia.Controls.DataGrid grid, object? newValue)
        {
            if (grid == null || grid.SelectedIndex < 0 || grid.CurrentColumn == null)
                return;
            int rowIndex = grid.SelectedIndex;
            int colIndex = grid.Columns.IndexOf(grid.CurrentColumn);
            if (colIndex < 0)
                return;
            if (grid.ItemsSource is ObservableCollection<Dictionary<string, object>> rows &&
            rowIndex >= 0 && rowIndex < rows.Count)
            {
                var rowDict = rows[rowIndex];
                string key = colIndex.ToString();
                rowDict[key] = newValue ?? string.Empty;
                // IMPORTANT: re-assign the row to trigger ObservableCollection update
                // (Dictionary itself does not raise property changed events)
                rows[rowIndex] = rowDict;
            }
        }
    }
}