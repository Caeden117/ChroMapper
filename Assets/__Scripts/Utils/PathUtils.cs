using System.IO;

public static class PathUtils
{
    // Path.Combine is banned because it returns host-specific separators Unity can't use (which we swap below).
    // This is the sole Path.Combine exemption. Normalize its platform-specific result before callers can use it.
    public static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');
}
