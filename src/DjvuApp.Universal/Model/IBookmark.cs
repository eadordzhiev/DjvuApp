using System.ComponentModel;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DjvuApp.Model
{
    public interface IBookmark : ISaveChanges, INotifyPropertyChanged
    {
        [NotNull]
        string Title { get; }

        uint PageNumber { get; }

        Task RemoveAsync();

        // This is needed to workaround a XAML bug
        // http://blog.pieeatingninjas.be/2015/11/15/xbind-and-inheritance/
        new event PropertyChangedEventHandler PropertyChanged;
    }
}