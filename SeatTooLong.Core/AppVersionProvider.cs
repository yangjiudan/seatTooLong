using System.Reflection;

namespace SeatTooLong.Core;

public static class AppVersionProvider
{
    public static string GetVersion(Assembly? assembly = null)
    {
        var targetAssembly = assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var infoVersion = targetAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            var plusIndex = infoVersion.IndexOf('+');
            return plusIndex >= 0 ? infoVersion[..plusIndex] : infoVersion;
        }

        var fileVersion = targetAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        if (!string.IsNullOrWhiteSpace(fileVersion))
            return fileVersion;

        return targetAssembly.GetName().Version?.ToString() ?? "unknown";
    }

    public static string GetVersion(AssemblyName assemblyName)
    {
        return assemblyName.Version?.ToString() ?? "unknown";
    }
}
