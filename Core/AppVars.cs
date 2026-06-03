using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Core
{
    public static class AppVars
    {


        /*-------------------------------------------------------------*
         * 
         * Sets up the var as local if it's not already a local var
         * 
         *-------------------------------------------------------------*/
        public static void MakeLocalVar(string varName, int row, int col, bool alterArray)
        {
            JAXObjects.Token tk;
            varName = varName.ToLower();

            if (JAXLib.InList(varName, "this", "thisform", "thisformset"))
            {
                // Can only set if it's not already in existance
                tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);
                if (tk.TType.Equals("U") == false)
                    throw new Exception("2405|" + varName.ToUpper());
            }

            if (row < 0 || col < 0)
            {
                // It's a simple var
                row = 1;
                col = 1;
            }

            if (row > 0 && col < 1)
            {
                // Set up 1D array settings
                col = row;
                row = 0;
            }

            // make sure col is at least 1
            if (col < 1) col = 1;

            // Is it a memory Var reference?  Strip m. if it is
            if (varName.Length > 2 && varName[..2].Equals("m.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            // Is it a legal var name?  First non underscore char must be a letter
            if (JAXLib.Between(varName.Replace("_", "")[..1].ToLower(), "a", "z") == false)
                throw new Exception(string.Format("225|{0}", varName));

            // Vars names may only contain letters, numbers, or underscores
            if (JAXLib.ChrTran(varName.ToLower(), "abcdefghijklmnopqrstuvwxyz0123456789_", "").Length > 0)
                throw new Exception(string.Format("225|{0}", varName));

            // Check local private variables
            tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrivateVars.GetToken(varName);

            // Check local variables
            if (tk.TType.Equals("U"))
            {
                // Not found anywhere so add it to the private sector
                // of the current level and if it's an array, tell it
                // that it's ok to set the dimensions
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.SetDimension(varName, row, col, true);
            }
            else
                throw new Exception("1208|" + varName);
        }

        /*-------------------------------------------------------------*
         * 
         * Sets up the var as public if it's not already a local var
         * 
         *-------------------------------------------------------------*/
        public static async Task MakePublicVar(string varName, int row, int col, bool alterArray)
        {
            JAXObjects.Token tk;
            varName = varName.ToLower();

            if (JAXLib.InList(varName, "this", "thisform", "thisformset"))
                throw new Exception("2403|" + varName.ToUpper());

            if (row > 0 && col < 1)
            {
                // Set up 1D array settings
                col = row;
                row = 0;
            }

            if (row < 0 && col < 0)
            {
                row = 1;
                col = 1;
            }

            // Is it a memory Var reference?  Strip m. if it is
            if (varName.Length > 2 && varName[..2].Equals("m.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            // Is it a legal var name?  First non underscore char must be a letter
            if (JAXLib.Between(varName.Replace("_", "")[..1].ToLower(), "a", "z") == false)
                throw new Exception(string.Format("225|{0}", varName));

            // Vars names may only contain letters, numbers, or underscores
            if (JAXLib.ChrTran(varName.ToLower(), "abcdefghijklmnopqrstuvwxyz0123456789_", "").Length > 0)
                throw new Exception(string.Format("225|{0}", varName));

            // Does this var exist?
            tk = await GetVarToken(varName);
            //GetVar(varName, out tk);

            // Check private variables (Globals are AppLevel[0] private vars
            if (tk.TType.Equals("U"))
            {
                // Not found so add it to the private sector of level 0
                // and if it's an array, set the dimensions if allowed
                Program.CurrentApp.AppLevels[0].PrivateVars.SetDimension(varName, row, col, alterArray);
            }
            else
                throw new Exception("1208|" + varName);
        }


        /*-------------------------------------------------------------*
         * Sets up the var as private if it's not already a public var 
         * and copies the entire contents of the JAXObjects.Token 
         * into the var.
         *-------------------------------------------------------------*/
        public static void SetVarOrMakePrivate(string varName, JAXObjects.Token tk)
        {
            SetVarOrMakePrivate(varName, 1, 1, false);
            SetVar(varName, tk);
        }

        /*-------------------------------------------------------------*
         * Sets up the var as private if it's not already available
         * Returns -1 =Local, 0=Public, >1 =Private level
         * indicating where the variable is located
         *
         * If makeArray is true, it will force the variable to be an 
         * array if element count > 1 and if false, it will be an 
         * array only if the element count > 1
         *-------------------------------------------------------------*/
        public static int SetVarOrMakePrivate(string varName, int row, int col, bool alterArray)
        {
            int iResult = 0;
            JAXObjects.Token tk;
            varName = varName.ToLower();

            if (JAXLib.InList(varName, "this", "thisform", "thisformset"))
                throw new Exception("2404|" + varName.ToUpper());

            // 1D arrays may come in as col < 1
            if (row > 0 && col < 1)
            {
                // Set up 1D array settings
                col = row;
                row = 0;
            }

            // make sure row & col are at least set to 1
            row = row < 0 ? 0 : row;
            col = col < 1 ? 1 : col;

            // Is it a memory Var reference?  Strip m. if it is
            if (varName.Length > 2 && varName[..2].Equals("m.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            // Legal var name?  First non underscore char must be a letter
            if (JAXLib.Between(varName.Replace("_", "")[..1].ToLower(), "a", "z") == false)
                throw new Exception(string.Format("225|{0}", varName));

            // Vars names only contain letters, numbers, or underscores
            if (JAXLib.ChrTran(varName.ToLower(), "abcdefghijklmnopqrstuvwxyz0123456789_", "").Length > 0)
                throw new Exception(string.Format("225|{0}", varName));

            // Check local variables
            tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);

            // Check private variables
            if (tk.TType.Equals("U"))
            {
                // Check all the private vars
                int i = Program.CurrentApp.CurrentAppLevel;
                while (i >= 0)
                {
                    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                    if (tk.TType.Equals("U") == false)
                    {
                        // Found it in the private vars of an app level
                        Program.CurrentApp.AppLevels[i].PrivateVars.SetDimension(varName, row, col, alterArray);
                        AppIO.DebugLog($"Set dimensions of {varName} to {row},{col} in AppLevel {i}", true);
                        iResult = i;
                        break;
                    }

                    if (i > Program.CurrentApp.AppLevels[i].CallingLevel)
                        i = Program.CurrentApp.AppLevels[i].CallingLevel;
                    else
                    {
                        AppIO.DebugLog($"***ERROR*** SETVARORMAKEPRIVATE - Current level is {i} and calling level is {Program.CurrentApp.AppLevels[i].CallingLevel}");
                        i = -1;
                    }
                }

                //for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
                //{
                //    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                //    if (tk.TType.Equals("U") == false)
                //    {
                //        // Found it in the private vars of an app level
                //        Program.CurrentApp.AppLevels[i].PrivateVars.SetDimension(varName, row, col, alterArray);
                //        AppIO.DebugLog($"Set dimensions of {varName} to {row},{col} in AppLevel {i}", true);
                //        iResult = i;
                //        break;
                //    }
                //}

                if (tk.TType.Equals("U"))
                {
                    // Not found anywhere so add it to the private
                    // sector of the current level
                    Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].PrivateVars.SetDimension(varName, row, col, alterArray);
                    AppIO.DebugLog($"Set dimensions of {varName} to {row},{col} in AppLevel {Program.CurrentApp.CurrentAppLevel}", true);
                    iResult = Program.CurrentApp.AppLevels.Count - 1;
                }
            }
            else
            {
                // Found it in the local variables of the current app level
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.SetDimension(varName, row, col, alterArray);
                AppIO.DebugLog($"Set dimensions of {varName} to {row},{col} in AppLevel {Program.CurrentApp.CurrentAppLevel}");
                iResult = -1;
            }

            return iResult;
        }





        /*-------------------------------------------------------------*
         * Get a variable from the App local/private, and global
         * stacks along with fields from the current workarea
         *
         * Precedence is:
         *      "table.", "a." - ".j", or "m." memory variables
         *      object.property
         *      current workarea fields
         *      local
         *      private
         *      global
         *
         *-------------------------------------------------------------*/
        public static async Task<JAXObjects.Token> GetVarToken(string varName) { return await GetVarToken(varName, false); }

        public static async Task<JAXObjects.Token> GetVarToken(string varName, bool fillAbsentData)
        {
            JAXObjects.Token tk;
            varName = varName.ToLower();

            // If it's a "M." variable, skip checking the local work area
            if (varName.Length > 2 && varName[..2].Equals("M.", StringComparison.OrdinalIgnoreCase))
            {
                // Strip the m. from the name
                varName = varName[2..];

                // Check local variables
                tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);
            }
            else
            {
                // Is it a Table.Field reference?
                if (varName.Contains('.'))
                {
                    // TODO - Get the table and field name
                    string[] varParts = varName.Split('.');

                    if (Program.CurrentApp.CurrentDS.IsWorkArea(varParts[0]))
                    {

                        // if tbl is a -> j then it's a work area reference
                        int wa = "abcdefghij".IndexOf(varParts[0], StringComparison.CurrentCultureIgnoreCase);

                        if (wa < 0)
                        {
                            // It's a table reference
                            tk = await Program.CurrentApp.CurrentDS.GetFieldToken(varParts[0], varParts[1], fillAbsentData);
                        }
                        else
                        {
                            // It's a work area refreence
                            tk = await Program.CurrentApp.CurrentDS.GetFieldToken(wa, varParts[1], fillAbsentData);
                        }
                    }
                    else
                    {
                        // Assume it's an object and we're using the wrong tool
                        tk = new() { TType = "U" };
                    }
                }
                else
                {
                    // is there a table open in this workarea?
                    if (Program.CurrentApp.CurrentDS.FieldExists(varName))
                    {
                        // Check the current open table
                        tk = await Program.CurrentApp.CurrentDS.GetFieldToken(-1, varName, fillAbsentData);
                    }
                    else
                    {
                        tk = new() { TType = "U" };
                    }
                }

                // Check local variables
                if (tk.TType.Equals("U")) tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);
            }

            // Check private variables (Globals are AppLevel[0] private vars)
            if (tk.TType.Equals("U"))
            {
                // Follow the applevel linked list back to 0
                int i = Program.CurrentApp.CurrentAppLevel;
                while (i >= 0)
                {
                    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                    if (tk.TType.Equals("U") == false)
                        break;  // FOUND IT!


                    if (Program.CurrentApp.AppLevels[i].CallingLevel >= 0)
                    {
                        if (i > Program.CurrentApp.AppLevels[i].CallingLevel)
                            i = Program.CurrentApp.AppLevels[i].CallingLevel;
                        else
                        {
                            AppIO.DebugLog($"***ERROR*** GETVARTOKEN - Current level is {i} and calling level is {Program.CurrentApp.AppLevels[i].CallingLevel}");
                            i = 0;
                        }
                    }
                    else
                        i--;
                }

                //// Check all the private vars
                //for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
                //{
                //    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                //    if (tk.TType.Equals("U") == false)
                //        break;   // Found it!
                //}
            }

            // Make sure this, thisform, and thisformset are used correctly
            if (JAXLib.InListC(varName, "this", "thisform", "thisformset"))
            {
                switch (varName)
                {
                    case "thisformset":
                        if ("X".Contains(tk.TType))
                            throw new Exception("2402|");
                        break;

                    case "this":
                        if ("X".Contains(tk.TType))
                            throw new Exception("2400|");
                        break;

                    default:
                        if ("X".Contains(tk.TType))
                            throw new Exception("2401|");
                        break;
                }
            }

            return tk;
        }


        /*-------------------------------------------------------------*
         * Specifically override with a new token
         *-------------------------------------------------------------*/
        public static void SetVar(string varName, JAXObjects.Token newTK)
        {
            JAXObjects.Token tk;

            varName = varName.Trim().ToLower();

            // Is it a memory Var reference?  Strip m. if it is
            if (varName.Length > 2 && varName[..2].Equals("M.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            if (JAXLib.InList(varName, "this", "thisform", "thisformset"))
            {
                // Can only set if it's not already in existance
                tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);
                if (tk.TType.Equals("U") == false)
                    throw new Exception("2405|" + varName.ToUpper());
            }

            // Check local variables
            tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);

            // Check private variables (Globals are AppLevel[0] private vars]
            if (tk.TType.Equals("U"))
            {
                // Follow the applevel linked list back to 0
                int i = Program.CurrentApp.CurrentAppLevel;
                while (i >= 0)
                {
                    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                    if (tk.TType.Equals("U") == false)
                    {
                        AppIO.DebugLog($"Set private var {varName} in AppLevel {i} to {newTK.Element.Value}");
                        Program.CurrentApp.AppLevels[i].PrivateVars.SetToken(varName, newTK);
                        break;   // Found it!
                    }

                    if (i > Program.CurrentApp.AppLevels[i].CallingLevel)
                        i = Program.CurrentApp.AppLevels[i].CallingLevel;
                    else
                    {
                        AppIO.DebugLog($"***ERROR*** SETVAR - Current level is {i} and calling level is {Program.CurrentApp.AppLevels[i].CallingLevel}");
                        i = 0;
                    }
                }

                //// Check all the private vars
                //for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
                //{
                //    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                //    if (tk.TType.Equals("U") == false)
                //    {
                //        AppIO.DebugLog($"Set private var {varName} in AppLevel {i} to {newTK.Element.Value}");
                //        Program.CurrentApp.AppLevels[i].PrivateVars.SetToken(varName, newTK);
                //        break;   // Found it!
                //    }
                //}
            }
            else
            {
                // Set the local variable
                AppIO.DebugLog($"Set local var {varName} in AppLevel {Program.CurrentApp.CurrentAppLevel} to {newTK.Element.Value}");
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.SetToken(varName, newTK);
            }
        }




        /*-------------------------------------------------------------*
         * This routine is not to be used except for setting local 
         * vars for the system
         * 
         * Such as THIS, THISFORM, THISFORMSET, etc
         *-------------------------------------------------------------*/
        public static void SetLocalSystemVar(string varName, object? obj, int row, int col, bool alterArray)
        {
            // Check local variables
            if (varName.Length > 3 && varName[..2].Equals("m.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            JAXObjects.Token tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);

            if (tk.TType.Equals("U"))
            {
                // Not found so add it to the local current level
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.SetDimension(varName, row, col, alterArray);
            }

            SetVar(varName, obj, row, col);
        }




        /**************************************************************
         * This routine takes a string which is expected to hold a 
         * variable reference and if it contains a ( or [ then assumes 
         * it's an array variable and solves for what's in the brackets
         * and returns a string in the format of
         *      var[x] or var[x,y]
         * where x and y are expected to be valid numeric values for 
         * that array
         * 
         * If ( or [ are not found, it just returns the string 
         * expecting it to be just a simple variable reference.
         * 
         * The OUT is a class that contains the variable name, row, 
         * and column values. The "col" value is zero if only one 
         * dimension is given
         **************************************************************/
        public static async Task<VarRef> SolveVariableReference(string varString)
        {
            VarRef vref = new();
            varString = varString.ToLower();

            if (varString.Length > 3 && varString[..2].Equals("m.", StringComparison.OrdinalIgnoreCase))
                varString = varString[2..];

            if (varString[^1] == '.')
                throw new Exception("10|");

            if (varString.Contains('[') || varString.Contains('('))
            {
                // Indications are that we have an array variable
                while (true)
                {
                    if ("[(".Contains(varString[0]))
                        break;
                    else
                    {
                        vref.varName += varString[0];
                        varString = varString[1..];
                    }
                }

                // We have the array variable, now solve for what's
                // inside the dimension brackets and strip the
                // outside brackets
                varString = varString[1..];
                varString = varString[..^1];

                // look for the comma
                string sRow = string.Empty;
                string sCol = string.Empty;
                string quote = string.Empty;

                while (string.IsNullOrEmpty(varString) == false)
                {
                    if (string.IsNullOrEmpty(quote) && "([\"'".Contains(varString[0]))
                    {
                        sRow += varString[0].ToString();
                        quote = varString[0] switch
                        {
                            '[' => "]",
                            '(' => ")",
                            _ => varString[0].ToString(),
                        };
                    }
                    else if (quote.Equals(varString[0]))
                    {
                        // found the other side of the quoted material
                        sRow += varString[0].ToString();
                        quote = string.Empty;
                    }
                    else if (string.IsNullOrEmpty(varString) || varString[0].Equals(','))
                    {
                        // Found the comma!
                        if (varString[0].Equals(','))
                        {
                            if (varString.Length > 1)
                                sCol = varString[1..].Trim();
                            else
                                throw new Exception("10|");
                        }
                        break;
                    }
                    else
                        sRow += varString[0];

                    // strip the leading character
                    varString = varString[1..];
                }

                // Solve for sRow & sCol
                if (string.IsNullOrWhiteSpace(sRow) == false)
                {
                    GenericClass st = await Program.CurrentApp.JaxMath.SolveMath(sRow);
                    if (st.Value.AsInt() < 1)
                        throw new Exception("31|");
                    else
                        vref.row = st.Value.AsInt();
                }

                if (string.IsNullOrWhiteSpace(sCol) == false)
                {
                    GenericClass st = await Program.CurrentApp.JaxMath.SolveMath(sCol);  // skip the comma
                    if (st.Value.AsInt() < 0)
                        throw new Exception("31|");
                    else
                        vref.col = st.Value.AsInt();
                }
            }
            else
            {
                // Just a simple variable
                vref.varName = varString;
            }

            return vref;
        }



        /*
         * Save an object to a  expression
         */
        public static async Task<string> SetVarFromExpression(string expr, object? obj, bool createVar)
        {
            AppIO.DebugLog($"Processing {expr} in {System.Reflection.MethodBase.GetCurrentMethod()!.Name}");

            string result = string.Empty;

            // Macro expansion
            if (expr.Contains('&'))
                expr = await JAXMacroHandler.Expand(Program.CurrentApp, expr);

            if (JAXLib.InListC(expr, ".null.", "null"))
                throw new Exception("10|.NULL.");

            // Get the var parts
            List<string> objList = BreakVar(expr);

            if (objList.Count > 0)
            {
                int withStack = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count;

                // Get the current with stack expression
                if (string.IsNullOrWhiteSpace(objList[0]))
                {
                    if (withStack > 0)
                    {
                        // This may be a layered with so we'll be inserting
                        // into objList[0] as long as the we keep finding
                        // variables starting with a period.
                        while (withStack > 0)
                        {
                            // if objList[0] is not empty then the previous with
                            // stack item started with a period, so that means we
                            // have to add another to the front of the list
                            if (string.IsNullOrWhiteSpace(objList[0]) == false)
                                objList.Insert(0, string.Empty);

                            // Get the stacked item and break it up if it's
                            // a multipart variable.  Then add all parts to 
                            // the front of the objList
                            string wsitem = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[withStack - 1];
                            string[] wsitems = wsitem.Split('.');

                            // Add last to first to keep it in the right order
                            for (int i = wsitems.Length; i > 0; i--)
                            {
                                if (string.IsNullOrWhiteSpace(objList[0]))
                                    objList[0] = wsitems[i - 1];            // Always blank on first iteration
                                else
                                    objList.Insert(0, wsitems[i - 1]);
                            }

                            withStack--;

                            if (objList[0][0] != '.')
                                break;

                            if (withStack < 0)
                                throw new Exception($"2300|Top of with stack is {Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[0]}");
                        }
                    }
                    else
                        throw new Exception($"2301|There is nothing on the with stack for .{objList[1]}");
                }

                // Now resolve it
                VarRef var = await SolveVariableReference(objList[0]);
                JAXObjects.Token currentVar;

                if (objList.Count > 1)
                {
                    currentVar = await GetVarFromExpression(objList[0], null);

                    // Is it an object?
                    if (currentVar.Element.Type.Equals("O") == false)
                        throw new Exception("1924|" + var.varName);

                    // Object Ref
                    JAXObjectWrapper? thisObject = (JAXObjectWrapper)currentVar.Element.Value;

                    for (int i = 1; i < objList.Count - 1; i++)
                    {
                        // Is the next list item an array or method call?
                        if (objList[i].Contains('(') || objList[i].Contains('['))
                        {
                            // We expect to be going after an array/object
                            currentVar = await GetVarFromExpression(objList[i], (JAXObjectWrapper)currentVar.Element.Value);

                            // TODO - Take a bite later - UDF or Array?
                        }
                        else
                        {
                            // Get the next item in the list - must be an object
                            string member = await thisObject.IsMember(objList[i]);
                            if (member.Equals("O") == false)
                                throw new Exception("1924|" + var.varName);

                            int f = await thisObject.FindObjectByName(objList[i]);
                            if (f < 0)
                                throw new Exception("9999|SetVarFromExpr|Failed to find a known object");

                            currentVar = await thisObject.GetProperty("objects", f);
                        }

                        // It must be an object
                        if (currentVar.Element.Type.Equals("O") == false)
                            throw new Exception("1924|" + var.varName);

                        // Store the object
                        thisObject = (JAXObjectWrapper)currentVar.Element.Value;
                    }

                    // Create a token to for the sent value
                    JAXObjects.Token val = new();

                    if (obj is null)
                        val.Element.MakeNull();
                    else
                        val.Element.Value = obj!;

                    VarRef lastVar = await SolveVariableReference(objList[^1]);

                    string memberType = (await thisObject.IsMember(lastVar.varName)).ToUpper();
                    switch (memberType)
                    {
                        case "U":   // Unknown
                            throw new Exception("1924|" + objList[^1]); // not found    

                        case "E":   // Event
                        case "M":   // Method
                        case "O":   // Object
                            throw new Exception("1737|"); // not a property

                        case "P":   // Property
                            JAXObjects.Token tk = await thisObject.GetProperty(lastVar.varName);

                            if (tk.TType.Equals("S"))
                                await thisObject.SetProperty(lastVar.varName, obj!);  // What about arrays and nulls???
                            else
                            {
                                // Handle A/M/S types here by getting the token and updating it
                                switch (tk.TType)
                                {
                                    case "A":       // Array
                                        // TODO - I believe here is where we might see ArrayVar=0 in which
                                        //        case we would set the entire array to that value
                                        if (lastVar.row < 1 || lastVar.col < 1)
                                            throw new Exception("5");

                                        if (lastVar.row > tk.Row)
                                            throw new Exception("5");

                                        if (lastVar.col > tk.Col)
                                            throw new Exception("5");

                                        break;

                                    case "E":       // Dictionary
                                        if (lastVar.row < 1 || lastVar.col < 1)
                                            throw new Exception("5");

                                        if (lastVar.row > tk._dictionary!.Count)
                                            throw new Exception("5");

                                        if (tk._dictionary!.ContainsKey(lastVar.row) == false)
                                            throw new Exception("5");

                                        if (lastVar.col > tk._dictionary[lastVar.row]._avalue.Count)
                                            throw new Exception("5");

                                        tk._dictionary![lastVar.row]._avalue[lastVar.col].Value = obj!;
                                        break;

                                    case "M":       // Mapped List of Dictionary
                                        if (lastVar.row < 1 || lastVar.col < 1)
                                            throw new Exception("5");

                                        if (lastVar.row > tk._mappedList!.Count)
                                            throw new Exception("5");

                                        if (lastVar.col > tk._dictionary![tk._mappedList![lastVar.row - 1]]._avalue.Count)
                                            throw new Exception("5");

                                        tk._dictionary![tk._mappedList![lastVar.row - 1]]._avalue[lastVar.col - 1].Value = obj!;
                                        break;
                                }
                            }

                            //thisObject.SetProperty(objList[^1], val);  // What about arrays???
                            break;

                        default:
                            throw new Exception($"1559|{objList[^1].ToUpper()}");
                    }
                }
                else
                {
                    // It's a var or array reference
                    if (var.col < 1)
                    {
                        var.col = var.row;
                        var.row = 1;
                    }

                    var.col = var.col < 1 ? 1 : var.col;
                    var.row = var.row < 1 ? 1 : var.row;

                    // Is it a non-array variable?  First, make sure it exists
                    // when the createVar flag is set to true.
                    if (createVar && var.row == 1 && var.col == 1)
                        SetVarOrMakePrivate(var.varName, var.row, var.col, false);

                    // Attempt to set the object element and if
                    // something goes wrong, an error is raised.
                    AppIO.DebugLog($"Set {var.varName} to '{obj}'");
                    SetVar(var.varName, obj, var.row, var.col);
                }
            }

            return result;
        }




        /*
         * Get the var token from an expression from a simple "i" to 
         * something as complex as Form1.object[3].value
         * 
         * If the var/property does not exist, then a JAXBase error 
         * is raised.
         * 
         * Objects cause this routine to call itself recursively 
         * until the desired property is located.
         * 
         * Examples of valid variable expressions:
         * 
         *  i
         *  ii[1,3+a]
         *  form1.object[3].aInfo[b,3+val(strInfo)]
         * 
         * If in format a.b, the checks a to see if it's an alias
         * and if it is, checks b to see if it's a field.
         * If it's an alias field, returns the value.
         * If it's not an alias field, checks to see if a is an
         * object variable,  and tosses an error if it's not.
         * 
         */
        public static async Task<JAXObjects.Token> GetVarFromExpression(string expr, JAXObjectWrapper? parent)
        {
            JAXObjects.Token? result = new();
            JAXObjects.Token answer;
            VarRef var;

            string thisVar = string.Empty;

            try
            {
                // Get the top of the list
                string varRemains = string.Empty;
                bool NotATableRef = true;
                int wa = 0;

                // Macro expansion
                if (expr.Contains('&'))
                    expr = await JAXMacroHandler.Expand(Program.CurrentApp, expr);

                if (JAXLib.InListC(expr, ".null.", "null"))
                    throw new Exception("41|.null.");

                if (expr.Contains('.'))
                {
                    // Expecting object.property or alias.field
                    List<string> objList = BreakVar(expr);
                    int WithCount = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count - 1;


                    // Is this an alias.field?
                    if (objList.Count == 2)
                    {
                        // Is this referencing a work area?
                        if (Program.CurrentApp.CurrentDS.IsWorkArea(objList[0]))
                        {
                            // Does the field exist in this work area?
                            wa = Program.CurrentApp.CurrentDS.GetWorkArea(objList[0]);
                            NotATableRef = Program.CurrentApp.CurrentDS.FieldExists(objList[1], wa) == false;

                            if (NotATableRef)
                            {
                                // Not a field, is it a var?
                                answer = await GetVarToken(objList[0]);
                                if (answer.TType.Equals("U") || answer.Element.Type.Equals("O") == false)
                                {
                                    // Not an object variable, so toss a a field error
                                    throw new Exception("4012|");
                                }
                            }
                        }
                    }

                    if (NotATableRef)
                    {
                        // Not an alias - so step throug it after grabbing
                        // the last one to pass on once we resolve this list
                        thisVar = objList[^1];

                        // Process all but the last object of the list
                        for (int i = 0; i < objList.Count - 1; i++)
                        {
                            if (objList[i].Length == 0)
                            {
                                if (i != 0) throw new Exception("REALLY?");
                                if (WithCount < 0) throw new Exception("NO WITH!");

                                // Grab the most recent WITH
                                string[] withVar = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[WithCount].Split('.');
                                for (int j = withVar.Length - 1; j >= 0; j--)
                                {
                                    if (string.IsNullOrWhiteSpace(objList[0]))
                                        objList[0] = withVar[j];
                                    else
                                        objList.Insert(0, withVar[j]);
                                }

                                WithCount--;
                            }

                            if (i == 0)
                            {
                                // Get the parent - TODO Arrays? UDF?
                                JAXObjects.Token ptk = await GetVarToken(objList[i].TrimStart('.'));
                                if (ptk.Element.Type.Equals("O"))
                                    parent = (JAXObjectWrapper)ptk.Element.Value;
                                else
                                    throw new Exception($"1924|{objList[i]}");
                            }
                            else
                            {
                                if (i < objList.Count - 1)
                                {
                                    if (parent is not null)
                                    {
                                        // Get the object
                                        var = await SolveVariableReference(objList[i].TrimStart('.'));
                                        string memb = await parent.IsMember(var.varName);

                                        if (memb.Equals("O"))
                                        {
                                            int f = await parent.FindObjectByName(var.varName);
                                            parent = (await parent.GetObject(f));
                                        }
                                        else
                                            throw new Exception("NOT AN OBJECT");
                                    }
                                }
                                else
                                    thisVar = objList[i].TrimStart('.');
                            }
                        }
                    }
                    else
                    {
                        // It's a field in a work area, so get that value
                        // and mark it with it's Alias.Field 
                        result.Element.Value = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.CurrentRow.Rows[0][objList[1]];
                        result.Alias = Program.CurrentApp.CurrentDS.WorkAreas[wa].DbfInfo.Alias + "." + objList[0];
                    }
                }
                else
                {
                    thisVar = expr.TrimStart(AppClass.literalStart).TrimEnd(AppClass.literalEnd);
                }

                if (NotATableRef)
                {
                    // Try to get the variable reference
                    var = await SolveVariableReference(thisVar);

                    if (parent is null)
                    {
                        // ARRAY
                        result = PluckToken(await GetVarToken(var.varName), var.row, var.col);
                    }
                    else
                    {
                        // TODO - Array? UDF?
                        string memb = await parent.IsMember(var.varName);

                        if (memb.Equals("O"))
                        {
                            int f = await parent.FindObjectByName(var.varName);
                            object? jow = (await parent.GetObject(f));

                            if (jow is not null)
                                result.Element.Value = jow;
                            else
                                result.Element.MakeNull();
                        }
                        else if (memb.Equals("M"))
                        {
                            await parent.MethodCall(var.varName);
                            result.Element.Value = Program.CurrentApp.ReturnValue.Element.Value;
                        }
                        else
                        {
                            // It's a property
                            result = PluckToken(await parent.GetProperty(var.varName, 0), var.row, var.col);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppIO.DebugLog("ERROR: Retrieving " + expr);
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                result.TType = "X";
            }

            return result!;
        }

        private static JAXObjects.Token PluckToken(JAXObjects.Token answer, int row, int col)
        {
            JAXObjects.Token result = new();

            if (row > 0 || col > 0)
            {
                // Return the element
                if (answer.TType.Equals("M"))
                {
                    // Have a Dictionary
                    if (answer._dictionary is not null)
                    {
                        if (answer._dictionary.ContainsKey(row))
                        {
                            if (col > 0)
                            {
                                // Get a column from the dictionary element
                                if (answer._dictionary[row].Col >= col)
                                    result.Element.Value = answer._dictionary[row]._avalue[col - 1].Value;
                                else
                                    throw new Exception("JON|");
                            }
                            else
                            {
                                // Get the entire ditionary element
                                result = answer;
                            }
                        }
                    }
                }
                else
                {
                    // Get an array element
                    if (col < 1)
                    {
                        col = row;
                        row = 1;
                    }

                    result.Element.Value = answer._avalue[(row - 1) * col + col - 1];
                }
            }
            else
                result = answer;    // Return the entire array

            return result;
        }


        /*-------------------------------------------------------------*
         * Set an object value
         *-------------------------------------------------------------*/
        public static void SetVar(string varName, object? obj, int r, int c)
        {
            JAXObjects.Token tk;

            varName = varName.Trim().ToLower();

            // Is it a memory Var reference?  Strip m. if it is
            if (varName.Length > 2 && varName[..2].Equals("M.", StringComparison.OrdinalIgnoreCase))
                varName = varName[2..];

            if (JAXLib.InList(varName, "this", "thisform", "thisformset"))
            {
                // Can only set if it's not already in existance
                // or it's a bool equal to false
                tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);
                if (tk.TType.Equals("U") == false && (tk.Element.Type.Equals("L") == false || tk.AsBool()))
                    throw new Exception("2405|" + varName.ToUpper());

                if (obj is not null)
                    AppIO.DebugLog($"Set {varName} to {((JAXObjectWrapper)obj).JOWName} in AppLevel {Program.CurrentApp.AppLevels.Count - 1}", false);
            }

            // Check local variables
            tk = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.GetToken(varName);

            // Check private variables
            if (tk.TType.Equals("U"))
            {
                // *********** TODO - Add new logic here
                // Check all the private vars
                for (int i = Program.CurrentApp.AppLevels.Count - 1; i >= 0; i--)
                {
                    tk = Program.CurrentApp.AppLevels[i].PrivateVars.GetToken(varName);
                    if (tk.TType.Equals("U") == false)
                    {
                        AppIO.DebugLog($"Set private var {varName} in AppLevel {i} to {obj ?? ".NULL."}");
                        Program.CurrentApp.AppLevels[i].PrivateVars.SetValue(varName, obj, r, c);
                        break;   // Found it!
                    }
                }
            }
            else
            {
                // Set the local variable
                AppIO.DebugLog($"Set local var {varName} in AppLevel {Program.CurrentApp.CurrentAppLevel} to {obj ?? ".NULL."}");
                Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].LocalVars.SetValue(varName, obj, r, c);
            }
        }


        /*-------------------------------------------------------------*
         * This routine is used to create global system variables
         * such as _JAX and _Screen during initialization
         *-------------------------------------------------------------*/
        public static void CreateSystemVars()
        {

            MakePublicVar("_screen", 1, 1, true).Wait();
            Program.CurrentApp.AppLevels[0].PrivateVars.jaxObject["_screen"].Element.Value = Program.CurrentApp._screen!;

            MakePublicVar("_jax", 1, 1, true).Wait();
            Program.CurrentApp.AppLevels[0].PrivateVars.jaxObject["_jax"].Element.Value = Program.CurrentApp._jax!;
        }

        public static async Task SetAsType(string varName, string rpn)
        {
            JAXObjects.Token answer = await GetVarToken(varName);
            string asType = (await Program.CurrentApp.SolveFromRPNString(rpn)).AsString().ToUpper().Trim();

            if (asType.Length > 0)
            {
                asType = asType switch
                {
                    "B" => "N",
                    "C" => "C",
                    "D" => "D",
                    "F" => "N",
                    "I" => "N",
                    "L" => "L",
                    "N" => "N",
                    "O" => "O",
                    "T" => "T",
                    "V" => "C",
                    "Y" => "N",
                    "INT" => "I",
                    "INTEGER" => "I",
                    "NUM" => "N",
                    "NUMERIC" => "N",
                    "NUMBER" => "N",
                    "DATE" => "D",
                    "DT" => "D",
                    "DATETIME" => "T",
                    "DTTM" => "T",
                    "LOGICAL" => "L",
                    "BOOL" => "L",
                    "BOOLEAN" => "L",
                    "LOG" => "L",
                    "CHAR" => "C",
                    "CHARACTER" => "C",
                    "STRING" => "C",
                    "STR" => "C",
                    "OBJ" => "O",
                    "OBJECT" => "O",
                    "DOUBLE" => "N",
                    "DOUB" => "N",
                    "FLOAT" => "N",
                    "VARCHAR" => "C",
                    "VARC" => "C",
                    "CURRENCY" => "N",
                    "CURR" => "N",
                    _ => throw new Exception("11|")
                };
            }
            if (asType.Length == 1 && "NDITLCO".Contains(asType))
                answer.SetAsType(asType);
        }


        /*
         * Break a var into 
        */
        public static List<string> BreakVar(string varName)
        {
            List<string> objList = [];

            try
            {
                int cpos = 0;
                char endQuote = '\0';

                if (varName.Contains('.'))
                {
                    while (varName.Length > 0 && cpos < varName.Length)
                    {
                        char c = varName[cpos++];

                        if (endQuote == '\0')
                        {
                            if ("(['\"".Contains(c))
                            {
                                if (c == '(')
                                    endQuote = ')';
                                else if (c == '[')
                                    endQuote = ']';
                                else
                                    endQuote = c;
                            }
                            else if (c == '.')
                            {
                                // Found a period to split
                                objList.Add(varName[..cpos].TrimEnd('.'));
                                if (cpos >= varName.Length) throw new Exception("10|");
                                varName = varName[cpos..];
                                cpos = 0;

                                if (AppHelper.IsCompoundVar(varName) == false)
                                {
                                    // Can't find a period, so we're done!
                                    objList.Add(varName);
                                    varName = string.Empty;
                                    break;
                                }
                            }
                        }
                        else if (endQuote == c)
                        {
                            // Found the end quote
                            endQuote = '\0';
                        }
                    }

                    // Shouldn't ever end up here with varName still holding a value
                    if (string.IsNullOrWhiteSpace(varName) == false) throw new Exception("10|");
                }
                else
                {
                    // Not an object name
                    objList.Add(varName.Trim());
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
                objList = [];
            }

            return objList;
        }




        /*-------------------------------------------------------------------------------------------*
         * 
         * OBJECT CALL
         * 
         * Call an object.method or return a object.property
         * 
         * It can be something simple like FORM1.SHOW, FORM1.REFRESH(), 
         * FORM1.Caption, or something complex like 
         * FORM1.PGFRAME1.PAGE1.CONTAINER1.OBJECT[4].Value
         * 
         * The eCodes.Expressions[0] holds the entire string for the
         * call and is broken down by element.
         * 
         * Parameter ExpectingValue = false means it has to be an
         * event or method call, which may still return a value.
         * 
         * Out parameter objResult will hold the property/object
         * returned at the end.
         * 
         *-------------------------------------------------------------------------------------------*/
        public static async Task<GenericClass> ObjectCall(ExecutorCodes eCodes, bool expectingValue)
        {
            GenericClass result = new();
            result.Result.Element.Value = "U";


            try
            {
                // Make sure thier is only one expression
                if (eCodes.Expressions.Count != 1) throw new Exception("10|Must have only one expression");

                // Resolve the expression
                JAXObjects.Token answer = await Program.CurrentApp.SolveFromRPNString(eCodes.Expressions[0].RNPExpr);

                // The expression must be a character string
                if (answer.Element.Type.Equals("C") == false) throw new Exception("11|");

                string expr = answer.AsString().Trim();

                // Macro expansion
                if (expr.Contains('&'))
                    expr = await JAXMacroHandler.Expand(Program.CurrentApp, expr);

                if (JAXLib.InListC(expr, ".null.", "null"))
                    throw new Exception("10|.NULL.");

                // Set up the object list
                List<string> objList = BreakVar(expr);

                // If anything was sent, process it
                if (objList.Count > 0)
                {
                    int withStack = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack.Count;

                    // Get the current with stack expression
                    if (string.IsNullOrWhiteSpace(objList[0]))
                    {
                        if (withStack > 0)
                        {
                            // This may be a layered with so we'll be inserting
                            // into objList[0] as long as the we keep finding
                            // variables starting with a period.
                            while (withStack > 0)
                            {
                                if (string.IsNullOrWhiteSpace(objList[0]) == false)
                                    objList.Insert(0, string.Empty);

                                objList[0] = Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[withStack - 1];
                                withStack--;

                                if (objList[0][0] != '.')
                                    break;

                                if (withStack < 0)
                                    throw new Exception($"2300|Top of with stack is {Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].WithStack[0]}");
                            }
                        }
                        else
                            throw new Exception($"2301|There is nothing on the with stack for .{objList[1]}");
                    }

                    // We have the object broken down by list so let's start processing it
                    // by getting the base object.
                    JAXObjects.Token currentVar = await GetVarFromExpression(objList[0], null);

                    // If the current var is not an object, then check to see if it's a UDF
                    // TODO

                    // If we don't come up with an object, toss an error
                    if (currentVar.Element.Type.Equals("O") == false)
                        throw new Exception("11|");

                    if (objList.Count == 1)
                    {
                        // There is only one item in the list
                        if (expectingValue)
                        {
                            // If we're expecting a value, send back the object
                            result.Result.Element.Value = "O";
                            result.Value.Element.Value = currentVar.Element.Value;
                        }
                        else
                            throw new Exception("10|Expecting a method/event call for " + objList[0]);
                    }
                    else
                    {
                        // We have 2 or more list items
                        for (int i = 1; i < objList.Count; i++)
                        {
                            // Save the current object
                            JAXObjectWrapper thisObject = (JAXObjectWrapper)currentVar.Element.Value;

                            if (objList[i].Contains('(') || objList[i].Contains('['))
                            {
                                // TODO - It's an array or UDF
                                List<string> varInfo = AppHelper.BreakArrayOrUDF(objList[i]);
                                string memberType = (await thisObject.IsMember(varInfo[0])).ToUpper();

                                if ("ME".Contains(memberType) == false) throw new Exception("1738|" + varInfo[0]);

                                // TODO - what about objects?

                                Program.CurrentApp.ParameterClassList.Clear();
                                List<ParameterClass> cParams = [];

                                // set up the parameter list
                                for (int j = 1; j < varInfo.Count; j++)
                                {
                                    // solve the math string for this parameter
                                    GenericClass gc = await Program.CurrentApp.JaxMath.SolveMath(varInfo[j]);
                                    ParameterClass c = new();
                                    c.token.CopyFrom(gc.Value);
                                    c.Type = "T";
                                    Program.CurrentApp.ParameterClassList.Add(c);
                                }

                                // Call the method/event
                                if ((await thisObject.MethodCall(varInfo[0])) < 0)
                                    throw new Exception($"{thisObject.GetErrorNo()}|");
                            }
                            else
                            {
                                // If we're not on the last element of the list, we have a problem
                                //if (i + 1 != objList.Count) throw new Exception("1575|" + objList[i]);

                                // Is it part of the current object?
                                string memberType = (await thisObject.IsMember(objList[i])).ToUpper();

                                // Use the return code to decide what to do
                                switch (memberType)
                                {
                                    case "E":   // Event
                                    case "M":   // Method
                                                // TODO - break out any parameters

                                        // Call the method
                                        await thisObject.MethodCall(objList[i]);

                                        // Return what was sent back as a token
                                        result.Value.Element.Value = Program.CurrentApp.ReturnValue.Element.Value;
                                        result.Result.Element.Value = memberType;
                                        break;

                                    case "O":   // We're looking for an OBJECT[]
                                                // If we're not looking for a value, we have a problem
                                                //if (i+1>=objList.Count && expectingValue == false)
                                                //    throw new Exception("1738|" + objList[i].ToUpper());

                                        // TODO - break down the object var call

                                        // Get the object index by name since we know it exists
                                        JAXObjectWrapper? jow = await thisObject.GetObject(objList[i]);
                                        if (jow is not null)
                                            result.Value.Element.Value = jow;
                                        else
                                            throw new Exception($"1901|{objList}");

                                        result.Result.Element.Value = memberType;
                                        currentVar = new();
                                        currentVar = result.Value;
                                        break;

                                    case "P":   // Property - array properties are handled above
                                                // If we're not expecting a value, then we have a
                                                // problem being here and will toss an exception.
                                        if (i + 1 >= objList.Count && expectingValue == false)
                                            throw new Exception("1738|" + objList[i].ToUpper());

                                        // Get the property token and return it
                                        result.Value = await thisObject.GetProperty(objList[i], 0);
                                        result.Result.Element.Value = memberType;
                                        break;

                                    default:
                                        throw new Exception("1999|Object member type " + memberType);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppErrorHandling.HandleException(System.Reflection.MethodBase.GetCurrentMethod()!.Name, ex.Message);
            }

            // Return the token
            return result;
        }
    }
}
