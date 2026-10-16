using System.IO;

public static class PathUtils
{
    private static readonly char[] pathCharacters = { '/', '\\', ':' };

    // Path.Combine is banned because it returns host-specific separators Unity can't use (which we swap below).
    // This is the sole Path.Combine exemption. Normalize its platform-specific result before callers can use it.
    public static string Combine(params string[] parts) => Path.Combine(parts).Replace('\\', '/');

    // Check both separator styles and drive prefixes, regardless of the OS loading the map.
    public static string RequireFileName(string fileName)
    {
        if (fileName != null && fileName.IndexOfAny(pathCharacters) >= 0)
            throw new InvalidDataException("Expected a filename without a path: " + fileName);

        return fileName;
    }
}
