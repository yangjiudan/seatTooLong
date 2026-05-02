using System.Reflection;
using System.Text.RegularExpressions;
using SeatTooLong.Core;

namespace SeatTooLong.Tests;

public class VersioningTests
{
    [Fact]
    public void DirectoryBuildProps_DefinesSemVerAppVersion()
    {
        var propsPath = FindFileFromRepoRoot("Directory.Build.props");
        Assert.True(File.Exists(propsPath), $"Missing version source file: {propsPath}");

        var content = File.ReadAllText(propsPath);
        var match = Regex.Match(content, "<AppVersion>([^<]+)</AppVersion>");
        Assert.True(match.Success, "Directory.Build.props must define <AppVersion>.");
        Assert.Matches("^\\d+\\.\\d+\\.\\d+$", match.Groups[1].Value.Trim());
    }

    [Fact]
    public void AppVersionProvider_UsesInformationalVersion_WhenAvailable()
    {
        var version = AppVersionProvider.GetVersion(typeof(SittingMonitor).Assembly);
        Assert.Matches("^\\d+\\.\\d+\\.\\d+$", version);
    }

    [Fact]
    public void AppVersionProvider_FallsBackToUnknown_WhenVersionMissing()
    {
        var version = AppVersionProvider.GetVersion(new AssemblyName("SeatTooLong.Tests.NoVersion"));
        Assert.Equal("unknown", version);
    }

    [Fact]
    public void InstallerScript_UsesOverridableVersionDefine()
    {
        var scriptPath = FindFileFromRepoRoot(Path.Combine("installer", "SeatTooLong.iss"));
        var content = File.ReadAllText(scriptPath);

        Assert.Contains("#ifndef MyAppVersion", content);
        Assert.Contains("#define MyAppVersion \"0.0.0\"", content);
        Assert.Contains("#ifndef MyOutputBaseFilename", content);
        Assert.Contains("#define MyOutputBaseFilename \"SeatTooLong-Setup-x64-\" + MyAppVersion", content);
        Assert.Contains("OutputBaseFilename={#MyOutputBaseFilename}", content);
    }

    private static string FindFileFromRepoRoot(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            var siblingCandidate = Path.Combine(current.FullName, "..", "..", "..", "..", relativePath);
            siblingCandidate = Path.GetFullPath(siblingCandidate);
            if (File.Exists(siblingCandidate))
            {
                return siblingCandidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Unable to locate file from repository root: {relativePath}");
    }
}
