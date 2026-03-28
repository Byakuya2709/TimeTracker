using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace TimeTracker.App.Services;

public static class ProcessMemoryCleaner
{
    public static void CollectAndTrim()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        IntPtr processHandle = Process.GetCurrentProcess().Handle;
        _ = EmptyWorkingSet(processHandle);
    }

    [DllImport("psapi.dll")]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);
}
