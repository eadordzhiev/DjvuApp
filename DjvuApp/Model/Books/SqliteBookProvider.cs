using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using DjvuApp.Annotations;
using DjvuLibRT;
using SQLite;

namespace DjvuApp.Model.Books
{
    public sealed class SqliteBookProvider : IBookProvider
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
                return other != null && this.Guid == other.Guid;
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

        private SQLiteAsyncConnection _connection;
        
        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection == null)
            {
                var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "books.sqlite");
                _connection = new SQLiteAsyncConnection(path, true);

                await _connection.CreateTableAsync<SqliteBook>();
            }

            return _connection;
        }

        public async Task<IList<IBook>> GetBooksAsync()
        {
            var connection = await GetConnectionAsync();
            var items = await connection.Table<SqliteBook>().ToListAsync();
            return new List<IBook>(items);
        }

        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            DjvuDocument document;

            try
            {
                document = await DjvuDocument.LoadAsync(file.Path);
            }
            catch (Exception ex)
            {
                throw new DjvuDocumentException("Cannot open document.", ex);
            }

            if (document.Type == DocumentType.Indirect || document.Type == DocumentType.OldIndexed)
            {
                throw new DocumentTypeNotSupportedException("Indirect and old indexed documents are not supported.");
            }

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
            
            //_connection.Close();

            return book;
        }

        public async Task RemoveBookAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var connection = await GetConnectionAsync();
            await connection.DeleteAsync(book);
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

        private static async Task<IStorageFolder> GetBooksFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
        }
    }
}