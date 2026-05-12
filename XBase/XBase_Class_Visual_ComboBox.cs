/*
 * JAXBase - XBase_Class_Visual_ComboBox.cs
 *
 * This class implements the ComboBox control for JAXBase. It manages the properties, methods, and events
 * specific to a ComboBox, including handling the list of items and their associated data.
 * 
 * The LostFocus event does not trigger the Valid event because it happens before the selection change is 
 * completed.  Instead, the Valid event is triggered in the DropDownClosed event handler, which occurs 
 * after the selection change is finalized.
 * 
 * 
 * 2026-04-07 - JLW
 *      Getting close to the point where it's workable for many use cases and the real testing will
 *      occur when I get example code and some tools created.
 *      
 * 2026-04-10 - JLW
 *      Had to retool the JAXObjects to be able to support a sorted dictionary and mapped list of the 
 *      sorted dictionary so that I could get List (ListArray) and ListItemID properties to work as 
 *      expected.  That same code also makes it possible to support collections!
 * 
 * 2026-04-19 - JLW
 *      Finished AddItem and AddListItem methods and added the ability to add blank items up to the 
 *      nItem position if nItem is greater than the list count.  This allows for adding items at 
 *      specific positions in the list without having to first add blank items up to that position.  
 *      Also added RemoveItem and RemoveListItem methods.
 *      
 * 2026-04-20 - JLW
 *      Added Array (rowsource type 5) and Files (rowsource type 7) support for the RowSource property.
 *      Debugged ListItem and AddListItem along with returning List and ListItem if the index is 0 to 
 *      return the whole collection.  Also went through system and updated various locations that
 *      would toss an error when they met the E/M types, converting the result to an empty string, which
 *      is how VFP handles it.
 *      
 */
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ComboBox : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "ComboBox";
        public new string MyDefaultName { get; } = "combobox";


        // This list holds the row source array followed by important related values
        public ObservableSortedDictionary<int, JAXObjects.Token> ListSource = [];
        private int ListColumns = 1;
        private int BoundColumn = 1;

        // Rowsource Type 2,3,4,6,8 workarea reference
        JAXDirectDBF? RowSourceDBF = null;
        JAXObjects.Token RowSourceExtra = new("");

        public Avalonia.Controls.ComboBox CboBox => (Avalonia.Controls.ComboBox)me.avaloniaObject!;
        public JAXObjects.Token ListItemID => UserProperties["list"];       // Use dictionay key
        public JAXObjects.Token ListArray => UserProperties["listitem"];    // Use array index

        public XBase_Class_Visual_ComboBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.ComboBox(), "Combobox", "combobox", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            UserProperties["list"] = new(ListSource, "E");      // Dictionary
            UserProperties["listitem"] = new(ListSource, "M");  // Mapped List of Dictionary

            CboBox.DropDownClosed += CboBox_DropDownClosed; // Used to trigger the valid event
            return result;
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
            int val, cols;
            JAXObjects.Token objtk = new();

            // Is this property already a token?
            // Primarily for the List and ListItem properties
            if (objValue is JAXObjects.Token token)
                objtk = token;
            else
                objtk.Element.Value = objValue;

            propertyName = propertyName.ToLower();

            if (UserProperties.TryGetValue(propertyName, out JAXObjects.Token? value) && value.Protected)
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

                        case "boundcolumn":
                            if (objtk.Element.Type.Equals("N"))
                            {
                                val = objtk.AsInt();
                                if (val < 1 || val > ListColumns) throw new Exception("31|");

                                // Are there rows in the ListItemArray?
                                if (BoundColumn != val && val > 0 && ListItemID._avalue.Count > 0)
                                {
                                    // Clear out the items
                                    CboBox.Items.Clear();

                                    // Add the new bound column
                                    for (int r = 0; r < ListItemID._avalue.Count; r++)
                                    {
                                        JAXObjects.Token row = (JAXObjects.Token)ListItemID._avalue[r].Value;
                                        CboBox.Items.Add(row._avalue[val - 1].ValueAsString);
                                    }
                                    // Set to the bound column to the first list item
                                    CboBox.SelectedIndex = 0;
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
                                    CboBox.SelectedIndex = -1;
                                else if (JAXLib.Between(objtk.AsInt(), 1, CboBox.ItemCount))
                                    CboBox.SelectedIndex = objtk.AsInt() - 1;
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
                                    CboBox.MaxHeight = objtk.AsInt();
                            }
                            else
                                result = 11;
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
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);
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
                //returnToken.CopyFrom(value);
                switch (propertyName)
                {
                    // Intercept special handling of properties
                    case "columncount":
                        returnToken.Element.Value = ListColumns;
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
                        returnToken.Element.Value = CboBox.Items.Count;
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
                        returnToken.Element.Value = CboBox.SelectedIndex + 1;
                        break;

                    case "sorted":
                        //returnToken.Element.Value = CboBox.Sorted; // Note: Avalonia ComboBox does not have a built-in Sorted property; implement custom sorting if needed
                        break;

                    case "text":
                        returnToken.Element.Value = CboBox.Text ?? "";
                        break;

                    default:
                        // Process standard properties
                        returnToken = await base.GetProperty(propertyName, idx);
                        result = returnToken.Element.IsNull() ? 1 : 0;
                        break;
                }

                // Just catch the known result codes
                if (JAXLib.InList(result, 1, 9))
                {
                    if (result < 9)
                        returnToken.CopyFrom(UserProperties[propertyName]);

                    // We get the value, not the reference
                    result = 0;
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, propertyName, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{propertyName}", string.Empty);

                returnToken.Element.MakeNull();
            }

            return returnToken;
        }


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

                                    // Place it where it's being assigned
                                    if (UserProperties["rowsourcetype"].AsInt() < 2)
                                        CboBox.Items.Insert(nItem.AsInt() - 1, cItem.AsString());
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
                                {
                                    ListItemID.AddItemID(cItem.AsString(), ItemID.AsInt(), Column.AsInt());

                                    if (Column.AsInt() < 2)
                                        CboBox.Items.Add(cItem.AsString());
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
                                    CboBox.Items.RemoveAt(iItem - 1);
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
                                            CboBox.Items.Remove(i);
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
            CboBox.Items.Clear();
            CboBox.SelectedIndex = -1;
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
                for (int i = 0; i < CboBox.Items.Count; i++)
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
                for (int i = 0; i < CboBox.Items.Count; i++)
                {
                    oItem.Element.Value = CboBox.Items[i]!;
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
                        CboBox.SelectedIndex = -1;
                        CboBox.Items.Clear();
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
                        CboBox.SelectedIndex = -1;
                        CboBox.Items.Clear();
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

        /*------------------------------------------------------------------------------------------*
         * This is where we want to call the valid event in a dropdown
         *------------------------------------------------------------------------------------------*/
        private void CboBox_DropDownClosed(object? sender, EventArgs e)
        {
            if (CboBox.IsDropDownOpen == false)
            {
                if (Methods["valid"].CompiledCode.Length > 0)
                {
                    // Check the valid clause
                    _CallMethod("valid").Wait();

                    if (App.ReturnValue.Element.Type.Equals("L"))
                    {
                        me.Validated = App.ReturnValue.AsBool();        // Did it validate?
                        me.ValidMoveDirection = me.MoveDirection;
                    }
                    else if (App.ReturnValue.Element.Type.Equals("N"))
                    {
                        me.Validated = App.ReturnValue.AsInt() != 0;    // 0 is not validated, anything else is validated
                        me.ValidMoveDirection = App.ReturnValue.AsInt(); // Set the move direction
                    }
                }
                else
                    me.Validated = true;    // If no code, it's valid by default
            }
        }

        public override void CleanUp(bool disposing)
        {
            base.CleanUp(disposing);

            // Close things up and clear them out being careful
            // not to try to perform against a null object
            if (RowSourceDBF is not null)
            {
                if (RowSourceDBF.DbfInfo is not null)
                {
                    if (RowSourceDBF.DbfInfo.DBFStream is not null)
                    {
                        RowSourceDBF.DBFClose().Wait();
                    }
                }

                RowSourceDBF = null;
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
                "left,N,0","list,,","listcount,n!,0","listindex,n,0","listitem,,","listitemid,n,0",
                "margin,n,2","maxlength,n,200","maxheight,n,-1","maxwidth,n,-1","minheight,n,-1","minwidth,n,-1",
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