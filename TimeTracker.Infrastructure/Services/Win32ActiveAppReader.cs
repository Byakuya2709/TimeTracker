using System.Diagnostics;
using System.Runtime.InteropServices;
using TimeTracker.Application.Abstractions;

namespace TimeTracker.Infrastructure.Services;

public class Win32ActiveAppReader : IActiveAppReader
{
    public string GetActiveAppName()
    {
        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
        {
            return "Unknown App";
        }

        uint processId;
        _ = GetWindowThreadProcessId(hWnd, out processId);

        try
        {
            Process process = Process.GetProcessById((int)processId);
            return string.IsNullOrWhiteSpace(process.ProcessName)
                ? "Unknown App"
                : process.ProcessName;
        }
        catch
        {
            return "Unknown App";
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
