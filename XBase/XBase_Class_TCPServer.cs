/*
 * GROK created 2026-05-19
 * 
 * 2026-05-19 - JLW
 *      Updated to work with JAXBase.  Unfortunately GROK still hasn't quite figured 
 *      out how to properly handle the JAXObjectWrapper and related complexities.
 *      
 */
using JAXBase.Core;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_TCPServer : XBase_Avalonia, IDisposable
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _listenCts;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, XBase_Class_TCPClient> _activeClients = new();
        private bool _isListening = false;
        private bool _disposed = false;

        public bool AutoStart { get; set; } = false;
        public string BindAddress { get; set; } = "0.0.0.0";
        public int Port { get; set; } = 0;
        public int Backlog { get; set; } = 10;
        public int MaxConnections { get; set; } = 50;
        public bool UseSSL { get; set; } = false;
        public string CertificateThumbprint { get; set; } = "";
        public string CertificatePath { get; set; } = ""; // Optional PFX file path

        public int ActiveConnections => _activeClients.Count;

        public List<XBase_Class_TCPClient> Clients => [.. _activeClients.Values];

        public event Action<XBase_Class_TCPClient>? OnClientConnected;
        public event Action<XBase_Class_TCPClient>? OnClientDisconnected;
        public event Action<XBase_Class_TCPClient, string>? OnClientLineReceived;
        public event Action<XBase_Class_TCPClient, byte[]>? OnClientBinaryReceived;
        public event Action<XBase_Class_TCPClient, string>? OnClientError;
        public event Action? OnStarted;
        public event Action? OnStopped;
        public event Action<string>? OnError;

        public int historyMax = 100;
        public List<WebHistory> history = [];
        public new string MyBaseClass = "TCPServer";
        public new string MyDefaultName = "tcpserver";

        string LastError = "";

        public XBase_Class_TCPServer(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();

            // Default properties
            UserProperties["port"] = new JAXObjects.Token(0);
            UserProperties["bindaddress"] = new JAXObjects.Token("0.0.0.0");
            UserProperties["maxconnections"] = new JAXObjects.Token(50);
            UserProperties["backlog"] = new JAXObjects.Token(10);
            UserProperties["autostart"] = new JAXObjects.Token(false);
            UserProperties["usessl"] = new JAXObjects.Token(false);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);
            SetStatus(0, "Initialized");

            if (AutoStart && Port > 0)
                Start();

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
                    case "active":
                        returnToken.Element.Value = IsListening;
                        break;

                    case "activeconnections":
                        returnToken.Element.Value = ActiveConnections;
                        break;

                    case "clients":
                        // Returns count when idx=0, or client reference when idx > 0
                        if (idx == 0)
                            returnToken.Element.Value = ActiveConnections;
                        else if (idx > 0 && idx <= ActiveConnections)
                            returnToken.Element.Value = Clients[idx - 1];
                        else
                            returnToken.Element.MakeNull();
                        break;

                    case "class": returnToken.Element.Value = me.Class; break;

                    case "history":
                        // Same history logic as TCPClient
                        if (idx == 0)
                        {
                            StringBuilder sb = new();
                            for (int i = 0; i < history.Count; i++)
                                sb.AppendLine($"{history[i].DateVisited}|{history[i].URL}|{history[i].Status}");
                            returnToken.Element.Value = sb.ToString();
                        }
                        else
                        {
                            returnToken.Element.Value = idx <= history.Count ? history[idx - 1] : "";
                        }
                        break;

                    case "historymax": returnToken.Element.Value = historyMax; break;
                    case "maxconnections": returnToken.Element.Value = MaxConnections; break;
                    case "backlog": returnToken.Element.Value = Backlog; break;
                    case "port": returnToken.Element.Value = Port; break;
                    case "bindaddress": returnToken.Element.Value = BindAddress; break;
                    case "usessl": returnToken.Element.Value = UseSSL; break;
                    default:
                        result = 1;
                        break;
                }

                if (result == 0 && UserProperties.ContainsKey(propertyName))
                    returnToken.CopyFrom(UserProperties[propertyName]);
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
                switch (propertyName)
                {
                    case "port":
                        if (IsListening) result = 1541;
                        else if (tk.Element.Type.Equals("N"))
                            Port = tk.AsInt();
                        else 
                            result = 11;
                        break;

                    case "bindaddress":
                        if (IsListening) result = 1541;
                        else if (tk.Element.Type.Equals("C"))
                            BindAddress = tk.AsString();
                        else 
                            result = 11;
                        break;

                    case "maxconnections":
                        if (tk.Element.Type.Equals("N") && tk.AsInt() > 0)
                            MaxConnections = tk.AsInt();
                        else 
                            result = 11;
                        break;

                    case "backlog":
                        if (tk.Element.Type.Equals("N") && tk.AsInt() > 0)
                            Backlog = tk.AsInt();
                        else 
                            result = 11;
                        break;

                    case "autostart":
                        if (tk.Element.Type.Equals("L"))
                            AutoStart = tk.AsBool();
                        else 
                            result = 11;
                        break;

                    case "usessl":
                        if (tk.Element.Type.Equals("L"))
                            UseSSL = tk.AsBool();
                        else 
                            result = 11;
                        break;

                    default: result = 1; 
                        break;
                }

                if (result == 0)
                    UserProperties[propertyName].Element.Value = objValue;
            }
            else result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, string.Empty, Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                result = -1;
            }
            return result;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            switch (methodName.ToLower())
            {
                case "start":
                    Start();
                    break;

                case "stop":
                    Stop();
                    break;

                case "closeclient":
                    if (Program.CurrentApp.ParameterClassList.Count > 0)
                    {
                        var client = Program.CurrentApp.ParameterClassList[0].token.Element.Value as XBase_Class_TCPClient;
                        if (client != null) CloseClient(client);
                    }
                    else 
                        result = 11;
                    break;

                case "closeallclients":
                    CloseAllClients();
                    break;

                case "broadcastline":
                    if (Program.CurrentApp.ParameterClassList.Count > 0 &&
                        Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        BroadcastLine(Program.CurrentApp.ParameterClassList[0].token.AsString());
                    else 
                        result = 11;
                    break;

                case "broadcastbinary":
                    if (Program.CurrentApp.ParameterClassList.Count > 0)
                        BroadcastBinary(Program.CurrentApp.ParameterClassList[0].token.Element.Value);
                    else 
                        result = 11;
                    break;

                default:
                    await base.DoDefault(methodName);
                    break;
            }
            return result;
        }


        public virtual bool Start()
        {
            if (_isListening || _disposed) return false;

            lock (_lock)
            {
                try
                {
                    IPAddress address = string.IsNullOrEmpty(BindAddress) || BindAddress == "0.0.0.0"
                        ? IPAddress.Any
                        : IPAddress.Parse(BindAddress);

                    _listener = new TcpListener(address, Port);
                    _listener.Start(Backlog);

                    _listenCts = new CancellationTokenSource();
                    _isListening = true;

                    SetStatus(1, $"Server listening on {BindAddress}:{Port}");
                    OnStarted?.Invoke();
                    _CallMethod("started").Wait();

                    Task.Run(() => AcceptLoop(_listenCts.Token));
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = $"Start failed: {ex.Message}";
                    OnError?.Invoke(LastError);
                    SetStatus(13, LastError);
                    return false;
                }
            }
        }

        public virtual void Stop()
        {
            lock (_lock)
            {
                _listenCts?.Cancel();
                try { _listener?.Stop(); } catch { }
                _listener = null;
                _isListening = false;

                CloseAllClients();

                SetStatus(0, "Server stopped");
                OnStopped?.Invoke();
            }
        }

        private async Task AcceptLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);

                    if (_activeClients.Count >= MaxConnections)
                    {
                        tcpClient.Close();
                        SetStatus(12, "Connection rejected - MaxConnections reached");
                        continue;
                    }

                    // Wrap in XBase_TCPClient
                    JAXObjectWrapper tcpJOW = new JAXObjectWrapper(Program.CurrentApp, "tcp", "", []);
                    var client = tcpJOW.nvObject as XBase_Class_TCPClient;
                    tcpJOW.SetName($"client_{Guid.NewGuid():N}");

                    //var client = new XBase_Class_TCPClient(null, $"client_{Guid.NewGuid():N}");
                    client!._client = tcpClient;
                    client._networkStream = tcpClient.GetStream();
                    client._stream = client._networkStream;
                    client._reader = new StreamReader(client._stream, client.Encoding);
                    client._writer = new StreamWriter(client._stream, client.Encoding) { AutoFlush = true };
                    client._isConnected = true;

                    string clientKey = tcpClient.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
                    _activeClients[clientKey] = client;

                    SetStatus(2, $"Client connected: {clientKey}");
                    OnClientConnected?.Invoke(client);
                    tcpJOW.MethodCall("clientconnected").Wait();

                    // Start client's read loop
                    client._readCts = new CancellationTokenSource();
                    Task.Run(() => client.ReadLoop(client._readCts.Token)).Wait();
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) when (!_disposed)
                {
                    LastError = $"Accept error: {ex.Message}";
                    OnError?.Invoke(LastError);
                    SetStatus(13, LastError);
                }
            }
        }

        public virtual void CloseClient(XBase_Class_TCPClient client)
        {
            if (client == null) return;
            client.Disconnect();
            var entry = _activeClients.FirstOrDefault(x => x.Value == client);
            if (!string.IsNullOrEmpty(entry.Key))
                _activeClients.TryRemove(entry.Key, out _);

            OnClientDisconnected?.Invoke(client);
            SetStatus(3, $"Client disconnected: {entry.Key}");
        }

        public virtual void CloseAllClients()
        {
            foreach (var client in _activeClients.Values.ToList())
                CloseClient(client);
        }

        public virtual void BroadcastLine(string data)
        {
            foreach (var client in _activeClients.Values)
                client.SendLine(data);
        }

        public virtual void BroadcastBinary(object data)
        {
            foreach (var client in _activeClients.Values)
                client.SendBinary(data);
        }

        private void SetStatus(int statuscode, string message)
        {
            WebHistory wh = new()
            {
                URL = $"Server:{Port}",
                Status = statuscode,
                Content = message
            };
            history.Insert(0, wh);
            if (historyMax > 0 && history.Count > historyMax)
                history.RemoveAt(history.Count - 1);
        }

        public bool IsListening => _isListening && !_disposed && _listener != null;

        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            GC.SuppressFinalize(this);
        }

        ~XBase_Class_TCPServer() => Dispose();

        public override string[] JAXMethods() =>
            [
            "addproperty", "broadcastbinary", "broadcastline", "closeallclients", "closeclient", "command", 
            "readexpression", "readmethod", "resettodefault", "saveasclass", "start", "stop", 
            "writeexpression", "writemethod"
            ];

        public override string[] JAXEvents() =>
            [
            "clientconnected", "clientdisconnected", "clientlinereceived", "clientbinaryreceived", "clienterror", 
            "destroy", "error", "init", "load", "started", "stopped", "statuschanged"
            ];

        public override string[] JAXProperties() =>
            [
            "active,l!,false", "activeconnections,n!,0",
            "backlog,n,10", "bindaddress,c,0.0.0.0",
            "class,C!,TCPServer", "clients,n!,0",
            "history,c!,", "historymax,n,100",
            "maxconnections,n,50",
            "name,C,TCPServer", 
            "parent,o$","parentclass,C$,", "port,n,0", "usessl,l,false",
            "status,n!,0", "statusmessage,c!,",
            "tag,C,",
            ];
        
    }
}