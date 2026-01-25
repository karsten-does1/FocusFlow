namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record ExtractTasksRequestDto(
        string Subject,
        string Body,
        string? Sender = null,
        string? ReceivedAtUtc = null, 
        string? ThreadHint = null
    );
}