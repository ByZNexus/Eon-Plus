using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FortniteLauncher.Pages
{
    public sealed partial class PlayPage : Page
    {
        public static SettingsCard Launch_Button;
        public static ProgressRing ProgressRing;
        private static readonly HttpClient PlayerCountClient = new HttpClient();
        private string Progress = DownloadService.DownloadProgress;
        private readonly string DisplayUsername = GetRandomGreeting();
        public static readonly string Season = "Launch Fortnite";
        public static readonly string Chapter = string.Empty;
        private List<string> _onlinePlayers = new();
        private System.Timers.Timer _playerCountTimer;

        private static string GetRandomGreeting()
        {
            string Username = GlobalSettings.Options.Username;
            string[] Greetings = new[]
            {
                $"Hello, {Username}!",
                $"Welcome, {Username}!",
                $"Hey, {Username}!",
                $"What's up, {Username}!",
                $"Greetings, {Username}!",
                $"Hi, {Username}!",
                $"Howdy, {Username}!"
            };
            var Random = new Random();
            return Greetings[Random.Next(Greetings.Length)];
        }

        public PlayPage()
        {
            InitializeComponent();
            LoadProfileImage();
            Launch_Button = LaunchButton;
            DownloadService.ProgressChanged += OnDownloadProgressChanged;
        }

        private void StartPlayerCountUpdates()
        {
            if (_playerCountTimer == null)
            {
                _playerCountTimer = new System.Timers.Timer(30000);
                _playerCountTimer.Elapsed += async (s, e) => await FetchPlayerCount();
                _playerCountTimer.AutoReset = true;
            }

            _playerCountTimer.Start();
            _ = FetchPlayerCount();
        }

        private async Task FetchPlayerCount()
        {
            try
            {
                var Response = await PlayerCountClient.GetStringAsync("https://services.eonfn.net:2087");
                var Json = JsonDocument.Parse(Response);
                var AllPlayers = new System.Collections.Generic.List<string>();

                foreach (var Player in Json.RootElement.GetProperty("Clients").GetProperty("clients").EnumerateArray())
                {
                    var Name = Player.GetString();
                    if (!string.IsNullOrWhiteSpace(Name) && !Name.StartsWith("user_", StringComparison.OrdinalIgnoreCase))
                        AllPlayers.Add(Name);
                }

                _onlinePlayers = AllPlayers;
                DispatcherQueue.TryEnqueue(() => PlayerCountButton.Content = $"{_onlinePlayers.Count} players");
            }
            catch
            {
                DispatcherQueue.TryEnqueue(() => PlayerCountButton.Content = "? players");
            }
        }

        private void ShowPlayerList(object Sender, RoutedEventArgs EventArgs)
        {
            DialogService.ShowPlayerListDialog(_onlinePlayers, "Players Online");
        }

        protected override void OnNavigatedTo(NavigationEventArgs EventArgs)
        {
            base.OnNavigatedTo(EventArgs);
            StartPlayerCountUpdates();
            AnimateBlur();
            UpdateIcons(GlobalSettings.Options.Theme ?? "Default");
        }

        protected override void OnNavigatedFrom(NavigationEventArgs EventArgs)
        {
            base.OnNavigatedFrom(EventArgs);
            _playerCountTimer?.Stop();
        }

        private void AnimateBlur()
        {
            var Animation = new Storyboard();
            var ColorAnimation = new ColorAnimation
            {
                From = Windows.UI.Color.FromArgb(178, 0, 0, 0),
                To = Windows.UI.Color.FromArgb(204, 0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(1250),
                EnableDependentAnimation = true,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(ColorAnimation, OverlayBrush);
            Storyboard.SetTargetProperty(ColorAnimation, "Color");
            Animation.Children.Add(ColorAnimation);
            Animation.Begin();
        }

        private async void Launch(object Sender, RoutedEventArgs EventArgs)
        {
            if (!Definitions.BindPlayButton)
                return;

            if (!PathHelper.IsPathValid(GlobalSettings.Options.FortnitePath))
            {
                DialogService.ShowSimpleDialog("You haven't selected a Fortnite installation path yet. Go to the Downloads tab and select your game folder.", "Installation Path Required");
                UI.ShowProgressRing((SettingsCard)Sender, false);
                return;
            }

            ShowDownloadProgress();
            UI.ShowProgressRing((SettingsCard)Sender, true);

            await Processes.ForceCloseFortnite();
            await Fortnite.Launch(GlobalSettings.Options.FortnitePath);

            DownloadInfo.IsOpen = false;
        }

        private void OnDownloadProgressChanged(string DownloadStatus)
        {
            Progress = DownloadStatus;
            DispatcherQueue.TryEnqueue(() => DownloadInfo.SetValue(TeachingTip.SubtitleProperty, DownloadStatus));
        }

        private async void ShowDownloadProgress()
        {
            DownloadInfo.IsOpen = true;
            while (DownloadInfo.IsOpen)
            {
                DispatcherQueue.TryEnqueue(() => DownloadInfo.Subtitle = DownloadService.DownloadProgress);
                await Task.Delay(5);
            }
            DownloadInfo.IsOpen = false;
        }

        private void LoadProfileImage()
        {
            string URL = GlobalSettings.Options.SkinUrl;

            if (!Uri.TryCreate(URL, UriKind.Absolute, out Uri ProfileUri))
            {
                Uri.TryCreate($"{Definitions.CDN_URL}/EonS17.png", UriKind.Absolute, out ProfileUri);
            }

            if (ProfileUri != null)
            {
                ProfileImageBrush.ImageSource = new BitmapImage(ProfileUri);
            }
        }

        private void OpenUri(string URI) => Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = URI });
        private void Tiktok(object Sender, RoutedEventArgs EventArgs) => OpenUri(ProjectDefinitions.Tiktok);
        private void Donations(object Sender, RoutedEventArgs EventArgs) => OpenUri(ProjectDefinitions.DonationsURL);

        public void UpdateIcons(string Theme)
        {
            string Suffix = Theme == "Light" ? "_B" : string.Empty;

            DonationsCard.HeaderIcon = new ImageIcon
            {
                Source = new BitmapImage(new Uri($"ms-appx:///Content/Texture/UI/T_Donate{Suffix}.png"))
            };

            TiktokCard.HeaderIcon = new ImageIcon
            {
                Source = new BitmapImage(new Uri($"ms-appx:///Content/Texture/UI/T_Tiktok{Suffix}.png"))
            };
        }
    }
}
