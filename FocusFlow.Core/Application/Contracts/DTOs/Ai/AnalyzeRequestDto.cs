namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record AnalyzeRequestDto(
        string Subject,
        string Body
    );
}
