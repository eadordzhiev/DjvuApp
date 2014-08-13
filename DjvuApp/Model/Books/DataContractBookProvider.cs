using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using DjvuApp.Annotations;
using DjvuLibRT;

namespace DjvuApp.Model.Books
{
    public sealed class DataContractBookProvider : IBookProvider
    {
        [DataContract]
        private sealed class DataContractBook : IBook
        {
            private string _title;

            [DataMember]
            public Guid Guid { get; set; }

            [DataMember]
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

            [DataMember]
            public DateTime LastOpeningTime { get; set; }

            [DataMember]
            public DateTime CreationTime { get; set; }

            [DataMember]
            public uint PageCount { get; set; }

            [DataMember]
            public uint Size { get; set; }

            [DataMember]
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

        public async Task<IList<IBook>> GetBooksAsync()
        {
            var serializer = new DataContractJsonSerializer(typeof(DataContractBook));
            
            var folder = await GetBooksFolderAsync();
            var books = new List<IBook>();

            foreach (var file in await folder.GetFilesAsync())
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    var book = (DataContractBook) serializer.ReadObject(stream);
                    books.Add(book);
                }
            }

            return books;
        }

        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

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

            var book = new DataContractBook
            {
                Guid = guid,
                PageCount = document.PageCount,
                CreationTime = DateTime.Now,
                Size = (uint) props.Size,
                Title = title,
                LastOpeningTime = DateTime.Now,
                Path = djvuFile.Path
            };
            
            await SaveBookDescriptionAsync(book);

            return book;
        }

        public async Task RemoveBookAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var file = await GetBookFileAsync(book as DataContractBook);
            await file.DeleteAsync();
        }
        
        public async Task ChangeTitleAsync(IBook book, [NotNull] string title)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("title can't be empty", "title");

            book.Title = title;
            await SaveBookDescriptionAsync(book);
        }
        
        private async Task SaveBookDescriptionAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");
            if (!(book is DataContractBook))
                throw new ArgumentException("book");

            var file = await GetBookFileAsync(book as DataContractBook);
            
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                stream.SetLength(0);
                var serializer = new DataContractJsonSerializer(typeof(DataContractBook));
                serializer.WriteObject(stream, book);
            }
        }

        private async Task<IStorageFile> GetBookFileAsync(IBook book)
        {
            if (book == null)
                throw new ArgumentNullException("book");

            var filename = book.Guid.ToString();

            var dir = await GetBooksFolderAsync();
            return await dir.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
        }

        private async Task<IStorageFolder> GetBooksFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
        }
    }
}