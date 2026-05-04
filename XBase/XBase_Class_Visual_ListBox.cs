/*
 * Listbox
 * 
 * 2026-04-22 - JLW 
 * 
 */
using System.ComponentModel;
using JAXBase.Core;
using JAXBase.Utilities;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_ListBox : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "ListBox";
        public new string MyDefaultName { get; } = "listbox";


        // Listbox item that holds DisplayValue & id
        private class ListBoxItem
        {
            public int ID = 0;
            private string _displayText = string.Empty;
            public string DisplayText
            {
                get => _displayText;
                set => _displayText = value;
            }

            // Sort-friendly version (used internally by comparer or BindingSource)
            public string SortKey => DisplayText?.ToUpperInvariant() ?? string.Empty;
        }

        // Binding list to tie into listbox
        private BindingList<ListBoxItem> listBoxItems = [];
        private int listID = 1;

        // Used for sorting list
        private BindingSource bindingSource = new();

        private string searchBuffer = "";
        private DateTime lastKeyTime = DateTime.MinValue;


        public Avalonia.Controls.ListBox lstBox => (Avalonia.Controls.ListBox)me.avaloniaObject!;

        public XBase_Class_Visual_ListBox(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            SetVisualObject(new Avalonia.Controls.ListBox(), "ListBox", "list", true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            if (InInit)
            {
                bindingSource.DataSource = listBoxItems;

                lstBox.DisplayMemberBinding = new Avalonia.Data.Binding("Name");
                lstBox.SelectedValueBinding = new Avalonia.Data.Binding("ID");
                lstBox.ItemsSource = listBoxItems;

                listBoxItems.AllowNew = false;
                listBoxItems.AllowEdit = false;
                listBoxItems.AllowRemove = false;

                lstBox.KeyDown -= listBox_KeyPress;
                lstBox.KeyDown += listBox_KeyPress;
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
            JAXObjects.Token tk = new(objValue);

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
                            break;

                        case "borderwidth":
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

                        case "listcount":
                            break;

                        case "listindex":
                            break;

                        case "listitem":
                            break;

                        case "listitemid":
                            break;

                        case "moverbars":
                            break;

                        case "multiselect":
                            if (tk.Element.Type.Equals("L"))
                                lstBox.SelectionMode = tk.AsBool() ? Avalonia.Controls.SelectionMode.Multiple : Avalonia.Controls.SelectionMode.Single;
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
                            break;

                        case "rowsourcetype":
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
                            if (tk.Element.Type.Equals("L"))
                            {
                                if (tk.AsBool())
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
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 4))
                                    objValue = tk.AsInt();
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        case "terminateread":
                            break;

                        case "topindex":
                            if (tk.Element.Type.Equals("N"))
                                lstBox.ScrollIntoView(tk.AsInt());
                            else
                                result = 11;
                            break;

                        case "topitemid":
                            break;

                        case "value":
                            if (tk.Element.Type.Equals("C"))
                                lstBox.SelectedValue = tk.AsString();
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
                    case "autohidescrollbar":
                        break;

                    case "bordercolor":
                        break;

                    case "borderwidth":
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

                    case "listcount":
                        break;

                    case "listindex":
                        break;

                    case "listitem":
                        break;

                    case "listitemid":
                        break;

                    case "moverbars":
                        break;

                    case "multiselect":
                        returnToken.Element.Value = lstBox.SelectionMode == Avalonia.Controls.SelectionMode.Multiple;
                        break;

                    case "newindex":
                        break;

                    case "newitemid":
                        break;

                    case "numberofelements":
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

                    case "selecteditembackcolor":
                        break;

                    case "selecteditemforecolor":
                        break;

                    case "sorted":
                        returnToken.Element.Value = string.IsNullOrWhiteSpace(bindingSource.Sort) == false;
                        break;

                    case "topindex":
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
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]); //returnToken.Element.Value = UserProperties[propertyName].Element.Value;
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
                "BaseClass,C!,listbox","bordercolor,n,6579300","borderwidth,n,0",
                "Class,C!,listbox","ClassLibrary,C!,","Comment,C,","controlsource,c,",
                "disabledbackcolor,R,255|255|255","disabledforecolor,R,109|109|109","disableditembackcolor,R,255|255|255",
                "disableditemforecolor,R,109|109|109","displayvalue,c,",
                "Enabled,L,true",
                "firstelement,n,1","FontBold,L,false","FontItalic,L,false",
                "FontName,C,Arial","FontSize,N,9","FontStrikeThrough,L,false","FontUnderline,L,false","forecolor,R,0",
                "Height,N,0",
                "incrementsearch,l,true","itemdata,n,0","itemforecolor,R,0","itemiddata,n,0",
                "left,N,0","list,#,","listcount,n,0","listindex,n,0","listitem,c,","listitemid,n,0",
                "moverbars,L,.F.","multiselect,L,.F.",
                "name,c,","newindex,n,0","newitemid,n,0","numberofelements,n,0",
                "parent,o!,","parentclass,C!,",
                "righttoleft,L,false","rowsource,c,","rowsourcetype,n,0",
                "selected,l,false","selectedid,l,false","selecteditembackcolor,R,0|120|215","selecteditemforecolor,R,255|255|255",
                "sorted,l,false","sorttype,n,",
                "tabindex,n,0","tabstop,l,true","tag,c,","terminateread,l,.F.","top,N,0","topindex,n,1","topitemid,n,-1","tooltiptext,c,",
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

            try
            {
                switch (methodName.ToLower())
                {
                    case "additem":
                    case "addlistitem":
                        if (App.ParameterClassList.Count != 1)
                        {
                            _AddError(6500, 0, methodName, methodName);
                            result = 6999;
                        }
                        else
                        {
                            JAXObjects.Token tk = await AppHelper.GetParameterToken(null);
                            if (tk.TType.Equals("A"))
                            {
                                listBoxItems.RaiseListChangedEvents = false;

                                if (tk.Row == 0)
                                {
                                    // 1D array
                                    for (int i = 0; i < tk.Col; i++)
                                    {
                                        ListBoxItem lbi = new();
                                        lbi.DisplayText = tk._avalue[i].ValueAsString;
                                        lbi.ID = listID++;
                                        listBoxItems.Add(lbi);
                                    }
                                }
                                else
                                {
                                    // 2D array
                                    for (int i = 0; i < tk.Row; i++)
                                    {
                                        tk.SetElement(i + 1, 1);
                                        ListBoxItem lbi = new();
                                        lbi.DisplayText = tk._avalue[i].ValueAsString;
                                        lbi.ID = listID++;
                                        listBoxItems.Add(lbi);
                                    }
                                }

                                listBoxItems.RaiseListChangedEvents = false;
                                listBoxItems.ResetBindings();
                            }
                            else
                            {
                                // Add a simple token value
                                ListBoxItem lbi = new();
                                lbi.DisplayText = tk.AsString();
                                lbi.ID = listID++;
                                listBoxItems.Add(lbi);
                            }
                        }
                        break;

                    case "removeitem":
                    case "removelistitem":
                        if (App.ParameterClassList.Count != 1)
                        {
                            _AddError(6500, 0, methodName, methodName);
                            result = 6999;
                        }
                        else
                        {
                            JAXObjects.Token tk =await AppHelper.GetParameterToken(null);
                            if (tk.TType.Equals("A"))
                            {
                                if (tk.Row == 0)
                                {
                                    // 1D array
                                    for (int i = 0; i < tk.Col; i++)
                                    {
                                        if (tk.Element.Type.Equals("N") && JAXLib.Between(tk.AsInt(), 1, lstBox.Items.Count))
                                            listBoxItems.RemoveAt(tk.AsInt() - 1); //lstBox.Items.RemoveAt(tk.AsInt() - 1);
                                        else
                                        {
                                            // Remove by name - Case Sensitive?
                                            for (int j = 0; j < listBoxItems.Count; j++)
                                            {
                                                if (listBoxItems[j].DisplayText.Equals(tk.AsString()))
                                                {
                                                    listBoxItems.RemoveAt(j);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // 2D array
                                    for (int i = 0; i < tk.Row; i++)
                                    {
                                        tk.SetElement(i + 1, 1);
                                        if (tk.Element.Type.Equals("N") && JAXLib.Between(tk.AsInt(), 1, lstBox.Items.Count))
                                            listBoxItems.RemoveAt(tk.AsInt() - 1);  //lstBox.Items.RemoveAt(tk.AsInt() - 1);
                                        else
                                        {
                                            // Remove by name - Case Sensitive?
                                            for (int j = 0; j < listBoxItems.Count; j++)
                                            {
                                                if (listBoxItems[j].DisplayText.Equals(tk.AsString()))
                                                {
                                                    listBoxItems.RemoveAt(j);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // If a number is sent and it's in the range of the item count
                                // then remove the object at that item count location (1 based)
                                if (tk.Element.Type.Equals("N") && JAXLib.Between(tk.AsInt(), 1, lstBox.Items.Count))
                                    lstBox.Items.RemoveAt(tk.AsInt() - 1);
                                else
                                    lstBox.Items.Remove(tk.AsString());
                            }
                        }
                        break;

                    default:
                        result = await base.DoDefault(methodName);
                        break;
                }
            }
            catch (Exception ex)
            {
                result = 9999;
                _AddError(result, 0, $"Error in ListBox DoDefault {methodName} - {ex.Message}", App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
            }

            return result;
        }


        // Default keypress event
        private void listBox_KeyPress(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.KeySymbol is null && e.Key != Avalonia.Input.Key.Back)
                return;

            // Is there JAXCode to execute first?
            if (System.String.IsNullOrWhiteSpace(Methods["keypress"].CompiledCode) == false)
            {
                // set up parameters and call the method
            }

            if (e.Key != Avalonia.Input.Key.Back && (e.KeySymbol == null || e.KeySymbol.Length != 1 || !System.Char.IsLetterOrDigit(e.KeySymbol[0])))
                return;  // optional: ignore non-alphanum

            e.Handled = true;  // suppress default prefix-jump

            if ((System.DateTime.Now - lastKeyTime).TotalSeconds > 1.5)
                searchBuffer = "";  // timeout → reset

            if (e.Key == Avalonia.Input.Key.Back)
            {
                if (searchBuffer.Length > 0)
                    searchBuffer = searchBuffer.Substring(0, searchBuffer.Length - 1);
            }
            else
            {
                searchBuffer += System.Char.ToUpper(e.KeySymbol![0]);
            }

            lastKeyTime = System.DateTime.Now;

            // Find first match (prefix search, case-insensitive – exact WinForms ListBox.FindString behavior)
            if (System.String.IsNullOrEmpty(searchBuffer))
                return;  // WinForms FindString("") returns -1 → no selection change

            // Use the original collection (listBoxItems) for correct indexing
            System.Collections.IList? source = listBoxItems as System.Collections.IList ?? lstBox.ItemsSource as System.Collections.IList;
            if (source == null || source.Count == 0)
                return;

            int foundIndex = -1;
            object? foundItem = null;

            for (int i = 0; i < source.Count; i++)
            {
                object? item = source[i];
                if (item == null) continue;

                // Respect DisplayMemberBinding = new Binding("Name")
                dynamic dynItem = item;
                string? displayText = dynItem.Name?.ToString();

                if (displayText != null &&
                    displayText.StartsWith(searchBuffer, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundIndex = i;
                    foundItem = item;
                    break;
                }
            }

            if (foundIndex >= 0 && foundItem != null)
            {
                lstBox.SelectedIndex = foundIndex;
                lstBox.ScrollIntoView(foundItem);   // Avalonia equivalent of TopIndex
            }
        }
    }
}
