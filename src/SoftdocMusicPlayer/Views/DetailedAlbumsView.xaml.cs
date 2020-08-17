using ExpressionBuilder;
using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Helpers;
using SoftdocMusicPlayer.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using EF = ExpressionBuilder.ExpressionFunctions;
using ExpressionNode = ExpressionBuilder.ExpressionNode;

namespace SoftdocMusicPlayer.Views
{
    public sealed partial class DetailedAlbumPage : Page, INotifyPropertyChanged
    {
        #region Private fields

        private ObservableCollection<SongModel> _collection;
        private SongModel _detailedObject;
        private MediaViewModel _viewModel;
        private CompositionPropertySet _props;
        private CompositionPropertySet _scrollerPropertySet;
        private Compositor _compositor;

        #endregion Private fields

        #region Public fields

        public ObservableCollection<SongModel> Collection
        {
            get => _collection;
            set
            {
                if (_collection != value)
                {
                    _collection = value;
                    OnPropertyChanged("Collection");
                }
            }
        }

        #endregion Public fields

        #region Default constructor

        public DetailedAlbumPage()
        {
            this.InitializeComponent();
            // Register event to handle "on scroll" animation
            Loaded += DetailedAlbumPage_Loaded;

            _detailedObject = new SongModel();
            _viewModel = new MediaViewModel();
            Collection = new ObservableCollection<SongModel>();
        }

        #endregion Default constructor

        #region NotifyPropertyChanged Event

        // Event fires whenever property is changed.
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion NotifyPropertyChanged Event

        #region Conntected page animation

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Forward connected page animation.
            base.OnNavigatedTo(e);
            // Store the item to be used in binding to UI.
            _detailedObject = e.Parameter as SongModel;

            // Get animation from the destination page.
            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");

            if (animation != null)
            {
                // new connected animation configuration.
                CreateNewConfiguration(animation);
                // Create coordinated animation
                CreateImplicitAnimations();

                // Play connected animation
                animation.TryStart(ProfileImage);
            }

            // Update UI bindings.
            await UpdateBindings();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Backward connected page animation.
            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                var animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backAnimation", ProfileImage);

                if (animation != null)
                {
                    // Use the recommended configuration for back animation.
                    animation.Configuration = new DirectConnectedAnimationConfiguration();
                }
            }
        }

        #endregion Conntected page animation

        #region Methods

        /// <summary>
        /// Set compositor value if null
        /// </summary>
        private void FetchCompositor()
        {
            if (_compositor == null)
            {
                _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            }
        }

        /// <summary>
        /// Panel to be coordinated with connected animation.
        /// </summary>
        private void CreateImplicitAnimations()
        {
            TextContainer.Opacity = 0;
            FetchCompositor();

            // Animate the "more info" panel opacity when it first shows.
            var fadeMoreInfroPanel = _compositor.CreateScalarKeyFrameAnimation();
            fadeMoreInfroPanel.InsertKeyFrame(0, 0f);
            fadeMoreInfroPanel.InsertKeyFrame(1, 1f);
            fadeMoreInfroPanel.Duration = TimeSpan.FromSeconds(1.5f);
            fadeMoreInfroPanel.Target = "Opacity";

            // Set animation at the right place.
            ElementCompositionPreview.SetImplicitShowAnimation(TextContainer, fadeMoreInfroPanel);
        }

        /// <summary>
        /// Create new configuration for connected page animation.
        /// </summary>
        /// <param name="animation"></param>
        private void CreateNewConfiguration(ConnectedAnimation animation)
        {
            FetchCompositor();
            animation.Configuration = new DirectConnectedAnimationConfiguration();

            // Configure for x-axis
            var offsetXAnimation = _compositor.CreateScalarKeyFrameAnimation();
            offsetXAnimation.Duration = System.TimeSpan.FromSeconds(0.75f);
            offsetXAnimation.InsertExpressionKeyFrame(0.0f, "StartingValue");
            offsetXAnimation.InsertExpressionKeyFrame(0.5f, "FinalValue");
            offsetXAnimation.InsertExpressionKeyFrame(1.0f, "FinalValue");
            animation.SetAnimationComponent(ConnectedAnimationComponent.OffsetX, offsetXAnimation);
            // Configure for y-axis
            var offsetYAnimation = _compositor.CreateScalarKeyFrameAnimation();
            offsetYAnimation.Duration = System.TimeSpan.FromSeconds(0.75f);
            offsetYAnimation.InsertExpressionKeyFrame(0.0f, "StartingValue");
            offsetYAnimation.InsertExpressionKeyFrame(0.5f, "FinalValue");
            offsetYAnimation.InsertExpressionKeyFrame(1.0f, "FinalValue");
            animation.SetAnimationComponent(ConnectedAnimationComponent.OffsetY, offsetYAnimation);
        }

        /// <summary>
        /// Update bindings to the UI elements.
        /// </summary>
        /// <returns></returns>
        private async System.Threading.Tasks.Task UpdateBindings()
        {
            if (_detailedObject != null)
            {
                // Update header bindings.
                ProfileImageBrush.ImageSource = _detailedObject.Thumbnail;
                BackgroundHost.ImageSource = _detailedObject.Thumbnail;
                TitleBlock.Text = _detailedObject.Album;
                SubtitleBlock.Text = _detailedObject.Artist;
                Genre.Text = _detailedObject.Genre;

                if (Convert.ToInt32(_detailedObject.Year) > 0)
                {
                    Year.Text = _detailedObject.Year;
                    Year.Visibility = Visibility.Visible;
                    Dot.Visibility = Visibility.Visible;
                }
                else
                {
                    Year.Text = string.Empty;
                    Year.Visibility = Visibility.Collapsed;
                    Dot.Visibility = Visibility.Collapsed;
                }

                // Retreve all tracks that matches to the current album.
                var tracks = await DataAccessLibrary.DataAccess.GetAlbumTracksAsync(_detailedObject.Album, _detailedObject.AlbumArtist);

                foreach (var track in tracks)
                {
                    Collection.Add(new SongModel()
                    {
                        Id = track.Id,
                        Thumbnail = track.Thumbnail,
                        Title = track.Title,
                        Artist = track.Artist,
                        Album = track.Album,
                        Duration = await ConvertToNaturalDuration(track.Duration)
                    });
                }
            }
        }

        /// <summary>
        /// Returns natural duration of total <paramref name="duration"/>
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        private async System.Threading.Tasks.Task<string> ConvertToNaturalDuration(string duration)
        {
            double min = 0, sec = 0, totalSeconds;

            await System.Threading.Tasks.Task.Run(() =>
            {
                // Converts total seconds to Natural Duration.
                totalSeconds = Convert.ToDouble(duration);
                min = Math.Floor(totalSeconds / 60);
                sec = totalSeconds % 60;
            });

            return ((sec < 10) ? $"{min}:0{sec}" : $"{min}:{sec}");
        }

        #endregion Methods

        #region Events

        private void DetailedAlbumPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Retrieve the ScrollViewer that the ListView is using internally
            var scrollViewer = collectionListView.ChildrenBreadthFirst().OfType<ScrollViewer>().First();
            var scrollBar = collectionListView.ChildrenBreadthFirst().OfType<ScrollBar>().First();
            if (scrollBar != null)
            {
                scrollBar.Margin = new Thickness(0, 32, 0, 90);
            }

            // Update the ZIndex of the header container so that the header is above the items when scrolling
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)collectionListView.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(headerContainer, 1);

            // Get the PropertySet that contains the scroll values from the ScrollViewer
            _scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            _compositor = _scrollerPropertySet.Compositor;

            // Create a PropertySet that has values to be referenced in the ExpressionAnimations below
            _props = _compositor.CreatePropertySet();
            _props.InsertScalar("progress", 0);
            _props.InsertScalar("clampSize", 160);
            _props.InsertScalar("scaleFactor", 0.5f);
            _props.InsertScalar("textScaleFactor", 0.8f);

            // Get references to our property sets for use with ExpressionNodes
            var scrollingProperties = _scrollerPropertySet.GetSpecializedReference<ExpressionBuilder.ManipulationPropertySetReferenceNode>();
            var props = _props.GetReference();
            var progressNode = props.GetScalarProperty("progress");
            var clampSizeNode = props.GetScalarProperty("clampSize");
            var scaleFactorNode = props.GetScalarProperty("scaleFactor");
            var textScaleFactorNode = props.GetScalarProperty("textScaleFactor");

            // Create and start an ExpressionAnimation to track scroll progress over the desired distance
            ExpressionNode progressAnimation = EF.Clamp(-scrollingProperties.Translation.Y / clampSizeNode, 0, 1);
            _props.StartAnimation("progress", progressAnimation);

            // Get the backing visual for the header so that its properties can be animated
            Visual headerVisual = ElementCompositionPreview.GetElementVisual(Header);

            // Create and start an ExpressionAnimation to clamp the header's offset to keep it onscreen
            ExpressionNode headerTranslationAnimation = EF.Conditional(progressNode < 1, 0, -scrollingProperties.Translation.Y - clampSizeNode);
            headerVisual.StartAnimation("Offset.Y", headerTranslationAnimation);

            //Set the header's CenterPoint to ensure the overpan scale looks as desired
            headerVisual.CenterPoint = new Vector3((float)(Header.ActualWidth / 2), (float)Header.ActualHeight, 0);

            // Create and start an ExpressionAnimation to scale the header during overpan
            ExpressionNode headerScaleAnimation = EF.Lerp(1, 1.25f, EF.Clamp(scrollingProperties.Translation.Y / 50, 0, 1));
            headerVisual.StartAnimation("Scale.X", headerScaleAnimation);
            headerVisual.StartAnimation("Scale.Y", headerScaleAnimation);

            // Get the backing visual for the profile picture visual so that its properties can be animated
            Visual profileVisual = ElementCompositionPreview.GetElementVisual(ProfileImage);

            // Create and start an ExpressionAnimation to scale the profile image with scroll position
            ExpressionNode scaleAnimation = EF.Lerp(1, scaleFactorNode, progressNode);
            profileVisual.StartAnimation("Scale.X", scaleAnimation);
            profileVisual.StartAnimation("Scale.Y", scaleAnimation);

            // Get the backing visuals for the text and button containers so that their properites can be animated
            Visual textVisual = ElementCompositionPreview.GetElementVisual(TextContainer);
            Visual buttonVisual = ElementCompositionPreview.GetElementVisual(ButtonPanel);

            // When the header stops scrolling it is 160 pixels offscreen. We want the text header to end up with 20 pixels of its content
            // offscreen which means it needs to go from offset 0 to 140 as we traverse through the scrollable region
            ExpressionNode profileOffsetXAnimation = progressNode * -20;
            ExpressionNode profileOffsetYAnimation = progressNode * 140;
            profileVisual.StartAnimation("Offset.X", profileOffsetXAnimation);
            profileVisual.StartAnimation("Offset.Y", profileOffsetYAnimation);

            ExpressionNode contentOffsetXAnimation = progressNode * -160;
            ExpressionNode contentOffsetYAnimation = progressNode * 140;
            textVisual.StartAnimation("Offset.X", contentOffsetXAnimation);
            textVisual.StartAnimation("Offset.Y", contentOffsetYAnimation);

            ExpressionNode buttonOffsetXAnimation = progressNode * -160;
            ExpressionNode buttonOffsetYAnimation = progressNode * 10;
            buttonVisual.StartAnimation("Offset.X", buttonOffsetXAnimation);
            buttonVisual.StartAnimation("Offset.Y", buttonOffsetYAnimation);

            // Get backing visuals for the text blocks so that their properties can be animated
            Visual titleVisual = ElementCompositionPreview.GetElementVisual(TitleBlock);
            Visual subtitleVisual = ElementCompositionPreview.GetElementVisual(SubtitleBlock);
            Visual moreVisual = ElementCompositionPreview.GetElementVisual(MoreText);

            // Create an ExpressionAnimation that moves between 1 and 0 with scroll progress, to be used for text block opacity
            ExpressionNode textOpacityAnimation = EF.Clamp(1 - (progressNode * 2), 0, 1);

            // Start opacity animation on the more text visual
            moreVisual.StartAnimation("Opacity", textOpacityAnimation);

            // Create and start an ExpressionAnimation to scale the text block visuals with scroll position
            ExpressionNode textScaleAnimation = EF.Lerp(1, textScaleFactorNode, progressNode);
            titleVisual.StartAnimation("Scale.X", textScaleAnimation);
            titleVisual.StartAnimation("Scale.Y", textScaleAnimation);

            subtitleVisual.StartAnimation("Scale.X", textScaleAnimation);
            subtitleVisual.StartAnimation("Scale.Y", textScaleAnimation);
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

        #endregion Events
    }
}
