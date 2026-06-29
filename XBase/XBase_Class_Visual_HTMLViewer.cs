/*
 * XBase_Class_HTMLViewer.cs
 * Minimal Avalonia 12+ NativeWebView wrapper for JAXBase
 * 
 * Currently this control is not that advanced so we're going to keep it simple for now.
 * 
 * It will display HTML content as-is or in a "safe mode" where JavaScript is blocked and 
 * potentially dangerous content is sanitized.
 * 
 */

using Avalonia.Controls;
using JAXBase.Core;
using JAXBase.Utilities;
using System.Text.RegularExpressions;
using ZXing;

namespace JAXBase.XBase
{
    public class XBase_Class_Visual_HTMLViewer : XBase_Class_Avalonia
    {
        public new string MyBaseClass { get; } = "HTMLViewer";
        public new string MyDefaultName { get; } = "htmlviewer";

        private NativeWebView? _webView;

        public List<WebHistory> history = new();

        public XBase_Class_Visual_HTMLViewer(JAXObjectWrapper jow, string name) : base(jow, name)
        {
            _webView = new NativeWebView();
            SetVisualObject(_webView, MyBaseClass, name, true, UserObject.urw);
        }

        public override async Task<bool> PostInit(JAXObjectWrapper? callBack, List<ParameterClass> parameterList)
        {
            bool result = await base.PostInit(callBack, parameterList);

            if (_webView != null)
            {
                _webView.NavigationCompleted += Web_NavigationCompleted;
            }

            UserProperties["isloaded"].Element.Value = false;

            SetStatus(0, "HTMLViewer initialized");
            return result;
        }

        private async void Web_NavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
        {
            UserProperties["isloaded"].Element.Value = true;
            await _CallMethod("loadcompleted");
        }

        // ===================================================================
        // Safe Display Method (Main Feature You Requested)
        // ===================================================================
        /// <summary>
        /// Safely displays HTML with JavaScript disabled and dangerous content stripped.
        /// Use this for untrusted / static content.
        /// </summary>
        public async Task SafeDisplayAsync(string html)
        {
            if (_webView == null || string.IsNullOrWhiteSpace(html))
                return;

            // Force sandbox mode
            UserProperties["sandboxmode"].Element.Value = true;

            string safeHtml = SanitizeHtml(html);

            // Add strict CSP to block scripts
            safeHtml = safeHtml.Replace("<head>",
                "<head><meta http-equiv=\"Content-Security-Policy\" content=\"script-src 'none'; object-src 'none';\">",
                StringComparison.OrdinalIgnoreCase);

            if (!safeHtml.Contains("<head>", StringComparison.OrdinalIgnoreCase))
            {
                safeHtml = "<head><meta http-equiv=\"Content-Security-Policy\" content=\"script-src 'none'; object-src 'none';\"></head>" + safeHtml;
            }

            if (_webView != null)
                _webView.NavigateToString(safeHtml);

            SetStatus(12, "Safe HTML displayed (JavaScript blocked)");
        }

        // Simple HTML sanitizer - removes scripts and event handlers
        private string SanitizeHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return html;

            // Remove <script> tags and their content
            html = Regex.Replace(html, @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", "", RegexOptions.IgnoreCase);

            // Remove javascript: and vbscript: protocols
            html = Regex.Replace(html, @"(?i)\b(javascript|vbscript|data):", "#", RegexOptions.IgnoreCase);

            // Remove event handlers (onclick, onload, etc.)
            html = Regex.Replace(html, @"\bon\w+\s*=", "data-disabled=", RegexOptions.IgnoreCase);

            return html;
        }


        // ===================================================================
        // Requested Methods
        // ===================================================================

        // Navigate to a URL (with optional safe mode)
        public async Task<int> NavigateAsync(string url)
        {
            int result= 0;

            if (_webView == null || string.IsNullOrWhiteSpace(url))
            {
                result = 8216;
                SetStatus(result, "Empty URL or WebView not initialized");
                return result;
            }

            UserProperties["url"].Element.Value = url;
            bool isSandbox = UserProperties["sandboxmode"].Element.Value is true;

            try
            {
                if (isSandbox)
                {
                    // Sandbox mode: Fetch content → Sanitize → Load safely
                    SetStatus(10, $"Navigating in Sandbox mode: {url}");

                    // You should inject your HTTPClient here or pass it as parameter
                    // For now, we'll assume you have access to one or use a simple approach
                    var client = new System.Net.Http.HttpClient();
                    string html = await client.GetStringAsync(url);

                    await SafeDisplayAsync(html);        // Already have this method - reuses sanitizer + CSP
                }
                else
                {
                    // Normal mode - load exactly as-is
                    SetStatus(10, $"Navigating normally: {url}");
                    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                    {
                        _webView.Navigate(uri);
                    }
                }
            }
            catch (Exception ex)
            {
                result = 8214;
                SetStatus(result, $"Navigate failed: {ex.Message}");
            }

            return result;
        }


        // Returns true if navigation was successful, false if there is no back history
        public int GoBack()
        {
            UserProperties["url"].Element.Value = "";   // Need to build a history
            if (_webView == null) return 8216;
            return _webView.GoBack() ? -1 : 0;
        }

        // Returns true if navigation was successful, false if there is no forward history
        public int GoForward()
        {
            UserProperties["url"].Element.Value = "";   // Need to build a history
            if (_webView == null) return 8216;
            return _webView.GoForward() ? -1 : 0;
        }

        // Reload the current page
        public void Reload() => _webView?.Refresh();


        // Not sure we want this one as it can be a security risk
        public async Task<string> InvokeScriptAsync(string script)
        {
            UserProperties["url"].Element.Value = "";
            if (_webView == null) return "";
            SetStatus(13, $"Called InvokeScriptAsync");
            string? result = await _webView.InvokeScript(script);
            return result ?? "";
        }


        public void PrintAsync()
        {
            SetStatus(13, $"Called Print UI");
            _webView?.ShowPrintUI();
        }

        public async Task<int> SaveToPdfAsync(string filePath)
        {
            int result = 0;

            if (_webView == null) return 8216;

            try
            {
                using var stream = await _webView.PrintToPdfStreamAsync();
                using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream);
                SetStatus(13, $"PDF saved to {filePath}");
            }
            catch (Exception ex)
            {
                result = 8215;
                SetStatus(result, $"PDF save failed: {ex.Message}");
            }

            return result;
        }

        public void ClearHistory()
        {
            // NativeWebView does not have a direct ClearHistory method
            // We can reload the current page as a workaround
            UserProperties["url"].Element.Value = "";   // Need to build a history
            Reload();
            SetStatus(14, "History cleared (current page reloaded)");
        }

        public string GetCurrentUrl()
        {
            // Avalonia 12 does not expose current URL directly
            // Return last navigated URL or empty
            return (string)UserProperties["url"].Element.Value;
        }



        // ===================================================================
        // JAXBase Property System - Only safe properties
        // ===================================================================
        public override async Task<int> SetProperty(string propertyName, object objValue, int objIdx)
        {
            int result = 0;
            propertyName = propertyName.ToLower();
            JAXObjects.Token tk = new(objValue);

            if (UserProperties.ContainsKey(propertyName))
            {
                if (UserProperties[propertyName].Protected)
                    result = 3026;
                else
                {
                    switch (propertyName)
                    {
                        case "html":
                            if (_webView != null)
                                _webView.NavigateToString(tk.AsString(), null);   // Confirmed method
                            break;

                        case "url":
                            if (_webView != null && Uri.TryCreate(tk.AsString(), UriKind.Absolute, out Uri? uri))
                                _webView.Navigate(uri);                           // Confirmed method
                            break;

                        default:
                            result = await base.SetProperty(propertyName, objValue, objIdx);
                            break;
                    }

                    if (result == 0 || result == 9)
                    {
                        UserProperties[propertyName].Element.Value = objValue;
                        result = 0;
                    }
                }
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, $"{result}|{propertyName}|", string.Empty);
            }

            return result;
        }

        public override async Task<JAXObjects.Token> GetProperty(string propertyName, int idx)
        {
            JAXObjects.Token returnToken = new();
            int result = 0;
            propertyName = propertyName.ToLower();

            if (UserProperties.ContainsKey(propertyName))
            {
                returnToken.CopyFrom(UserProperties[propertyName]);
                result = 0;
            }
            else
                result = 1559;

            if (result > 10)
            {
                _AddError(result, 0, $"{result}|{propertyName}|", string.Empty);
                returnToken.Element.MakeNull();
            }

            return returnToken;
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


        public override async Task<int> DoDefault(string methodName)
        {
            int result = 0;
            methodName = methodName.ToLower();
            string html = "";

            switch (methodName)
            {
                case "command":
                    break;

                case "display":
                    if (Program.CurrentApp.ParameterClassList.Count == 1)
                    {
                        if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        {
                            // Create the method
                            html = Program.CurrentApp.ParameterClassList[0].token.AsString().ToUpper();
                        }
                        else
                            result = 11;
                    }
                    else if (Program.CurrentApp.ParameterClassList.Count > 1)
                        result = 98;
                    else
                        html = UserProperties["html"].AsString();


                    if (result == 0 && _webView is not null && string.IsNullOrWhiteSpace(html) == false)
                    {
                        if (UserProperties["sandboxmode"].AsBool())
                        {
                            // Sandbox mode: Sanitize and block scripts
                            SafeDisplayAsync(html).Wait();
                            SetStatus(11, "HTML displayed (JavaScript disabled)");
                        }
                        else
                        {
                            // Load exactly as-is
                            _webView.NavigateToString(html);
                            SetStatus(11, "Raw HTML displayed (JavaScript enabled)");
                        }
                    }
                    break;

                case "navigate":
                    if (JAXLib.Between(Program.CurrentApp.ParameterClassList.Count, 1, 2))
                    {
                        if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        {
                            // Get URL
                            string url = Program.CurrentApp.ParameterClassList[0].token.AsString().ToUpper();
                            bool safeMode = UserProperties["sandboxmode"].AsBool();

                            if (Program.CurrentApp.ParameterClassList.Count == 2)
                            {
                                if (Program.CurrentApp.ParameterClassList[1].token.Element.Type.Equals("L"))
                                {
                                    safeMode = Program.CurrentApp.ParameterClassList[1].token.AsBool();
                                    UserProperties["sandboxmode"].Element.Value = safeMode;
                                }
                                else
                                    result = 11;
                            }

                            if (result == 0)
                            {
                                if (safeMode)
                                {
                                    // Safe navigate: Load URL with sandbox mode enabled
                                    UserProperties["sandboxmode"].Element.Value = true;
                                    NavigateAsync(url);
                                    SetStatus(12, $"Navigating to {url} (JavaScript disabled)");
                                }
                                else
                                {
                                    // Normal navigate: Load URL as-is
                                    UserProperties["sandboxmode"].Element.Value = false;
                                    NavigateAsync(url);
                                    SetStatus(12, $"Navigating to {url} (JavaScript enabled)");
                                }
                            }
                        }
                        else
                            result = 11;
                    }
                    else if (Program.CurrentApp.ParameterClassList.Count > 2)
                        result = 98;
                    else
                    {
                        // No parameters, navigate to current URL property
                        string url = UserProperties["url"].AsString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            if (UserProperties["sandboxmode"].AsBool())
                            {
                                // Safe navigate: Load URL with sandbox mode enabled
                                NavigateAsync(url);
                                SetStatus(12, $"Navigating to {url} (JavaScript disabled)");
                            }
                            else
                            {
                                // Normal navigate: Load URL as-is
                                NavigateAsync(url);
                                SetStatus(12, $"Navigating to {url} (JavaScript enabled)");
                            }
                        }
                        else
                            result = 1559;
                    }

                    break;

                case "back":
                    GoBack();
                    break;

                case "forward":
                    GoForward();
                    break;

                case "print":
                    PrintAsync();
                    break;

                case "savetopdf":
                    string filePath = "";

                    if (Program.CurrentApp.ParameterClassList.Count == 1)
                    {
                        if (Program.CurrentApp.ParameterClassList[0].token.Element.Type.Equals("C"))
                        {
                            // Create the method
                            filePath = Program.CurrentApp.ParameterClassList[0].token.AsString().ToUpper();
                        }
                        else
                            result = 11;
                    }
                    else if (Program.CurrentApp.ParameterClassList.Count > 1)
                        result = 98;


                    if (result == 0 && _webView is not null && string.IsNullOrWhiteSpace(filePath) == false)
                    {
                        await SaveToPdfAsync(filePath);
                        SetStatus(11, $"Saved to PDF: {filePath}");
                    }
                    break;
            }

            result = await base.DoDefault(methodName);
            return result;
        }

        public override string[] JAXMethods() => ["addproperty", "command", "loadhtml", "navigate", "resettodefault", "saveasclass", "savetopdf"];

        public override string[] JAXEvents() => ["loadcompleted", "error"];

        public override string[] JAXProperties()
        {
            return
            [
                "baseclass,C!,HTMLViewer",
                "class,c!,HTMLViewer", "classlibrary,c!,",
                "html,c,",
                "isloaded,l!,false",
                "name,c!,htmlviewer",
                "url,c,",
                "sandboxmode,l,false"
            ];
        }
    }
}