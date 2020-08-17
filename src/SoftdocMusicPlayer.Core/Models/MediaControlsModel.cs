using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SoftdocMusicPlayer.Core.Models
{
    public static class MediaControlsModel
    {
        public static MediaElement MP3Player { get; set; }
        public static Button PlayOrPauseBtn { get; set; }
        public static SymbolIcon PlayOrPauseBtnIcon { get; set; }
        public static SymbolIcon NextBtnIcon { get; set; }
        public static SymbolIcon PreviousBtnIcon { get; set; }
        public static SymbolIcon ShuffleBtnIcon { get; set; }
        public static SymbolIcon RepeatBtnIcon { get; set; }
        public static ImageBrush Thumbnail { get; set; }
        public static Grid MusicMetadata { get; set; }
        public static TextBlock Title { get; set; }
        public static TextBlock Artist { get; set; }
        public static Canvas GlassHost { get; set; }
        public static Canvas Overlay { get; set; }
    }
}