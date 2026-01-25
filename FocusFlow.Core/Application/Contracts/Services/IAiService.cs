using System.Threading;
using System.Threading.Tasks;
using FocusFlow.Core.Application.Contracts.DTOs.Ai;

namespace FocusFlow.Core.Application.Contracts.Services
{
    public interface IAiService
    {
        Task<AnalyzeResultDto> AnalyzeEmailAsync(AnalyzeRequestDto request, CancellationToken ct = default);
        Task<DraftReplyResponseDto> DraftReplyAsync(DraftReplyRequestDto request, CancellationToken ct = default);
        Task<ComposeEmailResponseDto> ComposeEmailAsync(ComposeEmailRequestDto request, CancellationToken ct = default);
        Task<ExtractTasksResponseDto> ExtractTasksAsync(ExtractTasksRequestDto request, CancellationToken ct = default);

    }
}