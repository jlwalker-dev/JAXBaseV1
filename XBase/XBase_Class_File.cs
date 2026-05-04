using JAXBase.Core;
using System.Security;
using System.Text;
using static JAXBase.XBase.JAXObjectsAux;
using JAXBase.Utilities;
using System.Windows.Interop;

namespace JAXBase.XBase
{
    public class XBase_Class_File : XBase_Avalonia
    {
        public new string MyBaseClass = "File";
        public new string MyDefaultName = "file";

        public XBase_Class_File(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "file" : name;
            SetVisualObject(null, "File", name, false, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * Call the JAXCode for a method
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> _CallMethod(string methodName)
        {
            int result = 0;
            string msg = "";
            List<JAXObjects.Token> appParams = [];

            try
            {
                if (Methods.ContainsKey(methodName.ToLower()))
                {
                    MethodClass mc = new();

                    string cCode = Methods[methodName.ToLower()].CompiledCode;

                    // Execute the code
                    if (cCode.Length > 0)
                    {
                        // Call the routine to compile and execute a block of code
                        _ = App.JaxExecuter.ExecuteCodeBlock(me, methodName, cCode);
                    }
                    else
                    {
                        // Call the normal process
                        await DoDefault(methodName);
                    }
                }
                else
                {
                    msg = methodName;
                    result = 6501;
                }

            }
            catch (Exception ex)
            {
                result = 9999;
                msg= ex.Message;
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{msg}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }


        /*
         * Special case situations for this class
         */
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            JAXObjects.Token tk = new();
            tk.Element.Value = objValue;

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (fs is not null && (fs.CanWrite || fs.CanWrite))
                {
                    // Filestream is open for business so check these properties
                    switch (propertyName.ToLower())
                    {
                        case "filename":
                        case "binary":
                        case "name":
                            // Can't rename when in use
                            result = 3024;
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            break;
                    }
                }
                else
                {
                    if (propertyName.Equals("filename", StringComparison.OrdinalIgnoreCase))
                    {
                        if (tk.Element.Type.Equals("C"))
                        {
                            // Fix the filename value for path and naming
                            string string2 = tk.AsString();
                            string2 = JAXLib.FixFilePath(string2, App.CurrentDS.JaxSettings.Default);
                            objValue = AppHelper.FixFileCase(string.Empty, string2, App.CurrentDS.JaxSettings.Naming, App.CurrentDS.JaxSettings.NamingAll);
                        }
                        else
                            result = 11;
                    }

                    if (result == 0)
                        result = await base.SetProperty(propertyName, objValue, objIdx);
                }

                if (result > 0)
                {
                    _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                    if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                        AppErrorHandling.SetError(result, $"{result}|", string.Empty);
                }
            }

            return result;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = methodName.ToLower() switch
            {
                "close" => FileClose(),
                "closed" => 1999,
                "goto" => FileLocate(),
                "open" => FileOpen(),
                "opened" => 1999,
                "read" => FileRead(),
                "write" => FileWrite(),
                _ => IsMember(methodName).Equals("M") ? 0 : 1737    // TODO - calling the dodefault of other methods?
            };

            string info = "";

            // Process any errors
            if (result > 0)
            {
                info = result switch
                {
                    11 => string.Empty,
                    333 => JAXLib.JustPath(UserProperties["filename"].AsString()),
                    401 => string.Empty,
                    1705 => string.Empty,
                    1737 => methodName.ToUpper(),
                    1963 => JAXLib.JustPath(UserProperties["filename"].AsString()),
                    _ => UserProperties["filename"].AsString(),
                };
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|{info}|{methodName}", System.Reflection.MethodBase.GetCurrentMethod()!.Name);

                result = -1;
            }

            return result;
        }

        public override string[] JAXMethods()
        {
            return ["close", "goto", "open", "read", "write", "writeexpression", "writemethod"];
        }

        public override string[] JAXEvents()
        {
            return ["closed", "destroy", "error", "init", "opened"];
        }

        /*
         * property data types
         *      C = Character
         *      N = Numeric         I=Integer       R=Color
         *      D = Date
         *      T = DateTime
         *      L = Logical         LY = Yes/No logical
         *      
         *      Attributes
         *          ! Protected - user can't change after initialization
         *          $ Special Handling - do not auto process
         */
        public override string[] JAXProperties()
        {
            return [
                "attributes,C!,",
                "baseclass,C!,Custom","binary,L,.F.",
                "class,C!,Custom","classlibrary,C$,","comment,C,","currentposition,n!,0",
                "eof,L!,.F.",
                "filename,C,",
                "name,C,custom",
                "parent,o$,","parentclass,C$,",
                "position,N,-1",
                "readcount,N!,0",
                "readlength,N,0",
                "tag,C,",
                "writecount,N!,0"
                ];
        }


        FileStream? fs = null;
        StreamReader? sr = null;
        StreamWriter? sw = null;

        bool FileWriteFlag = false;
        bool FileReadFlag = false;
        bool FileBinaryFlag = false;

        /*
         * Open a file for read/write
         */
        private int FileOpen()
        {
            int result = 0;
            JAXObjects.Token tk=new();

            if (fs is null)
            {
                if (App.ParameterClassList.Count < 2)
                {

                    if (App.ParameterClassList.Count == 0)
                    {
                        tk = new();
                        tk.Element.Value = 0;
                    }
                    else
                        tk.CopyFrom(App.ParameterClassList[0].token);

                    App.ParameterClassList.Clear();

                    if (tk.Element.Type.Equals("N"))
                    {
                        int b = tk.AsInt();
                        string fName;

                        if (UserProperties["filename"].Element.Type.Equals("C"))
                        {
                            fName = UserProperties["filename"].AsString().Trim();

                            if (string.IsNullOrWhiteSpace(fName))
                                result = 1;
                            else
                            {
                                /*----------------------------*
                                 * Byte Description
                                 *  1   1=Create
                                 *  2   1=Read
                                 *  3   1=Write
                                 *  4   1=Binary
                                 *----------------------------*/
                                bool create = (b & 1) > 0;
                                FileReadFlag = (b & 2) > 0;
                                FileWriteFlag = (b & 4) > 0;
                                FileBinaryFlag = (b & 8) > 0;

                                try
                                {
                                    // Text file operations
                                    if (create)
                                    {
                                        fs = File.Create(fName);
                                        fs.Close();
                                    }

                                    if (FileWriteFlag && FileReadFlag)
                                        fs = new(fName, FileMode.Open, FileAccess.ReadWrite);
                                    else if (FileWriteFlag)
                                        fs = new(fName, FileMode.Open, FileAccess.Write);
                                    else
                                        fs = new(fName, FileMode.Open, FileAccess.Read);

                                    if (FileBinaryFlag == false)
                                    {
                                        sr = new(fs);
                                        sw = new(fs)
                                        {
                                            AutoFlush = true
                                        };
                                    }
                                }
                                catch (DirectoryNotFoundException) { result = 1963; }
                                catch (FileNotFoundException) { result = 1; }
                                catch (UnauthorizedAccessException) { result = 1705; }
                                catch (SecurityException) { result = 2222; }
                                catch (PathTooLongException) { result = 202; }
                                catch (NotSupportedException) { result = 333; }
                                catch (Exception) { result = 2082; }
                            }
                        }
                        else
                            result = 11;
                    }
                    else
                        result = 11;
                }
                else
                    result = 1230;  // too many parameters sent
            }
            else
                result = 2080;      // In use

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        private int FileClose()
        {
            int result = 0;

            if (fs is null)
                result = 2081; // Not open
            else
            {
                // Dispose of everthing
                UserProperties["readcount"].Element.Value = 0;
                UserProperties["position"].Element.Value = -1;
                UserProperties["currentposition"].Element.Value = -1;
                UserProperties["eof"].Element.Value = false;
                UserProperties["filename"].Element.Value = string.Empty;

                sw?.Dispose();
                sr?.Dispose();
                fs?.Dispose();
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        // Read a line of text
        private int FileRead()
        {
            JAXObjects.Token tk = new();
            int result = 0;
            long pos = UserProperties["position"].AsLong();
            int linebytes = UserProperties["readlength"].AsInt();
            UserProperties["readcount"].Element.Value = 0;

            // Default a return value of an empty string
            App.ReturnValue.Element.Value = string.Empty;

            // Is there a parameter?  It should be line/byte count.
            if (App.ParameterClassList.Count > 0)
            {
                // We have a byte/line count
                tk.Element.Value = App.ParameterClassList[0].token.Element.Value;
                if (tk.Element.Type.Equals("N"))
                {
                    linebytes = tk.AsInt();
                    if (linebytes > 0)
                        UserProperties["readlength"].Element.Value = linebytes;
                    else
                        result = 11;    // Sent a non-numeric
                }
            }

            if (result == 0)
            {
                // If more than 1 parameter, toss an error
                if (App.ParameterClassList.Count > 1)
                    result = 1230;
                else
                {
                    if (fs is null)
                        result = 2081;  // no file open
                    else if (fs.CanRead == false)
                        result = 2083;
                    else
                    {
                        if (UserProperties["eof"].AsBool())
                            result = 4;  // EOF!
                        else if (sr is null)
                        {
                            // Read bytes
                            byte[] readBytes = new byte[linebytes];
                            long position = pos;
                            if (pos < 0)
                                position = UserProperties["currentposition"].AsLong();

                            // Correct if first read
                            position = position < 0 ? 0 : position;

                            int bytesRead = fs.Read(readBytes, 0, readBytes.Length);

                            // Did we hit an eof?
                            UserProperties["readcount"].Element.Value = bytesRead;
                            UserProperties["eof"].Element.Value = fs.Position >= fs.Length;
                            UserProperties["currentposition"].Element.Value = fs.Position;
                            if (pos >= 0)
                                UserProperties["position"].Element.Value = fs.Position;

                            // Set the return value
                            App.ReturnValue.Element.Value = System.Text.Encoding.UTF8.GetString(readBytes);
                        }
                        else
                        {
                            // Read lines
                            //sr.DiscardBufferedData();

                            StringBuilder lines = new();
                            int lineCount = 0;
                            for (int i = 0; i < (linebytes < 1 ? 1 : linebytes); i++)
                            {
                                string? line = sr.ReadLine();
                                if (line is null) break;  // Critical: this is how you detect EOF
                                lines.Append(line);
                                lineCount++;
                            }

                            UserProperties["readcount"].Element.Value = lineCount;
                            UserProperties["eof"].Element.Value = sr.EndOfStream;
                            UserProperties["currentposition"].Element.Value = -1;

                            // Set the return value
                            App.ReturnValue.Element.Value = lines.ToString();
                        }
                    }
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        // Write a line of text FileWrite(string [,position])
        private int FileWrite()
        {
            int result = 0;
            JAXObjects.Token tk = new();
            string WriteTo = string.Empty;
            long pos = UserProperties["position"].AsLong();
            UserProperties["readcount"].Element.Value = 0;

            if (fs is null)
                result = 2081; // Not open
            else
            {
                UserProperties["writecount"].Element.Value = 0;
                App.ReturnValue.Element.Value = true;

                // Is there a parameter?  It should be line/byte count.
                if (App.ParameterClassList.Count > 0)
                {
                    // We have string
                    tk.Element.Value = App.ParameterClassList[0].token.Element.Value;
                    if (tk.Element.Type.Equals("C"))
                    {
                        WriteTo = App.ParameterClassList[0].token.AsString();

                        if (App.ParameterClassList.Count > 1)
                        {
                            // We have a positiong (long)
                            tk.Element.Value = App.ParameterClassList[0].token.Element.Value;
                            if (tk.Element.Type.Equals("N"))
                            {
                                pos = App.ParameterClassList[0].token.AsLong();

                                // Did they send 3 or more parameters?
                                if (App.ParameterClassList.Count > 2)
                                    result = 1230;
                            }
                            else
                                result = 11;
                        }
                    }
                    else
                        result = 11;
                }
            }

            if (result == 0)
            {
                if (sw is null)
                {
                    if (fs is null)
                        result = 2081;
                    else if (fs.CanRead == false)
                        result = 2084;
                    else
                    {
                        // Binary write
                        int count = FilerLib.TryWriteAllBytes(fs, WriteTo, pos);
                        if (count == WriteTo.Length)
                        {
                            UserProperties["writecount"].Element.Value = count;
                            UserProperties["currentposition"].Element.Value = fs.Position;
                            if (pos >= 0)
                                UserProperties["position"].Element.Value = fs.Position;
                            UserProperties["eof"].Element.Value = fs.Position >= fs.Length;

                        }
                        else
                            result = 2084;  // Can not write
                    }
                }
                else
                {
                    // StreamWriter text write
                    try
                    {
                        // Check if base stream is writable
                        if (sw.BaseStream?.CanWrite == true)
                        {
                            sw.WriteLine(WriteTo);
                            sw.Flush();

                            int i = WriteTo.Count(c => c == '\r') + 1;
                            UserProperties["writecount"].Element.Value = i;
                            UserProperties["currentposition"].Element.Value = UserProperties["currentposition"].AsInt() + i;
                            UserProperties["eof"].Element.Value = true;
                        }
                        else
                            result = 2084;  // Cannot write
                    }
                    catch (ObjectDisposedException) { result = 9500; }
                    catch (IOException) { result = 2084; }
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }


        // Position the record pointer
        private int FileLocate()
        {
            int result = 0;
            JAXObjects.Token tk = new();
            UserProperties["readcount"].Element.Value = 0;

            if (fs is null)
                result = 1113;
            else
            {
                if (App.ParameterClassList.Count < 2)
                {
                    if (App.ParameterClassList.Count == 0)
                        tk.Element.Value = 0;
                    else
                        tk.CopyFrom(App.ParameterClassList[0].token);

                    App.ParameterClassList.Clear();

                    if (tk.Element.Type.Equals("N"))
                    {
                        int b = tk.AsInt();
                        if (b < 0)
                            result = 41;
                        else
                            fs.Seek(b, SeekOrigin.Begin);
                    }
                    else if (tk.Element.Type.Equals("C"))
                    {
                        if (tk.AsString().Equals("TOP", StringComparison.OrdinalIgnoreCase))
                        {
                            UserProperties["currentposition"].Element.Value = 0;
                            UserProperties["position"].Element.Value = -1;
                            fs.Seek(0, SeekOrigin.Begin);
                        }
                        else if (tk.AsString().StartsWith("BOT", StringComparison.OrdinalIgnoreCase))
                        {
                            if (sr is null)
                                UserProperties["currentposition"].Element.Value = fs.Length;
                            else
                                UserProperties["currentposition"].Element.Value = FilerLib.CountLines(sr!);

                            UserProperties["position"].Element.Value = -1;
                            fs.Seek(0, SeekOrigin.End);
                        }
                        else
                            result = 11;
                    }
                    else
                        result = 11;
                }
            }

            if (result > 0)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                result = -1;
            }

            return result;
        }
    }
}
