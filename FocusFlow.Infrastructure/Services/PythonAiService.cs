using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FocusFlow.Core.Application.Contracts.Services;

namespace FocusFlow.Infrastructure.Services
{
    public class PythonAiService : IAiService
    {
        private readonly HttpClient _httpClient;

        public PythonAiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string Summary, int Priority, string Category, string Action)> AnalyzeEmailAsync(string subject, string body)
        {
            try
            {   
                // 1. Het request object 
                var request = new { subject, body };

                // 2. Python aanroepen (Zorg dat je Python script draait!)
                var response = await _httpClient.PostAsJsonAsync(
                    "http://127.0.0.1:8000/email/analyze",
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    return ("Kan AI niet bereiken", 50, "Fout", "Check Console");
                }

                // 3. Antwoord lezen
                var result = await response.Content.ReadFromJsonAsync<AiResponse>();
                if (result == null)
                    return ("Geen data", 50, "Onbekend", "Lezen");

                return (result.Summary, result.PriorityScore, result.Category, result.SuggestedAction);
            }
            catch (Exception ex)
            {
                return ($"AI Fout: {ex.Message}", 50, "Error", "Retry");
            }
        }

        // Hulpklasse voor het lezen van de JSON
        private class AiResponse
        {
            [JsonPropertyName("summary")] public string Summary { get; set; } = "";
            [JsonPropertyName("priorityScore")] public int PriorityScore { get; set; }
            [JsonPropertyName("category")] public string Category { get; set; } = "";
            [JsonPropertyName("suggestedAction")] public string SuggestedAction { get; set; } = "";
        }
    }
}
