using System;
using System.Globalization;

namespace FocusFlow.Core.Application.Utilities
{
    public static class DateParser
    {
        public static DateTime ParseDateHeader(string? dateRaw)
        {
            if (string.IsNullOrWhiteSpace(dateRaw))
                return DateTime.UtcNow;

            if (DateTimeOffset.TryParse(
                    dateRaw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal,
                    out var dto))
            {
                return dto.UtcDateTime;
            }

            return DateTime.UtcNow;
        }
    }
}

