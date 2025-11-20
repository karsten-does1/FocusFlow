using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Application.Utilities;
using FocusFlow.Core.Domain.Entities;
using FocusFlow.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FocusFlow.Infrastructure.Services.Gmail
{
    public sealed class GmailMessageParser : IEmailMessageParser<JsonElement>
    {
        public Email? ParseMessage(JsonElement messageData, string messageId, Guid emailAccountId)
        {
            try
            {
                return ParseGmailMessage(messageData, messageId, emailAccountId);
            }
            catch
            {
                return null;
            }
        }

        private static Email? ParseGmailMessage(JsonElement messageRoot, string messageId, Guid emailAccountId)
        {
            if (!messageRoot.TryGetProperty("payload", out var payload))
                return null;

            var headers = ExtractHeaders(payload);

            headers.TryGetValue("From", out var from);
            headers.TryGetValue("Subject", out var subject);
            headers.TryGetValue("Date", out var dateRaw);

            var receivedUtc = GmailDateParser.Parse(messageRoot, dateRaw);

            var bodyText = ExtractBodyText(payload);

            return new Email(
                from ?? string.Empty,
                subject ?? string.Empty,
                bodyText,
                receivedUtc,
                EmailProvider.Gmail,
                messageId,
                emailAccountId
            );
        }

        private static Dictionary<string, string> ExtractHeaders(JsonElement payload)
        {
            var headersDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!payload.TryGetProperty("headers", out var headersElement))
                return headersDictionary;

            foreach (var header in headersElement.EnumerateArray())
            {
                if (!header.TryGetProperty("name", out var nameElement) ||
                    !header.TryGetProperty("value", out var valueElement))
                    continue;

                var headerName = nameElement.GetString();
                var headerValue = valueElement.GetString();

                if (!string.IsNullOrWhiteSpace(headerName) && headerValue != null)
                    headersDictionary[headerName] = headerValue;
            }

            return headersDictionary;
        }

        private static string ExtractBodyText(JsonElement payload)
        {
            var htmlText = ExtractBodyByMimeType(payload, "text/html");
            if (!string.IsNullOrWhiteSpace(htmlText))
                return EmailBodyCleaner.CleanHtml(htmlText);

            var plainText = ExtractBodyByMimeType(payload, "text/plain");
            if (!string.IsNullOrWhiteSpace(plainText))
                return EmailBodyCleaner.CleanPlainText(plainText);

            if (payload.TryGetProperty("body", out var bodyElement) &&
                bodyElement.TryGetProperty("data", out var dataElement))
            {
                var decoded = Base64UrlDecoder.Decode(dataElement.GetString() ?? "");
                if (!string.IsNullOrWhiteSpace(decoded))
                    return EmailBodyCleaner.CleanPlainText(decoded);
            }

            return string.Empty;
        }

        private static string ExtractBodyByMimeType(JsonElement payload, string mimeTypePrefix)
        {
            bool Matches(JsonElement messagePart) =>
                messagePart.TryGetProperty("mimeType", out var mimeTypeElement) &&
                mimeTypeElement.GetString()?.StartsWith(mimeTypePrefix, StringComparison.OrdinalIgnoreCase) == true;

            string? Decode(JsonElement messagePart)
            {
                if (!messagePart.TryGetProperty("body", out var bodyElement) ||
                    !bodyElement.TryGetProperty("data", out var dataElement))
                    return null;

                var decoded = Base64UrlDecoder.Decode(dataElement.GetString() ?? "");
                return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
            }

            if (Matches(payload))
            {
                var decoded = Decode(payload);
                if (!string.IsNullOrWhiteSpace(decoded))
                    return decoded!;
            }

            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var messagePart in parts.EnumerateArray())
                {
                    var bodyText = ExtractBodyByMimeType(messagePart, mimeTypePrefix);
                    if (!string.IsNullOrWhiteSpace(bodyText))
                        return bodyText;
                }
            }

            return string.Empty;
        }
    }
}
