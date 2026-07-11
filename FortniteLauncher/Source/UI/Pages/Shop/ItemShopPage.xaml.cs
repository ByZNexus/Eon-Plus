using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FortniteLauncher.Pages
{
    public sealed partial class ItemShopPage : Page
    {
        public ItemShopPage()
        {
            this.InitializeComponent();
            InitializeWebView();
        }

        private static string JsString(string value) => JsonSerializer.Serialize(value);

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
            MyWebView.Source = new Uri($"{Definitions.BaseURL}/Itemshop/");
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

                string videoScript = Theme == "Galaxy" ? @"
    const video = document.createElement('video');
    video.src = 'https://localassets/Content/Texture/Background/space_galaxy_star.mp4';
    video.autoplay = true;
    video.loop = true;
    video.muted = true;
    video.style.cssText = 'position:fixed;top:0;left:-100px;width:calc(100% + 100px);height:100%;object-fit:cover;z-index:-1;pointer-events:none;';
    document.body.appendChild(video);
" : string.Empty;

                string LightFix = Theme == "Light"
                    ? "const lightStyle = document.createElement('style'); lightStyle.textContent = `.shop-section-title { color: #000000 !important; } .item-name span { color: #ffffff !important; }`; document.head.appendChild(lightStyle);"
                    : string.Empty;

                bool NeedsTranslation = Localization.CurrentLanguage != "en-US";
                string FeaturedItems = Localization.Get("FeaturedItems");
                string DailyItems = Localization.Get("DailyItems");

                string TranslationScript = NeedsTranslation ? $@"
    document.querySelectorAll('.shop-section-title').forEach(el => {{
        if (el.textContent.trim() === 'Featured Items') el.textContent = {JsString(FeaturedItems)};
        if (el.textContent.trim() === 'Daily Items') el.textContent = {JsString(DailyItems)};
    }});
" : string.Empty;

                string Script = $"document.querySelector('.nav-container')?.remove();document.querySelector('.shop-vote-container')?.remove();document.querySelector('.otd-title')?.remove();document.querySelector('.otd-container')?.remove();document.querySelectorAll('[id^=\"vns-\"]').forEach(el => el.remove());document.querySelectorAll('.col-wide')?.forEach(el => el.remove());document.querySelector('.shop-rotation h2')?.remove();document.querySelectorAll('.shop-rotation > p').forEach(el => el.remove());document.querySelectorAll('iframe').forEach(el => el.remove());document.querySelectorAll('span[style*=\"position: fixed\"][style*=\"bottom: 0\"]').forEach(el => el.remove());document.querySelectorAll('div[id*=\"google_ads\"]').forEach(el => el.remove());document.querySelectorAll('a[href*=\"/bundle/\"]').forEach(el => el.closest('.item-responsive')?.remove());document.querySelectorAll('style').forEach(styleTag => {{if (styleTag.textContent.includes('#0e1220')) {{styleTag.textContent = styleTag.textContent.replace(/#0e1220/gi, '{BgColor}');}}}});const newStyle = document.createElement('style');newStyle.textContent = `body, html {{background-color: {BgColor} !important;margin: 0 !important;padding: 0 !important;}}main.content {{background-color: {BgColor} !important;padding-top: 0 !important;margin-top: 0 !important;}}.container {{padding-top: 20px !important;}}.shop-rotation {{background-color: {BgColor} !important;margin: 0 auto !important;padding-top: 0 !important;}}.col-ad, #ad-left, .ad-left, .left-ad, .sidebar-ad {{display: none !important;width: 0 !important;visibility: hidden !important;}}span[style*=\"position: fixed\"][style*=\"bottom\"],div[id*=\"google_ads_iframe\"] {{display: none !important;visibility: hidden !important;}}`;document.head.appendChild(newStyle);{LightFix}{videoScript}{TranslationScript}";

                await MyWebView.ExecuteScriptAsync(Script);

                MyWebView.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                return;
            }

            DialogService.ShowSimpleDialog(Localization.Get("ItemShopUpdatingMessage"), Localization.Get("ItemShopUpdatingTitle"));
        }
    }
}