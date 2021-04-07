namespace MyHealth.FileWatcher.Services
{
    public interface IFileWatcherService
    {
        void StartListening();
        void StopListening();
    }
}
