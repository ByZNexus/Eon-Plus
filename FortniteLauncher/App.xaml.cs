using FortniteLauncher.Pages;
using FortniteLauncher.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;

namespace FortniteLauncher
{
    public partial class App : Application
    {
        private Mutex MutexInstance;
        private Window MainWindowInstance;

        public App()
        {
            try
            {
                InitializeComponent();
                EnsureSingleInstance();
                Processes.ForceCloseFortnite();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Woah, there's an error: {ex.Message}");
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs Arguments)
        {
            try
            {
                UserSettings.LoadSettings();
                if (!string.IsNullOrEmpty(GlobalSettings.Options.Language))
                    Localization.SetLanguage(GlobalSettings.Options.Language);
                InitializeMainWindow();
                ConfigureSettings();
                _ = CheckForUpdatesAsync();
            }
            catch (Exception Error)
            {
                MessageBox.Show($"Report this error on our GitHub page at https://github.com/ByZNexus/Eon-Plus/issues, or make a ticket in the Eon Support server: {Error.Message}", "Error");
            }
        }

        private async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update == null) return;

            var titlePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            titlePanel.Children.Add(new Image
            {
                Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                    new Uri(System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "Texture", "Branding", "EonPlus.ico"))),
                Width = 24,
                Height = 24
            });

            titlePanel.Children.Add(new TextBlock
            {
                Text = Localization.Get("UpdateAvailableTitle"),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            var contentPanel = new StackPanel
            {
                Spacing = 4
            };

            contentPanel.Children.Add(new TextBlock
            {
                Text = Localization.Get("UpdateAvailableDescription"),
                TextWrapping = TextWrapping.Wrap
            });

            contentPanel.Children.Add(new TextBlock
            {
                Text = " ",
                FontSize = 4
            });

            contentPanel.Children.Add(new TextBlock
            {
                Text = string.Format(Localization.Get("CurrentVersionFormat"), UpdateService.GetCurrentVersionString())
            });

            contentPanel.Children.Add(new TextBlock
            {
                Text = string.Format(Localization.Get("LatestVersionFormat"), update.Version)
            });

            var dialog = new ContentDialog
            {
                Title = titlePanel,
                Content = contentPanel,
                PrimaryButtonText = Localization.Get("GoToReleaseButton"),
                CloseButtonText = Localization.Get("LaterButton"),
                PrimaryButtonStyle = (Style)Application.Current.Resources["AccentButtonStyle"],
                XamlRoot = MainWindowInstance.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await Windows.System.Launcher.LaunchUriAsync(new Uri(update.ReleaseUrl));
            }
        }

        private void EnsureSingleInstance()
        {
            MutexInstance = new Mutex(true, ProjectDefinitions.Name, out bool CreatedNew);
            if (CreatedNew)
            {
                MutexInstance.ReleaseMutex();
                return;
            }
            MessageBox.Show($"{ProjectDefinitions.Name} Launcher is already running. Please close it before opening a new instance.", "Already Running");
            Environment.Exit(1);
        }

        private void InitializeMainWindow()
        {
            MainWindowInstance = new MainWindow();
            MainWindowInstance.Activate();
            GlobalSettings.Windows = MainWindowInstance;
        }

        private void ConfigureSettings()
        {
            if (GlobalSettings.Options.IsSoundEnabled)
            {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
            }
        }
    }
}