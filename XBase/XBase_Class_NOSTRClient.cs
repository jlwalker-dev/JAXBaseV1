/*
 * 2026-05-22 - JLW
 *      GROK once again had trouble creating a JAXBase class so I'm touching up the many
 *      simple errors and omissions in the original output and adding more comments.
 *      
 *      For some reason, GROK became stupid yesterday.  I've been giving an almost identical
 *      set of prompts for building classes and up to the 20th the process just kept
 *      getting faster and cleaner.  Since Yesterday? Back to last month's SuperGROK where
 *      I eventually get good C# code, but I have to actually do all the integration into the
 *      JAXBase ecosystem.
 *      
 *      Eventually get good C# code?  Yeah, I can eventually get good code without falling into 
 *      a doom loop (which was a reocurring problem up to the end of February), but yesterday 
 *      and today I keep being forced to paste error after error to the chat, as if GROK would 
 *      guess at what was right and then give me the old "that error is caused by the fact 
 *      that XXX is not a function of...".
 *      
 *      I'm wondering if GROK lost some of it's processing power when Anthropic leased out
 *      Elon's big data centers on the KY/TN border?
 *      
 *      
 */

using JAXBase.Core;
using Nostr.Client.Client;
using Nostr.Client.Communicator;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Requests;
using ZXing;

namespace JAXBase.XBase
{
    internal class XBase_Class_NOSTRClient : XBase_Avalonia, IDisposable
    {
        public int HistoryMax { get; set; } = 100;

        public List<NostrHistoryEntry> History { get; private set; } = [];

        private readonly List<List<string>> _pendingTags = new List<List<string>>();

        private NostrWebsocketClient? _nostrClient;
        private NostrWebsocketCommunicator? _communicator;

        // Supporting inner classes
        public List<string> Relays { get; private set; } = [];
        public string RelayRegionFilter { get; set; } = "";
        public string SupportedNIPsFilter { get; set; } = "";

        public XBase_Class_NOSTRClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();
        }


        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);
            //SetStatus(0, "Initialized");
            return result;
        }


        // ===================================================================
        // Property Handling
        // ===================================================================
        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower().Trim();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "active":
                        returnToken.Element.Value = UserProperties["connected"].Element.Value;
                        break;
                    case "historymax":
                        returnToken.Element.Value = HistoryMax;
                        break;
                    case "relays":
                        returnToken.Element.Value = string.Join(";", Relays);
                        break;
                    default:
                        returnToken.CopyFrom(UserProperties[propertyName]);
                        break;
                }
            }
            else
                result = 1559; // Property not found

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
            propertyName = propertyName.ToLower().Trim();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "kind":
                        if (tk.Element.Type.Equals("N"))
                        {
                            if (tk.AsInt() >= 0)
                                objValue = tk.AsInt();
                            else
                                result = 41;
                        }
                        else
                            result = 11; // Type mismatch
                        break;

                    case "privatekey":
                        if (tk.Element.Type.Equals("C"))
                        {
                            string privateKeyString = tk.AsString();
                            NostrPrivateKey privateKey;

                            if (privateKeyString.StartsWith("nsec1"))
                            {
                                privateKey = NostrPrivateKey.FromBech32(privateKeyString);
                                UserProperties["privatekey"].Element.Value = privateKeyString;

                                // Save the public key
                                NostrPublicKey publicKey = privateKey.DerivePublicKey();
                                UserProperties["publickey"].Element.Value = publicKey.Bech32;
                            }
                            else
                                result = 8226;
                        }
                        else
                            result = 11;

                        break;

                    case "relays":
                        if (tk.Element.Type.Equals("C"))
                            LoadRelays(tk.AsString().Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                        else
                            result = 11;
                        break;

                    case "relayregionfilter":
                    case "supportednipsfilter":
                    case "filterauthors":
                    case "filterhashtags":
                    case "filterreplythreads":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        break;

                    case "historymax":
                        if (tk.Element.Type.Equals("N"))
                        {
                            if (tk.AsInt() >= 0)
                                HistoryMax = tk.AsInt();
                            else
                                result = 41;
                        }
                        else
                            result = 11; // Type mismatch
                        break;

                    default:
                        result = 1;
                        break;
                }

                if (result < 11 && result != 9)
                    UserProperties[propertyName].Element.Value = objValue;
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, $"{result}|{propertyName}|", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                result = -1;
            }

            return result;
        }


        // ===================================================================
        // Method Handling - DoDefault
        // ===================================================================
        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower().Trim();

            switch (methodName)
            {
                case "connect":
                    result = await ConnectAsync();
                    break;

                case "disconnect":
                    result = await DisconnectAsync();
                    break;

                case "sendnostrevent":
                    // Full implementation would parse parameters
                    result = await SendNOSTREventAsync("");
                    break;

                case "generateprivatekey":
                    result = GeneratePrivateKey();
                    break;

                case "getavailablerelays":
                    result = await GetAvailableRelaysAsync();
                    // Store result in a property or handle via callback
                    break;

                case "addrelay":
                    // Parameter would be passed in full parser
                    break;

                case "removerelay":
                    // Parameter handling
                    break;

                default:
                    await base.DoDefault(methodName);
                    break;
            }

            return result;
        }


        // ===================================================================
        // JAXBase Standard Method/Event/Property Lists
        // ===================================================================
        public override string[] JAXMethods() =>
            [
            "addproperty", "addmention", "addhashtag", "addrelay", "addreply", "connect", "disconnect",
            "generateprivatekey", "getavailablerelays", "removerelay", "sendnostrevent"
            ];


        public override string[] JAXEvents() =>
            [
            "sendsuccessful", "sendfailed", "receivesuccessful", "relaystatuschanged",
            "validationerror", "init", "destroy", "statuschanged"
            ];


        public override string[] JAXProperties() =>
            [
            "active,l!,false",
            "baseclass,C!,JAXNostrClient",
            "class,C!,","classlibary,c,","comment,c,","connected,l,false",
            "filterauthors,C,","filterhashtags,C,","filterreplythreads,C,",
            "historymax,N,100","host,c,wss://relay.damus.io",
            "kind,n,1",
            "parent,o$,","parentclass,C,","privatekey,C,","publickey,C,",
            "relayregionfilter,C,","relays,C,", "relaystatus,c,",
            "supportednipsfilter,C,",
            "tag,c,"
            ];


        // ===================================================================
        // Core NOSTR Methods
        // ===================================================================
        public async Task<int> ConnectAsync()
        {
            int result = 0;
            if (UserProperties["connected"].AsBool() == false)
            {
                try
                {
                    string keyInput = UserProperties["privatekey"].Element.Value?.ToString() ?? "";

                    if (keyInput.StartsWith("nsec1", StringComparison.OrdinalIgnoreCase) || (keyInput.Length == 64 && keyInput.All(c => Uri.IsHexDigit(c))))
                    {
                        // Valid format, proceed
                        NostrPrivateKey privateKey = NostrPrivateKey.FromBech32(keyInput);

                        string primaryUrl = Relays.FirstOrDefault() ?? "wss://relay.damus.io";
                        if (string.IsNullOrWhiteSpace(UserProperties["host"].AsString()) == false)
                            primaryUrl = UserProperties["host"].AsString();

                        _communicator = new NostrWebsocketCommunicator(new Uri(primaryUrl));

                        // Correct constructor: communicator + ILogger (use null for no logging)
                        _nostrClient = new NostrWebsocketClient(_communicator, null);

                        await _communicator.Start();
                        UserProperties["connected"].Element.Value = true;

                        OnRelayStatusChanged("System", "Connected");

                        // Setup subscription for incoming events
                        SetupEventSubscription();
                    }
                    else
                    {
                        string msg = "Invalid private key format. Must be nsec1.";
                        _AddError(1003, 0, msg, "ConnectAsync");
                        AddToHistory("error", msg);
                        result = 8226;
                    }
                }
                catch (Exception ex)
                {
                    // TODO - 
                    string msg = $"Nostr connection error: {ex.Message}";
                    _AddError(1000, 0, msg, "ConnectAsync");
                    AddToHistory("error", msg);
                    result = 8222;
                }
            }

            return result;
        }


        public async Task<int> DisconnectAsync()
        {
            int result = 0;

            try
            {
                if (_communicator != null)
                {
                    // The library primarily relies on disposal for clean shutdown
                    await _communicator.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Normal closure");           // Try synchronous Stop first
                    _communicator.Dispose();
                    _communicator = null;
                }

                if (_nostrClient != null)
                {
                    _nostrClient.Dispose();
                    _nostrClient = null;
                }

                UserProperties["connected"].Element.Value = false;
                OnRelayStatusChanged("System", "Disconnected");
            }
            catch (Exception ex)
            {
                // TODO - 
                string msg = $"Disconnect error: {ex.Message}";
                _AddError(1002, 0, msg, "disconnect");
                AddToHistory("error", msg);
                result = 8224;
            }

            return result;
        }


        ///// <summary>
        ///// Queries a specific relay for kind:10002 (NIP-65) relay list metadata events
        ///// and returns discovered relays as a semi-colon delimited string.
        ///// </summary>
        public async Task<int> GetAvailableRelaysAsync()
        {
            int result = 0;

            string relayUrl = UserProperties["host"].AsString();
            int maxEvents = 30;

            if (string.IsNullOrWhiteSpace(relayUrl))
            {
                _AddError(1003, 0, "Relay URL is required", "GetAvaliableRelaysAsync");
                result = 8215;
            }
            else
            {
                var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                NostrWebsocketCommunicator? tempCommunicator = null;
                NostrWebsocketClient? tempClient = null;

                try
                {
                    tempCommunicator = new NostrWebsocketCommunicator(new Uri(relayUrl));
                    tempClient = new NostrWebsocketClient(tempCommunicator, null);

                    // Subscribe to RelayListMetadata response event
                    tempClient.Streams.EventStream.Subscribe(response =>
                    {
                        var ev = response.Event;
                        if (ev?.Kind == NostrKind.RelayListMetadata) // NIP-65 Relay List
                        {
                            if (ev.Tags is not null)
                            {
                                foreach (var tag in ev.Tags)
                                {
                                    if (tag.TagIdentifier?.Equals("r", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        foreach (string s in tag.AdditionalData)
                                        {
                                            if (s.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                                                discovered.Add(s);
                                        }
                                    }
                                }
                            }
                        }
                    });

                    await tempCommunicator.Start();

                    // Send raw REQ message (this is the common pattern in this library)
                    var subscriptionId = Guid.NewGuid().ToString("N");
                    var req = new object[] { "REQ", subscriptionId, new { kinds = new[] { 10002 }, limit = maxEvents } };

                    string jsonReq = System.Text.Json.JsonSerializer.Serialize(req);
                    tempCommunicator.Send(jsonReq);

                    await Task.Delay(5000); // Give time to receive events

                    Program.CurrentApp.ReturnValue.Element.Value = string.Join(";", discovered);
                }
                catch (Exception ex)
                {
                    string msg = $"DiscoverRelaysFromAsync error: {ex.Message}";
                    _AddError(1004, 0, msg, "GetAvailableRelaysAsync");
                    AddToHistory("error", msg);
                    result = 8216;
                }
                finally
                {
                    tempCommunicator?.Dispose();
                    tempClient?.Dispose();
                }
            }

            return result;
        }



        // Fallback to your current reliable list
        //var list = new List<string>
        //    {
        //        "wss://relay.damus.io",
        //        "wss://nos.lol",
        //        "wss://relay.primal.net",
        //        "wss://nostr.wine",
        //        "wss://relay.snort.social",
        //        "wss://relay.nostr.band"


        public void LoadRelays(string[] relayUrls)
        {
            Relays.Clear();
            foreach (var url in relayUrls)
                AddRelay(url);
        }


        public void AddRelay(string relayUrl)
        {
            if (Relays.Contains(relayUrl) == false)
                Relays.Add(relayUrl);
        }


        public async Task<int> SendNOSTREventAsync(string content)
        {
            int result = 0;

            if (string.IsNullOrWhiteSpace(content) && UserProperties["kind"].AsInt() != 10002)
                result = 8227;
            else
            {
                if (_nostrClient == null)
                    result = await ConnectAsync();

                if (result == 0)
                {
                    // Sign the event
                    string keyInput = UserProperties["privatekey"].AsString();
                    var privateKey = keyInput.StartsWith("nsec1", StringComparison.OrdinalIgnoreCase) ? NostrPrivateKey.FromBech32(keyInput) : NostrPrivateKey.FromHex(keyInput);

                    // Build tags first
                    NostrEventTags tagCollection = new();

                    foreach (var tag in _pendingTags)
                    {
                        switch (tag.Count)
                        {
                            case 1:
                                tagCollection.Append(new NostrEventTag(tag[0]));
                                break;

                            case 2:
                                tagCollection.Append(new NostrEventTag(tag[0], tag[1]));
                                break;

                            case 3:
                                tagCollection.Append(new NostrEventTag(tag[0], tag[1], tag[2]));
                                break;

                            case 4:
                                tagCollection.Append(new NostrEventTag(tag[0], tag[1], tag[2], tag[3]));
                                break;

                            default:
                                // skip it
                                break;
                        }
                    }

                    NostrKind kind = UserProperties["kind"].AsInt() switch
                    {
                        1 => NostrKind.ShortTextNote,
                        2 => NostrKind.RecommendRelay,
                        3 => NostrKind.Contacts,
                        4 => NostrKind.EncryptedDm,
                        5 => NostrKind.EventDeletion,
                        6 => NostrKind.Reserved,
                        7 => NostrKind.Reaction,
                        8 => NostrKind.BadgeAward,
                        40 => NostrKind.ChannelCreation,
                        42 => NostrKind.ChannelMessage,
                        43 => NostrKind.ChannelHideMessage,
                        44 => NostrKind.ChanelMuteUser,
                        10002 => NostrKind.RelayListMetadata,
                        22242 => NostrKind.ClientAuthentication,
                        24133 => NostrKind.NostrConnect,
                        _ => NostrKind.Metadata
                    };

                    // Create the event
                    var ev = new NostrEvent
                    {
                        Kind = kind,
                        CreatedAt = DateTime.UtcNow,
                        Content = content,
                        Tags = new Nostr.Client.Messages.NostrEventTags(tagCollection)
                    };

                    var signedEvent = ev.Sign(privateKey);

                    // Send to relay
                    _nostrClient!.Send(new NostrEventRequest(signedEvent));

                    // TODO - Raise success event (implement your JAXBase event system here)
                    // SendSuccessful?.Invoke...
                }
            }

            return result;
        }

        /// <summary>
        /// Processes a received NOSTR event. Useful for handling replies and other incoming events.
        /// </summary>
        public void ProcessReceivedEvent(NostrEvent receivedEvent, string relayUrl = "")
        {
            if (receivedEvent == null)
                return;

            try
            {
                AppIO.DebugLog(">>> Processing response");

                bool isReply = false;
                string rootEventId = "";
                string replyEventId = "";

                if (receivedEvent.Tags is not null)
                {
                    // Check for NIP-10 reply structure
                    foreach (var tag in receivedEvent.Tags)
                    {
                        if (tag.TagIdentifier?.Equals("e", StringComparison.OrdinalIgnoreCase) == true && tag.AdditionalData.Count() > 0)
                        {
                            string marker = tag.AdditionalData.Count() > 2 ? tag.AdditionalData[2] : "";

                            if (marker.Equals("root", StringComparison.OrdinalIgnoreCase))
                            {
                                rootEventId = tag.AdditionalData[0];
                                AppIO.DebugLog($">>> RootID = {rootEventId}");
                            }
                            else if (marker.Equals("reply", StringComparison.OrdinalIgnoreCase))
                            {
                                AppIO.DebugLog($">>> Reply = {rootEventId}");
                                replyEventId = tag.AdditionalData[0];
                                isReply = true;
                            }
                        }
                    }
                }





                // You can raise different JAXBase events based on type
                if (isReply)
                {
                    // TODO: Raise a specific reply event if you want
                    AppIO.DebugLog($">>> LastReplyEventID = {rootEventId}");
                    UserProperties["lastreplyeventid"].Element.Value = replyEventId;
                    _CallMethod("receivesuccessful").Wait();   // or a dedicated "replyreceived"


                    //  reply
                    var ev = receivedEvent.Kind;
                    if (ev == NostrKind.RelayListMetadata) // NIP-65 Relay List
                    {
                    }
                }
                else
                {
                    // Normal received event
                    _CallMethod("receivesuccessful").Wait();
                }

                // Store in history if needed
                if (receivedEvent.Id is not null)
                    AddToHistory(receivedEvent.Id, $"Received event Kind {receivedEvent.Kind} from {relayUrl}");
            }
            catch (Exception ex)
            {
                _AddError(1050, 0, $"ProcessReceivedEvent error: {ex.Message}", "ProcessReceivedEvent");
            }
        }

        // ===================================================================
        // Event Subscription & Processing
        // ===================================================================
        private void SetupEventSubscription()
        {
            if (_nostrClient == null) return;

            _nostrClient.Streams.EventStream.Subscribe(response =>
            {
                if (response?.Event != null)
                {
                    string sourceRelay = response.CommunicatorName?.ToString() ?? "unknown";
                    ProcessReceivedEvent(response.Event, sourceRelay);
                }
            });
        }

        /// <summary>
        /// Generates a new NOSTR private key
        /// </summary>
        /// <param name="asNsec">If true, returns nsec1... format (recommended)</param>
        public int GeneratePrivateKey()
        {
            int result = 0;

            try
            {
                NostrPrivateKey privateKey = NostrPrivateKey.GenerateNew();
                Program.CurrentApp.ReturnValue.Element.Value = privateKey.Bech32;
            }
            catch (Exception ex)
            {
                result = 8225;
                string msg = $"Failed to generate private key: {ex.Message}";
                _AddError(1001, 0, msg, "GeneratenewPrivateKey");
                AddToHistory("error", msg);

                Program.CurrentApp.ReturnValue.Element.Value = "";
            }

            return result;
        }


        private void OnRelayStatusChanged(string relay, string status)
        {
            UserProperties["relaystatus"].Element.Value = $"{relay}:{status}";
            _CallMethod("relaystatuschanged").Wait();
        }


        public void RemoveRelay(string relayUrl)
        {
            Relays.Remove(relayUrl);
        }

        public int SaveRelays(string fileName)
        {
            int result = 0;

            return result;
        }

        public int LoadRelays(string fileName)
        {
            int result = 0;

            return result;
        }



        // Tag helpers
        /// <summary>
        /// Adds a mention (p tag) to the next message sent
        /// </summary>
        public void AddMention(string pubkey)
        {
            if (!string.IsNullOrWhiteSpace(pubkey))
                _pendingTags.Add(new List<string> { "p", pubkey });
        }

        /// <summary>
        /// Adds a hashtag (t tag) to the next message sent
        /// </summary>
        public void AddHashtag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                if (!tag.StartsWith("#"))
                    tag = "#" + tag;
                _pendingTags.Add(new List<string> { "t", tag.TrimStart('#') });
            }
        }

        /// <summary>
        /// Adds a reply/thread reference (e tag) to the next message sent
        /// </summary>
        public void AddReply(string eventId, string marker = "root")
        {
            if (!string.IsNullOrWhiteSpace(eventId))
                _pendingTags.Add(new List<string> { "e", eventId, "", marker });
        }

        /// <summary>
        /// Clears all pending tags (called automatically after sending)
        /// </summary>
        public void ClearPendingTags()
        {
            _pendingTags.Clear();
        }


        // History handling (modeled after TCPClient)
        private void AddToHistory(string rawEvent, string reason)
        {
            if (History.Count >= HistoryMax)
                History.RemoveAt(0);

            History.Add(new NostrHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                RelayUrl = UserProperties["host"].AsString(),
                RawEvent = rawEvent,
                Reason = reason
            });

            SetStatus($"History entry added: {reason}");
        }


        private void SetStatus(string message)
        {
            // Standard status handling
            UserProperties["status"].Element.Value = message;
        }
    }
}

