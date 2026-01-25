namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record ExtractTasksResponseDto(
        List<TaskProposalDto> Tasks,
        List<string> NeedsClarification
    );
}