using System.Threading.Tasks;

namespace Modules.DriverManagement.Interfaces
{
    public interface IDatabaseInitializer
    {
        Task EnsureCreatedAsync();
    }
}
