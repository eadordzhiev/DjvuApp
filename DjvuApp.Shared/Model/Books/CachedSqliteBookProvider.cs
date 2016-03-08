using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DjvuApp.Model.Books
{
    public sealed class CachedSqliteBookProvider : IBookProvider
    {
        public SqliteBookProvider Provider { get; }

        private List<IBook> _books; 

        private CachedSqliteBookProvider()
        {
            Provider = new SqliteBookProvider();
        }
        
        public static async Task<CachedSqliteBookProvider> CreateNewAsync()
        {
            var provider = new CachedSqliteBookProvider();
            await provider.RefreshCacheAsync();

            return provider;
        }

        public async Task RefreshCacheAsync()
        {
            var books = await Provider.GetBooksAsync();
            _books = books.ToList();
        }

        public Task<IEnumerable<IBook>> GetBooksAsync()
        {
            return Task.FromResult(_books.AsEnumerable());
        }

        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            var book = await Provider.AddBookAsync(file);
            _books.Add(book);
            return book;
        }

        public async Task RemoveBookAsync(IBook book)
        {
            await Provider.RemoveBookAsync(book);
            _books.Remove(book);
        }

        public async Task ChangeTitleAsync(IBook book, string title)
        {
            await Provider.ChangeTitleAsync(book, title);
        }

        public async Task<IEnumerable<IBookmark>> GetBookmarksAsync(IBook book)
        {
            return await Provider.GetBookmarksAsync(book);
        }

        public async Task<IBookmark> CreateBookmarkAsync(IBook book, string title, uint pageNumber)
        {
            return await Provider.CreateBookmarkAsync(book, title, pageNumber);
        }

        public async Task RemoveBookmarkAsync(IBookmark bookmark)
        {
            await Provider.RemoveBookmarkAsync(bookmark);
        }

        public async Task UpdateLastOpeningTimeAsync(IBook book)
        {
            await Provider.UpdateLastOpeningTimeAsync(book);
        }

        public async Task UpdateLastOpenedPageAsync(IBook book, uint pageNumber)
        {
            await Provider.UpdateLastOpenedPageAsync(book, pageNumber);
        }
    }
}
