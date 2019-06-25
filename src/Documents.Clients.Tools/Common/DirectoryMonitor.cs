namespace Documents.Clients.Tools.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;

    public delegate void FileSystemEvent(String path);

    public class DirectoryMonitor : IDisposable
    {
        private readonly FileSystemWatcher m_fileSystemWatcher = new FileSystemWatcher();
        private readonly Dictionary<string, DateTime> m_pendingEvents = new Dictionary<string, DateTime>();
        private readonly Timer m_timer;
        private bool m_timerStarted = false;

        public DirectoryMonitor(string dirPath)
        {
            if (Directory.Exists(dirPath))
                m_fileSystemWatcher.Path = dirPath;
            else if (File.Exists(dirPath))
            {
                m_fileSystemWatcher.Path = Path.GetDirectoryName(Path.GetFullPath(dirPath));
                m_fileSystemWatcher.Filter = Path.GetFileName(dirPath);
            }

            m_fileSystemWatcher.IncludeSubdirectories = true;
            m_fileSystemWatcher.Created += OnChange;
            m_fileSystemWatcher.Changed += OnChange;
            m_fileSystemWatcher.Renamed += OnRename;
            m_fileSystemWatcher.Deleted += OnChange;

            m_timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
        }

        public event FileSystemEvent Change;

        public void Start()
        {
            m_fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void OnRename(object sender, RenamedEventArgs e)
        {

            CaptureEvent(e.FullPath);
            CaptureEvent(e.OldFullPath);
        }

        private void OnChange(object sender, FileSystemEventArgs e)
        {
            CaptureEvent(e.FullPath);
        }

        private void CaptureEvent(string fullPath)
        {
            // Don't want other threads messing with the pending events right now
            lock (m_pendingEvents)
            {
                // Save a timestamp for the most recent event for this path
                m_pendingEvents[fullPath] = DateTime.Now;

                // Start a timer if not already started
                if (!m_timerStarted)
                {
                    m_timer.Change(100, 100);
                    m_timerStarted = true;
                }
            }
        }

        private void OnTimeout(object state)
        {
            List<string> paths;

            // Don't want other threads messing with the pending events right now
            lock (m_pendingEvents)
            {
                // Get a list of all paths that should have events thrown
                paths = FindReadyPaths(m_pendingEvents);

                // Remove paths that are going to be used now
                paths.ForEach(delegate (string path)
                {
                    m_pendingEvents.Remove(path);
                });

                // Stop the timer if there are no more events pending
                if (m_pendingEvents.Count == 0)
                {
                    m_timer.Change(Timeout.Infinite, Timeout.Infinite);
                    m_timerStarted = false;
                }
            }

            // Fire an event for each path that has changed
            paths.ForEach(delegate (string path)
            {
                FireEvent(path);
            });
        }

        private List<string> FindReadyPaths(Dictionary<string, DateTime> events)
        {
            List<string> results = new List<string>();
            DateTime now = DateTime.Now;

            foreach (KeyValuePair<string, DateTime> entry in events)
            {
                // If the path has not received a new event in the last 75ms
                // an event for the path should be fired
                double diff = now.Subtract(entry.Value).TotalMilliseconds;
                if (diff >= 75)
                {
                    results.Add(entry.Key);
                }
            }

            return results;
        }

        private void FireEvent(string path)
        {
            Change?.Invoke(path);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_fileSystemWatcher.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DirectoryMonitor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}