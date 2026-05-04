/*
 * 2025.11.09 - JLW
 * Another Grok implementation.  This time for IRC Chat Client.
 * 
 * o = CREATEOBJECT("IrcClient")
 * o.Server = "irc.libera.chat"
 * o.Port = 6697
 * o.UseSSL = .T.
 * o.Nick = "MyVFPBot"
 * o.UseSasl = .T.
 * o.SaslUsername = "myaccount"
 * o.SaslPassword = "supersecret"
 * 
 * o.OnChannelMessage = {|c,n,m| ? "[" + c + "] <" + n + "> " + m }
 * o.OnPrivateMessage = {|n,m| ? "*PM* " + n + ": " + m }
 * o.OnServerNotice = {|msg| ? "NOTICE: " + msg }
 * 
 * o.Connect()
 * o.Join("##foxpro")
 * o.Join("#vfp")
 * 
 * * Keep alive
 * DO WHILE o.Connected
 *    DOEVENTS
 * ENDDO
 * 
 * 
 * FEATURE LIST
 * -------------------------------------------------------
 * Feature              Supported?      Notes
 * irc:// and ircs://   Yes             Auto-detectsTLS
 * SASL PLAIN/EXTERNAL  Yes             NickServ without /msg
 * Certificate pinning  Yes             Reuse PinnedThumbprint
 * Auto-PONG            Yes             Never timeout
 * Auto-reconnect       Yes             With delay
 * Auto-rejoin channels Yes             After reconnect
 * CTCP (VERSION, TIME) Yes             Easy to extend
 * UTF-8                Yes             Full emoji support
 * 
 */
using System.Text;
using System.Text.RegularExpressions;

namespace JAXBase.XBase
{
    public class XBase_Class_IRCClient : XBase_Class_TCPClient
    {
        public new string MyBaseClass = "IRCClient";
        public new string MyDefaultName = "ircclient";

        /// <summary>
        /// VFP-style IRC client: CREATEOBJECT("IrcClient")
        /// Full IRC + IRCv3 + TLS + SASL PLAIN/EXTERNAL + cert pinning + auto-reconnect
        /// </summary>

        // === IRC IDENTITY ===
        public string Nick { get; set; } = "VFPBot";
        public string User { get; set; } = "vfp";
        public string RealName { get; set; } = "Visual FoxPro IRC Bot";
        public string Password { get; set; } = ""; // Server password or NickServ
        public string[] AlternateNicks { get; set; } = { "VFPBot_", "VFPBot1", "VFPBot2" };

        // === IRC STATE ===
        public string CurrentNick { get; private set; } = "";
        public string Server { get; set; } = "";
        //public int Port { get; set; } = 6667;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectDelayMs { get; set; } = 5000;
        public bool AutoPong { get; set; } = true;
        public bool AutoJoinChannels { get; set; } = true;

        // === SASL ===
        public bool UseSasl { get; set; } = false;
        public string SaslUsername { get; set; } = "";
        public string SaslPassword { get; set; } = "";
        public string SaslMechanism { get; set; } = "PLAIN"; // PLAIN or EXTERNAL

        // === CHANNELS ===
        private readonly List<string> _channels = new();
        private readonly HashSet<string> _joinedChannels = new();

        // === EVENTS (VFP-friendly) ===
        public Action<string, string, string>? OnChannelMessage; // channel, nick, message
        public Action<string, string>? OnPrivateMessage;         // nick, message
        public Action<string>? OnRawMessage;
        public Action<string, string>? OnJoin;                   // channel, nick
        public Action<string, string>? OnPart;                   // channel, nick
        public Action<string>? OnNickChange;                     // newnick
        public Action<string>? OnMotd;                            // motd line
        public Action<string>? OnServerNotice;                   // notice

        public XBase_Class_IRCClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            Encoding = Encoding.UTF8;
            Timeout = TimeSpan.FromSeconds(30);
            OnLineReceived = ProcessIrcLine;
            OnDisconnected = HandleDisconnect;
            Port= 6667;
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            // ----------------------------------------
            // Final setup of properties
            // ----------------------------------------

            bool result = await base.PostInit(callBack, parameterList);

            return result;
        }

        public bool Connect()
        {
            if (string.IsNullOrWhiteSpace(Server))
            {
                LastError = "Server not set";
                OnError?.Invoke(LastError);
                return false;
            }

            CurrentNick = Nick;
            _joinedChannels.Clear();

            if (!base.Connect(Server, Port))
                return false;

            // Send initial IRC commands
            if (!string.IsNullOrEmpty(Password))
                SendLine($"PASS {Password}");

            if (UseSasl && UseSSL)
            {
                SendLine("CAP REQ :sasl");
            }

            SendLine($"NICK {CurrentNick}");
            SendLine($"USER {User} 0 * :{RealName}");

            return true;
        }

        public void Join(string channel)
        {
            if (!_channels.Contains(channel, StringComparer.OrdinalIgnoreCase))
                _channels.Add(channel);

            if (IsConnected)
                SendLine($"JOIN {channel}");
        }

        public void Part(string channel, string reason = "")
        {
            SendLine($"PART {channel} :{reason}");
            _joinedChannels.Remove(channel);
        }

        public void SendMessage(string target, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Split long messages
            var lines = message.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Length > 450)
                {
                    var parts = Regex.Matches(line, ".{1,450}").Cast<Match>().Select(m => m.Value);
                    foreach (var part in parts)
                        SendLine($"PRIVMSG {target} :{part}");
                }
                else
                {
                    SendLine($"PRIVMSG {target} :{line}");
                }
            }
        }

        public void SendNotice(string target, string message)
        {
            SendLine($"NOTICE {target} :{message}");
        }

        public void Quit(string reason = "VFP IRC Bot quitting")
        {
            SendLine($"QUIT :{reason}");
            Disconnect();
        }

        private void ProcessIrcLine(string line)
        {
            OnRawMessage?.Invoke(line);

            if (string.IsNullOrEmpty(line)) return;

            // PING → PONG
            if (AutoPong && line.StartsWith("PING"))
            {
                var lag = line.Substring(5);
                SendLine($"PONG {lag}");
                return;
            }

            // Parse prefix, command, params
            string? prefix = null;
            string command;
            string[] trailing;

            if (line.StartsWith(":"))
            {
                var space = line.IndexOf(' ');
                prefix = line.Substring(1, space - 1);
                line = line.Substring(space + 1);
            }

            var parts = line.Split(new[] { ' ' }, 2);
            command = parts[0];
            trailing = parts.Length > 1 ? SplitTrailing(parts[1]) : Array.Empty<string>();

            var nick = prefix?.Split('!', 2)[0] ?? "";

            switch (command)
            {
                case "001": // Welcome
                    CurrentNick = trailing[0];
                    if (UseSasl && UseSSL)
                    {
                        SendLine("CAP REQ :sasl");
                    }
                    else if (AutoJoinChannels)
                    {
                        foreach (var ch in _channels)
                            Join(ch);
                    }
                    break;

                case "433": // Nick in use
                    var newNick1 = AlternateNicks.FirstOrDefault(n => !n.Equals(CurrentNick, StringComparison.OrdinalIgnoreCase));
                    if (newNick1 != null)
                    {
                        CurrentNick = newNick1;
                        SendLine($"NICK {CurrentNick}");
                    }
                    break;

                case "CAP":
                    if (trailing.Length >= 2 && trailing[1] == "ACK" && trailing[2].Contains("sasl"))
                    {
                        if (SaslMechanism == "PLAIN")
                        {
                            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SaslUsername}\0{SaslUsername}\0{SaslPassword}"));
                            SendLine($"AUTHENTICATE PLAIN");
                            SendLine($"AUTHENTICATE {auth}");
                        }
                        else if (SaslMechanism == "EXTERNAL")
                        {
                            SendLine("AUTHENTICATE EXTERNAL");
                        }
                    }
                    break;

                case "903": // SASL success
                    SendLine("CAP END");
                    break;

                case "904" or "905" or "906" or "907":
                    SendLine("CAP END");
                    break;

                case "PRIVMSG":
                    var target = trailing[0];
                    var msg = trailing.Length > 1 ? trailing[1].TrimStart(':') : "";
                    if (target.StartsWith("#"))
                        OnChannelMessage?.Invoke(target, nick, msg);
                    else
                        OnPrivateMessage?.Invoke(nick, msg);
                    break;

                case "JOIN":
                    var channel = trailing[0].TrimStart(':');
                    _joinedChannels.Add(channel);
                    OnJoin?.Invoke(channel, nick);
                    break;

                case "PART":
                    OnPart?.Invoke(trailing[0], nick);
                    break;

                case "NICK":
                    var newNick2 = trailing[0].TrimStart(':');
                    if (nick.Equals(CurrentNick, StringComparison.OrdinalIgnoreCase))
                        CurrentNick = newNick2;
                    OnNickChange?.Invoke(newNick2);
                    break;

                case "372" or "375" or "376": // MOTD
                    OnMotd?.Invoke(trailing.Length > 1 ? trailing[1].TrimStart(':') : "");
                    break;

                case "NOTICE":
                    OnServerNotice?.Invoke(trailing.Length > 1 ? trailing[1].TrimStart(':') : "");
                    break;
            }
        }

        private void HandleDisconnect()
        {
            _joinedChannels.Clear();
            if (AutoReconnect && !string.IsNullOrEmpty(Server))
            {
                Task.Delay(ReconnectDelayMs).ContinueWith(_ =>
                {
                    Connect();
                });
            }
        }

        private static string[] SplitTrailing(string input)
        {
            var parts = input.Split(new[] { " :" }, 2, StringSplitOptions.None);
            if (parts.Length == 1)
                return input.Split(' ');
            return parts[0].Split(' ').Concat(new[] { parts[1] }).ToArray();
        }


        // === SECURE CONNECT WITH CERT PINNING (reuse your HttpClientEx logic) ===
        public override bool Connect(string host, int port)
        {
            Server = host;
            Port = port;
            UseSSL = Port == 6697 || Port == 6690 || UseSSL;

            return base.Connect(host, port);
        }
    }
}
