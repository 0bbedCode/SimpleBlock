using System;
using System.Collections.Generic;
using System.Threading;

namespace SimpleBlock.ThreadEx {
    public class CancelToken : IDisposable {
        public List<UnsafeThread> Threads { get; private set; } = new List<UnsafeThread>();

        private bool _disposed;
        private bool _busy = false;

        public void StartAsync(ThreadStart thrStart, HasEndedThread onHasEnded = null) {
            var thr = new UnsafeThread(thrStart, this, onHasEnded);
            thr.Start();
        }

        public UnsafeThread StartAsyncEx(ThreadStart thrStart, HasEndedThread onHasEnded = null) {
            var thr = new UnsafeThread(thrStart, this, onHasEnded);
            thr.Start();
            return thr;
        }

        public void CancelAsync(bool useTerminate = false) {
            if (_busy)
                return;

            _busy = true;

            for (int i = Threads.Count; i-- > 0;) {
                var thr = Threads[i];
                if (thr is null)
                    continue;

                new Thread(() => {
                    thr.Stop(useTerminate);
                    thr.Dispose();
                });
            }
        }

        public void Cancel(bool useTerminate = true, bool invokeEnded = true) {
            if (Threads.Count == 0)
                return;

            for (int i = Threads.Count; i-- > 0;) {
                var thr = Threads[i];
                if (thr is null)
                    continue;

                thr.Stop(useTerminate, invokeEnded);
            }

            Threads.Clear();
        }

        ~CancelToken() {
            Dispose();
        }

        public virtual void Dispose() {
            if (_disposed)
                return;

            _disposed = true;
            Cancel();
            GC.Collect();
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: false);
            GC.SuppressFinalize(this);
        }
    }
}
