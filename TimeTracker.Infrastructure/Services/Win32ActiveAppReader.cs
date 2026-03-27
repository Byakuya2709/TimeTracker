using System.Diagnostics;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TimeTracker.Application.Abstractions;

namespace TimeTracker.Infrastructure.Services;

public class Win32ActiveAppReader : IActiveAppReader, IIdleThresholdConfigurable
{
    private const string UnknownAppName = "Unknown App";
    private const string IdleAppName = "Idle";
    private const int DefaultIdleThresholdSeconds = 300;

    private const int GaRootowner = 3;
    private const uint GwHwndnext = 2;
    private const uint GwHwndprev = 3;
    private const uint GwOwner = 4;
    private const int DwmwaCloaked = 14;
    private const int MaxWindowScan = 50;
    private const int GwlExstyle = -20;
    private const int WsExToolwindow = 0x00000080;
    private const int MaxProcessNameCacheSize = 512;

    private string _lastKnownAppDisplayName = UnknownAppName;
    private IntPtr _lastForegroundWindow = IntPtr.Zero;
    private string _lastResolvedAppName = UnknownAppName;
    private uint _idleThresholdMilliseconds;

    private static readonly ConcurrentDictionary<int, string> ProcessDisplayNameCache = new();
    private static readonly BrowserAddressBarReader BrowserAddressBarReader = new();

    public Win32ActiveAppReader()
        : this(TimeSpan.FromSeconds(DefaultIdleThresholdSeconds))
    {
    }

    public Win32ActiveAppReader(TimeSpan idleThreshold)
    {
        _idleThresholdMilliseconds = NormalizeThresholdMilliseconds(idleThreshold);
    }

    public void SetIdleThreshold(TimeSpan idleThreshold)
    {
        _idleThresholdMilliseconds = NormalizeThresholdMilliseconds(idleThreshold);
    }

    public string GetActiveAppName()
    {
        if (IsUserIdle(_idleThresholdMilliseconds))
        {
            _lastForegroundWindow = IntPtr.Zero;
            _lastResolvedAppName = IdleAppName;
            return IdleAppName;
        }

        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return _lastKnownAppDisplayName;
        }

        if (foregroundWindow == _lastForegroundWindow && !IsSupportedBrowserWindow(foregroundWindow))
        {
            return _lastResolvedAppName;
        }

        // When desktop is foreground (e.g., user pressed Show Desktop / minimized all apps),
        // return Unknown so upper layers can treat this period as unassigned time.
        if (IsDesktopShellWindow(foregroundWindow))
        {
            _lastForegroundWindow = foregroundWindow;
            _lastResolvedAppName = UnknownAppName;
            return UnknownAppName;
        }

        if (TryGetTrackableAppName(foregroundWindow, out string foregroundAppName))
        {
            _lastKnownAppDisplayName = foregroundAppName;
            _lastForegroundWindow = foregroundWindow;
            _lastResolvedAppName = foregroundAppName;
            return foregroundAppName;
        }

        if (TryFindTrackableAppNameAround(foregroundWindow, out string fallbackAppName))
        {
            _lastKnownAppDisplayName = fallbackAppName;
            _lastForegroundWindow = foregroundWindow;
            _lastResolvedAppName = fallbackAppName;
            return fallbackAppName;
        }

        _lastForegroundWindow = foregroundWindow;
        _lastResolvedAppName = _lastKnownAppDisplayName;
        return _lastKnownAppDisplayName;
    }

    private static bool TryFindTrackableAppNameAround(IntPtr startWindow, out string appName)
    {
        if (TryScanDirection(startWindow, GwHwndnext, out appName))
        {
            return true;
        }

        if (TryScanDirection(startWindow, GwHwndprev, out appName))
        {
            return true;
        }

        appName = UnknownAppName;
        return false;
    }

    private static bool TryScanDirection(IntPtr startWindow, uint direction, out string appName)
    {
        IntPtr currentWindow = startWindow;

        for (int i = 0; i < MaxWindowScan; i++)
        {
            currentWindow = GetWindow(currentWindow, direction);
            if (currentWindow == IntPtr.Zero)
            {
                break;
            }

            if (TryGetTrackableAppName(currentWindow, out appName))
            {
                return true;
            }
        }

        appName = UnknownAppName;
        return false;
    }

    private static bool TryGetTrackableAppName(IntPtr windowHandle, out string appName)
    {
        if (!IsCandidateUserWindow(windowHandle))
        {
            appName = UnknownAppName;
            return false;
        }

        appName = GetAppDisplayNameFromWindow(windowHandle);
        return !string.IsNullOrWhiteSpace(appName)
            && !appName.Equals(UnknownAppName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAppDisplayNameFromWindow(IntPtr windowHandle)
    {
        _ = GetWindowThreadProcessId(windowHandle, out uint processId);
        if (processId == 0)
        {
            return UnknownAppName;
        }

        try
        {
            int pid = (int)processId;
            Process process = Process.GetProcessById(pid);
            string processName = process.ProcessName;

            string displayName = ProcessDisplayNameCache.TryGetValue(pid, out string? cachedName)
                && !string.IsNullOrWhiteSpace(cachedName)
                ? cachedName
                : GetDisplayNameFromProcess(process);

            if (!string.IsNullOrWhiteSpace(displayName)
                && !displayName.Equals(UnknownAppName, StringComparison.OrdinalIgnoreCase))
            {
                if (ProcessDisplayNameCache.Count > MaxProcessNameCacheSize)
                {
                    ProcessDisplayNameCache.Clear();
                }

                ProcessDisplayNameCache[pid] = displayName;
            }

            if (BrowserAddressBarReader.IsSupportedBrowser(processName)
                && BrowserAddressBarReader.TryResolveTrackingAppName(windowHandle, displayName, out string trackingAppName))
            {
                return trackingAppName;
            }

            return displayName;
        }
        catch
        {
            return UnknownAppName;
        }
    }

    private static bool IsSupportedBrowserWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(windowHandle, out uint processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            Process process = Process.GetProcessById((int)processId);
            return BrowserAddressBarReader.IsSupportedBrowser(process.ProcessName);
        }
        catch
        {
            return false;
        }
    }

    private static string GetDisplayNameFromProcess(Process process)
    {
        try
        {
            string? executablePath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(executablePath);
                if (!string.IsNullOrWhiteSpace(fileVersion.FileDescription))
                {
                    return fileVersion.FileDescription;
                }
            }
        }
        catch
        {
            // Fallback below when module metadata is inaccessible.
        }

        return string.IsNullOrWhiteSpace(process.ProcessName)
            ? UnknownAppName
            : process.ProcessName;
    }

    private static uint NormalizeThresholdMilliseconds(TimeSpan idleThreshold)
    {
        double thresholdMilliseconds = idleThreshold.TotalMilliseconds;
        if (double.IsNaN(thresholdMilliseconds)
            || double.IsInfinity(thresholdMilliseconds)
            || thresholdMilliseconds < 1000)
        {
            thresholdMilliseconds = TimeSpan.FromSeconds(DefaultIdleThresholdSeconds).TotalMilliseconds;
        }

        if (thresholdMilliseconds > uint.MaxValue)
        {
            thresholdMilliseconds = uint.MaxValue;
        }

        return (uint)thresholdMilliseconds;
    }

    private static bool IsDesktopShellWindow(IntPtr windowHandle)
    {
        string className = GetWindowClassName(windowHandle);
        return className.Equals("Progman", StringComparison.OrdinalIgnoreCase)
            || className.Equals("WorkerW", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetWindowClassName(IntPtr windowHandle)
    {
        const int maxClassNameLength = 256;
        char[] buffer = new char[maxClassNameLength];
        int length = GetClassName(windowHandle, buffer, buffer.Length);

        if (length <= 0)
        {
            return string.Empty;
        }

        return new string(buffer, 0, length);
    }

    private static bool IsCandidateUserWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero || !IsWindowVisible(windowHandle) || IsIconic(windowHandle))
        {
            return false;
        }

        if (!IsAltTabWindow(windowHandle))
        {
            return false;
        }

        if (IsWindowCloaked(windowHandle))
        {
            return false;
        }

        if (GetWindowTextLength(windowHandle) == 0)
        {
            return false;
        }

        if (GetWindow(windowHandle, GwOwner) != IntPtr.Zero)
        {
            return false;
        }

        int exStyle = GetWindowLong(windowHandle, GwlExstyle);
        bool isToolWindow = (exStyle & WsExToolwindow) == WsExToolwindow;
        if (isToolWindow)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(windowHandle, out uint processId);
        return processId != 0 && processId != Environment.ProcessId;
    }

    private static bool IsAltTabWindow(IntPtr windowHandle)
    {
        IntPtr rootOwner = GetAncestor(windowHandle, GaRootowner);
        if (rootOwner == IntPtr.Zero)
        {
            return false;
        }

        IntPtr walk = rootOwner;
        IntPtr tryWindow;

        while ((tryWindow = GetLastActivePopup(walk)) != walk)
        {
            if (IsWindowVisible(tryWindow))
            {
                break;
            }

            walk = tryWindow;
        }

        return walk == windowHandle;
    }

    private static bool IsWindowCloaked(IntPtr windowHandle)
    {
        int result = DwmGetWindowAttribute(windowHandle, DwmwaCloaked, out int cloaked, Marshal.SizeOf<int>());
        return result == 0 && cloaked != 0;
    }

    private static bool IsUserIdle(uint idleThresholdMilliseconds)
    {
        LastInputInfo lastInputInfo = new()
        {
            cbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref lastInputInfo))
        {
            return false;
        }

        uint tickNow = GetTickCount();
        uint idleMilliseconds = unchecked(tickNow - lastInputInfo.dwTime);
        return idleMilliseconds >= idleThresholdMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, int gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();
}
