using System.Net.Http.Json;
using System.Text.Json;
using AgendaInteligente.Api.Domain.Exceptions;
using AgendaInteligente.Api.Models.AI;
using AgendaInteligente.Api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgendaInteligente.Api.Services;

public sealed class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeminiIntentResponse> ExtractIntentAsync(
        string systemPrompt, 
        string userMessage, 
        List<MessageHistory> history, 
        string apiKey, 
        string model)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new GeminiIntegrationException("API Key do Gemini não foi fornecida no contexto (Tenant ou Global).");

        var url = $"{GeminiApiBaseUrl}/{model}:generateContent?key={apiKey}";

        var contents = new List<object>();

        // Add history
        foreach (var msg in history)
        {
            contents.Add(new
            {
                role = msg.Role,
                parts = new[] { new { text = msg.Content } }
            });
        }

        // Add current user message
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userMessage } }
        });

        // Monta o payload conforme a documentação da API do Gemini
        var payload = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = systemPrompt } }
            },
            contents = contents,
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        intent = new { type = "string" },
                        date = new { type = "string", nullable = true },
                        time = new { type = "string", nullable = true },
                        service = new { type = "string", nullable = true },
                        professional = new { type = "string", nullable = true },
                        reply_message = new { type = "string" }
                    },
                    required = new[] { "intent", "reply_message" }
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erro ao chamar API do Gemini. Status: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
                throw new GeminiIntegrationException($"Erro na comunicação com Gemini: HTTP {(int)response.StatusCode}");
            }

            var resultDocument = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var textResult = resultDocument?
                .RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(textResult))
                throw new GeminiIntegrationException("O modelo não retornou um texto válido.");

            var intentResponse = JsonSerializer.Deserialize<GeminiIntentResponse>(textResult);
            if (intentResponse == null)
                throw new GeminiIntegrationException("Não foi possível desserializar a resposta do modelo para o formato esperado.");

            return intentResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Exceção de rede ao chamar o Gemini.");
            throw new GeminiIntegrationException("Falha de rede ao se conectar ao Gemini.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro de JSON ao desserializar retorno do Gemini.");
            throw new GeminiIntegrationException("Falha ao analisar a resposta do modelo.", ex);
        }
    }
}
