using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Windows.Storage.Pickers;

namespace FortniteLauncher.Pages
{
    public sealed partial class DownloadsPage : Page
    {
        private string CurrentPath;
        private string BuildPath;

        public DownloadsPage()
        {
            this.InitializeComponent();
            InitializeBuildPath();

            Loaded += DownloadsPage_Loaded;
            Unloaded += DownloadsPage_Unloaded;
        }

        private void DownloadsPage_Loaded(object Sender, RoutedEventArgs EventArgs)
        {
            Localization.LanguageChanged += ApplyLocalization;
            ApplyLocalization();
        }

        private void DownloadsPage_Unloaded(object Sender, RoutedEventArgs EventArgs)
        {
            Localization.LanguageChanged -= ApplyLocalization;
        }

        private void ApplyLocalization()
        {
            InstallDirHeaderText.Text = Localization.Get("InstallDirHeader");
            SelectPathButton.Content = Localization.Get("SelectPathButton");

            InitializeBuildPath();

            BuildHeaderText.Text = string.Format(Localization.Get("BuildHeaderFormat"), ProjectDefinitions.Name, ProjectDefinitions.Build, ProjectDefinitions.ContentLevel);

            DownloadInProgressInfoBar.Title = string.Format(Localization.Get("DownloadTitleFormat"), ProjectDefinitions.Build);
            DownloadInProgressInfoBar.Message = Localization.Get("DownloadInProgressMessage");

            InstallHeaderText.Text = string.Format(Localization.Get("InstallHeaderFormat"), ProjectDefinitions.Name);
            InstallBodyText.Text = string.Format(Localization.Get("InstallBodyFormat"), ProjectDefinitions.Build, ProjectDefinitions.Name);
            DownloadBuildButton.Content = Localization.Get("DownloadButton");

            UninstallSectionHeaderText.Text = Localization.Get("UninstallSectionHeader");
            UninstallHeaderText.Text = string.Format(Localization.Get("UninstallHeaderFormat"), ProjectDefinitions.Name);
            UninstallBodyText.Text = string.Format(Localization.Get("UninstallBodyFormat"), ProjectDefinitions.Chapter, ProjectDefinitions.Season, ProjectDefinitions.Name);
            Delete.Content = Localization.Get("UninstallButton");
        }

        private void InitializeBuildPath()
        {
            if (GlobalSettings.Options.FortnitePath == null || !PathHelper.IsPathValid(GlobalSettings.Options.FortnitePath))
            {
                CurrentPath = Localization.Get("GamePathPlaceholder");
                BuildPath = Localization.Get("BuildPathInvalid");
            }
            else
            {
                CurrentPath = GlobalSettings.Options.FortnitePath;
                BuildPath = string.Format(Localization.Get("BuildPathValidFormat"), ProjectDefinitions.Chapter, ProjectDefinitions.Season);
            }

            CurrentPathText.Text = CurrentPath;
            BuildPathText.Text = BuildPath;
        }

        private async void DeleteBuild(object Sender, RoutedEventArgs EventArgs)
        {
            string ConfirmationMessage = string.Format(Localization.Get("DeleteConfirmFormat"), ProjectDefinitions.Build);
            string ConfirmationTitle = string.Format(Localization.Get("DeleteConfirmTitleFormat"), ProjectDefinitions.Name);
            bool Confirmed = await DialogService.YesOrNoDialog(ConfirmationMessage, ConfirmationTitle);

            if (!Confirmed)
            {
                DialogService.ShowSimpleDialog(
                    string.Format(Localization.Get("DeleteCancelledFormat"), ProjectDefinitions.Build),
                    Localization.Get("DeleteCancelledTitle"));
                return;
            }

            try
            {
                if (!Directory.Exists(GlobalSettings.Options.FortnitePath))
                {
                    DialogService.ShowSimpleDialog(Localization.Get("NotFoundMsg"), Localization.Get("NotFoundTitle"));
                    return;
                }

                Directory.Delete(GlobalSettings.Options.FortnitePath, true);
                DialogService.ShowSimpleDialog(
                    string.Format(Localization.Get("RemovalSuccessFormat"), ProjectDefinitions.Name),
                    Localization.Get("RemovalSuccessTitle"));
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog(
                    string.Format(Localization.Get("GenericErrorFormat"), Exception.Message),
                    Localization.Get("GenericErrorTitle"));
            }
        }

        private async void ChangeInstallPath(object Sender, RoutedEventArgs EventArgs)
        {
            if (Sender is not Button Button)
                return;

            Button.IsEnabled = false;

            try
            {
                var Picker = new FolderPicker(Button.XamlRoot.ContentIslandEnvironment.AppWindowId);
                Picker.CommitButtonText = Localization.Get("SelectFolderCommitButton");
                Picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                Picker.ViewMode = PickerViewMode.Thumbnail;
                Picker.FileTypeFilter.Add("*");

                var SelectedFolder = await Picker.PickSingleFolderAsync();

                if (SelectedFolder == null)
                {
                    DialogService.ShowSimpleDialog(Localization.Get("NoFolderSelectedMsg"), Localization.Get("NoFolderSelectedTitle"));
                    return;
                }

                string FolderPath = SelectedFolder.Path;
                string[] CompressedExtensions = { ".rar", ".zip", ".7z" };

                if (CompressedExtensions.Any(Extension => FolderPath.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)))
                {
                    DialogService.ShowSimpleDialog(Localization.Get("CompressedFileMsg"), Localization.Get("CompressedFileTitle"));
                    return;
                }

                if (!PathHelper.IsPathValid(FolderPath))
                {
                    string ValidPath = PathHelper.FindValidInstallationPath(FolderPath);
                    if (string.IsNullOrEmpty(ValidPath))
                    {
                        DialogService.ShowSimpleDialog(Localization.Get("InvalidPathMsg"), Localization.Get("InvalidPathTitle"));
                        return;
                    }
                    FolderPath = ValidPath;
                }

                GlobalSettings.Options.FortnitePath = FolderPath;
                UserSettings.SaveSettings();

                Frame.Navigate(typeof(DownloadsPage), "Downloads");
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog(
                    string.Format(Localization.Get("GenericErrorFormat"), Exception.Message),
                    Localization.Get("GenericErrorTitle"));
            }
            finally
            {
                Button.IsEnabled = true;
            }
        }

        private void DownloadBuild(object Sender, RoutedEventArgs EventArgs)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProjectDefinitions.DownloadBuildURL,
                    UseShellExecute = true
                });
            }
            catch (Exception Exception)
            {
                DialogService.ShowSimpleDialog(
                    string.Format(Localization.Get("DownloadUrlErrorFormat"), Exception.Message),
                    Localization.Get("GenericErrorTitle"));
            }
        }
    }
}