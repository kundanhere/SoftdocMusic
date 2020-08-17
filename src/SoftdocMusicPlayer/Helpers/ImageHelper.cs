using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftdocMusicPlayer.Helpers
{
    public static class ImageHelper
    {
        public static async Task<BitmapImage> GetImageFromStringAsync(string data)
        {
            var byteArray = Convert.FromBase64String(data);
            var image = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(byteArray.AsBuffer());
                stream.Seek(0);
                await image.SetSourceAsync(stream);
            }

            return image;
        }

        public static BitmapImage GetImageFromAssetsFile(string fileName)
        {
            var image = new BitmapImage(new Uri($"ms-appx:///Assets/{fileName}"));
            return image;
        }

        public static async Task<BitmapImage> GetImageFromBytesAsync(byte[] array)
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

        public static async Task<byte[]> GetBytesFromFileAsync(StorageFile mediafile)
        {
            using (var inputStream = await mediafile.OpenSequentialReadAsync())
            {
                var readStream = inputStream.AsStreamForRead();
                byte[] buffer = new byte[readStream.Length];
                await readStream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public static async Task<byte[]> GetBytesForImageAsync(StorageFile mediafile)
        {
            byte[] bts;

            using (var thumbnail = await mediafile.GetThumbnailAsync(ThumbnailMode.MusicView, 300))
            {
                if (!(thumbnail is null))
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        await thumbnail.AsStream().CopyToAsync(stream);
                        bts = stream.ToArray();
                        return bts;
                    }
                }
                else
                {
                    return new byte[] { };
                }
            }
        }

        public static async Task<BitmapImage> GetThumbnailAsync(StorageFile file, bool defaultImg = true)
        {
            BitmapImage image = null;
            using (var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.MusicView, 300))
            {
                if (!(thumbnail is null))
                {
                    image = new BitmapImage();
                    image.SetSource(thumbnail);
                }
                else if (defaultImg)
                {
                    image = GetImageFromAssetsFile("default_album_300.png");
                }
            }
            return image;
        }
    }
}
