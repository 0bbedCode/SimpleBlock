using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBlock.ThreadEx {

    [Flags]
    public enum ProcessAccess : int {
        AllAccess = CreateThread | DuplicateHandle | QueryInformation | SetInformation | Terminate | VMOperation | VMRead | VMWrite | Synchronize,
        CreateThread = 0x2,
        DuplicateHandle = 0x40,
        QueryInformation = 0x400,
        SetInformation = 0x200,
        Terminate = 0x1,
        VMOperation = 0x8,
        VMRead = 0x10,
        VMWrite = 0x20,
        Synchronize = 0x100000
    }

    [Flags]
    public enum AllocationType {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    public enum MemoryProtection {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION {
        public int BaseAddress;
        public int AllocationBase;
        public int AllocationProtect;
        public int RegionSize;
        public int State;
        public int Protect;
        public int Type;
    }

    internal class ThreadWin32 {
        //[DllImport("kernel32.dll")]
        //public static extern IntPtr OpenProcess(ProcessAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        //[DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out int lpNumberOfBytesRead);

        //[DllImport("kernel32.dll", SetLastError = true)]
        //public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll")]
        static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, uint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);


        [DllImport("Kernel32.dll")]
        internal static extern IntPtr OpenThread(uint access, bool takeHandle, int threadId);

        [DllImport("Kernel32.dll")]
        internal static extern int GetCurrentThreadId(); //GetCurrentThreadId

        [DllImport("Kernel32.dll")]
        internal static extern bool CancelSynchronousIo(IntPtr hThread);

        [DllImport("Kernel32.dll")]
        internal static extern bool TerminateThread(IntPtr hThread, int exitCode);

        [DllImport("Kernel32.dll")]
        internal static extern bool DuplicateHandle(IntPtr sourceProcess, IntPtr sourceHandle, IntPtr targetProcess, out IntPtr dupHandle, uint access, bool bInheritHandle, int options);

        [DllImport("Kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("Kernel32.dll")]
        internal static extern int WaitForSingleObject(IntPtr hHandle, int timeout);

        /*[DllImport("Kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr handle);*/

        internal static IntPtr DuplicateHandleEx(IntPtr handle, uint access = 0) {
            if (handle == null || handle == IntPtr.Zero)
                return IntPtr.Zero;

            var op = (access == 0) ? 0x00000002 : 0;
            var hOutHandle = IntPtr.Zero;

            DuplicateHandle(GetCurrentProcess(), handle, GetCurrentProcess(), out hOutHandle, 0, false, op);

            return hOutHandle;
        }

        internal static void TerminateEx(IntPtr hThread, int exitCode) {
            if (hThread == IntPtr.Zero || hThread == null)
                return;

            if (!TerminateThread(hThread, exitCode)) {
                LogEx.LogError("Failed to Terminate Thread", $"Thread HTHREAD >> {hThread}");
                CancelSynchronousIo(hThread);
                CloseHandle(hThread);
            }
        }
    }
}
