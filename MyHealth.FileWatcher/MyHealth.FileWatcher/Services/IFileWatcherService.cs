using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyHealth.FileWatcher.Services
{
    public interface IFileWatcherService
    {
        void StartListening();
        void StopListening();
    }
}
