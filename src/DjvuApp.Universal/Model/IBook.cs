using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DjvuApp.Model
{
    public interface IBook : ISaveChanges, INotifyPropertyChanged
    {
        [NotNull]
        string Title { get; set; }

        DateTime LastOpeningTime { get; set; }

        uint? LastOpenedPage { get; set; }

        uint PageCount { get; }
        
        [NotNull]
        string BookPath { get; }
        
        string ThumbnailPath { get; }

        ReadOnlyObservableCollection<IBookmark> Bookmarks { get; }

        Task AddBookmarkAsync(string title, uint pageNumber);
            
        Task UpdateThumbnailAsync();

        Task RemoveAsync();

        // This is needed to workaround a XAML bug
        // http://blog.pieeatingninjas.be/2015/11/15/xbind-and-inheritance/
        new event PropertyChangedEventHandler PropertyChanged;
    }
}