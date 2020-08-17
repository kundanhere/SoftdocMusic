using ColorThiefDotNet;
using DataAccessLibrary;
using Microsoft.Toolkit.Uwp.UI.Animations;
using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Helpers;
using SoftdocMusicPlayer.Views;

using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftdocMusicPlayer.ViewModels
{
    public class MediaViewModel : Page
    {
        #region Private fields

        private const string MediaKey = "AppLastPlayedMedia";
        private const string VolumeKey = "AppLastMediaVolume";

        private MediaElement _media;
        private StorageFile _currentMediaFile;
        private StorageFolder _currentMediaFolder;
        private SystemMediaTransportControls _transportControls;

        #endregion Private fields

        #region Default constructor

        public MediaViewModel()
        {
            // Instantiate media element
            _media = MediaControlsModel.MP3Player;
            _transportControls = SystemMediaTransportControls.GetForCurrentView();

            // Register to handle the following system transpot control buttons.
            _media.MediaFailed += media_MediaFailed;
            _media.CurrentStateChanged += MediaElement_CurrentStateChanged;
            _transportControls.ButtonPressed += SystemControls_ButtonPressed;

            _transportControls.IsPlayEnabled = true;
            _transportControls.IsPauseEnabled = true;
            _transportControls.IsNextEnabled = true;
            _transportControls.IsPreviousEnabled = true;
        }

        #endregion Default constructor

        #region Methods

        public async Task PlayMediaAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // check if media is set to null
                if (ShellPage.timer is null)
                    return;
                else if (_media.CurrentState != MediaElementState.Playing)
                {
                    _media.Play();
                    ShellPage.timer.Start();
                }
            });
        }

        public async Task PlayOrPauseMediaAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // check if media is set to null
                if (ShellPage.timer is null)
                    return;
                else if (_media.CurrentState == MediaElementState.Playing)
                {
                    _media.Pause();
                    // Stop seek bar timer of base control.
                    ShellPage.timer.Stop();
                }
                else
                {
                    _media.Play();
                    // Start the seek bar timer of base control.
                    ShellPage.timer.Start();
                }
            });
        }

        public async Task LoadNextMediaAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // ...
            });
        }

        public async Task LoadPreviousMediaAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // ...
            });
        }

        public async Task StopMediaAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => _media.Stop());
        }

        public async Task ClearMedia()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _media.Stop();
                ShellPage.timer.Stop();
                _media.ClearValue(MediaElement.SourceProperty);
                await SaveMediaInSettingsAsync(0);
            });
        }

        public async Task UpdateBindingsAsync(StorageFile file)
        {
            // Get the updater.
            var properties = await file.Properties.GetMusicPropertiesAsync();
            var updater = _transportControls.DisplayUpdater;

            // Get Music metadata.
            var title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title;
            var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown Artist" : properties.Artist;
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 300);

            BitmapImage image;
            if (!(thumbnail is null))
            {
                image = new BitmapImage();
                image.SetSource(thumbnail);
            }
            else
            {
                image = ImageHelper.GetImageFromAssetsFile("default_album_300.png");
            }

            // Set the media controls metadata.
            MediaControlsModel.Title.Text = title;
            MediaControlsModel.Artist.Text = artist;
            MediaControlsModel.Thumbnail.ImageSource = image;

            // Set the system controls metadata.
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = title;
            updater.MusicProperties.Artist = artist;

            // Set the album art thumbnail.
            // RandomAccessStreamReference is defined in Windows.Storage.Streams
            updater.Thumbnail = thumbnail is null ? RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/default_album_300.png")) : RandomAccessStreamReference.CreateFromStream(thumbnail);

            // Update the system media transport controls.
            updater.Update();
            MediaControlsModel.MusicMetadata.Visibility = Visibility.Visible;
        }

        public async Task LoadMediaFromIdAsync(double id)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Get song metadata that corresponding to the Id.
                var data = await DataAccess.GetMediaPath(id);
                if (data == null) return;
                var path = data.First();
                var filename = data.Last();

                // Get storage file
                _currentMediaFolder = await StorageFolder.GetFolderFromPathAsync(path);
                _currentMediaFile = await _currentMediaFolder.GetFileAsync(filename);
                // Set new media source
                _media.SetSource(await _currentMediaFile.OpenAsync(FileAccessMode.Read), _currentMediaFile.ContentType);
                _media.MediaOpened += media_MediaOpenedAsync;
                // Update binding to ui elements and system controls.
                await UpdateBindingsAsync(_currentMediaFile);
                // Initialize new base color for media player controls.
                await InitializeAcrylicBrushAsync(_currentMediaFile);
                // Save media in settings, so whenever app loads it by default load last played media.
                await SaveMediaInSettingsAsync(id);
            });
        }

        public async Task InitializeAcrylicBrushAsync(StorageFile file)
        {
            // Get music thumbnail.
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 300);
            // Get dominant color from music thumbnail.
            var decoder = await BitmapDecoder.CreateAsync(thumbnail);
            var colorThief = new ColorThief();
            var dominantColor = await colorThief.GetColor(decoder, quality: 100);
            var color = dominantColor.Color;
            // Set UI Element color and opacity.
            MediaControlsModel.Overlay.Opacity = 0.7;
            MediaControlsModel.GlassHost.Background = new AcrylicBrush()
            {
                BackgroundSource = AcrylicBackgroundSource.Backdrop,
                TintColor = Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B),
                TintOpacity = 0.75f,
                TintLuminosityOpacity = 0.8f,
                FallbackColor = Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B),
                TintTransitionDuration = TimeSpan.FromMilliseconds(300)
            };
            // Animate overlay whenever new color is applied.
            await MediaControlsModel.Overlay.Fade(0.5f, 3000).StartAsync();
        }

        public static async Task<double> LoadMediaFromSettingsAsync()
        {
            // By default value is set to 0 which indicates to null, so media is not set on app load.
            double cacheMedia = 0;
            string mediaId = await ApplicationData.Current.LocalSettings.ReadAsync<string>(MediaKey);

            if (!string.IsNullOrEmpty(mediaId))
            {
                double.TryParse(mediaId, out cacheMedia);
            }

            return cacheMedia;
        }

        public static async Task SaveMediaInSettingsAsync(double id)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(MediaKey, id.ToString());
        }

        public static async Task<double> LoadVolumeFromSettingsAsync()
        {
            // By default volume is set to 50.
            double cacheVolume = 50;
            string volume = await ApplicationData.Current.LocalSettings.ReadAsync<string>(VolumeKey);

            if (!string.IsNullOrEmpty(volume))
            {
                double.TryParse(volume, out cacheVolume);
            }

            return cacheVolume;
        }

        public static async Task SaveVolumeInSettingsAsync(double volume)
        {
            await ApplicationData.Current.LocalSettings.SaveAsync(VolumeKey, volume.ToString());
        }

        #endregion Methods

        #region Events

        private void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            throw new Exception("Media is faild to load, Please try another song.");
        }

        private async void media_MediaOpenedAsync(object sender, RoutedEventArgs e)
        {
            await PlayOrPauseMediaAsync();
        }

        private void MediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            // Update symbols and tooltips of system controls and media controls.
            switch (_media.CurrentState)
            {
                case MediaElementState.Playing:
                    _transportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    MediaControlsModel.PlayOrPauseBtnIcon.Symbol = Symbol.Pause;
                    ToolTipService.SetToolTip(MediaControlsModel.PlayOrPauseBtn, "Pause");
                    break;

                case MediaElementState.Paused:
                    _transportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    MediaControlsModel.PlayOrPauseBtnIcon.Symbol = Symbol.Play;
                    ToolTipService.SetToolTip(MediaControlsModel.PlayOrPauseBtn, "Play");
                    break;

                case MediaElementState.Stopped:
                    _transportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    MediaControlsModel.PlayOrPauseBtnIcon.Symbol = Symbol.Play;
                    ToolTipService.SetToolTip(MediaControlsModel.PlayOrPauseBtn, "Play");
                    break;

                case MediaElementState.Closed:
                    _transportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    MediaControlsModel.PlayOrPauseBtnIcon.Symbol = Symbol.Play;
                    ToolTipService.SetToolTip(MediaControlsModel.PlayOrPauseBtn, "Play");
                    break;
            }
        }

        private async void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await PlayOrPauseMediaAsync();
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    await PlayOrPauseMediaAsync();
                    break;

                case SystemMediaTransportControlsButton.Next:
                    await LoadNextMediaAsync();
                    break;

                case SystemMediaTransportControlsButton.Previous:
                    await LoadPreviousMediaAsync();
                    break;

                case SystemMediaTransportControlsButton.Stop:
                    await StopMediaAsync();
                    break;
            }
        }

        #endregion Events
    }
}
