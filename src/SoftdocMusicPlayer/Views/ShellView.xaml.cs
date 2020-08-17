using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Helpers;
using SoftdocMusicPlayer.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SoftdocMusicPlayer.Views
{
    /// <summary>
    /// Home page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShellPage : Page, INotifyPropertyChanged
    {
        #region Private fields

        // What is the program's last known full-screen state?
        // We use this to detect when the system forced us out of full-screen mode.
        private bool isLastKnownFullScreen = ApplicationView.GetForCurrentView().IsFullScreenMode;

        private double _volume;
        private bool _isPageLoaded = false;
        private bool _shouldUpdate = true;
        private bool _isTapped = false;
        private bool _isMinimal = false;

        private ShellPage rootPage;
        private MediaViewModel viewModel;
        private SplitView splitView;
        private NavigationViewItemSeparator separator;
        private ContentControl suggestBox;
        private Button button;
        private Grid header1, header2, header3;

        #endregion Private fields

        #region Public fields

        public string AppName;
        public static DispatcherTimer timer;

        public double MediaVolume
        {
            get => this._volume;
            set
            {
                if (this._volume != value)
                {
                    _volume = value;
                    OnPropertyChanged("MediaVolume");
                }
            }
        }

        #endregion Public fields

        #region Default constructor

        public ShellPage()
        {
            // Enable page cache mode
            NavigationCacheMode = NavigationCacheMode.Enabled;
            // Get application display name.
            AppName = Windows.ApplicationModel.Package.Current.DisplayName;
            InitializeComponent();
            Loaded += ShellPage_Loaded;
            contentFrame.Navigate(typeof(MainPage));
            // Register to handle the app back request.
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
        }

        #endregion Default constructor

        #region Methods

        private void TimerTime_Tick(object sender, object e)
        {
            if (!_isTapped && _shouldUpdate)
            {
                TimelineSlider.Value = mediaElement.Position.TotalSeconds;
                var totalTime = mediaElement.Position.Duration();
                if (totalTime.Seconds < 10)
                {
                    CurrentTimeTextBlock.Text = $"{totalTime.Minutes}:0{ totalTime.Seconds}";
                }
                else
                {
                    CurrentTimeTextBlock.Text = $"{totalTime.Minutes}:{ totalTime.Seconds}";
                }
            }
        }

        private async void LoadMedia()
        {
            var id = await MediaViewModel.LoadMediaFromSettingsAsync();
            var volume = await MediaViewModel.LoadVolumeFromSettingsAsync();
            if (id != 0)
            {
                // Set media element source
                await viewModel.LoadMediaFromIdAsync(id);
            }
            // Set media element volume
            MediaVolume = volume;
            _isPageLoaded = true;
        }

        private void FetchControls()
        {
            MediaControlsModel.MP3Player = mediaElement;
            MediaControlsModel.PlayOrPauseBtn = PlayOrPause;
            MediaControlsModel.PlayOrPauseBtnIcon = PlayOrPauseIcon;
            MediaControlsModel.PreviousBtnIcon = PreviousIcon;
            MediaControlsModel.NextBtnIcon = NextIcon;
            MediaControlsModel.RepeatBtnIcon = RepeatIcon;
            MediaControlsModel.ShuffleBtnIcon = ShuffleIcon;
            MediaControlsModel.MusicMetadata = AlbumDetails;
            MediaControlsModel.Thumbnail = musicThumbnail;
            MediaControlsModel.Title = musicTitle;
            MediaControlsModel.Artist = musicArtist;
            MediaControlsModel.GlassHost = GlassHost;
            MediaControlsModel.Overlay = Overlay;
            ContentFrame.GetFrame = contentFrame;
        }

        private void NavigationPaneOpened()
        {
            // Update element properties when navigation pane is opened.
            AppTitle.Visibility = Visibility.Visible;

            if (suggestBox != null) suggestBox.Visibility = Visibility.Visible;
            if (separator != null) separator.Width = 286;
            if (button != null) button.Visibility = Visibility.Collapsed;
            if (header1 != null) header1.Height = 44;
            if (header2 != null) header2.Height = 44;
            if (header3 != null) header3.Height = 44;
        }

        private void NavigationPaneClosed()
        {
            // Update element properties when navigation pane is closed.
            AppTitle.Visibility = Visibility.Collapsed;

            if (suggestBox != null) suggestBox.Visibility = Visibility.Collapsed;
            if (separator != null) separator.Width = 58;
            if (button != null) button.Visibility = Visibility.Visible;
            if (header1 != null) header1.Height = 0;
            if (header2 != null) header2.Height = 0;
            if (header3 != null) header3.Height = 0;
        }

        private void UpdateContent()
        {
            var view = ApplicationView.GetForCurrentView();
            var isFullScreenMode = view.IsFullScreenMode;
            ToggleFullScreenMode.Icon = isFullScreenMode ? new SymbolIcon(Symbol.BackToWindow) : new SymbolIcon(Symbol.FullScreen);
            ToggleFullScreenMode.Label = isFullScreenMode ? "Back to normal screen" : "Switch to full screen";

            // Did the system force a change in full screen mode?
            if (isLastKnownFullScreen != isFullScreenMode)
            {
                isLastKnownFullScreen = isFullScreenMode;
                // Clear any stray messages that talked about the mode we are no longer in.
            }
        }

        #endregion Methods

        #region NotifyPropertyChanged Event

        // Event fires whenever property is changed.
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion NotifyPropertyChanged Event

        #region Events

        private MediaPlaybackList _mediaPlaybackList;

        private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
        {
            FetchControls();

            // Cross check if navigation pane is opened.

            if (NavView.IsPaneOpen)
            {
                NavigationPaneOpened();
            }
            else
            {
                NavigationPaneClosed();
            }

            // Initialize media functions
            viewModel = new MediaViewModel();
            LoadMedia();

            //_mediaPlaybackList = new MediaPlaybackList();

            ////Create a new picker
            //var filePicker = new Windows.Storage.Pickers.FileOpenPicker();

            ////Add filetype filters.

            //filePicker.FileTypeFilter.Add(".mp3");
            //filePicker.FileTypeFilter.Add(".wav");

            ////Set picker start location to the video library
            //filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;

            ////Retrieve multiple file from picker
            //var files = await filePicker.PickMultipleFilesAsync();

            //foreach (var file in files)
            //{
            //    var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromStorageFile(file));
            //    _mediaPlaybackList.Items.Add(mediaPlaybackItem);
            //}

            //_mediaPlaybackList.CurrentItemChanged += MediaPlaybackList_CurrentItemChanged;
            //_mediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
            //_mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;

            //_mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;

            //mediaElement.SetPlaybackSource(_mediaPlaybackList);
        }

        private void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            Debug.WriteLine($"CurrentItemChanged reason: {args.Reason.ToString()}");
        }

        private void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
        {
            throw new NotImplementedException();
        }

        private async void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                mediaElement.Play();
            });
        }

        /*----------------------------------------------------------------------------------------------------------+
        |                                          Navigation Events                                                |
        +----------------------------------------------------------------------------------------------------------*/

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            // handle page back request.
            try
            {
                if (contentFrame.CanGoBack)
                {
                    contentFrame.GoBack();
                    e.Handled = true;
                }
            }
            catch (Exception) { }
        }

        private void NavView_IntemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            // Navigate to the selected item in navigation view.
            if (args.IsSettingsInvoked)
            {
                contentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                NavigationViewItemBase item = args.InvokedItemContainer as NavigationViewItemBase;
                if (item.Tag == null)
                    return;
                switch (item.Tag.ToString())
                {
                    case "MainPage":
                        if (contentFrame.SourcePageType != typeof(MainPage))
                        {
                            contentFrame.Navigate(typeof(MainPage));
                        }
                        break;

                    case "AlbumsPage":
                        if (contentFrame.SourcePageType != typeof(AlbumsPage))
                        {
                            contentFrame.Navigate(typeof(AlbumsPage));
                        }
                        break;

                    case "ArtistPage":
                        if (contentFrame.SourcePageType != typeof(ArtistPage))
                        {
                            contentFrame.Navigate(typeof(ArtistPage));
                        }
                        break;

                    case "TracksPage":
                        if (contentFrame.SourcePageType != typeof(TracksPage))
                        {
                            contentFrame.Navigate(typeof(TracksPage));
                        }
                        break;
                }
            }
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility and padding.
            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = ((Frame)sender).CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

            appTitleContainer.Padding = contentFrame.CanGoBack ? new Thickness(60, 0, 0, 0) : new Thickness(12, 0, 0, 0);

            try
            {
                // Get page name.
                var pageName = contentFrame.Content.GetType().Name;
                // Find menu item that has the matching tag.
                var menuItem = NavView.MenuItems.OfType<NavigationViewItem>().Where(item => item.Tag.ToString() == pageName).First();
                if (menuItem == null) return;
                NavView.SelectedItem = menuItem;
            }
            catch (Exception) { }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get navigation view template controls.
                splitView = NavView.ChildrenBreadthFirst().OfType<SplitView>().First();
                separator = splitView.ChildrenBreadthFirst().OfType<NavigationViewItemSeparator>().First();
                suggestBox = splitView.FindName("PaneAutoSuggestBoxPresenter") as ContentControl;
                button = splitView.ChildrenBreadthFirst().OfType<Button>().First();
                header1 = NVIH1.ChildrenBreadthFirst().OfType<Grid>().First();
                header2 = NVIH2.ChildrenBreadthFirst().OfType<Grid>().First();
                //header3 = NVIH3.ChildrenBreadthFirst().OfType<Grid>().First();
            }
            catch (Exception) { }
            //finally
            //{
            //    NavigationViewItemHeader header = new NavigationViewItemHeader() { Content = "Playlists", Height=44 };
            //    var items = new ObservableCollection<NavigationViewItem>()
            //    {
            //        new NavigationViewItem() { Content = "Add new playlist", Icon = new SymbolIcon(Symbol.Add)},
            //        new NavigationViewItem() { Content = "Pop music", Icon = new SymbolIcon(Symbol.List)}
            //    };

            //    NavView.MenuItems.Add(header);
            //    foreach (var item in items)
            //    {
            //        NavView.MenuItems.Add(item);
            //    }
            //}

            // Register to handle the navigation pane opening and closing.
            splitView.PaneOpening += SplitView_PaneOpening;
            splitView.PaneClosing += SplitView_PaneClosing;
        }

        private void SplitView_PaneOpening(SplitView sv, object args)
        {
            // Each time pane open event occurs, update the controls's visibility and height.
            NavigationPaneOpened();
        }

        private void SplitView_PaneClosing(SplitView sv, SplitViewPaneClosingEventArgs args)
        {
            // Each time pane close event occurs, update the controls's visibility and height.
            NavigationPaneClosed();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = new ShellPage();

            Window.Current.SizeChanged += OnWindowResize;
            rootPage.KeyDown += OnKeyDown;
            UpdateContent();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Window.Current.SizeChanged -= OnWindowResize;
            rootPage.KeyDown -= OnKeyDown;
        }

        /*----------------------------------------------------------------------------------------------------------+
        |                                             Full screen feature                                           |
        +----------------------------------------------------------------------------------------------------------*/

        private void OnWindowResize(object sender, WindowSizeChangedEventArgs e)
        {
            UpdateContent();
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                var view = ApplicationView.GetForCurrentView();
                if (view.IsFullScreenMode)
                {
                    view.ExitFullScreenMode();
                    // Exited full screen mode due to keypress.
                    isLastKnownFullScreen = false;
                }
            }
        }

        private void ToggleFullScreenModeButton_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                // Exiting full screen mode.
                isLastKnownFullScreen = false;
                // The SizeChanged event will be raised when the exit from full screen mode is complete.
            }
            else
            {
                if (view.TryEnterFullScreenMode())
                {
                    // Entering full screen mode
                    isLastKnownFullScreen = true;
                    // The SizeChanged event will be raised when the entry to full screen mode is complete.
                }
                else
                {
                    // Failed to enter full screen mode.
                }
            }
        }

        private void ShowStandardSystemOverlaysButton_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            view.ShowStandardSystemOverlays();
        }

        private void UseMinimalOverlaysButton_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (_isMinimal)
            {
                view.FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Standard;
                UseMinimalOverlaysBtn.Icon.Visibility = Visibility.Collapsed;
                _isMinimal = false;
            }
            else
            {
                view.FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
                UseMinimalOverlaysBtn.Icon.Visibility = Visibility.Visible;
                _isMinimal = true;
            }
        }

        /*----------------------------------------------------------------------------------------------------------+
        |                                       Media Element Events                                                |
        +----------------------------------------------------------------------------------------------------------*/

        private async void PlayOrPauseBtn_Clicked(object sender, RoutedEventArgs e) => await viewModel.PlayOrPauseMediaAsync();

        private async void NextBtn_Clicked(object sender, RoutedEventArgs e) =>
            await viewModel.LoadNextMediaAsync();

        //_mediaPlaybackList.MoveNext();

        private async void PreviousBtn_Clicked(object sender, RoutedEventArgs e) =>
            await viewModel.LoadPreviousMediaAsync();

        //_mediaPlaybackList.MovePrevious();

        private void RepeatMedia_Clicked(object sender, RoutedEventArgs e)
        {
            // Toggle media looping.
            mediaElement.IsLooping = !mediaElement.IsLooping;
            ToolTipService.SetToolTip(sender as ToggleButton, mediaElement.IsLooping ? "Repeat: on" : "Repeat: off");
        }

        private async void OnVolumeChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var sliderValue = volumeSlider.Value;
            var volume = sliderValue / 100;
            mediaElement.Volume = volume;
            if (_isPageLoaded)
            {
                await MediaViewModel.SaveVolumeInSettingsAsync(sliderValue);
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Set max value for timeline slider
            TimelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
            // Store the time to be used in binding to UI.
            var totalTime = mediaElement.NaturalDuration.TimeSpan;
            if (totalTime.Seconds < 10)
            {
                LeftTimeTextBlock.Text = $"{totalTime.Minutes}:0{ totalTime.Seconds}";
            }
            else
            {
                LeftTimeTextBlock.Text = $"{totalTime.Minutes}:{ totalTime.Seconds}";
            }

            // Create a timer that will update the counters and the time slider
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += TimerTime_Tick;
        }

        private void MediaSlider_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isTapped = true;
            _shouldUpdate = false;
            mediaElement.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
            _isTapped = false;
            _shouldUpdate = true;
        }

        private void more_clicked(object sender, RoutedEventArgs e) => MoreAction.IsOpen = !MoreAction.IsOpen;

        private void MuteMedia_Clicked(object sender, RoutedEventArgs e)
        {
            // Mute or unmute media volume.
            if (ToggleMute.IsChecked.Value)
            {
                mediaElement.IsMuted = true;
                ToggleMuteIcon.Symbol = Symbol.Mute;
                ToolTipService.SetToolTip(sender as ToggleButton, "Mute: on");
            }
            else
            {
                mediaElement.IsMuted = false;
                ToggleMuteIcon.Symbol = Symbol.Volume;
                ToolTipService.SetToolTip(sender as ToggleButton, "Mute: off");
            }
        }

        private async void ClearMedia(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (timer != null)
                {
                    timer.Tick -= TimerTime_Tick;
                    timer.Stop();
                }
                mediaElement.Stop();
                mediaElement.ClearValue(MediaElement.SourceProperty);
                AlbumDetails.Visibility = Visibility.Collapsed;
                TimelineSlider.SetValue(RangeBase.ValueProperty, 0);
                TimelineSlider.ClearValue(RangeBase.MaximumProperty);
                CurrentTimeTextBlock.Text = "0:00";
                LeftTimeTextBlock.Text = "0:00";
                await MediaViewModel.SaveMediaInSettingsAsync(0);
            });
        }

        private void MediaSlider_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _isTapped = true;
            _shouldUpdate = false;
        }

        private void MediaSlider_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var slider = sender as Slider;
            mediaElement.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
            _shouldUpdate = true;
            _isTapped = false;
        }

        #endregion Events
    }
}
