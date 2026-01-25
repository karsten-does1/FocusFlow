namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record ComposeEmailRequestDto(
        string Prompt,
        string? Subject = null,
        string? Instructions = null,
        string Tone = "Neutral",
        string Length = "Medium",
        string? Language = null,
        string? ReplyToSubject = null,
        string? ReplyToBody = null,
        string? ReplyToSender = null,
        string? ReplyToReceivedAtUtc = null
    );
}
