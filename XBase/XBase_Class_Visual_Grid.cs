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
 * 2026-07-14 - JLW
 *      Have sucessfully created the ROBrowser class with a readonly grid.  I'm going
 *      to take what I've learned and apply those lessons here.  I'll be pretty much
 *      wiping out what I've got and starting fresh.
 * 
 *      Phase 1 will once again be a read-only grid that fills everything into a simple 
 *      data table.  Record source will be table based.  
 * 
 *      Phase 2 will be tying in lists and arrays. This will require data source columns 
 *      to be consistent in typing.
 * 
 *      Phase 3 will be allowing updates to the grid but not to the underlying data structure.
 * 
 *      Phase 4 will be updating the underlying data structure
 * 
 *      Phase 5 will involve refreshing the grid with new data or added/subtracted rows
 *      
 *      Phase 6 will be trying to support a column of objects
 * 
 * 
 */
using AvaloniaEdit.Document;
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.UI.Dialogs;
using JAXBase.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using ZXing;
namespace JAXBase.XBase
{
    public class XBase_Class_Visual_Grid : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "Grid";
        public new string MyDefaultName { get; } = "grid";

        // Table for grid
        private DataTable? _jaxTable;
        private List<int> _rowRef = [];

        // Original datasession and work area
        int currentDS = 0;
        int currentWA = 0;

        // Current work area
        int thisWA = 0;
        JAXDirectDBF.DBFInfo? thisDBF = null;
        JAXDirectDBF? thisWorkArea = null;


        // Grid's current information
        bool GridCellInEdit = false;
        int GridCurrentRow = -1;
        int GridCurrentColumn = -1;


        // Define grid reference
        public Avalonia.Controls.DataGrid grid => (Avalonia.Controls.DataGrid)me.avaloniaObject!;

        bool doPostInitSetup = true;

        public XBase_Class_Visual_Grid(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            currentDS = Program.CurrentApp.CurrentDataSession;
            currentWA = Program.CurrentApp.CurrentDS.CurrentWorkArea();
            thisWA = 0;

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
                SetGridEvents();

            bool result = await base.PostInit(callBack, parameterList);
            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * Handle key presses
         * ------------------------------------------------------------------------------------------*/
        private void Grid_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (grid.CurrentColumn is null || grid.SelectedIndex < 0)
                return;

            int colIndex = grid.Columns.IndexOf(grid.CurrentColumn);
            int rowIndex = grid.SelectedIndex;

            // Skip the marker column (index 0)
            const int firstDataCol = 1;
            int lastDataCol = grid.Columns.Count - 1;

            if (e.Key == Avalonia.Input.Key.Tab)
            {
                e.Handled = true;

                if (e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift))
                {
                    // Shift+Tab → move left
                    if (colIndex > firstDataCol)
                    {
                        grid.CurrentColumn = grid.Columns[colIndex - 1];
                    }
                    else if (rowIndex > 0)
                    {
                        // wrap to previous row, last data column
                        grid.SelectedIndex = rowIndex - 1;
                        grid.CurrentColumn = grid.Columns[lastDataCol];
                    }
                }
                else
                {
                    // Tab → move right
                    if (colIndex < lastDataCol)
                    {
                        grid.CurrentColumn = grid.Columns[colIndex + 1];
                    }
                    else if (rowIndex < grid.Columns.Count - 1)
                    {
                        // wrap to next row, first data column
                        grid.SelectedIndex = rowIndex + 1;
                        grid.CurrentColumn = grid.Columns[firstDataCol];
                    }
                }
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Fires just before a cell exits edit mode (Enter, Tab, click-away, or Cancel).
        /// Cancelable – set e.Cancel = true to keep the cell in edit mode.
        /// </summary>
        private async void Grid_CellEditEnding(object? sender, Avalonia.Controls.DataGridCellEditEndingEventArgs e)
        {
            AppIO.DebugLog("Grid_CellEditEnding", false);

            if (e.EditAction != Avalonia.Controls.DataGridEditAction.Commit)
                return;

            int colIdx = grid.Columns.IndexOf(e.Column);
            int rowIdx = e.Row != null ? e.Row.Index : -1;

            if (colIdx < 0 || rowIdx < 0) return;


            // Find the matching column by looking at order
            JAXObjects.Token colCount = await me.GetProperty("columncount");

            JAXObjectWrapper? col = null;
            JAXObjects.Token? colNum = new(-1);

            for (int i = 0; i < colCount.AsInt(); i++)
            {
                col = await me.GetObject(i);

                if (col is not null)
                {
                    colNum = await col.GetProperty("columnnumber");
                    if (colNum.AsInt() == colIdx)
                        break;  // We've got the column!
                }
            }

            if (col is not null)
            {
                object? value = Grid_GetCellValue(grid, colIdx, rowIdx);

                if (value is not null)
                {
                    // Convert the value, if possible
                    string strValue = (string)value;

                    // Check the field type
                    string type = thisDBF!.Fields[colNum.AsInt()].FieldType;

                    // Now check the input value against the expected type
                    if (JAXLib.CheckStringValueType(strValue, type))
                    {
                        // We can convert the value
                        switch (type)
                        {
                            case "B":
                            case "F":
                            case "I":
                            case "N":
                            case "Y":
                                if (double.TryParse(strValue, out double d)) d = 0;
                                value = d;
                                break;

                            case "D":
                                value = DateOnly.FromDateTime(JAXLib.CtoT(strValue));
                                break;

                            case "T":
                                value = JAXLib.CtoT(strValue);
                                break;

                            default:
                                // String accepts anything
                                value = strValue;
                                break;
                        }

                        // Check to make sure it's a valid data type for the cell
                        if (_jaxTable is not null)
                        {
                            // Save to column in table
                        }
                        else
                        {
                            // TODO- Save to array position
                        }

                        if (e.Cancel == false)
                            // Save the value to the indicated field
                            Grid_SetSelectedCellValue(grid, value);
                    }
                    else
                    {
                        // Invalid data type
                        Toast.Show("Invalid Entry");
                        e.Cancel = true;
                    }
                }
                else
                {
                    // Cancel if invalid type
                    Toast.Show("Invalid Entry");
                    e.Cancel = true;
                }
            }
            else
            {
                AppIO.DebugLog($"Could not find column {colIdx}");
                Toast.Show($"Could not find column {colIdx}");
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Fires after a cell has exited edit mode. This is the event that tells you which cell just changed.
        /// </summary>
        private async void Grid_CellEditEnded(object? sender, Avalonia.Controls.DataGridCellEditEndedEventArgs e)
        {
            AppIO.DebugLog("Grid_CellEditEnded", false);

            if (e.EditAction != Avalonia.Controls.DataGridEditAction.Commit)
                return;

            int colIdx = grid.Columns.IndexOf(e.Column);
            int rowIdx = e.Row != null ? e.Row.Index : -1;

            // The first column (0) is the row marker and always exists (but may not be visible)
            if (colIdx < 1 || rowIdx < 0)
                return;

            object? newValue = Grid_GetCellValue(grid, rowIdx, colIdx);

            AppIO.DebugLog($"Cell changed – Row: {rowIdx + 1}, Col: {colIdx + 1}, Value: {newValue}");

            Program.CurrentApp.ParameterClassList.Clear();
            AppHelper.LoadTokenValToParameters(new(rowIdx + 1));   // nRow
            AppHelper.LoadTokenValToParameters(new(colIdx + 1));   // nCol
            AppHelper.LoadTokenValToParameters(new(newValue));     // uValue

            await _CallMethod("afterrowcolchange");
        }

        /* ------------------------------------------------------------------------------------------*
         * Cell movement causes an update to the grid and underlying table
         * ------------------------------------------------------------------------------------------*/
        private async void Grid_CurrentCellChanged(object? sender, EventArgs e)
        {
            // Ignore the marker column
            if (grid.CurrentColumn?.Tag is string tag && tag == "__MARKER__")
                return;

            if (grid.CurrentColumn is null || grid.SelectedIndex < 0)
                return;

            // Give the DataGrid a moment to finish its own selection painting
            await Task.Delay(1);

            int track = 1;

            // Grid Tracking
            try
            {
                // Table? Move to the correct row
                if (thisWorkArea is not null)
                    await thisWorkArea.DBFGotoRecord(grid.SelectedIndex);

                track++;

                // Get current row/column information from grid
                if (grid.SelectedItem is SimpleDataRow row)
                    GridCurrentRow = row.RowNumber;

                track++;

                if (grid.CurrentColumn.Tag is int colNo)
                    GridCurrentColumn = colNo;
            }
            catch (Exception ex)
            {
                AppIO.DebugLog($"Error in Grid Tracking section {track} : {ex.Message}");
            }

            // Set to editing visual state
            try
            {
                // Force the cell into the editing visual state
                // (same appearance you already get with Tab / Shift+Tab)
                if (grid.CurrentColumn is Avalonia.Controls.DataGridBoundColumn boundCol &&
                    !boundCol.IsReadOnly)
                {
                    grid.BeginEdit();
                }
                else
                {
                    // Read-only column – just make sure it has focus so the focus border appears
                    grid.Focus();
                }
            }
            catch
            {
                // Some templates do not support BeginEdit – ignore
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Returns the currently focused/selected cell info (Avalonia equivalent of DataGridView.CurrentCell)
        /// Works with the Dictionary-based ItemsSource used in your LoadArrayIntoGrid()
        /// </summary>
        public static (int RowIndex, int ColumnIndex, object? CellValue) Grid_GetSelectedCellInfo(Avalonia.Controls.DataGrid grid)
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

        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Gets the value from any specific cell by row and column index
        /// (Avalonia equivalent of grid.Rows[rowIndex].Cells[colIndex].Value)
        /// Works with your Dictionary-based ItemsSource from LoadArrayIntoGrid()
        /// </summary>
        public static object? Grid_GetCellValue(Avalonia.Controls.DataGrid grid, int rowIndex, int columnIndex)
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


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        /// <summary>
        /// Sets a value into the currently selected cell
        /// (Avalonia equivalent of grid.CurrentCell.Value = newValue)
        /// Works with the Dictionary-based ItemsSource from your LoadArrayIntoGrid()
        /// </summary>
        public static void Grid_SetSelectedCellValue(Avalonia.Controls.DataGrid grid, object? newValue)
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

        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        // Added: Handler for bubbled clicks (e.g., from button or link columns)
        private async void Grid_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
            {
                // If the click was on the marker column, force the current column away from it
                if (grid.CurrentColumn?.Tag is string tag && tag == "__MARKER__")
                {
                    // TODO - do anything that's supposed to happen here

                    // Move to column 1
                    if (grid.Columns.Count > 1)
                        grid.CurrentColumn = grid.Columns[1];
                }
                else
                {
                    // Are we in edit


                    // TODO - Need to call the click method of the current column
                    await _CallMethod("click");
                }
            }
        }


        /* ------------------------------------------------------------------------------------------*
         * ADDCOLUMN(x)
         * Add a column where x is a numeric value indicating the type of column 
         * to add. If columncount is 0 then it becomes Column1. Setting column 
         * count to a higher number will simply add columns after.
         *
         ** ------------------------------------------------------------------------------------------*/
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
            JAXObjectWrapper colObj = new JAXObjectWrapper(Program.CurrentApp, "column", colName, p);

            await colObj.SetProperty("columnnumber", grid.Columns.Count);
            UserProperties["objects"].Add(colObj);

            // Add the created column from the wrapper
            Avalonia.Controls.DataGridColumn col = (Avalonia.Controls.DataGridColumn)colObj.nvObject!;

            if (col is Avalonia.Controls.DataGridBoundColumn boundCol && !string.IsNullOrEmpty(binding))
            {
                boundCol.Binding = new Avalonia.Data.Binding(binding);
                AppIO.DebugLog($"Column bound to {binding}");
            }

            col.IsReadOnly = UserProperties["readonly"].AsBool();
            grid.Columns.Add(col);

            // Force the header AFTER Binding and after the column is in the grid
            JAXObjectWrapper? h = await colObj!.thisObject!.GetObject(0);
            if (h is not null)
            {
                JAXObjects.Token tk = await h.GetProperty("caption", 0);
                string captionToUse = tk.AsString();
                captionToUse = string.IsNullOrEmpty(header) ? captionToUse : JAXLib.Proper(header);
                await h.SetProperty("caption", captionToUse, 0);   // this calls SetHeader again
            }

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


        /* ------------------------------------------------------------------------------------------*
         * Manually add a new column to the formj - grid does not change
         * until the RowSource changes or it's already rendered
         * ------------------------------------------------------------------------------------------*/
        public override async Task<int> AddObject(JAXObjectWrapper value)
        {
            int err = 1903;

            // You can only add columns to a grid's objects list
            if (value.nvObject is not null && value.nvObject is Avalonia.Controls.DataGridColumn)
            {
                await value.SetProperty("columnnumber", grid.Columns.Count);

                // If everything is ok, add it to the Objects array
                UserProperties["objects"].Add(value);

                // Make sure controlcount stays up to date
                if (UserProperties["controlcount"].AsInt() < grid.Columns.Count)
                    UserProperties["controlcount"].Element.Value = grid.Columns.Count;

                // Make sure columncount stays up to date
                if (UserProperties["columncount"].AsInt() < grid.Columns.Count)
                    UserProperties["columncount"].Element.Value = grid.Columns.Count;

                value.SetParent(me);

                // If the grid is already rendered, then set it up again
                // if (PrivateProperties["rendered"].AsBool())
                //     SetUpGrid();

                err = 0;
            }

            if (err > 0)
            {
                // Something went wrong
                _AddError(err, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(err, $"{err}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
            }

            return err > 0 ? -1 : grid.Columns.Count;
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        public override async Task<int> _CallMethod(string methodName)
        {
            int results = 0;
            string msg = string.Empty;
            methodName = methodName.ToLower();
            Program.CurrentApp.ReturnValue.Element.Value = true;
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
                                if (Program.CurrentApp.ParameterClassList.Count > 1)
                                    results = 94;
                                else
                                {
                                    JAXObjects.Token tk = new();
                                    if (Program.CurrentApp.ParameterClassList.Count == 1)
                                    {
                                        object? obj = AppHelper.GetParameterValue(Program.CurrentApp.ParameterClassList[0]);
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
                _AddError(results, 0, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(results, $"{results}|{msg}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                Program.CurrentApp.ReturnValue.Element.Value = false;
                results = -1;
            }
            return results;
        }


        /* ------------------------------------------------------------------------------------------*
         * SHOW has an override which selects the table in the current
         * work area (if one exists) if the recordsourcetype is set to -1.
         * ------------------------------------------------------------------------------------------*/
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
                _AddError(results, 0, msg, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(results, $"{results}|{msg}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                results = -1;
            }
            return results;
        }



        /* ------------------------------------------------------------------------------------------*
         * GetProperty method returns
         * 0 = Successfully returning value
         * -1 = Error code
         * ------------------------------------------------------------------------------------------*/
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        // Add this in when something needs to be done after being rendered onto the form
        public override void PostRender()
        {
            SetGridEvents();
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        public void SetGridEvents()
        {
            AppIO.DebugLog("Setting grid events");
            SetEvents();

            // Added: Subscribe to grid events for bubbling from columns
            // Cell edit completion (fires when user leaves the cell or presses Enter)
            grid.CellEditEnding += Grid_CellEditEnding;
            grid.CellEditEnded += Grid_CellEditEnded;

            grid.CurrentCellChanged += Grid_CurrentCellChanged;
            grid.KeyDown += Grid_KeyDown;

            // Added: General click handler for bubbled events (e.g., from button/link columns)
            grid.PointerPressed += Grid_PointerPressed;
            doPostInitSetup = false;

            // Prevent the marker column from ever becoming CurrentColumn
            grid.CurrentCellChanged += (s, e) =>
            {
                if (grid.CurrentColumn != null &&
                    grid.CurrentColumn.Tag is string tag &&
                    tag == "__MARKER__")
                {
                    // Move focus to the first real data column
                    if (grid.Columns.Count > 1)
                        grid.CurrentColumn = grid.Columns[1];
                }
            };

            // Block beginning an edit on the marker column
            grid.BeginningEdit += (s, e) =>
            {
                if (e.Column?.Tag is string tag && tag == "__MARKER__")
                    e.Cancel = true;
            };


            Avalonia.Controls.Canvas.SetLeft(grid, UserProperties["left"].AsInt());
            Avalonia.Controls.Canvas.SetTop(grid, UserProperties["top"].AsInt());
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        private void ApplyAutoSizeMode(int mode)
        {
            if (grid == null || grid.Columns.Count == 0)
                return;

            AppIO.DebugLog($"ApplyAutoSizeMode: {mode} while on parent '{me.Parent?.JOWName}'");

            switch (mode)
            {
                case 2: // Fixed widths (fallback)
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        grid.Columns[i].Width = new Avalonia.Controls.DataGridLength(150, Avalonia.Controls.DataGridLengthUnitType.Pixel);
                    }
                    break;

                default:    // Auto / Size to content
                    foreach (var column in grid.Columns)
                    {
                        if (column is Avalonia.Controls.DataGridBoundColumn boundColumn)
                        {
                            boundColumn.Width = Avalonia.Controls.DataGridLength.Auto;  // Avalonia auto-sizing
                        }
                        else
                        {
                            column.Width = Avalonia.Controls.DataGridLength.Auto;
                        }
                    }
                    break;
            }

            grid.InvalidateVisual();
            grid.InvalidateMeasure();
            grid.InvalidateArrange();
        }

        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        // Call this after data binding or column changes
        public async Task AutoFitColumns()
        {
            if (grid == null)
                return;

            ApplyAutoSizeMode(UserProperties["allowautocolumnfit"].AsInt());
            await Task.Delay(50); // Small delay for layout pass
            grid.UpdateLayout();
        }


        /* ------------------------------------------------------------------------------------------*
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
         * ------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            string msg = "";

            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;
            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                {

                    result = 3026;
                }
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
                                            AppIO.DebugLog($"Setting recordsourcetype to {tk.AsInt()}");

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
                                                    if (result == 0)
                                                    {
                                                        if (Program.CurrentApp.CurrentDS.IsWorkArea(tableName))
                                                        {
                                                            Program.CurrentApp.CurrentDS.SelectWorkArea(tableName);
                                                            result = await SetTableBinding();
                                                            await AutoFitColumns();
                                                        }
                                                        else
                                                        {
                                                            result = 13;
                                                            msg = tableName;
                                                        }

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
                                                        if (result == 0)
                                                        {
                                                            if (Program.CurrentApp.CurrentDS.IsWorkArea(alias))
                                                            {
                                                                Program.CurrentApp.CurrentDS.SelectWorkArea(alias);
                                                                result = await SetTableBinding();
                                                                await AutoFitColumns();
                                                            }
                                                            else
                                                            {
                                                                result = 13;
                                                                msg = alias;
                                                            }
                                                        }
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
                                                    JAXObjects.Token aGridData = await AppVars.GetVarToken(UserProperties["recordsource"].AsString());

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
                                                        await SetArrayBinding(aGridData);

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
                                            //ResetGridToBlank();
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
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                result = -1;
            }
            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        // Build a grid based on the current Objects[]
        private async Task<int> SetGridBinding()
        {
            int result = 0;

            // Prep the table
            _jaxTable = new();
            _rowRef = [];

            int ColCount = UserProperties["columncount"].AsInt();

            for (int i = 1; i <= ColCount; i++)
            {
                JAXObjects.Token col = await GetProperty("userobjects", i - 1);


            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        // Bind an array to the grid by putting it to a table of string based text columns.
        // Handling of values occurs in the browse/edit code.
        private async Task<int> SetArrayBinding(JAXObjects.Token aGrid)
        {
            int result = 0;

            if (false) SetGridBinding().Wait();

            // Prep the table
            _jaxTable = new();
            _rowRef = [];

            // Set up the columns
            for (int i = 1; i <= aGrid.Col; i++)
            {
                _jaxTable.Columns.Add(JAXLib.Proper($"Column{i}"), typeof(string));
            }

            for (int row = 1; row <= aGrid.Row; row++)
            {
                // Add a new row
                _jaxTable.Rows.Add();
                _rowRef.Add(row);

                // Populate it from the source table
                for (int col = 1; col <= aGrid.Col; col++)
                {
                    // Copy each column in _jaxTable over
                    string name = _jaxTable.Columns[col - 1].ColumnName;
                    aGrid.SetElement(row, col);
                    _jaxTable.Rows[^1][name] = aGrid.AsString().Trim();
                }
            }

            // Now call the grid setup
            SetGridFinal();

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * Set the dbf/cursor up with the grid.
         * 
         * If there are already columns defined, we use the existing definitions
         * If columncount=0, we use the entire table's non-system fields
         * 
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> SetTableBinding()
        {
            int result = 0;

            // Populate JAXTABLE with sample columns and rows
            string alias = UserProperties["recordsource"].AsString();

            // We may need the original DS/WA later
            int currentDS = Program.CurrentApp.CurrentDataSession;
            int currentWA = Program.CurrentApp.CurrentDS.CurrentWorkArea();
            int thisWA = 0;

            // Get the requested WA.  If alias is empty then they want current WA
            thisWA = string.IsNullOrWhiteSpace(alias) ? currentWA : Program.CurrentApp.CurrentDS.GetWorkArea(alias);

            // Go to the alias of choice
            JAXDataSession thisDS = Program.CurrentApp.CurrentDS;
            Program.CurrentApp.CurrentDS.SelectWorkArea(currentWA);

            thisDBF = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo;
            thisWorkArea = Program.CurrentApp.CurrentDS.CurrentWA;

            if (thisDBF.DBFStream is null)
                throw new Exception(string.IsNullOrWhiteSpace(alias) ? "52|" : $"13|{alias}");

            // Reset the objects and columncount properties
            await SetProperty("columncount", 0, 0);

            // Prep the table
            _jaxTable = new();
            _rowRef = [];

            // Set up the columns
            for (int i = 0; i < thisDBF.FieldCount; i++)
            {
                JAXTables.FieldInfo fld = thisDBF.Fields[i];

                if (fld.SystemColumn == false)
                {
                    System.Type type = fld.FieldType switch
                    {
                        "L" => typeof(bool),
                        "N" => typeof(double),
                        "B" => typeof(double),
                        "F" => typeof(float),
                        "Y" => typeof(decimal),
                        "I" => typeof(int),
                        "D" => typeof(DateOnly),
                        "T" => typeof(DateTime),
                        _ => typeof(string),
                    };

                    _jaxTable.Columns.Add(JAXLib.Proper(fld.FieldName), type);

                    // Add the JAXBase column object type 0 with
                    // header (2nd param) and field name for
                    // binding to the column's text box
                    await AddColumn(0, fld.FieldName, fld.FieldName);
                }
            }

            // Add all rows to grid table
            // All filtering is handled behind the scenes for DELETED and FILTER settings.
            await thisWorkArea.DBFGotoRecord("top");

            for (int row = 1; row <= thisDBF.RecCount; row++)
            {
                // Add a new row
                _jaxTable.Rows.Add();
                _rowRef.Add(thisDBF.CurrentRecNo);

                // Populate it from the source table
                for (int col = 0; col < _jaxTable.Columns.Count; col++)
                {
                    // Copy each column in _jaxTable over
                    string name = _jaxTable.Columns[col].ColumnName;
                    _jaxTable.Rows[^1][name] = thisDBF.CurrentRow.Rows[0][name];
                }

                // Now mark if it's deleted
                if (thisDBF.currentRowIsDeleted)
                    _jaxTable.Rows[^1].Delete();

                // Skip to the next record
                await thisWorkArea.DBFSkipRecord(1);
            }

            SetGridFinal();

            return result;
        }


        /* ------------------------------------------------------------------------------------------*
         * ------------------------------------------------------------------------------------------*/
        private void SetGridFinal()
        {
            // Build the row list
            var rowList = new System.Collections.ObjectModel.ObservableCollection<SimpleDataRow>();
            int rowNo = 0;

            foreach (System.Data.DataRow row in _jaxTable!.Rows)
            {
                var simpleRow = new SimpleDataRow();
                foreach (System.Data.DataColumn col in _jaxTable.Columns)
                    simpleRow.Values[col.ColumnName] = row[col] ?? DBNull.Value;

                // Keep track of row number and deletion value
                simpleRow.RowNumber = ++rowNo;
                simpleRow.Deleted = row.RowState == System.Data.DataRowState.Deleted;

                rowList.Add(simpleRow);
            }

            // ------------------------------------------------------------------
            // VFP-style record-mark column – ensure it exists exactly once
            // ------------------------------------------------------------------
            Avalonia.Controls.DataGridTextColumn? markerCol = null;

            // Look for an existing marker column (we tagged it with a special Header)
            foreach (var c in grid.Columns)
            {
                if (c is Avalonia.Controls.DataGridTextColumn tc && tc.Tag is string tag && tag == "__MARKER__")
                {
                    markerCol = tc;
                    break;
                }
            }

            if (markerCol is null)
            {
                markerCol = new Avalonia.Controls.DataGridTextColumn
                {
                    Header = "",
                    Tag = "__MARKER__",
                    Width = new Avalonia.Controls.DataGridLength(28),
                    IsReadOnly = true,
                    CanUserResize = false,
                    CanUserReorder = false,
                    CanUserSort = false,
                    // Prevent the column from ever becoming the current cell
                    IsVisible = true,
                    Binding = new Avalonia.Data.Binding("Values")
                    {
                        Converter = new DictionaryValueConverter(),
                        ConverterParameter = "__marker__"
                    }
                };
                grid.Columns.Insert(0, markerCol);
            }

            // Keep track of original column number
            for (int i = 1; i < grid.Columns.Count; i++)
            {
                grid.Columns[i].Tag = i;
            }


            // ------------------------------------------------------------------
            // Re-bind the real data columns
            // ------------------------------------------------------------------
            var converter = new DictionaryValueConverter();
            int colIdx = 0;

            foreach (var col in grid.Columns)
            {
                // Skip the marker column
                if (col == markerCol)
                    continue;

                if (col is Avalonia.Controls.DataGridBoundColumn boundCol)
                {
                    string fieldName = _jaxTable.Columns[colIdx].ColumnName;

                    boundCol.Binding = new Avalonia.Data.Binding("Values")
                    {
                        Converter = converter,
                        ConverterParameter = fieldName
                    };
                }
                colIdx++;
            }

            // ------------------------------------------------------------------
            // Visual appearance
            // ------------------------------------------------------------------
            grid.GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All;
            grid.BorderThickness = new Avalonia.Thickness(1);
            grid.BorderBrush = Avalonia.Media.Brushes.Gray;
            grid.HorizontalGridLinesBrush = Avalonia.Media.Brushes.LightGray;
            grid.VerticalGridLinesBrush = Avalonia.Media.Brushes.LightGray;

            // Kill the native Avalonia row-header strip
            grid.HeadersVisibility = Avalonia.Controls.DataGridHeadersVisibility.Column; // column headers only
            grid.RowHeaderWidth = 4;   // 1 is the smallest value that does not throw

            // ------------------------------------------------------------------
            // Data
            // ------------------------------------------------------------------
            grid.ItemsSource = null;
            grid.ItemsSource = rowList;

            grid.InvalidateVisual();
            grid.InvalidateMeasure();
            grid.InvalidateArrange();

            // Debug

            // debug block …
            AppIO.DebugLog("========= DataGrid Debug =========");
            AppIO.DebugLog($"ItemsSource Type: {grid.ItemsSource?.GetType().FullName}");
            AppIO.DebugLog($"Row Count in Table: {_jaxTable.Rows.Count}");
            AppIO.DebugLog($"Column Count: {_jaxTable.Columns.Count}");
            foreach (System.Data.DataColumn c in _jaxTable.Columns)
                AppIO.DebugLog($"Column: {c.ColumnName} ({c.DataType.Name})");
        }

        private void SetGridFinal2()
        {
            // Create simple POCO rows for reliable binding
            var rowList = new System.Collections.ObjectModel.ObservableCollection<SimpleDataRow>();

            foreach (System.Data.DataRow row in _jaxTable!.Rows)
            {
                var simpleRow = new SimpleDataRow();
                foreach (System.Data.DataColumn col in _jaxTable.Columns)
                {
                    simpleRow.Values[col.ColumnName] = row[col] ?? DBNull.Value;
                }
                rowList.Add(simpleRow);
            }



            // 1. Capture the current parent (if any) and the old visual
            Avalonia.Controls.Control? oldVisual = me.avaloniaObject;
            Avalonia.Controls.Canvas? parentCanvas = null;

            if (oldVisual?.Parent is Avalonia.Controls.Canvas canvas)
            {
                parentCanvas = canvas;
                parentCanvas.Children.Remove(oldVisual);
            }

            // 2. Create the new DataGrid
            Avalonia.Controls.DataGrid newGrid = new Avalonia.Controls.DataGrid
            {
                Name = "JAXDataGrid",
                GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All,
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = Avalonia.Media.Brushes.Gray,
                AutoGenerateColumns = false,
                CanUserResizeColumns = true,
                CanUserReorderColumns = true,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };

            // 3. Point the JAX object at the new visual
            me.avaloniaObject = newGrid;

            // Explicit columns using dictionary lookup
            grid.Columns.Clear();
            var testColumn = new Avalonia.Controls.DataGridTextColumn
            {
                Header = "All Data",
                Binding = new Avalonia.Data.Binding(".")   // binds to ToString()
            };

            grid.Columns.Add(testColumn);
            grid.ItemsSource = rowList;
            grid.LoadingRow += (sender, e) =>
            {
                // This can help force rendering
            };

            AppIO.DebugLog($"RowList Count: {rowList.Count}");

            // Force column regeneration and refresh
            grid.AutoGenerateColumns = false;

            // Bind the list
            grid.ItemsSource = rowList;

            AppIO.DebugLog($"RowList Count: {rowList.Count}");

            // Explicit columns using DictionaryValueConverter (add the converter class first)
            grid.Columns.Clear();
            var converter = new DictionaryValueConverter();

            foreach (System.Data.DataColumn dataCol in _jaxTable.Columns)
            {
                var textColumn = new Avalonia.Controls.DataGridTextColumn
                {
                    Header = dataCol.ColumnName,
                    Binding = new Avalonia.Data.Binding("Values")
                    {
                        Converter = converter,
                        ConverterParameter = dataCol.ColumnName
                    }
                };
                grid.Columns.Add(textColumn);
            }

            // 4. Restore the absolute position and size that the rest of the system expects
            Avalonia.Controls.Canvas.SetLeft(grid, UserProperties["left"].AsDouble());
            Avalonia.Controls.Canvas.SetTop(grid, UserProperties["top"].AsDouble());
            grid.Width = UserProperties["width"].AsDouble();
            grid.Height = UserProperties["height"].AsDouble();

            // 5. Put the new control back into the same parent canvas
            if (parentCanvas is not null)
            {
                parentCanvas.Children.Add(newGrid);
            }
            else
            {
                // Fallback – should rarely happen after the form has been shown
                // (you can also walk me.Parent to locate the correct canvas)
                AppIO.DebugLog("No parent canvas found when recreating grid");
            }

            // 6. Re-wire any events that were attached to the old instance
            SetGridEvents();

            grid.InvalidateVisual();
            grid.InvalidateMeasure();
            grid.InvalidateArrange();

            if (parentCanvas is not null)
            {
                parentCanvas.InvalidateVisual();
                parentCanvas.InvalidateMeasure();
                parentCanvas.InvalidateArrange();
            }

            // === Debug block ===
            AppIO.DebugLog($"========= DataGrid Debug =========");
            AppIO.DebugLog($"ItemsSource Type: {grid.ItemsSource?.GetType().FullName}");
            AppIO.DebugLog($"Row Count in Table: {_jaxTable.Rows.Count}");
            AppIO.DebugLog($"Column Count: {_jaxTable.Columns.Count}");

            foreach (System.Data.DataColumn col in _jaxTable.Columns)
            {
                AppIO.DebugLog($"Column: {col.ColumnName} ({col.DataType.Name})");
            }

            SuspendEvents();
        }


        /* ------------------------------------------------------------------------------------------*
         *
         * ------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return
            [
            "activatecell","addcolumn","addobject","autofit","addproperty","deletecolumn","doscroll",
            "gridhittest","move", "readexpression", "readmethod","refresh","removeobject","resettodefault",
            "saveasclass","setall","setfocus","writeexpression","writemethod","zorder"
            ];
        }

        /* ------------------------------------------------------------------------------------------*
         *
         * ------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
            [
            "afterrowcolchange","beforerowcolchange","click","dblclick","deleted","destroy","error","errormessage","gotfocus",
            "init","keypress","load","lostfocus","middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove",
            "mouseup","mousewheel","moved","resize","rightclick","scrolled","uienable","valid","visiblechanged"
            ];
        }

        /* ------------------------------------------------------------------------------------------*
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
         * ------------------------------------------------------------------------------------------*/
        public override string[] JAXProperties()
        {
            return
            [
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
            ];
        }


        /* ------------------------------------------------------------------------------------------*
         * Classes used to make the grid work
         * ------------------------------------------------------------------------------------------*/
        private class SimpleDataRow
        {
            public System.Collections.Generic.Dictionary<string, object> Values { get; } = [];

            // Extra data that the grid never sees
            public string Tag { get; set; } = string.Empty;
            public int RowNumber { get; set; } = 0;
            public bool Deleted { get; set; } = false;

            public override string ToString()
            {
                return string.Join(" | ", Values.Values);
            }
        }


        private class DictionaryValueConverter : Avalonia.Data.Converters.IValueConverter
        {
            public object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            {
                if (value is System.Collections.Generic.Dictionary<string, object> dict && parameter is string key)
                {
                    return dict.TryGetValue(key, out var val) ? val : null;
                }
                return value?.ToString();
            }

            public object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture)
            {
                return value;  // Simple passthrough for read-only grid
            }
        }
    }
}