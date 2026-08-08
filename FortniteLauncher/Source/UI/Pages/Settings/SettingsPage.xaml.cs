using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FortniteLauncher.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly string LauncherVersion = GlobalSettings.Version;

        private readonly string About_Header = $"About {ProjectDefinitions.Name} Launcher";
        private readonly string GitHub_Juri = ProjectDefinitions.GitHub_Juri;
        private readonly string Github_Greenwood = ProjectDefinitions.GitHub_Greenwood;
        private readonly string Discord = ProjectDefinitions.Discord;
        private readonly string Tiktok = ProjectDefinitions.Tiktok;

        private bool _isUpdatingItems = false;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void PageLoaded(object Sender, RoutedEventArgs Event)
        {
            SoundToggle.IsOn = GlobalSettings.Options.IsSoundEnabled;
            BubbleBuildsToggle.IsOn = GlobalSettings.Options.IsBubbleBuildsEnabled;
            ShowRarityBadgesToggle.IsOn = GlobalSettings.Options.ShowRarityBadges;

            if (ThemeSelector != null)
            {
                _isUpdatingItems = true;

                var secretItems = ThemeSelector.Items
                    .Cast<ComboBoxItem>()
                    .Where(item => {
                        string name = item.Content?.ToString();
                        return name == "KittyParty" || name == "Borris" || name == "Billoute" || name == "ttt";
                    })
                    .ToList();

                if (!GlobalSettings.SecretThemesUnlocked)
                {
                    foreach (var item in secretItems)
                        ThemeSelector.Items.Remove(item);
                }
                else
                {
                    string[] targetNames = { "KittyParty", "Borris", "Billoute", "ttt" };
                    foreach (var name in targetNames)
                    {
                        bool alreadyExists = ThemeSelector.Items
                            .Cast<ComboBoxItem>()
                            .Any(i => i.Content?.ToString() == name);

                        if (!alreadyExists)
                            ThemeSelector.Items.Add(new ComboBoxItem { Content = name });
                    }
                }

                _isUpdatingItems = false;
            }

            ThemeSelector.SelectedItem = ThemeSelector.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == GlobalSettings.Options.Theme)
                ?? ThemeSelector.Items.Cast<ComboBoxItem>().FirstOrDefault();

            LanguageSelector.SelectedItem = LanguageSelector.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == GlobalSettings.Options.Language)
                ?? LanguageSelector.Items.Cast<ComboBoxItem>().FirstOrDefault();

            ApplyLocalization();
        }

        private void ShowRarityBadgesToggle_Toggled(object sender, RoutedEventArgs e)
        {
            GlobalSettings.Options.ShowRarityBadges = ShowRarityBadgesToggle.IsOn;
            UserSettings.SaveSettings();
            ShopAppearanceSettings.NotifyChanged();
        }

        private void ThemeChanged(object Sender, SelectionChangedEventArgs Event)
        {
            if (_isUpdatingItems) return;

            if (ThemeSelector.SelectedItem is ComboBoxItem Selected)
            {
                var Theme = Selected.Content.ToString();
                GlobalSettings.Options.Theme = Theme;
                UserSettings.SaveSettings();
                ApplyTheme(Theme);
            }
        }

        private void LanguageChanged(object Sender, SelectionChangedEventArgs Event)
        {
            if (LanguageSelector.SelectedItem is ComboBoxItem Selected && Selected.Tag is string LanguageTag)
            {
                if (LanguageTag == GlobalSettings.Options.Language)
                    return;

                GlobalSettings.Options.Language = LanguageTag;
                UserSettings.SaveSettings();
                Localization.SetLanguage(LanguageTag);
                ApplyLocalization();
            }
        }

        private void ToggleSoundSwitch(object Sender, RoutedEventArgs Event)
        {
            var ToggleSwitch = (ToggleSwitch)Sender;
            if (ToggleSwitch.IsOn)
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
                GlobalSettings.Options.IsSoundEnabled = true;
            }
            else
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
                GlobalSettings.Options.IsSoundEnabled = false;
            }

            UserSettings.SaveSettings();
        }

        private void ToggleBubbleBuilds(object Sender, RoutedEventArgs Event)
        {
            if (BubbleBuildsToggle.IsOn)
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
                GlobalSettings.Options.IsBubbleBuildsEnabled = true;
            }
            else
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
                GlobalSettings.Options.IsBubbleBuildsEnabled = false;
            }

            UserSettings.SaveSettings();
        }

        private async void SignOutAccount(object Sender, RoutedEventArgs Event)
        {
            bool Result = await DialogService.YesOrNoDialog("This action will sign you out of your account. Your Account Information will be cleared from this device. Are you sure you want to proceed?", "Logging Out");

            if (Result)
            {
                GlobalSettings.Options.Email = string.Empty;
                GlobalSettings.Options.Password = string.Empty;
                UserSettings.SaveSettings();

                var Button = (Button)Sender;
                Button.IsEnabled = false;

                var ProgressRing = new ProgressRing
                {
                    IsIndeterminate = true,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                Button.Content = ProgressRing;

                MainWindow.ShellFrame.Navigate(typeof(LoginPage));
                Button.IsEnabled = true;
            }
        }

        public static void ApplyTheme(string Theme)
        {
            if (GlobalSettings.Windows is MainWindow MainWinClear && Theme != "Galaxy")
                MainWinClear.ClearVideoBackground();

            var RequestedTheme = Theme == "Light" ? ElementTheme.Light : ElementTheme.Dark;

            if (Theme == "Galaxy")
            {
                RequestedTheme = ElementTheme.Dark;

                if (GlobalSettings.Windows?.Content is FrameworkElement GalaxyRoot)
                    GalaxyRoot.RequestedTheme = RequestedTheme;

                if (GlobalSettings.Windows is MainWindow GalaxyWin)
                {
                    if (!GalaxyWin.IsVideoBackgroundActive())
                    {
                        string videoPath = System.IO.Path.Combine(
                            AppContext.BaseDirectory,
                            "Content", "Texture", "Background", "space_galaxy_star.mp4"
                        );
                        GalaxyWin.SetVideoBackground(videoPath);
                    }
                }

                if (MainWindow.ShellFrame?.Content is MainShellPage GalaxyShell)
                {
                    GalaxyShell.RequestedTheme = RequestedTheme;
                    GalaxyShell.SetBackground(new SolidColorBrush(Colors.Transparent));
                    GalaxyShell.UpdateIcons(Theme);

                    if (GalaxyShell.GetRootFrame() is Frame GalaxyFrame)
                        GalaxyFrame.RequestedTheme = RequestedTheme;

                    if (GalaxyShell.GetRootFrame()?.Content is FrameworkElement GalaxyPage)
                        GalaxyPage.RequestedTheme = RequestedTheme;

                    if (GalaxyShell.GetRootFrame()?.Content is PlayPage GalaxyPlay)
                        GalaxyPlay.UpdateIcons(Theme);
                }

                return;
            }

            Brush Brush;

            if (Theme == "KittyParty" || Theme == "Borris" || Theme == "Billoute" || Theme == "ttt")
            {
                string imageUrl = Theme switch
                {
                    "KittyParty" => "https://media.discordapp.net/attachments/1509928333264031826/1510659568009744525/image.png?ex=6a1d9edb&is=6a1c4d5b&hm=4aebcaa1ee9ff6a9a1e59094e0db12c0434f3d3b95f0f6e784ee0221d1c7ea7d&format=webp&quality=lossless&width=912&height=959&",
                    "Borris" => "https://media.discordapp.net/attachments/1509928333264031826/1510669092984717332/borris.png?ex=6a1da7ba&is=6a1c563a&hm=6cc87b8ae47eca0251b68a49f46567467f3a0dbc7f148a786ba190dd25d627b1&=&format=webp&quality=lossless&width=912&height=885",
                    "Billoute" => "https://media.discordapp.net/attachments/1500223387144945835/1510671953214574592/boos.png?ex=6a1daa64&is=6a1c58e4&hm=b528c96085670deff1b4d9472f91a3759e7a61380321ff01737a463a6fbe89ef&=&format=webp&quality=lossless",
                    "ttt" => "https://cdn.7tv.app/emote/01KNNF1BEWZNVCCAWVX96VQ7WB/4x.gif",
                    _ => string.Empty
                };

                Brush = new ImageBrush
                {
                    ImageSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imageUrl)),
                    Stretch = Stretch.UniformToFill
                };
                RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                var Color = Theme switch
                {
                    "Light" => Windows.UI.Color.FromArgb(255, 240, 240, 240),
                    "Dark" => Windows.UI.Color.FromArgb(255, 13, 17, 23),
                    _ => Windows.UI.Color.FromArgb(255, 32, 35, 54)
                };
                Brush = new SolidColorBrush(Color);
            }

            if (GlobalSettings.Windows?.Content is FrameworkElement Root)
                Root.RequestedTheme = RequestedTheme;

            if (GlobalSettings.Windows is MainWindow MainWin)
                MainWin.SetWindowBackground(Brush);

            if (MainWindow.ShellFrame?.Content is MainShellPage Shell)
            {
                Shell.RequestedTheme = RequestedTheme;
                Shell.SetBackground(Brush);
            }

            if (MainWindow.ShellFrame?.Content is MainShellPage ShellPage)
            {
                ShellPage.RequestedTheme = RequestedTheme;
                if (ShellPage.GetRootFrame() is Frame Frame)
                    Frame.RequestedTheme = RequestedTheme;

                ShellPage.SetBackground(Brush);
                ShellPage.UpdateIcons(Theme);
            }

            if (MainWindow.ShellFrame?.Content is MainShellPage ShellPager)
            {
                ShellPager.RequestedTheme = RequestedTheme;
                if (ShellPager.GetRootFrame() is Frame Frame)
                    Frame.RequestedTheme = RequestedTheme;

                ShellPager.SetBackground(Brush);
                ShellPager.UpdateIcons(Theme);

                if (ShellPager.GetRootFrame()?.Content is FrameworkElement Page)
                    Page.RequestedTheme = RequestedTheme;

                if (ShellPager.GetRootFrame()?.Content is PlayPage Play)
                    Play.UpdateIcons(Theme);
            }
        }

        private void ApplyLocalization()
        {
            AppearanceHeaderText.Text = Localization.Get("AppearanceHeader");
            SoundSettingsCard.Header = Localization.Get("SoundHeader");
            SoundSettingsCard.Description = Localization.Get("SoundDescription");
            ThemeSettingsCard.Header = Localization.Get("ThemeHeader");
            ThemeSettingsCard.Description = Localization.Get("ThemeDescription");
            LanguageCard.Header = Localization.Get("LanguageHeader");
            LanguageCard.Description = Localization.Get("LanguageDescription");
            BubbleBuildsCard.Header = Localization.Get("BubbleBuildsHeader");
            BubbleBuildsCard.Description = Localization.Get("BubbleBuildsDescription");
            AboutHeaderText.Text = Localization.Get("AboutHeader");
            LogoutBtn.Content = Localization.Get("LogOut");
            About.Header = Localization.Get("AboutEonHeader");
            AboutPlus.Header = Localization.Get("AboutEonPlusHeader");
            MadeByRun1.Text = Localization.Get("MadeBy");
            AndRun1.Text = Localization.Get("And");
            ModByRun.Text = Localization.Get("ModMadeBy");
            ForEonRun.Text = Localization.Get("ForEonLauncher");
        }
    }
}
