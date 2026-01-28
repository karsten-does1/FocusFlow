using System.Text.Json.Serialization;

namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record AnalyzeRequestDto(
     [property: JsonPropertyName("subject")] string Subject,
     [property: JsonPropertyName("body")] string Body
 );
}
