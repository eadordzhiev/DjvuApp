using System.Threading.Tasks;

namespace DjvuApp.Model
{
    public interface ISaveChanges
    {
        Task SaveChangesAsync();
    }
}