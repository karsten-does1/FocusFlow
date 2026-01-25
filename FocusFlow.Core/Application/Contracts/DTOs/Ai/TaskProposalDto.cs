namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record TaskProposalDto(
        string Title,
        string Description = "",
        string Priority = "Medium",
        string? DueDate = null,
        string? DueText = null,
        double Confidence = 0.7,
        string? SourceQuote = null
    );
}