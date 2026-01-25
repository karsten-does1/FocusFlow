using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FocusFlow.Core.Application.Contracts.DTOs.Ai;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.App.Services
{
    public sealed class AiApi : IAiService
    {
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public AiApi(HttpClient http)
        {
            _http = http;
        }

        public async Task<AnalyzeResultDto> AnalyzeEmailAsync(AnalyzeRequestDto request,CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/ai/email/analyze",request,JsonOptions,ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnalyzeResponseDto>(JsonOptions,cancellationToken: ct);

            if (result is null)
            {
                return new AnalyzeResultDto(Summary: "",Priority: 0,Category: "Overig",Action: "Lezen");
            }

            return new AnalyzeResultDto(Summary: result.Summary,Priority: result.PriorityScore,Category: result.Category,Action: result.SuggestedAction);
        }

        public async Task<DraftReplyResponseDto> DraftReplyAsync(
            DraftReplyRequestDto request,
            CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/ai/email/draft-reply",request,JsonOptions,ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DraftReplyResponseDto>(JsonOptions,cancellationToken: ct);

            if (result is null)
            {
                return new DraftReplyResponseDto("");
            }

            return result;
        }

        public async Task<ComposeEmailResponseDto> ComposeEmailAsync(
            ComposeEmailRequestDto request,
            CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/ai/email/compose",request,JsonOptions,ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ComposeEmailResponseDto>(JsonOptions,cancellationToken: ct);

            if (result is null)
            {
                return new ComposeEmailResponseDto(Subject: request.Subject ?? "",Body: "");
            }

            return result;
        }

        public async Task<ExtractTasksResponseDto> ExtractTasksAsync(
            ExtractTasksRequestDto request,
            CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/ai/email/extract-tasks",request,JsonOptions,ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExtractTasksResponseDto>(JsonOptions,cancellationToken: ct);

            if (result is null)
            {
                return new ExtractTasksResponseDto(new(),new());
            }

            return result;
        }
    }
}
