using System;
using System.Linq;
using System.Collections.Generic;
using Windows.System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUIEx;
using FortniteLauncher.Pages;
using Windows.Media.Core;

namespace FortniteLauncher
{
    public sealed partial class MainWindow : WindowEx
    {
        public string LauncherName { get; } = $"{ProjectDefinitions.Name} ";
        public static Frame ShellFrame { get; private set; }

        private MediaPlayerElement _videoBackground;

        private static readonly List<VirtualKey> KonamiSequence = new()
        {
            VirtualKey.Up, VirtualKey.Up,
            VirtualKey.Down, VirtualKey.Down,
            VirtualKey.Left, VirtualKey.Right,
            VirtualKey.Left, VirtualKey.Right,
            VirtualKey.B, VirtualKey.A
        };

        private readonly List<VirtualKey> _inputBuffer = new();

        public MainWindow()
        {
            InitializeComponent();
            ConfigureWindow();
            ConfigureBackdrop();
            InitializeNavigation();

            GlobalSettings.Windows = this;

            this.Activated += (s, e) =>
            {
                if (Content != null)
                    Content.Focus(FocusState.Programmatic);
            };

            if (this.Content != null)
                this.Content.PreviewKeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            VirtualKey pressedKey = e.Key;
            _inputBuffer.Add(pressedKey);

            if (_inputBuffer.Count > KonamiSequence.Count)
                _inputBuffer.RemoveAt(0);

            if (_inputBuffer.Count == KonamiSequence.Count &&
                _inputBuffer.SequenceEqual(KonamiSequence))
            {
                _inputBuffer.Clear();
                OnKonamiActivated();
                e.Handled = true;
            }
        }

        private void OnKonamiActivated()
        {
            GlobalSettings.SecretThemesUnlocked = true;
            DialogService.ShowSimpleDialog("Wow u found the secret! go check ur themes!", "Konami Code");

            if (MainWindowFrame?.Content != null)
            {
                Type currentPageType = MainWindowFrame.Content.GetType();
                MainWindowFrame.Navigate(currentPageType);
            }

            if (ShellFrame?.Content != null)
            {
                Type currentShellPageType = ShellFrame.Content.GetType();
                ShellFrame.Navigate(currentShellPageType);
            }
        }

        private void ConfigureWindow()
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            this.SetWindowSize(1200, 725);
            this.CenterOnScreen();
            this.SetIcon("Content\\Texture\\Branding\\EonPlus.ico");
            Title = LauncherName;
            SetTitleBarColors();
        }

        private void ConfigureBackdrop()
        {
            SystemBackdrop = new MicaBackdrop
            {
                Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
            };
        }

        private void InitializeNavigation()
        {
            if (Content is FrameworkElement fe)
                fe.Focus(FocusState.Programmatic);

            ShellFrame = MainWindowFrame;
            MainWindowFrame.Navigate(typeof(LoginPage));
        }

        public void SetWindowBackground(Brush Brush)
        {
            if (RootGrid != null)
                RootGrid.Background = Brush;
            else if (Content is Grid rootGrid)
                rootGrid.Background = Brush;

            if (AppTitleBar != null)
                AppTitleBar.Background = new SolidColorBrush(Colors.Transparent);

            SetTitleBarColors();
        }

        private void SetTitleBarColors()
        {
            if (AppWindow?.TitleBar != null)
            {
                AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.InactiveBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
                AppWindow.TitleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(60, 255, 255, 255);
                AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
        }

        public void SetVideoBackground(string Uri)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ClearVideoBackground();

                _videoBackground = new MediaPlayerElement
                {
                    Source = MediaSource.CreateFromUri(new Uri("file:///" + Uri.Replace("\\", "/"))),
                    Stretch = Stretch.UniformToFill,
                    AutoPlay = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    IsHitTestVisible = false,
                    Width = double.NaN,
                    Height = double.NaN
                };

                _videoBackground.MediaPlayer.IsMuted = true;
                _videoBackground.MediaPlayer.IsLoopingEnabled = true;

                if (RootGrid != null)
                {
                    Grid.SetRow(_videoBackground, 0);
                    Grid.SetColumn(_videoBackground, 0);
                    Grid.SetRowSpan(_videoBackground, 2);
                    Grid.SetColumnSpan(_videoBackground, 2);
                    RootGrid.Children.Insert(0, _videoBackground);
                    RootGrid.Background = new SolidColorBrush(Colors.Transparent);
                }

                SetTitleBarColors();
            });
        }

        public void ClearVideoBackground()
        {
            if (_videoBackground != null)
            {
                try { _videoBackground.MediaPlayer?.Pause(); } catch { }
                try { _videoBackground.Source = null; } catch { }

                if (RootGrid != null)
                    RootGrid.Children.Remove(_videoBackground);

                _videoBackground = null;
            }
        }
        public bool IsVideoBackgroundActive()
        {
            return _videoBackground != null;
        }
    }
}  // THIS WAS MADE BY WTC AND BYZN ANYONE WHO SKIDS OF US! THIS PROJECT IS CALLED EON+