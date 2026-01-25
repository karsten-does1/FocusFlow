namespace FocusFlow.Core.Application.Contracts.DTOs.Ai
{
    public sealed record ComposeEmailResponseDto(
        string Subject,
        string Body
    );
}
