using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;

namespace uk.JohnCook.dotnet.GameChatLite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string oauth2RequestURL = "https://discord.johncook.co.uk/oauth2/test1";
        private static readonly Uri oauth2TokenClientURL = new("https://discord.johncook.co.uk/oauth2/test.html");
        private static readonly Uri oauth2RedirectURL = new("https://discord.johncook.co.uk/oauth2/cb");
        private static readonly Uri restApiBase = new($"https://discord.com/api/v{Model.DiscordGateway.Version}/");
        private static readonly Uri imageCdnBase = new("https://cdn.discordapp.com/");
        private Uri? gatewayUri;
        private StringBuilder uriBuilder = new();
        private static readonly SolidColorBrush topBarBackgroundBrush = new(Color.FromRgb(0x22, 0x22, 0x22));
        private static readonly SolidColorBrush secureBrush = new(Color.FromRgb(0xfd, 0xfd, 0x96));
        private static readonly SolidColorBrush nonWebBrush = new(Color.FromRgb(0xaa, 0xbb, 0xcc));
        private Microsoft.Web.WebView2.Wpf.WebView2? webView;
        private Uri? currentRequest;
        private int currentRequestStatus;
        private bool reachedRedirectURL;
        private Uri? redirectUriResult;
        private Guid? stateGuid;
        private string? authCode;

        public MainWindow()
        {
            InitializeComponent();
            discordLoginButton.IsEnabled = true;
            Model.DiscordGateway.Payload test = new()
            {
                OpCode = Model.DiscordGateway.OpCode.Dispatch,
                Data = new(),
                Seq = 42,
                Name = "big test"
            };
            Debug.WriteLine(JsonSerializer.Serialize(test, new JsonSerializerOptions() { WriteIndented = true }));
        }

        private void RecreateWebView()
        {
            if (webView is not null)
            {
                reachedRedirectURL = false;
                currentRequest = null;
                redirectUriResult = null;
                webView.CoreWebView2.Navigate(oauth2RequestURL.Contains('?') ? oauth2RequestURL + $"+state={stateGuid}" : oauth2RequestURL + $"?state={stateGuid}");
                return;
            }
            DestroyWebView();
            webView = new();
            webView.Loaded += (sender, e) =>
            {
                WebViewLoaded(webView);
            };
            _ = webViewPanel.Children.Add(webView);
        }

        private void DestroyWebView()
        {
            if (webView != null)
            {
                if (webViewPanel.Children.Contains(webView))
                {
                    webViewPanel.Children.Remove(webView);
                }
                webView.Dispose();
            }
            webViewPanel.Visibility = Visibility.Collapsed;
        }

        private void WebViewLoaded(Microsoft.Web.WebView2.Wpf.WebView2 wv2)
        {
            wv2.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            wv2.NavigationStarting += WebView_NavigationStarting;
            wv2.NavigationCompleted += WebView_NavigationCompleted;
            InitializeAsync(wv2);
        }

        private async void InitializeAsync(Microsoft.Web.WebView2.Wpf.WebView2 wv2)
        {
            await wv2.EnsureCoreWebView2Async(null);
            //wv2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            wv2.CoreWebView2.OpenDevToolsWindow();
            wv2.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            wv2.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            wv2.CoreWebView2.ContentLoading += CoreWebView2_ContentLoading;
            wv2.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            wv2.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
            wv2.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
            wv2.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            wv2.CoreWebView2.AddWebResourceRequestedFilter(oauth2RedirectURL.ToString() + "*", CoreWebView2WebResourceContext.All);
            wv2.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }

        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e.Request.Uri.StartsWith(oauth2RedirectURL.ToString(), StringComparison.Ordinal))
            {
                Stream? responseData = e.Response?.Content;
                if (responseData is not null)
                {
                    Debugger.Break();
                }
            }
        }

        private void DiscordLogin_Click(object sender, RoutedEventArgs e)
        {
            discordLoginButton.IsEnabled = false;
            addressBar.Text = "Starting Web Interface...";
            addressBar.Background = nonWebBrush;
            topBar.Background = topBarBackgroundBrush;
            addressBar.Visibility = Visibility.Visible;
            stateGuid = Guid.NewGuid();
            RecreateWebView();
        }

        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (webView is not null && webView.CoreWebView2 is not null)
            {
                webViewPanel.Visibility = Visibility.Visible;
                addressBar.Visibility = Visibility.Visible;
                webView.Visibility = Visibility.Visible;
                webView.CoreWebView2.Navigate(oauth2RequestURL.Contains('?') ? oauth2RequestURL + $"+state={stateGuid}" : oauth2RequestURL + $"?state={stateGuid}");
            }
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            addressBar.Background = nonWebBrush;
            topBar.Background = topBarBackgroundBrush;
            addressBar.Text = "Navigating...";
            currentRequest = new(e.Uri);
            currentRequestStatus = 0;
            if (e.Uri.ToString().StartsWith(oauth2RedirectURL.GetLeftPart(UriPartial.Query), StringComparison.Ordinal))
            {
                reachedRedirectURL = true;
                e.Cancel = true;
            }
        }

        private void CoreWebView2_ContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
        {
            addressBar.Background = nonWebBrush;
            topBar.Background = topBarBackgroundBrush;
            addressBar.Text = "Loading...";
        }

        private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (e.IsUserInitiated)
            {
                webView?.CoreWebView2.Navigate(e.Uri);
            }
        }

        private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
        {
            e.Cancel = true;
            e.Handled = true;
        }

        private void CoreWebView2_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
        {
            Uri? requestUri = e.Request.Uri.Length > 0 ? new(e.Request.Uri) : null;
            bool equivalentUri = currentRequest?.Host == requestUri?.Host && currentRequest?.PathAndQuery == requestUri?.PathAndQuery;
            equivalentUri = equivalentUri && (currentRequest?.Scheme == requestUri?.Scheme || currentRequest?.Scheme is "http" or "https" && requestUri?.Scheme is "http" or "https");
            if (webView is not null && equivalentUri)
            {
                currentRequestStatus = e.Response.StatusCode;
                if (currentRequestStatus is 301 or 302 or 303 or 307 or 308)
                {
                    addressBar.Background = Brushes.Plum;
                    topBar.Background = topBarBackgroundBrush;
                    if (e.Response.Headers.Any(x => x.Key.ToLower() == "Location"))
                    {
                        string redirectTarget = e.Response.Headers.First(x => x.Key.ToLower() == "location").Value.Trim('"');
                        addressBar.Text = $"Following Redirect...";
                        if (redirectTarget != default)
                        {
                            currentRequest = new(redirectTarget);
                        }
                    }
                    else if (e.Response.Headers.Any(x => x.Key == "Non-Authoritative-Reason"))
                    {
                        string redirectReason = e.Response.Headers.First(x => x.Key == "Non-Authoritative-Reason").Value.Trim('"');
                        if (redirectReason == "HSTS" && currentRequest is not null)
                        {
                            currentRequest = new UriBuilder("https", currentRequest.Host, currentRequest.Port, currentRequest.PathAndQuery, currentRequest.Fragment).Uri;
                        }
                    }
                }
            }
            else if (webView is not null)
            {
                currentRequestStatus = e.Response.StatusCode;
                if (currentRequestStatus is 301 or 302 or 303 or 307 or 308)
                {
                    addressBar.Background = Brushes.Plum;
                    topBar.Background = topBarBackgroundBrush;
                    if (e.Response.Headers.Any(x => x.Key.ToLower() == "location"))
                    {
                        Uri redirectTarget = new(e.Response.Headers.First(x => x.Key.ToLower() == "location").Value.Trim('"'));
                        if (redirectTarget.ToString().StartsWith(oauth2RedirectURL.GetLeftPart(UriPartial.Query), StringComparison.Ordinal))
                        {
                            webView.CoreWebView2.Stop();
                            redirectUriResult = redirectTarget;
                        }
                    }
                    else if (e.Response.Headers.Any(x => x.Key == "Non-Authoritative-Reason"))
                    {
                        string redirectReason = e.Response.Headers.First(x => x.Key == "Non-Authoritative-Reason").Value.Trim('"');
                        if (redirectReason == "HSTS" && currentRequest is not null)
                        {
                            currentRequest = new UriBuilder("https", currentRequest.Host, currentRequest.Port, currentRequest.PathAndQuery, currentRequest.Fragment).Uri;
                        }
                    }
                }
            }
        }

        private void CoreWebView2_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (webView?.Source == oauth2TokenClientURL)
            {
                _ = webView.CoreWebView2.ExecuteScriptAsync($"getToken({JsonSerializer.Serialize(authCode)},{JsonSerializer.Serialize(stateGuid)});").ConfigureAwait(true).GetAwaiter();

            }
        }

        private static void GetIdnUriFromUri(Uri uri, ref StringBuilder sb)
        {
            _ = sb.Clear()
            .Append(uri.Scheme)
            .Append("://")
            .Append(uri.IdnHost);
            if (!uri.IsDefaultPort)
            {
                _ = sb.Append($":{uri.Port}");
            }
            _ = sb.Append(uri.PathAndQuery);
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (reachedRedirectURL && redirectUriResult is not null && redirectUriResult.GetLeftPart(UriPartial.Query).StartsWith(oauth2RedirectURL.ToString(), StringComparison.Ordinal))
            {
                reachedRedirectURL = false;
                addressBar.Background = nonWebBrush;
                topBar.Background = Brushes.DarkGreen;
                addressBar.Text = "Assessing Authorization Result...";
                addressBar.Background = nonWebBrush;
                //webViewPanel.Visibility = Visibility.Collapsed;
                NameValueCollection query = System.Web.HttpUtility.ParseQueryString(redirectUriResult.Query);
                redirectUriResult = null;
                if (query.AllKeys.Contains("error") || !query.AllKeys.Contains("code") || query.AllKeys.Contains("state") && query["state"] != stateGuid?.ToString())
                {
                    addressBar.Background = Brushes.PeachPuff;
                    topBar.Background = Brushes.OrangeRed;
                    stateGuid = null;
                    if (query.AllKeys.Contains("error"))
                    {
                        addressBar.Text = $"An error occurred: {query["error"]}";
                    }
                    else if (!query.AllKeys.Contains("code"))
                    {
                        addressBar.Text = "An error occurred: the code parameter was absent";
                    }
                    else if (query.AllKeys.Contains("state") && query["state"] != stateGuid?.ToString())
                    {
                        addressBar.Text = "An error occurred: the state parameter did not match";
                    }
                    discordLoginButton.IsEnabled = true;
                }
                else if (query.AllKeys.Contains("code") && query.AllKeys.Contains("state") && query["state"] == stateGuid?.ToString())
                {
                    addressBar.Background = Brushes.PaleGreen;
                    topBar.Background = Brushes.DarkGreen;
                    addressBar.Text = "Code obtained. Attempting to exchange for a token...";
                    authCode = query["code"];
                    webView?.CoreWebView2.Navigate(oauth2TokenClientURL.ToString());
                }
                return;
            }
            if (webView?.Source == oauth2RedirectURL)
            {
                addressBar.Text = "Unloading Web Interface...";
                webViewPanel.Visibility = Visibility.Collapsed;
                DestroyWebView();
                addressBar.Visibility = Visibility.Collapsed;
                addressBar.Text = string.Empty;
            }
            else if (webView?.Source == oauth2TokenClientURL)
            {
                addressBar.Background = Brushes.PaleGreen;
                topBar.Background = Brushes.DarkGreen;
                addressBar.Text = "Authorization Code Token Exchange Client Loaded";
                webViewPanel.Visibility = Visibility.Visible;
                discordLoginButton.IsEnabled = true;
            }
            else
            {
                if (webView?.Source.Scheme is "https" or "http")
                {
                    GetIdnUriFromUri(webView.Source, ref uriBuilder);
                    _ = uriBuilder.Append("    |    ");
                    if (!e.IsSuccess)
                    {
                        if (currentRequestStatus >= 400 && currentRequestStatus <= 599)
                        {
                            _ = uriBuilder.Append($"HTTP/{currentRequestStatus}").Append("    |    ");
                        }
                        else
                        {
                            _ = uriBuilder.Append($"{e.WebErrorStatus} Error").Append("    |    ");
                        }
                    }
                    _ = uriBuilder.Append(webView.CoreWebView2.DocumentTitle);
                    addressBar.Text = uriBuilder.ToString();
                }
                else
                {
                    addressBar.Text = webView?.Source.ToString();
                }
                if (webView?.Source.Scheme is "https" or "http")
                {
                    if (e.IsSuccess)
                    {
                        addressBar.Background = webView.Source.Scheme == "https" ? secureBrush : Brushes.LightPink;
                        topBar.Background = webView.Source.Scheme == "https" ? topBarBackgroundBrush : Brushes.Red;
                    }
                    else
                    {
                        addressBar.Background = webView.Source.Scheme == "http" ? Brushes.Salmon : e.WebErrorStatus switch
                        {
                            CoreWebView2WebErrorStatus.Unknown => Brushes.Plum,
                            CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect => Brushes.LightPink,
                            CoreWebView2WebErrorStatus.CertificateExpired => Brushes.LightPink,
                            CoreWebView2WebErrorStatus.ClientCertificateContainsErrors => Brushes.LightPink,
                            CoreWebView2WebErrorStatus.CertificateRevoked => Brushes.LightPink,
                            CoreWebView2WebErrorStatus.CertificateIsInvalid => Brushes.LightPink,
                            CoreWebView2WebErrorStatus.ServerUnreachable => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.Timeout => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.ErrorHttpInvalidServerResponse => Brushes.Silver,
                            CoreWebView2WebErrorStatus.ConnectionAborted => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.ConnectionReset => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.Disconnected => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.CannotConnect => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.HostNameNotResolved => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.OperationCanceled => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.RedirectFailed => Brushes.PeachPuff,
                            CoreWebView2WebErrorStatus.UnexpectedError => Brushes.Khaki,
                            _ => Brushes.Silver,
                        };
                        topBar.Background = webView.Source.Scheme == "http" ? Brushes.Red : e.WebErrorStatus switch
                        {
                            CoreWebView2WebErrorStatus.Unknown => Brushes.DarkMagenta,
                            CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect => Brushes.Red,
                            CoreWebView2WebErrorStatus.CertificateExpired => Brushes.Red,
                            CoreWebView2WebErrorStatus.ClientCertificateContainsErrors => Brushes.Red,
                            CoreWebView2WebErrorStatus.CertificateRevoked => Brushes.Red,
                            CoreWebView2WebErrorStatus.CertificateIsInvalid => Brushes.Red,
                            CoreWebView2WebErrorStatus.ServerUnreachable => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.Timeout => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.ErrorHttpInvalidServerResponse => Brushes.SlateGray,
                            CoreWebView2WebErrorStatus.ConnectionAborted => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.ConnectionReset => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.Disconnected => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.CannotConnect => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.HostNameNotResolved => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.OperationCanceled => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.RedirectFailed => Brushes.OrangeRed,
                            CoreWebView2WebErrorStatus.UnexpectedError => Brushes.Yellow,
                            _ => Brushes.SlateGray,
                        };
                    }
                }
                else
                {
                    addressBar.Background = e.IsSuccess ? nonWebBrush : Brushes.PeachPuff;
                    topBar.Background = topBarBackgroundBrush;
                }
                discordLoginButton.IsEnabled = true;
            }
        }
    }
}
