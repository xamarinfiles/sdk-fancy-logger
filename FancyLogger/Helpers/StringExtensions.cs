using System;
using static System.String;

namespace XamarinFiles.FancyLogger.Helpers
{
    internal static class StringExtensions
    {
        internal static string PadRight(this string? text, int totalWidth,
            string paddingStr)
        {
            text ??= "";

            if (totalWidth <= text.Length || IsNullOrEmpty(paddingStr))
            {
                return text;
            }

            var paddingLength = totalWidth - text.Length;

            // If one-character pad, use System.String.PadRight
            if (paddingStr.Length == 1)
            {
                var paddingChar = char.Parse(paddingStr);
                var paddedCharText =
                    text.PadRight(totalWidth, paddingChar);

                return paddedCharText;
            }

            var paddingCount =
                (uint)Math.Ceiling((double)paddingLength / paddingStr.Length);
            var paddedStrText = text + paddingStr.Repeat(paddingCount);
            var trimmedPaddedStrText =
                paddedStrText.Substring(0, totalWidth);

            return trimmedPaddedStrText;
        }

        // Adapted from "Using a for loop over a Span of characters"
        // https://blog.nimblepros.com/blogs/repeat-string-in-csharp/
        private static string Repeat(this string text, uint count)
        {
            var textAsSpan = text.AsSpan();

            var span = new Span<char>(new char[textAsSpan.Length * count]);

            for (var index = 0; index < count; index++)
            {
                textAsSpan.CopyTo(
                    span.Slice(index * textAsSpan.Length,
                    textAsSpan.Length));
            }

            return span.ToString();
        }
    }
}
