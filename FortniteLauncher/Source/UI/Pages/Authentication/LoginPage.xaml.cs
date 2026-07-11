using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.UI.Xaml.Media;

namespace FortniteLauncher.Pages
{
    public partial class LoginPage : Page
    {
        private bool IsInitialized = false;

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private static string JsString(string value) => JsonSerializer.Serialize(value);

        private string BuildTranslationsScript()
        {
            var Translations = new
            {
                emailLabel = Localization.Get("LoginEmailLabel"),
                emailPlaceholder = Localization.Get("LoginEmailPlaceholder"),
                passwordLabel = Localization.Get("LoginPasswordLabel"),
                passwordPlaceholder = Localization.Get("LoginPasswordPlaceholder"),
                forgotPassword = Localization.Get("LoginForgotPassword"),
                signInButton = Localization.Get("LoginSignInButton"),
                noAccount = Localization.Get("LoginNoAccount"),
                signUpLink = Localization.Get("LoginSignUpLink"),
                subtitle = Localization.Get("LoginSubtitle"),
                bannedTitle = Localization.Get("LoginBannedTitle"),
                bannedMessage = Localization.Get("LoginBannedMessage"),
                contactSupport = Localization.Get("LoginContactSupport"),
                donatorTitle = Localization.Get("LoginDonatorTitle"),
                donatorMessage = Localization.Get("LoginDonatorMessage"),
                becomeDonator = Localization.Get("LoginBecomeDonator"),
                updateTitle = Localization.Get("LoginUpdateTitle"),
                updateMessage = Localization.Get("LoginUpdateMessage"),
                downloadLatest = Localization.Get("LoginDownloadLatest"),
                missingFieldsTitle = Localization.Get("LoginMissingFieldsTitle"),
                missingFieldsMessage = Localization.Get("LoginMissingFieldsMessage"),
                invalidEmailTitle = Localization.Get("LoginInvalidEmailTitle"),
                invalidEmailMessage = Localization.Get("LoginInvalidEmailMessage"),
                connectionErrorTitle = Localization.Get("LoginConnectionErrorTitle"),
                connectionErrorMessage = Localization.Get("LoginConnectionErrorMessage"),
                loginFailedTitle = Localization.Get("LoginFailedTitle"),
                msgDeny = Localization.Get("LoginMsgDeny"),
                msgInvalid = Localization.Get("LoginMsgInvalid"),
                msgError = Localization.Get("LoginMsgError"),
                msgDefault = Localization.Get("LoginMsgDefault"),
                loadingMessages = new object[]
                {
                    new { text = Localization.Get("LoadingMsg1Text"), subtext = Localization.Get("LoadingMsg1Sub") },
                    new { text = Localization.Get("LoadingMsg2Text"), subtext = Localization.Get("LoadingMsg2Sub") },
                    new { text = Localization.Get("LoadingMsg3Text"), subtext = Localization.Get("LoadingMsg3Sub") },
                    new { text = Localization.Get("LoadingMsg4Text"), subtext = Localization.Get("LoadingMsg4Sub") },
                    new { text = Localization.Get("LoadingMsg5Text"), subtext = Localization.Get("LoadingMsg5Sub") },
                    new { text = Localization.Get("LoadingMsg6Text"), subtext = Localization.Get("LoadingMsg6Sub") },
                },
                welcomeMessages = new object[]
                {
                    new { greeting = Localization.Get("WelcomeMsg1Greeting"), subtext = Localization.Get("WelcomeMsg1Sub") },
                    new { greeting = Localization.Get("WelcomeMsg2Greeting"), subtext = Localization.Get("WelcomeMsg2Sub") },
                    new { greeting = Localization.Get("WelcomeMsg3Greeting"), subtext = Localization.Get("WelcomeMsg3Sub") },
                    new { greeting = Localization.Get("WelcomeMsg4Greeting"), subtext = Localization.Get("WelcomeMsg4Sub") },
                    new { greeting = Localization.Get("WelcomeMsg5Greeting"), subtext = Localization.Get("WelcomeMsg5Sub") },
                    new { greeting = Localization.Get("WelcomeMsg6Greeting"), subtext = Localization.Get("WelcomeMsg6Sub") },
                }
            };

            string Json = JsonSerializer.Serialize(Translations);
            return $"<script>window.__loginTranslations = {Json};</script>";
        }

        private async void PageLoaded(object Sender, RoutedEventArgs EventArgs)
        {
            try
            {
                bool IsGalaxy = GlobalSettings.Options.Theme == "Galaxy";
                RootGrid.Background = IsGalaxy
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 17, 30))
                    : (Brush)Application.Current.Resources["AppBackground"];

                Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");
                LoginWebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;

                await LoginWebView.EnsureCoreWebView2Async();

                if (LoginWebView.CoreWebView2 == null)
                {
                    DialogService.ShowSimpleDialog("Failed to initialize WebView2. CoreWebView2 is null. This error is only fixable by downloading WebView2 at https://msedge.sf.dl.delivery.mp.microsoft.com/filestreamingservice/files/63158e01-aca3-44d4-8c09-0d338d23779d/MicrosoftEdgeWebView2RuntimeInstallerX64.exe and then reinstalling your launcher.", "Error");
                    return;
                }

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
                    .Replace("<script src=\"LoginPage.js\"></script>", $"{BuildTranslationsScript()}<script>{JsContent}</script>");

                var Theme = GlobalSettings.Options.Theme;
                var BgColor = Theme switch
                {
                    "Dark" => "#0D1117",
                    "Light" => "#f0f0f0",
                    "Galaxy" => "#0D111E",
                    _ => "#202336"
                };

                string ThemeStyle = $"<style>html, body {{ background-color: {BgColor} !important; }}</style>";
                CombinedHtml = CombinedHtml.Replace("</head>", $"{ThemeStyle}</head>");

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
                        SettingsPage.ApplyTheme(GlobalSettings.Options.Theme ?? "Default");
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
                "Galaxy" => "#0D111E",
                _ => "#202336"
            };
            string TextColor = Theme == "Light" ? "#000000" : "#ffffff";
            string InputBg = Theme switch
            {
                "Light" => "rgba(0,0,0,0.05)",
                "Galaxy" => "rgba(20, 22, 40, 0.4)",
                _ => "rgba(20, 22, 40, 0.8)"
            };
            string InputColor = Theme == "Light" ? "#000000" : "#ffffff";
            string BoxBg = Theme switch
            {
                "Light" => "rgba(220,220,220,0.8)",
                "Galaxy" => "rgba(20, 22, 40, 0.35)",
                _ => "rgba(30, 33, 55, 0.6)"
            };
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