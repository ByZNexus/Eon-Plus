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

            if (ThemeSelector != null)
            {
                _isUpdatingItems = true;

                var secretItems = ThemeSelector.Items
                    .Cast<ComboBoxItem>()
                    .Where(item => {
                        string name = item.Content?.ToString();
                        return name == "ttt";
                    })
                    .ToList();

                if (!GlobalSettings.SecretThemesUnlocked)
                {
                    foreach (var item in secretItems)
                    {
                        ThemeSelector.Items.Remove(item);
                    }
                }
                else
                {
                    string[] targetNames = { "ttt" };
                    foreach (var name in targetNames)
                    {
                        bool alreadyExists = ThemeSelector.Items
                            .Cast<ComboBoxItem>()
                            .Any(i => i.Content?.ToString() == name);

                        if (!alreadyExists)
                        {
                            ThemeSelector.Items.Add(new ComboBoxItem { Content = name });
                        }
                    }
                }

                _isUpdatingItems = false;
            }

            ThemeSelector.SelectedItem = ThemeSelector.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == GlobalSettings.Options.Theme)
                ?? ThemeSelector.Items.Cast<ComboBoxItem>().FirstOrDefault();
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

            if (Theme == "ttt")
            {
                string imageUrl = Theme switch
                {
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
    }
}
