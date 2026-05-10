/***********************************************************************
 * Math Parser - Open source (no copyright by Jon Lee Walker)
 *  I have not had the time or inclination to create a version that
 *  uses recursion, which I believe would reduce the amount of code.
 *
 * This class provides the ability to execute the math syntax 
 * including xBase functions.
 *
 * Variable references are passed by reference using a 
 * List<Preferences> object.  The structure needs to be populated 
 * before calling SolveMath()
 *
 * Setup(MC context, string intftype)
 *      Set up the local context and set the interface type.  
 *
 * CutZeros(string svalue, int significance)
 *      Cut off "0" from the end of a string.  Provide 
 *      significance (eg 2 = .00) if requested.  If significance = 0 
 *      and string ends in . followed by zeros, then returns an 
 *      integer representation.
 *
 *
 * The Math methods are used to evaluate an expression in a string 
 * to a value:
 *      MathParse:  Parse the string into a list
 *      MathMakeRPN:Put the list into RPN format
 *      MathSolve:  Step through the RPN list, solve the problem, 
 *                  and return an answer
 *
 * Some noteable differnces between VFP and this:
 *      All dates/datetimes are stored as yyyyMMddTHHmmss
 *      Dates just end in 00:00:00
 *
 * Extra Commands
 *      BASE36 - return a base36 string
 *      CUT(x) - cuts trailing zeros from number along with 
 *      a trailing period
 *          1.000 -> 1
 *          1.100 -> 1.1
 *      GENPK - generates a new PK
 *      NEV(x,y) - if x is null or empty, return y
 *      UDATETIME - returns current UTC time without any kind of 
 *                  special flag be careful!
 *
 * Unfinished Commands
 *      EPOCH - Number of milliseconds since the Linux epoch
 *      NVL - return parameter 2 if 1 is null
 *      RAT - return the rightmost match of a string
 *
 * Need to look over
 *      Make sure when adding or subtracting an integer from a 
 *      datetime it adds/subtracts seconds  and subtracting a 
 *      datetime from another returns an interger of the number
 *      of seconds elapsed.
 *
 ***********************************************************************
 * History
 * 
 * 1998.05 - JLW
 *      Created VB6 version from an article in a magazine (Dr Dobsb?)
 *      and expanded upon it to allow use of some basic 
 *      FoxPro 2.6 functions
 *      
 * 2008.10 - JLW
 *      Converted to C# and expanded the function list
 *      
 * 2023.12 - JLW
 *      Updating and adding to Vertican vMedia project
 *      
 * 2024.05 - JLW
 *      This version is for the VFP convrsion system and will 
 *      reference most capabilities, though many will throw a 
 *      "Not Implimented" exception. Some new features are being 
 *      introduced.
 *      
 * 2024.06 - JLW
 *      Altered SolveMath to return a SimpleToken instead of 
 *      a string (see JAXObjects for more information)
 *      
 * 2024.07 - JLW
 *      Added object and array variable support
 *
 * 2024.07.13 -JLW
 *      As it turns out, passing back a SimpleToken is not sufficient
 *      as some routines create or alter a variable.  A Token is needed
 *      to overwrite a variable.  Creating a MathStack class to replace 
 *      the string stack array and spending time altering the code.
 *
 * 2025.01.18 - JLW
 *      Starting User Defined Function (UDF) support
 *      
 * 2025.02.13 - JLW
 *      Adding DODEFAULT() support
 *      
 * 2025.04.07 - JLW 
 *      Moved to the JAXBase system.  
 *      Simple token now has decimal width field (Dec) and must be 
 *      manually  updated during math manipulation to match how xBase
 *      versions do it.  Will be testing over next several days.  I 
 *      believe this was the last big ask on xBase compatibility for 
 *      math and number manipulation.
 *                
 *      Will need to go through all functions and look for any place 
 *      that needs dec width updates.
 * 
 * 2025.08.27 - JLW
 *      I'm thinking the following new Financial functions would be a 
 *      great addition to the system but need to learn more about them
 *      to make sure they belong in the math functions, the CALCULATE 
 *      command, or both.
 *      
 *          AVG() - Average
 *          CNT() - Count
 *          COR() - Correlation - New
 *          COV() - Covariance - New
 *          IRR() - Internal Rate of Return - New
 *          MAX() - Maximum value
 *          MED() - Median Value - New
 *          MIN() - Minimum value
 *          NPV() - Net Present Value
 *          NFV() - Net Future Value - New
 *          NOP() - Number Of Payments - New
 *          PPP() - Payment Per Period - New
 *          STD() - Standard Deviation
 *          TIP() - Total Interest Paid - New
 *          VAR() - Variance
 * 
 * 2026.03.17 - JLW
 *      Added ASYNC logic where needed to support Avalonica.  This 
 *      is going to be a big change and will require a lot of testing.
 *      Debugged functionality.  Still need to go through and make 
 *      sure all functions are updating the decimal width correctly.
 *      
 * 2026.04.20 - JLW
 *      Updated math to handle E/M types correctly.  When E/M types
 *      are converted to string, they should return an empty string.
 *      
 **********************************************************************/

using JAXBase.Core;
using JAXBase.Utilities;
using JAXBase.XBase;

namespace JAXBase.Math
{
    public class JAXMath()
    {
        //private int stackcount = 0;
        //private string[] stack = new string[255];
        private int varCount = 0;  // Number of variables in waiting
        private int inArray = 0;
        private bool inVar = false;
        private readonly JAXMathAux MathAux = new();
        private int DecWidth = 0;


        /***************************************************************
         * 2024-07-13
         *      I realized today that I need to be able to return
         *      a variable token to the calling program and have
         *      created this private class to replace the original
         *      string stack array.
         *
         *      Various functions return an array or object and the
         *      only way to accomplish that is by passing back a 
         *      variable token.  Each variable reference has a token
         *      associated with it that holds the variable value, array
         *      elements, or object properties.
         ***************************************************************/
        private class MathStack
        {
            List<JAXObjects.Token> Tokens = [];

            public int Count { private set { _ = value; } get { return Tokens.Count; } }

            public JAXObjects.Token Token
            {
                set { Tokens[0] = value; }
                get
                {
                    if (Tokens.Count < 1)
                        throw new Exception("9989|Stack underflow|Token");

                    return Tokens[0];
                }
            }

            public void Clear()
            {
                Tokens = [];
            }

            /// <summary>
            /// Pop the most recent token from the math stack
            /// </summary>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public JAXObjects.Token PopToken()
            {
                JAXObjects.Token tk;

                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|PopToken");

                tk = Tokens[0];
                Tokens.RemoveAt(0);
                return tk;
            }

            /// <summary>
            /// Push a token to the math stack
            /// </summary>
            /// <param name="token"></param>
            public void PushToken(JAXObjects.Token token) { Tokens.Insert(0, token); }

            public string Type() { return Type(0); }
            public string Type(int idx)
            {
                if (Tokens.Count < idx)
                    throw new Exception("9989|Stack underflow|Type");

                return Tokens[idx].Element.Type ?? string.Empty;
            }

            public string TType() { return TType(0); }
            public string TType(int idx)
            {
                if (Tokens.Count < idx)
                    throw new Exception("9989|Stack underflow|TType");

                return Tokens[idx].TType ?? string.Empty;
            }

            /// <summary>
            /// Get the string value from the bottom of the stack
            /// </summary>
            /// <returns></returns>
            public string GetString() { return GetString(0); }

            /// <summary>
            /// Get a string value from the math stack by index
            /// </summary>
            /// <returns></returns>
            public string GetString(int idx)
            {
                if (Tokens.Count < idx)
                    throw new Exception("9989|Stack underflow|GetString");

                return Tokens[idx].Element.Value.ToString() ?? string.Empty;
            }

            /// <summary>
            /// Update the bottommost token with a string
            /// </summary>
            /// <param name="value"></param>
            /// <exception cref="Exception"></exception>
            public void SetString(string value)
            {
                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|SetString");

                Tokens[0].Element.Value = value;
            }

            /// <summary>
            /// Pus a new token to the stack based on the type and string information
            /// </summary>
            /// <param name="value"></param>
            /// <param name="type"></param>
            public void PushTokenRef(string value, string type)
            {
                JAXObjects.Token token = new();
                token.Element.Value = value;
                token.TType = type;
                Tokens.Insert(0, token);
            }


            /// <summary>
            /// Push a string to the stack
            /// </summary>
            /// <param name="value"></param>
            public void PushString(string value)
            {
                JAXObjects.Token token = new();
                //token._avalue[0].Value = value;
                token.Element.Value = value;
                Tokens.Insert(0, token);
            }

            /// <summary>
            /// Pop a value off the stack as a string
            /// </summary>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public string PopString()
            {
                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|PopString");

                // Popping an E/M TType to string always produces an empty string
                string value = "EM".Contains(Tokens[0].TType) ? "" : Tokens[0].Element.Value.ToString() ?? string.Empty;
                Tokens.RemoveAt(0);
                return value;
            }

            /// <summary>
            ///  Pop a value off the stack in the format Type+ValueString
            /// </summary>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public string PopTokenString()
            {
                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|PopTokenString");

                string value = "EM".Contains(Tokens[0].TType) ? "C" : Tokens[0].Element.Type + Tokens[0].Element.Value.ToString() ?? string.Empty;
                Tokens.RemoveAt(0);
                return value;
            }

            public void PushValue(object? value)
            {
                JAXObjects.Token token = new();
                if (value is null)
                    token.Element.MakeNull();
                else
                    token.Element.Value = value;
                Tokens.Insert(0, token);
            }

            public void SetValue(object? value, int dec)
            {
                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|SetValue");

                if (value is null)
                {
                    Tokens[0].Element.MakeNull();
                    Tokens[0].Element.Dec = 0;
                }
                else
                {
                    Tokens[0].Element.Value = value;
                    Tokens[0].Element.Dec = dec;
                }
            }

            public void SetValue(object? value)
            {
                if (Tokens.Count < 1)
                    throw new Exception("9989|Stack underflow|SetValue");

                if (value is null)
                    Tokens[0].Element.MakeNull();
                else
                    Tokens[0].Element.Value = value;
            }
        }

        MathStack mathStack = new();

        /***************************************************************
         * Remove trailing zeros from the end of the string
         * and if the decimal is left at the end, remove it.
         * 
         * If significance>0 then it will leave that many
         * zeros after the decimal point.
         *
         * We're going to add a leading zero if the string
         * is empty or there is a leading decimal point.
         ***************************************************************
         *                      Math Evaluation Methods
         *
         * These methods provide an old tech brute force way of 
         * evaluating a string where a string is parsed into a list, 
         * the list is made RPN and the RPN list is processed.  Solving
         * a RPN (Reverse Polish Notation) problem is an easy thing for
         * a computer to do as it's completely left to right.
         *
         * Normal notation is:  abs(A + B * C - 1)  where B * C is 
         * processed, then + A is added, 1 is subtracted, then the 
         * absolute value is returned.
         *
         * RPN notation for same equation is: B C * A + 1 - abs()
         * 
         * The parse method takes a statement and figures out what the 
         * parts are.  Numbers are prefaced with "N", strings are
         * prefaced with a C, dates are prefaced with a D, variables 
         * are prefaced with an underscore, and functions are prefaced 
         * with an accent (`) sign. In the above example, it returns a 
         * list as follows:
         *
         * { "`ABS", "(", "_A", "+", "_B", "*", "_C", "-", "N1", ")" }
         *
         * The makerpn method then takes the list and formats it into 
         * RPN so it would return the following:
         *
         * { "_B", "_C", "*", "_A", "+", "N1", "-", "`ABS" }
         *
         * The calling routine would then step through the list and 
         * fill in the values for each varable.  Once completed, it 
         * then calls the solve method which will process the list 
         * and return an answer.
         *
         ***************************************************************
         * History
         *
         * 2009-07-12 - JLW
         *      Finally finished the first pass from the original 
         *      QuickBASIC version. Virtually no error checking and 
         *      needs further testing to make sure everything is 
         *      correctly programmed.
         *
         * 2024-06-01 - JLW
         *      Added this code to the VFP2C# system and will begin 
         *      to radically expand the code to make it compatible with 
         *      many xBase systems
         *      
         * 2025-05-01 - JLW
         *      Move this code to the JAXBase (Just Another XBase) 
         *      system so that I can test while I continue to expand 
         *      it's capabilities
         *      
         * 2025-05-20 - JLW
         *      Updating to handle object references and test out UDF 
         *      calls.  This has become somewhat obfuscated and I will 
         *      begin the process of cleaning it up and making sure 
         *      lots of informative comments are included in the code 
         *      so that I don't have to spend so much time trying to 
         *      remember what something is supposed to do.
         ***************************************************************/

        /***************************************************************
         * Solve the rpn list returning an JAX object token
         ***************************************************************/
        public async Task<JAXObjects.Token> MathSolve(List<string> rpn)
        {
            List<string> pop;

            if (rpn.Count > 5 && rpn[4].Contains("flags", StringComparison.OrdinalIgnoreCase))
            {
                int iii = 0;
            }

            double val1, val2;
            int dec1 = 0;
            int dec2 = 0;
            string string1, string2, stype1, stype2;
            JAXObjects.Token t1, t2;
            JAXObjects.Token tAnswer = new() { TType = "U" };
            bool exactMatch = Program.CurrentApp.CurrentDS.JaxSettings.Exact;
            int decimalPlaces = Program.CurrentApp.CurrentDS.JaxSettings.Decimals;

            // Set the environment for another Solve
            mathStack.Clear();
            inArray = 0;

            foreach (string _rpn in rpn)
            {
                // Handle the NOT operator
                if (_rpn.Equals("!", StringComparison.OrdinalIgnoreCase))
                {
                    if (mathStack.Count > 0)
                    {
                        // process if variable without popping
                        // from the stack
                        mathStack.Token = await SolveIfVar(false);

                        if (mathStack.Token.Element.Type.Equals("L", StringComparison.OrdinalIgnoreCase))
                        {
                            mathStack.Token.Element.Value = !mathStack.Token.Element.ValueAsBool;
                            continue;
                        }
                        else
                            AppErrorHandling.SetError(9, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name); // data type mismatch
                    }
                    else
                        AppErrorHandling.SetError(1231, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);  // Missing operand
                }

                // Start of variable & object handling
                if (_rpn[..1].Equals("_", StringComparison.OrdinalIgnoreCase))
                {
                    // We found a variable, array variable, or UDF
                    // so add it and expect the next token to be
                    // an object part, a left bracket/paren, an
                    // operator, or end of rpn list
                    varCount++;
                    inVar = true;
                    mathStack.PushTokenRef(_rpn[1..], "V");

                    // loop around
                    continue;
                }

                // Handling of JAX true & false expressions
                if (_rpn.Equals(".T.", StringComparison.OrdinalIgnoreCase) || _rpn.Equals(".F.", StringComparison.OrdinalIgnoreCase))
                {
                    mathStack.PushValue(_rpn.Equals(".T.", StringComparison.OrdinalIgnoreCase));
                    inVar = false;
                    continue;
                }

                if (_rpn.Equals(".null.", StringComparison.OrdinalIgnoreCase))
                {
                    mathStack.PushValue(null);
                    inVar = false;
                    continue;
                }

                // Object references
                if (_rpn[..1].Equals(".", StringComparison.OrdinalIgnoreCase))
                {
                    // We found an ObjectPart, are we currently
                    // in a Varialble reference?
                    if (inVar)
                    {
                        // Yes, so put it onto the stack as is
                        mathStack.PushTokenRef(_rpn, "v");
                    }
                    else
                    {
                        // No, so push the WITH keyword followed
                        // by the object part reference
                        mathStack.PushTokenRef("with", "V");

                        mathStack.PushTokenRef(_rpn, "v");
                        inVar = true;
                        varCount++;
                    }

                    // loop around
                    continue;
                }

                // JAX Functions don't have () so this delimits the
                // end of the expression as you might have 1, 2, or
                // more parameters
                if (_rpn[..1].Equals("~", StringComparison.OrdinalIgnoreCase) && inArray > 0)
                {
                    // Finding a ~ in an array means there were
                    // parenthises that were not part of a
                    // function call
                    inVar = false;
                    continue;
                }

                // If there is a variable in waiting, the
                // next token needs to be a left braket
                // or paren, otherwise it's a simple variable
                // and needs to be resolved and stacked
                if (inVar && (_rpn.Equals("[") || _rpn.Equals("(")))
                {
                    // Add left bracket/parent to the stack
                    mathStack.PushString(_rpn);
                    inArray++;
                    continue;
                }

                // parameter delimiter for array
                if (_rpn.Equals(",") && inArray > 0)
                {
                    //SolveArrayParam(_rpn);
                    mathStack.PushTokenRef(",", "c");    // comma flag
                    continue;
                }

                // Right bracket/parent closes the literal - TODO: I think we should solve to , or [
                if (_rpn.Equals("]"))
                {
                    if (inArray == 0)
                        throw new Exception("10|");

                    //SolveArrayParam(_rpn);

                    // Add right bracket/parent to the stack
                    mathStack.PushString(_rpn);
                    inArray--;
                    continue;
                }

                // JAX built in functions like str(), strtran(), ect
                // dont have the parens in the RPN.  Since they can
                // have 1, 2, or more parameters, this tells the
                // code when to stop reading them in
                if (_rpn.Equals("~", StringComparison.OrdinalIgnoreCase))
                {
                    // End of a function call
                    mathStack.PushString("~");
                }
                else if (" CNLDT".IndexOf(_rpn[..1]) > 0)
                {
                    // It's a literal, so break it out and put it
                    // into a token before storing to the stack
                    inVar = false;

                    JAXObjects.Token oAnswer = new();
                    string sAnswer = _rpn;
                    char cType = 'U';

                    if (sAnswer.Length > 0)
                    {
                        cType = sAnswer[0];
                        sAnswer = sAnswer![1..];
                    }

                    switch (cType)
                    {
                        case 'N':       // Number
                            if (sAnswer.Contains('.'))
                            {
                                if (double.TryParse(sAnswer, out double dval) == false)
                                    dval = 0D;
                                oAnswer.Element.Value = dval;
                                oAnswer.Element.Dec = sAnswer.Length - sAnswer.IndexOf(".") - 1;

                            }
                            else
                            {
                                if (long.TryParse(sAnswer, out long lval) == false)
                                    lval = 0L;
                                oAnswer.Element.Value = lval;
                                oAnswer.Element.Dec = 0;
                            }
                            break;

                        case 'L':       // Logical
                            oAnswer.Element.Value = sAnswer.Equals(".T.", StringComparison.OrdinalIgnoreCase);
                            break;

                        case 'C':       // Character
                            oAnswer.Element.Value = sAnswer;
                            break;

                        case 'T':       // DateTime
                            if (DateTime.TryParse(sAnswer, out DateTime tval) == false)
                                tval = DateTime.MinValue;
                            oAnswer.Element.Value = tval;
                            break;

                        case 'D':       // DateOnly
                            if (DateOnly.TryParse(sAnswer, out DateOnly doval) == false)
                                doval = DateOnly.MinValue;
                            oAnswer.Element.Value = doval;
                            break;
                    }

                    mathStack.PushToken(oAnswer);
                }
                else
                {
                    if (_rpn.Equals("(", StringComparison.OrdinalIgnoreCase))
                    {
                        // found a left paren, push it to the stack
                        mathStack.PushString("(");
                    }
                    else if (" +-*/^%".IndexOf(_rpn) > 0)
                    {
                        // Trigger to resolve if inVar = true
                        if (inVar && inArray == 0)
                        {
                            inVar = false;
                            await SolveIfVar(false);
                        }

                        // TODO - what a mess!  Clean this up!

                        // is there more than 1 item on the stack?
                        if (mathStack.Count > 1)
                        {
                            t2 = await SolveIfVar(true);
                            string2 = t2.AsString();
                            stype2 = t2.Element.Type;
                            dec2 = t2.Element.Dec;

                            t1 = await SolveIfVar(false);
                            string1 = t1.AsString();
                            stype1 = t1.Element.Type;
                            dec1 = t1.Element.Dec;

                            List<string> vInfo = [];

                            if (!double.TryParse(string2.Trim(), out val2)) val2 = 0d;
                            if (!double.TryParse(string1.Trim(), out val1)) val1 = 0d;

                            if (stype1.Equals("C") || stype2.Equals("C"))
                            {
                                if (_rpn[..1].Equals("+"))
                                {
                                    // Concatinating two strings
                                    mathStack.SetString(string1 + string2);
                                }
                                else
                                    throw new Exception("9|");
                            }
                            else if (JAXLib.InListC(stype1 + stype2, "DN", "ND"))
                            {
                                // Date math
                                DateOnly dt;
                                int days;

                                if ((stype1 + stype2).Equals("DN"))
                                {
                                    dt = t1.AsDate();
                                    days = t2.AsInt();
                                }
                                else
                                {
                                    dt = t2.AsDate();
                                    days = t1.AsInt();
                                }

                                switch (_rpn[..1])
                                {
                                    case "+":
                                        mathStack.SetValue(dt.AddDays(days), 0);
                                        break;

                                    case "-":
                                        mathStack.SetValue(dt.AddDays(-days), 0);
                                        break;

                                    default:
                                        throw new Exception("9|");
                                }
                            }
                            else if (JAXLib.InListC(stype1 + stype2, "TN", "NT"))
                            {
                                // DateTime math
                                DateTime dt = t1.AsDateTime();
                                int secs;

                                if ((stype1 + stype2).Equals("DN"))
                                {
                                    dt = t1.AsDateTime();
                                    secs = t2.AsInt();
                                }
                                else
                                {
                                    dt = t2.AsDateTime();
                                    secs = t1.AsInt();
                                }

                                switch (_rpn[..1])
                                {
                                    case "+":
                                        mathStack.SetValue(dt.AddSeconds(secs), 0);
                                        break;

                                    case "-":
                                        mathStack.SetValue(dt.AddSeconds(-secs), 0);
                                        break;

                                    default:
                                        throw new Exception("9|");
                                }
                            }
                            else
                            {
                                if ((stype1 + stype2).Equals("NN"))
                                {
                                    // Numeric math
                                    switch (_rpn[..1])
                                    {
                                        case "+":
                                            val1 += val2;
                                            DecWidth = DecWidth > dec1 ? DecWidth : dec1;
                                            DecWidth = DecWidth > dec2 ? DecWidth : dec2;
                                            break;
                                        case "-":
                                            val1 -= val2;
                                            DecWidth = DecWidth > dec1 ? DecWidth : dec1;
                                            DecWidth = DecWidth > dec2 ? DecWidth : dec2;
                                            break;
                                        case "*":
                                            val1 *= val2;
                                            DecWidth = DecWidth > dec1 + dec2 ? DecWidth : dec1 + dec2;
                                            break;
                                        case "/":
                                            if (val2 == 0) throw new Exception("1307|");
                                            val1 /= val2;
                                            DecWidth = DecWidth > dec1 + dec2 ? DecWidth : dec1 + dec2;
                                            break;
                                        case "^":
                                            val1 = System.Math.Pow(val1, val2);
                                            DecWidth = DecWidth > decimalPlaces ? DecWidth : decimalPlaces;
                                            DecWidth = DecWidth > dec1 ? DecWidth : dec1;
                                            break;
                                        case "%":
                                            val1 %= val2;   // modulus always returns an int/long value
                                            val1 = System.Math.Truncate(val1);
                                            DecWidth = DecWidth > dec1 ? DecWidth : dec1;
                                            break;
                                    }

                                    mathStack.SetValue(val1, DecWidth);
                                }
                                else
                                    throw new Exception("9|");
                            }
                        }
                        else
                        {
                            // We have a problem with the math
                            // since there is an operator but
                            // only one item to work on
                            AppErrorHandling.SetError(1231, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                        }
                    }
                    else if (" @=#><{}&|$?".IndexOf(_rpn) > 0)
                    {
                        inVar = false;

                        // We are comparing
                        if (mathStack.Count > 1)
                        {
                            bool boolAnswer = false;

                            t2 = await SolveIfVar(true);
                            string2 = t2.AsString();
                            stype2 = t2.Element.Type;

                            t1 = await SolveIfVar(false);
                            string1 = t1.AsString();
                            stype1 = t1.Element.Type;

                            // TODO - What happens when we compare
                            // objects?  JAX should throw an error
                            // Only compare if types are compatible
                            if (JAXLib.InListC(stype1 + stype2, "NN", "DD", "CC", "TT", "LL", "DT", "TD", "OO") == false)
                                throw new Exception($"11||Attempting to compare {stype1 + stype2}");

                            if (stype1.Equals("O"))
                            {
                                JAXObjectWrapper jow1 = (JAXObjectWrapper)t1.Element.Value;
                                JAXObjectWrapper jow2 = (JAXObjectWrapper)t2.Element.Value;

                                // Comparing objects
                                // we're comparing strings
                                switch (_rpn[..1])
                                {
                                    case "?":   // pointing at the same object
                                        string1 = jow1.ClassID == jow1.ClassID ? "L.T." : "L.F.";
                                        break;

                                    case "#":
                                        string1 = jow1.ClassID == jow1.ClassID ? "L.F." : "L.T.";
                                        break;

                                    case "=":   // Ojbect properties match
                                        if (jow1.ClassID == jow2.ClassID)
                                            string1 = "L.T.";
                                        else
                                            string1 = await MathFuncsC.CompObj(jow1, jow2) ? "L.T." : "L.F.";
                                        break;

                                    case ">":
                                        string1 = String.Compare(jow1.ClassID, jow1.ClassID) > 0 ? "L.T." : "L.F.";
                                        break;

                                    case "<":
                                        string1 = String.Compare(jow1.ClassID, jow1.ClassID) < 0 ? "L.T." : "L.F.";
                                        break;

                                    default:
                                        throw new Exception("9|");
                                }

                            }
                            else if (stype1.Equals("C"))
                            {
                                int slen1 = string1.Length;
                                int slen2 = string2.Length;
                                bool slenAll = slen1 == slen2;
                                bool slenZero = slen1 == 0 || slen2 == 0;

                                // we're comparing strings - I'm getting the correct results
                                // but I'm definitely not doing it the same way as VFP.
                                // TODO - Play around with space and zero filling like VFP
                                // to see if it makes better sense and/or takes less code
                                switch (_rpn[..1])
                                {
                                    case "?":   // Exact match
                                        boolAnswer = string1 == string2;
                                        break;

                                    case "=":
                                        if (Program.CurrentApp.CurrentDS.JaxSettings.ANSI && slenZero == false)
                                        {
                                            // Compares up to shortest string as long
                                            // both of them are at least 1 char long
                                            if (slen1 > slen2)
                                                string1 = string1[..slen2];
                                            else if (slen2 > slen1)
                                                string2 = string1[..slen1];
                                        }

                                        if (exactMatch)
                                            boolAnswer = string1 == string2;
                                        else
                                            boolAnswer = string1.StartsWith(string2);
                                        break;


                                    case "@":  // !=
                                    case "#":   // <>
                                        if (Program.CurrentApp.CurrentDS.JaxSettings.ANSI && slenZero == false)
                                        {
                                            // Compares up to shortest string
                                            if (slen1 > slen2)
                                                string1 = string1[..slen2];
                                            else if (slen2 > slen1)
                                                string2 = string1[..slen1];
                                        }

                                        if (exactMatch)
                                            boolAnswer = string1 == string2;
                                        else
                                            boolAnswer = string1.StartsWith(string2);
                                        break;

                                    case ">":
                                        if (slenAll == false && slenZero)
                                            string1 = "L.F.";    // one but not both are empty string
                                        else if (exactMatch)
                                            boolAnswer = string1.CompareTo(string2) > 0;
                                        else
                                        {
                                            if (string1.Length > string2.Length)
                                                string1 = string1[..string2.Length];
                                            else if (string2.Length > string1.Length)
                                                string2 = string2[..string1.Length];

                                            boolAnswer = string1.CompareTo(string2) > 0;
                                        }
                                        break;

                                    case "<":
                                        if (slenAll == false && slen1 == 0)
                                            string1 = "L.T.";    // one but not both are empty string
                                        else if (exactMatch)
                                            boolAnswer = string1.CompareTo(string2) < 0;
                                        else
                                        {
                                            if (string1.Length > string2.Length)
                                                string1 = string1[..string2.Length];

                                            boolAnswer = string1.CompareTo(string2) < 0;
                                        }
                                        break;

                                    case "}":
                                        if (exactMatch)
                                            boolAnswer = string1.CompareTo(string2) >= 0;
                                        else
                                            boolAnswer = string1.CompareTo(string2) >= 0;
                                        break;

                                    case "{":
                                        if (exactMatch)
                                            boolAnswer = string1.CompareTo(string2) <= 0;
                                        else
                                        {

                                            if (slen1 > slen2)
                                                string1 = string1[..slen2];
                                            else if (slen2 > slen1)
                                                string2 = string2[..slen1];

                                            boolAnswer = string1.CompareTo(string2) <= 0;
                                        }
                                        break;

                                    case "&":
                                    case "|":   // Invalid
                                        throw new Exception("11|");

                                    case "$":
                                        boolAnswer = (string2.IndexOf(string1) + 1) > 0;
                                        break;
                                }
                            }
                            else if (stype1.Equals("L"))
                            {
                                // we're comparing T/F
                                switch (_rpn[..1])
                                {
                                    case "=":
                                        boolAnswer = string1 == string2;
                                        break;
                                    case "@":
                                    case "#":
                                        boolAnswer = string1 != string2;
                                        break;
                                    case ">":
                                        boolAnswer = string1.CompareTo(string2) > 0;
                                        break;
                                    case "<":
                                        boolAnswer = string1.CompareTo(string2) < 0;
                                        break;
                                    case "}":
                                        boolAnswer = string1.CompareTo(string2) >= 0;
                                        break;
                                    case "{":
                                        boolAnswer = string1.CompareTo(string2) <= 0;
                                        break;
                                    case "&":
                                        boolAnswer = string1.Equals(".T.") && string2.Equals(".T.");
                                        break;
                                    case "|":
                                        boolAnswer = string1.Equals(".T.") || string2.Equals(".T.");
                                        break;
                                    default:
                                        throw new Exception("11|");
                                }
                            }
                            else if (JAXLib.InListC(stype1, "D", "T"))
                            {
                                // we're comparing date/datetime strings
                                switch (_rpn[..1])
                                {
                                    case "=":
                                        boolAnswer = string1 == string2;
                                        break;
                                    case "@":
                                    case "#":
                                        boolAnswer = string1 != string2;
                                        break;
                                    case ">":
                                        boolAnswer = string1.CompareTo(string2) > 0;
                                        break;
                                    case "<":
                                        boolAnswer = string1.CompareTo(string2) < 0;
                                        break;
                                    case "}":
                                        boolAnswer = string1.CompareTo(string2) >= 0;
                                        break;
                                    case "{":
                                        boolAnswer = string1.CompareTo(string2) <= 0;
                                        break;
                                    default:
                                        throw new Exception("11|");
                                }
                            }
                            else
                            {
                                // We're comparing numbers
                                val1 = t1.AsDouble();
                                val2 = t2.AsDouble();

                                switch (_rpn[..1])
                                {
                                    case "=":
                                        boolAnswer = val1 == val2;
                                        break;
                                    case "@":
                                    case "#":
                                        boolAnswer = val1 != val2;
                                        break;
                                    case ">":
                                        boolAnswer = val1 > val2;
                                        break;
                                    case "<":
                                        boolAnswer = val1 < val2;
                                        break;
                                    case "}":
                                        boolAnswer = val1 >= val2;
                                        break;
                                    case "[":
                                        boolAnswer = val1 <= val2;
                                        break;
                                    default:
                                        throw new Exception("11|");
                                }
                            }

                            mathStack.SetValue(boolAnswer);
                        }
                    }
                    else
                    {
                        inVar = false;

                        // We're left with functions - handle the array based functions separately
                        if (JAXLib.InList(_rpn, "`ACLASS", "`ACLASS", "`ACOPY", "`ADATABASES", "`ADBOBJECTS", "`ADDBS", "`ADDPROPERTY", "`ADEL", "`ADIR", "`ADLLS", "`ADOCKSTATE", "`AELEMENT", "`AERROR", "`AEVENTS",
                            "`AFIELDS", "`AFONT", "`AGETCLASS", "`AGETFILEVERSION", "`AINS", "`AINSTANCE", "`ALEN", "`ALINES", "`AMEMBERS", "`AMOUSEOBJ", "`ANETRECOURCES", "`APRINTERS", "`APROCINFO", "`ASCAN",
                            "`ASELOBJ", "`ASESSIONS", "`ASORT", "`ASQLHANDLES", "`ASTACKINFO", "`ASUBSCRIPT", "`ATAGINFO", "`AUSED", "`AVCXCLASSES", "`REMOVEPROPERTY"))
                        {
                            // Functions that deal with arrays need
                            // special handling
                            pop = PopStackItems(100);

                            // Most array functions start with an A
                            if (JAXLib.InList(_rpn, "`REMOVEPROPERTY"))
                                mathStack.Token = await MathFuncsR.R(Program.CurrentApp, _rpn, pop);
                            else
                                mathStack.Token = await MathFuncsA.A0(Program.CurrentApp, _rpn, pop);
                        }
                        else
                        {
                            List<JAXObjects.Token> popTokens = await PopStackTokens(100);

                            pop = [];
                            for (int i = 0; i < popTokens.Count; i++)
                                pop.Add(popTokens[i].Element.Type + popTokens[i].AsString());

                            stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
                            string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);

                            switch (_rpn[1].ToString())
                            {
                                case "A":
                                    mathStack.Token = MathFuncsA.A1(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "B":
                                    mathStack.Token = MathFuncsB.B(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "C":
                                    mathStack.Token = await MathFuncsC.C(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "D":
                                case "E":
                                    mathStack.Token = await MathFuncsD.D(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "F":
                                    mathStack.Token = MathFuncsF.F(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "G":
                                    mathStack.Token = await MathFuncsG.G(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "H":
                                case "I":
                                    mathStack.Token = MathFuncsH.H(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "J":
                                case "K":
                                case "L":
                                    mathStack.Token = MathFuncsL.L(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "M":
                                case "N":
                                    mathStack.Token = await MathFuncsM.M(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "O":
                                case "P":
                                case "Q":
                                    mathStack.Token = MathFuncsP.P(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "R":
                                    mathStack.Token = await MathFuncsR.R(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "S":
                                    mathStack.Token = await MathFuncsS.S(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "T":
                                    mathStack.Token = await MathFuncsT.T(Program.CurrentApp, _rpn, pop);
                                    break;

                                case "U":
                                case "V":
                                case "W":
                                case "X":
                                case "Y":
                                case "Z":
                                    mathStack.Token = MathFuncsU.U(Program.CurrentApp, _rpn, pop);
                                    break;

                                default:
                                    // Some form of var to resolve
                                    JAXObjects.Token v = new();
                                    v.Element.Value = _rpn;
                                    popTokens.Insert(0, v);
                                    mathStack.Token = await GetVar(popTokens);
                                    break;
                            }
                        }
                    }
                }
            }

            // Should have just one thing left on the stack
            if (mathStack.Count > 0)
            {
                if (inArray > 0) throw new Exception("10|");

                await SolveIfVar(false);
                tAnswer = mathStack.PopToken();
            }
            else
                throw new Exception("9989||Math");

            return tAnswer;
        }


        /***************************************************************
         * Used to grab one or more items off the stack with and return
         * them in a List<Token>.  Includes a hard stop count. The 
         * tilde char (~) is also a hard stop. Variables are 
         * automatically processed as they are found.
         ***************************************************************/
        private async Task<List<JAXObjects.Token>> PopStackTokens(int iHardStop)
        {
            List<JAXObjects.Token> pop = [];

            while (mathStack.Count > 0 && iHardStop-- > 0 && mathStack.GetString().Equals("~", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (mathStack.GetString().Equals("~", StringComparison.OrdinalIgnoreCase) || mathStack.Token.TType.Equals("c", StringComparison.OrdinalIgnoreCase))
                    _ = mathStack.PopString();  // Get rid of ~ and comma delimeters
                else
                {
                    JAXObjects.Token t = await SolveIfVar(true);
                    pop.Insert(0, t);  // we want them left to right
                }
            }

            return pop;
        }



        /***************************************************************
         ***************************************************************/
        private List<string> PopStackItems(int iHardStop)
        {
            List<string> pop = [];

            // grab parameters until you find the ~
            // or it's the end of the stack or end
            // of the hard stop count
            while (mathStack.Count > 0 && iHardStop-- > 0 && mathStack.GetString().Equals("~", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (mathStack.GetString().Equals("~", StringComparison.OrdinalIgnoreCase))
                    _ = mathStack.PopString();
                else
                {
                    if (mathStack.Token.TType.Equals("V", StringComparison.OrdinalIgnoreCase))
                        pop.Insert(0, "_" + mathStack.PopString());
                    else
                    {
                        if (mathStack.Token.TType.Equals("c", StringComparison.OrdinalIgnoreCase))
                        {
                            //pop.Insert(0, ",");
                            mathStack.PopString();
                        }
                        else
                            pop.Insert(0, mathStack.PopTokenString());  // we want them left to right
                    }
                }
            }

            return pop;
        }



        /***************************************************************
         ***************************************************************/
        public List<string> ReturnRPN(string slEquation)
        {
            List<string> rpn = [];

            List<string> parsestring = MathAux.MathParse(slEquation);

            if (MathAux.err == 0)
                rpn = MathAux.MathMakeRPN(parsestring);

            return rpn;
        }


        /***************************************************************
         ***************************************************************/
        public async Task<GenericClass> SolveMath(string slEquation)
        {
            GenericClass gc = new();
            gc.Value._avalue[0].MakeNull();
            mathStack = new();          // Refresh the stack

            slEquation = slEquation.Length < 1 ? ".T." : slEquation;    // Assumes .T. when nothing to process

            if (slEquation.Contains("seek", StringComparison.OrdinalIgnoreCase))
            {
                int iii = 0;
            }

            List<string> parsestring = MathAux.MathParse(slEquation);

            if (MathAux.err == 0)
            {
                // TODO - Func has ( in it
                List<string> rpn = MathAux.MathMakeRPN(parsestring);

                if (MathAux.err == 0)
                    gc.Value = await MathSolve(rpn);
            }

            if (MathAux.err > 0)
                AppErrorHandling.SetError(MathAux.err, MathAux.errmsg, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            gc.Error = AppErrorHandling.LastErrorNo();
            return gc;
        }



        /***************************************************************
         * Used to convert the current stack item to
         * a value if there is a variable otherwise
         * Sent a true if stack should be decramented
         ***************************************************************/
        private async Task<JAXObjects.Token> SolveIfVar(bool bPop)
        {
            JAXObjects.Token sVal = mathStack.Token;
            _ = mathStack.PopString();
            bool inArray = false;

            string test = sVal.TType;
            if (sVal.AsString().Length > 0 && ("Vv".Contains(test) || "]".Contains(sVal.AsString())))
            {
                List<JAXObjects.Token> vList = [];
                bool done = false;
                while (done == false)
                {
                    if (sVal.TType.Equals("V") && inArray == false)
                    {
                        vList.Add(sVal);

                        if (vList.Count > 1)
                        {
                            // working with an array so reverse
                            // the list for GetVar()
                            vList.Reverse();
                        }

                        sVal = await GetVar(vList);
                        varCount--;
                        done = true;    // Don't need it, but OCD says do it
                        break;
                    }
                    if (sVal.TType.Equals("V") && inArray)
                    {
                        // Solve for this variable
                        mathStack.PushToken(sVal);
                        await SolveIfVar(false);
                        sVal = mathStack.PopToken();
                        vList.Add(sVal);
                        sVal = mathStack.PopToken();
                    }
                    else if (sVal.TType.Equals("v"))
                    {
                        // Working with table or class
                        vList.Add(sVal);

                        // when we get a "v", we stay here until we get the header "V"
                        while (mathStack.Token.TType.Equals("v", StringComparison.OrdinalIgnoreCase))
                        {
                            // Found a variable reference; stack it.
                            vList.Add(mathStack.Token);
                            _ = mathStack.PopString();

                            // Break out when we get to the header variable
                            // GetVarObject want's head var at bottom of list
                            if (vList[^1].TType.Equals("V"))
                            {
                                sVal = await GetVarObject(vList);
                                varCount--;
                                done = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // just stack it until and get the
                        // next one until we find the variable
                        vList.Add(sVal);
                        if (sVal.AsString().Equals("]"))
                            inArray = true;
                        else if (sVal.AsString().Equals("["))
                            inArray = false;
                        sVal = mathStack.PopToken();
                    }
                }
            }

            // Do we want to leave the results in the stack?
            if (bPop == false)
                mathStack.PushToken(sVal);
            //mathStack.PushValue(sVal.Element.Value);

            return sVal;
        }



        /***************************************************************
         * Create and return a token representing a current element in 
         * a variable, array, or UDF
         * 
         * The list holds the var/UDF name and optional parameters
         * 
         * First thing off the list should be the var/UDF name
         * then you'll optionally see a left bracket/paren, followed 
         * by 0, 1, or multiple parameters separated by commas, and 
         * finally the right bracket/paren
         * 
         * Nothing get's solved until you pul everything
         * 
         * Example:  _A  [ 1 , 7 ]
         * 
         *      Get _A
         *      
         *      Get [, 1, 7, and ] putting 1 into p[0] and 7 into p[1]
         *      
         *      Nothing left? 
         *          Are there any parametrs?  If no, solve if for
         *          simple variable.  
         *          Is it an array?  Two parameters max for arrays
         *          then solve for array
         *          If not var/array then try to call the UDF named
         *          A sending the parameters captured and get a 
         *          return value
         *          
         *      Return value from solve process sent back to 
         *      calling routine
         *      
         *      Bonus: [] is only for array references
         * 
         ***************************************************************/
        private async Task<JAXObjects.Token> GetVar(List<JAXObjects.Token> rpn)
        {
            JAXObjects.Token sResult = new();
            JAXObjects.Token tk;
            List<JAXObjects.Token> parms = [];
            int row = -1;
            int col = -1;

            string sVar = rpn[0].AsString().TrimStart('_');
            tk = await AppVars.GetVarToken(sVar, true);

            if (tk.TType.Equals("U"))
                throw new Exception($"12|{sVar}");

            // Get the parameters
            for (int i = 1; i < rpn.Count; i++)
            {
                string pString = rpn[i].AsString();
                if ("[](),".Contains(pString) == false) parms.Add(rpn[i]);
            }


            if (tk.TType.Equals("U", StringComparison.OrdinalIgnoreCase) || parms.Count > 2)
            {
                // It must be a User Defined Function
                int iii = 0;
            }
            else
            {
                if (tk.TType.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    if (parms.Count > 0)
                    {
                        if (parms[0].Element.Type.Equals("N", StringComparison.OrdinalIgnoreCase))
                            row = parms[0].AsInt();
                        else
                            throw new Exception("11|");
                    }

                    if (parms.Count > 1)
                    {
                        if (parms[1].Element.Type.Equals("N", StringComparison.OrdinalIgnoreCase))
                            col = parms[1].AsInt();
                        else
                            throw new Exception("11|");
                    }

                    // Make 1D if col < 1
                    if (col < 1 && row > 0)
                    {
                        col = row;
                        row = 0;
                    }
                    else if (parms.Count == 0)
                    {
                        col = 1;
                        row = 1;
                    }

                    if (row < 0 || col < 1) throw new Exception("1234|");               // 1 based array check
                    if (row == 0 && col > tk._avalue.Count) throw new Exception("1234|"); // 1D array check
                    if (row > 0 && col > tk.Col) throw new Exception("1234|");          // 2D array check
                    if (row > 1 && row > tk.Row) throw new Exception("1234|");          // 2D array check
                    tk.SetElement(row, col);
                }

                // Get the var element's value
                sResult.Element.Value = tk.Element.Value;
            }

            return sResult;
        }

        /***************************************************************
         * Return a token representing an object or object property
         * 
         * The list holds the object, object parts, and optional parameters as needed
         * 
         * First thing off the list should be the object reference (_var) and
         * then you'll see following either an object part (.objpart), a
         * value (N1, Chello, etc), or left/right bracket/paren
         * 
         * Nothing get's solved unless you run into an object part or a right
         * bracket/parent.  Then you check to see if what you have is a
         * property or method call and solve, continuing until done
         * 
         * Example:  _FRM  .OBJECTS [ 1 ] .NAME
         * 
         *      Get _FRM
         *      
         *      Get .OBJECTS, solve for FRM and 
         *          put into object var if object
         *          else put to results var
         *          
         *      Get [, 1, and ] putting 1 into p1 token and noting 1 parameter
         *      
         *      Get .NAME, are you in an object? If yes, continue else toss error
         *      
         *      Is OBJECTS a property or method of object var
         *          If property, solve for object.OBJECTS[p1]
         *          else call object.OBJECTS(p1)
         *              if result is object put to object var
         *              else put to results token
         *          BONUS: if method call and brackets were used, throw syntax error
         *          
         *      Nothing left? 
         *          If you are still in an object, then solve for object.Name
         *          else toss an error
         * 
         ***************************************************************/
        private async Task<JAXObjects.Token> GetVarObject(List<JAXObjects.Token> vInfo)
        {
            JAXObjects.Token sResult = new();
            JAXObjects.Token CurrentPart = new();
            JAXObjectWrapper? thisObject = null;

            List<JAXObjects.Token> parms = [];
            List<JAXObjects.Token> vInfo2 = [];

            int inLiteralSpace = 0;
            bool notField = true;

                // Last sVar element is the head variable - It has to be an object or table alias
                string sVar = vInfo[^1].AsString();
                //if (sVar[0] == '_') sVar = sVar[1..];

                // Is it an alias.field?
                if (vInfo.Count == 2 && Program.CurrentApp.CurrentDS.IsWorkArea(sVar))
                {
                    // Found the alias
                    int wa = Program.CurrentApp.CurrentDS.GetWorkArea(sVar);
                    string fName = vInfo[1].AsString().Trim('.').Trim();
                    if (Program.CurrentApp.CurrentDS.FieldExists(fName, wa))
                    {
                        // Found the field
                        sResult = await Program.CurrentApp.CurrentDS.GetFieldToken(wa, fName);
                        notField = false;
                    }
                }

                if (notField)
                {
                    // Not a table.field so look for an object
                    CurrentPart = await AppVars.GetVarFromExpression(sVar, null);


                    // Is it an object?
                    if (CurrentPart.Element.Type.Equals("O", StringComparison.OrdinalIgnoreCase) == false)
                        throw new Exception("1924|" + sVar);

                    // We have an object, assign it to thisObject
                    thisObject = (JAXObjectWrapper)CurrentPart.Element.Value;

                    for (int i = vInfo.Count - 2; i >= 0; i--)
                    {
                        string oPart = vInfo[i].AsString().TrimStart('.');

                        if ("[(".Contains(oPart[..1]))
                        {
                            if (inLiteralSpace != 0)
                                throw new Exception("3010|");

                            // We're inside brakets/parens
                            inLiteralSpace++;
                        }
                        else if ("])".Contains(oPart[..1]))
                        {
                            // Close out the literal space
                            inLiteralSpace--;

                            if (inLiteralSpace != 0)
                                throw new Exception("3011|");
                        }
                        else if (vInfo[i].TType.Equals("C", StringComparison.OrdinalIgnoreCase))
                        {
                            // We've got a comma, skip it - DO WE NEED THIS?
                        }
                        else if (vInfo[i].TType.Equals("V"))        // Object name
                        {
                            // Found a variable
                            if (inLiteralSpace > 0)
                            {
                                // Solve for this literal and put it 
                                // into the parameters
                                vInfo2 = [];
                                vInfo2.Add(vInfo[i]);
                                parms.Add(await GetVar(vInfo2));
                            }
                            else
                                throw new Exception("3012|" + vInfo[i].AsString());
                        }
                        else if (vInfo[i].TType.Equals("v"))        // Object property, method, or event
                        {
                            // Found an object part
                            if (inLiteralSpace > 0)
                                throw new Exception("3013|" + vInfo[i].AsString());

                            JAXObjects.Token tk = new();

                            // Is it a property or method call?
                            if ((await thisObject.IsMember(oPart)).Equals("M", StringComparison.OrdinalIgnoreCase))
                            {
                                // Method call
                                while (i > 0)
                                {
                                    // Adding parameters until we hit the next object part
                                    if (vInfo[i - 1].TType.Equals("V", StringComparison.OrdinalIgnoreCase))
                                        break;

                                    ParameterClass c = new();
                                    c.Type = "T";
                                    c.token.Element.Value = vInfo[i - 1];
                                    Program.CurrentApp.ParameterClassList.Add(c);
                                    i--;
                                }
                                await thisObject.MethodCall(oPart);
                                tk.Element.Value = Program.CurrentApp.ReturnValue.Element.Value;
                            }
                            else
                            {
                                // Is this an array or UDF?
                                if (i > 0 && "([".Contains(vInfo[i - 1].AsString()))
                                {
                                    // Add each parameter to the oPart comma-delimited var string
                                    string endPart = vInfo[i - 1].AsString() == "(" ? ")" : "]";
                                    oPart += vInfo[i - 1].AsString();
                                    i--;

                                    while (i > 0 && vInfo[--i].AsString().Equals(endPart) == false)
                                        oPart += vInfo[i].AsString() + ",";

                                    // Clear out the trailing comma and add the closing paren/bracket
                                    if (i >= 0)
                                        oPart = oPart.Trim(',') + vInfo[i].AsString();
                                    else
                                        throw new Exception("3014|");
                                }

                                // It's a property or object
                                tk = await AppVars.GetVarFromExpression(oPart, thisObject);
                            }

                            if (i > 0)
                            {
                                // Still have parts to process
                                if (tk.Element.Type.Equals("O", StringComparison.OrdinalIgnoreCase))
                                    thisObject = (JAXObjectWrapper)tk.Element.Value;
                                else
                                    throw new Exception("1766|" + vInfo[i].AsString());
                            }
                            else
                            {
                                // We are out of here!
                                //sResult.Element.Value = tk.Element.Value;
                                sResult = tk;
                            }

                            parms = [];
                        }
                        else
                        {
                            // Add values to the parameters
                            parms.Add(vInfo[i]);
                        }
                    }

                    // Can't leave parms dangling
                    if (parms.Count > 0) throw new Exception("10|");
                }

            return sResult;
        }
    }
}
