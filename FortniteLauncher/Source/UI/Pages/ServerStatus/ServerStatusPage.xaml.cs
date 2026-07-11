using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;

namespace FortniteLauncher.Pages
{
    public sealed partial class ServerStatusPage : Page
    {
        public ServerStatusPage()
        {
            this.InitializeComponent();
            InitializeWebView();

            Loaded += ServerStatusPage_Loaded;
            Unloaded += ServerStatusPage_Unloaded;
        }
        private static string JsString(string value) => JsonSerializer.Serialize(value);
        private void ServerStatusPage_Loaded(object Sender, RoutedEventArgs EventArgs)
        {
            Localization.LanguageChanged += OnLanguageChanged;

            if (MyWebView.CoreWebView2 != null)
            {
                _ = InjectTranslationScript();
            }
        }

        private void ServerStatusPage_Unloaded(object Sender, RoutedEventArgs EventArgs)
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

        private async Task InjectTranslationScript()
        {
            bool NeedsTranslation = Localization.CurrentLanguage != "en-US";

            string FilterEU = Localization.Get("FilterEU");
            string FilterNA = Localization.Get("FilterNA");
            string Joinable = Localization.Get("StatusJoinable");
            string InGame = Localization.Get("StatusInGame");
            string Full = Localization.Get("StatusFull");
            string Ended = Localization.Get("StatusEnded");
            string Restarting = Localization.Get("StatusRestarting");
            string ArenaDuos = Localization.Get("ModeArenaDuos");
            string ArenaSolos = Localization.Get("ModeArenaSolos");
            string ArenaSquads = Localization.Get("ModeArenaSquads");
            string PreLobby = Localization.Get("PreLobby");
            string PlayersLeft = Localization.Get("PlayersLeft");
            string TeamsLeft = Localization.Get("TeamsLeft");
            string MatchInProgress = Localization.Get("MatchInProgress");
            string ServerOnlineFilling = Localization.Get("ServerOnlineFilling");
            string MatchFullWaiting = Localization.Get("MatchFullWaiting");
            string ServerOnlineEmptyLobby = Localization.Get("ServerOnlineEmptyLobby");
            string MatchesRestartingFormat = Localization.Get("MatchesRestartingFormat").Replace("{0}", "$1");
            string GameEndedFormat = Localization.Get("GameEndedFormat").Replace("{0}", "$1");
            string PageOfFormat = Localization.Get("PageOfFormat").Replace("{0}", "$1").Replace("{1}", "$2");
            string ServerSummaryFormat = Localization.Get("ServerSummaryFormat").Replace("{0}", "$1").Replace("{1}", "$2").Replace("{2}", "$3");

            string Script = $@"
    (function() {{
        const euBtn = document.getElementById('filter-eu');
        const naBtn = document.getElementById('filter-na');
        if (euBtn) euBtn.textContent = {JsString(FilterEU)};
        if (naBtn) naBtn.textContent = {JsString(FilterNA)};

        const needsTranslation = {(NeedsTranslation ? "true" : "false")};

        function translateNode(root) {{
            if (!needsTranslation) return;

            const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
            const replacements = [
                ['JOINABLE', {JsString(Joinable)}],
                ['IN-GAME', {JsString(InGame)}],
                ['FULL', {JsString(Full)}],
                ['ENDED', {JsString(Ended)}],
                ['RESTARTING', {JsString(Restarting)}],
                ['Arena Duos', {JsString(ArenaDuos)}],
                ['Arena Solos', {JsString(ArenaSolos)}],
                ['Arena Squads', {JsString(ArenaSquads)}],
                ['Pre-Lobby', {JsString(PreLobby)}],
                ['Players Left', {JsString(PlayersLeft)}],
                ['Teams Left', {JsString(TeamsLeft)}],
                ['Match in progress, new game will start soon after this one ends.', {JsString(MatchInProgress)}],
                ['Server online, filling up the match with players in the pre-game lobby.', {JsString(ServerOnlineFilling)}],
                ['Match is full, waiting for match to start.', {JsString(MatchFullWaiting)}],
                ['Server online, awaiting players in an empty lobby.', {JsString(ServerOnlineEmptyLobby)}]
            ];

            const regexReplacements = [
                [/Matches restarting and will be available shortly in (\d+) seconds\./, {JsString(MatchesRestartingFormat)}],
                [/Game has ended, servers are restarting in (\d+) seconds\./, {JsString(GameEndedFormat)}],
                [/Page (\d+) of (\d+)/, {JsString(PageOfFormat)}],
                [/(\d+) servers \((\d+) joinable, (\d+) in-game\)/, {JsString(ServerSummaryFormat)}]
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

        translateNode(document.body);

        if (window.__localizationObserver) {{
            window.__localizationObserver.disconnect();
        }}

        if (needsTranslation) {{
            let pending = false;
            window.__localizationObserver = new MutationObserver(() => {{
                if (pending) return;
                pending = true;
                requestAnimationFrame(() => {{
                    pending = false;
                    translateNode(document.body);
                }});
            }});
            window.__localizationObserver.observe(document.body, {{
                childList: true,
                subtree: true,
                characterData: true
            }});
        }}
    }})();
";

            try
            {
                await MyWebView.CoreWebView2.ExecuteScriptAsync(Script);
            }
            catch
            {
                // WebView2 may not be ready yet, safe to ignore here
            }
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
                await InjectTranslationScript();
                MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }

            DialogService.ShowSimpleDialog(Localization.Get("ServerUpdatingMessage"), Localization.Get("ServerUpdatingTitle"));
        }
    }
}