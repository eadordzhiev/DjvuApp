using Microsoft.EntityFrameworkCore;

namespace DjvuApp.Model
{
    public sealed class EfBooksContext : DbContext
    {
        public DbSet<EfBookDto> Books { get; set; }

        public DbSet<EfBookmarkDto> Bookmarks { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=books.db");
        }
    }
}