using DataAccessLibrary;
using Microsoft.Toolkit.Uwp.UI.Animations;
using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Helpers;
using SoftdocMusicPlayer.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SoftdocMusicPlayer.Views
{
    /// <summary>
    /// Albums page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumsPage : Page, INotifyPropertyChanged
    {
        #region Private fields

        private string _totalAlbums = $"Shuffle all ({0})";
        private ObservableCollection<SongModel> _albums;
        private MediaViewModel _model;
        private object _storedItem;

        #endregion Private fields

        #region Public fields and properties

        public ObservableCollection<SongModel> AlbumsItem
        {
            get => _albums;
            set
            {
                if (_albums != value)
                {
                    _albums = value;
                    NotifyPropertyChanged("AlbumsItem");
                }
            }
        }

        public string TotalAlbums
        {
            get => _totalAlbums;
            set
            {
                if (_totalAlbums != value)
                {
                    _totalAlbums = value;
                    NotifyPropertyChanged("TotalAlbums");
                }
            }
        }

        #endregion Public fields and properties

        #region Default Constructor

        public AlbumsPage()
        {
            // Enable page cache mode
            NavigationCacheMode = NavigationCacheMode.Required;
            InitializeComponent();
            // Load albums
            LoadData();
            this.DataContext = AlbumsItem;
            // Register to handle the on load event
            Loaded += AlbumsPage_Loaded;
        }

        #endregion Default Constructor

        #region NotifyPropertyChanged Event

        // Event fires whenever property is changed.
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion NotifyPropertyChanged Event

        #region Choriographed animations

        private void GridViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // fade up to 1
            var buttons = ((StackPanel)(sender as Grid).FindName("ButtonContainer"));
            if (!(buttons is null))
            {
                _ = buttons.Fade(1, 300).StartAsync();
            }
        }

        private void GridViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // fade up to 0
            var buttons = ((StackPanel)(sender as Grid).FindName("ButtonContainer"));
            if (!(buttons is null))
            {
                _ = buttons.Fade(0, 300).StartAsync();
            }
        }

        private async void element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Scale up to 1.15
            _ = await (sender as UIElement).Scale(scaleX: 1.15f,
                scaleY: 1.15f,
                centerX: 25f,
                centerY: 25f,
                duration: 500, delay: 0).StartAsync();
        }

        private async void element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            _ = await (sender as UIElement).Scale(scaleX: 1.0f,
                scaleY: 1.0f,
                centerX: 25f,
                centerY: 25f,
                duration: 500, delay: 0).StartAsync();
        }

        #endregion Choriographed animations

        #region Connected page animation

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Retrieve the GridView item container of clicked item.
            var container = AlbumCollectionGridView.ContainerFromItem(e.ClickedItem) as GridViewItem;
            if (container != null)
            {
                _storedItem = container.Content as SongModel;

                // Prepare the ConnectedAnimation
                AlbumCollectionGridView.PrepareConnectedAnimation("forwardAnimation", _storedItem, "SongThumbnail");
            }

            // Navigate to the DestinationPage
            var Frame = ContentFrame.GetFrame;
            Frame.Navigate(typeof(DetailedAlbumPage), _storedItem, new DrillInNavigationTransitionInfo());
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                try
                {
                    // if the connected item appears outside the viewport, scroll it into view
                    AlbumCollectionGridView.ScrollIntoView(_storedItem, ScrollIntoViewAlignment.Default);
                    AlbumCollectionGridView.UpdateLayout();
                }
                catch (Exception) { }
                finally
                {
                    // Play the second connected animation
                    var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("backAnimation");

                    if (animation != null)
                    {
                        _ = AlbumCollectionGridView.TryStartConnectedAnimationAsync(animation, _storedItem, "SongThumbnail");
                    }
                }
            }
        }

        #endregion Connected page animation

        #region Methods

        /// <summary>
        /// Load albums data
        /// </summary>
        private void LoadData()
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Get all albums
                // and merge duplicate albums.
                var original = await DataAccess.GetMediaAsync();
                var unique = original.GroupBy(x => new { x.Album, x.AlbumArtist }).Select(g => g.First());
                AlbumsItem = new ObservableCollection<SongModel>(unique);

                // display total albums
                var total = AlbumsItem.Count();
                TotalAlbums = $"Shuffle all ({total})";
                // Sort by default
                SortingHelper.Sort(AlbumsItem, SortingHelper.DescendingSortByYear);
            });
        }

        #endregion Methods

        #region Events

        private void AlbumsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollBar that the GridView is using internally.
            var scrollBar = AlbumCollectionGridView.ChildrenBreadthFirst().OfType<ScrollBar>().First();
            // Set scrollbar margin
            if (scrollBar != null)
                scrollBar.Margin = new Thickness(0, 0, -24, 90);
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // Set default source for an image if binding source is null.
            (sender as Image).Source = ImageHelper.GetImageFromAssetsFile("default_album_300.png");
        }

        private async void PlayBtn_Clicked(object sender, RoutedEventArgs e)
        {
            // Retrieve the "grid view item" of clicked item.
            var itemPresenter = ViewHelper.FindParent<ListViewItemPresenter>(sender as DependencyObject);
            if (itemPresenter == null)
                return;
            if (itemPresenter.DataContext is SongModel container)
            {
                _model = new MediaViewModel();
                await _model.LoadMediaFromIdAsync(container.Id);
            }
        }

        private void DataSort(object sender, RoutedEventArgs e) =>
            // Toggle "Combo box" options visibility.
            SortingOption.IsDropDownOpen = !SortingOption.IsDropDownOpen;

        private void SortingChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlbumsItem == null)
                return;

            // Find index of selected item
            // Sort the data to its index number.
            var index = (sender as ComboBox).SelectedIndex;
            switch (index)
            {
                case 0:
                    SortingHelper.Sort(AlbumsItem, SortingHelper.DescendingSortByYear);
                    break;

                case 1:
                    SortingHelper.Sort(AlbumsItem, SortingHelper.SortByAlbum);
                    break;

                case 2:
                    SortingHelper.Sort(AlbumsItem, SortingHelper.SortByYear);
                    break;

                case 3:
                    SortingHelper.Sort(AlbumsItem, SortingHelper.SortByAlbumArtist);
                    break;
            }
        }

        #endregion Events
    }
}
