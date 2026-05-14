/*
 * 2025.11.09 - JLW
 *      Took a shortcut and just asked Grok to create me a TCP client.  Had to do
 *      some back and forth before he got it where it didn't pop up errors everywhere.
 *      I'm hoping this is a close-enough implementation so I won't need to spend
 *      a lot of time rewriting it.
 *      
 * 2026.05.13 - JLW
 *      Finally getting around to tying it into the system.  Hope to get it working
 *      in the next couple of days.
 * 
 * 
 * 
 * 
 * GROK Instructions
 * ------------------------------------
 * Use
 * o = CREATEOBJECT("HttpClient")
 * 
 * * UNSECURED HTTP (port 80) — works perfectly
 * ? o.Get("http://httpbin.org/get")
 * 
 * * SECURE HTTPS (port 443) — full certificate validation
 * ? o.Get("https://httpbin.org/get")
 * 
 * * MIXED — no problem
 * o.BaseUrl = "https://api.example.com"
 * ? o.Post("login", "user=fox&pass=pro")  && HTTPS
 * ? o.Get("http://legacy.local/data")     && HTTP (same object!)
 * 
 * 
 * 
 * Secure Usage
 * o = CREATEOBJECT("HttpClient")
 * 
 * * Trust only Let's Encrypt
 * o.TrustedRootCerts.Add( FILETOSTR("letsencrypt-r3.pem") )  && PEM → X509Certificate2
 * 
 * * Or pin GitHub's cert
 * o.PinnedThumbprint = "5C 3B 7F 2D 3A 5E 8B..."  && SHA-1 or SHA-256
 * 
 * * Or dev mode
 * o.ValidateServerCertificate = .F.
 * 
 * ? o.Get("https://api.github.com")
 * 
 * 
 * 
 * You control security per-request or globally
 * 
 * o = CREATEOBJECT("HttpClient")
 * o.Timeout = 30000
 * 
 * * Talk to modern API
 * o.BaseUrl = "https://api.stripe.com/v1"
 * o.AddHeader("Authorization", "Bearer sk_live_...")
 * ? o.Post("charges", "amount=999&currency=usd&source=tok_visa")
 * 
 * * Then hit internal unsecured server
 * ? o.Get("http://192.168.1.50/status")
 * 
 * * Then secure again
 * o.PinnedThumbprint = "A1B2C3D4E5..."  && lock to company cert
 * ? o.Get("https://intranet.corp.local/secret")
 * 
 * 
 * FEATURE LIST
 * --------------------------------------------------
 * Feature                  Supported?      Notes
 * http:// (unencrypted)    Yes             Full speed, no TLS
 * https:// (encrypted)     Yes             TLS 1.2/1.3, full validation
 * Mixed in same session    Yes             Same object, no reconnect issues
 * Disable validation (dev) Yes             .ValidateServerCertificate = .F.
 * Certificate pinning      Yes             Per-object or per-request
 * Custom CA trusts         Yes             Load .pem or .crt files
 * 
 * Handles both HTTP and HTTPS correctly
 * Never blindly trusts certificates
 * Supports enterprise security (pinning, custom CAs)
 * Still feels like 1999 VFP code
 * 
 */
using JAXBase.Core;
using JAXBase.Utilities;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_TCPClient : XBase_Avalonia, IDisposable
    {
        public TcpClient? _client;
        public NetworkStream? _networkStream;
        public Stream? _stream;
        public StreamReader? _reader;
        public StreamWriter? _writer;
        private readonly object _lock = new();
        private bool _disposed = false;
        private bool _isConnected = false;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string LastError { get; set; } = "";
        public bool AutoReconnect { get; set; } = true;

        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action<string>? OnLineReceived;
        public event Action<string>? OnError;
        public event Action<string>? OnWarning;

        public int historyMax = 100;
        public List<WebHistory> history = [];
        public string hostAddress = "";

        public new string MyBaseClass = "TCPClient";
        public new string MyDefaultName = "tcpclient";

        public bool UseSSL = false;
        public int Port = 0;

        //public TcpClient MyConnection => (TcpClient)me.nvObject!;

        public XBase_Class_TCPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------
            bool result = await base.PostInit(callBack, parameterList);

            SetStatus(0,"Initialized");

            return result;
        }

        /*------------------------------------------------------------------------------------------*
         * 
         * Non visual classes will typically call here to get the value of the 
         * property from the UserProperties dictionary.
         * 
         * Return INT result
         *      0   - Successfully proccessed
         *      1   - Just saved to UserProperties
         *      2   - Requires special handling, did not process
         *      >10 - Error code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName.ToLower())
                {
                    case "active":
                        returnToken.Element.Value = false;
                        break;

                    case "available":
                        returnToken.Element.Value = false;
                        break;

                    case "history":
                        if (idx == 0)
                        {
                            // Get the entire history as a string
                            StringBuilder sb = new();
                            for (int i = 0; i < history.Count; i++)
                                sb.AppendLine(history[i].DateVisited.ToString()+"|"+history[i].URL+"|"+history[i].Status.ToString());

                            returnToken.Element.Value = sb.ToString();
                        }
                        else
                        {
                            // Get one entry of the history
                            if (idx < 1)
                                result = 31;
                            else if (idx > history.Count)
                                returnToken.Element.Value = "";
                            else
                                returnToken.Element.Value = history[idx - 1];
                        }
                        break;

                    case "historymax":
                        returnToken.Element.Value = historyMax;
                        break;

                    case "isconnected":
                        returnToken.Element.Value = IsConnected;
                        break;

                    case "status":
                        break;

                    // Intercept special handling of properties
                    default:
                        // Process standard properties
                        result = 1;
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
                    AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                returnToken.Element.MakeNull();
            }
            else
                result = 0;

            return returnToken;
        }


        /*------------------------------------------------------------------------------------------*
         * Handle the commmon properties by calling the base and then
         * handle the special cases.
         * 
         * Return result from XBase_Visual_Class
         *      0   - Successfully proccessed
         *      1   - Did not process
         *      2   - Requires special processing
         *      >10 - Error code
         * 
         * 
         * Return from here
         *      0   - Successfully processed
         *     -1   - Error Code
         *      
         *------------------------------------------------------------------------------------------*/
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();

            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else
            {
                if (UserProperties.ContainsKey(propertyName))
                {
                    // Intercept special handling of properties
                    switch (propertyName)
                    {
                        case "historymax":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                    historyMax = tk.AsInt();
                            }
                            else
                                result = 11;
                            break;

                        case "host":
                            if (tk.Element.Type.Equals("C") == false)
                                result = 11;
                            else
                                result = 1;
                            break;

                        case "receivebuffersize":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 128)
                                    result = 41;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "recievetimeout":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "secure":
                            if (tk.Element.Type.Equals("L") == false)
                                result = 11;
                            else
                                result = 1;
                            break;

                        case "sendbuffersize":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 128)
                                    result = 41;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "sendtimeout":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (tk.AsInt() < 0)
                                    result = 41;
                                else
                                    result = 1;
                            }
                            else
                                result = 11;
                            break;

                        case "server":
                            if (IsConnected)
                                result = 1541;
                            else if (tk.Element.Type.Equals("L") == false)
                                result = 11;
                            else
                                result = 1;
                            break;

                        case "port":
                            if (tk.Element.Type.Equals("N"))
                            {
                                if (JAXLib.Between(tk.AsInt(), 0, 65535))
                                {
                                    objValue = tk.AsInt();
                                    result = 1;
                                }
                                else
                                    result = 41;
                            }
                            else
                                result = 11;
                            break;

                        default:
                            // Just tell it to update the standard property
                            result = 1;
                            break;
                    }

                    // Do we need to process this property?
                    if (JAXLib.Between(result, 1, 10))
                    {
                        // First, we check to make sure that the property exists
                        if (result < 9)
                        {
                            if (UserProperties.ContainsKey(propertyName))
                                UserProperties[propertyName].Element.Value = objValue;
                        }

                        result = 0;
                    }
                }
                else
                    result = 1559;

                // Deal with errors
                if (result > 10)
                {
                    _AddError(result, 0, string.Empty, App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);

                    if (string.IsNullOrWhiteSpace(App.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                        AppErrorHandling.SetError(result, $"{result}|", string.Empty);

                    result = -1;
                }
                else
                    result = 0;
            }

            return result;
        }


        /*
         * This is where the default actions for the methods occur
         */
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            string errMsg = string.Empty;

            switch (methodName.ToLower())
            {
                case "close":
                    break;

                case "command":
                    break;

                case "connect":
                    if (IsConnected)
                        result = 1541;
                    else
                        Connect();
                    break;

                case "connected":
                    break;

                case "disconnect":
                    break;

                case "disconnected":
                    break;

                case "get":
                    if (UserProperties["server"].AsBool())
                    {

                    }
                    else
                    {
                        if (_client is not null)
                        {
                            using NetworkStream stream = _client.GetStream();

                            string request =
                                $"GET / HTTP/1.1\r\n" +
                                $"Host: {hostAddress}\r\n" +
                                "Connection: close\r\n" +   // Important: tells server to close after response
                                "Accept: text/html\r\n" +
                                "\r\n";

                            byte[] requestBytes = Encoding.ASCII.GetBytes(request);

                            // Send the request
                            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                            await stream.FlushAsync();
                            SetStatus(2, "HTTP GET / request sent.");

                            // Optional: Read the response
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            StringBuilder responseBuilder = new StringBuilder();

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                responseBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                            string response = responseBuilder.ToString();
                            SetStatus(1, $"Received: {response.Length}");
                        }
                    }
                    break;

                case "post":
                    break;

                case "statuschanged":
                    break;

                default:
                    // Try base code
                    await base.DoDefault(methodName);
                    break;
            }

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
                    _ => string.Empty,
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

        /*
         * The history stack is FIFO
         * 
         * Make sure history stack does not overrun the max lengtyh
         * and then push the message onto the top of the stack
         * 
         * Status codes
         * ----------------------------------------
         * 1 - Connected
         * 2 - Line read
         * 3 - Line sent
         * 
         * 11 - try again
         * 13 - error
         * 32 - pipe broken
         * 98 - address already in use
         * 101 - network is unreachable
         * 104 - connection reset by peer
         * 110 - connection timeout
         * 111 - connection refused
         * 113 - no route to host
         * 
         */
        private void SetStatus(int statuscode, string message)
        {
            WebHistory webHistory = new()
            {
                URL = hostAddress,
                Status = statuscode,
                Content = message
            };

            history.Insert(0, webHistory);
            AppIO.DebugLog($"URL: {hostAddress} -> Status: {statuscode} - {message}");

            // Lose anything that is beyond the max
            if (historyMax > 0)
            {
                while (history.Count > historyMax)
                    history.RemoveAt(history.Count - 1);
            }
        }


        /*
         * 2026-05-13 - JLW
         *      Modified GROK code
         *      
         */
        public bool IsConnected => _isConnected && !_disposed && _client?.Connected == true;

        public virtual bool Connect()
        {
            hostAddress = UserProperties["host"].AsString();
            Port = UserProperties["port"].AsInt();

            if (_disposed)
                throw new ObjectDisposedException(nameof(XBase_Class_TCPClient));

            lock (_lock)
            {
                try
                {
                    DisconnectInternal();
                    _client = new TcpClient();
                    var task = _client.ConnectAsync(hostAddress, Port);

                    if (!task.Wait(Timeout))
                    {
                        LastError = "Connection timeout";
                        // TODO - register the error
                        OnError?.Invoke(LastError);
                        SetStatus(13, LastError);
                        TryAutoReconnect(hostAddress, Port);
                        return false;
                    }

                    _networkStream = _client.GetStream();
                    _stream = _networkStream;
                    _reader = new StreamReader(_stream, Encoding);
                    _writer = new StreamWriter(_stream, Encoding) { AutoFlush = true };
                    _isConnected = true;

                    _CallMethod("connected").Wait();
                    OnConnected?.Invoke();
                    SetStatus(1, "connected");

                    Task.Run(ReadLoop);
                    return true;
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                {
                    LastError = $"Connect failed: {ex.Message}";
                    // TODO - register the error
                    OnError?.Invoke(LastError);
                    SetStatus(13, LastError);
                    TryAutoReconnect(hostAddress, Port);
                    return false;
                }
                catch (Exception ex)
                {
                    LastError = $"Connect failed: {ex.Message}";
                    // TODO - register the error
                    OnError?.Invoke(LastError);
                    SetStatus(13, LastError);
                    TryAutoReconnect(hostAddress, Port);
                    return false;
                }
            }
        }

        public virtual void ReadLoop()
        {
            try
            {
                while (!_disposed && _reader != null)
                {
                    var line = _reader.ReadLine();
                    if (line == null) 
                        break;

                    // TODO - call the recieved event
                    SetStatus(2, line);
                    //OnLineReceived?.Invoke(line);
                }
            }
            catch (Exception ex) when (!_disposed)
            {
                string LastError = "Read error: " + ex.Message;
                OnError?.Invoke(LastError);

                SetStatus(13, LastError);
                TryAutoReconnect();
            }
            finally
            {
                if (!_disposed)
                {
                    Disconnect();
                    SetStatus(0, "Disconnected");
                }
            }
        }

        public virtual void TryAutoReconnect(string? host = null, int port = 0)
        {
            if (!AutoReconnect || _disposed) return;
            OnWarning?.Invoke("Auto-reconnecting in 2s...");
            Task.Delay(2000).ContinueWith(_ =>
            {
                if (string.IsNullOrEmpty(hostAddress)) Connect();
            });
        }

        public virtual bool SendLine(string data)
        {
            if (!IsConnected) return false;
            try
            {
                lock (_lock)
                {
                    _writer?.WriteLine(data);
                    SetStatus(3, data);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LastError = "Send failed: " + ex.Message;
                OnError?.Invoke(LastError);
                SetStatus(13, LastError);
                TryAutoReconnect();
                return false;
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
            try { _writer?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            try { _networkStream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _client?.Dispose(); } catch { }
            _client = null;
        }

        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ~XBase_Class_TCPClient() => Dispose();

        public override string[] JAXMethods()
        {
            return [
                "addproperty",
                "close", "command","connect",
                "disconnect",
                "get",
                "open",
                "post",
                "readexpression", "readmethod", "resettodefault",
                "saveasclass",
                "writeexpression", "writemethod"];
        }

        public override string[] JAXEvents()
        {
            return ["connected", "destroy", "disconnected", "error", "init", "load", "statuschanged"];
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
         *          ! Protected - can't change after initialization
         *          $ Special Handling - do not auto process
         */
        public override string[] JAXProperties()
        {
            return [
                "active,l!,false", "available,l!,false", "authentication,n,0", $"appname,C,{App.AppLevels[0].PrgName}",
                "baseclass,C!,SQL",
                "class,C!,", "classlibrary,C$,", "comment,C,",
                "encryption,L,.F.",
                "history,c!,", "historymax,n,100",
                "isconnected,L!,.F.", "IP,c,",
                "host,c,",
                "name,C,SQL",
                "parent,o$,","parentclass,C$,","port,n,80",
                "receivebuffersize,n,1024", "receivetimeout,n,10000",
                "secure,l,false", "sendbuffersize,n,1024", "sendtimeout,n,10000", "server,L,false", "status,n!,0", "statusmessage,c!,",
                "trustservercertificate,L,false", "tag,C,"
                ];
        }
    }
}
