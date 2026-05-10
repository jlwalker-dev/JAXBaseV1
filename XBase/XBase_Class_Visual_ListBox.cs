/* --------------------------------------------------------------------------------------------------*
 * Listbox
 * 
 * 2026-03-18 - JLW
 *      Got a basic setup from Grok which might actually seems to makes a lot of sense 
 *      for the listbox control.
 *      
 *      
 * 2026-05-04 - JLW 
 *      Started copying portions of ComboBox class over.
 *      Tied in the CollectionChanged event.
 * 
 * 
 * --------------------------------------------------------------------------------------------------*/
using Avalonia.VisualTree;
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ListBox : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "ListBox";
        public new string MyDefaultName { get; } = "listbox";


        // List ID counter
        private int listID = 1;

        public ObservableSortedDictionary<int, JAXObjects.Token> ListSource = [];
        private int ListColumns = 1;
        private int BoundColumn = 1;

        public JAXObjects.Token ListItemID => UserProperties["list"];       // Use dictionay key
        public JAXObjects.Token ListArray => UserProperties["listitem"];    // Use array index

        // Rowsource Type 2,3,4,6,8 workarea reference
        JAXDirectDBF? RowSourceDBF = null;
        JAXObjects.Token RowSourceExtra = new("");


        // Used for sorting list
        private BindingSource bindingSource = new();

        private string searchBuffer = "";
        private DateTime lastKeyTime = DateTime.MinValue;


        public Avalonia.Controls.ListBox lstBox => (Avalonia.Controls.ListBox)me.avaloniaObject!;

        public XBase_Class_Visual_ListBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.ListBox(), MyBaseClass, MyDefaultName, true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            UserProperties["list"] = new(ListSource, "E");      // Dictionary
            UserProperties["listitem"] = new(ListSource, "M");  // Mapped List of Dictionary

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
         *      9   - Success, do no further processing
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
            JAXObjects.Token objtk = new(objValue);
            int val, cols;

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "autohidescrollbar":
                            break;

                        case "bordercolor":
                            objValue = JAXUtilities.ReturnColorInt(objValue);
                            lstBox.BorderBrush = new Avalonia.Media.SolidColorBrush(XClass_AuxCode.IntToAvColor((int)objValue));
                            break;

                        case "borderwidth":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 255))
                                {
                                    lstBox.BorderThickness = new Avalonia.Thickness(objtk.AsInt());
                                    objValue = objtk.AsInt();
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "boundcolumn":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                val = objtk.AsInt();
                                if (val < 1 || val > ListColumns) throw new Exception("31|");

                                // Are there rows in the ListItemArray?
                                if (BoundColumn != val && val > 0 && ListItemID._avalue.Count > 0)
                                {
                                    // Clear out the items
                                    lstBox.Items.Clear();

                                    // Add the new bound column
                                    for (int r = 0; r < ListItemID._avalue.Count; r++)
                                    {
                                        JAXObjects.Token row = (JAXObjects.Token)ListItemID._avalue[r].Value;
                                        lstBox.Items.Add(row._avalue[val - 1].ValueAsString);
                                    }
                                    // Set to the bound column to the first list item
                                    lstBox.SelectedIndex = 0;
                                    BoundColumn = val;
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "columncount": // Dictates how many columsn are in the List and ListItem arrays
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
                                        ListItemID._dictionary = [];
                                    }
                                }
                                else
                                {
                                    ListColumns = cols;
                                    ListItemID.SetDimension(1, cols, true);
                                }
                            }
                            else
                                result = 11;
                            break;

                        case "columnwidths":
                            if (objtk.Element.Type.Equals("C") == false)
                                result = 11;
                            else if (string.IsNullOrWhiteSpace(objtk.Element.ValueAsString) == false)
                            {
                                if (JAXLib.ChrTran(objtk.AsString(), "0123456789, ", "").Length > 0)
                                    result = 11;
                                else
                                {
                                    // Expecting a comma delimited list of column widths
                                    string[] widths = objtk.AsString().Split(',');

                                    for (int i = 0; i < widths.Length; i++)
                                    {
                                        widths[i] = widths[i].Trim();

                                        if (widths[i].Contains(' '))
                                            result = 11;
                                        else
                                        {
                                            if (int.TryParse(widths[i], out int w))
                                            {
                                                // Avalonia ComboBox does not support column widths as it does not have built-in support for multiple columns. 
                                                // You would need to implement a custom control template to achieve this functionality.
                                            }
                                            else
                                                result = 11;
                                        }

                                        if (result > 0)
                                            break;
                                    }
                                }
                            }

                            break;
                        case "disabledbackcolor":
                            break;

                        case "disabledforecolor":
                            break;

                        case "disableditembackcolor":
                            break;

                        case "disableditemforecolor":
                            break;

                        case "displayvalue":
                            break;

                        case "firstelement":
                            break;

                        case "incrementsearch":
                            break;

                        case "itemdata":
                            break;

                        case "itemforecolor":
                            break;

                        case "itembackcolor":
                            break;

                        case "itemiddata":
                            break;

                        case "list": // Update current listarray index, column(s) should already be updated
                            if (JAXLib.Between(objIdx, 1, ListArray._avalue.Count))
                                ListItemID._dictionary![ListArray._avalue[objIdx].ValueAsInt] = objtk;
                            break;

                        case "listitem": // Update current ListItemID, column(s) should already be updated
                            if (InInit == false && ListItemID._dictionary!.ContainsKey(objIdx))
                                ListItemID._dictionary[objIdx] = objtk;
                            break;

                        case "listindex": // Move to the item index
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (objtk.AsInt() == 0)
                                    lstBox.SelectedIndex = -1;
                                else if (JAXLib.Between(objtk.AsInt(), 1, lstBox.ItemCount))
                                    lstBox.SelectedIndex = objtk.AsInt() - 1;
                                else
                                    result = 41;
                            }
                            break;

                        case "listcount":
                            // Ignore the attempt
                            break;

                        case "maxheight":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (objtk.AsInt() > 74)
                                    lstBox.MaxHeight = objtk.AsInt();
                            }
                            else
                                result = 11;
                            break;

                        case "listitemid":
                            break;

                        case "moverbars":
                            break;

                        case "multiselect":
                            if (objtk.Element.Type.Equals("L"))
                                lstBox.SelectionMode = objtk.AsBool() ? Avalonia.Controls.SelectionMode.Multiple : Avalonia.Controls.SelectionMode.Single;
                            else
                                result = 11;
                            break;

                        case "newindex":
                            break;

                        case "newitemid":
                            break;

                        case "numberofelements":
                            break;

                        case "rowsource":
                            // When assigned, loads the array, if the data source exists
                            UserProperties["rowsource"].Element.Value = objtk.AsString();
                            result = await SetRowSource();
                            result = result == 0 ? 9 : result;  // Skip assigning the value again
                            break;

                        case "rowsourcetype":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 10))
                                {
                                    // Only do something if the rowsourcetype is changed
                                    if (objtk.AsInt() != UserProperties["rowsourcetype"].AsInt())
                                    {
                                        UserProperties["rowsourcetype"].Element.Value = objtk.AsInt();
                                        result = await SetRowSource();
                                    }

                                    // Set result as 9 if nothing is wrong
                                    result = result == 0 ? 9 : result;
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 41;
                            break;

                        case "selected":
                            break;

                        case "selectedid":
                            break;

                        case "selecteditembackcolor":
                            break;

                        case "selecteditemforecolor":
                            break;

                        case "sorted":
                            if (objtk.Element.Type.Equals("L"))
                            {
                                if (objtk.AsBool())
                                {
                                    // 0 - no sort
                                    // 1 - Ascending case sensitive
                                    // 2 - Descending case sensitive
                                    // 3 - Ascending case insensitive
                                    // 4 - Descending case insensitive
                                    int sortType = UserProperties["sorttype"].AsInt();

                                    // Sort the list via binding source (wild!)
                                    string sortString = sortType == 0 ? string.Empty : sortType < 3 ? "DisplayText " : "SortKey ";
                                    sortString += sortType == 0 ? string.Empty : JAXLib.InList(sortType, 1, 3) ? "ASC" : "DES";
                                    bindingSource.Sort = sortString;
                                }
                                else
                                    bindingSource.Sort = "";    // Turn off sort
                            }
                            else
                                result = 11;
                            break;


                        case "sorttype":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(objtk.AsInt(), 0, 4))
                                    objValue = objtk.AsInt();
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "topindex":
                            if (objtk.Element.Type.Equals("N"))
                                lstBox.ScrollIntoView(objtk.AsInt());
                            else
                                result = 11;
                            break;

                        case "topitemid":
                            break;

                        case "value":
                            if (objtk.Element.Type.Equals("C"))
                                lstBox.SelectedValue = objtk.AsString();
                            else
                                result = 11;
                            break;

                        default:
                            // Process standard properties
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            result = result == 0 ? 9 : result;
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
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

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
                    case "columncount":
                        returnToken.Element.Value = ListColumns;
                        break;

                    case "displayvalue":
                        returnToken.Element.Value = "";
                        break;

                    case "firstelement":
                        returnToken.Element.Value = 1;
                        break;

                    case "itemdata":
                        break;

                    case "itemiddata":
                        break;

                    case "list":
                        if (JAXLib.Between(idx, 1, ListItemID._avalue.Count))
                            returnToken.Element.Value = ListItemID._dictionary![ListArray._avalue[idx].ValueAsInt]; // Return the list item token
                        else
                            if (idx == 0)
                                returnToken = ListArray;   // Need to return the whole array
                            else
                                result = 5;                 // Out of bounds
                        break;

                    case "listcount":
                        returnToken.Element.Value = lstBox.Items.Count;
                        break;

                    case "listitem":
                        if (ListArray._dictionary!.ContainsKey(idx))
                            returnToken = ListArray._dictionary![ListArray._avalue[idx].ValueAsInt]; // Return the list item token
                        else
                            if (idx == 0)
                                returnToken = ListArray;   // Need to return the whole dictionary
                            else
                                result = 5;
                        break;

                    case "listindex":
                        returnToken.Element.Value = lstBox.SelectedIndex + 1;
                        break;

                    case "listitemid":
                        break;

                    case "newindex":
                        break;

                    case "newitemid":
                        break;

                    case "numberofelements":
                        returnToken.Element.Value = lstBox.Items.Count;
                        break;

                    case "selected":
                        if (lstBox.SelectedItems is null)
                            returnToken.Element.Value = "";
                        else if (lstBox.SelectionMode == Avalonia.Controls.SelectionMode.Single)
                        {
                            // Return the selected object
                            returnToken.Element.Value = (string)(lstBox.SelectedItem ?? System.String.Empty);
                        }
                        else
                        {
                            // Return comma delimited string
                            string temp = System.String.Empty;
                            foreach (object selectedItem in lstBox.SelectedItems)
                            {
                                string t = (string)selectedItem;
                                temp += t.Trim() + ",";
                            }

                            returnToken.Element.Value = temp.TrimEnd(',');
                        }
                        break;

                    case "selectedid":
                        break;

                    case "sorted":
                        returnToken.Element.Value = string.IsNullOrWhiteSpace(bindingSource.Sort) == false;
                        break;

                    case "topindex":
                        returnToken.Element.Value = GetTopIndex();
                        break;

                    case "topitemid":
                        break;

                    case "value":
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;

                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

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
                "additem","addlistitem","addproperty","clear","indextoitemid","move","readexpression","readmethod",
                "refresh","removeitem","removelistitem","requery","resettodefault","saveasclass","setfocus",
                "writeexpression","writemethod","zorder"
                ];
        }


        /*------------------------------------------------------------------------------------------*
         * 
         *------------------------------------------------------------------------------------------*/
        public override string[] JAXEvents()
        {
            return
                [
                "click","dblclick","destroy","error","gotfocus","init","keypress","load","lostfocus",
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
                "anchor,N,0","autohidescrollbar,n,0",
                "BaseClass,C!,listbox","bordercolor,n,6579300","borderwidth,n,0","boundcolumn,n,0","boundto,n,0",
                "Class,C!,listbox","ClassLibrary,C!,","columncount,n,0","columnlines,l,f","columnwidths,c,","Comment,C,","controlsource,c,",
                "disabledbackcolor,R,255|255|255","disabledforecolor,R,109|109|109","disableditembackcolor,R,255|255|255",
                "disableditemforecolor,R,109|109|109","displayvalue,c,",
                "Enabled,L,true",
                "firstelement,n,1","FontBold,L,false","FontItalic,L,false",
                "FontName,C,Arial","FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0",
                "Height,N,0",
                "incrementsearch,l,true","itemdata,n,0","itemforecolor,R,0","itemiddata,n,0",
                "left,N,0","list,c,","listcount,n,0","listindex,n,0","listitem,c,","listitemid,n,0",
                "moverbars,L,.F.","multiselect,L,.F.",
                "name,c,","newindex,n,0","newitemid,n,0","numberofelements,n,0",
                "parent,o!,","parentclass,C!,",
                "righttoleft,L,false","rowsource,c,","rowsourcetype,n,0",
                "selected,l,false","selectedid,l,false","selecteditembackcolor,R,0|120|215","selecteditemforecolor,R,255|255|255",
                "sorted,l,false","sorttype,n,",
                "tabindex,n,0","tabstop,l,true","tag,c,","top,N,0","topindex,n,1","topitemid,n,-1","tooltiptext,c,",
                "value,,","visible,l,true",
                "width,N,100"
                ];
        }


        /*
         * Support for listbox specific methods and events
         * 
         * additem/addlistitem - add an entry or entries to the listbox
         * removeitem/removelistitem - remove an entry or entries from the listbox
         * 
         */
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            string msg = "";

            // Process the object's native method if appropriate
            switch (methodName.ToLower())
            {
                case "additem":         // Add or update based on the ListArray if rowsourcetype<2
                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                    {
                        if (App.ParameterClassList.Count > 0)
                        {
                            JAXObjects.Token cItem = await AppHelper.GetParameterToken(null);
                            JAXObjects.Token nItem = await AppHelper.GetParameterToken(null);
                            JAXObjects.Token nColumn = await AppHelper.GetParameterToken(null);

                            if (cItem.Element.IsNull())
                            {
                                msg = "cItem";
                                result = 97;
                            }
                            else
                            {
                                if (nColumn.Element.IsNull())
                                    nColumn.Element.Value = 1;

                                if (nColumn.Element.Type.Equals("N"))
                                {
                                    if (nColumn.AsInt() < 1)
                                        result = 41;
                                    else
                                    {
                                        // Now check nItem value - an "add" value is 0 or greater than the list count.
                                        // Otherwise a positive value < list count means insert at this position.
                                        if (nItem.Element.IsNull())
                                            nItem.Element.Value = 0;

                                        if (nItem.AsInt() < 0)
                                            result = 41;    // Invalid nItem value
                                        else
                                        {
                                            if (nItem.Element.Type.Equals("N"))
                                            {
                                                int iItem = nItem.AsInt();

                                                // We have recevied a valid nItem greater than the list count, so we
                                                // need to add blank items up to the nItem position - 1
                                                if (iItem > ListArray._mappedList!.Count)
                                                {
                                                    // Add as many rows as specified by nItem if nItem is greater than the list count
                                                    for (int i = ListArray._mappedList!.Count; i < iItem - 1; i++)
                                                        ListArray.AddItem("", i, 1, 0); // Adding a blank item
                                                }
                                            }
                                            else
                                            {
                                                msg = "nItem";
                                                result = 97;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    msg = "nColumn";
                                    result = 97;
                                }

                                // If no errors then add/update the nItem entry
                                if (result == 0)
                                {
                                    ListArray.AddItem(cItem.AsString(), nItem.AsInt(), nColumn.AsInt(), 0);

                                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                                        lstBox.Items.Add(cItem.AsString());
                                }
                            }
                        }
                        else
                        {
                            result = 95;
                            msg = "1";
                        }
                    }
                    break;

                case "addlistitem":     // Add to the ListItemID if rowsourcetype<2
                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                    {
                        if (App.ParameterClassList.Count > 0)
                        {
                            JAXObjects.Token cItem = await AppHelper.GetParameterToken(null);
                            JAXObjects.Token ItemID = await AppHelper.GetParameterToken(null);
                            JAXObjects.Token Column = await AppHelper.GetParameterToken(null);

                            if (cItem.Element.IsNull())
                            {
                                msg = "cItem";
                                result = 97;
                            }
                            else
                            {
                                if (ItemID.Element.IsNull())
                                    ItemID.Element.Value = 0;
                                else if (ItemID.Element.Type.Equals("N"))
                                {
                                    // Verify itemID > 0
                                    if (ItemID.AsInt() < 1)
                                        ItemID.Element.Value = 0;
                                }
                                else
                                {
                                    msg = "nItemID";
                                    result = 97;
                                }

                                if (Column.Element.IsNull())
                                    Column.Element.Value = 1;
                                else if (Column.Element.Type.Equals("N"))
                                {
                                    // Verify column is > 0
                                    if (Column.AsInt() < 1)
                                        Column.Element.Value = 1;
                                }
                                else
                                {
                                    result = 97;
                                    msg = "nColumn";
                                }

                                // If no errors then add/update the nItem entry
                                if (result == 0)
                                    ListItemID.AddItemID(cItem.AsString(), ItemID.AsInt(), Column.AsInt());
                            }
                        }
                        else
                        {
                            result = 95;
                            msg = "1";
                        }
                    }
                    break;

                case "removeitem":
                    // Remove from the ListArray for rowsourcetype 0 and 1
                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                    {
                        if (App.ParameterClassList.Count > 0)
                        {
                            JAXObjects.Token nItem = await AppHelper.GetParameterToken(null);
                            if (nItem.Element.IsNull())
                            {
                                msg = "nItem";
                                result = 97;
                            }
                            else if (nItem.Element.Type.Equals("N"))
                            {
                                int iItem = nItem.AsInt();
                                if (JAXLib.Between(iItem, 1, ListArray._mappedList!.Count))
                                {
                                    // Remove from the dictionary which will update the List and ListItemID properties
                                    ListSource.Remove(ListArray._avalue[iItem].ValueAsInt);

                                    // Remove from the combo box
                                    lstBox.Items.RemoveAt(iItem - 1);
                                }
                                else
                                {
                                    msg = "nItem";
                                    result = 41;
                                }
                            }
                            else
                            {
                                msg = "nItem";
                                result = 97;
                            }
                        }
                        else
                        {
                            result = 95;
                            msg = "1";
                        }
                    }
                    break;

                case "removelistitem":
                    // Remove from the ListItemID for rowsourcetype 0 & 1
                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                    {
                        if (App.ParameterClassList.Count > 0)
                        {
                            JAXObjects.Token nItemID = await AppHelper.GetParameterToken(null);
                            if (nItemID.Element.IsNull())
                            {
                                msg = "nItemID";
                                result = 97;
                            }
                            else if (nItemID.Element.Type.Equals("N"))
                            {
                                int iItemID = nItemID.AsInt();

                                if (ListItemID._dictionary!.ContainsKey(iItemID))
                                {
                                    for (int i = 0; i < ListArray.Count; i++)
                                    {
                                        // Look for and remove the key from the combo box
                                        if (ListArray._avalue[i].ValueAsInt == iItemID)
                                        {
                                            lstBox.Items.Remove(i);
                                            break;
                                        }
                                    }

                                    // Remove from the dictionary so everything else gets updated
                                    ListItemID._dictionary!.Remove(iItemID);
                                }
                                else
                                {
                                    msg = "nItemID";
                                    result = 41;
                                }
                            }
                            else
                            {
                                msg = "nItemID";
                                result = 97;
                            }
                        }
                        else
                        {
                            result = 95;
                            msg = "1";
                        }
                    }
                    break;

                case "requery":
                    // force the combo box to requery the data based on the rowsource and rowsourcetype settings
                    result = await ReQuery();
                    break;

                case "clear":
                    if (App.ParameterClassList.Count > 0)
                        result = 98;
                    else
                        ListItemID.Clear();
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            // Handle any error encountered.
            if (result > 0)
            {
                _AddError(result, 0, msg, methodName);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{msg}|{methodName}", methodName);
            }

            return result;
        }


        // Default keypress event
        //private void listBox_KeyPress(object? sender, Avalonia.Input.KeyEventArgs e)
        //{
        //    if (e.KeySymbol is null && e.Key != Avalonia.Input.Key.Back)
        //        return;

        //    // Is there JAXCode to execute first?
        //    if (System.String.IsNullOrWhiteSpace(Methods["keypress"].CompiledCode) == false)
        //    {
        //        // set up parameters and call the method
        //    }

        //    if (e.Key != Avalonia.Input.Key.Back && (e.KeySymbol == null || e.KeySymbol.Length != 1 || !System.Char.IsLetterOrDigit(e.KeySymbol[0])))
        //        return;  // optional: ignore non-alphanum

        //    e.Handled = true;  // suppress default prefix-jump

        //    if ((System.DateTime.Now - lastKeyTime).TotalSeconds > 1.5)
        //        searchBuffer = "";  // timeout → reset

        //    if (e.Key == Avalonia.Input.Key.Back)
        //    {
        //        if (searchBuffer.Length > 0)
        //            searchBuffer = searchBuffer.Substring(0, searchBuffer.Length - 1);
        //    }
        //    else
        //    {
        //        searchBuffer += System.Char.ToUpper(e.KeySymbol![0]);
        //    }

        //    lastKeyTime = System.DateTime.Now;

        //    // Find first match (prefix search, case-insensitive – exact WinForms ListBox.FindString behavior)
        //    if (System.String.IsNullOrEmpty(searchBuffer))
        //        return;  // WinForms FindString("") returns -1 → no selection change

        //    // Use the original collection (listBoxItems) for correct indexing
        //    System.Collections.IList? source = listBoxItems as System.Collections.IList ?? lstBox.ItemsSource as System.Collections.IList;
        //    if (source == null || source.Count == 0)
        //        return;

        //    int foundIndex = -1;
        //    object? foundItem = null;

        //    for (int i = 0; i < source.Count; i++)
        //    {
        //        object? item = source[i];
        //        if (item == null) continue;

        //        // Respect DisplayMemberBinding = new Binding("Name")
        //        dynamic dynItem = item;
        //        string? displayText = dynItem.Name?.ToString();

        //        if (displayText != null &&
        //            displayText.StartsWith(searchBuffer, System.StringComparison.OrdinalIgnoreCase))
        //        {
        //            foundIndex = i;
        //            foundItem = item;
        //            break;
        //        }
        //    }

        //    if (foundIndex >= 0 && foundItem != null)
        //    {
        //        lstBox.SelectedIndex = foundIndex;
        //        lstBox.ScrollIntoView(foundItem);   // Avalonia equivalent of TopIndex
        //    }
        //}

        /*
         * RowSource Types
         * 
         *  0 - None
         *  1 - Delimited string
         *  2 - Alias - no optional field list
         *  3 - SQL Statement
         *  4 - Query (qpr file)
         *  5 - Array
         *  6 - Fields from a table or alias
         *  7 - Files
         *  8 - Structure of table
         *  9 - 
         * 10 - Collection -> rowsource = MyCollection (simple values) or MyCollection,PropertyName (property value from each collection object)
         *  
         */
        private async Task<int> SetRowSource()
        {
            int result = 0;

            // Don't bother doing anything if rowsourcetype>1 and rowsource is blank
            if (UserProperties["rowsourcetype"].AsInt() < 2 || string.IsNullOrWhiteSpace(UserProperties["rowsource"].AsString()) == false)
            {
                // Now process the RowSource setting
                switch (UserProperties["rowsourcetype"].AsInt())
                {
                    case 0:
                        // Nothing to do
                        break;

                    case 1:     // Value list
                        result = await RowSource1();
                        break;

                    case 2:     // Alias
                        result = await RowSource2();
                        break;

                    case 3:     // SQL Statement
                        result = await RowSource3();
                        break;

                    case 4:     // Query File
                        result = await RowSource4();
                        break;

                    case 5:     // Array
                        result = await RowSource5();
                        break;

                    case 6:     // Field list
                        result = await RowSource6();
                        break;

                    case 7:     // Files
                        result = await RowSource7();
                        break;

                    case 8:     // Stucture
                        result = await RowSource8();
                        break;

                    case 9:     // JSON
                        result = await RowSource9();
                        break;

                    case 10:    // Collection
                        result = await RowSource10();
                        break;

                    default:
                        result = 41;
                        break;
                }
            }

            return result;
        }


        private async Task<int> ReQuery()
        {
            int result = 0;

            // Clear the dictionary which clears ListItem and List
            // and resets the ListItemID counter
            ListItemID.Clear();
            lstBox.Items.Clear();
            lstBox.SelectedIndex = -1;
            UserProperties["listindex"].Element.Value = 0;
            UserProperties["listitemid"].Element.Value = 0;
            UserProperties["newindex"].Element.Value = 0;
            UserProperties["newitemid"].Element.Value = 0;
            UserProperties["topindex"].Element.Value = 0;
            RowSourceExtra.Clear();

            // Now process the RowSource setting
            switch (UserProperties["rowsourcetype"].AsInt())
            {
                case 0:
                    // Nothing to do
                    break;

                case 1:     // Value list
                    result = await Requery1();
                    break;

                case 2:     // Alias
                    result = await Requery2();
                    break;

                case 3:     // SQL Statement
                    result = await Requery3();
                    break;

                case 4:     // Query File
                    result = await Requery4();
                    break;

                case 5:     // Array
                    result = await Requery5();
                    break;

                case 6:     // Field list
                    result = await Requery6();
                    break;

                case 7:     // Files
                    result = await Requery7();
                    break;

                case 8:     // Stucture
                    result = await Requery8();
                    break;

                case 9:     // JSON
                    result = await Requery9();
                    break;

                case 10:    // Collection
                    result = await Requery10();
                    break;

                default:
                    result = 41;
                    break;
            }

            // Value and Display Value depends on the old value still being in the list
            if (BoundColumn > 1 && string.IsNullOrWhiteSpace(UserProperties["value"].AsString()) == false)
            {
                // Check the bound column for this value
                string cItem = UserProperties["value"].AsString().Trim();
                UserProperties["value"].Element.Value = "";

                JAXObjects.Token oItem = new();

                // Check column 1 for this value, if it does not exist then remove it
                for (int i = 0; i < lstBox.Items.Count; i++)
                {

                    oItem.Element.Value = ListArray._avalue[BoundColumn].Value;
                    if (oItem.AsString().Trim().Equals(cItem, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found it!  So set it back as it was
                        UserProperties["value"].Element.Value = cItem;
                        break;
                    }
                }
            }
            else
            {
                // Clear it out (may repopulate if displayvalue is found)
                UserProperties["value"].Element.Value = "";
            }

            if (string.IsNullOrWhiteSpace(UserProperties["displayvalue"].AsString()) == false)
            {
                string cItem = UserProperties["displayvalue"].AsString().Trim();
                UserProperties["displayvalue"].Element.Value = "";

                JAXObjects.Token oItem = new();

                // Check column 1 for this value, if it does not exist then remove it
                for (int i = 0; i < lstBox.Items.Count; i++)
                {
                    oItem.Element.Value = lstBox.Items[i]!;
                    if (oItem.AsString().Trim().Equals(cItem, StringComparison.OrdinalIgnoreCase))
                    {
                        // Found it!  So set it back as it was
                        UserProperties["displayvalue"].Element.Value = cItem;

                        if (BoundColumn < 2)
                            UserProperties["value"].Element.Value = cItem;
                        break;
                    }
                }
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------
         * Comma-delimited list
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource1()
        {
            return await Requery1();
        }


        /* ------------------------------------------------------------
         * REQUERY 1
         * 
         * Requery the comma-delimited list and bring it back
         * to the rowsource elements
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery1()
        {
            int result = 0;

            JAXObjects.Token values = await XClass_AuxCode.GetRowSource(App, UserProperties["rowsource"].AsString(), UserProperties["rowsourcetype"].AsInt());

            if (values.Element.Type.Equals("N"))
                result = values.AsInt();
            else if (values.TType.Equals("A") || string.IsNullOrWhiteSpace(values.AsString()) == false)
            {
                // If we got something back, load it!
                // Get max columns to load based on ColumnCount property and array column count
                int cols = System.Math.Max(UserProperties["columnCount"].AsInt(), values.Col);
                cols = System.Math.Max(cols, 1);

                // Fill the List and ListItem arrays and the combo box items based on the array values
                // for each row
                for (int r = 1; r <= values.Row; r++)
                {
                    // And column of the source array
                    for (int c = 1; c <= cols; c++)
                    {
                        values.SetElement(r, c);

                        // Convert the array element to a string
                        string cItem = values.Element.IsNull() ? "" : values.AsString();

                        if (c == 1)
                        {
                            // Create a new row in the ListArray and associated dictionary
                            // Note: A Key of 0 causes the dictionary key to be automatically created
                            ListArray.AddItem(cItem, r, c, 0);
                            UserProperties["newindex"].Element.Value = r;
                            UserProperties["newitemid"].Element.Value = ListArray._avalue[r].ValueAsInt;
                        }
                        else
                        {
                            // Fill in the columns for the current ListArray row
                            ListArray.SetElement(r, c);
                            ListArray.Element.Value = cItem;
                        }
                    }
                }
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------
         * Alias
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource2()
        {
            int result = 0;
            string msg = "";

            int cds = App.CurrentDataSession;
            int cwa = App.CurrentDS.CurrentWorkArea();

            try
            {
                // Get the alias or table from the rowsource property and
                // load the combo box using first ColumnCount columns
                // Load the entire array up to ColumnCount
                JAXObjects.Token table = await XClass_AuxCode.GetRowSource(App, UserProperties["rowsource"].AsString(), UserProperties["rowsourcetype"].AsInt());

                if (table.Element.Type.Equals("N"))
                    result = table.AsInt();
                else if (table.Element.Type.Equals("C"))
                {
                    // If we got something back, load it!
                    int wa = App.CurrentDS.ReturnWorkArea(table.AsString(), 0);
                    if (wa > 0)
                    {
                        App.CurrentDS.SelectWorkArea(wa);
                        RowSourceDBF = App.CurrentDS.WorkAreas[wa];

                        if (RowSourceDBF.DbfInfo.DBFStream is not null)
                        {
                            // Fill the List/ListItem arrays and the combo box items based
                            // on the columns of each of the table's rows
                            result = await Requery2();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            // Put us back where we started
            App.SetDataSession(cds);
            App.CurrentDS.SelectWorkArea(cwa);

            return result;
        }


        /* ------------------------------------------------------------
         * REQUERY 2 - Alias
         * 
         * Clear the combobox and repopulate everything from an alias
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery2()
        {
            int result = 0;
            string msg = "";

            try
            {
                // Is there a rowsource DBF?
                if (RowSourceDBF is not null)
                {
                    // Is the table open?
                    if (RowSourceDBF.DbfInfo.DBFStream is null)
                        result = 52;
                    else
                    {
                        // Clear everything out
                        lstBox.SelectedIndex = -1;
                        lstBox.Items.Clear();
                        ListSource.Clear();
                        UserProperties["listindex"].Element.Value = 0;
                        UserProperties["listitemid"].Element.Value = 0;


                        // We have an open table so check columns against ColumnCount property
                        int cols = System.Math.Min(UserProperties["columnCount"].AsInt(), RowSourceDBF.DbfInfo.VisibleFields);
                        cols = cols < 1 ? 1 : cols; // Must be at least 1

                        // Load the table
                        JAXDirectDBF.DBFInfo db = RowSourceDBF.DbfInfo;

                        // Go to the top record, respecting the current index
                        await RowSourceDBF.DBFGotoRecord("top");
                        int currentRecNo = db.CurrentRecNo;
                        int row = 0;    // can't rely on physical row if there's an index

                        while (db.DBFEOF == false)
                        {
                            int visibleFieldsUsed = 0;  // temp counter to track number of fields used in this row

                            // And column of the source array
                            for (int c = 1; c < db.FieldCount; c++)
                            {
                                // Skip deleted rows
                                if (db.currentRowIsDeleted)
                                    continue;

                                if (db.Fields[c - 1].SystemColumn == false)   // System columns are not visible
                                {
                                    visibleFieldsUsed++;
                                    if (visibleFieldsUsed > cols)   // Drop out if we have already used the max number of visible fields
                                        break;

                                    if ("MQW".Contains(db.Fields[c - 1].FieldType))       // Skip process Memo, General and Picture fields
                                        continue;

                                    // Convert the field value to a token so we can more
                                    // easily convert to a string and handle nulls
                                    JAXObjects.Token tk = new();

                                    // Null handling goes here

                                    if (tk.Element.IsNull())
                                        tk.Element.Value = "";  // TODO - perform null handling based on property setting
                                    else
                                        tk.Element.Value = db.CurrentRow.Rows[0][c - 1].ToString() ?? "";

                                    // TODO - any special handling based on field type can go here
                                    string cItem = tk.AsString();


                                    // Add it to the combo box, List and ListItem arrays
                                    if (visibleFieldsUsed == 1)
                                    {
                                        // First field used creates a new row in the ListArray and associated dictionary
                                        // Note: A Key of 0 causes the dictionary key to be automatically created
                                        ListArray.AddItem(cItem, ++row, c, 0);
                                        UserProperties["newindex"].Element.Value = row;
                                        UserProperties["newitemid"].Element.Value = row;
                                    }
                                    else
                                    {
                                        // Fill in the remaining columns for the current ListArray row
                                        ListArray.SetElement(row, c);
                                        ListArray.Element.Value = cItem;
                                    }
                                }
                            }

                            // Goto next record
                            await RowSourceDBF.DBFSkipRecord(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            return result;
        }


        /* ------------------------------------------------------------------------------------------
         * 3 - SQL Statement
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource3()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------
         * REQUERY3
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery3()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 4 - Query File
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource4()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------
         * REQUERY 4
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery4()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 5 - Array
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource5()
        {
            int result = 0;
            result = await Requery5();
            return result;
        }


        /* ------------------------------------------------------------
         * REQUERY 5
         * 
         * Array
         * ------------------------------------------------------------*/
        private async Task<int> Requery5()
        {
            int result = 0;
            string msg = "";

            try
            {
                // Load the entire array up to ColumnCount
                JAXObjects.Token LoadArray = await XClass_AuxCode.GetRowSource(App, UserProperties["rowsource"].AsString(), UserProperties["rowsourcetype"].AsInt());

                if (LoadArray.Element.Type.Equals("N"))
                    result = LoadArray.AsInt();
                else if (LoadArray.TType.Equals("A"))
                {
                    // If we got something back, load it!
                    // Get max columns to load based on ColumnCount property and array column count
                    int cols = System.Math.Max(UserProperties["columnCount"].AsInt(), 1);
                    if (cols > LoadArray.Col) cols = LoadArray.Col;

                    // Fill the List and ListItem arrays and the combo box items based on the array values
                    // for each row
                    for (int r = 1; r <= LoadArray.Row; r++)
                    {
                        // And column of the source array
                        for (int c = 1; c <= cols; c++)
                        {
                            LoadArray.SetElement(r, c);

                            // Convert the array element to a string
                            string cItem = LoadArray.Element.IsNull() ? "" : LoadArray.AsString();

                            if (c == 1)
                            {
                                // Create a new row in the ListArray and associated dictionary
                                // Note: A Key of 0 causes the dictionary key to be automatically created
                                ListArray.AddItem(cItem, r, c, 0);
                                UserProperties["newindex"].Element.Value = r;
                                UserProperties["newitemid"].Element.Value = ListArray._avalue[r].ValueAsInt;
                            }
                            else
                            {
                                // Fill in the columns for the current ListArray row
                                ListArray.SetElement(r, c);
                                ListArray.Element.Value = cItem;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 6 - Alias.Field1,Field2,Field3,...
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource6()
        {
            int result = 0;
            string msg = "";
            try
            {
                // Load the field list for the combo box
                JAXObjects.Token fieldList = await XClass_AuxCode.GetRowSource(App, UserProperties["rowsource"].AsString(), UserProperties["rowsourcetype"].AsInt());

                if (fieldList.Element.Type.Equals("N"))
                    result = fieldList.AsInt();
                else if (fieldList.Element.Type.Equals("A"))
                {
                    string table = fieldList._avalue[0].ValueAsString;

                    // If we got something back, load it!
                    int wa = App.CurrentDS.ReturnWorkArea(table, 0);
                    if (wa > 0)
                    {
                        App.CurrentDS.SelectWorkArea(wa);
                        RowSourceDBF = App.CurrentDS.WorkAreas[wa];

                        if (RowSourceDBF.DbfInfo.DBFStream is not null)
                        {
                            // Fill the List/ListItem arrays and the combo box items based
                            // on the columns of each of the table's rows
                            RowSourceExtra.CopyFrom(fieldList);
                            result = await Requery6();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }
            return result;
        }

        /* ------------------------------------------------------------
         * REQUERY 6
         * 
         * Fields from a table or alias
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery6()
        {
            int result = 0;
            string msg = "";

            try
            {
                // Is there a rowsource DBF?
                if (RowSourceDBF is not null)
                {
                    // Is the table open?
                    if (RowSourceDBF.DbfInfo.DBFStream is null)
                        result = 52;
                    else
                    {
                        // Clear everything out
                        lstBox.SelectedIndex = -1;
                        lstBox.Items.Clear();
                        ListSource.Clear();
                        UserProperties["listindex"].Element.Value = 0;
                        UserProperties["listitemid"].Element.Value = 0;


                        // We have an open table so check columns against ColumnCount property
                        UserProperties["columncount"].Element.Value = RowSourceExtra.Col - 1;
                        int cols = RowSourceExtra.Col - 1;

                        if (cols < 1)
                            result = 9999;
                        else
                        {
                            ListColumns = cols;

                            // Load the table
                            JAXDirectDBF.DBFInfo db = RowSourceDBF.DbfInfo;

                            // Go to the top record, respecting the current index
                            await RowSourceDBF.DBFGotoRecord("top");
                            int currentRecNo = db.CurrentRecNo;
                            int row = 0;    // can't rely on physical row if there's an index

                            while (db.DBFEOF == false)
                            {
                                int visibleFieldsUsed = 0;  // temp counter to track number of fields used in this row

                                // And column of the source array
                                for (int c = 1; c < db.FieldCount; c++)
                                {
                                    // Skip deleted rows
                                    if (db.currentRowIsDeleted)
                                        continue;

                                    if (db.Fields[c - 1].SystemColumn == false)   // System columns are not visible
                                    {
                                        visibleFieldsUsed++;
                                        if (visibleFieldsUsed > cols)   // Drop out if we have already used the max number of visible fields
                                            break;

                                        if ("MQW".Contains(db.Fields[c - 1].FieldType))       // Skip process Memo, General and Picture fields
                                            continue;

                                        // Convert the field value to a token so we can more
                                        // easily convert to a string and handle nulls
                                        JAXObjects.Token tk = new();

                                        // Null handling goes here

                                        if (tk.Element.IsNull())
                                            tk.Element.Value = "";  // TODO - perform null handling based on property setting
                                        else
                                            tk.Element.Value = db.CurrentRow.Rows[0][c - 1].ToString() ?? "";

                                        // TODO - any special handling based on field type can go here
                                        string cItem = tk.AsString();


                                        // Add it to the combo box, List and ListItem arrays
                                        if (visibleFieldsUsed == 1)
                                        {
                                            // First field used creates a new row in the ListArray and associated dictionary
                                            // Note: A Key of 0 causes the dictionary key to be automatically created
                                            ListArray.AddItem(cItem, ++row, c, 0);
                                            UserProperties["newindex"].Element.Value = row;
                                            UserProperties["newitemid"].Element.Value = row;
                                        }
                                        else
                                        {
                                            // Fill in the remaining columns for the current ListArray row
                                            ListArray.SetElement(row, c);
                                            ListArray.Element.Value = cItem;
                                        }
                                    }
                                }

                                // Correct as there may be fewer visible fields
                                ListColumns = visibleFieldsUsed;


                                // Goto next record
                                await RowSourceDBF.DBFSkipRecord(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 7 - Files
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource7()
        {
            return await Requery7();
        }


        /* ------------------------------------------------------------
         * REQUERY 7
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery7()
        {
            int result = 0;
            string msg = "";
            try
            {
                // Load the field list for the combo box
                JAXObjects.Token fileList = await XClass_AuxCode.GetRowSource(App, UserProperties["rowsource"].AsString(), UserProperties["rowsourcetype"].AsInt());

                if (fileList.Element.Type.Equals("N"))
                    result = fileList.AsInt();
                else if (fileList.Element.Type.Equals("A"))
                {
                    for (int i = 0; i < fileList._avalue.Count; i++)
                    {
                        string cItem = fileList._avalue[i].ValueAsString;

                        if (i == 1)
                        {
                            // First field used creates a new row in the ListArray and associated dictionary
                            // Note: A Key of 0 causes the dictionary key to be automatically created
                            ListArray.AddItem(cItem, i + 1, 1, 0);
                            UserProperties["newindex"].Element.Value = i;
                            UserProperties["newitemid"].Element.Value = i;
                        }
                        else
                        {
                            // Fill in the remaining columns for the current ListArray row
                            ListArray.SetElement(i, 1);
                            ListArray.Element.Value = cItem;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                msg = ex.Message;
            }

            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 8 - Structure
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource8()
        {
            int result = 0;

            result = await Requery8();

            return result;
        }

        /* ------------------------------------------------------------
         * REQUERY 8
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery8()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 9 - JSON
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource9()
        {
            return await Requery9();
        }

        /* ------------------------------------------------------------
         * REQUERY 9
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery9()
        {
            int result = 0;
            result = 1999;
            return result;
        }

        /* ------------------------------------------------------------------------------------------
         * 10 - Collection
         * ------------------------------------------------------------------------------------------*/
        private async Task<int> RowSource10()
        {
            int result = 0;

            result = await Requery10();

            return result;
        }

        /* ------------------------------------------------------------
         * REQUERY 10
         * 
         * ------------------------------------------------------------*/
        private async Task<int> Requery10()
        {
            int result = 0;
            result = 1999;
            return result;
        }


        // Update the list from scratch
        //private void UpdateList()
        //{
        //    listBoxItems.RaiseListChangedEvents = false;

        //    if (ListSource is null)
        //    {
        //        // create a new list
        //        listBoxItems = [];
        //        ListSource = [];
        //    }
        //    else
        //    {
        //        for (int i = 0; i < ListArray.Count; i++)
        //        {
        //            JAXObjects.Token tk = new();
        //            tk.Element.Value = ListItemID._dictionary![ListArray._avalue[i].ValueAsInt]; // Return the list item token
        //            ListBoxItem lbi = new();
        //            lbi.DisplayText = tk._avalue[0].ValueAsString;
        //            lbi.ID = ListArray._avalue[i].ValueAsInt;
        //            listBoxItems.Add(lbi);
        //        }
        //    }

        //    listBoxItems.RaiseListChangedEvents = false;
        //    listBoxItems.ResetBindings();
        //}

        public int GetTopIndex()
        {
            if (lstBox == null)
                return -1;

            var scrollViewer = lstBox.GetVisualDescendants().OfType<Avalonia.Controls.ScrollViewer>().FirstOrDefault();
            if (scrollViewer == null)
                return -1;

            // Find the topmost visible container
            var panel = scrollViewer.Content as Avalonia.Controls.VirtualizingStackPanel; // or ItemsPresenter
            if (panel == null)
                return -1;

            var topContainer = panel.Children
                .OfType<Avalonia.Controls.ListBoxItem>()
                .Where(item => item.IsVisible)
                .OrderBy(item => item.Bounds.Top)
                .FirstOrDefault();

            if (topContainer != null)
            {
                // Use the non-obsolete ItemsControl method
                return lstBox.IndexFromContainer(topContainer);
            }

            return -1;
        }
    }
}
