using System.IO;

namespace GameGuard.Core;

public static class BlocklistMatcher
{
    public static bool IsBlocked(string processName, string? executablePath, GameGuardConfig config)
    {
        var name = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : processName + ".exe";

        foreach (var blocked in config.BlockedExecutables)
            if (string.Equals(blocked, name, StringComparison.OrdinalIgnoreCase))
                return true;

        if (!string.IsNullOrEmpty(executablePath))
        {
            string full;
            try { full = NormalizeDir(Path.GetFullPath(executablePath)); }
            catch { full = ""; }

            if (full.Length > 0)
            {
                foreach (var folder in config.BlockedFolders)
                {
                    string root;
                    try { root = NormalizeDir(Path.GetFullPath(folder)); }
                    catch { continue; }

                    if (full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        || full.Equals(root, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        return false;
    }

    private static string NormalizeDir(string path)
        => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
