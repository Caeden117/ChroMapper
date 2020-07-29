static class CharExtensions
{
    public static bool IsHex(this char c)
    {
        return (c >= '0' && c <= '9') ||
                 (c >= 'a' && c <= 'f') ||
                 (c >= 'A' && c <= 'F');
    }
}
