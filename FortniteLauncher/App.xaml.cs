using FortniteLauncher.Pages;
using Microsoft.UI.Xaml;
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
            }
            catch (Exception Error)
            {
                MessageBox.Show($"Report this error on our GitHub page at https://github.com/ByZNexus/Eon-Plus/issues, or make a ticket in the Eon Support server: {Error.Message}", "Error");
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