using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Text.Json;

namespace FortniteLauncher.Pages
{
    public sealed partial class LeaderboardPage : Page
    {
        public LeaderboardPage()
        {
            this.InitializeComponent();
            InitializeWebView();

            Loaded += LeaderboardPage_Loaded;
            Unloaded += LeaderboardPage_Unloaded;
        }

        private static string JsString(string value) => JsonSerializer.Serialize(value);

        private void LeaderboardPage_Loaded(object Sender, RoutedEventArgs EventArgs)
        {
            Localization.LanguageChanged += OnLanguageChanged;

            if (MyWebView.CoreWebView2 != null)
            {
                _ = InjectTranslationScript();
            }
        }

        private void LeaderboardPage_Unloaded(object Sender, RoutedEventArgs EventArgs)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        private async void OnLanguageChanged()
        {
            if (MyWebView.CoreWebView2 == null)
                return;

            await InjectTranslationScript();
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

        private async Task InjectTranslationScript()
        {
            bool NeedsTranslation = Localization.CurrentLanguage != "en-US";

            string FilterPoints = Localization.Get("FilterPoints");
            string FilterKills = Localization.Get("FilterKills");
            string FilterWins = Localization.Get("FilterWins");
            string SearchPlaceholder = Localization.Get("SearchPlaceholder");
            string SearchButtonText = Localization.Get("SearchButton");
            string SearchingButtonText = Localization.Get("SearchingButton");
            string LoadingLeaderboardTitle = Localization.Get("LoadingLeaderboardTitle");
            string LoadingLeaderboardSubtext = Localization.Get("LoadingLeaderboardSubtext");
            string RefreshingLeaderboardTitle = Localization.Get("RefreshingLeaderboardTitle");
            string RefreshingLeaderboardRow = Localization.Get("RefreshingLeaderboardRow");
            string ErrorConnectingServer = Localization.Get("ErrorConnectingServer");
            string ErrorRefreshingData = Localization.Get("ErrorRefreshingData");
            string HeaderRank = Localization.Get("HeaderRank");
            string HeaderPlayer = Localization.Get("HeaderPlayer");
            string HeaderKills = Localization.Get("HeaderKills");
            string HeaderWins = Localization.Get("HeaderWins");
            string HeaderPoints = Localization.Get("HeaderPoints");
            string PercentCompleteFormat = Localization.Get("PercentCompleteFormat").Replace("{0}", "$1");
            string NextUpdateFormat = Localization.Get("NextUpdateFormat").Replace("{0}", "$1");
            string LevelFormat = Localization.Get("LevelFormat").Replace("{0}", "$1");
            string PlayerNotFoundFormat = Localization.Get("PlayerNotFoundFormat").Replace("{0}", "$1").Replace("{1}", "$2");

            string Script = $@"
    (function() {{
        try {{
            const needsTranslation = {(NeedsTranslation ? "true" : "false")};

            function setStaticText() {{
                const searchInput = document.getElementById('UserSearchInput');
                if (searchInput) searchInput.placeholder = {JsString(SearchPlaceholder)};
            }}

            function translateNode(root) {{
                if (!needsTranslation) return;

                const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
                const replacements = [
                    ['Loading Leaderboard...', {JsString(LoadingLeaderboardTitle)}],
                    ['Please wait while we fetch the data', {JsString(LoadingLeaderboardSubtext)}],
                    ['Refreshing Leaderboard...', {JsString(RefreshingLeaderboardTitle)}],
                    ['Refreshing leaderboard...', {JsString(RefreshingLeaderboardRow)}],
                    ['Error connecting to server', {JsString(ErrorConnectingServer)}],
                    ['Error refreshing data', {JsString(ErrorRefreshingData)}],
                    ['Searching...', {JsString(SearchingButtonText)}],
                    ['Search', {JsString(SearchButtonText)}],
                    ['RANK', {JsString(HeaderRank)}],
                    ['PLAYER', {JsString(HeaderPlayer)}],
                    ['KILLS', {JsString(HeaderKills)}],
                    ['WINS', {JsString(HeaderWins)}],
                    ['POINTS', {JsString(HeaderPoints)}],
                    ['Points', {JsString(FilterPoints)}],
                    ['Kills', {JsString(FilterKills)}],
                    ['Wins', {JsString(FilterWins)}]
                ];

                const regexReplacements = [
                    [/(\d+)% complete/, {JsString(PercentCompleteFormat)}],
                    [/Next update in ([\d:]+)/, {JsString(NextUpdateFormat)}],
                    [/Lvl (\d+)/, {JsString(LevelFormat)}],
                    [/Player &quot;(.+?)&quot; not found among (\d+) players\./, {JsString(PlayerNotFoundFormat)}],
                    [/Player ""(.+?)"" not found among (\d+) players\./, {JsString(PlayerNotFoundFormat)}]
                ];

                let node;
                const nodesToChange = [];
                while (node = walker.nextNode()) {{
                    nodesToChange.push(node);
                }}

                for (const n of nodesToChange) {{
                    let text = n.nodeValue;

                    for (const [pattern, replacement] of regexReplacements) {{
                        if (pattern.test(text)) {{
                            text = text.replace(pattern, replacement);
                        }}
                    }}

                    for (const [en, fr] of replacements) {{
                        if (text.includes(en)) {{
                            text = text.split(en).join(fr);
                        }}
                    }}

                    if (text !== n.nodeValue) {{
                        n.nodeValue = text;
                    }}
                }}
            }}

            setStaticText();
            translateNode(document.body);

            if (window.__leaderboardLocalizationObserver) {{
                window.__leaderboardLocalizationObserver.disconnect();
            }}

            if (needsTranslation) {{
                let pending = false;
                window.__leaderboardLocalizationObserver = new MutationObserver(() => {{
                    if (pending) return;
                    pending = true;
                    requestAnimationFrame(() => {{
                        pending = false;
                        setStaticText();
                        translateNode(document.body);
                    }});
                }});
                window.__leaderboardLocalizationObserver.observe(document.body, {{
                    childList: true,
                    subtree: true,
                    characterData: true
                }});
            }}

            return 'OK:' + document.body.innerText.substring(0, 200);
        }} catch (e) {{
            return 'ERROR:' + e.message;
        }}
    }})();
";

            var Result = await MyWebView.CoreWebView2.ExecuteScriptAsync(Script);
            System.Diagnostics.Debug.WriteLine($"[LeaderboardLocalization] Script result: {Result}");
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
                await InjectTranslationScript();
                MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }
        }
    }
}