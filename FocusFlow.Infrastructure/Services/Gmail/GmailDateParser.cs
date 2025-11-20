using System;
using System.Text.Json;
using FocusFlow.Core.Application.Utilities;

namespace FocusFlow.Infrastructure.Services.Gmail
{
    public static class GmailDateParser
    {
        public static DateTime Parse(JsonElement messageRoot, string? dateHeader)
        {
            var internalDate = TryParseInternalDate(messageRoot);
            if (internalDate.HasValue)
                return internalDate.Value;

            return DateParser.ParseDateHeader(dateHeader);
        }

        private static DateTime? TryParseInternalDate(JsonElement messageRoot)
        {
            try
            {
                if (messageRoot.TryGetProperty("internalDate", out var internalDateElement))
                {
                    var internalDateString = internalDateElement.GetString();
                    if (!string.IsNullOrWhiteSpace(internalDateString) &&
                        long.TryParse(internalDateString, out var millis))
                    {
                        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
                    }
                }
            }
            catch
            {
                // Fall back to Date header
            }

            return null;
        }
    }
}

