/*
 * Based on Grok's UDP Client
 *
 * Usage Examples
 * ----------------------------------------------------------------------------
 * o = CREATEOBJECT("UdpClient")
 * o.EnableBroadcast = .T.
 * o.Bind(5000)
 * 
 * o.Send("PING", "255.255.255.255", 5000)
 * o.Send("Hello", "google.com", 5000)  && Resolves DNS → sends to first IP
 * 
 * o.OnDataReceived = {|data,ip,port|
 *    ? "UDP from", ip + ":" + TRANSFORM(port), "→", data
 * }
 * 
 * ----------------------------------------------------------------------------
 * 
 * * 1. Network discovery
 * o = CREATEOBJECT("UdpClient")
 * o.EnableBroadcast = .T.
 * o.Bind(5000)
 * 
 * o.Send("VFP-HELLO", "255.255.255.255", 5000)
 * 
 * o.OnDataReceived = {|data,ip,port|
 *    ? "Found:", ip, "says:", data
 * }
 * 
 * WAIT TIMEOUT 10
 * 
 * * 2. DNS lookup
 * o = CREATEOBJECT("UdpClient")
 * aIPs = o.ResolveHost("api.github.com")
 * FOR EACH ip IN aIPs
 *    ? "GitHub IP:", ip
 * ENDFOR
 * 
 * * 3. Multicast chat
 * o = CREATEOBJECT("UdpClient")
 * o.JoinMulticast("239.1.1.1")
 * o.Bind(5001)
 * 
 * o.OnDataReceived = {|msg,ip,port|
 *    ? "<", ip, ">", msg
 * }
 * 
 * o.Send("VFP joined the multicast!", "239.1.1.1", 5001)
 * 
 * -------------------------------------------------------------------------
 * With extra error checking
 * 
 * o = CREATEOBJECT("UdpClient")
 * o.AutoRebindOnError = .T.
 * o.EnableBroadcast = .T.
 * 
 * o.OnError = {|msg| ? "ERROR: " + msg }
 * o.OnWarning = {|msg| ? "WARN: " + msg }
 * o.OnDataReceived = {|d,i,p| ? "FROM", i + ":" + TRANSFORM(p), "→", d }
 * o.OnBound = {|port| ? "UDP bound to port", port }
 * 
 * o.Bind(5000)
 * o.Send("VFP IS ALIVE", "255.255.255.255", 5000)
 * 
 * * Even if network dies — it auto-rebinds!
 * 
 * Error Handling Features
 *      SocketException codes
 *      Timeout handling
 *      ObjectDisposed safety
 *      NullReference guards
 *      lock() thread safety
 *      AutoRebindOnError
 *      OnError / OnWarning
 *      LastError property
 *      Dispose() + finalizer
 *      DNS fallback
 *      Port conflict detection
 *  
 */
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_UDPClient : XBase_Avalonia, IDisposable
    {
        public new string MyBaseClass = "UDPClient";
        public new string MyDefaultName = "udpclient";

        private UdpClient? _udp;
        private IPEndPoint? _remoteEp;
        private IPEndPoint? _localEp;
        private bool _isBound = false;
        private bool _isDisposed = false;
        private readonly object _lock = new();

        // === CONFIG ===
        public int LocalPort { get; set; } = 0;
        public string LocalIP { get; set; } = "0.0.0.0";
        public bool EnableBroadcast { get; set; } = false;
        public int TimeoutMs { get; set; } = 5000;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public bool AutoRebindOnError { get; set; } = true;

        // === EVENTS (VFP-STYLE) ===
        public Action<string, string, int>? OnDataReceived;   // data, ip, port
        public Action<string>? OnError;                       // error message
        public Action<string>? OnWarning;                     // non-fatal
        public Action? OnDisposed;
        public Action<int>? OnBound;                           // local port

        public int GetLocalPort() => _isBound ? ((IPEndPoint?)_udp?.Client?.LocalEndPoint)?.Port ?? 0 : LocalPort;
        public bool IsBound => _isBound && !_isDisposed;
        public string LastError { get; private set; } = "";

        public XBase_Class_UDPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "udp" : name;
            SetVisualObject(null, "UDP", name, false, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public bool Bind(int port = 0)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(XBase_Class_UDPClient));
            if (_isBound) return true;

            lock (_lock)
            {
                try
                {
                    LocalPort = port == 0 ? GetRandomPort() : port;
                    _localEp = new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort);

                    _udp = new UdpClient();
                    _udp.Client.Bind(_localEp);
                    _udp.Client.ReceiveTimeout = TimeoutMs;
                    _udp.Client.SendTimeout = TimeoutMs;

                    if (EnableBroadcast)
                    {
                        try { _udp.EnableBroadcast = true; }
                        catch (Exception ex) { OnWarning?.Invoke("Broadcast enable failed: " + ex.Message); }
                    }

                    _isBound = true;
                    OnBound?.Invoke(LocalPort);
                    Task.Run(ReceiveLoop);
                    return true;
                }
                catch (SocketException ex)
                {
                    LastError = $"Socket error {ex.SocketErrorCode}: {ex.Message}";
                    OnError?.Invoke(LastError);
                    TryAutoRebind();
                    return false;
                }
                catch (Exception ex)
                {
                    LastError = "Bind failed: " + ex.Message;
                    OnError?.Invoke(LastError);
                    return false;
                }
            }
        }

        public bool Send(string data, string host, int port)
        {
            if (_isDisposed) { OnError?.Invoke("Send failed: Object disposed"); return false; }
            if (!_isBound && !Bind()) return false;

            lock (_lock)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        OnWarning?.Invoke("Send called with empty data");
                        return true;
                    }

                    var bytes = Encoding.GetBytes(data);
                    var ipAddresses = ResolveHostSafe(host);

                    if (ipAddresses.Length == 0)
                    {
                        OnError?.Invoke($"Cannot resolve host: {host}");
                        return false;
                    }

                    _remoteEp = new IPEndPoint(IPAddress.Parse(ipAddresses[0]), port);
                    int sent = _udp!.Send(bytes, bytes.Length, _remoteEp);

                    if (sent != bytes.Length)
                        OnWarning?.Invoke($"Partial send: {sent}/{bytes.Length} bytes");

                    return true;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    OnError?.Invoke("Send timeout");
                    TryAutoRebind();
                    return false;
                }
                catch (SocketException ex)
                {
                    LastError = $"Send failed ({ex.SocketErrorCode}): {ex.Message}";
                    OnError?.Invoke(LastError);
                    TryAutoRebind();
                    return false;
                }
                catch (Exception ex)
                {
                    LastError = "Send exception: " + ex.Message;
                    OnError?.Invoke(LastError);
                    return false;
                }
            }
        }

        public string? Receive(int timeoutMs = -1)
        {
            if (_isDisposed || !_isBound) return null;

            try
            {
                _udp!.Client.ReceiveTimeout = timeoutMs > 0 ? timeoutMs : TimeoutMs;
                var result = _udp.Receive(ref _localEp!);
                var data = Encoding.GetString(result);
                OnDataReceived?.Invoke(data, _localEp.Address.ToString(), _localEp.Port);
                return data;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                return null;
            }
            catch (SocketException ex)
            {
                OnError?.Invoke($"Receive socket error ({ex.SocketErrorCode})");
                TryAutoRebind();
                return null;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Receive failed: " + ex.Message);
                return null;
            }
        }

        public void JoinMulticast(string groupIp, string localIp = "")
        {
            if (!_isBound) Bind();
            try
            {
                var group = IPAddress.Parse(groupIp);
                var local = string.IsNullOrEmpty(localIp) ? IPAddress.Any : IPAddress.Parse(localIp);
                _udp!.JoinMulticastGroup(group, local);
            }
            catch (Exception ex)
            {
                OnError?.Invoke("JoinMulticast failed: " + ex.Message);
            }
        }

        public void LeaveMulticast(string groupIp)
        {
            try { _udp?.DropMulticastGroup(IPAddress.Parse(groupIp)); }
            catch (Exception ex) { OnWarning?.Invoke("LeaveMulticast: " + ex.Message); }
        }

        public string[] ResolveHost(string host)
        {
            var result = ResolveHostSafe(host);
            if (result.Length == 0)
                OnError?.Invoke($"DNS resolve failed: {host}");
            return result;
        }

        private string[] ResolveHostSafe(string host)
        {
            try
            {
                if (host == "255.255.255.255" || host == "0.0.0.0" || IPAddress.TryParse(host, out _))
                    return new[] { host };

                var entry = Dns.GetHostEntry(host);
                return Array.ConvertAll(entry.AddressList, a => a.ToString());
            }
            catch (SocketException ex)
            {
                OnError?.Invoke($"DNS Socket error {ex.SocketErrorCode}: {host}");
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"DNS failed for {host}: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private int GetRandomPort()
        {
            var used = new HashSet<int>();
            for (int i = 0; i < 10; i++)
            {
                var port = new Random().Next(50000, 60000);
                if (!used.Contains(port) && IsPortAvailable(port))
                    return port;
                used.Add(port);
            }
            return 0;
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch { return false; }
        }

        private void TryAutoRebind()
        {
            if (!AutoRebindOnError || _isDisposed) return;

            OnWarning?.Invoke("Auto-rebinding after network error...");
            Task.Delay(1000).ContinueWith(_ =>
            {
                Close();
                Bind(LocalPort);
            });
        }

        private async void ReceiveLoop()
        {
            while (!_isDisposed && _isBound && _udp != null)
            {
                try
                {
                    var result = await _udp.ReceiveAsync().ConfigureAwait(false);
                    if (_isDisposed) break;

                    var data = Encoding.GetString(result.Buffer);
                    var remote = result.RemoteEndPoint;
                    OnDataReceived?.Invoke(data, remote.Address.ToString(), remote.Port);
                }
                catch (ObjectDisposedException) { break; }
                catch (NullReferenceException) { break; }
                catch (SocketException ex)
                {
                    if (!_isDisposed)
                        OnError?.Invoke($"ReceiveLoop socket error: {ex.SocketErrorCode}");
                    TryAutoRebind();
                }
                catch (Exception ex)
                {
                    if (!_isDisposed)
                        OnError?.Invoke("ReceiveLoop error: " + ex.Message);
                }
            }
        }

        public void Close()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                _isBound = false;
                try { _udp?.Client?.Shutdown(SocketShutdown.Both); } catch { }
                try { _udp?.Close(); } catch { }
                try { _udp?.Dispose(); } catch { }
                _udp = null;
            }
        }

        public new void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Close();
            OnDisposed?.Invoke();
            GC.SuppressFinalize(this);
        }

        ~XBase_Class_UDPClient() => Dispose();
    }
}
