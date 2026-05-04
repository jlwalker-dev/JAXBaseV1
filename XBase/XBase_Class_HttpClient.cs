/*
 * 2025.11.09 - JLW
 * 
 * 100% Grok produced with some direction on what I wanted.  I'm sure I'm going to
 * have to tweak this and do some actual work, but I'm hoping I have a generally
 * workable product that just needs to be reworked for my needs.
 * 
 * How to use for Unsecured
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
 * And use as a Secured HTTP client
 * o = CREATEOBJECT("HttpClient")
 *
 * * Force HTTPS only (reject HTTP)
 * o.ValidateServerCertificate = .T.
 * o.Get("http://insecure.site")  && Returns "" + LastError = "SSL required but UseSSL=false"
 * 
 * * Allow both (default behavior)
 * o.Get("http://oldserver.local")  && Works
 * o.Get("https://bank.com")        && Works + validated
 * 
 * * Temporarily disable validation (dev only)
 * o.ValidateServerCertificate = .F.
 * o.Get("https://selfsigned.local") && Works even with bad cert
 * 
 */
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_HttpClient : XBase_Class_TCPClient
    {
        public new string MyBaseClass = "HTTPClient";
        public new string MyDefaultName = "httpclient";

        /// <summary>
        /// JAXBase HTTP client: CREATEOBJECT("HttpClient")
        /// Full GET/POST/PUT/DELETE, form posts, JSON, headers, cookies, redirects, timeouts.
        /// Built on TcpClientEx — no System.Net.Http dependency.
        /// </summary>
        /// 
        private readonly Dictionary<string, string> _headers = new();
        private readonly List<Cookie> _cookies = new();
        private readonly StringBuilder _responseHeaders = new();
        private int _statusCode = 0;
        private string _statusDescription = "";
        private string _redirectUrl = "";

        // === HTTPS SECURITY OPTIONS ===
        public bool ValidateServerCertificate { get; set; } = true;
        public List<X509Certificate2> TrustedRootCerts { get; } = new();
        public string? PinnedThumbprint { get; set; } // SHA-1 or SHA-256 hex
        public string? PinnedPublicKey { get; set; }  // Base64 SPKI
        public SslPolicyErrors AllowedSslErrors { get; set; } = SslPolicyErrors.None;

        public string BaseUrl { get; set; } = "";
        public string LastResponse { get; private set; } = "";
        public string LastResponseHeaders => _responseHeaders.ToString();
        public int StatusCode => _statusCode;
        public string StatusDescription => _statusDescription;
        public bool FollowRedirects { get; set; } = true;
        public int MaxRedirects { get; set; } = 5;
        public string ContentType { get; set; } = "application/x-www-form-urlencoded";
        public string UserAgent { get; set; } = "VFP-HttpClientEx-Secure/1.0";


        public XBase_Class_HttpClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            Timeout = TimeSpan.FromSeconds(30);
            Encoding = Encoding.UTF8;
            AddHeader("User-Agent", UserAgent);

        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }



        public void AddHeader(string name, string value)
        {
            _headers[name] = value;
        }

        public void RemoveHeader(string name)
        {
            _headers.Remove(name);
        }

        public void ClearHeaders()
        {
            _headers.Clear();
            AddHeader("User-Agent", UserAgent);
        }

        public void AddCookie(string name, string value, string domain = "", string path = "/")
        {
            _cookies.Add(new Cookie(name, value, path, domain));
        }

        public string Get(string url)
        {
            return Execute(url, "GET", null);
        }

        public string Post(string url, string data)
        {
            ContentType = "application/x-www-form-urlencoded";
            return Execute(url, "POST", data);
        }

        public string PostJson(string url, string json)
        {
            ContentType = "application/json";
            return Execute(url, "POST", json);
        }

        public string PostForm(string url, Dictionary<string, string> form)
        {
            var data = string.Join("&", form.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
            ContentType = "application/x-www-form-urlencoded";
            return Execute(url, "POST", data);
        }

        public string Put(string url, string data)
        {
            ContentType = "application/x-www-form-urlencoded";
            return Execute(url, "PUT", data);
        }

        public string Delete(string url)
        {
            return Execute(url, "DELETE", null);
        }

        private string Execute(string url, string method, string? postData)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is required.", nameof(url));

            url = ResolveUrl(url);
            var uri = new Uri(url);
            int port = uri.Port == -1 ? (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : uri.Port;
            UseSSL = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);

            LastResponse = "";
            _responseHeaders.Clear();
            _statusCode = 0;
            _statusDescription = "";
            _redirectUrl = "";

            int redirectCount = 0;

            while (true)
            {
                if (!ConnectSecure(uri.Host, port))
                    return "";

                try
                {
                    // === BUILD REQUEST ===
                    var sb = new StringBuilder();
                    sb.AppendLine($"{method} {uri.PathAndQuery} HTTP/1.1");
                    sb.AppendLine($"Host: {uri.Host}{(port != 80 && port != 443 ? ":" + port : "")}");

                    foreach (var h in _headers)
                        sb.AppendLine($"{h.Key}: {h.Value}");

                    var cookieHeader = GetCookieHeader(uri);
                    if (!string.IsNullOrEmpty(cookieHeader))
                        sb.AppendLine($"Cookie: {cookieHeader}");

                    if (!string.IsNullOrEmpty(postData))
                    {
                        sb.AppendLine($"Content-Type: {ContentType}");
                        sb.AppendLine($"Content-Length: {Encoding.GetByteCount(postData)}");
                    }
                    sb.AppendLine("Connection: close");
                    sb.AppendLine();

                    if (!string.IsNullOrEmpty(postData))
                        sb.Append(postData);

                    if (!Send(sb.ToString()))
                        return "";

                    // === READ RESPONSE ===
                    BeginReceive();
                    bool headersDone = false;
                    OnLineReceived = line =>
                    {
                        if (_statusCode == 0)
                        {
                            if (line.StartsWith("HTTP/"))
                            {
                                var parts = line.Split(' ', 3);
                                if (parts.Length >= 2) int.TryParse(parts[1], out _statusCode);
                                _statusDescription = parts.Length > 2 ? parts[2] : "";
                            }
                            else if (!string.IsNullOrEmpty(line) && line.Contains(':'))
                            {
                                _responseHeaders.AppendLine(line);
                                var header = line.Split(new[] { ':' }, 2);
                                if (header.Length == 2)
                                {
                                    var name = header[0].Trim();
                                    var value = header[1].Trim();

                                    if (name.Equals("Location", StringComparison.OrdinalIgnoreCase))
                                        _redirectUrl = value;
                                    if (name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                                        ParseSetCookie(value, uri.Host);
                                }
                            }
                            else if (string.IsNullOrEmpty(line.Trim()))
                            {
                                headersDone = true;
                                OnLineReceived = bodyLine => LastResponse += bodyLine + "\r\n";
                            }
                        }
                        else if (headersDone)
                        {
                            LastResponse += line + "\r\n";
                        }
                    };

                    Thread.Sleep(100);
                    while (IsConnected && _statusCode == 0)
                        Thread.Sleep(50);

                    StopReceive();
                    Disconnect();

                    // === REDIRECT ===
                    if (FollowRedirects && (_statusCode >= 300 && _statusCode <= 399) && !string.IsNullOrEmpty(_redirectUrl))
                    {
                        if (++redirectCount > MaxRedirects)
                        {
                            LastError = "Too many redirects";
                            OnError?.Invoke(LastError);
                            return "";
                        }

                        url = _redirectUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? _redirectUrl
                            : new Uri(uri, _redirectUrl).ToString();

                        uri = new Uri(url);
                        port = uri.Port == -1 ? (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 80) : uri.Port;
                        UseSSL = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    return LastResponse.TrimEnd('\r', '\n');
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    OnError?.Invoke(ex.Message);
                    Disconnect();
                    return "";
                }
            }
        }

        private string ResolveUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            if (url.StartsWith("http")) return url;
            if (string.IsNullOrEmpty(BaseUrl)) return url;
            return BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
        }

        private string GetCookieHeader(Uri uri)
        {
            var cookies = _cookies.Where(c =>
                (string.IsNullOrEmpty(c.Domain) || c.Domain.Equals(uri.Host, StringComparison.OrdinalIgnoreCase)) &&
                uri.PathAndQuery.StartsWith(c.Path));

            return string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
        }

        private void ParseSetCookie(string setCookie, string host)
        {
            if (string.IsNullOrWhiteSpace(setCookie)) return;
            var parts = setCookie.Split(';');
            if (parts.Length == 0) return;

            var main = parts[0].Split('=', 2);
            if (main.Length != 2) return;

            var name = main[0].Trim();
            var value = main[1].Trim();
            if (string.IsNullOrEmpty(name)) return;

            string path = "/";
            string domain = host;

            for (int i = 1; i < parts.Length; i++)
            {
                var kv = parts[i].Split('=', 2);
                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv.Length > 1 ? kv[1].Trim() : "";
                if (key == "path" && !string.IsNullOrEmpty(val)) path = val;
                if (key == "domain" && !string.IsNullOrEmpty(val)) domain = val;
            }

            _cookies.RemoveAll(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
                c.Path == path &&
                c.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));

            _cookies.Add(new Cookie(name, value, path, domain));
        }

        // === SECURE CONNECT WITH FULL CERT VALIDATION ===
        private bool ConnectSecure(string host, int port)
        {
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

                if (!UseSSL)
                {
                    _stream = _networkStream;
                }
                else
                {
                    var ssl = new SslStream(
                        _networkStream,
                        false,
                        ValidateServerCertificate ? RemoteCertificateValidationCallback : ((a, b, c, d) => true),
                        null
                    );

                    var sslTask = ssl.AuthenticateAsClientAsync(host);
                    if (!sslTask.Wait(Timeout))
                    {
                        LastError = "SSL handshake timeout";
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

        private bool RemoteCertificateValidationCallback(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null) return false;

            var cert = new X509Certificate2(certificate);

            // 1. Allow specific SSL errors (e.g. self-signed in dev)
            if ((sslPolicyErrors & ~AllowedSslErrors) != SslPolicyErrors.None)
            {
                LastError = $"SSL Error: {sslPolicyErrors}";
                return false;
            }

            // 2. Certificate pinning
            if (!string.IsNullOrEmpty(PinnedThumbprint))
            {
                var thumb = cert.Thumbprint.Replace(" ", "").ToUpperInvariant();
                if (!thumb.Equals(PinnedThumbprint.Replace(" ", "").ToUpperInvariant(), StringComparison.Ordinal))
                {
                    LastError = "Certificate thumbprint mismatch (pinning failed)";
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(PinnedPublicKey))
            {
                var spki = Convert.ToBase64String(cert.GetPublicKey());
                if (!spki.Equals(PinnedPublicKey, StringComparison.Ordinal))
                {
                    LastError = "Certificate public key mismatch (pinning failed)";
                    return false;
                }
            }

            // 3. Custom trusted roots
            if (TrustedRootCerts.Count > 0)
            {
                using var customChain = new X509Chain();
                customChain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                customChain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                foreach (var root in TrustedRootCerts)
                    customChain.ChainPolicy.CustomTrustStore.Add(root);
                customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;

                bool chainValid = customChain.Build(cert);
                if (!chainValid)
                {
                    LastError = "Certificate not trusted by custom roots";
                    return false;
                }
            }

            return true;
        }


    }
}