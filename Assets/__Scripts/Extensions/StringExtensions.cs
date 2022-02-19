internal static class StringExtensions
{
    /*
    * Original code by @lolPants, put into an Extension class.
    * 
    * The second replace is not necessary, but is there for completeness. If you need to strip TMP tags regularly,
    * you can easily remove it for added performance.
    */
    public static string StripTMPTags(this string source) => source.Replace(@"<", "<\u200B").Replace(@">", "\u200B>");
}
