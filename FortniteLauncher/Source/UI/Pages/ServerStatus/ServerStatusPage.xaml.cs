using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
namespace FortniteLauncher.Pages
{
    public sealed partial class ServerStatusPage : Page
    {
        public ServerStatusPage()
        {
            this.InitializeComponent();
            InitializeWebView();
        }
        private async void InitializeWebView()
        {
            MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            await MyWebView.EnsureCoreWebView2Async();

            if (GlobalSettings.Options.Theme == "Galaxy")
            {
                MyWebView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
                MyWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "localassets",
                    AppContext.BaseDirectory,
                    CoreWebView2HostResourceAccessKind.Allow
                );
            }

            MyWebView.CoreWebView2.NavigationCompleted += ShowWebView;
            MyWebView.Source = new Uri($"{Definitions.BaseURL}/ServerStatus/");
        }
        private async void ShowWebView(object Sender, CoreWebView2NavigationCompletedEventArgs Event)
        {
            if (Event.IsSuccess)
            {
                await Task.Delay(500);
                var Theme = GlobalSettings.Options.Theme;
                var BgColor = Theme switch
                {
                    "Dark" => "#0D1117",
                    "Light" => "#f0f0f0",
                    "Galaxy" => "transparent",
                    _ => "#202336"
                };
                string TextColor = Theme == "Light" ? "#000000" : "#ffffff";
                string CardStyle = Theme == "Light"
                    ? ".server-card { background-color: #999999 !important; } .server-card:hover { background-color: #888888 !important; } #pagination-container, #pagination-container * { color: #000000 !important; }"
                    : string.Empty;
                string videoScript = Theme == "Galaxy" ? $@"
    const video = document.createElement('video');
    video.src = 'https://localassets/Content/Texture/Background/space_galaxy_star.mp4';
    video.autoplay = true;
    video.loop = true;
    video.muted = true;
    video.style.cssText = 'position:fixed;top:0;left:-100px;width:calc(100% + 100px);height:100%;object-fit:cover;z-index:-1;pointer-events:none;';
    document.body.appendChild(video);
" : string.Empty;
                string Script = $@"
    const style = document.createElement('style');
    style.textContent = `
        body, html {{
            background-color: {BgColor} !important;
            color: {TextColor} !important;
        }}
        {CardStyle}
    `;
    document.head.appendChild(style);
    {videoScript}
";
                await MyWebView.CoreWebView2.ExecuteScriptAsync(Script);
                MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }
            DialogService.ShowSimpleDialog("No, this is not an error. The API is getting updated. This will be resolved shortly. Thank you.", "Updating");
        }
    }
}