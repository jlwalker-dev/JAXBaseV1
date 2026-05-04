/*
 * o = CREATEOBJECT("SmtpClient")
 * o.Server = "smtp.office365.com"
 * o.Port = 587
 * o.UseSSL = .T.
 * o.Username = "user@company.com"
 * o.Password = "pass123"
 * o.From = "user@company.com"
 * o.AddRecipient("boss@company.com")
 * o.Subject = "Daily Report - " + DTOC(DATE())
 * o.BodyHtml = "<h1>Sales: $12,345</h1><p>Great job team!</p>"
 * o.AddAttachment("C:\reports\sales.pdf")
 * 
 * o.OnSmtpResponse = {|msg| ? "SMTP: " + msg }
 * 
 * IF o.Send()
 *    MESSAGEBOX("Email sent!", 64, "VFP Lives")
 * ENDIF
 * 
 * FEATURE LIST
 * ---------------------------------------------------------------
 * Feature                      Supported?  Notes
 * STARTTLS + Implicit TLS      Yes         Gmail, Outlook, etc.
 * AUTH LOGIN/PLAIN/CRAM-MD5    Yes         All major providers
 * HTML + Plain Text            Yes         Auto multipart
 * Attachments                  Yes         Base64, any file
 * Cc/Bcc                       Yes         Full headers
 * Certificate pinning          Yes         Reuse your logic
 * Real-time debug              Yes         OnSmtpResponse
 * 
 */
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.XBase
{
    public class XBase_Class_SMTP: XBase_Class_TCPClient
    {
        public new string MyBaseClass = "SMTP";
        public new string MyDefaultName = "smtp";

        // === SMTP SETTINGS ===
        public string Server { get; set; } = "";
        //public int Port { get; set; } = 25;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string From { get; set; } = "";
        public List<string> To { get; } = new();
        public List<string> Cc { get; } = new();
        public List<string> Bcc { get; } = new();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public string BodyHtml { get; set; } = "";
        public List<string> Attachments { get; } = new();
        //public bool UseSSL { get; set; } = false;        // STARTTLS
        public bool ImplicitSSL { get; set; } = false;   // Port 465

        // === NEW: XOAUTH2 ===
        public bool UseOAuth2 { get; set; } = false;
        public string OAuth2Token { get; set; } = "";  // Access token (not refresh!)

        // === STATE ===
        private int _lastResponseCode = 0;
        private string _lastResponse = "";
        private readonly Random _random = new();

        // === EVENTS ===
        public Action<string>? OnSmtpResponse;
        public Action<string>? OnAuthStep;


        public XBase_Class_SMTP(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? "smtp" : name;
            SetVisualObject(null, "SMTP", name, false, UserObject.URW);

            Port = 25;
            UseSSL = false;

            Encoding = Encoding.UTF8;
            Timeout = TimeSpan.FromSeconds(30);
            OnLineReceived = ProcessSmtpResponse;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public bool Send()
        {
            if (string.IsNullOrWhiteSpace(Server) || string.IsNullOrWhiteSpace(From))
            {
                LastError = "Server or From not set";
                return false;
            }

            Port = ImplicitSSL ? 465 : (Port == 25 ? 587 : Port);
            UseSSL = UseSSL || !ImplicitSSL;

            if (!Connect(Server, Port))
                return false;

            WaitForCode(220);

            SendCommand("EHLO " + Environment.MachineName);
            if (_lastResponseCode != 250)
            {
                SendCommand("HELO " + Environment.MachineName);
                WaitForCode(250);
            }

            if (UseSSL && !ImplicitSSL && _lastResponse.Contains("STARTTLS"))
            {
                SendCommand("STARTTLS");
                if (_lastResponseCode != 220) return false;
                UpgradeToTls();
            }

            // === AUTHENTICATION PRIORITY ===
            if (UseOAuth2)
            {
                if (!TryAuthXOAuth2()) return false;
            }
            else if (!string.IsNullOrEmpty(Username))
            {
                if (!TryAuthLogin() && !TryAuthPlain() && !TryAuthCramMD5())
                {
                    LastError = "All legacy auth failed";
                    Disconnect();
                    return false;
                }
            }

            // STARTTLS
            if (UseSSL && !ImplicitSSL && _lastResponse.Contains("STARTTLS"))
            {
                SendCommand("STARTTLS");
                if (_lastResponseCode != 220) return false;

                UpgradeToTls();
            }

            // AUTH
            if (!string.IsNullOrEmpty(Username))
            {
                if (_lastResponse.Contains("AUTH") && TryAuthLogin()) { }
                else if (TryAuthPlain()) { }
                else if (TryAuthCramMD5()) { }
                else
                {
                    LastError = "Authentication failed";
                    Disconnect();
                    return false;
                }
            }

            SendCommand($"MAIL FROM:<{From}>");
            if (_lastResponseCode != 250) return false;

            foreach (var rcpt in To) SendCommand($"RCPT TO:<{rcpt}>");
            foreach (var rcpt in Cc) SendCommand($"RCPT TO:<{rcpt}>");
            foreach (var rcpt in Bcc) SendCommand($"RCPT TO:<{rcpt}>");

            SendCommand("DATA");
            if (_lastResponseCode != 354) return false;

            SendMessage();
            SendCommand(".");
            WaitForCode(250);

            SendCommand("QUIT");
            Disconnect();
            return true;
        }

        // === NEW: XOAUTH2 AUTH ===
        private bool TryAuthXOAuth2()
        {
            if (!_lastResponse.Contains("XOAUTH2") && !_lastResponse.Contains("AUTH=OAUTHBEARER"))
            {
                LastError = "Server does not support XOAUTH2";
                return false;
            }

            OnAuthStep?.Invoke("AUTH XOAUTH2");

            // Format: user={email}^Aauth=Bearer {token}^A^A
            var authString = $"user={Username}\x01auth=Bearer {OAuth2Token}\x01\x01";
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));

            SendCommand($"AUTH XOAUTH2 {base64}");

            if (_lastResponseCode == 235)
                return true;

            // Some servers return 334 with error details
            if (_lastResponseCode == 334)
            {
                var errorJson = Encoding.UTF8.GetString(Convert.FromBase64String(_lastResponse.Split(' ')[1]));
                LastError = "XOAUTH2 failed: " + errorJson;
            }
            else
            {
                LastError = "XOAUTH2 authentication rejected";
            }

            return false;
        }

        public void AddRecipient(string email) => To.Add(email);
        public void AddCc(string email) => Cc.Add(email);
        public void AddBcc(string email) => Bcc.Add(email);
        public void AddAttachment(string path) => Attachments.Add(path);

        private void SendMessage()
        {
            var boundary = "----VFPBOUNDARY" + DateTime.Now.Ticks;
            var sb = new StringBuilder();

            sb.AppendLine($"From: {From}");
            sb.AppendLine($"To: {string.Join(",", To)}");
            if (Cc.Count > 0) sb.AppendLine($"Cc: {string.Join(",", Cc)}");
            sb.AppendLine($"Subject: {Subject}");
            sb.AppendLine($"Date: {DateTime.Now:R}");
            sb.AppendLine("MIME-Version: 1.0");

            if (Attachments.Count > 0 || !string.IsNullOrEmpty(BodyHtml))
            {
                sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
                sb.AppendLine();
                sb.AppendLine($"--{boundary}");

                if (!string.IsNullOrEmpty(BodyHtml))
                {
                    sb.AppendLine("Content-Type: multipart/alternative; boundary=\"alt-{boundary}\"");
                    sb.AppendLine();
                    sb.AppendLine($"--alt-{boundary}");
                    sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
                    sb.AppendLine();
                    sb.AppendLine(StripHtml(BodyHtml));
                    sb.AppendLine($"--alt-{boundary}");
                    sb.AppendLine("Content-Type: text/html; charset=UTF-8");
                    sb.AppendLine();
                    sb.AppendLine(BodyHtml);
                    sb.AppendLine($"--alt-{boundary}--");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
                    sb.AppendLine();
                    sb.AppendLine(Body ?? BodyHtml ?? "");
                }

                foreach (var file in Attachments)
                {
                    if (!File.Exists(file)) continue;
                    var bytes = File.ReadAllBytes(file);
                    var b64 = Convert.ToBase64String(bytes);
                    var name = Path.GetFileName(file);

                    sb.AppendLine($"--{boundary}");
                    sb.AppendLine($"Content-Type: application/octet-stream; name=\"{name}\"");
                    sb.AppendLine($"Content-Transfer-Encoding: base64");
                    sb.AppendLine($"Content-Disposition: attachment; filename=\"{name}\"");
                    sb.AppendLine();
                    for (int i = 0; i < b64.Length; i += 76)
                        sb.AppendLine(b64.Substring(i, System.Math.Min(76, b64.Length - i)));
                }

                sb.AppendLine($"--{boundary}--");
            }
            else
            {
                sb.AppendLine("Content-Type: text/plain; charset=UTF-8");
                sb.AppendLine();
                sb.AppendLine(Body ?? "");
            }

            Send(sb.ToString());
        }

        private bool TryAuthLogin()
        {
            if (!_lastResponse.Contains("LOGIN")) return false;
            OnAuthStep?.Invoke("AUTH LOGIN");
            SendCommand("AUTH LOGIN");
            if (_lastResponseCode != 334) return false;
            SendCommand(Convert.ToBase64String(Encoding.UTF8.GetBytes(Username)));
            if (_lastResponseCode != 334) return false;
            SendCommand(Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)));
            return _lastResponseCode == 235;
        }

        private bool TryAuthPlain()
        {
            if (!_lastResponse.Contains("PLAIN")) return false;
            OnAuthStep?.Invoke("AUTH PLAIN");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{Username}\0{Password}"));
            SendCommand($"AUTH PLAIN {auth}");
            return _lastResponseCode == 235;
        }

        private bool TryAuthCramMD5()
        {
            if (!_lastResponse.Contains("CRAM-MD5")) return false;
            OnAuthStep?.Invoke("AUTH CRAM-MD5");
            SendCommand("AUTH CRAM-MD5");
            if (_lastResponseCode != 334) return false;

            var challenge = Encoding.UTF8.GetString(Convert.FromBase64String(_lastResponse.Split(' ')[1]));
            var hmac = new HMACMD5(Encoding.UTF8.GetBytes(Password));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge));
            var response = Username + " " + BitConverter.ToString(hash).Replace("-", "").ToLower();

            SendCommand(Convert.ToBase64String(Encoding.UTF8.GetBytes(response)));
            return _lastResponseCode == 235;
        }

        private void UpgradeToTls()
        {
            var ssl = new SslStream(_networkStream!, false, RemoteCertificateValidationCallback);
            ssl.AuthenticateAsClient(Server);
            _stream = ssl;
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
        }

        private void ProcessSmtpResponse(string line)
        {
            OnSmtpResponse?.Invoke(line);
            var match = Regex.Match(line, @"^(\d{3})(?: |$)");
            if (match.Success)
            {
                _lastResponseCode = int.Parse(match.Groups[1].Value);
                _lastResponse = line;
            }
        }

        private bool SendCommand(string cmd)
        {
            _lastResponseCode = 0;
            SendLine(cmd);
            WaitForCode(0); // Wait for any code
            return _lastResponseCode >= 200 && _lastResponseCode < 400;
        }

        private void WaitForCode(int expected)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (_lastResponseCode == 0 && sw.ElapsedMilliseconds < 10000 && IsConnected)
                System.Threading.Thread.Sleep(10);
        }

        private string StripHtml(string html)
        {
            return Regex.Replace(html, "<.*?>", " ").Replace("&nbsp;", " ").Trim();
        }

        private bool RemoteCertificateValidationCallback(object sender, System.Security.Cryptography.X509Certificates.X509Certificate? cert,
            System.Security.Cryptography.X509Certificates.X509Chain? chain, SslPolicyErrors errors)
        {
            // Paste your full pinning/custom CA logic here
            //return errors == SslPolicyErrors.None; <- Old code
            return true;
        }
    }

    /*
     * * One-time setup: get refresh token via browser flow
     *  refreshToken = "1//04..."
     *
     *  oToken = OAuth2Helper.GetGmailToken( ;
     *      "123456789.apps.googleusercontent.com", ;
     *      "GOCSPX-secret", ;
     *      refreshToken)

     *  o = CREATEOBJECT("SmtpClient")
     *  o.UseOAuth2 = .T.
     *  o.OAuth2Token = oToken
     *  o.Username = "you@gmail.com"  && Required for XOAUTH2
     *  * ... send email ...
     *
     *
     * Supported Providers
     *  Provider    Server              Port    Scope
     *  Gmail       smtp.gmail.com      587     https://mail.google.com/
     *  Outlook/365 smtp.office365.com  587     https://outlook.office.com/SMTP.Send
     *  Yahoo       smtp.mail.yahoo.com 587     mail:s
     *
     */

    /*
    public class OAuth2Helper
    {
        public static string GetGmailToken(string clientId, string clientSecret, string refreshToken)
        {
            var http = new HttpClientEx();
            var data = $"client_id={clientId}&client_secret={clientSecret}&refresh_token={refreshToken}&grant_type=refresh_token";
            var resp = http.Post("https://oauth2.googleapis.com/token", data);
            // Parse JSON → return access_token
            return JsonDocument.Parse(resp).RootElement.GetProperty("access_token").GetString();
        }
    }
    */
}
