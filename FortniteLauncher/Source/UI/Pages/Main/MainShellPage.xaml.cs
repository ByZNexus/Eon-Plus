using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace FortniteLauncher.Pages
{
    public sealed partial class MainShellPage : Page
    {
        public static NavigationView STATIC_MainNavigation;
        private bool _hasLoadedDefaultPage;
        private bool _shopServiceAvailable;

        public void SetBackground(Brush Brush)
        {
            MainNavigation.Background = Brush;
            RootFrame.Background = Brush;
            MainNavigation.Resources["NavigationViewContentGridBorderBrush"] = new SolidColorBrush(Colors.Transparent);
            // MainNavigation.Resources["NavigationViewContentBackground"] = Brush;
        }

        public MainShellPage()
        {
            this.InitializeComponent();
            NavigationService.InitializeNavigationService(MainNavigation, MainBreadcrumb, RootFrame);
            MainNavigation.LayoutUpdated += MainNavigation_LayoutUpdated;
        }

        private void MainNavigation_SelectionChanged(NavigationView Sender, NavigationViewSelectionChangedEventArgs Args)
        {
            if (Args.SelectedItem is not NavigationViewItem SelectedItem)
                return;

            if (SelectedItem == PlayPageItem) { NavigationService.Navigate(typeof(PlayPage), true); NavigationService.ChangeBreadcrumbVisibility(false); }
            else if (SelectedItem == DownloadsItem) { NavigationService.Navigate(typeof(DownloadsPage), true); }
            else if (SelectedItem == ItemShopItem) { NavigationService.Navigate(typeof(ItemShopPage), true); }
            else if (SelectedItem == LeaderboardItem) { NavigationService.Navigate(typeof(LeaderboardPage), true); }
            else if (SelectedItem == ServerStatusItem) { NavigationService.Navigate(typeof(ServerStatusPage), true); }
            else if (SelectedItem == SettingsItem) { NavigationService.Navigate(typeof(SettingsPage), true); }
            ElementSoundPlayer.Play(ElementSoundKind.Invoke);
        }

        private void MainBreadcrumb_ItemClicked(BreadcrumbBar Sender, BreadcrumbBarItemClickedEventArgs Args)
        {
            if (Args.Index < NavigationService.BreadCrumbs.Count - 1)
            {
                var Crumb = (NavigationService.Breadcrumb)Args.Item;
                Crumb.NavigateToFromBreadcrumb(Args.Index);
            }
        }

        private void MainNavigation_Loaded(object Sender, RoutedEventArgs Event)
        {
            STATIC_MainNavigation = MainNavigation;
            SettingsPage.ApplyTheme(GlobalSettings.Options.Theme ?? "Default");
            _ = CheckShopServiceAvailabilityAsync();

            if (_hasLoadedDefaultPage)
                return;

            _hasLoadedDefaultPage = true;
            DispatcherQueue.TryEnqueue(() =>
            {
                MainNavigation.SelectionChanged -= MainNavigation_SelectionChanged;
                MainNavigation.SelectedItem = PlayPageItem;
                MainNavigation.SelectionChanged += MainNavigation_SelectionChanged;

                NavigationService.Navigate(typeof(PlayPage), true);
                NavigationService.ChangeBreadcrumbVisibility(false);
            });
        }

        private async Task CheckShopServiceAvailabilityAsync()
        {
            var isAvailable = await new HistoricalShopService().IsShopServiceAvailableAsync();
            _shopServiceAvailable = isAvailable;
            ItemShopItem.IsEnabled = isAvailable;
            ShopServiceUnavailableOverlay.Visibility = isAvailable ? Visibility.Collapsed : Visibility.Visible;
            ToolTipService.SetToolTip(ItemShopItem, isAvailable ? null : "Shop Services are currently unavailable");
            UpdateShopServiceUnavailableOverlay();
        }

        private void MainNavigation_LayoutUpdated(object sender, object e)
        {
            if (!_shopServiceAvailable)
                UpdateShopServiceUnavailableOverlay();
        }

        private void UpdateShopServiceUnavailableOverlay()
        {
            if (ItemShopItem.ActualWidth <= 0 || ItemShopItem.ActualHeight <= 0) return;

            var bounds = ItemShopItem.TransformToVisual(PageRoot).TransformBounds(
                new Rect(0, 0, ItemShopItem.ActualWidth, ItemShopItem.ActualHeight));
            Canvas.SetLeft(ShopServiceUnavailableOverlay, bounds.X);
            Canvas.SetTop(ShopServiceUnavailableOverlay, bounds.Y);
            ShopServiceUnavailableOverlay.Width = bounds.Width;
            ShopServiceUnavailableOverlay.Height = bounds.Height;
        }

        private void ShopServiceUnavailableOverlay_PointerPressed(object sender, PointerRoutedEventArgs e) => e.Handled = true;

        public void UpdateIcons(string Theme)
        {
            string Suffix = Theme == "Light" ? "_B" : string.Empty;

            SetIcon(PlayPageItem, $"ms-appx:///Content/Texture/Icons/IC_Play{Suffix}.png");
            SetIcon(DownloadsItem, $"ms-appx:///Content/Texture/Icons/IC_Download{Suffix}.png");
            SetIcon(ItemShopItem, $"ms-appx:///Content/Texture/Icons/IC_Shop{Suffix}.png");
            SetIcon(LeaderboardItem, $"ms-appx:///Content/Texture/Icons/IC_Leaderboard{Suffix}.png");
            SetIcon(ServerStatusItem, $"ms-appx:///Content/Texture/Icons/IC_ServerStatus{Suffix}.png");
            SetIcon(SettingsItem, $"ms-appx:///Content/Texture/Icons/IC_Settings{Suffix}.png");
        }

        private void SetIcon(NavigationViewItem Item, string IconUri)
        {
            Item.Icon = new ImageIcon
            {
                Width = 24,
                Height = 24,
                Margin = new Microsoft.UI.Xaml.Thickness(-4),
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(IconUri))
            };
        }
        public Frame GetRootFrame() => RootFrame;
    }
}
