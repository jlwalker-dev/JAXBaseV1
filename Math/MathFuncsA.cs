/******************************************************************************************************************************************
 * "A" Functions
 * Method A0 is for functions that use or process array variables rather
 * than values from an array.  Those functions need the actual array name
 * passed in and will process all other expressions as needed.
 * 
 * Method A1 is for the traditional functions than return results based
 * on values passed to them.
 *
 * Reminder: xBase arrays are 1 based
 * 
 ******************************************************************************************************************************************/
using JAXBase.Core;
using JAXBase.Data;
using JAXBase.Utilities;
using JAXBase.XBase;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Management;
using System.Text;

namespace JAXBase.Math
{
    public class MathFuncsA
    {
        public static async Task<JAXObjects.Token> A0(string _rpn, List<string> pop)
        {
            JAXObjects.Token tAnswer = new();
            JAXObjects.Token answer;

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);
            string stype4 = (pop.Count > 3 ? pop[3][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 2 ? pop[2][1..] : string.Empty);
            string string4 = (pop.Count > 3 ? pop[3][1..] : string.Empty);

            int intval2 = 0;
            int intval3 = 0;

            if (pop.Count > 1 && int.TryParse(string2, out intval2) == false) intval2 = 0;
            if (pop.Count > 2 && int.TryParse(string3, out intval3) == false) intval3 = 0;

            switch (_rpn)
            {
                case "`ACLASS":                                                             // Get class info
                    if (stype1.Equals("_"))
                    {
                        string arrayName = string1;
                        List<string> ClassList = [];

                        if (stype2.Equals("_"))
                        {
                            answer = await AppVars.GetVarFromExpression(string2, null);
                            if (answer.TType.Equals("O"))
                            {
                                // We have an obejct to traverse!
                                JAXObjectWrapper obj = (JAXObjectWrapper)answer.Element.Value;
                                JAXObjects.Token tk = await obj.GetProperty("Class");
                                ClassList.Add(tk.AsString());

                                string tName;
                                tk = await obj.GetProperty("baseclass");
                                tName = tk.AsString();

                                while (string.IsNullOrWhiteSpace(tName) == false)
                                {
                                    JAXObjects.Token tk1 = await obj.GetProperty("parent");
                                    obj = (JAXObjectWrapper)tk1.Element.Value;

                                    ClassList.Add((await obj.GetProperty("class")).AsString());
                                    {
                                        ClassList.Add(tk1.AsString());
                                    }

                                    tName = (await obj.GetProperty("baseclass")).AsString();
                                }
                            }

                            // Create the array and fill it
                            AppVars.SetVarOrMakePrivate(arrayName, 1, ClassList.Count, true);
                            for (int i = 0; i < ClassList.Count; i++)
                                AppVars.SetVar(arrayName, ClassList[i], 1, i + 1);

                            tAnswer._avalue[0].Value = ClassList.Count;
                        }
                        else
                            throw new Exception("11|");
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ACOPY":                                                              // Copy from one array to another
                    if (stype1.Equals("_") & stype2.Equals("_"))
                        tAnswer = await ACopy(Program.CurrentApp, string1, string2, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ADATABASES":                                                         // Array of all open databases
                    if (stype1.Equals("_"))
                        tAnswer = ADatabases(Program.CurrentApp, string1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ADBOBJECTS":                                                         // Array of named connections, relations, tables, or SQL views from current database
                    if (stype1.Equals("_"))
                        tAnswer = await ADBObjects(Program.CurrentApp, string1, string2, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ADDPROPERTY":                                                        // Add a property at runtime - TODO - WTF? - Fever dream?  Forgot to finish?  Too much beer?
                    tAnswer.Element.Value = false;

                    if (stype1.Equals("_"))
                    {
                        // Make sure it's a class object
                        JAXObjects.Token tk = await AppVars.GetVarToken(string1);
                        if (tk.TType.Equals("O"))
                        {
                            // We have an object, so add the property and the value
                            // list[0] = property name
                            // list[1] = value

                            object? var = pop.Count > 2 ? AppHelper.Convert2STValue(pop[2]) : false;
                            tk.AddElement(string2, var, true, true);
                            tAnswer.Element.Value = true;
                            //AppVars.SetVar(string1, tk);
                        }
                        else
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);    // Not an object
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`ADEL":                                                               // Delete a row or column from an array - does not shrink array
                    if (stype1.Equals("_"))
                        tAnswer = await ADel(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 1));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ADIR":                                                               // Gets file/directory information to an array
                    if (stype1.Equals("_"))
                        tAnswer = await ADir(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 1));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AELEMENT":                                                           // Returns element number based on subscript
                    if (stype1.Equals("_"))
                        tAnswer = await AElement(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AERROR":                                                             // Array of last error
                    if (stype1.Equals("_"))
                        tAnswer = AError(Program.CurrentApp, string1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AEVENTS":                                                            // Number of exising binding events
                    if (stype1.Equals("_"))
                    {
                        AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AFIELDS":                                                            // Get fields in table
                    if (stype1.Equals("_"))
                        tAnswer = AFields(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AFONT":                                                              // Available fonts
                    int fCount = 0;
                    if (stype1.Equals("_"))
                    {
                        using (InstalledFontCollection col = new())
                        {
                            foreach (FontFamily fa in col.Families)
                            {
                                if (string.IsNullOrWhiteSpace(string2) || fa.Name.Contains(string2, StringComparison.OrdinalIgnoreCase))
                                {
                                    fCount++;
                                    AppVars.SetVarOrMakePrivate(string1, 1, fCount, true);
                                    AppVars.SetVar(string1, fa.Name, 1, fCount);
                                    // Only list the first 100
                                    if (fCount > 99)
                                        break;
                                }
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    tAnswer.Element.Value = fCount;
                    break;

                case "`AGETCLASS":                                                          // Select ClassLibrary & Class dialog
                    // --------------------------------------------------------------------------------------------------------
                    if (stype1.Equals("_"))
                    {
                        throw new Exception("1999||AGETCLASS");
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AINS":                                                               // insert into an array - does not grow array
                    if (stype1.Equals("_"))
                        tAnswer = await AIns(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 1));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AINSTANCE":                                                          // Class instances
                    List<string> matchX = [];

                    if (stype1.Equals("_"))
                    {
                        if (stype2.Equals("C"))
                        {
                            matchX = await GetClassVarList(Program.CurrentApp, string2);

                            // Sort the matched vars
                            matchX.Sort();

                            // Move them to the array
                            AppVars.SetVarOrMakePrivate(string1, 1, matchX.Count, true);
                            for (int j = 0; j < matchX.Count; j++)
                                AppVars.SetVar(string1, matchX[j], 1, j);

                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    tAnswer.Element.Value = matchX.Count;
                    break;

                case "`ALEN":                                                               // Array length, rows, cols
                    if (stype1.Equals("_"))
                        tAnswer = await ALen(Program.CurrentApp, string1, intval2);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ALINE":                                                             // Copy lines from a character expression or memo field to an array
                    if (stype1.Equals("_"))
                        tAnswer = ALines(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 1));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AMEMBERS":                                                           // Return an array of all properties of an object (by default)
                    if (stype1.Equals("_") && stype2.Equals("_"))
                    {
                        JAXObjects.Token sourcetk = await AppVars.GetVarToken(string2);
                        tAnswer._avalue[0].Value = 0;

                        if (sourcetk.Element.Type.Equals("O") && (string.IsNullOrWhiteSpace(stype3) || stype3.Equals("N")))
                        {
                            JAXObjectWrapper jow = (JAXObjectWrapper)sourcetk.Element.Value;
                            JAXObjects.Token desttk = new();

                            List<string> props = jow.GetPropertyList();
                            List<string> meths = jow.GetMethodList();
                            //List<string> objs = jow.GetObjectList();

                            if (stype4.Equals("_"))
                            {
                                // Get the value of stype4
                                List<string> vList = [];
                                vList.Add(string4);
                                if (pop.Count < 5)
                                {
                                    JAXObjects.Token s4 = await AppVars.GetVarToken(string4);
                                    if (s4.Element.Type.Equals("C"))
                                        string4 = s4.AsString();
                                    else
                                        throw new Exception("11|");
                                }
                                else
                                {
                                    int p3 = 3;
                                    while (p3 < pop.Count - 1)
                                        vList.Add(pop[p3++]);

                                    // TODO - Now what?
                                }
                            }

                            string4 = string4.ToUpper();
                            bool lProtected = string4.Contains('P');
                            bool lHidden = string4.Contains('H');
                            bool lPublic = string4.Contains('G');

                            bool lNative = string4.Contains('N');
                            bool lUser = string4.Contains('U');

                            bool lInherited = string4.Contains('I');
                            bool lBase = string4.Contains('B');

                            int nChanged = string4.IndexOf('C');

                            // Make sure public is set if protected
                            // and hidden are not.
                            lPublic = lPublic || (!lProtected && !lHidden);

                            if (!lNative && !lUser)
                            {
                                // Mark them both as true
                                lNative = true;
                                lUser = true;
                            }

                            if (!lBase && !lInherited)
                            {
                                // Mark them both as true
                                lBase = true;
                                lInherited = true;
                            }


                            int PropsSaved = 0;
                            int columns = 1;

                            switch (intval3)
                            {
                                case 0:
                                    if (props.Count > 0)
                                    {
                                        desttk.SetDimension(0, props.Count, true);

                                        for (int i = 0; i < props.Count; i++)
                                        {
                                            JAXObjects.Token proptk = await jow.GetProperty(props[i], 0);

                                            // Do we include this property?
                                            if (lPublic || (lProtected && proptk.Protected) || (proptk.Hidden && lHidden))
                                            {
                                                if ((lNative && proptk.PropType.Equals("N")) || (lUser && proptk.PropType.Equals("U")))
                                                {
                                                    if ((lBase && proptk.PropType.Equals("N")) || (lInherited && proptk.PropType.Equals("I")))
                                                    {
                                                        if (nChanged == 0 || (nChanged > 1 && proptk.Changed))
                                                        {
                                                            desttk._avalue[PropsSaved].Value = props[i];
                                                            PropsSaved++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;

                                case 1:
                                    if (props.Count + meths.Count > 0)
                                    {
                                        columns = 2;
                                        desttk.SetDimension(props.Count + meths.Count, 2, true);

                                        for (int i = 0; i < props.Count; i++)
                                        {
                                            JAXObjects.Token proptk = await jow.GetProperty(props[i], 0);

                                            // Do we include this property?
                                            if ((lPublic && proptk.Hidden == false) || (lProtected && proptk.Protected) || (proptk.Hidden && lHidden))
                                            {
                                                if ((lNative && "NI".Contains(proptk.Tag)) || (lUser && "UH".Contains(proptk.PropType)))
                                                {
                                                    if ((lBase && "NI".Contains(proptk.Tag)) || (lInherited && "HI".Contains(proptk.Tag)))
                                                    {
                                                        if (nChanged < 0 || proptk.Changed)
                                                        {
                                                            desttk.SetElement(PropsSaved + 1, 1);
                                                            desttk.Element.Value = props[i];
                                                            desttk.SetElement(PropsSaved + 1, 2);
                                                            desttk.Element.Value = "P";
                                                            PropsSaved++;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        for (int i = 0; i < meths.Count; i++)
                                        {
                                            JAXObjects.Token proptk = await jow.GetProperty(meths[i], 0);
                                            JAXObjectsAux.MethodClass minfo = jow.MethodInfo(meths[i]);

                                            // Do we include this property?
                                            if ((lPublic && minfo.Hidden == false) || (lProtected && minfo.Protected) || (minfo.Hidden && lHidden))
                                            {
                                                if ((lNative && minfo.Tag.Equals("N")) || (lUser && minfo.Tag.Contains("UI")))
                                                {
                                                    if ((lBase && "NU".Contains(minfo.Tag)) || (lInherited && "HI".Contains(minfo.Tag)))
                                                    {
                                                        if (nChanged < 0 || proptk.Changed)
                                                        {
                                                            desttk.SetElement(PropsSaved + 1, 1);
                                                            desttk.Element.Value = meths[i];
                                                            desttk.SetElement(PropsSaved + 1, 2);
                                                            desttk.Element.Value = minfo.Type;  // M or E
                                                            PropsSaved++;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        // TODO Get the children
                                    }
                                    break;

                                case 2:

                                    // TODO Get the children with class and baseclass columns
                                    break;
                            }

                            while (desttk._avalue.Count > PropsSaved * columns)
                                desttk._avalue.RemoveAt(desttk._avalue.Count - 1);

                            tAnswer._avalue[0].Value = desttk.Row < 2 ? PropsSaved : desttk.Row;

                            // If there's something there, create/fix the array
                            // and copy the data over to it
                            if (props.Count + meths.Count > 0)
                            {
                                AppVars.SetVarOrMakePrivate(string1, desttk.Row, desttk.Col, true);
                                JAXObjects.Token finaltk = await AppVars.GetVarToken(string1);

                                for (int i = 0; i < (desttk.Row < 1 ? 1 : desttk.Row) * desttk.Col; i++)
                                    finaltk._avalue[i].Value = desttk._avalue[i].Value;
                            }
                        }
                        else
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;


                case "`ANETRESOURCES":
                    List<string> shares = [];

                    if (stype1.Equals("_"))
                    {
                        // Get network shares?
                        if ((intval2 & 1) > 0)
                        {
                            try
                            {
                                // Connect to the WMI namespace on the specified computer
                                ManagementScope scope = new($@"\\{string2}\root\cimv2");
                                scope.Connect();

                                // Query for Win32_Share instances
                                ObjectQuery query = new("SELECT Name FROM Win32_Share");
                                ManagementObjectSearcher searcher = new(scope, query);

                                // Iterate through the results and add share names to the list
                                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                                {
                                    string? name = obj["Name"].ToString();

                                    if (name is not null)
                                        shares.Add(name);
                                }
                            }
                            catch (ManagementException ex)
                            {
                                Console.WriteLine($"Error retrieving shares: {ex.Message}");
                            }
                        }

                        // Get printer shares?
                        if ((intval2 & 2) > 0)
                        {
                            foreach (string printer in PrinterSettings.InstalledPrinters)
                                shares.Add(printer);
                        }

                        // Set the array only if there is something to return
                        if (shares.Count > 0)
                        {
                            AppVars.SetVarOrMakePrivate(string1, 1, shares.Count, true);
                            for (int j = 0; j < shares.Count; j++)
                                AppVars.SetVar(string1, shares[j], 1, j);
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    // Send back the count
                    tAnswer.Element.Value = shares.Count;
                    break;

                case "`APROCINFO":
                    int idx = 0;
                    int err = 0;

                    if (stype1.Equals("_"))
                    {
                        if (File.Exists(string2))
                        {
                            ExtensionTypes? xTypes = null;
                            string jfType = string.Empty;

                            string fStem = JAXLib.JustStem(string2);
                            string ext = JAXLib.JustExt(string2);
                            foreach (char c in "CDFLMOPQRV")
                            {
                                ExtensionTypes xType = AppHelper.GetCodeFileExtensions("C");
                                if (xType.IsJAXCodeExtension(ext))
                                {
                                    jfType = c.ToString();
                                    xTypes = xType;
                                    break;
                                }
                            }

                            if (xTypes is null)
                                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                            else
                            {
                                string cCode;

                                if (ext.Equals(xTypes.CompiledCode, StringComparison.OrdinalIgnoreCase) == false)
                                {
                                    // Worry about uncompile code later
                                    string sCode = JAXLib.FileToStr(string2);
                                    string MD5 = Program.CurrentApp.utl.GetFileCheckSum_MD5(sCode);

                                    cCode = Program.CurrentApp.JaxCompiler.CompileBlock(sCode, true, out int errCount);
                                    if (errCount > 0)
                                    {
                                        err = 9992;
                                        AppErrorHandling.SetError(err, string2, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                    }

                                    string header = AppHelper.CreateHeader(string2, MD5, jfType, fStem);
                                    string pmap = AppHelper.CreateProcedureMap(cCode, fStem);
                                    cCode = header + pmap + cCode;
                                }
                                else
                                    cCode = JAXLib.FileToStr(string2);

                                if (err == 0)
                                {
                                    // Break out the header
                                    // Get the header
                                    int f = cCode.IndexOf(AppClass.headerEndByte) + 1;

                                    if (f < 1)
                                    {
                                        err = 9992;
                                        AppErrorHandling.SetError(err, string2, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                                    }

                                    string headerstring = cCode[..f];
                                    cCode = cCode[f..];

                                    FileHeader fileHeader = AppHelper.BreakHeader(string2, headerstring);
                                    CCodeCache cc = AppHelper.BreakHeaderMap(jfType, fileHeader, cCode);

                                    // Fill the array
                                    AppVars.SetVarOrMakePrivate(string1, cc.Procedures.Count + cc.Classes.Count, 4, true);
                                    foreach (KeyValuePair<string, int> proc in cc.Procedures)
                                    {
                                        idx++;
                                        AppVars.SetVar(string1, "CODE", idx, 1);

                                        if (cc.StartProc.Equals(proc.Key, StringComparison.OrdinalIgnoreCase))
                                            AppVars.SetVar(string1, "PROGRAM", idx, 2);   // MAIN or PROCEDURE
                                        else
                                            AppVars.SetVar(string1, "PROCEDURE", idx, 2);   // MAIN or PROCEDURE

                                        AppVars.SetVar(string1, proc.Key, idx, 3);
                                        AppVars.SetVar(string1, proc.Value, idx, 4);
                                    }

                                    // TODO - Class definitions
                                    foreach (KeyValuePair<string, int> proc in cc.Classes)
                                    {
                                        string[] kval = proc.Key.Split(";");        // Name;BaseClass

                                        if (kval.Length > 1)
                                        {
                                            idx++;
                                            AppVars.SetVar(string1, "CLASS", idx, 1);
                                            AppVars.SetVar(string1, kval[1], idx, 2);   // BaseClass or PROCEDURE
                                            AppVars.SetVar(string1, kval[0], idx, 3);   // Class/Method/Event Name
                                            AppVars.SetVar(string1, proc.Value, idx, 4);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    tAnswer.Element.Value = idx;
                    break;

                case "`ASCAN":                                                              // Scan an array
                    if (stype1.Equals("_"))
                        tAnswer = await AScan(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ASELOBJ":                                    // Returns selected object reference(s) in an array
                    // --------------------------------------------------------------------------------------------------------
                    if (stype1.Equals("_"))
                    {
                        throw new Exception("1999||ASELOBJ");
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ASESSIONS":
                    if (stype1.Equals("_"))
                        tAnswer = ASessions(Program.CurrentApp, string1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ASORT":                                                              // Sort an array
                    if (stype1.Equals("_"))
                        tAnswer.Element.Value = ASort(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;


                case "`ASTACKINFO":
                    if (stype1.Equals("_"))
                        tAnswer = AStackinfo(Program.CurrentApp, string1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ASUBSCRIPT":                                                         // Return row or col of element number
                    if ((stype1).Equals("_"))
                        tAnswer = await ASubscript(Program.CurrentApp, string1, await JAXMathAux.ProcessPops(Program.CurrentApp, pop, 2));
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ATAGINFO":   // Get array of index information for current table
                    if (stype1.Equals("_"))
                    {
                        // Remember current work area and data session
                        int cwa = Program.CurrentApp.CurrentDS.CurrentWorkArea();
                        int cds = Program.CurrentApp.CurrentDataSession;


                        // Set the data session
                        if (stype3.Equals("N"))
                        {
                            if (intval3 > 0)
                                Program.CurrentApp.SetDataSession(intval3);
                        }
                        else if (string.IsNullOrWhiteSpace(stype3) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                        // Set the work area
                        if (stype2.Equals("N"))
                        {
                            if (intval2 > 0)
                                Program.CurrentApp.CurrentDS.SelectWorkArea(intval2);
                        }
                        else if (stype2.Equals("C"))
                            Program.CurrentApp.CurrentDS.SelectWorkArea(string2);
                        else if (string.IsNullOrWhiteSpace(stype2) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                        JAXDirectDBF.DBFInfo dbf = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo;

                        // Are there indexes to report on?
                        if (dbf.IDX.Count > 0)
                        {
                            // Create the array
                            AppVars.SetVarOrMakePrivate(string1, dbf.IDX.Count, 6, true);

                            // Fill it in with index information
                            for (int i = 0; i < dbf.IDX.Count; i++)
                            {
                                string type;

                                if (dbf.IDX[0].IsUnique)
                                    type = "UNIQUE";
                                else if (dbf.IDX[0].IsCandidate)
                                    type = "CANDIDATE";             // Future option
                                else
                                    type = "REGULAR";

                                AppVars.SetVar(string1, dbf.IDX[i].Name, i, 1);                                     // Index name
                                AppVars.SetVar(string1, type, i, 2);                                                // Type
                                AppVars.SetVar(string1, dbf.IDX[i].KeyClause, i, 3);                                // Index key expression
                                AppVars.SetVar(string1, dbf.IDX[i].ForClause, i, 4);                                // Filter (for) expression
                                AppVars.SetVar(string1, dbf.IDX[i].Descending ? "DESCENDING" : "ASCENDING", i, 5);  // .T. for descending
                                AppVars.SetVar(string1, "MACHINE", i, 6);                                           // Collation sequence
                                AppVars.SetVar(string1, dbf.IDX[i].FileName, i, 7);                                 // Filename
                                AppVars.SetVar(string1, dbf.IDX[i].IsRegistered ? "REGISTERED" : string.Empty, i, 8); // Is it a registered index?
                            }
                        }

                        Program.CurrentApp.SetDataSession(cds);                // Restore the data session
                        Program.CurrentApp.CurrentDS.SelectWorkArea(cwa);      // Restore the work area
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AUSED":
                    /*
                     * AUSED does not create in Opened order - need to add a SYSID to the DBFInfo class
                     * so that can be acomplished.
                     */
                    if (stype1.Equals("_"))
                    {

                        int cds = Program.CurrentApp.CurrentDataSession;   // Remember the current data session

                        // Select the data session in question if > 0
                        if (intval2 > 0)
                            Program.CurrentApp.SetDataSession(intval2);

                        Dictionary<int, JAXDirectDBF> temp = [];

                        // Create a temp copy of the work areas list for this data session
                        foreach (KeyValuePair<int, JAXDirectDBF> wa in Program.CurrentApp.CurrentDS.WorkAreas)
                            temp.Add(wa.Key, wa.Value);

                        // Grab the current low SysID from the WA in the temp list, record
                        // it into the array var, and remove that WA from the temp list
                        idx = 0;
                        while (temp.Count > 0)
                        {
                            // Reset the lowID to the current high value of the system counter
                            string lowID = Program.CurrentApp.SystemCounter();

                            // Work areas start at 1
                            int lowWA = 0;

                            // We just want the stem of the table name passed in
                            string3 = JAXLib.JustStem(string3);

                            // Go through what's left of the list
                            foreach (KeyValuePair<int, JAXDirectDBF> wa in temp)
                            {
                                // If there is a table name then it must match
                                if (string.IsNullOrWhiteSpace(string3) || wa.Value.DbfInfo.TableName.Equals(string3, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Does the current work area table have a lower
                                    // SysID?  If so, it was opened earlier than
                                    // the current loID
                                    if (wa.Value.DbfInfo.SysID.CompareTo(lowID) <= 0)
                                    {
                                        lowID = wa.Value.DbfInfo.SysID; // Get this SysID
                                        lowWA = wa.Key;                 // Get this work area
                                    }
                                }
                            }

                            // Add the latest "low value" work area to the list
                            // and then remove it from the temp list
                            if (lowWA > 0)
                            {
                                idx++;
                                AppVars.SetVarOrMakePrivate(string1, idx, 2, true);
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.Alias, idx, 1);
                                AppVars.SetVar(string1, lowWA, idx, 2);

                                temp.Remove(lowWA);
                            }
                            else
                                break;  // No more matches, so we're done
                        }

                        // And finally, go back to the current data session
                        Program.CurrentApp.SetDataSession(cds);

                        tAnswer.Element.Value = idx;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AVCXCLASSES@??":                             // Return array of classes in library
                    // --------------------------------------------------------------------------------------------------------
                    AppErrorHandling.SetError(1999, _rpn[..1], System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`AWORKAREAS":     // List of active workareas AWORKAREAS(datasession)
                    /*
                     * AUSED does not create in Opened order - need to add a SYSID to the DBFInfo class
                     * so that can be acomplished.
                     */
                    if (stype1.Equals("_"))
                    {
                        int cds = Program.CurrentApp.CurrentDataSession;   // Remember the current data session

                        // Select the data session in question if > 0
                        if (intval2 > 0)
                            Program.CurrentApp.SetDataSession(intval2);

                        Dictionary<int, JAXDirectDBF> temp = [];

                        // Create a temp copy of the work areas list for this data session
                        foreach (KeyValuePair<int, JAXDirectDBF> wa in Program.CurrentApp.CurrentDS.WorkAreas)
                            temp.Add(wa.Key, wa.Value);

                        // Grab the current low SysID from the WA in the temp list, record
                        // it into the array var, and remove that WA from the temp list
                        idx = 0;
                        while (temp.Count > 0)
                        {
                            // Reset the lowID to the current high value of the system counter
                            string lowID = Program.CurrentApp.SystemCounter();

                            // Work areas start at 1
                            int lowWA = 0;

                            // We just want the stem of the table name passed in
                            string3 = JAXLib.JustStem(string3);

                            // Go through what's left of the list
                            foreach (KeyValuePair<int, JAXDirectDBF> wa in temp)
                            {
                                // If there is a table name then it must match
                                if (string.IsNullOrWhiteSpace(string3) || wa.Value.DbfInfo.TableName.Equals(string3, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Does the current work area table have a lower
                                    // SysID?  If so, it was opened earlier than
                                    // the current loID
                                    if (wa.Value.DbfInfo.SysID.CompareTo(lowID) <= 0)
                                    {
                                        lowID = wa.Value.DbfInfo.SysID; // Get this SysID
                                        lowWA = wa.Key;                 // Get this work area
                                    }
                                }
                            }

                            // Add the latest "low value" work area to the list
                            // and then remove it from the temp list
                            if (lowWA > 0)
                            {
                                idx++;
                                AppVars.SetVarOrMakePrivate(string1, idx, 6, true);
                                AppVars.SetVar(string1, lowWA, idx, 1);                         // Work area number
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.Alias, idx, 2);     // Alias
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.TableType, idx, 3); // Table type
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.TableName, idx, 4); // Table Name
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.TableRef, idx, 5);  // Table FQFN or database!table for sql result set
                                AppVars.SetVar(string1, temp[lowWA].DbfInfo.RecCount, idx, 6);  // Record Count

                                temp.Remove(lowWA);
                            }
                            else
                                break;  // No more matches, so we're done
                        }

                        // And finally, go back to the current data session
                        Program.CurrentApp.SetDataSession(cds);

                        tAnswer.Element.Value = idx;
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                default:
                    throw new Exception("1999|" + _rpn[..1] + "|A0");
            }

            return tAnswer;
        }

        public static JAXObjects.Token A1(string _rpn, List<string> pop)
        {
            JAXObjects.Token tAnswer = new();

            // token types (_ is var, N=Numeric, C=character, etc)
            string stype1 = (pop.Count > 0 ? pop[0][..1] : string.Empty);
            string stype2 = (pop.Count > 1 ? pop[1][..1] : string.Empty);
            string stype3 = (pop.Count > 2 ? pop[2][..1] : string.Empty);
            string stype4 = (pop.Count > 3 ? pop[3][..1] : string.Empty);

            // get the variable names or values
            string string1 = (pop.Count > 0 ? pop[0][1..] : string.Empty);
            string string2 = (pop.Count > 1 ? pop[1][1..] : string.Empty);
            string string3 = (pop.Count > 1 ? pop[2][1..] : string.Empty);
            string string4 = (pop.Count > 1 ? pop[3][1..] : string.Empty);

            // translate into numbers for certain commands
            if (double.TryParse(string1.Trim(), out double val1) == false) val1 = 0D;
            if (double.TryParse(string2.Trim(), out double val2) == false) val2 = 0D;
            if (double.TryParse(string3.Trim(), out double val3) == false) val3 = 0D;
            if (double.TryParse(string4.Trim(), out double val4) == false) val4 = 0D;

            int intval2 = (int)val2;
            int intval3 = (int)val3;
            int intval4 = (int)val4;

            switch (_rpn)
            {
                case "!":                                                                   // Not expression
                                                                                            // TODO - this is full logical now!
                    if (stype1.Equals("L"))
                        tAnswer._avalue[0].Value = string1.Equals(".F.") ? ".T." : ".F.";
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ABS":                                                                // Get the absolute value of the number
                    if (stype1.Equals("N"))
                        tAnswer._avalue[0].Value = System.Math.Abs(val1);
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    break;

                case "`ACOS":                                                               // Arc Cosine
                    tAnswer = ACos(Program.CurrentApp, stype1, intval2, val1);
                    break;

                case "`ADDBS":                                                          // Add backslash to path
                    tAnswer._avalue[0].Value = JAXLib.Addbs(string1);
                    break;

                case "`ALIAS":                                                              // Current alias name
                    if (Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo is not null)
                        tAnswer._avalue[0].Value = Program.CurrentApp.CurrentDS.CurrentWA.DbfInfo.Alias;
                    else
                        tAnswer._avalue[0].Value = string.Empty;
                    break;

                case "`ALLTRIM":                                                        // Trim all spaces front and back
                    if (stype1.Equals("C"))
                    {
                        pop.RemoveAt(0);    // Remove string1
                        tAnswer = Alltrim(string1, pop);
                    }
                    else
                        throw new Exception("11|");
                    break;

                case "`ASC":                                                                // Return the int val of the ascii character
                    tAnswer = Asc(string1);
                    break;

                case "`ASIN":                               // Arc sine
                    tAnswer = ASin(Program.CurrentApp, string1, stype1);
                    break;

                case "`AT":  // Return postion of string in string
                    tAnswer._avalue[0].Value = string2.IndexOf(string1) + 1;
                    break;

                case "`AT_C":                               // Double byte search case sensitive
                    break;

                case "`ATAN":                               // A-Tangent
                    if (stype1.Equals("N") && stype2.Equals("N"))
                        tAnswer._avalue[0].Value = System.Math.Atan(val1);
                    else
                        throw new Exception("11}");
                    break;

                case "`ATC@":                                // Search a a double byte string
                    break;

                case "`ATCC":                               // Double byte search case insensitive
                    break;

                case "`ATCLINE":
                    break;

                case "`ATLINE":
                    tAnswer.Element.Value = 0;
                    int occurence = 0;
                    StringComparison comp = StringComparison.Ordinal;

                    if (stype3.Equals("N"))
                        occurence = intval3;
                    else
                    {
                        if (string.IsNullOrWhiteSpace(stype3) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (stype4.Equals("N"))
                    {
                        comp = intval4 switch
                        {
                            0 => StringComparison.Ordinal,
                            _ => StringComparison.OrdinalIgnoreCase
                        };
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(stype3) == false)
                            AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                    }

                    if (stype1.Equals("C") && stype2.Equals("C"))
                    {
                        List<string> list = [];
                        list.Add(string1);
                        list = MathFuncsA.GetALinesList(Program.CurrentApp, []);
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i].Contains(string2, comp))
                            {
                                if (occurence > 0)
                                    occurence--;
                                else
                                {
                                    tAnswer.Element.Value = i;
                                    break;
                                }
                            }
                        }
                    }
                    else
                        AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                    break;

                case "`ATN2":
                    if (stype1.Equals("N") && stype2.Equals("N"))
                        tAnswer._avalue[0].Value = System.Math.Atan2(val1, val2);
                    else
                        throw new Exception("11}");
                    break;

                default:
                    throw new Exception("1999|" + _rpn[..1] + "|A1");
            }

            return tAnswer;
        }

        // -------------------------------------------------------------------------------------------------------------------------
        // Copy one array to another - creating or expanding the destination if needed
        public static async Task<JAXObjects.Token> ACopy(AppClass App, string string1, string string2, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            int j = 0;

            JAXObjects.Token SourceTK = await AppVars.GetVarToken(string1);
            JAXObjects.Token DestTK = await AppVars.GetVarToken(string2);

            if (SourceTK.TType.Equals("A"))
            {
                if (p.Count < 1 || int.TryParse(p[0][1..], out int nFirstSourceElement) == false) nFirstSourceElement = 1;
                if (p.Count < 2 || int.TryParse(p[1][1..], out int nElementCount) == false) nElementCount = SourceTK._avalue.Count;
                if (p.Count < 3 || int.TryParse(p[2][1..], out int nFirstDestElement) == false) nFirstDestElement = 1;

                if (nFirstSourceElement < 1 || nFirstDestElement < 1)
                    throw new Exception("31||");

                // Source exists
                int nSourceElementCount = SourceTK.Row * SourceTK.Col;
                if (nElementCount < 1 || nElementCount > nSourceElementCount) nElementCount = nSourceElementCount;

                // Do we need to create or enlarge the Destination?
                if (DestTK.TType.Equals("A") == false || (DestTK.Row < 1 ? 1 : DestTK.Row) * DestTK.Col < nSourceElementCount)
                {
                    AppVars.SetVarOrMakePrivate(string2, SourceTK.Row, SourceTK.Col, true);
                    DestTK = await AppVars.GetVarToken(string2);
                }

                // Make sure the offset doesn't go past the current element count
                if (nFirstSourceElement + nElementCount - 1 > (DestTK.Row < 1 ? 1 : DestTK.Row) * DestTK.Col)
                {
                    int icount = (nFirstSourceElement + nElementCount) / DestTK.Col;
                    if (icount * DestTK.Col < nFirstSourceElement + nElementCount) icount++;
                    AppVars.SetVarOrMakePrivate(string2, icount, SourceTK.Col, true);
                }

                // Load the pointer for the variable in question and then
                // step through the source array, replacing elements of
                // the destination array by value
                JAXObjects.Token destArray = await AppVars.GetVarToken(string2);

                // Adjust the starting points for a zero based array
                nFirstDestElement--;
                nFirstSourceElement--;

                // Copy things over
                for (int i = 0; i < nElementCount; i++)
                    destArray._avalue[nFirstDestElement++].Value = SourceTK._avalue[nFirstSourceElement++].Value;

                if (nElementCount > 0)
                    j = nElementCount;
            }
            else
                throw new Exception($"232|{string1.ToUpper().Trim()}");

            tAnswer._avalue[0].Value = j;
            return tAnswer;
        }


        public static JAXObjects.Token ACos(AppClass App, string stype1, int intval2, double val1)
        {
            JAXObjects.Token tAnswer = new();

            if (stype1.Equals("N"))
                tAnswer._avalue[0].Value = System.Math.Acos(val1);
            else
                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            return tAnswer;
        }
        public static JAXObjects.Token ADatabases(AppClass App, string string1)
        {
            JAXObjects.Token tAnswer = new();
            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            // TODO - find out if this looks at current or all datasessions
            int j1 = 0;
            int j2 = 0;

            for (int i = 0; i < thisDS.Databases.Count; i++)
                if (thisDS.Databases.ElementAt(i).Value.DbfInfo.IsDBC) j1++;

            if (j1 > 0)
            {

                // Set the dimensions
                AppVars.SetVarOrMakePrivate(string1, j1, 2, true);

                for (int i = 0; i < thisDS.Databases.Count; i++)
                {
                    if (thisDS.Databases.ElementAt(i).Value.DbfInfo.IsDBC)
                    {
                        AppVars.SetVar(string1, thisDS.Databases.ElementAt(i).Value.DbfInfo.Alias, i, 1);
                        AppVars.SetVar(string1, JAXLib.JustFullPath(thisDS.Databases.ElementAt(i).Value.DbfInfo.FQFN), i, 2);
                    }
                }
            }

            tAnswer._avalue[0].Value = j2;
            return tAnswer;
        }

        public static async Task<JAXObjects.Token> ADBObjects(AppClass App, string string1, string string2, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            List<string> results = [];
            if (p.Count > 0) string2 = p[0].ToUpper();

            switch (string2)
            {
                case "CONNECTION":
                    break;

                case "RELATION":
                    break;

                case "VIEW":    // Views in database
                    // TODO results = App.DataSessions.DataBases[App.DataSessions.CurrentDatabase].SqlCon.Util_ListTables(true);
                    break;

                default:        // Tables in database
                    // TODO results = App.DataSessions.DataBases[App.DataSessions.CurrentDatabase].SqlCon.Util_ListTables(false);
                    break;
            }

            if (results.Count > 0)
            {
                JAXObjects.Token tkarray = await AppVars.GetVarToken(string1);
                if (tkarray.Col > 1 || tkarray.Row < results.Count)
                    AppVars.SetVarOrMakePrivate(string1, results.Count, 1, true);

                for (int i = 0; i < results.Count; i++)
                    AppVars.SetVar(string1, results[i], i, 1);
            }

            tAnswer._avalue[0].Value = results.Count.ToString();
            return tAnswer;
        }

        // Delete one or more elements from an array object
        public static async Task<JAXObjects.Token> ADel(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            bool llSuccess = true;

            try
            {
                if (p.Count < 1 || (int.TryParse(p[0][1..], out int intval2) == false)) intval2 = 0;    // which row/col
                if (p.Count < 2 || (int.TryParse(p[1][1..], out int intval3) == false)) intval3 = 0;    // 2=col

                // Get the token from the memory location
                JAXObjects.Token tkArray = await AppVars.GetVarToken(string1);

                if (tkArray.TType.Equals("A"))
                {
                    if (intval3 == 2)
                    {
                        // delete a column
                        if (intval3 < 1 || intval3 > tkArray.Col)
                            throw new Exception("Invalid array reference");

                        // Set values in column to false
                        for (int r = intval2; r < tkArray.Row - 1; r++)
                        {
                            int destElement = (r - 1) * tkArray.Col + intval2 - 1;
                            tkArray._avalue[destElement].Value = false;
                        }

                        tkArray.SetElement(tkArray.Row, intval2 - 1);
                        tkArray.Element.Value = false;
                    }
                    else
                    {
                        if (tkArray.Col > 1)
                        {
                            // Delete a row in a 2D column moving rows up
                            if (intval2 < 1 || intval2 > tkArray.Row)
                                throw new Exception("Invalid array reference");

                            // Move rows towards top and set last one to .F.
                            for (int r = intval2; r < tkArray.Row - 1; r++)
                            {
                                for (int c = 0; c < tkArray.Col; c++)
                                {
                                    int destElement = (r - 1) * tkArray.Col + c;
                                    int sourceElement = r * tkArray.Col + c;
                                    tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                                }
                            }

                            // Set the last row to false
                            for (int c = 0; c < tkArray.Col; c++)
                            {
                                tkArray.SetElement(tkArray.Row, c + 1);
                                tkArray.Element.Value = false;
                            }
                        }
                        else
                        {
                            // Move elements towards top and set last one to false
                            for (int i = intval2 - 1; i < tkArray.Row - 1; i++)
                                tkArray._avalue[i].Value = tkArray._avalue[i + 1].Value;

                            tkArray._avalue[tkArray.Row - 1].Value = false;
                        }
                    }
                }
                else
                {
                    llSuccess = false;
                }
            }
            catch (Exception e)
            {
                AppErrorHandling.SetError(9999, "ADEL: " + e.Message, "*JLW");
                llSuccess = false;
            }

            tAnswer._avalue[0].Value = (llSuccess ? 1 : 0);
            return tAnswer;
        }


        // return a 2D array containing file information
        public static async Task<JAXObjects.Token> ADir(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            string string2, stype2;

            if (p.Count > 0)
            {
                stype2 = p[0][..1];
                string2 = p[0][1..];
            }
            else
            {
                stype2 = "C";
                string2 = "*.*";
            }

            int results = 0;

            // TODO 
            if (p.Count > 1 || (int.TryParse(p[1][1..], out int intval3) == false)) intval3 = 0;
            if (p.Count > 2 || (int.TryParse(p[2][1..], out int intval4) == false)) intval4 = 0;

            if (stype2.Equals("C"))
            {
                string folder = JAXLib.JustPath(string2);
                string skel = JAXLib.JustFName(string2);
                skel = string.IsNullOrEmpty(skel) ? "*.*" : skel;

                folder = string.IsNullOrEmpty(folder) ? App.CurrentDS.JaxSettings.Default : JAXLib.Addbs(folder);

                FilerLib.GetDirectory(folder + skel, out string[] fileArray);

                if (fileArray.Length > 0)
                {
                    JAXObjects.Token tkArray = await AppVars.GetVarToken(string1);
                    if (tkArray.TType.Equals("A") == false || tkArray.Row != fileArray.Length || tkArray.Col != 5)
                        AppVars.SetVarOrMakePrivate(string1, fileArray.Length, 5, true);

                    JAXObjects.Token tk = await AppVars.GetVarToken(string1);
                    int k = 0;

                    for (int i = 0; i < fileArray.Length; i++)
                    {
                        FilerLib.GetFileInfo(fileArray[i], out string[] fileInfo);
                        if (string.IsNullOrEmpty(fileInfo[0]))
                            break;

                        results++;

                        if (int.TryParse(fileInfo[1], out int fsize) == false) fsize = 0;
                        if (DateTime.TryParse(fileInfo[2], out DateTime dtm) == false) dtm = DateTime.MinValue;
                        dtm = TimeLib.UTCtoLocal(dtm, TimeZoneInfo.Local);

                        tk._avalue[k++].Value = fileInfo[0];
                        tk._avalue[k++].Value = fsize;
                        tk._avalue[k++].Value = App.CurrentDS.JaxSettings.Century ? dtm.ToString("MM/dd/yyyy") : dtm.ToString("MM/dd/yy");
                        tk._avalue[k++].Value = dtm.ToString("HH:mm:ss");
                        tk._avalue[k++].Value = fileInfo[3];
                    }
                }

                tAnswer._avalue[0].Value = results;
            }
            else
                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            return tAnswer;
        }


        // Returns the number of an array element from the element's subscripts.
        public static async Task<JAXObjects.Token> AElement(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            int intval1, intval2, intval3;
            tAnswer._avalue[0].Value = 0;

            if (p.Count < 1 || (int.TryParse(p[0][1..], out intval2) == false)) intval2 = 0;
            if (p.Count < 2 || (int.TryParse(p[1][1..], out intval3) == false)) intval3 = 0;

            JAXObjects.Token tk = await AppVars.GetVarToken(string1);
            if (tk.TType.Equals("A"))
            {
                intval1 = tk.Row * tk.Col;

                if (intval3 > 0)
                {
                    intval3 = (intval2 - 1) * tk.Col + intval3;
                    if (intval3 <= intval1)
                        tAnswer._avalue[0].Value = (intval3 - 1);
                }
                else
                {
                    intval3 = (intval2 - 1) * tk.Col;
                    if (intval3 <= intval1)
                        tAnswer._avalue[0].Value = (intval3 - 1);
                }
            }

            return tAnswer;
        }

        // Make/update an array with the latest error information
        public static JAXObjects.Token AError(AppClass App, string string1)
        {
            JAXObjects.Token tAnswer = new();
            AppVars.SetVarOrMakePrivate(string1, 1, 7, true);

            var Err = AppErrorHandling.GetCurrentError();
            AppVars.SetVar(string1, Err.ErrorNo, 1, 1);
            AppVars.SetVar(string1, Err.ErrorMessage, 1, 2);
            AppVars.SetVar(string1, null, 1, 3);
            AppVars.SetVar(string1, Err.ErrorProcedure, 1, 4);
            AppVars.SetVar(string1, null, 1, 5);
            AppVars.SetVar(string1, null, 1, 6);
            AppVars.SetVar(string1, null, 1, 7);

            tAnswer._avalue[0].Value = 1;
            return tAnswer;
        }

        // Return a 2D arrray of field information (field count x 18)
        public static JAXObjects.Token AFields(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            string string2 = string.Empty, stype2;
            stype2 = string.Empty;
            int result = 0, wa = 0;

            if (p.Count > 0)
            {
                string2 = p[0];
                stype2 = string2[..1];
                string2 = string2[1..];
            }

            switch (stype2)
            {
                case "N":
                    // Get the numeric value for the work area to use
                    if (int.TryParse(string2, out wa) == false) wa = -1;
                    break;

                case "C":
                    // Convert the string alias to numeric work area
                    wa = App.jaxDataSession[App.CurrentDataSession].ReturnWorkArea(string2, 0);
                    break;
            }

            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            if (thisDS.CurrentWA.DbfInfo.FieldCount > 0)
            {
                JAXDirectDBF.DBFInfo DbfInfo = thisDS.WorkAreas[wa].DbfInfo;
                result = DbfInfo.FieldCount;
                AppVars.SetVarOrMakePrivate(string1, result, 18, true);

                for (int i = 1; i <= result; i++)
                {
                    AppVars.SetVar(string1, DbfInfo.Fields[i].FieldName, i, 1); // Field Name
                    AppVars.SetVar(string1, DbfInfo.Fields[i].FieldType, i, 2); // Field Type
                    AppVars.SetVar(string1, DbfInfo.Fields[i].FieldLen, i, 3);   // Field width
                    AppVars.SetVar(string1, DbfInfo.Fields[i].FieldDec, i, 4);  // Field decimal
                    AppVars.SetVar(string1, DbfInfo.Fields[i].NullOK, i, 5); // Field is nullable
                    AppVars.SetVar(string1, DbfInfo.Fields[i].BinaryData, i, 6); // Codepage translation not allowed (data is binary)
                    AppVars.SetVar(string1, string.Empty, i, 7);    // Field Validation Expresion
                    AppVars.SetVar(string1, string.Empty, i, 8);    // Field validation Text
                    AppVars.SetVar(string1, DbfInfo.Fields[i].DefaultValue, i, 9);
                    AppVars.SetVar(string1, string.Empty, i, 10);   // Table validation Expression
                    AppVars.SetVar(string1, string.Empty, i, 11);   // Table validation Text
                    AppVars.SetVar(string1, string.Empty, i, 12);   // Long Table Name
                    AppVars.SetVar(string1, string.Empty, i, 13);   // Insert Trigger Expression
                    AppVars.SetVar(string1, string.Empty, i, 14);   // Update Trigger Expression
                    AppVars.SetVar(string1, string.Empty, i, 15);   // Delete Trigger Expression
                    AppVars.SetVar(string1, string.Empty, i, 16);   // Table comment
                    AppVars.SetVar(string1, DbfInfo.Fields[i].AutoIncNext, i, 17);  // NextValue for autoincrementing
                    AppVars.SetVar(string1, DbfInfo.Fields[i].AutoIncrement, i, 18);  // Step for autoincrementing
                }
            }

            tAnswer._avalue[0].Value = result;
            return tAnswer;
        }


        // Insert .F. at an array location and move everything down.  The last value
        // in the array is lost.
        public static async Task<JAXObjects.Token> AIns(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            bool llSuccess = true;
            int intval2, intval3;

            try
            {
                if (p.Count < 1 || (int.TryParse(p[0][1..], out intval2) == false)) intval2 = 0;
                if (p.Count < 2 || (int.TryParse(p[1][1..], out intval3) == false)) intval3 = 0;

                // Get the token from the memory location
                JAXObjects.Token tkArray = await AppVars.GetVarToken(string1);

                if (tkArray.TType.Equals("A"))
                {
                    if (intval3 == 2)
                    {
                        // Inserting a column
                        if (intval2 < 1 || intval2 > tkArray.Col)
                            throw new Exception("Invalid array reference");

                        // Set values in column to .F.
                        for (int r = intval2; r < tkArray.Row - 1; r++)
                        {
                            int destElement = (r - 1) * tkArray.Col + intval2 + 1;
                            int sourceElement = r * tkArray.Col + intval2;
                            tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                            tkArray._avalue[sourceElement].Value = false;
                        }
                    }
                    else
                    {
                        // Adding a row to a 2D array?
                        if (intval2 < 1 || intval2 > tkArray.Row)
                            throw new Exception("Invalid array reference");

                        if (tkArray.Col > 1)
                        {

                            // Move rows towards down and set target row to false
                            for (int r = tkArray.Row - 1; r > intval2 - 1; r--)
                            {
                                for (int c = 0; c < tkArray.Col; c++)
                                {
                                    int destElement = (r - 1) * tkArray.Col + c;
                                    int sourceElement = r * tkArray.Col + c;
                                    tkArray._avalue[destElement].Value = tkArray._avalue[sourceElement].Value;
                                    tkArray._avalue[sourceElement].Value = false;
                                }
                            }

                            for (int c = 0; c < tkArray.Col; c++)
                            {
                                tkArray.SetElement(tkArray.Row, c + 1);
                                tkArray.Element.Value = false;
                            }
                        }
                        else
                        {
                            // Adding an element at the position
                            for (int r = tkArray.Row - 1; r > intval2 - 1; r--)
                            {
                                tkArray._avalue[r].Value = tkArray._avalue[r - 1].Value;
                                tkArray._avalue[r - 1].Value = false;
                            }
                        }
                    }
                }
                else
                {
                    llSuccess = false;
                }
            }
            catch (Exception e)
            {
                AppErrorHandling.SetError(9999, "AINS: " + e.Message, System.Reflection.MethodBase.GetCurrentMethod()!.Name);
                llSuccess = false;
            }

            tAnswer._avalue[0].Value = llSuccess ? 1 : 0;
            return tAnswer;
        }

        // Return the rows or columns of an array
        public static async Task<JAXObjects.Token> ALen(AppClass App, string string1, int intval2)
        {
            JAXObjects.Token tAnswer = new();
            tAnswer._avalue[0].Value = 0;

            JAXObjects.Token tk = await AppVars.GetVarToken(string1);
            if (tk.TType.Equals("A"))
            {
                tAnswer._avalue[0].Value = intval2 switch
                {
                    1 => tk.Row,
                    2 => tk.Col,
                    _ => (tk.Row * tk.Col)
                };
            }

            return tAnswer;
        }



        public static JAXObjects.Token ALines(AppClass App, string varName, List<string> p)
        {
            JAXObjects.Token tAnswer = new();


            List<string> ln = GetALinesList(Program.CurrentApp, p);
            AppVars.SetVarOrMakePrivate(varName, ln.Count, 1, true);   // adjust array length

            for (int i = 1; i <= ln.Count; i++)
                AppVars.SetVar(varName, ln[i - 1], i, 1);

            tAnswer._avalue[0].Value = ln.Count;
            return tAnswer;
        }


        /*
         * Return a list of strings based on how VFP would break it
         * a memo field using parse character(s) and memowidth setting
         */
        /// <summary>
        /// Break a string up into a List based on VFP rules - pList[] is "CString", "NFlags", "CParsechars1", "CParsechars2"...
        /// </summary>
        /// <param name="App"></param>
        /// <param name="pList"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<string> GetALinesList(AppClass App, List<string> pList)
        {
            // Get the string to parse
            string string1 = string.Empty;
            if (pList.Count > 0) string1 = pList[0][1..];

            // Next, check for flags
            bool RemoveSpaces = false;
            bool IncludeLast = false;
            bool SkipEmpty = false;
            bool CaseSensitiveParse = false;
            bool IncludeParseChars = false;

            if (pList.Count > 1)
            {
                if (pList[1][0] == 'N')
                {
                    if (int.TryParse(pList[1][1..], out int nFlags))
                    {
                        RemoveSpaces = (nFlags & 1) > 0;
                        IncludeLast = (nFlags & 2) > 0;
                        SkipEmpty = (nFlags & 4) > 0;
                        CaseSensitiveParse = (nFlags & 8) > 0;
                        IncludeParseChars = (nFlags & 16) > 0;
                    }
                    else
                        throw new Exception("11|");
                }
                else
                    throw new Exception("11|");
            }

            if (pList.Count > 2)
            {
                pList.RemoveAt(0);
                pList.RemoveAt(0);
            }
            else
            {
                // Set up the default parse char list
                pList = [];
                pList.Add("N\r");
            }

            // Clean out the parse character list by trimming
            // off the "C" type flag.  Also toss error if
            // C not found which means it's an invalid type
            for (int i = 0; i < pList.Count; i++)
            {
                if (pList[i].Length < 1 || pList[i][0] != 'C') throw new Exception("11|");

                // Don't accept empty strings
                if (pList[i].Length > 1)
                    pList[i] = pList[i][1..];
            }

            // Init the properties
            List<string> listAnswer = [];
            int memowidth = Convert.ToInt32(App.CurrentDS.JaxSettings.MemoWidth);
            memowidth = memowidth < 1 ? string1.Length : memowidth;

            int aStart = 0;
            int aEnd = 0;
            StringComparison comp = CaseSensitiveParse ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            // Start parsing until done
            while (string1.Length > aEnd + memowidth)
            {
                string prs = string.Empty;
                int f = string1.Length;
                for (int i = 0; i < pList.Count; i++)
                {
                    // Is this parse character found and is
                    // it before the current end of line
                    int e = string1.IndexOf(pList[i], aStart, string1.Length - aStart, comp);
                    if (JAXLib.Between(e, 0, f - 1))
                    {
                        prs = pList[i]; // Remember the parse char
                        f = e;          // New end of line
                    }
                }

                // was a parsed character found and if so
                // was it less than or equal to the memowidth?
                if (f < 0 || f > memowidth)
                {
                    // Parsed char out of range so
                    // make sure it's empty string
                    f = memowidth;
                    prs = string.Empty;
                }

                // Extract the line
                string thisLine = string1[aStart..(aEnd + f)];

                if (RemoveSpaces)
                    thisLine = thisLine.Trim();

                // Include the parse characters?
                if (IncludeParseChars)
                    thisLine += prs;

                if (SkipEmpty == false || string.IsNullOrWhiteSpace(thisLine) == false)
                    listAnswer.Add(thisLine);

                aStart = aEnd + f;
                aEnd += f;
            }

            // Catch anything dangling off the end
            if (aStart < string1.Length)
            {
                string thisLine = string1[aStart..];

                if (RemoveSpaces)
                    thisLine = thisLine.Trim();

                if (IncludeLast || string.IsNullOrWhiteSpace(thisLine) == false)
                    listAnswer.Add(thisLine);
            }

            // Return the list
            return listAnswer;
        }



        // ALLTIM() functionality
        public static JAXObjects.Token Alltrim(string string1, List<string> pop)
        {
            JAXObjects.Token tAnswer = new();

            // Check and get the string to be trimed
            string strToTrim = string1;
            List<char> toTrim = [];

            if (pop.Count == 0)
            {
                // We trim these if nothing is sent
                toTrim.Add('\0');
                toTrim.Add(' ');
            }
            else
            {
                for (int i = 0; i < pop.Count; i++)
                {
                    // Make sure it's not an empty string
                    if (pop[i].Length > 1)
                    {
                        // Add the first char if a string
                        if (pop[i][0] == 'C')
                            toTrim.Add(pop[i][0]);
                        else
                            throw new Exception("11|");
                    }
                }
            }

            // Now start trimming until there
            // is a signal to stop
            while (true)
            {
                // If nothing left, we're done
                if (strToTrim.Length == 0)
                    break;

                // Remember what it looked like before trim
                string strBefore = strToTrim;

                // Attempt to trim each character off
                for (int i = 0; i < toTrim.Count; i++)
                {
                    strToTrim = strToTrim.TrimStart(toTrim[i]).TrimEnd(toTrim[i]);
                }

                // If no change, we're done
                if (strBefore.Equals(strToTrim))
                    break;
            }

            // Save and return
            tAnswer.Element.Value = strToTrim;
            return tAnswer;
        }


        public static JAXObjects.Token Asc(string string1)
        {
            JAXObjects.Token tAnswer = new();
            int intval1 = 0;

            if (string1.Length > 0)
            {
                byte[] b = Encoding.ASCII.GetBytes(string1);
                intval1 = b[0];
            }

            tAnswer._avalue[0].Value = intval1;
            return tAnswer;
        }

        public static JAXObjects.Token ASin(AppClass App, string string1, string stype1)
        {
            JAXObjects.Token tAnswer = new();
            tAnswer._avalue[0].Value = 0;

            if (double.TryParse(string1, out double val1) == false) val1 = 0D;
            if (stype1.Equals("N"))
                tAnswer._avalue[0].Value = System.Math.Asin(val1);
            else
                AppErrorHandling.SetError(11, string.Empty, System.Reflection.MethodBase.GetCurrentMethod()!.Name);

            return tAnswer;
        }


        // Scan a 1D or 2D array for an expresson with options
        public static async Task<JAXObjects.Token> AScan(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            string string2 = string.Empty;
            if (p.Count > 0) string2 = p[0];

            int nStartElement, nElementsSearched, nSearchColumn, nFlags;

            if (p.Count < 2 || (int.TryParse(p[1][1..], out nStartElement) == false)) nStartElement = 0;
            if (p.Count < 3 || (int.TryParse(p[2][1..], out nElementsSearched) == false)) nElementsSearched = 0;
            if (p.Count < 4 || (int.TryParse(p[3][1..], out nSearchColumn) == false)) nSearchColumn = 0;
            if (p.Count < 5 || (int.TryParse(p[4][1..], out nFlags) == false)) nFlags = 0;

            // nFlag
            //   0      Case sensitive
            //   1      Case Insensitive
            //   2      Case sensitive
            //   3      Case Insensitive
            //   4      Exact off
            //   5      Case Insensitive; exact off
            //   6      Exact on
            //   7      Case Insensitive; exact on
            //   8      Return row number
            //   9      Case Insensitive; return row number
            //  10      Return row number
            //  11      Case Insensitive; return row number
            //  12      Return row number; exact off
            //  13      Return row number; exact off; case insensitive
            //  14      Return row number; exact on
            //  15      Return row number; exact on; case insensitive

            bool caseSensitive = (nFlags & (1 << 0)) > 0;
            bool exactOff = (nFlags & (1 << 1)) > 0 && (nFlags & (1 << 2)) > 0;
            bool returnRow = (nFlags & (1 << 3)) > 0;

            JAXObjects.Token searchTK = await AppVars.GetVarToken(string1);
            if (searchTK.TType.Equals("A"))
            {
                if (nSearchColumn > 0)
                {
                    if (nSearchColumn <= searchTK.Col)
                    {
                        // Search a column in a 2D array (even if rows x 1)
                        for (int i = 0; i < searchTK.Row; i++)
                        {
                            searchTK.SetElement(i, nSearchColumn);
                            string stval = searchTK.Element.Type + searchTK.Element.Value.ToString();
                            string aval = string2;

                            if (caseSensitive == false)
                            {
                                stval = (searchTK.Element.Type + searchTK.Element.Value.ToString()).ToUpper();
                                aval = string2.ToUpper();
                            }

                            if (exactOff)
                            {
                                // Exact off
                                if ((stval.Length <= aval.Length) && stval.Equals(aval[..stval.Length]))
                                {
                                    // GOT IT!
                                    if (returnRow)
                                        tAnswer._avalue[0].Value = i + 1;
                                    else
                                        tAnswer._avalue[0].Value = i * searchTK.Col + nSearchColumn;
                                    break;
                                }
                            }
                            else
                            {
                                // Exact on
                                if (stval.Equals(aval))
                                {
                                    // GOT IT!
                                    if (returnRow)
                                        tAnswer._avalue[0].Value = i + 1;
                                    else
                                        tAnswer._avalue[0].Value = i * searchTK.Col + nSearchColumn;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Search by element
                    for (int i = nStartElement; i < nStartElement + nElementsSearched; i++)
                    {
                        searchTK.SetElement(i, 1);
                        string stval = searchTK.Element.Type + searchTK.Element.Value.ToString();
                        string aval = string2;

                        if (caseSensitive == false)
                        {
                            stval = (searchTK.Element.Type + searchTK.Element.Value.ToString()).ToUpper();
                            aval = string2.ToUpper();
                        }

                        if (exactOff)
                        {
                            if ((stval.Length <= aval.Length) && stval.Equals(aval[..stval.Length]))
                            {
                                // GOT IT!
                                tAnswer._avalue[0].Value = returnRow ? ((i / searchTK.Col) + 1) : i;
                                break;
                            }
                        }
                        else
                        {
                            if (stval.Equals(aval))
                            {
                                // GOT IT!
                                if (returnRow)
                                    tAnswer._avalue[0].Value = ((i / searchTK.Col) + 1);
                                else
                                    tAnswer._avalue[0].Value = i;
                                break;
                            }
                        }
                    }
                }
            }

            return tAnswer;
        }

        // Make/update a 1D array with the list of data session IDs
        public static JAXObjects.Token ASessions(AppClass App, string string1)
        {
            JAXObjects.Token tAnswer = new();
            int j1 = 0;
            JAXDataSession thisDS = App.jaxDataSession[App.CurrentDataSession];

            j1 = App.jaxDataSession.Count;

            // Get a list of active data sessions
            if (j1 > 0)
            {
                AppVars.SetVarOrMakePrivate(string1, j1, 1, true);
                for (int i = 0; i < App.jaxDataSession.Count; i++)
                    AppVars.SetVar(string1, App.jaxDataSession.ElementAt(i).Key, i, 1);
            }

            tAnswer._avalue[0].Value = j1;
            return tAnswer;
        }


        // return app stack level information
        public static JAXObjects.Token AStackinfo(AppClass App, string string1)
        {
            JAXObjects.Token tAnswer = new();
            AppVars.SetVarOrMakePrivate(string1, App.AppLevels.Count, 6, true);

            for (int i = 0; i < App.AppLevels.Count; i++)
            {
                // TODO - Should I support 4 - 6?
                AppVars.SetVar(string1, i, 1, 1);                                           // Level
                AppVars.SetVar(string1, App.AppLevels[0].PrgName, 2, 1);                    // Main Program
                AppVars.SetVar(string1, JAXLib.JustStem(App.AppLevels[i].PrgName), 3, 1);   // Object or module
                AppVars.SetVar(string1, string.Empty, 4, 1);                                // Object or model source name
                AppVars.SetVar(string1, 0, 5, 1);                                           // Line Number
                AppVars.SetVar(string1, string.Empty, 6, 1);                                // Source
            }

            tAnswer._avalue[0].Value = App.AppLevels.Count;
            return tAnswer;
        }


        // Return subscript information for an element
        public static async Task<JAXObjects.Token> ASubscript(AppClass App, string string1, List<string> p)
        {
            JAXObjects.Token tAnswer = new();
            int intval2, intval3;

            if (p.Count < 1 || (int.TryParse(p[0][1..], out intval2) == false)) intval2 = 0;
            if (p.Count < 2 || (int.TryParse(p[1][1..], out intval3) == false)) intval3 = 3;

            JAXObjects.Token tk = await AppVars.GetVarToken(string1);

            if (intval2 <= tk.Row * tk.Col)
                tAnswer._avalue[0].Value = intval3 == 1 ? (tk.Col == 1 ? intval2 : (intval2 / tk.Col)) : (tk.Col == 1 ? 1 : intval2 % tk.Col + 1);
            else
                tAnswer._avalue[0].Value = 0;

            return tAnswer;
        }


        public static async Task<List<string>> GetClassVarList(AppClass App, string className)
        {
            List<string> matchX = [];

            // Load the local vars for the current level
            for (int i = App.AppLevels.Count - 1; i >= 0; i--)
            {
                List<string> varsX = [];

                if (i == App.AppLevels.Count - 1)
                {
                    varsX = App.AppLevels[i].LocalVars.GetVarNames();

                    for (int j = 0; j < varsX.Count; j++)
                    {
                        JAXObjects.Token t = await AppVars.GetVarToken(varsX[j]);
                        if (t.TType.Equals("O"))
                        {
                            JAXObjectWrapper this1 = (JAXObjectWrapper)t.Element.Value;
                            if (this1.BaseClass.Equals(className, StringComparison.OrdinalIgnoreCase) || this1.Class.Equals(className, StringComparison.OrdinalIgnoreCase))
                            {
                                if (matchX.Contains(varsX[j].ToLower()) == false)
                                    matchX.Add(varsX[j].ToLower());
                            }
                        }
                    }
                }


                // Load the private vars for each level
                // Note: Level 0 private vars are the global
                // vars of the current appllication
                varsX = App.AppLevels[i].PrivateVars.GetVarNames();

                for (int j = 0; j < varsX.Count; j++)
                {
                    JAXObjects.Token t = await AppVars.GetVarToken(varsX[j]);
                    if (t.TType.Equals("O"))
                    {
                        JAXObjectWrapper this1 = (JAXObjectWrapper)t.Element.Value;
                        if (this1.BaseClass.Equals(className, StringComparison.OrdinalIgnoreCase) || this1.Class.Equals(className, StringComparison.OrdinalIgnoreCase))
                        {
                            if (matchX.Contains(varsX[j].ToLower()) == false)
                                matchX.Add(varsX[j].ToLower());
                        }
                    }
                }
            }

            // Sort the matched vars
            matchX.Sort();
            return matchX;
        }


        public static int ASort(AppClass App, string string1, List<string> p)
        {
            throw new Exception("1999||ASort");
        }
    }
}
