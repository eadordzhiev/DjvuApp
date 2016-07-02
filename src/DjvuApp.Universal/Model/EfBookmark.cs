using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DjvuApp.Model
{
    public sealed class EfBookmark : IBookmark
    {
        public string Title
        {
            get
            {
                return _efBookmarkDto.Title;
            }
            set
            {
                if (value == _efBookmarkDto.Title) return;
                _efBookmarkDto.Title = value;
                OnPropertyChanged();
            }
        }

        public uint PageNumber
        {
            get
            {
                return _efBookmarkDto.PageNumber;
            }
            set
            {
                if (value == _efBookmarkDto.PageNumber) return;
                _efBookmarkDto.PageNumber = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly EfBookmarkDto _efBookmarkDto;
        private readonly EfBooksContext _context;

        public EfBookmark(EfBookmarkDto efBookmarkDto, EfBooksContext context)
        {
            _efBookmarkDto = efBookmarkDto;
            _context = context;
        }

        public async Task RemoveAsync()
        {
            _efBookmarkDto.EfBookDto = null;
            await SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}