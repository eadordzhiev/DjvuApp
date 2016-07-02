using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using DjvuApp.Djvu;
using Microsoft.EntityFrameworkCore;

namespace DjvuApp.Model
{
    public class EfBookProvider : IBookProvider
    {
        public ReadOnlyObservableCollection<IBook> Books { get; private set; }

        private readonly EfBooksContext _context = new EfBooksContext();
        private readonly ObservableCollection<IBook> _books = new ObservableCollection<IBook>();

        private EfBookProvider()
        {
            _context.Database.Migrate();
        }

        public static async Task<IBookProvider> CreateAsync()
        {
            var provider = new EfBookProvider();
            await provider.InitializeAsync();
            return provider;
        }

        private async Task InitializeAsync()
        {
            var bookDtos = await _context.Books
                .Include(bookDto => bookDto.Bookmarks)
                .ToArrayAsync();
            foreach (var bookDto in bookDtos)
            {
                _books.Add(new EfBook(bookDto, _context, _books));
            }
            Books = new ReadOnlyObservableCollection<IBook>(_books);
        }
        
        public async Task<IBook> AddBookAsync(IStorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var document = await DjvuDocument.LoadAsync(file);            
            var bookDto = new EfBookDto
            {
                PageCount = document.PageCount,
                Title = Path.GetFileNameWithoutExtension(file.Name),
                LastOpeningTime = DateTime.Now
            };
            _context.Books.Add(bookDto);
            await _context.SaveChangesAsync();

            var booksFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
            var bookFile = await file.CopyAsync(booksFolder, $"{bookDto.Id}.djvu", NameCollisionOption.ReplaceExisting);

            bookDto.BookPath = bookFile.Path;
            await _context.SaveChangesAsync();

            var book = new EfBook(bookDto, _context, _books);
            await book.UpdateThumbnailAsync();
            _books.Add(book);
            return book;
        }
    }
}