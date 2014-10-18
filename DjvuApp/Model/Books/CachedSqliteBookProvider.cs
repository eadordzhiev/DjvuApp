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
        private readonly SqliteBookProvider _provider;
        private List<IBook> _books; 

        private CachedSqliteBookProvider()
        {
            _provider = new SqliteBookProvider();
        }
        
        public static async Task<CachedSqliteBookProvider> CreateNewAsync()
        {
            var provider = new CachedSqliteBookProvider();

            var books = await provider._provider.GetBooksAsync();
            provider._books = books.ToList();

            return provider;
        }

        public async Task<IEnumerable<IBook>> GetBooksAsync()
        {
            return _books;
        }

        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            var book = await _provider.AddBookAsync(file);
            _books.Add(book);
            return book;
        }

        public async Task RemoveBookAsync(IBook book)
        {
            await _provider.RemoveBookAsync(book);
            _books.Remove(book);
        }

        public async Task ChangeTitleAsync(IBook book, string title)
        {
            await _provider.ChangeTitleAsync(book, title);
        }

        public async Task<IEnumerable<IBookmark>> GetBookmarksAsync(IBook book)
        {
            return await _provider.GetBookmarksAsync(book);
        }

        public async Task<IBookmark> CreateBookmarkAsync(IBook book, string title, uint pageNumber)
        {
            return await _provider.CreateBookmarkAsync(book, title, pageNumber);
        }

        public async Task RemoveBookmarkAsync(IBookmark bookmark)
        {
            await _provider.RemoveBookmarkAsync(bookmark);
        }

        public async Task UpdateLastOpeningTimeAsync(IBook book)
        {
            await _provider.UpdateLastOpeningTimeAsync(book);
        }

        public async Task UpdateLastOpenedPageAsync(IBook book, uint pageNumber)
        {
            await _provider.UpdateLastOpenedPageAsync(book, pageNumber);
        }
    }
}
