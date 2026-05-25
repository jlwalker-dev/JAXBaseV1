using JAXBase.Core;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_MQTTClient : XBase_Avalonia, IDisposable
    {
        private IManagedMqttClient? _mqttClient;
        private readonly List<MqttApplicationMessageReceivedEventArgs> _messagesWaiting = new List<MqttApplicationMessageReceivedEventArgs>();
        private readonly object _messagesLock = new object();

        public int historyMax = 100;
        public List<WebHistory> history = [];

        public new string MyBaseClass = "MQTTClient";
        public new string MyDefaultName = "mqttclient";

        public XBase_Class_MQTTClient(JAXObjectWrapper jow, string name) : base(jow, name)
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

        public override string[] JAXEvents() =>
            ["connected", "disconnected", "messagereceived", "error", "statuschanged"];

        public override string[] JAXMethods() =>
            ["connect", "disconnect", "publish", "subscribe", "unsubscribe", "clearmessages"];


        public override string[] JAXProperties() =>
            [
            "autoreconnect,l,true", "autoreconnectdelayseconds,n,5", "broker,c,", "cleansession,l,",
            "keepaliveseconds,n,60","lasterror,c,", "messageswaiting,n,0", "port,n,0", "password,c,",
            "secure,l,false", "status,c,", "username,c,"
            ];


        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            propertyName = propertyName.ToLower().Trim();
            int result = 0;

            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName) && UserProperties[propertyName].Protected)
                result = 3026;
            else if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "broker":
                        if (tk.Element.Type.Equals("C"))
                            UserProperties["Broker"].Element.Value = tk.AsString();
                        else
                            result = 11;
                        break;

                    case "port":
                        if (tk.Element.Type.Equals("N"))
                            UserProperties["Port"].Element.Value = tk.AsInt();
                        else
                            result = 11;
                        break;

                    case "username":
                        if (tk.Element.Type.Equals("C"))
                            UserProperties["Username"].Element.Value = tk.AsString();
                        else
                            result = 11;
                        break;

                    case "password":
                        if (tk.Element.Type.Equals("C"))
                            UserProperties["Password"].Element.Value = tk.AsString();
                        else
                            result = 11;
                        break;

                    case "secure":
                        if (tk.Element.Type.Equals("L"))
                            UserProperties["Secure"].Element.Value = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "cleansession":
                        if (tk.Element.Type.Equals("L"))
                            UserProperties["CleanSession"].Element.Value = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "keepaliveseconds":
                        if (tk.Element.Type.Equals("N"))
                            UserProperties["KeepAliveSeconds"].Element.Value = tk.AsInt();
                        else
                            result = 11;
                        break;

                    case "autoreconnect":
                        if (tk.Element.Type.Equals("N"))
                            UserProperties["AutoReconnect"].Element.Value = tk.AsBool();
                        else
                            result = 11;
                        break;

                    case "autoreconnectdelayseconds":
                        if (tk.Element.Type.Equals("N"))
                            UserProperties["AutoReconnectDelaySeconds"].Element.Value = tk.AsInt();
                        else
                            result = 11;
                        break;
                }
            }

            return result;
        }

        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();

            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                switch (propertyName)
                {
                    case "messageswaiting":
                        lock (_messagesLock)
                        {
                            returnToken.Element.Value = _messagesWaiting.Count;
                        }
                        break;

                    default:
                        returnToken.Element.Value = UserProperties[propertyName].Element.Value;
                        break;
                }
            }

            return returnToken;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower().Trim();

            try
            {
                switch (methodName)
                {
                    case "connect":
                        result = await ConnectAsync();   // Blocking for JAXBase compatibility; internal is async
                        break;

                    case "disconnect":
                        result = await DisconnectAsync();
                        break;

                    case "publish":
                        result = await PublishAsync("", "", 0, true);
                        break;

                    case "subscribe":
                        result = await SubscribeAsync("");
                        break;

                    case "unsubscribe":
                        result = await UnsubscribeAsync("");
                        break;

                    case "clearmessages":
                        ClearMessages();
                        break;

                    default:
                        result = await base.DoDefault(methodName);
                        break;
                }
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        // ==================== MQTT Core Methods ====================
        private async Task<int> ConnectAsync()
        {
            int result = 0;

            try
            {
                if (_mqttClient?.IsConnected == true)
                    result = 8223;
                else
                {
                    var factory = new MqttFactory();
                    _mqttClient = factory.CreateManagedMqttClient();

                    _mqttClient.ConnectedAsync += OnConnectedAsync;
                    _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
                    _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

                    var clientOptions = new MqttClientOptionsBuilder()
                        .WithClientId(UserProperties["clientid"].AsString())
                        .WithTcpServer(UserProperties["broker"].AsString(), UserProperties["port"].AsInt())
                        .WithCleanSession(UserProperties["cleansession"].AsBool())
                        .WithKeepAlivePeriod(TimeSpan.FromSeconds(UserProperties["keepaliveseconds"].AsInt()));

                    if (!string.IsNullOrEmpty(UserProperties["username"].AsString()))
                        clientOptions.WithCredentials(UserProperties["username"].AsString(), UserProperties["password"].AsString());

                    if (UserProperties["secure"].AsBool())
                        clientOptions.WithTlsOptions(o => { });

                    var managedOptions = new ManagedMqttClientOptionsBuilder()
                        .WithClientOptions(clientOptions.Build())
                        .WithAutoReconnectDelay(TimeSpan.FromSeconds(UserProperties["autoreconnectdelayseconds"].AsInt()))
                        .Build();

                    await _mqttClient.StartAsync(managedOptions);
                    SetStatus(0, "Connecting...");
                }
            }
            catch (Exception ex)
            {
                SetStatus(13, $"Connect failed: {ex.Message}");
                result = 8222;
            }

            return result;
        }

        private async Task<int> DisconnectAsync()
        {
            int result = 0;
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.StopAsync();
                    SetStatus(210, "Disconnected");
                }
                else
                    result = 8221;
            }
            catch (Exception ex)
            {
                SetStatus(211, $"Disconnect failed: {ex.Message}");
                result = 8224;
            }

            return result;
        }

        private async Task<int> PublishAsync(string topic, string payload, int qos = 0, bool retain = false)
        {
            int result = 0;
            try
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                {
                    SetStatus(301, "Not connected");
                    result = 8221;
                }
                else
                {
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(Encoding.UTF8.GetBytes(payload))
                        .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                        .WithRetainFlag(retain)
                        .Build();

                    await _mqttClient.EnqueueAsync(message);
                }
            }
            catch (Exception ex)
            {
                SetStatus(302, $"Publish failed: {ex.Message}");
                result = 8206;
            }

            return result;
        }
        

        private async Task<int> SubscribeAsync(string topic, int qos = 0)
        {
            int result = 0;
            try
            {
                if (_mqttClient == null)
                    result = 8221;
                else
                {
                    await _mqttClient.SubscribeAsync(
                     [
                        new MqttTopicFilterBuilder()
                        .WithTopic(topic)
                        .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                        .Build()
                    ]);

                    SetStatus(400, $"Subscribed to {topic}");
                }
            }
            catch (Exception ex)
            {
                SetStatus(401, $"Subscribe failed: {ex.Message}");
                result = 8206;
            }

            return result;
        }

        private async Task<int> UnsubscribeAsync(string topic)
        {
            int result = 0;
            try
            {
                if (_mqttClient == null)
                    result = 8221;
                else
                {
                    await _mqttClient.UnsubscribeAsync(topic);
                    SetStatus(410, $"Unsubscribed from {topic}");
                }
            }
            catch (Exception ex)
            {
                SetStatus(411, $"Unsubscribe failed: {ex.Message}");
                result = 8206;
            }

            return result;
        }

        private void ClearMessages()
        {
            lock (_messagesLock)
            {
                _messagesWaiting.Clear();
            }
        }

        // ==================== Event Handlers ====================

        private Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            SetStatus(100, "Connected");
            _CallMethod("connected").Wait();
            return Task.CompletedTask;
        }

        private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            SetStatus(110, "Disconnected");
            _CallMethod("disconnected").Wait();
            return Task.CompletedTask;
        }

        private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            lock (_messagesLock)
            {
                _messagesWaiting.Add(arg);
            }

            _CallMethod("messagereceived").Wait();
            return Task.CompletedTask;
        }

        // Helper to mimic TCPClient SetStatus
        private void SetStatus(int statuscode, string message)
        {
            WebHistory webHistory = new()
            {
                URL = "",
                Status = statuscode,
                Content = message
            };

            AppIO.DebugLog($"Status: {statuscode} - {message}");
            history.Insert(0, webHistory);
            if (historyMax > 0 && history.Count > historyMax)
                history.RemoveAt(history.Count - 1);
        }
        protected override void Dispose(bool disposing)
        {
            _mqttClient?.Dispose();
            base.Dispose(disposing);
        }
    }
}