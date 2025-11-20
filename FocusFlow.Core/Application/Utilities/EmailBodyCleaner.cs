using System;
using System.Net;
using System.Text.RegularExpressions;

namespace FocusFlow.Core.Application.Utilities
{
    public static class EmailBodyCleaner
    {
        public static string CleanHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            html = RemoveScriptAndStyleBlocks(html);
            var text = RemoveHtmlTags(html);
            text = DecodeHtmlEntities(text);
            text = RemoveInlineStyles(text);
            text = RemoveUrls(text);
            text = RemoveSpecialArtifacts(text);
            text = NormalizeWhitespace(text);
            text = RemoveFooter(text);

            return text;
        }

        public static string CleanPlainText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = RemoveScriptAndStyleBlocks(text);
            text = RemoveHtmlTags(text);
            text = DecodeHtmlEntities(text);
            text = RemoveInlineStyles(text);
            text = RemoveCssBlocks(text);
            text = RemoveUrls(text);
            text = RemoveSpecialArtifacts(text);
            text = NormalizeWhitespace(text);
            text = RemoveFooter(text);

            return text;
        }

        private static string RemoveScriptAndStyleBlocks(string input) =>
            Regex.Replace(
                input,
                "<(script|style)[^>]*?>.*?</\\1>",
                "",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static string RemoveHtmlTags(string input) =>
            Regex.Replace(input, "<.*?>", " ", RegexOptions.Singleline);

        private static string DecodeHtmlEntities(string input) =>
            WebUtility.HtmlDecode(input);

        private static string RemoveInlineStyles(string input) =>
            Regex.Replace(input, "style=\"[^\"]*\"", " ", RegexOptions.IgnoreCase);

        private static string RemoveCssBlocks(string input) =>
            Regex.Replace(
                input,
                @"@media[^{]+{[^}]+}",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static string RemoveUrls(string input) =>
            Regex.Replace(input, @"https?://\S+|www\.\S+", " ", RegexOptions.IgnoreCase);

        private static string RemoveSpecialArtifacts(string input) =>
            input.Replace(" 96 ", " ");

        private static string NormalizeWhitespace(string input) =>
            Regex.Replace(input, @"\s+", " ").Trim();

        private static string RemoveFooter(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var lower = text.ToLowerInvariant();
            var cutIndex = text.Length;

            string[] footerKeywords =
            {
                "unsubscribe",
                "privacy policy",
                "privacy beleid",
                "terms & conditions",
                "terms and conditions",
                "conditions",
                "contact us",
                "uitschrijven",
                "view this e-mail in your browser",
                "view this email in your browser",
                "view online",
                "copyright",
                " ryanair",
                " privacy",
                
            };

            foreach (var keyword in footerKeywords)
            {
                var idx = lower.IndexOf(keyword, StringComparison.Ordinal);
                if (idx >= 0 && idx > 100 && (text.Length - idx) < 800 && idx < cutIndex)
                {
                    cutIndex = idx;
                }
            }

            return cutIndex < text.Length ? text.Substring(0, cutIndex).Trim() : text;
        }
    }
}

