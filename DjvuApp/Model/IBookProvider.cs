using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using DjvuApp.Annotations;
using DjvuLibRT;

namespace DjvuApp
{
    public interface IBookProvider
    {
        Task<IList<Book>> GetBooksAsync();
        Task<Book> AddBookAsync(IStorageFile file);
        Task RemoveBookAsync(Book book);
        Task ChangeTitleAsync(Book book, string title);
        Task UpdateBookPositionAsync(Book book, uint pageNumber);
    }

    public class DocumentTypeNotSupportedException : Exception
    {
        public DocumentTypeNotSupportedException()
        {
        }

        public DocumentTypeNotSupportedException(string message) : base(message)
        {
        }

        public DocumentTypeNotSupportedException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class DjvuDocumentException : Exception
    {
        public DjvuDocumentException()
        {
        }

        public DjvuDocumentException(string message) : base(message)
        {
        }

        public DjvuDocumentException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class BookProvider : IBookProvider
    {
        public async Task<IList<Book>> GetBooksAsync()
        {
            var serializer = new DataContractJsonSerializer(typeof(Book));

            var folder = await GetBooksFolderAsync();
            var books = new List<Book>();

            foreach (var file in await folder.GetFilesAsync())
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var book = (Book) serializer.ReadObject(stream);
                    books.Add(book);
                }
            }

            return books;
        }

        public async Task<Book> AddBookAsync(IStorageFile file)
        {
            DjvuDocument document;

            try
            {
                document = new DjvuDocument(file.Path);
            }
            catch (COMException exception)
            {
                throw new DjvuDocumentException("Cannot open document.", exception);
            }
            
            if (document.Type == DocumentType.Indirect || document.Type == DocumentType.OldIndexed)
            {
                throw new DocumentTypeNotSupportedException("Indirect and old indexed documents are not supported.");
            }

            var guid = Guid.NewGuid();
            var props = await file.GetBasicPropertiesAsync();
            var title = Path.GetFileNameWithoutExtension(file.Name);

            var booksFolder = await GetBooksFolderAsync();
            var djvuFolder = await booksFolder.CreateFolderAsync("Djvu", CreationCollisionOption.OpenIfExists);
            var djvuFile = await file.CopyAsync(djvuFolder, string.Format("{0}.djvu", guid));

            var book = new Book
            {
                Guid = guid,
                PageCount = document.PageCount,
                CreationTime = DateTime.Now,
                Size = props.Size,
                Title = title,
                LastOpeningTime = DateTime.Now,
                LastOpeningPageNumber = 1,
                Path = djvuFile.Path
            };
            
            await SaveBookDescriptionAsync(book);

            return book;
        }

        public async Task RemoveBookAsync(Book book)
        {
            var file = await GetBookFileAsync(book);
            await file.DeleteAsync();
        }
        
        public async Task ChangeTitleAsync(Book book, [NotNull] string title)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("title can't be empty", "title");

            book.Title = title;
            await SaveBookDescriptionAsync(book);
        }

        public async Task UpdateBookPositionAsync(Book book, uint pageNumber)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (pageNumber < 1 || pageNumber > book.PageCount)
                throw new ArgumentOutOfRangeException("pageNumber", string.Format("pageNumber should be in rage of 1 to {0}", book.PageCount));

            book.LastOpeningPageNumber = pageNumber;
            book.LastOpeningTime = DateTime.Now;
            await SaveBookDescriptionAsync(book);
        }

        private async Task SaveBookDescriptionAsync(Book book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var file = await GetBookFileAsync(book);
            
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.SetLength(0);
                var serializer = new DataContractJsonSerializer(typeof(Book));
                serializer.WriteObject(stream, book);
            }
        }

        private async Task<IStorageFolder> GetBooksFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
        }

        private async Task<IStorageFile> GetBookFileAsync(Book book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var filename = book.Guid.ToString();

            var dir = await GetBooksFolderAsync();
            return await dir.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }
    }
}
