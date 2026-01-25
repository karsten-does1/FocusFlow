namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record AnalyzeResponseDto(
        string Summary,
        int PriorityScore,
        string Category,
        string SuggestedAction
    );
}
