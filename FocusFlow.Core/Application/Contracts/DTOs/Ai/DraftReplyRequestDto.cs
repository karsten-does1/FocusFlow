namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record DraftReplyRequestDto(
        string Subject,
        string Body,
        string? Sender = null,
        string? ReceivedAtUtc = null,
        string? ThreadHint = null,
        string Tone = "Neutral",
        string Length = "Medium",
        string? Language = null
    );
}