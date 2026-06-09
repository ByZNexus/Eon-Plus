using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace FortniteLauncher.Pages
{
    public sealed partial class LeaderboardPage : Page
    {
        public LeaderboardPage()
        {
            this.InitializeComponent();
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            await MyWebView.EnsureCoreWebView2Async();
            MyWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "localassets",
                AppContext.BaseDirectory,
                CoreWebView2HostResourceAccessKind.Allow
            );

            // Make WebView2 background transparent for Galaxy theme
            if (GlobalSettings.Options.Theme == "Galaxy")
            {
                MyWebView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            }

            MyWebView.CoreWebView2.NavigationCompleted += ShowWebView;
            MyWebView.Source = new Uri($"{Definitions.BaseURL}/Leaderboard.html");
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

                string LightModeStyle = Theme == "Light" ? @"
    .LeaderboardItem { background: rgba(0, 0, 0, 0.08) !important; }
    .LeaderboardItem:hover { background: rgba(0, 0, 0, 0.15) !important; }
    .LeaderboardItem:nth-child(1) { background: linear-gradient(90deg, rgba(255, 215, 0, 0.3), rgba(0, 0, 0, 0.08)) !important; }
    .LeaderboardItem:nth-child(2) { background: linear-gradient(90deg, rgba(192, 192, 192, 0.3), rgba(0, 0, 0, 0.08)) !important; }
    .LeaderboardItem:nth-child(3) { background: linear-gradient(90deg, rgba(205, 127, 50, 0.3), rgba(0, 0, 0, 0.08)) !important; }
    .ColumnHeaders { background: rgba(0, 0, 0, 0.1) !important; color: #333 !important; }
    .Rank, .Username, .Stat { color: #000000 !important; }
    .PodiumPlayer.First .PlayerAvatarLarge { box-shadow: 0 0 20px rgba(255, 215, 0, 0.8) !important; }
    .PodiumPlayer.Second .PlayerAvatarLarge { box-shadow: 0 0 20px rgba(192, 192, 192, 0.8) !important; }
    .PodiumPlayer.Third .PlayerAvatarLarge { box-shadow: 0 0 20px rgba(205, 127, 50, 0.8) !important; }
    .FilterButton { background: rgba(0, 0, 0, 0.1) !important; color: #000 !important; }
    .FilterButton.Active { background: #333 !important; color: #fff !important; }
    .LevelBox { background: rgba(0, 0, 0, 0.15) !important; color: #000 !important; }
    .UserSearchInput { background: rgba(0, 0, 0, 0.05) !important; color: #000 !important; border-color: rgba(0,0,0,0.2) !important; }
" : string.Empty;

                string LoadingStyle = $@"
    .LoadingOverlay {{ 
        background: rgba({(Theme == "Light" ? "255, 255, 255" : Theme == "Dark" ? "13, 17, 23" : Theme == "Galaxy" ? "0, 0, 0" : "32, 35, 54")}, {(Theme == "Galaxy" ? "0.4" : "0.95")}) !important; 
    }}
    .LoadingText, .LoadingSubtext {{ 
        color: {TextColor} !important; 
    }}";

                string videoScript = Theme == "Galaxy" ? $@"
    const video = document.createElement('video');
    video.src = 'https://localassets/Content/Texture/Background/space_galaxy_star.mp4';
    video.autoplay = true;
    video.loop = true;
    video.muted = true;
    video.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;object-fit:cover;z-index:-1;pointer-events:none;';
    document.body.appendChild(video);
" : string.Empty;

                string Script = $@"
    const style = document.createElement('style');
    style.textContent = `
        body, html {{
            background-color: {BgColor} !important;
            background: {BgColor} !important;
            color: {TextColor} !important;
        }}
        {LoadingStyle}
        {LightModeStyle}
    `;
    document.head.appendChild(style);
    {videoScript}
";

                await MyWebView.CoreWebView2.ExecuteScriptAsync(Script);
                MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }
        }
    }
}