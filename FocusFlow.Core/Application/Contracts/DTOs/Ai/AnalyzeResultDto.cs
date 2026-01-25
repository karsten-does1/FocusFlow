namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record AnalyzeResultDto(
        string Summary,
        int Priority,
        string Category,
        string Action
    );
}
