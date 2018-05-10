﻿using Newtonsoft.Json.Linq;
using ReactNative.UIManager;
using ReactNative.UIManager.Annotations;
using ReactNative.Views.Web.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.Web;
using Windows.Web.Http;
using static System.FormattableString;

namespace ReactNative.Views.Web
{
    /// <summary>
    /// A view manager responsible for rendering webview.
    /// </summary>
    public class ReactWebViewManager : SimpleViewManager<WebView>
    {
        private const string BLANK_URL = "about:blank";

        private const int CommandGoBack = 1;
        private const int CommandGoForward = 2;
        private const int CommandReload = 3;

        private readonly Dictionary<int, string> _injectedJS = new Dictionary<int, string>();
        private const string BridgeName = "__REACT_WEB_VIEW_BRIDGE";

        private readonly ConcurrentDictionary<WebView, WebViewData> _webViewData = new ConcurrentDictionary<WebView, WebViewData>();
        private readonly ReactContext _context;

        /// <summary>
        /// Instantiates the <see cref="ReactWebViewManager"/>.
        /// </summary>
        /// <param name="context">The React context.</param>
        public ReactWebViewManager(ReactContext context)
        {
            _context = context;
        }

        /// <summary>
        /// The name of the view manager.
        /// </summary>
        public override string Name
        {
            get
            {
                return "RCTWebView";
            }
        }

        /// <summary>
        /// The commands map for the webview manager.
        /// </summary>
        public override IReadOnlyDictionary<string, object> CommandsMap
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { "goBack", CommandGoBack },
                    { "goForward", CommandGoForward },
                    { "reload", CommandReload },
                };
            }
        }

        /// <summary>
        /// Sets whether JavaScript is enabled or not.
        /// </summary>
        /// <param name="view">A webview instance.</param>
        /// <param name="enabled">A flag signaling whether JavaScript is enabled.</param>
        [ReactProp("javaScriptEnabled")]
        public void SetJavaScriptEnabled(WebView view, bool enabled)
        {
            view.Settings.IsJavaScriptEnabled = enabled;
        }

        /// <summary>
        /// Sets whether Indexed DB is enabled or not.
        /// </summary>
        /// <param name="view">A webview instance.</param>
        /// <param name="enabled">A flag signaling whether Indexed DB is enabled.</param>
        [ReactProp("indexedDbEnabled")]
        public void SetIndexedDbEnabled(WebView view, bool enabled)
        {
            view.Settings.IsIndexedDBEnabled = enabled;
        }

        /// <summary>
        /// Sets the JavaScript to be injected when the webpage loads.
        /// </summary>
        /// <param name="view">A webview instance.</param>
        /// <param name="injectedJavaScript">An injected JavaScript.</param>
        [ReactProp("injectedJavaScript")]
        public void SetInjectedJavaScript(WebView view, string injectedJavaScript)
        {
            _injectedJS[view.GetTag()] = injectedJavaScript;         
        }

        /// <summary>
        /// Sets webview source.
        /// </summary>
        /// <param name="view">A webview instance.</param>
        /// <param name="source">A source for the webview (either static html or an uri).</param>
        [ReactProp("source")]
        public void SetSource(WebView view, JObject source)
        {
            var webViewData = GetWebViewData(view);
            webViewData.Source = source;
            webViewData.SourceUpdated = true;
        }

        /// <summary>
        /// Receive events/commands directly from JavaScript through the 
        /// <see cref="UIManagerModule"/>.
        /// </summary>
        /// <param name="view">
        /// The view instance that should receive the command.
        /// </param>
        /// <param name="commandId">Identifer for the command.</param>
        /// <param name="args">Optional arguments for the command.</param>
        public override void ReceiveCommand(WebView view, int commandId, JArray args)
        {
            switch (commandId)
            {
                case CommandGoBack:
                    if (view.CanGoBack) view.GoBack();
                    break;
                case CommandGoForward:
                    if (view.CanGoForward) view.GoForward();
                    break;
                case CommandReload:
                    view.Refresh();
                    break;
                case CommandStopLoading:
                    view.Stop();
                    break;
                case CommandPostMessage:
                    PostMessage(view, args[0].Value<string>());
                    break;
                case CommandInjectJavaScript:
                    InvokeScript(view, args[0].Value<string>());
                    break;
                default:
                    throw new InvalidOperationException(
                        Invariant($"Unsupported command '{commandId}' received by '{typeof(ReactWebViewManager)}'."));
            }
        }

        /// <summary>
        /// Called when view is detached from view hierarchy and allows for 
        /// additional cleanup by the <see cref="ReactWebViewManager"/>.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view.</param>
        public override void OnDropViewInstance(ThemedReactContext reactContext, WebView view)
        {
            base.OnDropViewInstance(reactContext, view);
            view.NavigationStarting -= OnNavigationStarting;
            view.DOMContentLoaded -= OnDOMContentLoaded;
            view.NavigationFailed -= OnNavigationFailed;
            view.NavigationCompleted -= OnNavigationCompleted;

            _webViewData.TryRemove(view, out _);
        }

        /// <summary>
        /// Creates a new view instance of type <see cref="WebView"/>.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <returns>The view instance.</returns>
        protected override WebView CreateViewInstance(ThemedReactContext reactContext)
        {
            var view = new WebView(WebViewExecutionMode.SeparateThread);
            var data = new WebViewData();
            _webViewData.AddOrUpdate(view, data, (k, v) => data);
            return view;
        }

        /// <summary>
        /// Subclasses can override this method to install custom event 
        /// emitters on the given view.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view instance.</param>
        protected override void AddEventEmitters(ThemedReactContext reactContext, WebView view)
        {
            base.AddEventEmitters(reactContext, view);
            view.NavigationStarting += OnNavigationStarting;
            view.DOMContentLoaded += OnDOMContentLoaded;
            view.NavigationFailed += OnNavigationFailed;
            view.NavigationCompleted += OnNavigationCompleted;
        }

        /// <summary>
        /// Callback that will be triggered after all properties are updated         
        /// </summary>
        /// <param name="view">The view instance.</param>
        protected override void OnAfterUpdateTransaction(WebView view)
        {
            var webViewData = GetWebViewData(view);
            if (webViewData.SourceUpdated)
            {
                NavigateToSource(view);
                webViewData.SourceUpdated = false;
            }
        }

        private void NavigateToSource(WebView view)
        {
            var webViewData = GetWebViewData(view);
            var source = webViewData.Source;
>>>>>>> f4734e79b... Hardened view managers for multi-window scenarios. (#1654)
            if (source != null)
            {
                var html = source.Value<string>("html");
                if (html != null)
                {
                    var baseUrl = source.Value<string>("baseUrl");
                    if (baseUrl != null)
                    {
                        view.Source = new Uri(baseUrl);
                    }

                    view.NavigateToString(html);
                    return;
                }

                var uri = source.Value<string>("uri");
                if (uri != null)
                {
                    // HTML files need to be loaded with the ms-appx-web schema.
                    uri = uri.Replace("ms-appx:", "ms-appx-web:");

                    using (var request = new HttpRequestMessage())
                    {
                        request.RequestUri = new Uri(uri);

                        var method = source.Value<string>("method");
                        if (method != null)
                        {
                            if (method.Equals("GET"))
                            {
                                request.Method = HttpMethod.Get;
                            }
                            else if (method.Equals("POST"))
                            {
                                request.Method = HttpMethod.Post;
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    Invariant($"Unsupported method '{method}' received by '{typeof(ReactWebViewManager)}'."));
                            }
                        }
                        else
                        {
                            request.Method = HttpMethod.Get;
                        }

                        var headers = (JObject)source.GetValue("headers", StringComparison.Ordinal);
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.Append(header.Key, header.Value.Value<string>());
                            }
                        }

                        var body = source.Value<string>("body");
                        if (body != null)
                        {
                            request.Content = new HttpStringContent(body);
                        }

                        view.NavigateWithHttpRequestMessage(request);
                        return;
                    }
                }
            }
            
            view.Navigate(new Uri(BLANK_URL));
        }

        /// <summary>
        /// Receive events/commands directly from JavaScript through the 
        /// <see cref="UIManagerModule"/>.
        /// </summary>
        /// <param name="view">
        /// The view instance that should receive the command.
        /// </param>
        /// <param name="commandId">Identifer for the command.</param>
        /// <param name="args">Optional arguments for the command.</param>
        public override void ReceiveCommand(WebView view, int commandId, JArray args)
        {
            switch (commandId)
            {
                case CommandGoBack:
                    if (view.CanGoBack) view.GoBack();
                    break;
                case CommandGoForward:
                    if (view.CanGoForward) view.GoForward();
                    break;
                case CommandReload:
                    view.Refresh();
                    break;
                default:
                    throw new InvalidOperationException(
                        Invariant($"Unsupported command '{commandId}' received by '{typeof(ReactWebViewManager)}'."));
            }
        }

        /// <summary>
        /// Called when view is detached from view hierarchy and allows for 
        /// additional cleanup by the <see cref="ReactWebViewManager"/>.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view.</param>
        public override void OnDropViewInstance(ThemedReactContext reactContext, WebView view)
        {
            base.OnDropViewInstance(reactContext, view);
            view.NavigationCompleted -= OnNavigationCompleted;
            view.NavigationStarting -= OnNavigationStarting;
        }


        /// <summary>
        /// Creates a new view instance of type <see cref="WebView"/>.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <returns>The view instance.</returns>
        protected override WebView CreateViewInstance(ThemedReactContext reactContext)
        {
            return new WebView();
        }

        /// <summary>
        /// Subclasses can override this method to install custom event 
        /// emitters on the given view.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view instance.</param>
        protected override void AddEventEmitters(ThemedReactContext reactContext, WebView view)
        {
            base.AddEventEmitters(reactContext, view);
            view.NavigationCompleted += OnNavigationCompleted;
            view.NavigationStarting += OnNavigationStarting;
            view.UnsupportedUriSchemeIdentified += OnUnsupportedUriSchemeIdentified;
        }

        private void OnUnsupportedUriSchemeIdentified(object sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs e)
        {
            var webView = (WebView)sender;
            webView.GetReactContext().GetNativeModule<UIManagerModule>()
                .EventDispatcher
                .DispatchEvent(
                    new WebViewLoadingEvent(
                        webView.GetTag(),
                        "Start",
                        e.Uri.ToString(),
                        true,
                        webView.DocumentTitle,
                        webView.CanGoBack,
                        webView.CanGoForward));
            e.Handled = true;
        }

        private async void OnNavigationCompleted(object sender, WebViewNavigationCompletedEventArgs e)
        {
            var webView = (WebView)sender;
            LoadFinished(webView, e.Uri?.ToString());

            if (e.IsSuccess)
            {
                var script = default(string);

                if (_injectedJS.TryGetValue(webView.GetTag(), out script) && !string.IsNullOrWhiteSpace(script))
                {
                    string[] args = { script };
                    try
                    {
                        await webView.InvokeScriptAsync("eval", args).AsTask().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {    
                        LoadFailed(webView, e.WebErrorStatus, ex.Message);
                    }
                }  
            }
            else
            {
                LoadFailed(webView, e.WebErrorStatus, null);
            }      
        }

        private static void OnNavigationStarting(object sender, WebViewNavigationStartingEventArgs e)
        {
            var webView = (WebView)sender;

            webView.GetReactContext().GetNativeModule<UIManagerModule>()
                .EventDispatcher
                .DispatchEvent(
                    new WebViewLoadingEvent(
                         webView.GetTag(),
                         "Start",
                         e.Uri?.ToString(), 
                         true, 
                         webView.DocumentTitle, 
                         webView.CanGoBack, 
                         webView.CanGoForward));
        }

        private static void LoadFinished(WebView webView, string uri)
        {
            webView.GetReactContext().GetNativeModule<UIManagerModule>()
                    .EventDispatcher
                    .DispatchEvent(
                         new WebViewLoadingEvent(
                            webView.GetTag(),
                            "Finish",
                            uri,
                            false,
                            webView.DocumentTitle,
                            webView.CanGoBack,
                            webView.CanGoForward));
        }

        private void LoadFailed(WebView webView, WebErrorStatus status, string message)
        {
            var reactContext = webView.GetReactContext();
            reactContext.GetNativeModule<UIManagerModule>()
                .EventDispatcher
                .DispatchEvent(
                    new WebViewLoadingErrorEvent(
                        webView.GetTag(),
                        status,
                        message));
        }
    }
}
