using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Language;
using JAXBase.Utilities;

namespace JAXBase.Math
{
    public class JAXMathAux
    {
        public int err = 0;
        public string errmsg = string.Empty;
        readonly JAXLanguageLists lists = new();
        readonly string[] functions = [];

        const StringComparison ignCase = StringComparison.OrdinalIgnoreCase;

        public JAXMathAux()
        {
            // Load the math functions
            functions = lists.MathFunctions;
        }

        /********************************************************************************
         * Take the parsed list and make it into an RPN list
         * The basic idea for this routine was taken from a magazine back in
         * the 1980's... perhaps Dr Dobbs?  Greatly expanded as it only
         * allowed you to convert expressions like 41+3*8 and not handle
         * functions, parens, or variables.
         * 
         * TODO - rewrite this monstrosity!
         *
         * 2025-05-16 - JLW
         *      Realized I needed to add the ability to recognize and 
         *      correctly parse objects such as the following
         *      
         *      Original string:    frm.objects[1].grdView.column1.name 
         *      becomes rpn list:   [_frm][~][N1][.objects][.grdview][.column1][.name]
         *      
         ********************************************************************************/
        public List<string> MathMakeRPN(List<string> parsed)
        {
            List<string> rpn = [];
            List<string> brpn = [];

            if (parsed.Count > 0)
            {
                int stackcount = 0;
                string[] stack = new string[parsed.Count];

                try
                {
                    int bracketcount = 0;
                    int parencount = 0;
                    int parencountinbool = -1;
                    int parencountincomparison = -1;
                    int a, b;
                    int fpos;
                    string lastfunc = string.Empty;
                    string zstring;
                    string sstring;
                    string fstring;
                    //string flist = "! #={}<>? `+-*/^$%";
                    string flist = "! #={}<>?@`+-*/^$%";
                    bool loop;
                    string opOnStack = string.Empty;
                    List<char> lastBP = ['\0'];

                    foreach (string _parsed in parsed)
                    {
                        loop = false;
                        fstring = _parsed;

                        if (opOnStack.Length > 0)
                        {
                            // Process operator on stack condition
                            // If the next char is an operator or a
                            // open paren or bracket, then leave this
                            // operator on the stack
                            if (" @#=[(<>?${}+-*/".IndexOf(_parsed) < 1)
                            {
                                if (stackcount > 0)
                                {
                                    // Pop this operator off the stack
                                    rpn.Add(opOnStack);
                                    stack[stackcount--] = string.Empty;
                                }
                            }

                            // Clear the flag
                            opOnStack = string.Empty;
                        }

                        if (fstring.Equals(","))
                        {
                            if (lastBP[^1] == '[')
                            {
                                if (bracketcount == 0) throw new Exception("10||unexpected comma");
                                // Pop everything off until you find the
                                // matching [ bracket but leave the [
                                while (true)
                                {
                                    if (stackcount == 0) throw new Exception("10||Bracket mismatch");
                                    if (stack[stackcount].Equals("[")) break;
                                    rpn.Add(stack[stackcount--]);
                                }
                                loop = true;
                            }
                            else if (lastBP[^1] == '(')
                            {
                                if (parencount == 0) throw new Exception("10||unexpected comma");
                                // Pop everything off until you find the
                                // matching ( bracket but leave the (
                                while (true)
                                {
                                    if (stackcount == 0) throw new Exception("10||Bracket mismatch");
                                    if (stack[stackcount].Equals("(")) break;
                                    rpn.Add(stack[stackcount--]);
                                }
                                loop = true;
                            }
                            else
                                throw new Exception("10||Bracket mismatch");
                        }
                        else if ("[".Contains(fstring))
                        {
                            // Found a left paren, stack it
                            rpn.Add(fstring);
                            lastBP.Add(fstring[0]);
                            stack[++stackcount] = fstring; // Push this onto the stack to track any math in the brackets
                            bracketcount++;
                        }
                        else if ("(".Contains(fstring))
                        {
                            // Found a left paren, stack it as ~
                            lastBP.Add(fstring[0]);
                            stack[++stackcount] = fstring;
                            parencount++;
                            if (lastfunc.Length > 0) rpn.Add("~");
                        }
                        else if (fstring.Equals("]"))
                        {
                            // Pop everything off until you find
                            // the matching [ bracket
                            while (true)
                            {
                                if (stackcount == 0) throw new Exception("10||Bracket mismatch");
                                if (stack[stackcount].Equals("["))
                                {
                                    stackcount--;
                                    break;
                                }

                                rpn.Add(stack[stackcount--]);
                            }

                            lastBP.RemoveAt(lastBP.Count - 1);
                            bracketcount--;
                            rpn.Add("]");
                        }
                        else if (")".Contains(fstring))
                        {
                            parencount--;

                            // Get anything from the stack back to
                            // the first left bracket we find
                            while (stackcount > 0)
                            {
                                if (stack[stackcount].Equals("(") == true)
                                {
                                    rpn.Add(")");
                                    stack[stackcount--] = string.Empty;
                                    break;
                                }

                                rpn.Add(stack[stackcount]);
                                stack[stackcount--] = string.Empty;
                            }

                            lastBP.RemoveAt(lastBP.Count - 1);

                            // ************* if function or _var or _var and matching fstring ) or ]
                            // If next stack item is a function, pull it
                            if (stackcount > 0 && stack[stackcount][0].Equals("_"))
                            {
                                // it's a _var so check that it matches the fstring
                                if (fstring[0].Equals(stack[stackcount][1]))
                                {
                                    rpn.Add(stack[stackcount]);
                                    stack[stackcount--] = string.Empty;
                                }
                            }
                            if (stackcount > 0 && stack[stackcount][0].Equals('`'))
                            {
                                if (rpn[^1].Equals(")"))
                                {
                                    // Functions don't need the ")"
                                    rpn.RemoveAt(rpn.Count - 1);
                                }

                                rpn.Add(stack[stackcount]);
                                stack[stackcount--] = string.Empty;
                            }
                        }
                        else
                        {
                            // no need to track the function any further
                            lastfunc = string.Empty;

                            // Get the function marker (`) precedence
                            fpos = (flist.IndexOf('`') - 1) / 2;
                            a = flist.IndexOf(fstring[..1]);

                            if (a > 0)
                            {
                                // keep track for comparison operators
                                if (a > 1 && a <= 8)
                                    parencountincomparison = parencount;

                                // Is it a function or operand?
                                a = (flist.IndexOf(fstring[..1]) - 1) / 2;      // Get the precedence of this operand
                                if (stackcount > 0)
                                {
                                    // yes it is, so grab what's on the stack to
                                    // check its precedence and decide if it
                                    // needs to switch places or be left
                                    // on the stack for later
                                    sstring = stack[stackcount];
                                    b = (flist.IndexOf(sstring[..1]) - 1) / 2;  // Get the precedence of the stacked operand

                                    // Is the stack val an op or func?
                                    if (b < 0)
                                    {
                                        // no, stack the new value as
                                        // no decision needs to be made
                                        stack[++stackcount] = fstring;
                                        loop = true;
                                    }
                                    else if (b > a && a != fpos)
                                    {
                                        // Yes stack val is math and higher
                                        // precedence, so switch
                                        stack[stackcount] = fstring;
                                        rpn.Add(sstring);
                                        loop = true;
                                    }
                                    else if (b == a)
                                    {
                                        // Yes and same precedence so switch
                                        stack[stackcount] = fstring;
                                        rpn.Add(sstring);
                                        loop = true;
                                    }
                                    else if (b < a || (b > a && a == fpos))
                                    {
                                        // It's a function that needs to be stacked
                                        stack[++stackcount] = fstring;
                                        loop = true;
                                    }
                                }
                                else
                                {
                                    // Everything that end up here will
                                    // just stacked for later use
                                    stack[++stackcount] = fstring;
                                    loop = true;
                                }

                                lastfunc = fstring[0].Equals('`') ? fstring : string.Empty;
                            }


                            if (!loop)
                            {
                                if (" &|".IndexOf(fstring) > 0)
                                {
                                    // AND and OR operations
                                    if (stackcount < 1)
                                    {
                                        stackcount = 0; // reset
                                        stack[++stackcount] = fstring;
                                    }
                                    else
                                    {
                                        while (stackcount > 0)
                                        {
                                            zstring = stack[stackcount];

                                            if ("([".Contains(zstring) || stackcount == 0)
                                            {
                                                stack[++stackcount] = fstring;
                                                parencountinbool = parencount;  // need to remember so we place in correct order
                                                break;
                                            }
                                            rpn.Add(zstring);
                                            stack[stackcount--] = string.Empty;
                                        }
                                    }
                                }
                                else if (fstring == ")" || fstring == "]")
                                {
                                    // Working with right paren (end of function)
                                    while (stackcount > 0)
                                    {
                                        zstring = stack[stackcount];
                                        stack[stackcount--] = string.Empty;

                                        if (zstring == "(")
                                        {
                                            lastBP.RemoveAt(lastBP.Count - 1);
                                            parencount--;
                                            break;
                                        }

                                        if (zstring == "[")
                                        {
                                            lastBP.RemoveAt(lastBP.Count - 1);
                                            bracketcount--;
                                            break;
                                        }

                                        rpn.Add(zstring);
                                    }

                                    // If there is something left on the stack, check to 
                                    // see if it starts with a accent mark (`), meaning
                                    // it's a function, or A-Z meaning it's an array
                                    // name or a program call and needs (``) added
                                    // to it so that we can handle it later.
                                    if (stackcount > 0)
                                    {
                                        if (stack[stackcount][..1] == "`")
                                        {
                                            // It's a function, pop it off
                                            rpn.Add(stack[stackcount]);
                                            stack[stackcount--] = string.Empty;
                                            loop = true;
                                        }
                                        else
                                        {
                                            if (JAXLib.Between(stack[stackcount][..1].ToUpper(), 'A', 'Z'))
                                            {
                                                // It starts with an alpha so it's either an
                                                // array or a program call to be handeld later
                                                rpn.Add("``" + stack[stackcount]);
                                                stack[stackcount--] = string.Empty;
                                                loop = true;
                                            }
                                        }
                                    }

                                    // do we need to put the and/or into the mix from the stack?
                                    if (parencountinbool >= 0 && parencountinbool == parencount)
                                    {
                                        parencountinbool = -1;

                                        if (" &|".IndexOf(stack[stackcount]) > 0)
                                        {
                                            rpn.Add(stack[stackcount]);
                                            stack[stackcount--] = string.Empty;
                                        }
                                    }

                                    // Are we working with a comparison operator?
                                    // If so, and we're in the same paren level
                                    // then pop of the comparison operator.
                                    if (parencountincomparison >= 0 && parencountincomparison == parencount)
                                    {
                                        parencountincomparison = -1;

                                        if (" @#=[]<>$%".IndexOf(stack[stackcount]) > 0)
                                        {
                                            rpn.Add(stack[stackcount]);
                                            stack[stackcount--] = string.Empty;
                                        }
                                    }
                                }
                                else if (fstring == "!")
                                {
                                    stack[++stackcount] = fstring;
                                }
                                else if (fstring == ",")
                                {
                                    // Found a comma which should only be in a
                                    // parameter list or array dimensions.
                                    // So pop everything off the stack until
                                    // you are done or find a (, [, or ~
                                    while (stackcount > 0)
                                    {
                                        if (stack[stackcount].Equals("[") || stack[stackcount].Equals("(") || stack[stackcount].Equals("~"))
                                            break;

                                        rpn.Add(stack[stackcount]);
                                        stack[stackcount--] = string.Empty;
                                    }

                                    rpn.Add(fstring);
                                }
                                else
                                {
                                    // variables and literals end up here
                                    if (fstring.Length > 1 && (fstring[..2].Equals("_)") || fstring[..2].Equals("_]")))
                                    {
                                        // _]var and _)var gets stacked like a function
                                        stack[++stackcount] = fstring;
                                    }
                                    else
                                        rpn.Add(fstring);   // stick everything else into the RPN list

                                    // do we need to put the and/or into the mix from the stack?
                                    if (parencountinbool >= 0 && parencountinbool == parencount)
                                    {
                                        parencountinbool = -1;

                                        if (" &|".IndexOf(stack[stackcount]) > 0)
                                        {
                                            rpn.Add(stack[stackcount]);
                                            stack[stackcount--] = string.Empty;
                                        }
                                    }

                                    // Are we working with a comparison operator?
                                    // If so, and we're in the same paren level
                                    // then pop off the comparison operator.
                                    if (parencountincomparison >= 0 && parencountincomparison == parencount)
                                    {
                                        parencountincomparison = -1;

                                        // Was happending too soon on seconds()<86400/2
                                        // You must look at next token to decide if this
                                        // token is to be popped or left on the stack 
                                        if (" @#={}<>$%".IndexOf(stack[stackcount]) > 0)
                                            opOnStack = stack[stackcount];

                                        //if (" #=[]<>$".IndexOf(stack[stackcount]) > 0)
                                        //    rpn.Add(stack[stackcount--]);
                                    }
                                }
                            }
                        }
                    }

                    // Must not have any hanging brackets or parens
                    if (parencount > 0 || bracketcount > 0)
                        throw new Exception("10|");
                }
                catch (Exception ex)
                {
                    err = 9994;
                    errmsg = ex.Message;
                }

                // Pop everything off the parsed stack
                // and add it to the RPN stack
                for (int i = stackcount; i > 0; i--)
                    rpn.Add(stack[i]);

                // it's possible for the RPN list to have parens
                // if the expression is "(expr)".  They should
                // not exist in the rpn list, so remove them!
                for (int i = rpn.Count - 1; i >= 0; i--)
                {
                    if (rpn[i].Equals("(") || rpn[i].Equals(")"))
                        rpn.RemoveAt(i);
                }
            }

            return rpn;
        }

        /*********************************************************************
         * Parse the string into a list
         * Parse by space first, but then check last char vs next
         * char.  If last was an alpha and next is period, ( or [ then 
         * ignore the space and put them together.
         * 
         * Exmaples:  
         *      a = (b + c) * 4         -> [a][=][(][b][+][c][)][*][4]
         *      not done                -> [not][done]
         *      not a (3) *4 +1         -> [not][a(3)][*][4][+][1]
         *      a="hello" and b=4       -> [a][=]["hello"][and][b][=][4]
         *      
         * 2025-05-16 - JLW
         *      Adding ability to correctly parse objects
         *
         *          String:     a=frm.objects[1]
         *          Becomes:    [a][=][frm.objects[1]]
         *      
         *          String:     a=frm.objects[1].grdView.Name
         *          Becomes:    [a][=][frm][.objects][1][.grdview][.name]
         *          
         * 2025-07-01 - JLW
         *      Found that it's not understanding the difference between
         *      table.field and object.property - will need to look it
         *      over, but table.field takes precidence if it matches
         *      to an open alias or table name.  A table.field is not
         *      an object part is is handled in var processing and needs
         *      no special handling in the math routines.
         *      
         * 2025-08-10 - JLW
         *      The parser is seeing floating point numbers as object
         *      variables because of the period (oops).
         *      
         * 2025-12-31 - JLW
         *      Time for a rewrite. I've been putting it off but it's
         *      absolutely time.  Getting rid of BASIC converted code 
         *      from the 1900's and giving it better error catching.
         *      
         *      Regarding macro substitution; expressions that contain
         *      macros (example: b+aTest[&iExpr]) are kept as strings
         *      during the compile process and marked as needing to be
         *      parsed and solved during execution after the macro
         *      is resolved.
         *      
         *********************************************************************/
        public List<string> MathParse(string prob)
        {
            string hold = prob; // Debuggin purposes

            List<string> parsed = [];
            //string[] operands = ["+", "-", "/", "*", "^", "%", "==", ">=", "<=", "!=", "<>", "=", ">", "<", "&&", "||", "!", "$", "(", ")", ",", "[", "]"];
            string[] operands = ["+", "-", "/", "*", "^", "%", "==", ">=", "<=", "!=", "<>", "=", "@", ">", "<", "&&", "||", "!", "$", "(", ")", ",", "[", "]"];

            int i;

            string lastquote = string.Empty;
            List<string> nospacesList = [];

            try
            {
                List<string> arrayPartStack = [];
                char lastType = 'X';

                while (prob.Length > 0)
                {
                    i = 0;

                    // Check for a variable or function
                    if ((prob.Length > 1 && prob[i] == '.' && "abcdefghijklmnopqrstuvwxyz_".Contains(prob[i + 1], ignCase)) || "abcdefghijklmnopqrstuvwxyz_".Contains(prob[i], ignCase))
                    {
                        if (lastType == 'V')
                            throw new Exception("10|");

                        // Found the start of a variable or function
                        lastType = 'X';

                        // Keep looking as long as it's valid 
                        while (i < prob.Length && "abcdefghijklmnopqrstuvwxyz_.0123456789".Contains(prob[i], ignCase))
                            i++;

                        string vftest = string.Empty;

                        // Is the next char an open paren?
                        if (prob.Length > i && prob[i] == '(')
                        {
                            // Possible function, check it out
                            int funcVal = -1;
                            string testfunc = prob[..i];

                            // Functions have to be at least 3 chars in length
                            for (int ii = 0; ii < functions.Length; ii++)
                            {
                                // Is it an exact match?
                                if (functions[ii].Equals(testfunc + "(", ignCase))
                                {
                                    // We have a short function match
                                    funcVal = ii;
                                    break;
                                }
                                else if (testfunc.Length > 2)
                                {
                                    // If at least 3 chars, then try a partial match
                                    if (functions[ii].StartsWith(testfunc, ignCase))
                                    {
                                        funcVal = ii;
                                        break;
                                    }
                                }
                            }

                            if (funcVal >= 0)
                            {
                                // It's a known function
                                i++;
                                lastType = 'F';
                                arrayPartStack.Add("(");
                                vftest = "`" + functions[funcVal].Trim('(');
                                nospacesList.Add(vftest);
                            }
                        }
                        else
                        {
                            // Not a function
                            vftest = prob[..i];
                        }

                        // Pull it from the problem string
                        prob = prob[i..];

                        if (lastType == 'X')
                        {
                            // Take care of some last minute alterations
                            switch (vftest.ToLower())
                            {
                                case ".t.":
                                case ".f.":
                                    nospacesList.Add(vftest.ToUpper());
                                    lastType = 'L';
                                    break;

                                case ".or.":
                                case "or":
                                    nospacesList.Add("|");
                                    lastType = 'O';
                                    break;

                                case ".and.":
                                case "and":
                                    nospacesList.Add("&");
                                    lastType = 'O';
                                    break;

                                case ".not.":
                                case "not":
                                    nospacesList.Add("!");
                                    lastType = 'O';
                                    break;

                                case ".null.":
                                case "null":
                                    nospacesList.Add(".NULL.");
                                    lastType = 'N';
                                    break;
                            }
                        }

                        if (lastType == 'X')
                        {
                            // Not a function so it's a variable which
                            // may or may not be followed by a [ or (
                            lastType = 'V';

                            // Make sure nothing is wrong with the var expression
                            if (vftest.Contains('.'))
                            {
                                string[] parts = vftest.Split('.');

                                // if var starts with a period then
                                // put it back to the first part
                                if (vftest[0] == '.')
                                    parts[0] = "." + parts[0];

                                // mark it as a var
                                parts[0] = "_" + parts[0];

                                for (int ii = 0; ii < parts.Length; ii++)
                                {
                                    // Won't accept things like ..a, a..b, a.1, or a._
                                    if (parts[ii].Length < 1 || string.IsNullOrWhiteSpace(parts[ii].Replace("_", "")) || "0123456789".Contains(parts[ii][0]))
                                        throw new Exception("10|");
                                    else
                                    {
                                        // Add the valid variable parts making sure
                                        // anything following the first entry has 
                                        // a period attached to the front
                                        nospacesList.Add((ii > 0 ? "." : string.Empty) + parts[ii]);
                                    }
                                }
                            }
                            else
                            {
                                // Plain var, just mark an add it
                                nospacesList.Add("_" + vftest);
                            }
                        }

                        if (lastType == 'F') nospacesList.Add("(");
                        continue;
                    }

                    // Is it an array?
                    if ("[(".Contains(prob[0]) && lastType == 'V')
                    {
                        // Looks like an array
                        arrayPartStack.Add(prob[0].ToString());
                        lastType = 'A';

                        nospacesList.Add(prob[..1]);
                        prob = prob[1..];
                        continue;
                    }

                    if (prob[0] == '(')
                    {
                        if ("XO".Contains(lastType))
                        {
                            // Found a left bracket
                            arrayPartStack.Add(prob[0].ToString());
                            nospacesList.Add(prob[..1]);
                            prob = prob[1..];
                            lastType = 'B';
                            continue;
                        }
                        else
                            throw new Exception("10|");
                    }

                    // end parent/brackets
                    if (")]".Contains(prob[0]))
                    {
                        if (arrayPartStack.Count > 0)
                        {
                            if ((prob[0] == ')' && arrayPartStack[^1] == "(") || (prob[0] == ']' && arrayPartStack[^1] == "["))
                            {
                                // Found the end of the array designation
                                arrayPartStack.RemoveAt(arrayPartStack.Count - 1);
                                nospacesList.Add(prob[..1]);
                                prob = prob[1..];
                                lastType = 'b';
                                continue;
                            }
                            else
                                throw new Exception("10||Paren/bracket mismatch");
                        }
                        else
                            throw new Exception("10||Paren/bracket mismatch");
                    }

                    if (prob[0] == ',' && arrayPartStack.Count > 0)
                    {
                        // TODO - must be in function call
                        nospacesList.Add(",");
                        prob = prob[1..];
                        lastType = 'X';
                        continue;

                    }

                    if (prob[0] == '.')
                    {
                        // Number or operand?
                        if (prob.Length > 1)
                        {
                            int iVar = 0;

                            if ("0123456789".Contains(prob[1]) == false)
                            {
                                // Not a number, is it .T. or .F.?
                                if (prob.Length > 2 && JAXLib.InListC(prob[..3], ".T.", ".F."))
                                {
                                    iVar = 3;
                                    nospacesList.Add("L" + prob[..iVar].ToUpper());
                                }
                                else if (prob.Length > 3 && prob[..4].Equals(".OR.", ignCase))
                                {
                                    iVar = 4;
                                    nospacesList.Add("|");
                                }
                                else if (prob.Length > 4 && prob[..5].Equals(".AND.", ignCase))
                                {
                                    iVar = 5;
                                    nospacesList.Add("&");
                                }
                                else if (prob.Length > 4 && prob[..5].Equals(".NOT.", ignCase))
                                {
                                    iVar = 5;
                                    nospacesList.Add("!");
                                }
                                else if (prob.Length > 5 && prob[..6].Equals(".NULL."))
                                {
                                    iVar = 6;
                                    nospacesList.Add(".NULL.");
                                }

                                // Save it as a number
                                prob = prob[iVar..];
                                continue;
                            }
                        }
                        else
                            throw new Exception("10|");
                    }

                    // Is it a number?
                    int periodcount = 0;
                    // Do we have the start of a signed or unsigned number (eg 90, +90, or -90)?
                    if ((prob.Length > 1 && "XOF".Contains(lastType) && ("+-".Contains(prob[0]) && "0123456789.".Contains(prob[1])) || "0123456789.".Contains(prob[0])))
                    {
                        i++;
                        while (i < prob.Length && "0123456789.".Contains(prob[i]))
                        {
                            if (prob[i] == '.')
                            {
                                if (periodcount == 0)
                                    periodcount = 1;
                                else
                                {
                                    // we've got a problem
                                    throw new Exception("10|");
                                }
                            }

                            i++;
                        }

                        string part = prob[..i];
                        prob = prob[i..];
                        nospacesList.Add("N" + part);
                        lastType = 'N';
                        continue;
                    }

                    // TODO - Is it an operand?
                    if ("-+*/><+!%$^=".Contains(prob[i]))
                    {
                        int ivar = 0;

                        // operands can follow operands
                        if (lastType == 'O')
                            throw new Exception("10|");

                        if (prob.Length > 1)
                        {
                            if (JAXLib.InList(prob[..2], "==", "!=", ">=", "<=", "<>", "**"))
                            {
                                ivar = 2;

                                switch (prob[..2])
                                {
                                    case "==":
                                        nospacesList.Add("?");
                                        break;

                                    case "!=":
                                        nospacesList.Add("@");
                                        break;

                                    case ">=":
                                        nospacesList.Add("}");
                                        break;

                                    case "<=":
                                        nospacesList.Add("{");
                                        break;

                                    case "<>":
                                        nospacesList.Add("#");
                                        break;

                                    case "**":
                                        nospacesList.Add("^");
                                        break;

                                }
                            }
                            else if (JAXLib.InList(prob[..1], "=", ">", "<", "!", "-", "+", "*", "/", "%", "$", "^"))
                            {
                                ivar = 1;
                                nospacesList.Add(prob[..ivar]);
                            }

                            // These operands can't follow the current match
                            if ("*/><+!%$^=".Contains(prob[i + ivar]))
                                throw new Exception("10|");

                            // save what we found
                            prob = prob[ivar..];
                            lastType = 'O';
                            continue;
                        }
                        else
                            throw new Exception("10|"); // An operand can't end a statement
                    }

                    // TODO - Are we dealing with a quoted string?
                    if ("\"'".Contains(prob[0]) || (prob[0] == '[' && "XO".Contains(lastType)))
                    {
                        int f;

                        // do a search to find the end of the quote
                        if ("\"'".Contains(prob[0]))
                            f = prob.IndexOf(prob[0], 1);
                        else
                            f = prob.IndexOf(']', 1);

                        if (f > 0)
                        {
                            f++;
                            // save what we found
                            string part = prob[..f];
                            part = "C" + part[1..^1];

                            if (f >= prob.Length)
                                prob = string.Empty;
                            else
                                prob = prob[f..];

                            nospacesList.Add(part);
                            lastType = 'Q';
                            continue;

                        }
                        else
                            throw new Exception("10|");     // No end quote
                    }

                    // TODO - Date & datetime strings
                    if (prob[0] == '{')
                    {
                        int f = prob.IndexOf('}');
                        if (f > 0)
                        {
                            f++;
                            string part = prob[..f];
                            prob = prob[f..];
                            part = part[1..^1].Trim();

                            // Make sure the date is valid and then convert
                            // to D or T depending on what was sent
                            DateTime? dtchk = TimeLib.CToT(part);
                            if (dtchk is null)
                                throw new Exception("10|");
                            else
                            {
                                DateTime dtc = (DateTime)dtchk;
                                part = (part.Contains(':') ? "T" : "D") + dtc.ToString("yyyy-MM-ddTHH:mm:ss");
                            }

                            nospacesList.Add(part);
                            lastType = 'T';
                            continue;
                        }
                        else
                            throw new Exception("10|");
                    }

                    // TODO - Space handling?
                    if (prob[0] == ' ')
                    {
                        // just ignore it?
                        prob = prob[1..];
                    }
                }

                if (arrayPartStack.Count > 0)
                    throw new Exception("10||paren/bracket mismatch");

                if (nospacesList[^1].Length == 0)
                    nospacesList.RemoveAt(nospacesList.Count - 1);
            }
            catch (Exception ex)
            {
                err = 9992;
                errmsg = ex.Message;
            }

            if (hold.Contains("seek", ignCase))
            {
                int iii = 0;
            }

            return nospacesList;
        }



        // Takes the List<string> starting at a specific element and pops until
        // it reaches the end of the list or finds a comma.  It then solves
        // for that list, adds to the results list, and if more remains, continues
        // to process what remains.
        public static async Task<List<string>> ProcessPops(AppClass App, List<string> pops, int startingElement)
        {
            JAXMath jaxMath = new(App);
            List<string> results = [];
            List<string> rpn = [];

            while (startingElement < pops.Count)
            {
                if (pops[startingElement].Equals(","))
                {
                    if (rpn.Count > 0)
                    {
                        // Solve for and add to results
                        JAXObjects.Token answer = await jaxMath.MathSolve(rpn);
                        string r = answer.Element.Value.ToString() ?? string.Empty;
                        results.Add(answer.Element.Type + (r.Length < 1, " ", r));
                    }

                    // reset the rpn stack
                    rpn = [];
                    startingElement++;
                }
                else
                    rpn.Add(pops[startingElement++]);
            }

            // Anything left to process?
            if (rpn.Count > 0)
            {
                // Solve for and add to results
                JAXObjects.Token answer = await jaxMath.MathSolve(rpn);
                results.Add(answer.Element.Type + answer.Element.Value.ToString());
            }

            return results;
        }

        public static System.Type GetTokenDataType(JAXObjects.Token token)
        {
            System.Type result = token.Element.Type switch
            {
                "D" => typeof(DateOnly),
                "T" => typeof(DateTime),
                "N" => typeof(double),
                "C" => typeof(string),
                "L" => typeof(bool),
                "O" => typeof(JAXTables.JAXMemo),
                _ => throw new Exception("1662|" + token.Element.Type)
            };

            return result;
        }

        public static JAXObjects.Token SovleSimpleTokenString(string stok)
        {
            JAXObjects.Token tok = new();

            if (stok.Length > 1)
            {
                char stype = stok[0];
                switch (stype)
                {
                    case 'N':
                        if (double.TryParse(stok[1..], out double dval) == false) throw new Exception("3401||Numeric");
                        tok.Element.Value = dval;
                        break;

                    case 'L':
                        tok.Element.Value = stok[1..].ToUpper().Equals(".T.");
                        break;

                    case 'D':
                        if (DateTime.TryParse(stok[1..], out DateTime dto) == false) throw new Exception("3401||Date");
                        tok.Element.Value = dto.Date;
                        break;

                    case 'T':
                        if (DateTime.TryParse(stok[1..], out DateTime dtm) == false) throw new Exception("3401||DateTime");
                        tok.Element.Value = dtm;
                        break;

                    case 'X':
                        tok.Element.MakeNull();
                        break;

                    default:  // Character
                        tok.Element.Value = stok[1..];
                        break;
                }
            }
            else
                throw new Exception($"3401||Expression {stok}");

            return tok;
        }
    }
}
