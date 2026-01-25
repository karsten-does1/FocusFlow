using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using FocusFlow.Core.Application.Contracts.DTOs.Ai;
using FocusFlow.Core.Application.Contracts.Services;

using Microsoft.Extensions.Logging;

namespace FocusFlow.Infrastructure.Services
{
    public class PythonAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PythonAiService> _logger;

        public PythonAiService(HttpClient httpClient, ILogger<PythonAiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<AnalyzeResultDto> AnalyzeEmailAsync(
            AnalyzeRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("email/analyze", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await TryReadErrorAsync(response, ct);

                    _logger.LogWarning(
                        "AI analyze failed. Status={StatusCode}. Body={Body}",
                        (int)response.StatusCode,
                        error ?? "<no body>");

                    return new AnalyzeResultDto(
                        Summary: "AI niet beschikbaar. Probeer later opnieuw.",
                        Priority: 0,
                        Category: "Onbekend",
                        Action: "Lezen"
                    );
                }

                var result = await response.Content.ReadFromJsonAsync<AiAnalyzeResponse>(cancellationToken: ct);
                if (result is null)
                {
                    _logger.LogWarning("AI analyze returned empty/invalid JSON.");

                    return new AnalyzeResultDto(
                        Summary: "AI gaf geen geldige response.",
                        Priority: 0,
                        Category: "Onbekend",
                        Action: "Lezen"
                    );
                }

                return new AnalyzeResultDto(
                    Summary: result.Summary ?? "",
                    Priority: result.PriorityScore,
                    Category: result.Category ?? "Onbekend",
                    Action: result.SuggestedAction ?? "Lezen"
                );
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("AI analyze cancelled.");

                return new AnalyzeResultDto(
                    Summary: "",
                    Priority: 0,
                    Category: "Onbekend",
                    Action: "Lezen"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analyze exception.");

                return new AnalyzeResultDto(
                    Summary: "AI fout tijdens analyse. Probeer later opnieuw.",
                    Priority: 0,
                    Category: "Onbekend",
                    Action: "Lezen"
                );
            }
        }

        public async Task<DraftReplyResponseDto> DraftReplyAsync(
            DraftReplyRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("email/draft-reply", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await TryReadErrorAsync(response, ct);

                    _logger.LogWarning(
                        "AI draft-reply failed. Status={StatusCode}. Body={Body}",
                        (int)response.StatusCode,
                        error ?? "<no body>");

                    return new DraftReplyResponseDto("AI niet beschikbaar om een conceptantwoord te genereren. Probeer later opnieuw.");
                }

                var result = await response.Content.ReadFromJsonAsync<DraftReplyResponseDto>(cancellationToken: ct);
                return result ?? new DraftReplyResponseDto("");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("AI draft-reply cancelled.");
                return new DraftReplyResponseDto("");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI draft-reply exception.");
                return new DraftReplyResponseDto("AI fout tijdens het genereren van het conceptantwoord. Probeer later opnieuw.");
            }
        }

        public async Task<ComposeEmailResponseDto> ComposeEmailAsync(
            ComposeEmailRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("email/compose", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await TryReadErrorAsync(response, ct);

                    _logger.LogWarning(
                        "AI compose failed. Status={StatusCode}. Body={Body}",
                        (int)response.StatusCode,
                        error ?? "<no body>");

                    return new ComposeEmailResponseDto(
                        Subject: request.Subject ?? "",
                        Body: "AI niet beschikbaar om een e-mail te genereren. Probeer later opnieuw."
                    );
                }

                var result = await response.Content.ReadFromJsonAsync<ComposeEmailResponseDto>(cancellationToken: ct);
                return result ?? new ComposeEmailResponseDto(Subject: request.Subject ?? "", Body: "");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("AI compose cancelled.");
                return new ComposeEmailResponseDto(Subject: request.Subject ?? "", Body: "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI compose exception.");
                return new ComposeEmailResponseDto(
                    Subject: request.Subject ?? "",
                    Body: "AI fout tijdens het genereren van de e-mail. Probeer later opnieuw."
                );
            }
        }

        public async Task<ExtractTasksResponseDto> ExtractTasksAsync(
            ExtractTasksRequestDto request,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("email/extract-tasks", request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await TryReadErrorAsync(response, ct);

                    _logger.LogWarning(
                        "AI extract-tasks failed. Status={StatusCode}. Body={Body}",
                        (int)response.StatusCode,
                        error ?? "<no body>");

                    return new ExtractTasksResponseDto(
                        Tasks: new List<TaskProposalDto>(),
                        NeedsClarification: new List<string> { "AI niet beschikbaar om taken te extraheren. Probeer later opnieuw." }
                    );
                }

                var result = await response.Content.ReadFromJsonAsync<ExtractTasksResponseDto>(cancellationToken: ct);
                return result ?? new ExtractTasksResponseDto(new List<TaskProposalDto>(), new List<string>());
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("AI extract-tasks cancelled.");
                return new ExtractTasksResponseDto(new List<TaskProposalDto>(), new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI extract-tasks exception.");
                return new ExtractTasksResponseDto(
                    Tasks: new List<TaskProposalDto>(),
                    NeedsClarification: new List<string> { "AI fout tijdens taakextractie. Probeer later opnieuw." }
                );
            }
        }

        private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var text = await response.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(text)) return null;

                const int maxLen = 800;
                return text.Length <= maxLen ? text : text.Substring(0, maxLen) + "…";
            }
            catch
            {
                return null;
            }
        }

        private sealed class AiAnalyzeResponse
        {
            [JsonPropertyName("summary")] public string? Summary { get; set; }
            [JsonPropertyName("priorityScore")] public int PriorityScore { get; set; }
            [JsonPropertyName("category")] public string? Category { get; set; }
            [JsonPropertyName("suggestedAction")] public string? SuggestedAction { get; set; }
        }
    }
}
