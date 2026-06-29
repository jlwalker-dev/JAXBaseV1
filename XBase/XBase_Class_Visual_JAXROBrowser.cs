/*
 * This class is a sub class of XBase\XBase_Class_Visual_Form.cs 
 * The form class uses UI\FakeWindow.cs to create a standalone window, a window in the IDE, or a window in a window.
 * 
 * Because most of the class logic was designed before switching to Avalonia, some issues arose in that Avalonia
 * performs it's rendering at a different point than WinForms.  This tends to cause problems with making form based
 * compound classes.
 * 
 * Further, unlike WinForms, Avalonia does not really care for data alterations after setup.  It's possible, but so 
 * far it's turning out to be a royal pain.
 * 
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using System.Data;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_JAXROBrowser : XBase_Class_Visual_Form
    {

        private DataTable? _jaxTable;
        private Avalonia.Controls.DataGrid? _dataGrid;

        public new string MyBaseClass = "ROBrowser";
        public new string MyDefaultName = "robrowser";

        public XBase_Class_Visual_JAXROBrowser(JAXObjectWrapper jow, string name) : base(jow, "ROBrowser") { }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);

            // Populate JAXTABLE with sample columns and rows
            string alias = UserProperties["alias"].AsString();

            int currentDS = Program.CurrentApp.CurrentDataSession;
            int currentWA = Program.CurrentApp.CurrentDS.CurrentWorkArea();
            int thisWA = 0;

            if (string.IsNullOrWhiteSpace(alias))
            {
                // Is there a table open in the current workarea?
                thisWA = currentWA;
            }
            else
            {
                // Try to access this alias
                thisWA = Program.CurrentApp.CurrentDS.GetWorkArea(alias);
            }

            // Go to the alias of choice
            JAXDataSession thisDS = Program.CurrentApp.CurrentDS;
            Program.CurrentApp.CurrentDS.SelectWorkArea(currentWA);

            JAXDirectDBF.DBFInfo dbfInfo = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo;
            JAXDirectDBF thisWorkArea = Program.CurrentApp.CurrentDS.CurrentWA;

            if (dbfInfo.DBFStream is null)
                throw new Exception(string.IsNullOrWhiteSpace(alias) ? "52|" : $"13|{alias}");

            // Prep the table
            _jaxTable = new();

            // Set up the columns
            for (int i = 0; i < dbfInfo.FieldCount; i++)
            {
                JAXTables.FieldInfo fld = dbfInfo.Fields[i];

                if (fld.SystemColumn == false)
                {
                    System.Type type = fld.FieldType switch
                    {
                        "L" => typeof(bool),
                        "N" => typeof(double),
                        "Y" => typeof(decimal),
                        "I" => typeof(int),
                        "D" => typeof(DateOnly),
                        "T" => typeof(DateTime),
                        _ => typeof(string),
                    };

                    _jaxTable.Columns.Add(JAXLib.Proper(fld.FieldName), type);
                }
            }

            // Add sample rows (records)
            await thisWorkArea.DBFGotoRecord("top");

            for (int row = 1; row <= dbfInfo.RecCount; row++)
            {
                // TODO - Set deleted support
                if (dbfInfo.currentRowIsDeleted == false || thisDS.JaxSettings.Deleted == false)
                {
                    // Add a new row
                    _jaxTable.Rows.Add();

                    // Populate it from the source table
                    for (int col = 0; col < _jaxTable.Columns.Count; col++)
                    {
                        // Copy each column in _jaxTable over
                        string name = _jaxTable.Columns[col].ColumnName;
                        _jaxTable.Rows[^1][name] = dbfInfo.CurrentRow.Rows[0][name];
                    }
                }

                // Skip to the next record
                await thisWorkArea.DBFSkipRecord(1);
            }


            // Create the read-only DataGrid
            _dataGrid = new Avalonia.Controls.DataGrid
            {
                Name = "JAXDataGrid",
                IsReadOnly = true,
                GridLinesVisibility = Avalonia.Controls.DataGridGridLinesVisibility.All,
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = Avalonia.Media.Brushes.Gray,
                AutoGenerateColumns = true,
                CanUserResizeColumns = true,
                CanUserReorderColumns = true,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                MinWidth = 1200
            };

            // Bind to the DataTable
            _dataGrid.ItemsSource = _jaxTable.DefaultView;

            // Force column regeneration and refresh
            _dataGrid.AutoGenerateColumns = false;
            _dataGrid.Columns.Clear();
            _dataGrid.AutoGenerateColumns = true;

            _dataGrid.InvalidateVisual();
            _dataGrid.InvalidateMeasure();
            _dataGrid.InvalidateArrange();

            // Additional layout refresh for Canvas scenario
            if (InnerCanvas != null)
            {
                InnerCanvas.InvalidateVisual();
                InnerCanvas.InvalidateMeasure();
                InnerCanvas.InvalidateArrange();
            }

            // === Debug block ===
            AppIO.DebugLog($"========= DataGrid Debug =========");
            AppIO.DebugLog($"ItemsSource Type: {_dataGrid.ItemsSource?.GetType().FullName}");
            AppIO.DebugLog($"Row Count in Table: {_jaxTable.Rows.Count}");
            AppIO.DebugLog($"Column Count: {_jaxTable.Columns.Count}");

            foreach (System.Data.DataColumn col in _jaxTable.Columns)
            {
                AppIO.DebugLog($"Column: {col.ColumnName} ({col.DataType.Name})");
            }

            // Layout
            Avalonia.Controls.Grid mainGrid = new();
            mainGrid.Children.Add(_dataGrid);

            Avalonia.Controls.Canvas.SetLeft(mainGrid, double.NaN);
            Avalonia.Controls.Canvas.SetTop(mainGrid, double.NaN);

            mainGrid.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            mainGrid.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (InnerCanvas is not null)
                InnerCanvas.Children.Add(mainGrid);
            else
                throw new Exception("9999|");

            SuspendEvents();

            return true;
        }

        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token objtk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "alias":
                            if (objtk.Element.Type != "C")
                                result = 11;
                            break;

                        case "autohidescrollbar":
                            if (objtk.Element.Type.Equals("L") == false)
                                result = 11;
                            break;

                        case "backcolor":
                            int colorInt = JAXUtilities.ReturnColorInt(objtk.AsString());
                            //_textEditor.Background = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(colorInt));
                            objValue = colorInt;
                            break;

                        case "borderstyle":
                            if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 3))
                                fakeWindow.BorderStyle = objtk.AsInt();
                            else
                                result = objtk.Element.Type != "N" ? 11 : 41;
                            break;

                        case "caption":
                            fakeWindow.Title = objtk.AsString();
                            break;

                        case "editortype":
                            break;

                        case "filename":
                            break;

                        case "filepath":
                            break;

                        case "fontname":
                            //_textEditor.FontFamily = objtk.AsString();
                            //_textEditor.FontFamily ??= "Segoe UI";
                            //_textEditor.FontFamily ??= "Arial";
                            //_textEditor.FontFamily ??= "Hevelica";
                            break;

                        case "fontsize":
                            //_textEditor.FontSize = objtk.AsDouble() / 72 * 96;
                            break;

                        case "forecolor":
                            //_textEditor.Foreground = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor(JAXUtilities.ReturnColorInt(objtk.AsString())));
                            objValue = JAXUtilities.ReturnColorInt(objtk.AsString());
                            break;

                        case "fontbold":
                            //_textEditor.FontWeight = objtk.AsBool() ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal;
                            break;

                        case "fontitalic":
                            //_textEditor.FontStyle = objtk.AsBool() ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal;
                            break;

                        case "height":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Height = objtk.AsDouble() + HeightDelta;
                                objValue = objtk.AsDouble();
                                me.originalHeight = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "icon":
                            if (objtk.Element.Type.Equals("C"))
                            {
                                if (JAXApp.MainWindowInstance is not null)
                                {
                                    // set up the image and apply it
                                    var icon = string.IsNullOrEmpty(objtk.AsString()) ? null : Program.CurrentApp.JaxImages!.GetImage(objtk.AsString(), out _);
                                    icon ??= Program.CurrentApp.JaxImages!.GetImage("*jax*", out _);

                                    JAXApp.MainWindowInstance!.Icon = new Avalonia.Controls.WindowIcon(Program.CurrentApp.JaxImages!.Resize(icon, 32, 32));
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "left":
                            fakeWindow.Left = objtk.AsDouble();
                            me.originalLeft = objtk.AsDouble();
                            break;

                        case "maxbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MaxButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "minbutton":
                            if (objtk.Element.Type == "L")
                                fakeWindow.MinButton = objtk.AsBool();
                            else
                                result = 11;
                            break;

                        case "maxheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxHeight = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minheight":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinHeight = 0;
                                else
                                    fakeWindow.MinHeight = objtk.AsInt() + HeightDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "maxwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MaxWidth = double.PositiveInfinity;
                                else
                                    fakeWindow.MaxWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "minwidth":
                            if (objtk.Element.Type == "N")
                            {
                                if (objtk.AsInt() < -1)
                                    result = 41;
                                else if (objtk.AsInt() < 0)
                                    fakeWindow.MinWidth = 0;
                                else
                                    fakeWindow.MinWidth = objtk.AsInt() + WidthDelta;
                            }
                            else
                                result = 11;
                            break;

                        case "showwindow":
                            if (windowLocked)
                                result = 9702;
                            else if (objtk.Element.Type == "N" && JAXLib.Between(objtk.AsInt(), 0, 2))
                                fakeWindow.ShowWindow = objtk.AsInt();
                            else
                                result = 41;
                            break;

                        case "top":
                            fakeWindow.Top = objtk.AsDouble();
                            me.originalTop = objtk.AsDouble();
                            break;

                        case "visible":
                            if (objtk.Element.Type == "L")
                            {
                                // Visibility handled in Show/Hide
                                if (objtk.AsBool() && !InInit)
                                    windowLocked = true;
                            }
                            else
                                result = 11;
                            break;

                        case "width":
                            if (objtk.Element.Type == "N" && objtk.AsInt() >= 0)
                            {
                                fakeWindow.Width = objtk.AsDouble() + WidthDelta;
                                objValue = objtk.AsDouble();
                                me.originalWidth = objtk.AsDouble();
                            }
                            else
                                result = 11;
                            break;

                        case "windowstate":
                            if (objtk.Element.Type == "N")
                            {
                                int vfpState = objtk.AsInt();
                                fakeWindow.WindowState = vfpState switch
                                {
                                    1 => Avalonia.Controls.WindowState.Minimized,
                                    2 => Avalonia.Controls.WindowState.Maximized,
                                    _ => Avalonia.Controls.WindowState.Normal   // 0 or invalid → Normal
                                };
                                objValue = vfpState;
                            }
                            else
                                result = 11;  // type mismatch
                            break;



                        default:
                            // Process standard properties
                            result = 1;
                            break;
                    }

                    // Was the property retrieved?
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
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                result = -1;
            }

            return result;
        }



        public override string[] JAXMethods()
        {
            return
                [
                "move", "readexpression", "readmethod", "refresh", "show", "writemethod"
                ];
        }

        public override string[] JAXEvents()
        {
            return
            [
                "activate","click","dblclick","deactivate","destroy","error","gotfocus","init","keypress","load","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "moved","queryunload","resize","rightclick","scrolled","unload","visiblechanged"
            ];
        }


        public override string[] JAXProperties()
        {
            return
            [
                "alias,c,","alwaysontop,L,false", "autocenter,L,true","autohidescrollbar,L,false",
                "backcolor,R,255|255|255","baseclass,C!,robrowser","bindcontrols,L,true","borderstyle,N,3",
                "caption,C,Editor","class,C!,robrowser","classlibrary,C!,","closable,L,true","comment,C,","controlbox,L,true","controlcount,N!,0",
                "datasession,N,1","datasessionid,N!,1",
                "Enabled,L,true",
                "FontBold,L,false","FontItalic,L,false","FontName,C,Arial","FontSize,N,12","forecolor,R,0",
                "Height,N,600",
                "icon,C,*jax*",
                "keypreview,L,false",
                "left,N,0","lockscreen,L,false",
                "maxbutton,L,true","maxheight,N,-1","maxwidth,N,-1","minbutton,L,true","minheight,N,-1","minwidth,N,-1","mousepointer,n,0","moveable,L,true",
                "name,C,JAXEdit",
                "objects,*,",
                "parent,o!$,","parentclass,C!$,","picture,C,",
                "righttoleft,L,false",
                "scalefactor,N,0","scrollbars,n,3","showintaskbar,L,.T.","showwindow,N,2",
                "tabindex,N,1","tabstop,L,true","tag,C,","top,N,0","tooltiptext,c,",
                "visible,L,true",
                "width,N,800","windowstate,N,0","windowtype,N,0",
            ];
        }
    }
}
