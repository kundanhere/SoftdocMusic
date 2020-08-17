using Microsoft.Data.Sqlite;
using SoftdocMusicPlayer.Core.Models;
using SoftdocMusicPlayer.Views;

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftdocMusicPlayer.ViewModels
{
    public static class AlbumsViewModel
    {
        private static readonly string _dbFolderName = "Database";
        private static readonly string _dbFileName = "AppData.db";
        private static string _dbPath;

        public static async void init()
        {
            // Get the app's local folder.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Get the subfolder of the app's local folder.
            StorageFolder subFolder = await localFolder.GetFolderAsync(_dbFolderName);

            _dbPath = Path.Combine(subFolder.Path, _dbFileName);
        }

        public static async Task<ObservableCollection<SongModel>> GetAlbumsAsync()
        {
            ObservableCollection<SongModel> albums = new ObservableCollection<SongModel>();
            using (SqliteConnection db =
                new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = "SELECT id, title, albumArtist, thumbnail FROM MusicTable";
                    SqliteDataReader reader = selectCommand.ExecuteReader();
                    var count = 0;
                    while (reader.Read())
                    {
                        byte[] buffer = GetBytes(reader);
                        var bitmap = await GetImageFromBytesAsync(buffer);
                        albums.Add(new SongModel() { Id = reader.GetInt32(0), Title = reader.GetString(1), Artist = reader.GetString(2), Thumbnail = bitmap });

                        if (count > 50)
                        {
                            UpdateView(albums);
                            count = 0;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    reader.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return albums;
        }

        private static void UpdateView(ObservableCollection<SongModel> albums)
        {
            var page = new AlbumsPage();

            page.AlbumsItem = albums;
        }

        private static byte[] GetBytes(SqliteDataReader reader)
        {
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(3, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

        private static async Task<BitmapImage> GetImageFromBytesAsync(byte[] array)
        {
            //This task will return a image from provided the byte[].
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(array);
                    await writer.StoreAsync();
                }
                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(stream);
                return image;
            }
        }
    }
}