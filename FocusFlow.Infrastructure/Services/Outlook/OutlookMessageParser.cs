using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Application.Utilities;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Core.Domain.Enums;
using System;
using System.Text.Json;

namespace FocusFlow.Infrastructure.Services.Outlook
{

    public sealed class OutlookMessageParser : IEmailMessageParser<JsonElement>
    {
        public Email? ParseMessage(JsonElement messageData, string messageId, Guid emailAccountId)
        {
            try
            {
                return ParseOutlookMessage(messageData, messageId, emailAccountId);
            }
            catch
            {
                return null;
            }
        }

        private static Email? ParseOutlookMessage(JsonElement messageElement, string messageId, Guid emailAccountId)
        {
            var subjectText = GetStringProperty(messageElement, "subject");
            var senderDisplayName = ExtractSender(messageElement);
            var receivedUtc = ExtractReceivedDateTime(messageElement);
            var bodyContent = ExtractBody(messageElement);

            return new Email(
                senderDisplayName,
                subjectText,
                bodyContent,
                receivedUtc,
                EmailProvider.Outlook,
                messageId,
                emailAccountId);
        }

        private static string ExtractSender(JsonElement messageElement)
        {
            if (!messageElement.TryGetProperty("from", out var fromObject))
                return string.Empty;

            if (!fromObject.TryGetProperty("emailAddress", out var emailAddressObject))
                return string.Empty;

            var senderName = GetNullableString(emailAddressObject, "name");
            var senderAddress = GetNullableString(emailAddressObject, "address");

            if (!string.IsNullOrWhiteSpace(senderName) && !string.IsNullOrWhiteSpace(senderAddress))
                return $"{senderName} <{senderAddress}>";

            return senderAddress ?? senderName ?? string.Empty;
        }

        private static DateTime ExtractReceivedDateTime(JsonElement messageElement)
        {
            var receivedRaw = GetNullableString(messageElement, "receivedDateTime");

            if (!string.IsNullOrWhiteSpace(receivedRaw) &&
                DateTimeOffset.TryParse(receivedRaw, out var parsedTimestamp))
            {
                return parsedTimestamp.UtcDateTime;
            }

            return DateTime.UtcNow;
        }

        private static string ExtractBody(JsonElement messageElement)
        {
            if (!messageElement.TryGetProperty("body", out var bodyElement))
                return string.Empty;

            var contentType = GetStringProperty(bodyElement, "contentType", "text");
            var rawContent = GetStringProperty(bodyElement, "content");
            var isHtmlBody = string.Equals(contentType, "html", StringComparison.OrdinalIgnoreCase);

            if (isHtmlBody)
                return EmailBodyCleaner.CleanHtml(rawContent);

            return EmailBodyCleaner.CleanPlainText(rawContent);
        }

        private static string GetStringProperty(JsonElement parent, string propertyName, string fallback = "")
        {
            if (!parent.TryGetProperty(propertyName, out var propertyValue))
                return fallback;

            return propertyValue.GetString() ?? fallback;
        }

        private static string? GetNullableString(JsonElement parent, string propertyName)
        {
            if (!parent.TryGetProperty(propertyName, out var propertyValue))
                return null;

            return propertyValue.GetString();
        }
    }
}