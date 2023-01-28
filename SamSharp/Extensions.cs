using System;

namespace SamSharp
{
    internal static class Extensions
    {
        internal static string JsSubstring(this string text, int pos, int length) =>
            text.Substring(pos, Math.Min(length, text.Length - pos));

        internal static string JsSubstring(this string text, int pos) => pos > text.Length ? "" : text[pos..];
    }
}