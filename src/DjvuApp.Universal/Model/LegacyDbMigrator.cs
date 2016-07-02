using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using SQLite;

namespace DjvuApp.Model
{
    public sealed class LegacyDbMigrator
    {
        private sealed class SqliteBook
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public Guid Guid { get; set; }

            [MaxLength(255)]
            public string Title { get; set; }

            public DateTime LastOpeningTime { get; set; }

            public uint? LastOpenedPage { get; set; }

            public DateTime CreationTime { get; set; }

            public uint PageCount { get; set; }

            public uint Size { get; set; }

            [MaxLength(255)]
            public string Path { get; set; }

            [MaxLength(255)]
            public string ThumbnailPath { get; set; }
        }

        private sealed class SqliteBookmark
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int BookId { get; set; }

            [MaxLength(255)]
            public string Title { get; set; }

            public uint PageNumber { get; set; }
        }

        public bool IsMigrationNeeded { get; private set; }

        private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "books.sqlite");

        private LegacyDbMigrator()
        {
            
        }

        public static async Task<LegacyDbMigrator> CreateAsync()
        {
            var migrator = new LegacyDbMigrator();
            try
            {
                await StorageFile.GetFileFromPathAsync(DbPath);
                migrator.IsMigrationNeeded = true;
            }
            catch (FileNotFoundException)
            {
            }
            return migrator;
        }
        
        public async Task MigrateAsync()
        {
            SqliteBook[] books = null;
            SqliteBookmark[] bookmarks = null;

            await Task.Factory.StartNew(() =>
            {
                using (var connection = new SQLiteConnection(DbPath))
                {
                    books = connection.Table<SqliteBook>().ToArray();
                    bookmarks = connection.Table<SqliteBookmark>().ToArray();
                }
            });
            
            using (var context = new EfBooksContext())
            {
                context.Books.AddRange(books.Select(book => new EfBookDto
                {
                    BookPath = book.Path,
                    LastOpenedPage = book.LastOpenedPage,
                    LastOpeningTime = book.LastOpeningTime,
                    PageCount = book.PageCount,
                    ThumbnailPath = book.ThumbnailPath,
                    Title = book.Title,
                    Bookmarks = bookmarks
                        .Where(bookmark => bookmark.BookId == book.Id)
                        .Select(bookmark => new EfBookmarkDto
                        {
                            PageNumber = bookmark.PageNumber,
                            Title = bookmark.Title
                        }).ToList()
                }));
                await context.SaveChangesAsync();
            }
            
            var dbFile = await StorageFile.GetFileFromPathAsync(DbPath);
            await dbFile.DeleteAsync();
        }
    }
}
