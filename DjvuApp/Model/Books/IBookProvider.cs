using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using JetBrains.Annotations;

namespace DjvuApp.Model.Books
{
    public interface IBookProvider
    {
        Task<IEnumerable<IBook>> GetBooksAsync();

        Task<IBook> AddBookAsync([NotNull] IStorageFile file);

        Task RemoveBookAsync([NotNull] IBook book);

        Task ChangeTitleAsync(
            [NotNull] IBook book, 
            [NotNull] string title);

        Task<IEnumerable<IBookmark>> GetBookmarksAsync([NotNull] IBook book);

        Task<IBookmark> CreateBookmarkAsync(
            [NotNull] IBook book, 
            [NotNull] string title, 
            uint pageNumber);

        Task RemoveBookmarkAsync([NotNull] IBookmark bookmark);
    }
}
