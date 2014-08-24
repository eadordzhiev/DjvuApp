using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace DjvuApp.Model.Books
{
    public interface IBookProvider
    {
        Task<IList<IBook>> GetBooksAsync();
        Task<IBook> AddBookAsync(IStorageFile file);
        Task RemoveBookAsync(IBook book);
        Task ChangeTitleAsync(IBook book, string title);
        Task<IList<IBookmark>> GetBookmarksAsync(IBook book);
        Task<IBookmark> CreateBookmarkAsync(IBook book, string title, uint pageNumber);
        Task RemoveBookmarkAsync(IBookmark bookmark);
    }
}
