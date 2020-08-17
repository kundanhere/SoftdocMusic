using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftdocMusicPlayer.Core.Models
{
    public class SongModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Duration { get; set; }
        public string Genre { get; set; }
        public string Year { get; set; }
        public string Source { get; set; }
        public Symbol State { get; set; }
        public BitmapImage Thumbnail { get; set; }
        public string Type { get; set; }
        public bool IsFavorite { get; set; }
    }
}