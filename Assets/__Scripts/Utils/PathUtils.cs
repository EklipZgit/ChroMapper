using System.Text;

public static class PathUtils
{
    // Path.Combine is banned because it returns host-specific separators Unity can't use, and it only
    // treats host-native roots as rooted: on Linux a Windows-rooted child such as "D:\Maps\bookmarks.dat"
    // is appended instead of discarding earlier segments
    // (PathUtilsTest.CombineRootedChildDiscardsEarlierSegments). Normalize separators up front and detect
    // '/', '//' UNC, and 'X:' drive-letter roots ourselves so every host combines identically.
    public static string Combine(params string[] parts)
    {
        if (parts.Length == 0) return string.Empty;

        var normalized = new string[parts.Length];
        var first = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            normalized[i] = parts[i].Replace('\\', '/');
            if (IsRooted(normalized[i])) first = i;
        }

        var builder = new StringBuilder(normalized[first]);
        for (var i = first + 1; i < normalized.Length; i++)
        {
            if (builder.Length > 0 && builder[builder.Length - 1] != '/') builder.Append('/');
            builder.Append(normalized[i]);
        }
        return builder.ToString();
    }

    private static bool IsRooted(string path) =>
        path.StartsWith('/')
        || (path.Length >= 2 && path[1] == ':' && (path[0] is >= 'a' and <= 'z' or >= 'A' and <= 'Z'));
}
