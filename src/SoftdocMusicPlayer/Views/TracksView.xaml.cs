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
using Windows.UI.Xaml.Navigation;

namespace SoftdocMusicPlayer.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TracksPage : Page, INotifyPropertyChanged
    {
        #region Private fields

        private string _total = $"Shuffle all ({0})";
        private ObservableCollection<SongModel> _collection;
        private MediaViewModel _viewModel;
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

        public TracksPage()
        {
            // Enable page cache mode
            NavigationCacheMode = NavigationCacheMode.Required;
            InitializeComponent();
            // Load artist
            LoadData();
            this.DataContext = Collection;
            // Register to handle the on load event
            Loaded += TracksPage_Loaded;
        }

        #endregion Default Constructor

        #region NotifyPropertyChanged Event

        // Event fires whenever property is changed.
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion NotifyPropertyChanged Event

        #region Methods

        /// <summary>
        /// Load artist data
        /// </summary>
        private void LoadData()
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // Get all music data
                Collection = await DataAccessLibrary.DataAccess.GetMediaAsync();

                // display total songs
                var total = Collection.Count();
                Total = $"Shuffle all ({total})";

                // Sort by default
                SortingHelper.Sort(Collection, SortingHelper.SortByName);
            });
        }

        #endregion Methods

        #region Events

        private void TracksPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollBar that the GridView is using internally.
            var scrollBar = collectionListView.ChildrenBreadthFirst().OfType<ScrollBar>().First();
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
            // Retrieve the "list view item" of clicked item.
            var content = ViewHelper.FindParent<ContentPresenter>(sender as DependencyObject);
            if (content == null)
                return;
            if (content.DataContext is SongModel container)
            {
                _viewModel = new MediaViewModel();
                await _viewModel.LoadMediaFromIdAsync(container.Id);
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
                    SortingHelper.Sort(Collection, SortingHelper.SortByName);
                    break;

                case 2:
                    SortingHelper.Sort(Collection, SortingHelper.SortByYear);
                    break;
            }
        }

        #endregion Events
    }
}
