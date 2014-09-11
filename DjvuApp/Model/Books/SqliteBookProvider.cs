using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using DjvuApp.Djvu;
using JetBrains.Annotations;
using SQLite;

namespace DjvuApp.Model.Books
{
    public class SqliteBookProvider : IBookProvider
    {
        private sealed class SqliteBook : IBook
        {
            private string _title;

            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public Guid Guid { get; set; }

            [MaxLength(255)]
            public string Title
            {
                get { return _title; }
                set
                {
                    if (value == _title) return;
                    _title = value;
                    OnPropertyChanged();
                }
            }

            public DateTime LastOpeningTime { get; set; }

            public DateTime CreationTime { get; set; }

            public uint PageCount { get; set; }

            public uint Size { get; set; }

            [MaxLength(255)]
            public string Path { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }

            public bool Equals(IBook other)
            {
                return other != null && Guid == other.Guid;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as IBook);
            }

            public override int GetHashCode()
            {
                return Guid.GetHashCode();
            }
        }

        private sealed class SqliteBookmark : IBookmark
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int BookId { get; set; }

            [MaxLength(255)]
            public string Title { get; set; }

            public uint PageNumber { get; set; }
        }

        private SQLiteAsyncConnection _connection;

        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "books.sqlite");
                _connection = new SQLiteAsyncConnection(path, true);

                await _connection.CreateTableAsync<SqliteBook>();
                await _connection.CreateTableAsync<SqliteBookmark>();
            }

            return _connection;
        }

        public async Task<IEnumerable<IBook>> GetBooksAsync()
        {
            var connection = await GetConnectionAsync();
            var items = await connection.Table<SqliteBook>().ToListAsync();
            return items;
        }

        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            var document = await DjvuAsyncDocument.LoadFileAsync(file.Path);

            var guid = Guid.NewGuid();
            var properties = await file.GetBasicPropertiesAsync();
            var title = Path.GetFileNameWithoutExtension(file.Name);

            var booksFolder = await GetBooksFolderAsync();
            var djvuFolder = await booksFolder.CreateFolderAsync("Djvu", CreationCollisionOption.OpenIfExists);
            var djvuFile = await file.CopyAsync(djvuFolder, string.Format("{0}.djvu", guid));

            var book = new SqliteBook
            {
                Guid = guid,
                PageCount = document.PageCount,
                CreationTime = DateTime.Now,
                Size = (uint) properties.Size,
                Title = title,
                LastOpeningTime = DateTime.Now,
                Path = djvuFile.Path
            };

            var connection = await GetConnectionAsync();
            await connection.InsertAsync(book);

            return book;
        }

        public async Task RemoveBookAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var connection = await GetConnectionAsync();
            await connection.DeleteAsync(book);

            var sqliteBook = (SqliteBook)book;
            var bookmarksToRemove = await connection.Table<SqliteBookmark>().Where(bookmark => bookmark.BookId == sqliteBook.Id).ToListAsync();
            foreach (var bookmark in bookmarksToRemove)
            {
                await connection.DeleteAsync(bookmark);
            }
        }

        public async Task ChangeTitleAsync(IBook book, string title)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("title can't be empty", "title");

            book.Title = title;

            var connection = await GetConnectionAsync();
            await connection.UpdateAsync(book);
        }

        public async Task<IEnumerable<IBookmark>> GetBookmarksAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var sqliteBook = (SqliteBook)book;

            var connection = await GetConnectionAsync();
            var bookmarks = connection.Table<SqliteBookmark>().Where(bookmark => bookmark.BookId == sqliteBook.Id);
            return await bookmarks.ToListAsync();
        }

        public async Task<IBookmark> CreateBookmarkAsync(IBook book, string title, uint pageNumber)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title is empty", "title");
            if (pageNumber < 1 || pageNumber > book.PageCount)
                throw new ArgumentOutOfRangeException("pageNumber");

            var sqliteBook = (SqliteBook) book;
            var bookmark = new SqliteBookmark { BookId = sqliteBook.Id, Title = title, PageNumber = pageNumber };

            var connection = await GetConnectionAsync();
            await connection.InsertAsync(bookmark);

            return bookmark;
        }

        public async Task RemoveBookmarkAsync(IBookmark bookmark)
        {
            if (bookmark == null)
                throw new ArgumentNullException("bookmark");

            var connection = await GetConnectionAsync();
            await connection.DeleteAsync(bookmark);
        }

        public async Task UpdateLastOpeningTimeAsync(IBook book)
        {
            var sqliteBook = (SqliteBook)book;
            sqliteBook.LastOpeningTime = DateTime.Now;

            var connection = await GetConnectionAsync();
            await connection.UpdateAsync(sqliteBook);
        }

        private static async Task<IStorageFolder> GetBooksFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
        }
    }
}