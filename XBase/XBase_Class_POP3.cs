/*
 * 
 * 
 * o = CREATEOBJECT("Pop3Client")
 * o.Server = "pop.gmail.com"
 * o.Port   = 995
 * o.ImplicitSSL = .T.
 * o.UseOAuth2 = .T.
 * o.OAuth2Token = GetGmailAccessToken()  && Your helper
 * o.Username = "you@gmail.com"
 * 
 * o.OnPop3Response = {|msg| ? "POP3: " + msg }
 * 
 * IF o.Connect()
 *    ? "You have", o.MessageCount, "emails"
 *    FOR i = 1 TO o.MessageCount
 *       uid = o.GetMessageUid(i)
 *       ? i, uid
 *    ENDFOR
 * 
 *    o.RetrieveMessage(1, "C:\mail\inbox1.eml")
 *    o.Disconnect()
 * ENDIF
 * 
 * -----------------------------------------------------------------------
 * 
 * o = CREATEOBJECT("Pop3Client")
 * o.Server = "outlook.office365.com"
 * o.Port   = 995
 * o.ImplicitSSL = .T.
 * o.UseOAuth2 = .T.
 * o.OAuth2Token = "ya29.a0AfB3..."  && From your OAuth flow
 * o.Username = "you@company.com"
 * 
 * IF o.Connect()
 *    ? "Inbox has", o.MessageCount, "messages"
 *    o.RetrieveMessage(1, "C:\emails\first.eml")
 *    o.DownloadAttachment(1, 1, "C:\attach\report.pdf")
 *    o.DeleteMessage(1)
 *    o.Disconnect()
 * ENDIF
 * 
 */

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.XBase
{
    public class XBase_Class_POP3 : XBase_Class_TCPClient
    {
        public new string MyBaseClass = "POP3";
        public new string MyDefaultName = "pop3";

        // === POP3 SETTINGS ===
        public string Server { get; set; } = "";
        //public int Port { get; set; } = 110;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        //public bool UseSSL { get; set; } = false;        // STARTTLS
        public bool ImplicitSSL { get; set; } = false;   // Port 995
        public bool UseOAuth2 { get; set; } = false;
        public string OAuth2Token { get; set; } = "";

        // === STATE ===
        private int _messageCount = 0;
        private long _mailboxSize = 0;
        private Dictionary<int, long> _messageSizes = new();
        private Dictionary<int, string> _messageUids = new();
        private int _lastResponseCode = 0;
        private string _lastResponse = "";

        // === EVENTS ===
        public Action<string>? OnPop3Response;
        public Action<int, int>? OnRetrieveProgress; // bytes, total

        public int MessageCount => _messageCount;
        public long MailboxSize => _mailboxSize;


        public XBase_Class_POP3(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            Port = 110;
            UseSSL = false;

            Encoding = Encoding.UTF8;
            Timeout = TimeSpan.FromSeconds(30);
            OnLineReceived += ProcessPop3Response;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public override bool Connect()
        {
            if (string.IsNullOrWhiteSpace(Server)) return false;

            Port = ImplicitSSL ? 995 : (Port == 995 ? 110 : Port);
            UseSSL = UseSSL || ImplicitSSL;

            if (!base.Connect()) return false;

            WaitForResponse();
            if (!_lastResponse.StartsWith("+OK")) return false;

            if (UseSSL && !ImplicitSSL && _lastResponse.Contains("STLS"))
            {
                SendCommand("STLS");
                if (!_lastResponse.StartsWith("+OK")) return false;
                UpgradeToTls();
            }

            // AUTH
            if (UseOAuth2)
            {
                if (!TryAuthXOAuth2()) return false;
            }
            else if (!string.IsNullOrEmpty(Username))
            {
                if (!TryAuthUserPass() && !TryAuthApop())
                {
                    LastError = "All auth failed";
                    Disconnect();
                    return false;
                }
            }

            // Get mailbox stats
            SendCommand("STAT");
            if (!ParseStat()) return false;

            // Get UIDL for unique IDs
            SendCommand("UIDL");
            ParseUidl();

            return true;
        }

        public bool RetrieveMessage(int msgNum, string savePath)
        {
            if (!SendCommand($"RETR {msgNum}")) return false;

            using var fs = new FileStream(savePath, FileMode.Create);
            using var sw = new StreamWriter(fs, Encoding.UTF8);
            var inBody = false;

            OnLineReceived += line =>
            {
                OnPop3Response?.Invoke(line);
                if (line == ".") return;
                if (inBody) sw.WriteLine(line);
                if (string.IsNullOrEmpty(line)) inBody = true;
            };

            WaitForResponse(); // Wait for final +OK
            return _lastResponse.StartsWith("+OK");
        }

        public string GetMessageHeaders(int msgNum)
        {
            if (!SendCommand($"TOP {msgNum} 0")) return "";
            var sb = new StringBuilder();
            var done = false;

            OnLineReceived += line =>
            {
                if (line == ".") done = true;
                else if (!done) sb.AppendLine(line);
            };

            WaitForResponse();
            return sb.ToString();
        }

        public bool DownloadAttachment(int msgNum, int partNum, string savePath)
        {
            // Simple: retrieve full message and extract part
            var temp = Path.GetTempFileName();
            if (!RetrieveMessage(msgNum, temp)) return false;

            var eml = File.ReadAllText(temp);
            var parts = Regex.Split(eml, "--");
            // Very basic — improve if needed
            File.Delete(temp);
            return false; // Placeholder
        }

        public bool DeleteMessage(int msgNum)
        {
            return SendCommand($"DELE {msgNum}") && _lastResponse.StartsWith("+OK");
        }

        public string GetMessageUid(int msgNum)
        {
            return _messageUids.TryGetValue(msgNum, out var uid) ? uid : "";
        }

        public override void Disconnect()
        {
            SendCommand("QUIT");
            base.Disconnect();
        }

        // === AUTH METHODS ===
        private bool TryAuthUserPass()
        {
            SendCommand($"USER {Username}");
            if (!_lastResponse.StartsWith("+OK")) return false;
            SendCommand($"PASS {Password}");
            return _lastResponse.StartsWith("+OK");
        }

        private bool TryAuthApop()
        {
            var match = Regex.Match(_lastResponse, @"<(.+?)>");
            if (!match.Success) return false;

            var timestamp = match.Groups[1].Value;
            var secret = timestamp + Password;
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.ASCII.GetBytes(secret));
            var digest = BitConverter.ToString(hash).Replace("-", "").ToLower();

            SendCommand($"APOP {Username} {digest}");
            return _lastResponse.StartsWith("+OK");
        }

        private bool TryAuthXOAuth2()
        {
            var auth = $"user={Username}\x01auth=Bearer {OAuth2Token}\x01\x01";
            var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(auth));
            SendCommand($"AUTH XOAUTH2 {b64}");

            if (_lastResponse.StartsWith("+OK")) return true;
            if (_lastResponse.StartsWith("+ "))
            {
                var err = Encoding.UTF8.GetString(Convert.FromBase64String(_lastResponse.Substring(2)));
                LastError = "XOAUTH2 error: " + err;
            }
            return false;
        }

        // === PARSE RESPONSES ===
        private void ProcessPop3Response(string line)
        {
            OnPop3Response?.Invoke(line);
            if (line.StartsWith("+OK"))
            {
                _lastResponseCode = 1;
                _lastResponse = line;
            }
            else if (line.StartsWith("-ERR"))
            {
                _lastResponseCode = -1;
                _lastResponse = line;
                LastError = line;
            }
        }

        private bool ParseStat()
        {
            var parts = _lastResponse.Split(' ');
            if (parts.Length < 3) return false;
            int.TryParse(parts[1], out _messageCount);
            long.TryParse(parts[2], out _mailboxSize);
            return true;
        }

        private void ParseUidl()
        {
            _messageUids.Clear();
            var inMultiLine = false;

            OnLineReceived += line =>
            {
                if (line == ".") inMultiLine = false;
                if (inMultiLine)
                {
                    var parts = line.Split(' ', 2);
                    if (parts.Length == 2 && int.TryParse(parts[0], out var num))
                        _messageUids[num] = parts[1];
                }
                if (line.StartsWith("+OK")) inMultiLine = true;
            };

            WaitForResponse();
        }

        private bool SendCommand(string cmd)
        {
            _lastResponseCode = 0;
            _lastResponse = "";
            SendLine(cmd);
            WaitForResponse();
            return _lastResponse.StartsWith("+OK");
        }

        private void WaitForResponse()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (_lastResponseCode == 0 && sw.ElapsedMilliseconds < 10000 && IsConnected)
                System.Threading.Thread.Sleep(10);
        }

        private void UpgradeToTls()
        {
            var ssl = new SslStream(_networkStream!, false, RemoteCertificateValidationCallback);
            ssl.AuthenticateAsClient(Server);
            _stream = ssl;
            _reader = new StreamReader(_stream, Encoding.UTF8);
            _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
        }

        private bool RemoteCertificateValidationCallback(object sender, X509Certificate? cert, X509Chain? chain, SslPolicyErrors errors)
        {
            return errors == SslPolicyErrors.None;
        }
    }
}
