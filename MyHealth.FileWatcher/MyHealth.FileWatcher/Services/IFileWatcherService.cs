using System.Threading.Tasks;

namespace MyHealth.FileWatcher.Services
{
    public interface IFileWatcherService
    {
        Task StartListening();
        void StopListening();
    }
}
