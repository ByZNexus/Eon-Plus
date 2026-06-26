using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace FortniteLauncher.Pages
{
    public partial class LoginPage : Page
    {
        private bool IsInitialized = false;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void PageLoaded(object Sender, RoutedEventArgs EventArgs)
        {
            try
            {
                // --- FIX: Force the underlying WebView2 environment to use 0 alpha (True Transparency) ---
                // This eliminates the stubborn solid grey background box before EnsureCoreWebView2Async runs.
                Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");

                await LoginWebView.EnsureCoreWebView2Async();

                if (LoginWebView.CoreWebView2 == null)
                {
                    DialogService.ShowSimpleDialog("Failed to initialize WebView2. CoreWebView2 is null.", "Error");
                    return;
                }

                // Explicitly keep the framework-level background transparent
                LoginWebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;

                LoginWebView.CoreWebView2.WebMessageReceived += MessageReceived;

                string BasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Source", "UI", "Pages", "Authentication", "Public");
                string HtmlPath = Path.Combine(BasePath, "LoginPage.html");
                string CssPath = Path.Combine(BasePath, "LoginPage.css");
                string JsPath = Path.Combine(BasePath, "LoginPage.js");

                if (!File.Exists(HtmlPath) || !File.Exists(CssPath) || !File.Exists(JsPath))
                {
                    DialogService.ShowSimpleDialog($"Required files not found at: {BasePath}", "Error");
                    return;
                }

                string HtmlContent = File.ReadAllText(HtmlPath);
                string CssContent = File.ReadAllText(CssPath);
                string JsContent = File.ReadAllText(JsPath);

                string CombinedHtml = HtmlContent
                    .Replace("<link rel=\"stylesheet\" href=\"LoginPage.css\">", $"<style>{CssContent}</style>")
                    .Replace("<script src=\"LoginPage.js\"></script>", $"<script>{JsContent}</script>");

                LoginWebView.NavigateToString(CombinedHtml);
                LoginWebView.CoreWebView2.NavigationCompleted += ApplyLoginTheme;
                IsInitialized = true;
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog($"Error loading WebView2: {Exception.Message}", "Error");
            }
        }

        private async void MessageReceived(CoreWebView2 Sender, CoreWebView2WebMessageReceivedEventArgs Args)
        {
            try
            {
                var Message = JsonSerializer.Deserialize<LoginMessage>(Args.WebMessageAsJson);

                if (Message?.Action == "CheckCredentials")
                {
                    await CheckCredentials();
                }
                else if (Message?.Action == "Login")
                {
                    await HandleLogin(Message);
                }
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog($"Error handling message: {Exception.Message}", "Error");
                await SendMessageToWebView(new
                {
                    Status = "Error",
                    Title = "Error",
                    Message = Exception.Message
                });
            }
        }

        private async Task CheckCredentials()
        {
            if (!string.IsNullOrEmpty(GlobalSettings.Options.Email) && !string.IsNullOrEmpty(GlobalSettings.Options.Password))
            {
                ApiResponse Response = await Authenticator.CheckLogin(GlobalSettings.Options.Email, GlobalSettings.Options.Password);

                await SendMessageToWebView(new
                {
                    Action = "AutoLogin",
                    Status = Response.Status,
                    Username = GlobalSettings.Options.Username ?? "Player",
                    SkinUrl = GlobalSettings.Options.SkinUrl ?? $"{Definitions.CDN_URL}/EonS17.png",
                    DownloadUrl = ProjectDefinitions.DownloadLauncherURL
                });

                if (Response.Status == "Success")
                {
                    await Task.Delay(2500);
                    GlobalSettings.Windows.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.ShellFrame.Navigate(typeof(MainShellPage));
                    });
                }
                return;
            }

            await SendMessageToWebView(new { Action = "ShowLogin" });
        }

        private async Task HandleLogin(LoginMessage Message)
        {
            ApiResponse Response = await Authenticator.CheckLogin(Message.Email, Message.Password);

            if (Response.Status == "Success")
            {
                GlobalSettings.Options.Email = Message.Email;
                GlobalSettings.Options.Password = Message.Password;
                UserSettings.SaveSettings();
            }

            await SendMessageToWebView(new
            {
                Action = "LoginResponse",
                Status = Response.Status,
                Username = GlobalSettings.Options.Username ?? "Player",
                SkinUrl = GlobalSettings.Options.SkinUrl ?? $"{Definitions.CDN_URL}/EonS17.png",
                DownloadUrl = ProjectDefinitions.DownloadLauncherURL
            });

            if (Response.Status == "Success")
            {
                await Task.Delay(2000);
                GlobalSettings.Windows.DispatcherQueue.TryEnqueue(() =>
                {
                    MainWindow.ShellFrame.Navigate(typeof(MainShellPage));
                });
            }
        }

        private async Task SendMessageToWebView(object Data)
        {
            if (IsInitialized == false || LoginWebView.CoreWebView2 == null)
                return;

            try
            {
                string Json = JsonSerializer.Serialize(Data);
                string Script =
                $@"
                    if (window.chrome && window.chrome.webview) {{
                        window.dispatchEvent(new MessageEvent('message', {{ 
                            data: {Json} 
                        }}));
                    }}
                ";
                await LoginWebView.CoreWebView2.ExecuteScriptAsync(Script);
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog($"Error sending message to WebView: {Exception.Message}", "Error");
            }
        }

        private class LoginMessage
        {
            public string Action { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
        }

        private async void ApplyLoginTheme(object Sender, CoreWebView2NavigationCompletedEventArgs Args)
        {
            var Theme = GlobalSettings.Options.Theme;
            var BgColor = Theme switch
            {
                "Dark" => "#0D1117",
                "Light" => "#f0f0f0",
                _ => "#202336"
            };
            string TextColor = Theme == "Light" ? "#000000" : "#ffffff";
            string InputBg = Theme == "Light" ? "rgba(0,0,0,0.05)" : "rgba(20, 22, 40, 0.8)";
            string InputColor = Theme == "Light" ? "#000000" : "#ffffff";
            string BoxBg = Theme == "Light" ? "rgba(220,220,220,0.8)" : "rgba(30, 33, 55, 0.6)";
            string Script = $@"
    const style = document.createElement('style');
    style.textContent = `
        body, html {{
            background: {BgColor} !important;
            color: {TextColor} !important;
        }}
        .bg-gradient {{
            background: none !important;
        }}
        .login-box {{
            background: {BoxBg} !important;
        }}
        .welcome-text, .subtitle, .form-label, .checkbox-label, 
        .signup-container, .forgot-link, h1, h2, h3, p, label, span, div {{
            color: {TextColor} !important;
        }}
        input[type='text'], input[type='email'], input[type='password'] {{
            background: {InputBg} !important;
            color: {InputColor} !important;
            border-color: rgba(0,0,0,0.15) !important;
        }}
        input::placeholder {{
            color: {(Theme == "Light" ? "rgba(0,0,0,0.4)" : "rgba(255,255,255,0.2)")} !important;
        }}
        .login-button {{
            background: {(Theme == "Light" ? "#333333" : "rgba(255,255,255,0.95)")} !important;
            color: {(Theme == "Light" ? "#ffffff" : "#202336")} !important;
        }}
        .forgot-link {{
            color: #3b82f6 !important;
        }}
        .signup-link {{
            color: #3b82f6 !important;
        }}
    `;
    document.head.appendChild(style);
";

            await LoginWebView.CoreWebView2.ExecuteScriptAsync(Script);
        }
    }
}