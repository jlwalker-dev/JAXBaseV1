using JAXBase.Core;
using JAXBase.Utilities.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ComboBox : XBase_Class_Avalonia
    {
        // This list holds the row source array followed by important related values
        private List<ListObjectCollection> ListItemArray = [];
        private readonly List<string> ItemList = [];
        private int ListCounter = 0;
        private int ListColumns = 1;
        private int BoundColumn = 1;

        public Avalonia.Controls.ComboBox CboBox => (Avalonia.Controls.ComboBox)me.avaloniaObject!;

        public XBase_Class_Visual_ComboBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.ComboBox(), "Combobox", "combobox", true, UserObject.urw);
        }

        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         *
         * Return result from XBase_Visual_Class
         * 0 - Successfully proccessed
         * 1 - Did not process
         * 2 - Requires special processing
         * 9 - Processed and saved, do not do anything else
         * 10 - Processed and saved
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
            int val, rows, cols;
            JAXObjects.Token tk = new JAXObjects.Token();
            JAXObjects.Token objtk = new JAXObjects.Token();
            objtk.Element.Value = objValue;
            propertyName = propertyName.ToLower();
            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    switch (propertyName)
                    {
                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objValue);
                            CboBox.BorderBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor((int)objValue));
                            break;
                        case "borderwidth":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 255))
                                {
                                    CboBox.BorderThickness = new Avalonia.Thickness(objtk.AsInt());
                                    objValue = objtk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;
                        // Intercept special handling of properties
                        case "boundcolumn":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                val = objtk.AsInt();
                                if (val < 1 || val > ListColumns) throw new Exception("31|");
                                // Are there rows in the ListItemArray?
                                if (BoundColumn != val && val > 0 && ListItemArray.Count > 0 && ListItemArray[0].ItemRow.Count >= val)
                                {
                                    // Clear out the items
                                    CboBox.Items.Clear();
                                    // Add the new bound column
                                    for (int r = 0; r < ListItemArray.Count; r++)
                                        CboBox.Items.Add(ListItemArray[r].ItemRow._avalue[val - 1].ValueAsString);
                                    // Set to the bound column to the first list item
                                    CboBox.SelectedIndex = 0;
                                    BoundColumn = val;
                                }
                            }
                            else
                                result = 11;
                            break;
                        case "columncount":
                            try
                            {
                                if (objtk.Element.Type.Equals("N"))
                                {
                                    // Resize the array by copying to a new one
                                    cols = objtk.AsInt();
                                    if (cols < 1)
                                    {
                                        if (cols < 0)
                                            result = 41;
                                        else
                                        {
                                            ListColumns = 0;
                                            ListItemArray = new List<ListObjectCollection>();
                                        }
                                    }
                                    else
                                    {
                                        for (int r = 0; r < ListItemArray.Count; r++)
                                        {
                                            tk = ListItemArray[r].ItemRow;
                                            ListItemArray[r].ItemRow = AppHelper.ACopyToNew(tk, 1, cols);
                                        }
                                        ListColumns = cols;
                                    }
                                }
                                else
                                    result = 11;
                            }
                            catch (Exception ex)
                            {
                                throw new Exception(ex.Message + "LISTITEM");
                            }
                            break;
                        case "list": // by Item index
                        case "listitem": // by ItemID
                                         // Update the item index TODO - 1 or 2D (row,col)
                            if (objIdx > ItemList.Count + 1) throw new Exception("1231|");
                            if (objIdx < 0) throw new Exception("31|");
                            if (objIdx == 0 || objIdx == ItemList.Count + 1)
                                CboBox.Items.Add(objValue.ToString() ?? string.Empty); // Add to end
                            else
                                CboBox.Items[objIdx - 1] = objValue.ToString() ?? string.Empty; // Replace existing
                            // TODO - Fix up the CboBox.Item if it's the bound column
                            break;
                        case "listindex": // Move to the item index
                            rows = Convert.ToInt32(objValue);
                            if (ListItemArray.Count > 0)
                            {
                                rows = rows > 0 && rows <= ListItemArray.Count ? rows : 1;
                                GetObjectArrayRow(rows);
                                CboBox.SelectedIndex = rows - 1;
                            }
                            break;

                        case "listcount":
                            if (InInit == false)
                                throw new Exception(string.Format("Property {0} is read only.", propertyName.ToUpper()));
                            break;

                        case "maxheight":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (objtk.AsInt() > 74)
                                    CboBox.MaxHeight = objtk.AsInt();
                            }
                            else
                                result = 11;
                            break;

                        case "rowsource":
                            // When assigned, loads the array, if the data source exists
                            string rowsource = objtk.AsString();
                            // Load an array with the load source
                            JAXObjects.Token LoadArray = await XClass_AuxCode.GetRowSource(App, rowsource, UserProperties["rowsourcetype"].AsInt());
                            // If we got something back, load it!
                            if (LoadArray.TType.Equals("A"))
                            {
                                // Clear out the ListItemArray object & counter
                                ListItemArray = new List<ListObjectCollection>();
                                CboBox.Items.Clear();
                                ListCounter = 0;
                                AddObjectArrayRow(LoadArray);
                            }
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
                            // We processed it or just need to save the property (perhaps again)
                            // Ignore the CA1854 as it won't put the value into the property
                            UserProperties[propertyName].Element.Value = objValue;
                        }
                        result = 0;
                    }
                }
                else
                    result = 1559;
            }
            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{propertyName}", string.Empty);
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
                // Get the property and fill in the value
                returnToken.CopyFrom(UserProperties[propertyName]);
                switch (propertyName)
                {
                    // Intercept special handling of properties
                    case "columncount":
                        returnToken.Element.Value = ListColumns;
                        break;

                    case "list":
                        if (idx > ItemList.Count + 1) throw new Exception("1231|");
                        if (idx < 0) throw new Exception("31|");
                        returnToken.Element.Value = CboBox.Items[idx - 1] ?? string.Empty;
                        break;

                    case "listcount":
                        returnToken.Element.Value = CboBox.Items.Count;
                        break;

                    case "listitem":
                        if (idx < 0) throw new Exception("31|");
                        if (JAXLib.Between(idx, 1, ListItemArray.Count))
                            returnToken.Element.Value = ItemList[idx - 1]; // Return the list item string
                        else
                            returnToken.Element.Value = string.Empty; // we're very forgiving
                        break;

                    case "listindex":
                        returnToken.Element.Value = CboBox.SelectedIndex + 1;
                        break;

                    case "sorted":
                        //returnToken.Element.Value = CboBox.Sorted; // Note: Avalonia ComboBox does not have a built-in Sorted property; implement custom sorting if needed
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
                _AddError(result, 0, propertyName, App.AppLevels[^1].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[^1].Procedure))
                    App.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;

            // Process the object's native method if appropriate
            switch (methodName.ToLower())
            {
                case "additem":
                case "addlistitem":
                    if (App.ParameterClassList.Count > 0)
                    {
                        for (int i = 0; i < App.ParameterClassList.Count; i++)
                        {
                            // Can add more than one element to the row
                            // at a time using multiple parameters
                            JAXObjects.Token tk = await AppHelper.GetParameterClassToken(App, App.ParameterClassList[i]);
                            AddObjectArrayRow(tk);
                        }
                    }
                    else
                        result = 41;
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Accept a new item which may be an array. If it is, fill
         * up the row, if possible. Extra elements in the newItem
         * array are ignored.
         *
         * First element is always converted to string since the item
         * displayed and returned is to be a string.
         *
         * Multiple rows are respected and added
         *
         *------------------------------------------------------------------------------------------*/
        private int AddObjectArrayRow(JAXObjects.Token newItem)
        {
            // Ignore value, just add a new ListListItemArray to ListItemArray
            int addingRows = newItem.Row;
            int addingCols = newItem.Col;

            // Add what was sent to the current list.  If it's a
            // multirow array, handle it, but we usually get a
            // 1D array (which might just be a single value).
            for (int r = 0; r < addingRows; r++)
            {
                // This is in the JAXClasses.cs and holds an ID and token for each entry.
                // The token is treated as an array and can hold numerous columns per row.
                // It assumes that the number of columns in each row will be the same.
                ListObjectCollection newLoc = new ListObjectCollection(ListColumns, ++ListCounter);

                // Move the newItem array to the newLoc.ItemRow array
                for (int c = 0; c < addingCols; c++)
                {
                    newItem.SetElement(r + 1, c + 1);
                    newLoc.ItemRow._avalue[c].Value = newItem.Element.ValueAsString;
                }

                // Add to the list item and the bound column as a string to the CbBox items collection
                ListItemArray.Add(newLoc);
                CboBox.Items.Add(newLoc.ItemRow._avalue[BoundColumn - 1].ValueAsString);
            }

            // Return the last item property's index, id, and itemiddata
            UserProperties["newindex"].Element.Value = ListItemArray.Count;
            UserProperties["newitemid"].Element.Value = ListCounter;
            UserProperties["itemiddata"].Element.Value = ListCounter;

            // Return the total number of ListItemArray rows
            return ListItemArray.Count - 1;
        }


        /*------------------------------------------------------------------------------------------*
         * Insert an item or item array into the ListItemArray at the index.
         * Only the first row is inserted, others are ignored.
         *------------------------------------------------------------------------------------------*/
        private void InsertObjectArrayRowAt(JAXObjects.Token newItem, int moveIDX)
        {
            // Ignore value, just add a new ListListItemArray to ListItemArray
            int addingRows = newItem.Row;
            int addingCols = newItem.Col;
            // Clear out the ListItemArray object & counter
            ListItemArray = new List<ListObjectCollection>();
            ListCounter = 0;
            // Add what we have
            ListObjectCollection loc = new ListObjectCollection(ListColumns, ++ListCounter);
            for (int c = 0; c < addingCols; c++)
            {
                if (addingCols < ListColumns)
                    loc.ItemRow._avalue[c] = newItem._avalue[c];
            }
            if (moveIDX >= 0)
            {
                if (moveIDX < ListItemArray.Count)
                    ListItemArray.Insert(moveIDX, loc);
                else
                    ListItemArray.Add(loc);
            }
            else
                throw new Exception("3003|");
        }
        /*------------------------------------------------------------------------------------------*
         * Remove an item from the ListItemArray.
         *------------------------------------------------------------------------------------------*/
        private void RemoveObjectArrayRow(int idx) { ListItemArray.RemoveAt(idx); }

        /*------------------------------------------------------------------------------------------*
         * Move the selected ListItemArray element into the related UserProperties
         *
         * The idx parameter must be the JAXBase value (1+)
         *------------------------------------------------------------------------------------------*/
        private void GetObjectArrayRow(int idx)
        {
            JAXObjects.Token objectRow = new JAXObjects.Token();
            AppHelper.ASetDimension(objectRow, 1, ListColumns);
            if (ListItemArray.Count > 0)
            {
                if (idx < 1 || idx > ListItemArray.Count)
                    throw new Exception($"1234||Index received was {idx}");
                idx--;
                for (int i = 0; i < ListColumns; i++)
                    objectRow._avalue[i] = ListItemArray[idx].ItemRow._avalue[i];
                UserProperties["list"] = objectRow;
                UserProperties["value"].Element.Value = objectRow._avalue[BoundColumn - 1];
            }
            else
            {
                // Nothing in the item array
                UserProperties["list"] = objectRow;
                UserProperties["value"].Element.Value = string.Empty;
            }
        }



        /*------------------------------------------------------------------------------------------*
         * Methods for class
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXMethods()
        {
            return 
                [
                "additem","addlistitem","addproperty","clear", "indextoitemid","itemidtoindex","move", "readexpression", "readmethod",
                "refresh", "removeitem","removelistitem","requery","resettodefault","saveasclass", 
                "setfocus", "setviewport", "writeexpression", "writemethod", "zorder"
                ];
        }

        /*------------------------------------------------------------------------------------------*
         * Events for class
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","downclick","error","gotfocus",
                "init","interactivechange","keypress","lostfocus",
                "middleclick","mousedown","mouseenter","mousehover","mouseleave","mousemove","mouseup","mousewheel",
                "programmaticchange","rangehigh","rangelow","rightclick","upclick","valid","visiblechanged","when"
                ];
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
            return 
                [
                "alignment,N,0","anchor,N,0",
                "backcolor,R,255|255|255","BaseClass,C!,Combobox","bordercolor,R,0","borderstyle,n,0","borderwidth,n,0",
                "boundcolumn,n,0","boundto,l,.F.",
                "Class,C!,ComboBox","ClassLibrary,C!,","ColumnCount,N,0","columnlines,l,true","columnwidths,c,","Comment,C,","controlsource,c,",
                "disabledbackcolor,R,128|128|128","disabledforecolor,R,64|64|64","disableditembackcolor,R,128|128|128","disableditemforcolor,R,64|64|64",
                "displaycount,n,0","displayvalue,c,Combo1",
                "Enabled,L,true",
                "firstelement,n,1","FontBold,L,false","FontItalic,L,false","FontName,C,Arial","FontSize,N,9",
                "forecolor,R,0","format,c,",
                "Height,N,0","hideselection,L,.F.",
                "incrementsearch,l,true","inputmask,c,","itembackcolor,R,255|255|255","itemdata,n,0","itemforecolor,R,0",
                "itemiddata,n,0",
                "left,N,0","list,C,","listcount,n!,0","listindex,n,0","listitem,c,","listitemid,n,0",
                "margin,n,2","maxlength,n,200",
                "name,c,","newindex,n,0","newitemid,n,0","nulldisplay,c,","numberofelements,n,0",
                "parent,o!,","parentclass,C!,","picture,c,","pictureselectiondisplay,n,0",
                "readonly,l,false","righttoleft,L,false","rowsource,c,","rowsourcetype,n,0",
                "selected,l,false","selectedbackcolor,R,0|0|255","selectedforecolor,R,255|255|255","selecteditemforcolor,R,0|255|0","selecteditembackcolor,R,255|255|255",
                "selectedid,l,false","sellength,n,0","selstart,n,0","seltext,c,","sorted,l,false","style,n,0",
                "tabindex,n,0","tabstop,l,true","tag,c,","text,c,","top,N,0","topindex,n,1","topitemid,n,-1","tooltiptext,c,",
                "value,,","visible,l,true",
                "width,N,100"
                ];
        }
    }
}