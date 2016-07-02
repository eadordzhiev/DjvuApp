using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using DjvuApp.Djvu;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DjvuApp.Model
{
    public sealed class EfBook : IBook
    {
        public string Title
        {
            get
            {
                return _efBookDto.Title;
            }
            set
            {
                if (value == _efBookDto.Title) return;
                _efBookDto.Title = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastOpeningTime
        {
            get
            {
                return _efBookDto.LastOpeningTime;
            }
            set
            {
                if (value.Equals(_efBookDto.LastOpeningTime)) return;
                _efBookDto.LastOpeningTime = value;
                OnPropertyChanged();
            }
        }

        public uint? LastOpenedPage
        {
            get
            {
                return _efBookDto.LastOpenedPage;
            }
            set
            {
                if (value == _efBookDto.LastOpenedPage) return;
                _efBookDto.LastOpenedPage = value;
                OnPropertyChanged();
            }
        }

        public uint PageCount => _efBookDto.PageCount;

        public string BookPath => _efBookDto.BookPath;

        public string ThumbnailPath
        {
            get
            {
                return _efBookDto.ThumbnailPath;
            }
            private set
            {
                if (value == _efBookDto.ThumbnailPath) return;
                _efBookDto.ThumbnailPath = value;
                OnPropertyChanged();
            }
        }

        public ReadOnlyObservableCollection<IBookmark> Bookmarks { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly EfBookDto _efBookDto;
        private readonly EfBooksContext _context;
        private readonly ObservableCollection<IBook> _books;
        private readonly ObservableCollection<IBookmark> _bookmarks;

        public EfBook(EfBookDto efBookDto, EfBooksContext context, ObservableCollection<IBook> books)
        {
            _efBookDto = efBookDto;
            _context = context;
            _books = books;

            var bookmarks = _efBookDto.Bookmarks.Select(bookmarkDto => new EfBookmark(bookmarkDto, _context));
            _bookmarks = new ObservableCollection<IBookmark>(bookmarks);
            Bookmarks = new ReadOnlyObservableCollection<IBookmark>(_bookmarks);
        }

        public async Task AddBookmarkAsync(string title, uint pageNumber)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("title is empty", nameof(title));
            if (pageNumber < 1 || pageNumber > PageCount)
                throw new ArgumentOutOfRangeException(nameof(pageNumber));

            var bookmarkDto = new EfBookmarkDto
            {
                Title = title,
                PageNumber = pageNumber,
                EfBookDto = _efBookDto
            };
            _context.Bookmarks.Add(bookmarkDto);
            await SaveChangesAsync();

            var bookmark = new EfBookmark(bookmarkDto, _context);
            _bookmarks.Add(bookmark);
        }

        public async Task UpdateThumbnailAsync()
        {
            var bookFile = await StorageFile.GetFileFromPathAsync(BookPath);
            var document = await DjvuDocument.LoadAsync(bookFile);
            var thumbnailFile = await SaveThumbnail(_efBookDto.Id, document);
            ThumbnailPath = thumbnailFile.Path;
            await SaveChangesAsync();
        }

        public async Task RemoveAsync()
        {
            if (!_books.Contains(this))
            {
                return;
            }

            _books.Remove(this);
            _context.Books.Remove(_efBookDto);
            await _context.SaveChangesAsync();

            var bookFile = await StorageFile.GetFileFromPathAsync(BookPath);
            await bookFile.DeleteAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private static async Task<IStorageFile> SaveThumbnail(int id, DjvuDocument document)
        {
            var page = await document.GetPageAsync(1);

            var maxWidth = 140 * DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var aspectRatio = (double)page.Width / page.Height;
            var width = (uint)Math.Min(maxWidth, page.Width);
            var height = (uint)(width / aspectRatio);

            var bitmap = new WriteableBitmap((int)width, (int)height);
            await page.RenderRegionAsync(
                bitmap: bitmap,
                rescaledPageSize: new BitmapSize { Width = width, Height = height },
                renderRegion: new BitmapBounds { Width = width, Height = height });

            var booksFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Books", CreationCollisionOption.OpenIfExists);
            var thumbnailFile = await booksFolder.CreateFileAsync($"{id}.jpg", CreationCollisionOption.ReplaceExisting);
            using (var thumbnailStream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, thumbnailStream);
                encoder.SetPixelData(
                    pixelFormat: BitmapPixelFormat.Bgra8,
                    alphaMode: BitmapAlphaMode.Ignore,
                    width: (uint)bitmap.PixelWidth,
                    height: (uint)bitmap.PixelHeight,
                    dpiX: 96.0,
                    dpiY: 96.0,
                    pixels: bitmap.PixelBuffer.ToArray());
                await encoder.FlushAsync();
            }
            return thumbnailFile;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}