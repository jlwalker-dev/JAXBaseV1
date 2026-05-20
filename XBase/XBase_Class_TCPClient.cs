/*
 * 2025.11.09 - JLW
 *      Took a shortcut and just asked Grok to create me a TCP client.  Had to do
 *      some back and forth before he got it where it didn't pop up errors everywhere.
 *      I'm hoping this is a close-enough implementation so I won't need to spend
 *      a lot of time rewriting it.
 *      
 * 2026.05.13 - JLW
 *      Finally getting around to tying it into the system.  Got it working pretty quickly, but 
 *      I can already see some areas for improvement.  I'm going to circle back with GROK to
 *      see if we can improve upon the class.  After all, 6 months is a long time in the software
 *      industry and I can only image it's equivalent to serveral lifetimes in AI terms.
 * 
 * 
 * 2026.05.14 - GROK
 *   Added the following 
 *   - Full secure (TLS) + non-secure support with SslStream
 *   - TCP Keep-Alive configuration (cross-platform)
 *   - Improved TLS certificate validation (default strict, disable option, basic pinning)
 *   - Consistent behavior on Windows and Linux
 *   - Maintains full JAXBase / VFP-style class integration
 *   
 * 2026.05.17 - JLW
 *   Fixed up some issues and had GROK give me a hand with
 *   - Added binary send/receive support (sendbinary, readbinary, OnBinaryReceived)
 *   - BinaryMode property for mixed or pure binary usage
 *   - OnBinaryReceived fires in addition to line events
 *   - Full secure/non-secure, TCP Keep-Alive, TLS validation unchanged
 *   - Thread-safe LinesWaiting (prevents concurrent access)
 *   
 *   
 *
 *
 *  --------------------------------------------------------
 *  SetStatus codes and meaning
 *  --------------------------------------------------------
 *      0 - Disconnected / Idle
 *      1 - Connected
 *      2 - Line received
 *      3 - Line sent
 *      4 - Binary data received
 *      5 - Binary data sent
 *      12 - End of stream / Remote closed connection
 *      13 - Error (check status message for details)
 *
 */

using JAXBase.Core;
using JAXBase.Utilities;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_TCPClient : XBase_Avalonia, IDisposable
    {
        public TcpClient? _client;
        public NetworkStream? _networkStream;
        public Stream? _stream;
        public SslStream? _sslStream;
        public StreamReader? _reader;
        public StreamWriter? _writer;

        private readonly object _lock = new();           // General connection lock
        private readonly object _linesLock = new();      // Specific lock for LinesWaiting
        private bool _disposed = false;

        public CancellationTokenSource? _readCts;
        public bool _isConnected = false;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string LastError { get; set; } = "";
        public bool AutoReconnect { get; set; } = true;

        // TCP Keep-Alive
        public bool TcpKeepAlive { get; set; } = true;
        public int TcpKeepAliveTime { get; set; } = 120;
        public int TcpKeepAliveInterval { get; set; } = 30;
        public int TcpKeepAliveRetryCount { get; set; } = 5;

        // TLS
        public bool ValidateServerCertificate { get; set; } = true;
        public string PinnedThumbprint { get; set; } = "";

        // Binary support
        public bool BinaryMode { get; set; } = false;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<string>? OnLineReceived;
        public event Action<byte[]>? OnBinaryReceived;
        public event Action<string>? OnError;
        public event Action<string>? OnWarning;

        public int historyMax = 100;
        public List<WebHistory> history = [];
        public string hostAddress = "";

        public new string MyBaseClass = "TCPClient";
        public new string MyDefaultName = "tcpclient";

        private string _linesWaiting = "";   // Backing field

        public XBase_Class_TCPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);
            SetStatus(0, "Initialized");
            return result;
        }

        // ===================================================================
        // Property Handling
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
                    case "active": returnToken.Element.Value = IsConnected; break;

                    case "byteswaiting":
                        if (IsConnected && _stream != null)
                            returnToken.Element.Value = _stream.CanRead ? (_stream is NetworkStream ns ? ns.DataAvailable ? ns.Socket.Available : 0 : 0) : 0;
                        else
                            returnToken.Element.Value = 0;
                        break;

                    case "class": returnToken.Element.Value = me.Class; break;
                    case "classlibrary": returnToken.Element.Value = ""; break;

                    case "history":
                        if (idx == 0)
                        {
                            StringBuilder sb = new();
                            for (int i = 0; i < history.Count; i++)
                                sb.AppendLine(history[i].DateVisited.ToString() + "|" + history[i].URL + "|" + history[i].Status.ToString());
                            returnToken.Element.Value = sb.ToString();
                        }
                        else
                        {
                            if (idx < 1) result = 31;
                            else if (idx > history.Count) returnToken.Element.Value = "";
                            else returnToken.Element.Value = history[idx - 1];
                        }
                        break;

                    case "historymax": returnToken.Element.Value = historyMax; break;

                    case "lineswaiting":
                        lock (_linesLock)
                        {
                            string[] testLines = _linesWaiting.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
                            returnToken.Element.Value = testLines.Length;

                            AppIO.DebugLog($"GetProperty 'lineswaiting': {_linesWaiting} (Count: {testLines.Length})");
                        }
                        break;

                    case "tcpkeepalive": returnToken.Element.Value = TcpKeepAlive; break;
                    case "tcpkeepalivetime": returnToken.Element.Value = TcpKeepAliveTime; break;
                    case "tcpkeepaliveinterval": returnToken.Element.Value = TcpKeepAliveInterval; break;
                    case "tcpkeepaliveretrycount": returnToken.Element.Value = TcpKeepAliveRetryCount; break;
                    case "validateservercertificate": returnToken.Element.Value = ValidateServerCertificate; break;
                    case "pinnedthumbprint": returnToken.Element.Value = PinnedThumbprint; break;
                    case "binarymode": returnToken.Element.Value = BinaryMode; break;

                    case "parent":
                        if (me.parent is null) returnToken.Element.MakeNull();
                        else returnToken.Element.Value = me.parent;
                        break;

                    case "parentclass":
                        returnToken.Element.Value = me.parent?.Class ?? "";
                        break;


                    default: result = 1; break;
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
                _AddError(result, 0, $"{result}|{propertyName}|", App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{propertyName}|", string.Empty);
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
                switch (propertyName)
                {
                    case "tcpkeepalive":
                        if (tk.Element.Type.Equals("L"))
                            TcpKeepAlive = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "tcpkeepalivetime":
                        if (tk.Element.Type.Equals("N"))
                            if (tk.AsInt() > 0)
                                TcpKeepAliveTime = tk.AsInt();
                            else
                                result = 41;
                        else
                            result = 11;
                        break;

                    case "tcpkeepaliveinterval":
                        if (tk.Element.Type.Equals("L"))
                            if (tk.AsInt() > 0)
                                TcpKeepAliveInterval = tk.AsInt();
                            else
                                result = 41;
                        else
                            result = 11;
                        break;

                    case "tcpkeepaliveretrycount":
                        if (tk.Element.Type.Equals("L"))
                            if (tk.AsInt() > 0)
                                TcpKeepAliveRetryCount = tk.AsInt();
                            else
                                result = 41;
                        else
                            result = 11;
                        break;

                    case "validateservercertificate":
                        if (tk.Element.Type.Equals("L"))
                            ValidateServerCertificate = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "pinnedthumbprint":
                        if (tk.Element.Type.Equals("C"))
                            PinnedThumbprint = tk.AsString();
                        else
                            result = 11;
                        break;

                    case "binarymode":
                        if (IsConnected)
                            result = 1541;
                        else if (tk.Element.Type.Equals("L"))
                            BinaryMode = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "host":
                        if (IsConnected)
                            result = 1541;
                        else if (tk.Element.Type.Equals("C"))
                            UserProperties[propertyName].Element.Value = tk.AsString();
                        else
                            result = 11;
                        break;

                    case "port":
                        if (IsConnected)
                            result = 1541;
                        else if (tk.Element.Type.Equals("N"))
                            if (JAXLib.Between(tk.AsInt(), 0, 65535))
                                UserProperties[propertyName].Element.Value = tk.AsInt();
                            else result = 41;
                        else
                            result = 11;
                        break;
                    default: result = 1; break;
                }

                if (result == 0 && UserProperties.ContainsKey(propertyName))
                    UserProperties[propertyName].Element.Value = objValue;
            }
            else result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                result = -1;
            }
            return result;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            switch (methodName.ToLower())
            {
                case "connect":
                    if (IsConnected)
                        result = 1541;
                    else 
                        await Task.Run(Connect);
                    break;

                case "disconnect":
                    Disconnect();
                    break;

                case "readbytes":
                    if (Program.CurrentApp.ParameterClassList.Count > 0 &&
                        Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("N"))
                    {
                        byte[] data = ReadBinary(Program.CurrentApp.ParameterClassList[0].token.AsInt());
                        Program.CurrentApp.ReturnValue.Element.Value = data;
                    }
                    else 
                        result = 11;
                    break;

                case "readline":
                    if (IsConnected)
                    {
                        lock (_linesLock)
                        {
                            if (!string.IsNullOrEmpty(_linesWaiting))
                            {
                                // If there are no lines waiting, return empty string
                                Program.CurrentApp.ReturnValue.Element.Value = "";
                            }
                            else
                            {
                                // Extract first line and update _linesWaiting
                                int newlineIndex = _linesWaiting.IndexOf("\r\n", StringComparison.Ordinal);

                                if (newlineIndex >= 0)
                                {
                                    Program.CurrentApp.ReturnValue.Element.Value = _linesWaiting.Substring(0, newlineIndex);
                                    _linesWaiting = _linesWaiting.Substring(newlineIndex + 2); // +2 to skip \r\n
                                }
                                else
                                {
                                    // Only one line waiting without newline, return it and clear _linesWaiting
                                    Program.CurrentApp.ReturnValue.Element.Value = _linesWaiting;
                                    _linesWaiting = string.Empty;
                                }
                            }

                        }
                    }
                    else 
                        result = 1541;
                    break;

                case "sendline":
                    if (Program.CurrentApp.ParameterClassList.Count > 0 &&
                        Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        SendLine(Program.CurrentApp.ParameterClassList[0].token.AsString());
                    else 
                        result = 11;
                    break;

                case "sendbinary":
                    if (Program.CurrentApp.ParameterClassList.Count > 0)
                    {
                        var p = Program.CurrentApp.ParameterClassList[0].token;
                        if (p.Element.Type.Equals("C"))
                            SendBinary(p.AsString());
                        else if (p.Element.Value is byte[] bytes)
                            SendBinary(bytes);
                        else 
                            result = 11;
                    }
                    else 
                        result = 1558;
                    break;

                default:
                    await base.DoDefault(methodName);
                    break;
            }
            return result;
        }

        public bool IsConnected => _isConnected && !_disposed && _client?.Connected == true;

        public virtual bool Connect()
        {
            // ... (unchanged from your version - kept for brevity) ...
            // (full Connect method is identical to what you posted)
            hostAddress = UserProperties["host"].AsString();
            int port = UserProperties["port"].AsInt();
            bool useSecure = UserProperties["secure"].AsBool();

            if (_disposed) throw new ObjectDisposedException(nameof(XBase_Class_TCPClient));
            if (string.IsNullOrWhiteSpace(hostAddress) || port <= 0) return false;

            lock (_lock)
            {
                try
                {
                    DisconnectInternal();
                    _client = new TcpClient();
                    ApplyTcpKeepAlive();

                    var connectTask = _client.ConnectAsync(hostAddress, port);
                    if (!connectTask.Wait(Timeout))
                    {
                        LastError = "Connection timeout";
                        OnError?.Invoke(LastError);
                        SetStatus(13, LastError);
                        TryAutoReconnect();
                        return false;
                    }

                    _networkStream = _client.GetStream();

                    if (useSecure)
                    {
                        _sslStream = new SslStream(_networkStream, false, ValidateServerCertificateCallback);
                        _sslStream.AuthenticateAsClientAsync(hostAddress).Wait(Timeout);
                        _stream = _sslStream;
                    }
                    else
                    {
                        _stream = _networkStream;
                    }

                    _reader = new StreamReader(_stream, Encoding);
                    _writer = new StreamWriter(_stream, Encoding) { AutoFlush = true };

                    _readCts = new CancellationTokenSource();
                    _isConnected = true;

                    _CallMethod("connected").Wait();
                    OnConnected?.Invoke();
                    SetStatus(1, "connected");

                    Task.Run(() => ReadLoop(_readCts.Token));
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = $"Connect failed: {ex.Message}";
                    OnError?.Invoke(LastError);
                    SetStatus(13, LastError);
                    TryAutoReconnect();
                    return false;
                }
            }
        }

        // ... ValidateServerCertificateCallback, ApplyTcpKeepAlive unchanged ...

        public async Task ReadLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (!_disposed && !cancellationToken.IsCancellationRequested && _stream != null)
                {
                    if (BinaryMode)
                    {
                        if (_stream.CanRead && _stream is NetworkStream ns && ns.DataAvailable)
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead > 0)
                            {
                                byte[] data = new byte[bytesRead];
                                Array.Copy(buffer, data, bytesRead);
                                SetStatus(4, $"Binary received: {bytesRead} bytes");   // Updated status code
                                OnBinaryReceived?.Invoke(data);
                            }
                        }
                        await Task.Delay(10, cancellationToken);
                        continue;
                    }

                    // Line mode
                    string? line = null;
                    try
                    {
                        line = await _reader!.ReadLineAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        SetStatus(12, "Read loop cancelled (normal disconnect)");
                        return;
                    }
                    catch (IOException ex) when (ex.InnerException is SocketException se &&
                        (se.SocketErrorCode == SocketError.ConnectionReset || se.SocketErrorCode == SocketError.ConnectionAborted))
                    {
                        SetStatus(12, "Remote server closed connection gracefully.");
                        return;
                    }
                    catch (Exception ex) when (!_disposed)
                    {
                        LastError = $"Read error: {ex.Message}";
                        OnError?.Invoke(LastError);
                        SetStatus(13, LastError);
                        return;
                    }

                    if (line == null)
                    {
                        SetStatus(12, "End of stream reached");
                        return;
                    }

                    SetStatus(2, line);

                    if (OnLineReceived is null)
                    {
                        lock (_linesLock)
                        {
                            _linesWaiting += line + Environment.NewLine;
                        }
                    }
                    else
                    {
                        lock (_linesLock)
                        {
                            _linesWaiting = "";
                        }
                        OnLineReceived?.Invoke(line);
                    }
                }
            }
            finally
            {
                if (!_disposed)
                    Disconnect();
            }
        }

        public virtual bool SendLine(string data)
        {
            if (!IsConnected) return false;
            try
            {
                lock (_lock)
                {
                    _writer?.WriteLine(data);
                    _writer?.Flush();
                    SetStatus(3, data);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = $"Send failed: {ex.Message}";
                OnError?.Invoke(LastError);
                SetStatus(13, LastError);
                TryAutoReconnect();
                return false;
            }
        }

        public virtual bool SendBinary(object data)
        {
            if (!IsConnected || _stream == null) return false;
            try
            {
                lock (_lock)
                {
                    byte[] bytes = data switch
                    {
                        byte[] b => b,
                        string s => Encoding.GetBytes(s),
                        _ => Array.Empty<byte>()
                    };

                    if (bytes.Length == 0) return false;

                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                    SetStatus(5, $"Binary sent: {bytes.Length} bytes");   // Updated status code
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = $"Binary send failed: {ex.Message}";
                OnError?.Invoke(LastError);
                SetStatus(13, LastError);
                TryAutoReconnect();
                return false;
            }
        }

        public virtual byte[] ReadBinary(int count)
        {
            if (!IsConnected || _stream == null || count <= 0) return [];
            try
            {
                byte[] buffer = new byte[count];
                int bytesRead = _stream.Read(buffer, 0, count);
                if (bytesRead == 0) return [];
                if (bytesRead < count)
                {
                    byte[] result = new byte[bytesRead];
                    Array.Copy(buffer, result, bytesRead);
                    return result;
                }
                return buffer;
            }
            catch (Exception ex)
            {
                LastError = $"Binary read failed: {ex.Message}";
                OnError?.Invoke(LastError);
                SetStatus(13, LastError);
                return [];
            }
        }

        public virtual void Disconnect()
        {
            lock (_lock) DisconnectInternal();
            _isConnected = false;
            SetStatus(0, "Disconnected");
            OnDisconnected?.Invoke();
        }

        private void DisconnectInternal()
        {
            _readCts?.Cancel();

            try { _writer?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _sslStream?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            try { _networkStream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }

            _client = null;
            _sslStream = null;
            _stream = null;
            _networkStream = null;
            _readCts?.Dispose();
            _readCts = null;
        }

        public virtual void TryAutoReconnect(string? host = null, int port = 0)
        {
            if (!AutoReconnect || _disposed) return;
            OnWarning?.Invoke("Auto-reconnecting in 2s...");
            Task.Delay(2000).ContinueWith(_ => Connect());
        }

        private bool ValidateServerCertificateCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            if (!ValidateServerCertificate) return true;
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            if (!string.IsNullOrEmpty(PinnedThumbprint) && certificate != null)
            {
                string thumb = certificate.GetCertHashString();
                if (thumb.Equals(PinnedThumbprint, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            OnError?.Invoke($"TLS validation failed: {sslPolicyErrors}");
            return false;
        }

        private void ApplyTcpKeepAlive()
        {
            if (_client?.Client == null || !TcpKeepAlive) return;
            try
            {
                var socket = _client.Client;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, TcpKeepAliveTime);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, TcpKeepAliveInterval);
            }
            catch { }
        }

        private void SetStatus(int statuscode, string message)
        {
            WebHistory webHistory = new()
            {
                URL = hostAddress,
                Status = statuscode,
                Content = message
            };

            AppIO.DebugLog($"Status: {statuscode} - {message}");
            history.Insert(0, webHistory);
            if (historyMax > 0 && history.Count > historyMax)
                history.RemoveAt(history.Count - 1);
        }

        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ~XBase_Class_TCPClient() => Dispose();

        public override string[] JAXMethods() =>
            ["addproperty", "command", "connect", "disconnect",
             "readexpression", "readbytes", "readline", "readmethod", "resettodefault", "saveasclass", "sendline", "sendbinary",
             "writeexpression", "writemethod"];

        public override string[] JAXEvents() =>
            ["bytesreceived", "connected", "destroy", "disconnected", "error", "init", "linereceived", "load", "statuschanged"];

        public override string[] JAXProperties()
        {
            return [
                "active,l!,false", "available,l!,false",
                "baseclass,C!,TCPClient", "byteswaiting,n!,0",
                "class,C!,", "classlibrary,C$,",
                "history,c!,", "historymax,n,100", "host,c,",
                "lineswaiting,n!,0",
                "name,C,SQL",
                "parent,o$,","parentclass,C$,","pinnedthumbprint,c,","port,n,80",
                "secure,l,false",
                "tcpkeepalive,l,true", "tcpkeepalivetime,n,120", "tcpkeepaliveinterval,n,30", "tcpkeepaliveretrycount,n,5",
                "validateservercertificate,l,true", "pinnedthumbprint,c,",
                "binarymode,l,false",
                "receivebuffersize,n,1024", "receivetimeout,n,10000",
                "sendbuffersize,n,1024", "sendtimeout,n,10000",
                "tcpkeepalive,l,true", "tcpkeepalivetime,n,120", "tcpkeepaliveinterval,n,30", "tcpkeepaliveretrycount,n,5",
                "status,n!,0", "statusmessage,c!,",
                "tag,C,",
                "validateservercertificate,l,true",
            ];
        }
    }
}