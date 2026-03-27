using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace TimeTracker.Infrastructure.Services;

public sealed class BrowserAddressBarReader
{
    private static readonly HashSet<string> SupportedBrowserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "msedge",
        "chrome",
        "browser"
    };

    private static readonly Dictionary<string, string> HostToAppName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chatgpt.com"] = "ChatGPT",
        ["gemini.com"] = "ChatGPT",
        ["openai.com"] = "OpenAI",
        ["github.com"] = "GitHub",
        ["youtube.com"] = "YouTube",
        ["notion.so"] = "Notion",
        ["notion.site"] = "Notion",
        ["facebook.com"] = "Facebook"
    };

    public static bool IsSupportedBrowser(string processName)
    {
        return !string.IsNullOrWhiteSpace(processName)
            && SupportedBrowserProcesses.Contains(processName);
    }

    public bool TryResolveTrackingAppName(IntPtr browserWindowHandle, string browserDisplayName, out string trackingAppName)
    {
        trackingAppName = browserDisplayName;

        if (browserWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        string? url = TryReadBrowserUrl(browserWindowHandle);
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        string? webAppName = ResolveWebAppName(url);
        if (string.IsNullOrWhiteSpace(webAppName))
        {
            return false;
        }

        trackingAppName = webAppName;
        return true;
    }

    private static string? TryReadBrowserUrl(IntPtr browserWindowHandle)
    {
        try
        {
            using UIA3Automation automation = new();
            AutomationElement root = automation.FromHandle(browserWindowHandle);
            if (root is null)
            {
                return null;
            }

            AutomationElement[] editElements = root.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit));
            if (editElements.Length == 0)
            {
                return null;
            }

            foreach (AutomationElement editElement in editElements)
            {
                string candidate = GetElementValue(editElement);
                if (LooksLikeUrl(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetElementValue(AutomationElement element)
    {
        try
        {
            string? value = element.Patterns.Value.PatternOrDefault?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        catch
        {
            // Ignore and fallback to Name.
        }

        return element.Name?.Trim() ?? string.Empty;
    }

    private static bool LooksLikeUrl(string value)
    {
        return TryParseBrowserUri(value, out _);
    }

    private static string? ResolveWebAppName(string url)
    {
        if (!TryParseBrowserUri(url, out Uri? uri))
        {
            return null;
        }


        string host = NormalizeHost(uri!.Host);

        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        if (HostToAppName.TryGetValue(host, out string? appName))
        {
            return appName;
        }

        string? matchedSuffix = HostToAppName.Keys
            .FirstOrDefault(key => host.EndsWith($".{key}", StringComparison.OrdinalIgnoreCase));

        if (matchedSuffix is not null)
        {
            return HostToAppName[matchedSuffix];
        }

        return ExtractFirstHostLabel(host);
    }

    private static string? ExtractFirstHostLabel(string host)
    {
        string normalizedHost = NormalizeHost(host);
        if (string.IsNullOrWhiteSpace(normalizedHost))
        {
            return null;
        }

        int firstDot = normalizedHost.IndexOf('.');
        if (firstDot <= 0)
        {
            return null;
        }

        string label = normalizedHost[..firstDot].Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        return $"WEB: {label}";
    }

    private static bool TryParseBrowserUri(string? rawUrl, out Uri? uri)
    {
        uri = null;

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return false;
        }

        string candidate = rawUrl.Trim();
        if (!candidate.Contains('.'))
        {
            return false;
        }

        return Uri.TryCreate(candidate, UriKind.Absolute, out uri)
            || Uri.TryCreate($"https://{candidate}", UriKind.Absolute, out uri);
    }

    private static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return string.Empty;
        }

        string normalizedHost = host.Trim().ToLowerInvariant();
        if (normalizedHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            normalizedHost = normalizedHost[4..];
        }

        return normalizedHost;
    }
}