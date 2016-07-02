using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage;
using JetBrains.Annotations;

namespace DjvuApp.Model
{
    public interface IBookProvider
    {
        ReadOnlyObservableCollection<IBook> Books { get; }

        Task<IBook> AddBookAsync([NotNull] IStorageFile file);
    }
}
