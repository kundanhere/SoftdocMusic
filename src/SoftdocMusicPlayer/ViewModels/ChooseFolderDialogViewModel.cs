using DataAccessLibrary;
using SoftdocMusicPlayer.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

namespace SoftdocMusicPlayer.ViewModels
{
    public static class ChooseFolderDialogViewModel
    {
        /// <summary>
        /// Returns storage folder async.
        /// </summary>
        public static async Task<StorageFolder> GetFolderAsync()
        {
            StorageFolder folder = null;
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.ViewMode = PickerViewMode.List;
                folderPicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                folderPicker.FileTypeFilter.Add("*");
                folder = await folderPicker.PickSingleFolderAsync();
            }
            catch { }
            return folder;
        }

        public static async Task SaveFilePropertiesAsync(StorageFolder folder)
        {
            // Application now has read/write access to the picked folder.
            List<string> fileTypeFilter = new List<string>();
            string[] fileTypes = new string[] { ".mp3", ".wma", ".wav", ".ogg", ".flac", ".aiff", ".aac" };

            foreach (var type in fileTypes)
            {
                fileTypeFilter.Add(type);
            }

            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);
            StorageFileQueryResult results = folder.CreateFileQueryWithOptions(queryOptions);
            IReadOnlyList<StorageFile> sortedFiles = await results.GetFilesAsync();

            if (sortedFiles != null)
            {
                foreach (StorageFile file in sortedFiles.OrderBy(a => a.DateCreated))
                {
                    // Get music properties.
                    MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();
                    var name = file.Name;
                    var title = string.IsNullOrWhiteSpace(properties.Title) ? file.Name : properties.Title;
                    var album = string.IsNullOrWhiteSpace(properties.Album) ? "Unknown Album" : properties.Album;
                    var albumArtist = string.IsNullOrWhiteSpace(properties.AlbumArtist) ? "Unknown Artist" : properties.AlbumArtist;
                    var artist = string.IsNullOrWhiteSpace(properties.Artist) ? "Unknown Artist" : properties.Artist;
                    var duration = Math.Floor(properties.Duration.TotalSeconds).ToString();
                    var genres = properties.Genre;
                    var list = new List<string>();
                    foreach (var i in genres)
                    {
                        list.Add(i);
                    }
                    var genre = string.Join(',', list);
                    var year = (properties.Year).ToString();
                    var byteArray = await ImageHelper.GetBytesForImageAsync(file);

                    // Save properties into database.
                    DataAccess.InsertFileProperties(title, album, albumArtist, artist, duration, genre, year, name, folder.Path, byteArray);
                }
            }
        }
    }
}
