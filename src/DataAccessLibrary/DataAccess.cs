using Microsoft.Data.Sqlite;
using SoftdocMusicPlayer.Core.Models;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace DataAccessLibrary
{
    public static class DataAccess
    {
        #region Private members

        private static readonly string _dbFolderName = "Database";
        private static readonly string _dbFileName = "AppData.db";
        private static string _dbPath;

        #endregion Private members

        #region Initialize database

        /// <summary>
        /// InitializeDatabase()
        /// </summary>
        public async static void InitializeDatabaseAsync()
        {
            // Get the app's local folder.
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            // Create a new subfolder in the current folder.
            // Return if already exists.
            StorageFolder newFolder = await localFolder.CreateFolderAsync(_dbFolderName, CreationCollisionOption.OpenIfExists);

            // Create database file if not exsist.
            await newFolder.CreateFileAsync(_dbFileName, CreationCollisionOption.OpenIfExists);

            // Get the subfolder of the app's local folder.
            // Create table in database if not exists.
            StorageFolder subFolder = await localFolder.GetFolderAsync(_dbFolderName);

            _dbPath = Path.Combine(subFolder.Path, _dbFileName);
            using (SqliteConnection db =
               new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    string tableCommand1 = "CREATE TABLE IF NOT EXISTS " +
                        "MusicTable (id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE," +
                        "title NVARCHAR(2048) NOT NULL," +
                        "album NVARCHAR(2048) NOT NULL," +
                        "albumArtist NVARCHAR(2048) NOT NULL," +
                        "artist NVARCHAR(2048) NOT NULL," +
                        "duration NVARCHAR(2048) NOT NULL," +
                        "genre NVARCHAR(2048) NOT NULL," +
                        "year NVARCHAR(2048) NOT NULL," +
                        "fileName NVARCHAR(2048) NOT NULL," +
                        "dirPath NVARCHAR(2048) NOT NULL," +
                        "thumbnail BLOB)";

                    string tableCommand2 = "CREATE TABLE IF NOT EXISTS " +
                        "DirectoryTable (id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE," +
                        "dirName NVARCHAR(2048) NOT NULL," +
                        "path NVARCHAR(2048) NOT NULL)";

                    SqliteCommand createTable1 = new SqliteCommand(tableCommand1, db);
                    SqliteCommand createTable2 = new SqliteCommand(tableCommand2, db);

                    createTable1.ExecuteReader();
                    createTable2.ExecuteReader();
                }
                catch (Exception) { }
            }
        }

        #endregion Initialize database

        /// <summary>
        /// Inserts the specified <paramref name="name"/> and <paramref name="path"/> in database.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public static void InsertFolderProperties(string name, string path)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand insertCommand = new SqliteCommand();
                    insertCommand.Connection = db;

                    // Use parameterized query to prevent SQL injection attacks
                    insertCommand.CommandText = "INSERT INTO DirectoryTable VALUES (NULL, @Name, @Path);";
                    insertCommand.Parameters.AddWithValue("@Name", name);
                    insertCommand.Parameters.AddWithValue("@Path", path);

                    insertCommand.ExecuteNonQuery();

                    db.Close();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Determines whether the specified <paramref name="name"/> and <paramref name="path"/> are available in database.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public static int CountFolder(string name, string path)
        {
            var entries = 0;
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = $"SELECT dirName, path FROM DirectoryTable WHERE dirName='{name}' and path='{path}'";
                    SqliteDataReader query = selectCommand.ExecuteReader();
                    while (query.Read())
                    {
                        entries++;
                    }
                    query.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return entries;
        }

        /// <summary>
        /// Returns the folder properties from database.
        /// </summary>
        public static ObservableCollection<FolderItem> GetFolderProperties()
        {
            ObservableCollection<FolderItem> entries = new ObservableCollection<FolderItem>();
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = "SELECT dirName, path FROM DirectoryTable";
                    SqliteDataReader query = selectCommand.ExecuteReader();
                    while (query.Read())
                    {
                        entries.Add(new FolderItem() { DirectoryName = query.GetString(0), DirectoryPath = query.GetString(1) });
                    }
                    query.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return entries;
        }

        /// <summary>
        /// Removes the data from database where the specified <paramref name="name"/> and <paramref name="path"/> are same.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public static void RemoveFolder(string name, string path)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand deleteCommand = new SqliteCommand();
                    deleteCommand.Connection = db;
                    deleteCommand.CommandText = $"DELETE FROM DirectoryTable WHERE dirName='{name}' and path='{path}'";
                    deleteCommand.ExecuteNonQuery();

                    db.Close();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Inserts the specified file properties into database.
        /// </summary>
        public static void InsertFileProperties(
            string title = "Untitled",
            string album = "Unknown Album",
            string albumArtist = "Unknown Artist",
            string artist = "Unknown Artist",
            string duration = null,
            string genre = null,
            string year = null,
            string fileName = null,
            string dirPath = null,
            byte[] thumbnail = null)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand insertCommand = new SqliteCommand();
                    insertCommand.Connection = db;

                    // Use parameterized query to prevent SQL injection attacks
                    insertCommand.CommandText = "INSERT INTO MusicTable VALUES (NULL, @Title, @Album, @AlbumArtist, @Artist, @Duration, @Genre, @Year, @fileName, @DirPath, @Thumbnail);";
                    insertCommand.Parameters.AddWithValue("@Title", title);
                    insertCommand.Parameters.AddWithValue("@Album", album);
                    insertCommand.Parameters.AddWithValue("@AlbumArtist", albumArtist);
                    insertCommand.Parameters.AddWithValue("@Artist", artist);
                    insertCommand.Parameters.AddWithValue("@Duration", duration);
                    insertCommand.Parameters.AddWithValue("@Genre", genre);
                    insertCommand.Parameters.AddWithValue("@Year", year);
                    insertCommand.Parameters.AddWithValue("@fileName", fileName);
                    insertCommand.Parameters.AddWithValue("@DirPath", dirPath);
                    insertCommand.Parameters.AddWithValue("@Thumbnail", thumbnail);

                    insertCommand.ExecuteNonQuery();

                    db.Close();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Removes the specified <paramref name="path"/> file propeties from database.
        /// </summary>
        /// <paramref name="path"/>
        public static void RemoveFileProperties(string path)
        {
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand deleteCommand = new SqliteCommand();
                    deleteCommand.Connection = db;
                    deleteCommand.CommandText = $"DELETE FROM MusicTable WHERE dirPath='{path}'";
                    deleteCommand.ExecuteNonQuery();

                    db.Close();
                }
                catch (Exception) { }
            }
        }

        /// <summary>
        /// Returns the Media properties as SongModel object
        /// </summary>
        public async static Task<ObservableCollection<SongModel>> GetMediaAsync()
        {
            ObservableCollection<SongModel> media = new ObservableCollection<SongModel>();
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = "SELECT id, title, artist, album, albumArtist, duration, genre, year, thumbnail FROM MusicTable";
                    SqliteDataReader reader = selectCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        byte[] buffer = GetBytes(reader);
                        var bitmap = await GetImageFromBytesAsync(buffer);
                        media.Add(item: new SongModel()
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Artist = reader.GetString(2),
                            Album = reader.GetString(3),
                            AlbumArtist = reader.GetString(4),
                            Duration = reader.GetString(5),
                            Genre = reader.GetString(6),
                            Year = reader.GetString(7),
                            Thumbnail = bitmap
                        });
                    }
                    reader.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return media;
        }

        private static byte[] GetBytes(SqliteDataReader reader)
        {
            // Converts bitmap image to byte array
            const int CHUNK_SIZE = 2 * 1024;
            byte[] buffer = new byte[CHUNK_SIZE];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(8, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

        private async static Task<BitmapImage> GetImageFromBytesAsync(byte[] array)
        {
            // This task will return a image from provided byte[].
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

        /// <summary>
        /// Resturn folder path and file name to of <paramref name="id"/>
        /// <param name="id">
        /// </summary>
        public async static Task<List<string>> GetMediaPath(double id)
        {
            List<string> collection = new List<string>();
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = $"SELECT dirPath, fileName FROM MusicTable WHERE id = {id}";
                    SqliteDataReader reader = selectCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        collection.Add(reader.GetString(0));
                        collection.Add(reader.GetString(1));
                    }
                    reader.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return collection;
        }

        /// <summary>
        /// Returns the Album tracks as SongModel object
        /// </summary>
        public async static Task<ObservableCollection<SongModel>> GetAlbumTracksAsync(string album, string artist)
        {
            ObservableCollection<SongModel> media = new ObservableCollection<SongModel>();
            using (SqliteConnection db = new SqliteConnection($"Filename={_dbPath}"))
            {
                try
                {
                    db.Open();

                    SqliteCommand selectCommand = new SqliteCommand();
                    selectCommand.Connection = db;
                    selectCommand.CommandText = $"SELECT id, title, artist, album, albumArtist, duration, genre, year, thumbnail FROM MusicTable WHERE album='{album}' and albumArtist='{artist}'";
                    SqliteDataReader reader = selectCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        byte[] buffer = GetBytes(reader);
                        var bitmap = await GetImageFromBytesAsync(buffer);
                        media.Add(item: new SongModel()
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Artist = reader.GetString(2),
                            Album = reader.GetString(3),
                            AlbumArtist = reader.GetString(4),
                            Duration = reader.GetString(5),
                            Genre = reader.GetString(6),
                            Year = reader.GetString(7),
                            Thumbnail = bitmap
                        });
                    }
                    reader.Close();

                    db.Close();
                }
                catch (Exception) { }
            }
            return media;
        }
    }
}