using JAXBase.Core;
using JAXBase.XBase;

namespace JAXBase.Executer
{
    public static class JAXBase_ECodes
    {
        public static async Task<ExecuterCodes> Split(string[] mProc)
        {
            ExecuterCodes eCodes = new();

            // --------------------------------------------------
            // Load the eCodes class with the various components
            // of the statement.  Usually under 4 but could be
            // several for a few commands.
            // --------------------------------------------------
            for (int i = 0; i < mProc.Length; i++)
            {
                // Skip blank entries
                if (string.IsNullOrWhiteSpace(mProc[i])) continue;

                char k = mProc[i][0];       // Get the runtime key
                string rpn = mProc[i][1..]; // Strip the key from the RPN expression
                string codeName = Program.CurrentApp.RunTimeCodes[Program.CurrentApp.XRef4Runtime[k]];    // Get the code name

                JAXObjects.Token rpnValue;
                string[] rpns;
                string[] rpnSplit;
                JAXObjects.Token answer;

                // Put the RPN expression into the code
                // Some can be solved here, others have to wait for later
                //AppIO.DebugLog($"Processing line {App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLine} in level {Program.CurrentApp.CurrentAppLevel} source {App.AppLevels[Program.CurrentApp.CurrentAppLevel].CurrentLineOfCode} -> code: {codeName} with RPN: {rpn}", App.CurrentDS.JaxSettings.Talk == false);

                switch (codeName)
                {
                    case "as":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);
                        for (int j = 0; j < rpnSplit.Length; j++)
                            eCodes.As.Add(rpnSplit[j]);
                        break;

                    case "at":
                        rpns = rpn.Split(AppClass.expParam);
                        if (rpns.Length != 2) throw new Exception($"10||Invalid AT expression has {rpns.Length} parameters");
                        
                        answer = await Program.CurrentApp.SolveFromRPNString(rpns[0]);
                        if (answer.Element.Type.Equals("N"))
                            eCodes.At.row = answer.AsInt();
                        else
                            throw new Exception("11|");

                        answer = await Program.CurrentApp.SolveFromRPNString(rpns[1]);
                        if (answer.Element.Type.Equals("N"))
                            eCodes.At.col = answer.AsInt();
                        else
                            throw new Exception("11|");

                        break;

                    case "command":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.COMMAND = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "collate":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.COLLATE = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "codepage":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("N"))
                            eCodes.CODEPAGE = rpnValue.AsInt();
                        else
                            throw new Exception("11|");
                        break;

                    case "database":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.DATABASE = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "expressions":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);
                        
                        for (int j = 0; j < rpnSplit.Length; j++)
                        {
                            if (string.IsNullOrWhiteSpace(rpnSplit[j]) == false)
                            {
                                // We only deal with non-blank expressions
                                ExCodeRPN e = new()
                                {
                                    // Is the RPN expression a Literal, eXpression, or plain Text?
                                    Type = rpnSplit[j][0] == AppClass.literalStart ? "L" : rpnSplit[j][0] == AppClass.expByte ? "X" : throw new Exception("11|"),
                                    RNPExpr = rpnSplit[j]
                                };

                                eCodes.Expressions.Add(e);
                            }
                        }
                        break;

                    case "flags":
                        eCodes.Flags = rpn.Split(AppClass.expParam);
                        
                        for (int j = 0; j < eCodes.Flags.Length; j++)
                            eCodes.Flags[j] = (await Program.CurrentApp.SolveFromRPNString(eCodes.Flags[j])).AsString();
                        break;

                    case "from":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);
                        
                        if (rpnSplit.Length != 2) throw new Exception("11|");
                        eCodes.From.Type = rpnSplit[0];
                        eCodes.From.Name = (await Program.CurrentApp.SolveFromRPNString(rpnSplit[1])).AsString();
                        break;

                    case "fname":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.FNAME = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "for":
                        eCodes.ForExpr = rpn;
                        break;

                    case "fields":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j += 2)
                        {
                            if (rpns.Length > j + 1)
                            {
                                ExCodeName fld = new()
                                {
                                    Type = rpns[j],
                                    Name = rpns[j + 1]
                                };

                                eCodes.Fields.Add(fld);
                            }
                        }

                        break;

                    case "fileexpr":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpnSplit.Length; j++)
                        {
                            if (string.IsNullOrWhiteSpace(rpnSplit[j]) == false)
                            {
                                // We only deal with non-blank expressions
                                ExCodeRPN e = new()
                                {
                                    // Is the RPN expression a Literal, eXpression, or plain Text?
                                    Type = rpnSplit[j][0] == AppClass.literalStart ? "L" : rpnSplit[j][0] == AppClass.expByte ? "X" : throw new Exception("11|"),
                                    RNPExpr = rpnSplit[j]
                                };

                                eCodes.FileExpr.Add(e);
                            }
                        }
                        break;

                    case "in":
                        eCodes.InExpr = rpn;
                        break;

                    case "index":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j++)
                        {
                            rpnSplit = rpns[j].Split(AppClass.expParam);

                            ExCodeName fld = new()
                            {
                                Type = rpnSplit[0],
                                Name = rpnSplit[1]
                            };

                            eCodes.Index.Add(fld);
                        }
                        break;

                    case "into":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.INTO = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "like":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j += 2)
                        {
                            // Is it a valid literal/expression or empty?
                            if (rpns[j].Length > 2)
                                eCodes.Like.Add((await Program.CurrentApp.SolveFromRPNString(rpns[j])).AsString());
                        }

                        break;

                    case "of":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.OF = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "on":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.ON = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "order":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.ORDER = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "record":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("N"))
                            eCodes.RECORD = rpnValue.AsInt();
                        else if (rpnValue.Element.Type.Equals("C"))
                        {
                            if (rpnValue.AsString().Equals("top", StringComparison.OrdinalIgnoreCase))
                                eCodes.RECORD = -1;
                            else if (rpnValue.AsString().Equals("bottom", StringComparison.OrdinalIgnoreCase))
                                eCodes.RECORD = -2;
                            else
                                throw new Exception("11|");
                        }
                        else
                            throw new Exception("11|");

                        break;

                    case "sesssion":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);
                        
                        if (rpnValue.Element.Type.Equals("N"))
                            eCodes.SESSION = rpnValue.AsInt();
                        else
                            throw new Exception("11|");
                        break;

                    case "scope":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);
                        
                        if (rpnSplit.Length != 2) throw new Exception("11|");
                        
                        eCodes.Scope.Type = rpnSplit[0];
                        answer = await Program.CurrentApp.SolveFromRPNString(rpnSplit[1]);

                        if (answer.Element.Type.Equals("N"))
                            eCodes.Scope.Count = answer.AsInt();
                        else
                            throw new Exception("11|");
                        break;

                    case "sheet":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);

                        if (rpnValue.Element.Type.Equals("C"))
                            eCodes.SHEET = rpnValue.AsString();
                        else
                            throw new Exception("11|");
                        break;

                    case "step":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);

                        if (rpnValue.Element.Type.Equals("N"))
                            eCodes.RECORD = rpnValue.AsInt();
                        else
                            throw new Exception("11|");
                        break;

                    case "subcmd":
                        if (string.IsNullOrWhiteSpace(rpn))
                            throw new Exception("11|");
                        else
                            eCodes.SUBCMD = rpn;
                        break;

                    case "table":
                        eCodes.TABLE = rpn;
                        break;

                    case "tag":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j++)
                        {
                            ExCodeName fld = new()
                            {
                                Type = rpns[j][0] == AppClass.literalStart ? "L" : "X",
                                Name = rpns[j]
                            };

                            eCodes.Tag.Add(fld);
                        }
                        break;

                    case "timeout":
                        rpnValue = await Program.CurrentApp.SolveFromRPNString(rpn);

                        if (rpnValue.Element.Type.Equals("N"))
                            eCodes.TIME = rpnValue.AsInt();
                         else
                            throw new Exception("11|");
                        break;

                    case "to":
                        // TO may be two parts or just one
                        // TO expr/lit
                        // TO expr/lit expr/lit
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j++)
                        {
                            ExCodeName fld = new()
                            {
                                Type = rpns.Length == 1 ? (rpns[0][0] == AppClass.literalStart ? "L" : "X") : rpns[0],
                                Name = rpns.Length == 1 ? rpns[0] : rpns[1]
                            };

                            eCodes.To.Add(fld);
                        }
                        break;

                    case "type":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j++)
                        {
                            ExCodeName fld = new()
                            {
                                Type = rpns[j][0] == AppClass.literalStart ? "L" : "X",
                                Name = rpns[j]
                            };

                            eCodes.Type.Add(fld);
                        }
                        break;

                    case "values":
                        rpnSplit = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpnSplit.Length; j++)
                        {
                            if (string.IsNullOrWhiteSpace(rpnSplit[j]) == false)
                            {
                                // We only deal with non-blank expressions
                                ExCodeRPN e = new()
                                {
                                    // Is the RPN expression a Literal, eXpression, or plain Text?
                                    Type = rpnSplit[j][0] == AppClass.literalStart ? "L" : rpnSplit[j][0] == AppClass.expByte ? "X" : throw new Exception("11|"),
                                    RNPExpr = rpnSplit[j]
                                };

                                eCodes.Values.Add(e);
                            }
                        }
                        break;

                    case "when":
                        eCodes.WhenExpr = rpn;
                        break;

                    case "while":
                        eCodes.WhileExpr = rpn;
                        break;

                    case "with":
                        rpns = rpn.Split(AppClass.expDelimiter);

                        for (int j = 0; j < rpns.Length; j += 2)
                        {
                            if (rpns.Length > j + 1)
                            {
                                ExCodeRPN exCodeRPN = new()
                                {
                                    Type = rpns[j],
                                    RNPExpr = rpns[j + 1]
                                };

                                eCodes.With.Add(exCodeRPN);
                            }
                        }
                        break;
                }
            }

            return eCodes;
        }
    }
}
