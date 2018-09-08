using System.Threading.Tasks;

namespace Gofer.NET
{
    public interface IBackendLock
    {
        Task Release();
    }
}