using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.ViewModels;

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SoftdocMusicPlayer.Views
{
    public sealed partial class MainPage : Page
    {
        private SongModel DetailedObject;
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            InitializeComponent();
            DetailedObject = new SongModel();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Store the item to be used in binding to UI
            DetailedObject = e.Parameter as SongModel;

            if (DetailedObject != null)
            {
                destinationImage.Source = DetailedObject.Thumbnail;
                backgroundHost.Source = DetailedObject.Thumbnail;
                musicTitle.Text = DetailedObject.Title;
                albumArtist.Text = DetailedObject.Artist;
            }

            var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("forwardAnimation");

            if (imageAnimation != null)
            {
                FetchCompositor();
                imageAnimation.Configuration = new DirectConnectedAnimationConfiguration();

                var customXAnimation = _compositor.CreateScalarKeyFrameAnimation();
                customXAnimation.Duration = TimeSpan.FromSeconds(.75);
                customXAnimation.InsertExpressionKeyFrame(0.0f, "StartingValue");
                customXAnimation.InsertExpressionKeyFrame(0.5f, "FinalValue");
                customXAnimation.InsertExpressionKeyFrame(1.0f, "FinalValue");
                imageAnimation.SetAnimationComponent(ConnectedAnimationComponent.OffsetX, customXAnimation);

                var customYAnimation = _compositor.CreateScalarKeyFrameAnimation();
                customYAnimation.Duration = TimeSpan.FromSeconds(.75);
                customYAnimation.InsertExpressionKeyFrame(0.0f, "StartingValue");
                customYAnimation.InsertExpressionKeyFrame(0.5f, "FinalValue");
                customYAnimation.InsertExpressionKeyFrame(1.0f, "FinalValue");
                imageAnimation.SetAnimationComponent(ConnectedAnimationComponent.OffsetY, customYAnimation);

                //imageAnimation.TryStart(destinationImage);

                // Play Coordinated animation
                CreateImplicitAnimations();
                imageAnimation.Completed += ImageAnimation_Completed;
                imageAnimation.TryStart(destinationImage, new UIElement[] { coordinatedPanel });
            }
        }

        // Toggle "more info panel" visibility
        private void ImageAnimation_Completed(ConnectedAnimation sender, object args)
        {
            backgroundHost.Visibility = Visibility.Visible;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (e.NavigationMode == NavigationMode.Back)
            {
                var animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backAnimation", destinationImage);

                if (animation != null)
                {
                    // Use the recommended configuration for back animation.
                    animation.Configuration = new DirectConnectedAnimationConfiguration();
                }
            }
        }

        // Choreographed animations
        private Windows.UI.Composition.Compositor _compositor = null;

        private void FetchCompositor()
        {
            if (_compositor == null)
            {
                _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            }
        }

        private void CreateImplicitAnimations()
        {
            backgroundHost.Visibility = Visibility.Collapsed;
            FetchCompositor();

            // Animate the header background scale when it first shows
            var scaleHeaderAnimation = _compositor.CreateScalarKeyFrameAnimation();
            scaleHeaderAnimation.InsertKeyFrame(0, 0.5f);
            scaleHeaderAnimation.InsertKeyFrame(1, 1f);
            scaleHeaderAnimation.Duration = TimeSpan.FromSeconds(.30);
            scaleHeaderAnimation.Target = "Scale.X";

            ElementCompositionPreview.SetImplicitShowAnimation(headerBackground, scaleHeaderAnimation);

            // Animate the "more info" panel opacity when it first shows
            var fadeMoreInfroPanel = _compositor.CreateScalarKeyFrameAnimation();
            fadeMoreInfroPanel.InsertKeyFrame(0, 0f);
            fadeMoreInfroPanel.InsertKeyFrame(1, 1f);
            fadeMoreInfroPanel.Duration = TimeSpan.FromSeconds(.2);
            fadeMoreInfroPanel.Target = "Opacity";

            ElementCompositionPreview.SetImplicitShowAnimation(backgroundHost, fadeMoreInfroPanel);
        }
    }
}
