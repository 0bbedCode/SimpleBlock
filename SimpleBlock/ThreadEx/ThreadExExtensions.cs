using System;
using System.Threading;

namespace SimpleBlock.ThreadEx {
    public static class ThreadExExtensions {

        /// <summary>
        /// Wait for the Thread to Exit
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="timeoutMS"></param>
        public static void WaitForExit(this UnsafeThread thread, int timeoutMS = -1) {
            if (!thread.IsRunning || thread.Handle == IntPtr.Zero)
                return;

            var dupHandle = ThreadWin32.DuplicateHandleEx(thread.Handle);
            if (dupHandle == IntPtr.Zero)
                return;

            ThreadWin32.WaitForSingleObject(dupHandle, timeoutMS);
            ThreadWin32.CloseHandle(dupHandle);
        }

        /// <summary>
        /// This will Start the Thread and Wait til Completion
        /// Dont Use this if you dont care to Use WaitForSingleObject
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="timeoutMS"></param>
        /// <param name="killIfAlive"></param>
        public static void StartAwaitEx(this UnsafeThread thread, int timeoutMS = -1, bool killIfAlive = false) {
            if (thread.IsRunning || thread.IsBlocked)
                return;

            var stpWatch = new System.Diagnostics.Stopwatch();
            if (timeoutMS != -1)
                stpWatch.Start();

            thread.Start();

            while (thread.IsRunning && stpWatch.ElapsedMilliseconds < timeoutMS)
                Thread.Sleep(800);

            if (thread.IsRunning && killIfAlive)
                thread.Stop();//revise
        }

        /// <summary>
        /// Wait and Get Running Time of Thread retuns in MS Milliseconds
        /// </summary>
        /// <param name="thread"></param>
        /// <param name="timoutMS"></param>
        /// <returns></returns>
        public static long StartGetRunningTime(this UnsafeThread thread, int timoutMS = -1) {
            if (thread.IsRunning || thread.IsBlocked)
                return 0;

            var stpWatch = new System.Diagnostics.Stopwatch();
            stpWatch.Start();
            thread.Start();
            thread.WaitForExit(timoutMS);
            stpWatch.Stop();
            return stpWatch.ElapsedMilliseconds - 20;
        }
    }
}
