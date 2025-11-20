using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FocusFlow.Infrastructure.Services.Gmail
{
    public static class GmailLabelFilter
    {
        private static readonly HashSet<string> BlockedLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "SPAM",
            "TRASH",
            "CATEGORY_PROMOTIONS",
            "CATEGORY_SOCIAL",
            "CATEGORY_UPDATES",
            "CATEGORY_FORUMS"
        };

        public static bool ShouldSkip(JsonElement messageRoot)
        {
            if (!messageRoot.TryGetProperty("labelIds", out var labelsElement))
                return false;

            foreach (var label in labelsElement.EnumerateArray())
            {
                var labelValue = label.GetString();
                if (labelValue != null && BlockedLabels.Contains(labelValue))
                    return true;
            }

            return false;
        }
    }
}

