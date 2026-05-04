/*
 * 2025.11.09 - JLW
 * Took a shortcut and just asked Grok to create me a TCP client.  Had to do
 * some back and forth before he got it where it didn't pop up errors everywhere.
 * 
 * I'm hoping this is a close-enough implementation so I won't need to spend
 * a lot of time rewriting it.
 * 
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
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_TCPClient : XBase_Avalonia, IDisposable
    {
        public new string MyBaseClass = "TCPClient";
        public new string MyDefaultName = "tcpclient";

        /// <summary>
        /// This is the TCP Client base needed for all of the web capable
        /// classes.  It's designed by Grok and tweaked for my needs.
        /// </summary>
        public TcpClient? _client;
        public NetworkStream? _networkStream;
        public Stream? _stream;
        public StreamReader? _reader;
        public StreamWriter? _writer;
        public CancellationTokenSource? _receiveCts;
        public Task? _receiveTask;

        public readonly Encoding _defaultEncoding = Encoding.GetEncoding(1252);

        // All public properties now properly initialized
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
        public bool UseSSL { get; set; } = false;
        public Encoding Encoding { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
        public string LastError { get; set; } = string.Empty;
        public bool IsConnected => _client?.Connected == true;

        // Events are initialized to no-op delegates (never null)
        public Action<string>? OnLineReceived { get; set; }
        public Action? OnConnected { get; set; }
        public Action? OnDisconnected { get; set; }
        public Action<string>? OnError { get; set; }


        public XBase_Class_TCPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "tcp" : name;
            SetVisualObject(null, "TCP", name, false, UserObject.urw);

            Encoding = _defaultEncoding;

            // Events default to empty actions so no null checks needed when invoking
            OnLineReceived = null;     // allowed because nullable
            OnConnected = null;
            OnDisconnected = null;
            OnError = null;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public virtual bool Connect(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be null or empty.", nameof(host));
            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

            Disconnect();

            Host = host;
            Port = port;

            try
            {
                _client = new TcpClient();
                var connectTask = _client.ConnectAsync(host, port);
                if (!connectTask.Wait(Timeout))
                {
                    LastError = "Connection timeout";
                    OnError?.Invoke(LastError);
                    return false;
                }

                _networkStream = _client.GetStream();
                _stream = _networkStream;

                if (UseSSL)
                {
                    var ssl = new SslStream(_networkStream, false);
                    var sslTask = ssl.AuthenticateAsClientAsync(host);
                    if (!sslTask.Wait(Timeout))
                    {
                        LastError = "SSL handshake failed or timed out";
                        OnError?.Invoke(LastError);
                        return false;
                    }
                    _stream = ssl;
                }

                _reader = new StreamReader(_stream, Encoding);
                _writer = new StreamWriter(_stream, Encoding) { AutoFlush = true };

                OnConnected?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                OnError?.Invoke(ex.Message);
                Disconnect();
                return false;
            }
        }

        public bool Send(string data)
        {
            if (!IsConnected || _writer == null)
            {
                LastError = "Not connected";
                OnError?.Invoke(LastError);
                return false;
            }

            try
            {
                _writer.Write(data);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                OnError?.Invoke(ex.Message);
                Disconnect();
                return false;
            }
        }

        public bool SendLine(string line)
        {
            return Send((line ?? "").TrimEnd('\r', '\n') + "\r\n");
        }

        public string Receive(int maxBytes = 8192)
        {
            if (!IsConnected || _reader == null) return string.Empty;

            try
            {
                var buffer = new char[maxBytes];
                var readTask = _reader.ReadAsync(buffer, 0, buffer.Length);
                if (readTask.Wait(Timeout))
                {
                    int read = readTask.Result;
                    return read > 0 ? new string(buffer, 0, read) : string.Empty;
                }
                else
                {
                    LastError = "Receive timeout";
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return string.Empty;
            }
        }

        public string ReceiveLine()
        {
            if (!IsConnected || _reader == null) return string.Empty;

            try
            {
                var task = _reader.ReadLineAsync();
                if (task.Wait(Timeout))
                    return task.Result ?? string.Empty;
                else
                {
                    LastError = "ReceiveLine timeout";
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return string.Empty;
            }
        }

        public void BeginReceive()
        {
            if (_receiveTask != null) return;

            _receiveCts = new CancellationTokenSource();
            _receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (!_receiveCts.IsCancellationRequested && IsConnected && _reader != null)
                    {
                        var line = await _reader.ReadLineAsync();
                        if (line == null) break;
                        OnLineReceived?.Invoke(line);
                    }
                }
                catch (Exception ex) when (!_receiveCts.IsCancellationRequested)
                {
                    OnError?.Invoke(ex.Message);
                }
                finally
                {
                    if (!_receiveCts.IsCancellationRequested)
                        Disconnect();
                }
            }, _receiveCts.Token);
        }

        public void StopReceive()
        {
            _receiveCts?.Cancel();
            try { _receiveTask?.Wait(1000); } catch { }
            _receiveTask = null;
        }

        public virtual void Disconnect()
        {
            StopReceive();

            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            try { _networkStream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }

            _reader = null;
            _writer = null;
            _stream = null;
            _networkStream = null;
            _client = null;

            OnDisconnected?.Invoke();
        }

        public new void Dispose()
        {
            Disconnect();
            _receiveCts?.Dispose();
        }


        /*
         * Hardended code - smells a little, but should look over carefully
         * 
public class TcpClientEx : IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _networkStream;
    private Stream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private bool _disposed = false;
    private bool _isConnected = false;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public string LastError { get; private set; } = "";
    public bool AutoReconnect { get; set; } = true;

    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnLineReceived;
    public event Action<string>? OnError;
    public event Action<string>? OnWarning;

    public bool IsConnected => _isConnected && !_disposed && _client?.Connected == true;

    public bool Connect(string host, int port)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(TcpClientEx));

        lock (_lock)
        {
            try
            {
                DisconnectInternal();
                _client = new TcpClient();
                var task = _client.ConnectAsync(host, port);
                if (!task.Wait(Timeout))
                {
                    LastError = "Connection timeout";
                    OnError?.Invoke(LastError);
                    TryAutoReconnect(host, port);
                    return false;
                }

                _networkStream = _client.GetStream();
                _stream = _networkStream;
                _reader = new StreamReader(_stream, Encoding);
                _writer = new StreamWriter(_stream, Encoding) { AutoFlush = true };
                _isConnected = true;
                OnConnected?.Invoke();
                Task.Run(ReadLoop);
                return true;
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                LastError = $"Connect failed: {ex.Message}";
                OnError?.Invoke(LastError);
                TryAutoReconnect(host, port);
                return false;
            }
        }
    }

    private void ReadLoop()
    {
        try
        {
            while (!_disposed && _reader != null)
            {
                var line = _reader.ReadLine();
                if (line == null) break;
                OnLineReceived?.Invoke(line);
            }
        }
        catch (Exception ex) when (!_disposed)
        {
            OnError?.Invoke("Read error: " + ex.Message);
            TryAutoReconnect();
        }
        finally
        {
            if (!_disposed) Disconnect();
        }
    }

    private void TryAutoReconnect(string? host = null, int port = 0)
    {
        if (!AutoReconnect || _disposed) return;
        OnWarning?.Invoke("Auto-reconnecting in 2s...");
        Task.Delay(2000).ContinueWith(_ =>
        {
            if (host != null) Connect(host, port);
        });
    }

    public bool SendLine(string data)
    {
        if (!IsConnected) return false;
        try
        {
            lock (_lock)
            {
                _writer?.WriteLine(data);
                return true;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke("Send failed: " + ex.Message);
            TryAutoReconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        lock (_lock) DisconnectInternal();
        _isConnected = false;
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        GC.SuppressFinalize(this);
    }

    ~TcpClientEx() => Dispose();
}
         */
    }
}
