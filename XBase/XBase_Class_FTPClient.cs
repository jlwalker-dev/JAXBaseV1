/*
 * JAXBase FTP/SFTP Client Class
 * Exact pattern of XBase_Class_TCPClient.cs
 * Uses FluentFTP (FTP/FTPS) + SSH.NET (SFTP)
 * All user-facing state in UserProperties
 */

using FluentFTP;
using JAXBase.Core;
using JAXBase.Utilities;
using NodaTime.Calendars;
using Org.BouncyCastle.Utilities.Collections;
using Renci.SshNet;
using System.Reflection;
using System.Windows.Interop;
using static System.Net.WebRequestMethods;

namespace JAXBase.XBase
{
    public class XBase_Class_FTPClient : XBase_Avalonia, IDisposable
    {
        public new string MyBaseClass = "FtpClient";
        public new string MyDefaultName = "ftpclient";

        // Internal clients (performance)
        private AsyncFtpClient? _ftpClient;
        private SftpClient? _sftpClient;
        private bool _isSftp = false;
        private readonly object _lock = new();
        private bool _disposed = false;

        // Events (VFP-style)
        public event Action<string>? OnFtpResponse;
        public event Action<long, long>? OnProgress;
        public event Action<string>? OnError;

        public List<WebHistory> history = [];

        public XBase_Class_FTPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();

            RegisterProperties();
        }

        private void RegisterProperties()
        {
            AddProperty("protocol", "FTP");
            AddProperty("host", "");
            AddProperty("port", 21);
            AddProperty("username", "anonymous");
            AddProperty("password", "");
            AddProperty("privatekeypath", "");
            AddProperty("passphrase", "");
            AddProperty("passive", true);
            AddProperty("usessl", false);
            AddProperty("isconnected", false, true);   // read-only
            AddProperty("lasterror", "", true);        // read-only
        }

        private void AddProperty(string name, object defaultValue, bool readOnly = false)
        {
            if (!UserProperties.ContainsKey(name))
            {
                UserProperties[name] = new JAXObjects.Token { Protected = readOnly };
                UserProperties[name].Element.Value = defaultValue;
            }
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);
            SetStatus(0, "Initialized");
            return result;
        }


        private void SetStatus(int statuscode, string message)
        {
            WebHistory webHistory = new()
            {
                URL = UserProperties["lasturl"].AsString(),
                Status = statuscode,
                Content = message
            };

            AppIO.DebugLog($"HTTP Status: {statuscode} - {message}");
            history.Insert(0, webHistory);
            if (UserProperties["historymax"].AsInt() > 0 && history.Count > UserProperties["historymax"].AsInt())
                history.RemoveAt(history.Count - 1);
        }


        // ===================================================================
        // Property Helpers (performance)
        // ===================================================================
        private string GetString(string key) => UserProperties.TryGetValue(key, out var t) ? t.AsString() : "";
        private void SetString(string key, string value)
        {
            if (UserProperties.ContainsKey(key)) UserProperties[key].Element.Value = value;
        }

        private int GetInt(string key) => UserProperties.TryGetValue(key, out var t) ? t.AsInt() : 0;
        private void SetInt(string key, int value)
        {
            if (UserProperties.ContainsKey(key)) UserProperties[key].Element.Value = value;
        }

        private bool GetBool(string key) => UserProperties.TryGetValue(key, out var t) && t.AsBool();
        private void SetBool(string key, bool value)
        {
            if (UserProperties.ContainsKey(key)) UserProperties[key].Element.Value = value;
        }

        // ===================================================================
        // GetProperty / SetProperty
        // ===================================================================
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "isconnected":
                        returnToken.Element.Value = GetBool("isconnected");
                        break;
                    case "lasterror":
                        returnToken.Element.Value = GetString("lasterror");
                        break;
                    default:
                        result = 1;
                        returnToken.CopyFrom(UserProperties[propertyName]);
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
                _AddError(result, 0, $"{result}|{propertyName}|", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                returnToken.Element.MakeNull();
            }

            return returnToken;
        }

        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else if (UserProperties.ContainsKey(propertyName))
            {
                bool isConnected = GetBool("isconnected");

                switch (propertyName)
                {
                    case "protocol":
                        if (tk.Element.Type.Equals("C"))
                        {
                            if (isConnected) result = 1541;
                            else SetString("protocol", tk.AsString().ToUpper());
                        }
                        else result = 11;
                        break;

                    case "host":
                        if (isConnected) result = 1541;
                        else if (tk.Element.Type.Equals("C")) SetString("host", tk.AsString());
                        else result = 11;
                        break;

                    case "port":
                        if (isConnected) result = 1541;
                        else if (tk.Element.Type.Equals("N")) SetInt("port", tk.AsInt());
                        else result = 11;
                        break;

                    case "username":
                        if (tk.Element.Type.Equals("C")) SetString("username", tk.AsString());
                        else result = 11;
                        break;

                    case "password":
                        if (tk.Element.Type.Equals("C")) SetString("password", tk.AsString());
                        else result = 11;
                        break;

                    case "privatekeypath":
                        if (tk.Element.Type.Equals("C")) SetString("privatekeypath", tk.AsString());
                        else result = 11;
                        break;

                    case "passphrase":
                        if (tk.Element.Type.Equals("C")) SetString("passphrase", tk.AsString());
                        else result = 11;
                        break;

                    case "passive":
                        if (tk.Element.Type.Equals("L")) SetBool("passive", tk.AsBool());
                        else result = 11;
                        break;

                    case "usessl":
                        if (tk.Element.Type.Equals("L")) SetBool("usessl", tk.AsBool());
                        else result = 11;
                        break;

                    default:
                        result = 1;
                        break;
                }

                if (result == 0 && UserProperties.ContainsKey(propertyName))
                    UserProperties[propertyName].Element.Value = tk.Element.Value;
            }
            else
                result = 1559;

            if (result > 0)
            {
                SetString("lasterror", $"Property error {result} on {propertyName}");
                OnError?.Invoke(GetString("lasterror"));
            }

            return result;
        }

        // ===================================================================
        // Core Methods
        // ===================================================================
        public int Connect()
        {
            int err = 0;

            lock (_lock)
            {
                try
                {
                    _isSftp = GetString("protocol") == "SFTP";
                    int port = GetInt("port");
                    if (_isSftp && port == 21) SetInt("port", 22);

                    if (_isSftp)
                        err = ConnectSftp();
                    else
                        err = ConnectFtp();
                }
                catch (Exception ex)
                {
                    SetString("lasterror", ex.Message);
                    OnError?.Invoke(ex.Message);
                    string msg = $"FTP Connect Error: {ex.Message}";
                    SetStatus(13, msg);
                    err = 8220;
                    _AddError(err, 0, $"{err}|{msg}|Connect", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            Program.CurrentApp.ReturnValue.Element.Value = err == 0;
            return err;
        }

        private int ConnectFtp()
        {
            int err = 0;

            _ftpClient = new AsyncFtpClient(GetString("host"), GetString("username"), GetString("password"), GetInt("port"));

            if (GetBool("usessl"))
            {
                _ftpClient.Config.EncryptionMode = FtpEncryptionMode.Explicit;
            }

            _ftpClient.ValidateCertificate += (_, e) => e.Accept = true;

            // Correct call for AsyncFtpClient
            _ftpClient.AutoConnect().Wait();

            bool connected = _ftpClient.IsConnected;
            SetBool("isconnected", connected);
            SetStatus(connected ? 1 : 0, connected ? "Connected" : "Failed");
            OnFtpResponse?.Invoke($"Connected to {GetString("host")}:{GetInt("port")}");

            return err;
        }

        private int ConnectSftp()
        {
            int err = 0;

            string pkPath = GetString("privatekeypath");
            if (!string.IsNullOrEmpty(pkPath))
            {
                var key = new PrivateKeyFile(pkPath, GetString("passphrase"));
                _sftpClient = new SftpClient(GetString("host"), GetInt("port"), GetString("username"), key);
            }
            else
            {
                _sftpClient = new SftpClient(GetString("host"), GetInt("port"), GetString("username"), GetString("password"));
            }

            _sftpClient.Connect();
            bool connected = _sftpClient.IsConnected;
            SetBool("isconnected", connected);
            SetStatus(connected ? 1 : 0, connected ? "Connected" : "Failed");
            OnFtpResponse?.Invoke($"SFTP Connected to {GetString("host")}:{GetInt("port")}");

            return err;
        }


        public int Disconnect()
        {
            bool result = false;
            int err = 0;

            lock (_lock)
            {
                try
                {
                    if (_ftpClient != null)
                    {
                        _ftpClient.Disconnect();
                        _ftpClient.Dispose();
                        _ftpClient = null;
                    }
                    if (_sftpClient != null)
                    {
                        _sftpClient.Disconnect();
                        _sftpClient.Dispose();
                        _sftpClient = null;
                    }
                    SetBool("isconnected", false);
                    SetStatus(0, "Disconnected");
                    result = true;
                }
                catch (Exception ex)
                {
                    SetString("lasterror", ex.Message);
                    OnError?.Invoke(ex.Message);
                    result = false;
                    err = 8220;
                    string msg = "FTP Disconnect Error: " + ex.Message;
                    SetStatus(13, msg);
                    _AddError(err, 0, $"{err}|{msg}|Disconnect", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            Program.CurrentApp.ReturnValue.Element.Value = result;
            return err;
        }


        public int ListDirectory(string remotePath = ".")
        {
            int err = 0;

            if (!GetBool("isconnected"))
                err = 8221;
            else
            {
                try
                {
                    if (_isSftp && _sftpClient != null)
                    {
                        var files = _sftpClient.ListDirectory(remotePath);
                        var list = new List<string>();

                        JAXObjects.Token fList = new();
                        int i = 0;
                        foreach (var f in files)
                        {
                            fList.SetDimension(1, ++i, true);
                            fList.SetElement(1, i);
                            fList.Element.Value = f.Name;
                        }

                        Program.CurrentApp.ReturnValue.CopyFrom(fList);
                    }
                    else if (_ftpClient != null)
                    {
                        // Correct call for AsyncFtpClient
                        FtpListItem[] items = _ftpClient.GetListing(remotePath).Result;   // .Result to keep sync-style API
                        var list = new List<string>();

                        JAXObjects.Token fList = new();
                        int i = 0;
                        foreach (var f in items)
                        {
                            fList.SetDimension(1, ++i, true);
                            fList.SetElement(1, i);
                            fList.Element.Value = f.Name;
                        }

                        Program.CurrentApp.ReturnValue.CopyFrom(fList);
                    }
                }
                catch (Exception ex)
                {
                    SetString("lasterror", ex.Message);
                    OnError?.Invoke(ex.Message);
                    err = 8220;
                    string msg = "FTP List Directory Error: " + ex.Message;
                    SetStatus(13, msg);
                    _AddError(err, 0, $"{err}|{msg}|ListDirectory", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            return err;
        }

        public int DownloadFile(string remotePath, string localPath)
        {
            bool result = false;
            int err = 0;

            if (!GetBool("isconnected"))
            {
                err = 8210;
            }
            else
            {
                try
                {
                    if (_isSftp && _sftpClient != null)
                    {
                        using var fs = System.IO.File.Create(localPath);
                        _sftpClient.DownloadFile(remotePath, fs, p => OnProgress?.Invoke((long)p, 0));
                        OnFtpResponse?.Invoke($"Downloaded {remotePath}");
                        result = true;
                    }
                    else if (_ftpClient != null)
                    {
                        var progress = new Progress<FtpProgress>(p =>
                            OnProgress?.Invoke(p.TransferredBytes, 0));

                        FtpStatus status = _ftpClient.DownloadFile(localPath, remotePath,
                            FtpLocalExists.Overwrite, FtpVerify.None, progress).Result;

                        bool success = status == FtpStatus.Success;

                        if (success) OnFtpResponse?.Invoke($"Downloaded {remotePath}");
                        else SetString("lasterror", $"Download failed with status: {status}");

                        result = success;
                    }
                }
                catch (Exception ex)
                {
                    SetString("lasterror", ex.Message);
                    string msg = "FTP Download Error " + ex.Message;
                    SetStatus(13, msg);
                    OnError?.Invoke(ex.Message);
                    err = 8220;
                    _AddError(err, 0, $"{err}|{msg}|DownloadFile", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            Program.CurrentApp.ReturnValue.Element.Value = result;
            return err;
        }

        public int UploadFile(string localPath, string remotePath)
        {
            bool result = false;
            int err = 0;

            if (!GetBool("isconnected") || !System.IO.File.Exists(localPath))
                err = 8221;
            else
            {
                try
                {
                    if (_isSftp && _sftpClient != null)
                    {
                        using var fs = System.IO.File.OpenRead(localPath);
                        long fileSize = fs.Length;
                        _sftpClient.UploadFile(fs, remotePath, p => OnProgress?.Invoke((long)p * fileSize / 100, fileSize));
                        OnFtpResponse?.Invoke($"Uploaded {localPath}");
                        result = true;
                    }
                    else if (_ftpClient != null)
                    {
                        var progress = new Progress<FtpProgress>(p =>
                            OnProgress?.Invoke(p.TransferredBytes, 0));

                        FtpStatus status = _ftpClient.UploadFile(localPath, remotePath,
                            FtpRemoteExists.Overwrite, true, FtpVerify.None, progress).Result;

                        bool success = status == FtpStatus.Success;

                        if (success) OnFtpResponse?.Invoke($"Uploaded {localPath}");
                        else SetString("lasterror", $"Upload failed with status: {status}");

                        result = success;
                    }
                }
                catch (Exception ex)
                {

                    SetString("lasterror", ex.Message);
                    string msg = "FTP Upload Error " + ex.Message;
                    SetStatus(13, msg);
                    OnError?.Invoke(ex.Message);
                    err = 8220;   // Generic upload/download error
                    _AddError(err, 0, $"{err}|{msg}|UploadFile", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            Program.CurrentApp.ReturnValue.Element.Value = result;
            return err;
        }


        public int DeleteFile(string remotePath)
        {
            bool result = false;
            int err = 0;

            if (!GetBool("isconnected"))
                err = 8221;
            try
            {
                if (_isSftp && _sftpClient != null)
                {
                    _sftpClient.DeleteFile(remotePath);
                    result = true;
                }
                else if (_ftpClient != null)
                {
                    // Correct for methods that return plain Task
                    _ftpClient.DeleteFile(remotePath).Wait();

                    result = true;   // FluentFTP throws on real errors, so success = no exception
                }
            }
            catch (Exception ex)
            {
                SetString("lasterror", ex.Message);
                err = 8220;
                string msg = "FTP Delete Error " + ex.Message;
                SetStatus(13, msg);
                OnError?.Invoke(ex.Message);
                _AddError(err, 0, $"{err}|{msg}|DeleteFile", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
            }

            Program.CurrentApp.ReturnValue.Element.Value = result;
            return err;
        }


        public int CreateDirectory(string remotePath)
        {
            bool result = false;
            int err = 0;

            if (!GetBool("isconnected"))
                err = 8220;
            {
                try
                {
                    if (_isSftp && _sftpClient != null)
                    {
                        _sftpClient.CreateDirectory(remotePath);
                        result = true;
                    }
                    else if (_ftpClient != null)
                    {
                        // Correct: returns Task<bool> in current FluentFTP
                        bool success = _ftpClient.CreateDirectory(remotePath).Result;

                        if (!success)
                            throw new Exception("Create directory failed - check if it already exists or if the path is valid");

                        result = success;
                    }
                }
                catch (Exception ex)
                {
                    SetString("lasterror", ex.Message);
                    err = 8220;
                    string msg = "FTP Create Directory Error " + ex.Message;
                    SetStatus(13, msg);
                    OnError?.Invoke(ex.Message);
                    _AddError(err, 0, $"{err}|{msg}|CreateDirectory", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                }
            }

            Program.CurrentApp.ReturnValue.Element.Value = result;
            return err;
        }


        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ~XBase_Class_FTPClient() => Dispose();


        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower();
            string param1 = "";
            string param2 = "";

            if (Program.CurrentApp.ParameterClassList.Count == 1)
            {
                if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                    param1 = Program.CurrentApp.ParameterClassList[0].token.AsString();
            }
            else
            {
                if (JAXLib.InListC(methodName, "uploadfile", "downloadfile"))
                {
                    // These methods require 2 parameters, so if we only have 1 it's an error
                    if (Program.CurrentApp.ParameterClassList.Count == 2)
                    {
                        if (Program.CurrentApp.ParameterClassList[1].token.Element.Type.Equals("C"))
                            param2 = Program.CurrentApp.ParameterClassList[1].token.AsString();
                        else
                            result = 11;
                    }
                    else
                    {
                        param2 = JAXLib.JustFName(param1);

                        if (param2.Contains('*') || param2.Contains('?'))
                        {
                            result = 8215;   // Invalid filename for upload/download)
                            _AddError(result, 0, $"{result}|Wildcards not allowed in destination|{methodName}", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                        }
                    }
                }
                else if (Program.CurrentApp.ParameterClassList.Count > 1)
                    result = 98;
            }


            if (result == 0)
            {
                switch (methodName)
                {
                    case "connect":
                        result = Connect();
                        break;

                    case "disconnect":
                        result = Disconnect();
                        break;

                    case "listdirectory":
                        if (string.IsNullOrWhiteSpace(param1))
                            result = ListDirectory();
                        else
                            result = ListDirectory(param1);
                        break;

                    case "downloadfile":
                        if (string.IsNullOrWhiteSpace(param1))
                            result = 8217;
                        else
                            result = DownloadFile(param1, Path.GetFileName(param2));
                        break;

                    case "uploadfile":
                        if (string.IsNullOrWhiteSpace(param1))
                            result = 8217;
                        else
                            result = UploadFile(param1, Path.GetFileName(param2));
                        break;

                    case "deletefile":
                        if (string.IsNullOrWhiteSpace(param1))
                            result = 8217;
                        else
                            result = DeleteFile(param1);
                        break;

                    case "createdirectory":
                        if (string.IsNullOrWhiteSpace(param1))
                            result = 8217;
                        else
                            result = CreateDirectory(param1);
                        break;

                    default:
                        result = await base.DoDefault(methodName);
                        break;
                }
            }

            return result;
        }


        // ===================================================================
        // JAXBase Integration
        // ===================================================================
        public override string[] JAXMethods() =>
            ["addproperty", "command", "connect", "disconnect", "listdirectory",
             "downloadfile", "uploadfile", "deletefile", "createdirectory",
             "readexpression", "readmethod", "resettodefault", "saveasclass",
             "writeexpression", "writemethod"
            ];

        public override string[] JAXEvents() =>
            [
            "connected", "destroy", "disconnected", "error", "ftpresponse",
             "init", "load", "progress", "statuschanged"
            ];

        public override string[] JAXProperties()
        {
            return
                [
                "baseclass,C!,FtpClient",
                "class,C!,", "classlibrary,C$,",
                "host,c,",
                "isconnected,l!,false",
                "lasterror,c!,",
                "name,C,FtpClient",
                "parent,o$,", "parentclass,C$,", "port,n,21", "protocol,c,FTP", "password,c,", "privatekeypath,c,", "passphrase,c,", "passive,l,true",
                "tag,C,",
                "status,n!,0", "statusmessage,c!,",
                "username,c,anonymous", "usessl,l,false"
                ];
        }
    }
}