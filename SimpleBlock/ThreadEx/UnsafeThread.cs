using System;
using System.Threading;

namespace SimpleBlock.ThreadEx {
    public delegate void HasEndedThread(UnsafeThread thr);

    public class UnsafeThread : IDisposable {
        private ThreadStart _action;
        private CancelToken _token;
        private Thread _thread;
        private int _id = 0;
        private bool _disposed;
        public bool IsBlocked { get; private set; } = false;
        public event HasEndedThread OnHasEnded;

        private IntPtr _handle = IntPtr.Zero;
        public IntPtr Handle {
            get {
                if (_handle == IntPtr.Zero)
                    //Terminate | Sync
                    _handle = ThreadWin32.OpenThread(0x0001 | 0x000100000, false, _id);

                return _handle;
            }
        }

        public bool IsRunning => _thread.IsAlive;

        public UnsafeThread(ThreadStart action, HasEndedThread hasEnded = null) {
            _action = action;
            _thread = new Thread(callback_try_start);
            if (hasEnded != null) {
                OnHasEnded += hasEnded;
            }
        }

        public UnsafeThread(ThreadStart action, CancelToken cancellationToken, HasEndedThread hasEnded = null) {
            _token = cancellationToken;
            _action = action;
            _thread = new Thread(callback_start);
            //_thread.Join();
            _token.Threads.Add(this);
            if (hasEnded != null) {
                OnHasEnded += hasEnded;
            }
        }

        public void InvokeEnded()
            => OnHasEnded?.Invoke(this);

        private void callback_start() {
            _id = ThreadWin32.GetCurrentThreadId();
            _action.Invoke();
            OnHasEnded?.Invoke(this);
            InternalCleanUp();
        }

        private void callback_try_start() {
            try {
                callback_start();
            }
            catch {
                InternalCleanUp();
            }
        }

        public void Start() {
            if (IsBlocked)
                return;

            IsBlocked = true;
            _thread.Start();
        }

        //Start Have Stop notify Callback when done ??? :0
        public void StartAwait(int timeoutMs = -1, bool killIfAlive = false) {
            if (IsBlocked)
                return;

            IsBlocked = true;
            _thread.Start();
            Thread.Sleep(300);

            this.WaitForExit(timeoutMs);
            if (killIfAlive && IsRunning)
                Stop();//fix up
        }

        public void Stop(bool useTerminate = false, bool waitExit = false, bool invokeEnded = true) {
            if (Handle == IntPtr.Zero || _id == 0) {
                LogEx.LogError("Thread Is NULL", "Thread Handle / ID Is 0");
                return;
            }

            if (useTerminate) ThreadWin32.TerminateEx(Handle, 1);
            else _thread.Abort();

            if (waitExit)
                this.WaitForExit();

            LogEx.LogData("Thread Killed", $"Thread Should Have been Aborted/Terminated >> {_id}");

            if (invokeEnded) 
                OnHasEnded?.Invoke(this);

            InternalCleanUp();
        }

        private void InternalCleanUp() {
            if (_id == 0)
                return;

            if (_token != null)
                _token.Threads.Remove(this);

            if (_handle != IntPtr.Zero)
                ThreadWin32.CloseHandle(_handle);

            _id = 0;
            _token = null;
        }

        public static void Sleep(int time)
            => Thread.Sleep(time);

        ~UnsafeThread() {
            Dispose();
        }

        public virtual void Dispose() {
            if (_disposed)
                return;

            _disposed = true;
            GC.SuppressFinalize(this);
            _thread = null;
            InternalCleanUp();
        }
    }
}
