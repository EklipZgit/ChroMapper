using System.IO;

public static class PathUtils
{
    // This is the sole Path.Combine exemption; normalize its platform-specific result before callers can use it.
    public static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
}
