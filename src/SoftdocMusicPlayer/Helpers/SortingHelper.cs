using SoftdocMusicPlayer.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SoftdocMusicPlayer.Helpers
{
    public static class SortingHelper
    {
        //public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        //{
        //    var sortedSource = source.OrderBy(keySelector).ToList();

        //    for (var i = 0; i < sortedSource.Count; i++)
        //    {
        //        var itemToSort = sortedSource[i];

        //        // If the item is already at the right position, leave it and continue.
        //        if (source.IndexOf(itemToSort) == i)
        //        {
        //            continue;
        //        }

        //        source.Remove(itemToSort);
        //        source.Insert(i, itemToSort);
        //    }
        //}

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            var sortableList = new List<T>(collection);
            sortableList.Sort(comparison);

            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }

        public static int SortByName(SongModel x, SongModel y)
        {
            return x.Title.CompareTo(y.Title);
        }

        public static int SortByAlbum(SongModel x, SongModel y)
        {
            return x.Album.CompareTo(y.Album);
        }

        public static int SortByArtist(SongModel x, SongModel y)
        {
            return x.Artist.CompareTo(y.Artist);
        }

        public static int SortByAlbumArtist(SongModel x, SongModel y)
        {
            return x.AlbumArtist.CompareTo(y.AlbumArtist);
        }

        public static int SortByYear(SongModel x, SongModel y)
        {
            return x.Year.CompareTo(y.Year);
        }

        public static int DescendingSortByYear(SongModel x, SongModel y)
        {
            return y.Year.CompareTo(x.Year);
        }
    }
}
