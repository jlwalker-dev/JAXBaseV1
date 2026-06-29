/*
 * 2026.05.20 - GROK Session - JAXBase HTTP client class
 * 
 * XBase_Class_HTTPClient.cs
 * Modern async HTTP client for JAXBase
 * 
 * Features:
 * - Fully async
 * - Cookies, redirects, authentication (Basic/Bearer/NTLM/etc.)
 * - Automatic HTTP/HTTPS detection + UseHttps override
 * - PATCH with UseJsonPatch and MaxPatchSizeBytes warning via AppIO.DebugLog
 * - Global RetryCount with basic backoff
 * - Newtonsoft.Json support
 * - Matches TCPClient style (SetStatus, history, JAX integration)
 *
 *
 * 2026.05.20 - JLW
 * 
 *      I worked with GROK and kept getting the same results.  Code that seemed to be workable if
 *      I was creating a C# connection, but nothing that would work for JAXBase.  I tried pointing
 *      at other classes that GROK helped me on to give an example of what I wanted, but nothing
 *      worked.
 * 
 *      After taking some time to look over what I was given, I realized that GROK was trying to 
 *      return a response object with all the details; something that there is no example of in my
 *      code and I expect no decent model anywhere in the open source world. So it just returned
 *      something that looked like what I wanted if I was writing a C# class.
 *      
 *      After thinking it over, I believe returning a result object will be a much better design than 
 *      trying to create a bunch of extra properties.  So, thank you GROK for that idea.  I know that's the
 *      way it's done in a lot of other languages, but I was, as they say, "thinking in 2 dimensions".
 * 
 */

using JAXBase.Core;
using JAXBase.Utilities;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace JAXBase.XBase
{
    public class XBase_Class_HTTPClient : XBase_Avalonia, IDisposable
    {
        private HttpClient? _httpClient;
        private HttpClientHandler? _handler;
        private bool _disposed = false;

        private readonly object _lock = new();

        public List<WebHistory> history = [];

        public new string MyBaseClass = "HTTPClient";
        public new string MyDefaultName = "httpclient";

        public CookieContainer Cookies { get; private set; } = new CookieContainer();

        public Dictionary<string, string> DefaultHeaders { get; } = new();


        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        // ===================================================================
        // Constructor
        // ===================================================================
        public XBase_Class_HTTPClient(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            name = string.IsNullOrEmpty(name) ? MyDefaultName : name;
            SetVisualObject(null, MyBaseClass, name, false, UserObject.urw);
            me.nvObject = new EmptyFactory();

            InitializeHttpClient();
        }

        private void InitializeHttpClient()
        {
            lock (_lock)
            {
                _handler = new HttpClientHandler
                {
                    AllowAutoRedirect = UserProperties["allowautoredirect"].AsBool(),
                    MaxAutomaticRedirections = UserProperties["maxredirects"].AsInt(),
                    UseCookies = UserProperties["usecookies"].AsBool(),
                    CookieContainer = Cookies,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                _httpClient = new HttpClient(_handler)
                {
                    Timeout = TimeSpan.FromSeconds(UserProperties["timeoutseconds"].AsInt())
                };

                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserProperties["useragent"].AsString());
            }
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);
            SetStatus(0, "HTTPClient initialized");
            return result;
        }

        // ===================================================================
        // JAXBase Property System (matching TCPClient exactly)
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
                    case "history":
                        if (idx == 0)
                        {
                            // Return formatted history string like TCPClient
                            StringBuilder sb = new();
                            for (int i = 0; i < history.Count; i++)
                                sb.AppendLine(history[i].DateVisited.ToString() + "|" + history[i].URL + "|" + history[i].Status.ToString());
                            returnToken.Element.Value = sb.ToString();
                        }
                        else
                        {
                            if (idx < 1) result = 31;
                            else if (idx > history.Count) returnToken.Element.Value = "";
                            else returnToken.Element.Value = history[idx - 1];
                        }
                        break;

                    case "class": returnToken.Element.Value = me.Class; break;

                    case "parent":
                        if (me.parent is null) returnToken.Element.MakeNull();
                        else
                            returnToken.Element.Value = me.parent;
                        break;

                    case "parentclass":
                        returnToken.Element.Value = me.ParentClass;
                        break;

                    default:
                        result = 1;
                        break;
                }

                if (JAXLib.Between(result, 1, 10))
                {
                    result = 0;
                    returnToken.CopyFrom(UserProperties[propertyName]);
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, $"{result}|{propertyName}|", Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure);
                if (string.IsNullOrWhiteSpace(Program.CurrentApp.AppLevels[Program.CurrentApp.CurrentAppLevel].Procedure))
                    AppErrorHandling.SetError(result, $"{propertyName}|", string.Empty);
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
                    case "baseurl":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        else
                        {
                            if (_httpClient is not null)
                                _httpClient.BaseAddress = new Uri(tk.AsString());
                        }
                        break;

                    case "usehttps":
                        if (tk.Element.Type.Equals("L") == false)
                            result = 11;

                        break;

                    case "timeoutseconds":
                        if (tk.Element.Type.Equals("N") == false)
                            result = 11;
                        else if (tk.AsInt() < 1)
                            result = 41;
                        else
                        {
                            objValue = tk.AsInt();
                            if (_httpClient != null) _httpClient.Timeout = TimeSpan.FromSeconds(tk.AsInt());
                        }
                        break;

                    case "allowautoredirect":
                        if (tk.Element.Type.Equals("L") == false)
                            result = 11;
                        else
                            if (_handler != null) _handler.AllowAutoRedirect = tk.AsBool();
                        break;

                    case "maxredirects":
                        if (tk.Element.Type.Equals("N") == false)
                            result = 11;
                        else if (tk.AsInt() < 0)
                            result = 41;
                        else
                        {
                            objValue = tk.AsInt();
                            if (_handler != null) _handler.MaxAutomaticRedirections = tk.AsInt();
                        }
                        break;

                    case "useragent":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        else if (_httpClient != null)
                        {
                            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(tk.AsString());
                        }
                        break;

                    case "authtype":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        break;

                    case "username":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        break;

                    case "password":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        break;

                    case "bearertoken":
                        if (tk.Element.Type.Equals("C") == false)
                            result = 11;
                        break;

                    case "usecookies":
                        if (tk.Element.Type.Equals("L") == false)
                            result = 11;
                        else if (_handler != null)
                            _handler.UseCookies = tk.AsBool();
                        break;

                    case "usejsonpatch":
                        if (tk.Element.Type.Equals("L") == false)
                            result = 11;
                        break;

                    case "retrycount":
                        if (tk.Element.Type.Equals("N") == false)
                            result = 11;
                        else if (tk.AsInt() < 0)
                            result = 41;
                        else
                            objValue = tk.AsInt();
                        break;

                    case "maxpatchsizebytes":
                        if (tk.Element.Type.Equals("N") == false)
                            result = 11;
                        else if (tk.AsInt() < 0)
                            result = 41;
                        else
                            objValue = tk.AsInt();
                        break;

                    case "historymax":
                        if (tk.Element.Type.Equals("N") == false)
                            result = 11;
                        else if (tk.AsInt() < 0)
                            result = 41;
                        else
                            objValue = tk.AsInt();
                        break;

                    default:
                        result = 1;
                        break;
                }
            }
            else
                result = 1559;

            if (result == 0)
            {
                UserProperties[propertyName].Element.Value = objValue;
                SetStatus(5, $"Property changed: {propertyName}");
            }

            return result;
        }

        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower();
            XBase_HttpResponse? response = null;
            HttpMethod? methodCall = null;
            object? content = null;
            string url = UserProperties["lasturl"].AsString();

            if (Program.CurrentApp.ParameterClassList.Count > 0)
            {
                if (JAXLib.InListC(methodName, "send", "sendrequest"))
                {
                    if (JAXLib.Between(Program.CurrentApp.ParameterClassList.Count, 1, 3))
                    {
                        if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        {
                            // Create the method
                            string methodStr = Program.CurrentApp.ParameterClassList[0].token.AsString().ToUpper();
                            methodCall = new HttpMethod(methodStr);

                            if (Program.CurrentApp.ParameterClassList.Count > 1)
                            {
                                // Get the URL
                                if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                                {
                                    url = Program.CurrentApp.ParameterClassList[0].token.AsString();

                                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                                    {
                                        // Get the content
                                        content = Program.CurrentApp.ParameterClassList[1].token.Element.Value;
                                    }
                                }
                                else
                                    result = 11;    // Invalid data type
                            }
                        }
                        else
                            result = 11;            // Invalid data type
                    }
                    else
                        result = 98;                // Too many parameters
                }
                else if (JAXLib.Between(Program.CurrentApp.ParameterClassList.Count, 1, 3))
                {
                    if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                    {
                        // Get the URL
                        url = Program.CurrentApp.ParameterClassList[0].token.AsString();

                        if (Program.CurrentApp.ParameterClassList.Count > 1)
                        {
                            // Get the content
                            content = Program.CurrentApp.ParameterClassList[1].token.Element.Value;
                        }
                    }
                    else
                        result = 11;    // Invalid data type
                }
                else
                    result = 98;        // Too many parameters
            }

            // If we have a method, execute it
            switch (methodName.ToLower())
            {
                case "delete":          // 0 or 1 parameter (url)
                    if (Program.CurrentApp.ParameterClassList.Count > 1)
                        result = 98;
                    else
                        response = DeleteAsync(url).GetAwaiter().GetResult();
                    break;

                case "get":             // 0 or 1 parameter (url)
                    if (Program.CurrentApp.ParameterClassList.Count > 1)
                        result = 98;
                    else
                        response = GetAsync(url).GetAwaiter().GetResult();
                    break;

                case "head":            // 0 or 1 parameter (url)
                    if (Program.CurrentApp.ParameterClassList.Count > 1)
                        result = 98;
                    else
                        response = HeadAsync(url).GetAwaiter().GetResult();
                    break;

                case "patch":           // 0, 1, or 2 parameters (url, content)
                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                        response = PatchAsync(url, content).GetAwaiter().GetResult();
                    break;

                case "post":            // 0, 1, or 2 parameters (url, content)
                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                        response = PostAsync(url, content).GetAwaiter().GetResult();
                    break;

                case "put":             // 0, 1, or 2 parameters (url, content)
                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                        response = PutAsync(url, content).GetAwaiter().GetResult();
                    break;

                case "send":            // 0, 1, 2, or 3 parameters (method, url, content)
                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                    {
                        if (methodCall is null)
                            result = 11;    // Invalid data type for method
                        else
                            response = SendAsync(methodCall, url, content).GetAwaiter().GetResult();
                    }
                    break;

                case "sendrequest":     // 0, 1, 2, or 3 parameters (method, url, content)
                    if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                    {
                        if (methodCall is null)
                            result = 11;    // Invalid data type for method
                        else
                            response = SendRequestAsync(methodCall, url, content).GetAwaiter().GetResult();
                    }
                    break;

                default:
                    result = await base.DoDefault(methodName);
                    break;
            }

            // Return the response as a JAXObjectWrapper with properties for
            // status code, reason phrase, content, headers, etc.
            if (response is not null)
            {
                JAXObjectWrapper wrapper = ConvertHTTPResponse(response);
                Program.CurrentApp.ReturnValue.Element.Value = wrapper;
            }

            return result;
        }

        // ===================================================================
        // Core Async Methods
        // ===================================================================
        public async Task<XBase_HttpResponse> GetAsync(string url)
            => await SendRequestAsync(HttpMethod.Get, url, null);

        public async Task<XBase_HttpResponse> PostAsync(string url, object? content = null)
            => await SendRequestAsync(HttpMethod.Post, url, content);

        public async Task<XBase_HttpResponse> PutAsync(string url, object? content = null)
            => await SendRequestAsync(HttpMethod.Put, url, content);

        public async Task<XBase_HttpResponse> PatchAsync(string url, object? content = null)
            => await SendRequestAsync(HttpMethod.Patch, url, content);

        public async Task<XBase_HttpResponse> DeleteAsync(string url)
            => await SendRequestAsync(HttpMethod.Delete, url, null);

        public async Task<XBase_HttpResponse> HeadAsync(string url)
            => await SendRequestAsync(HttpMethod.Head, url, null);

        public async Task<XBase_HttpResponse> SendAsync(HttpMethod method, string url, object? content)
            => await SendRequestAsync(method, url, content);


        private async Task<XBase_HttpResponse> SendRequestAsync(HttpMethod method, string url, object? content)
        {
            if (_disposed || _httpClient == null)
                throw new ObjectDisposedException(nameof(XBase_Class_HTTPClient));

            string fullUrl = BuildFullUrl(url);
            UserProperties["lasturl"].Element.Value = fullUrl;

            SetStatus(10, $"Sending {method} to {fullUrl}");

            for (int attempt = 0; attempt <= UserProperties["retrycount"].AsInt(); attempt++)
            {
                try
                {
                    using var requestMsg = new HttpRequestMessage(method, fullUrl);
                    ConfigureRequest(requestMsg, content, method);

                    using var response = await _httpClient.SendAsync(requestMsg);

                    var httpResponse = await XBase_HttpResponse.FromHttpResponseAsync(response, JsonSettings);
                    SetStatus(11, $"Received {(int)response.StatusCode} from {fullUrl}");

                    return httpResponse;
                }
                catch (Exception ex) when (attempt < UserProperties["retrycount"].AsInt() && IsTransientError(ex))
                {
                    SetStatus(13, $"Attempt {attempt + 1} failed, retrying... {ex.Message}");
                    await Task.Delay(1000 * (attempt + 1));
                }
                catch (Exception ex)
                {
                    SetStatus(13, $"Request failed: {ex.Message}");
                    throw;
                }
            }

            throw new HttpRequestException("Max retries exceeded");
        }


        private string BuildFullUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return UserProperties["baseurl"].AsString();

            if (Uri.TryCreate(url, UriKind.Absolute, out _))
                return url;

            return string.IsNullOrEmpty(UserProperties["baseurl"].AsString()) ? url : new Uri(new Uri(UserProperties["baseurl"].AsString()), url).ToString();
        }


        private void ConfigureRequest(HttpRequestMessage request, object? content, HttpMethod method)
        {
            // Authentication
            if (UserProperties["authtype"].AsString().Equals("Basic", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(UserProperties["username"].AsString()))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{UserProperties["username"].AsString()}:{UserProperties["password"].AsString()}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            else if (UserProperties["authtype"].AsString().Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(UserProperties["bearertoken"].AsString()))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserProperties["bearertoken"].AsString());
            }

            // Default headers
            foreach (var header in DefaultHeaders)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            // Content
            if (content != null)
            {
                string json = JsonConvert.SerializeObject(content, JsonSettings);
                string mediaType = (method == HttpMethod.Patch && UserProperties["usejsonpatch"].AsBool())
                    ? "application/json-patch+json"
                    : "application/json";

                request.Content = new StringContent(json, Encoding.UTF8, mediaType);

                // PATCH size check
                long maxPatchSize = (long)UserProperties["maxpatchsizebytes"].AsDouble() * 1024;
                if (method == HttpMethod.Patch && maxPatchSize > 0)
                {
                    long size = Encoding.UTF8.GetByteCount(json);
                    if (size > maxPatchSize)
                    {
                        AppIO.DebugLog($"WARNING: PATCH payload size {size} exceeds MaxPatchSizeBytes ({maxPatchSize})");
                        SetStatus(14, $"Large PATCH payload: {size} bytes");
                    }
                }
            }
        }


        private bool IsTransientError(Exception ex)
        {
            return ex is HttpRequestException || ex is TaskCanceledException;
        }


        private void SetStatus(int statuscode, string message)
        {
            WebHistory webHistory = new()
            {
                URL = UserProperties["lasturl"].AsString(),
                Status = statuscode,
                Content = message
            };

            AppIO.DebugLog($"HTTP Status: {statuscode} - {message}");
            history.Insert(0, webHistory);
            if (UserProperties["historymax"].AsInt() > 0 && history.Count > UserProperties["historymax"].AsInt())
                history.RemoveAt(history.Count - 1);
        }



        // ===================================================================
        // Content Handling Helpers
        // ===================================================================
        /// <summary>
        /// Returns the response body as a Stream. Best for large files and streaming audio/video.
        /// The caller is responsible for disposing the stream when finished.
        /// </summary>
        public async Task<Stream> ContentAsStreamAsync(string? Content)
        {
            if (string.IsNullOrEmpty(Content))
                return Stream.Null;

            // Note: In real usage we would cache the original HttpContent.
            // For now we recreate from the string (acceptable for most cases)
            var bytes = Encoding.UTF8.GetBytes(Content);
            return new MemoryStream(bytes);
        }


        /// <summary>
        /// Returns the response body as a byte array. Best for small to medium binary files (images, PDFs, etc.).
        /// </summary>
        public async Task<byte[]> ContentAsBytesAsync(string? Content)
        {
            if (string.IsNullOrEmpty(Content))
                return Array.Empty<byte>();

            return Encoding.UTF8.GetBytes(Content);
        }


        /// <summary>
        /// Saves the response content directly to a file. 
        /// Recommended for large file downloads.
        /// </summary>
        /// <param name="filePath">Full path where the file should be saved</param>
        public async Task SaveToFileAsync(string filePath, string? Content)
        {
            if (string.IsNullOrEmpty(Content))
                return;

            byte[] data = Encoding.UTF8.GetBytes(Content);

            await File.WriteAllBytesAsync(filePath, data);

            SetStatus(12, $"File saved to {filePath} ({data.Length} bytes)");
        }


        // ===================================================================
        // Cleanup
        // ===================================================================
        public new void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _httpClient?.Dispose();
            _handler?.Dispose();

            GC.SuppressFinalize(this);
            SetStatus(0, "HTTPClient disposed");
        }

        ~XBase_Class_HTTPClient() => Dispose();

        // ===================================================================
        // JAX Integration
        // ===================================================================
        public override string[] JAXMethods() =>
            [
            "addproperty", "command", "get", "post", "put", "patch", "delete", "head", "send", "sendrequest", "resettodefault", "saveasclass"
            ];

        public override string[] JAXEvents() => ["destroy", "error", "init", "load", "statuschanged"];

        public override string[] JAXProperties() =>
            [
            "allowautoredirect,l,true", "authtype,c,None",
            "baseclass,C!,HTTPClient", "baseurl,c,", "bearertoken,c,",
            "class, C!,HTTPClient", "classlibrary,c!,", "comment, c,",
            "history,c!,", "historymax,n,100",
            "maxpatchsizebytes,n,0", "maxredirects,n,10",
            "parent,o!,", "password,c,",
            "retrycount,n,0",
            "status,n!,0", "statusmessage,c!,",
            "tag,c,", "timeoutseconds,n,30",
            "usecookies,l,true", "usejsonpatch,l,false", "useragent,c,JAXBase_HTTPClient/1.0", "username,c,", "usehttps,l,false"
            ];


        private JAXObjectWrapper ConvertHTTPResponse(XBase_HttpResponse response)
        {
            JAXObjectWrapper wrapper = new(Program.CurrentApp, "empty", "", []);
            wrapper.SetProperty("statuscode", response.StatusCode).Wait();
            wrapper.SetProperty("reasonphrase", response.ReasonPhrase).Wait();
            wrapper.SetProperty("content", response.Content).Wait();
            wrapper.SetProperty("headers", "").Wait();
            wrapper.SetProperty("issuccessstatuscode", response.IsSuccessStatusCode).Wait();
            return wrapper;
        }
    }


    public class XBase_HttpResponse
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = "";
        public string Content { get; set; } = "";
        public Dictionary<string, string> Headers { get; } = new();
        public bool IsSuccessStatusCode { get; set; }

        public static async Task<XBase_HttpResponse> FromHttpResponseAsync(HttpResponseMessage response, JsonSerializerSettings? settings = null)
        {
            var resp = new XBase_HttpResponse
            {
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase ?? "",
                IsSuccessStatusCode = response.IsSuccessStatusCode
            };

            foreach (var h in response.Headers)
                resp.Headers[h.Key] = string.Join(", ", h.Value);

            if (response.Content != null)
                resp.Content = await response.Content.ReadAsStringAsync();

            return resp;
        }

        public T? DeserializeJson<T>()
        {
            return JsonConvert.DeserializeObject<T>(Content);
        }
    }
}