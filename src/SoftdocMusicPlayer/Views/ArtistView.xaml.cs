using Microsoft.Toolkit.Uwp.UI.Animations;
using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Helpers;
using SoftdocMusicPlayer.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SoftdocMusicPlayer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ArtistPage : Page, INotifyPropertyChanged
    {
        #region Private fields

        private string _total = $"Shuffle all ({0})";
        private ObservableCollection<SongModel> _collection;
        private MediaViewModel _model;
        private object _storedItem;

        #endregion Private fields

        #region Public fields and properties

        public ObservableCollection<SongModel> Collection
        {
            get => _collection;
            set
            {
                if (_collection != value)
                {
                    _collection = value;
                    NotifyPropertyChanged("Collection");
                }
            }
        }

        public string Total
        {
            get => _total;
            set
            {
                if (_total != value)
                {
                    _total = value;
                    NotifyPropertyChanged("Total");
                }
            }
        }

        #endregion Public fields and properties

        #region Default Constructor

        public ArtistPage()
        {
            // Enable page cache mode
            NavigationCacheMode = NavigationCacheMode.Required;
            InitializeComponent();
            // Load artist
            LoadData();
            this.DataContext = Collection;
            // Register to handle the on load event
            Loaded += ArtistPage_Loaded;
        }

        private void ArtistPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollBar that the GridView is using internally.
            var scrollBar = collectionGridView.ChildrenBreadthFirst().OfType<ScrollBar>().First();
            // Set scrollbar margin
            if (scrollBar != null)
                scrollBar.Margin = new Thickness(0, 0, -24, 90);
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

        #region Methods

        /// <summary>
        /// Load artist data
        /// </summary>
        private void LoadData()
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Get all music data
                // and merge duplicate artist.
                var original = await DataAccessLibrary.DataAccess.GetMediaAsync();
                var unique = original.GroupBy(x => x.Artist).Select(g => g.First());
                Collection = new ObservableCollection<SongModel>(unique);

                // display total artist
                var total = Collection.Count();
                Total = $"Shuffle all ({total})";

                // Sort by default
                SortingHelper.Sort(Collection, SortingHelper.SortByArtist);
            });
        }

        #endregion Methods

        #region Events

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
            if (Collection == null)
                return;

            // Find index of selected item
            // Sort the data to its index number.
            var index = (sender as ComboBox).SelectedIndex;
            switch (index)
            {
                case 0:
                    SortingHelper.Sort(Collection, SortingHelper.DescendingSortByYear);
                    break;

                case 1:
                    SortingHelper.Sort(Collection, SortingHelper.SortByArtist);
                    break;

                case 2:
                    SortingHelper.Sort(Collection, SortingHelper.SortByYear);
                    break;
            }
        }

        #endregion Events
    }
}
