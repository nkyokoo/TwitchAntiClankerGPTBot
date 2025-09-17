using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using log4net;

/// <summary>
/// Interface for chat moderation functionality.
/// </summary>
public interface IChatModerator
{
    /// <summary>
    /// Classifies a chat message for moderation purposes.
    /// </summary>
    /// <param name="username">The username of the message sender.</param>
    /// <param name="message">The chat message to classify.</param>
    /// <returns>A classification result string (e.g., "spam", "ok").</returns>
    Task<string> ClassifyMessage(string username, string message);
}

/// <summary>
/// Implements chat moderation using OpenAI GPT and logs events via log4net.
/// </summary>
public class ChatModerator : IChatModerator
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ChatModerator));
    private readonly HttpClient httpClient;
    private readonly string ruleset;
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4o-mini";

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatModerator"/> class.
    /// Loads the moderation ruleset and configures the OpenAI API client.
    /// </summary>
    /// <param name="apiKey">OpenAI API key for authentication.</param>
    /// <exception cref="ArgumentException">Thrown if the API key is missing or empty.</exception>
    public ChatModerator(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            log.Error("API key is missing or empty.");
            throw new ArgumentException("API key is required.", nameof(apiKey));
        }

        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Load ruleset (copied to output by the .csproj)
        if (File.Exists("ruleset.txt"))
        {
            ruleset = File.ReadAllText("ruleset.txt");
            log.Info("Loaded moderation ruleset.");
        }
        else
        {
            ruleset = string.Empty;
            log.Warn("ruleset.txt not found. Using empty ruleset.");
        }
    }

    /// <summary>
    /// Classifies a chat message using the OpenAI GPT API and logs the process.
    /// </summary>
    /// <param name="username">The username of the message sender.</param>
    /// <param name="message">The chat message to classify.</param>
    /// <returns>
    /// A string representing the classification result (e.g., "spam", "ok").
    /// Returns an empty string if the message is empty or classification fails.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the OpenAI API request fails.</exception>
    public async Task<string> ClassifyMessage(string username, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            log.Debug("Received empty message for classification.");
            return string.Empty;
        }

        string input = $"Classify this input:\nUsername: {username}\nMessage: {message}";
        log.Info($"Classifying message from '{username}': {message}");

        var payload = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "system", content = ruleset },
                new { role = "user", content = input }
            },
            max_tokens = 64,
            temperature = 0.0
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var resp = await httpClient.PostAsync(Endpoint, content).ConfigureAwait(false);
            var respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                log.Error($"OpenAI request failed ({(int)resp.StatusCode}): {respBody}");
                throw new InvalidOperationException($"OpenAI request failed ({(int)resp.StatusCode}): {respBody}");
            }

            using var doc = JsonDocument.Parse(respBody);
            if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var messageElem) &&
                    messageElem.TryGetProperty("content", out var contentElem))
                {
                    string result = contentElem.GetString()?.Trim().ToLower() ?? string.Empty;
                    log.Info($"Classification result for '{username}': {result}");
                    return result;
                }
            }

            log.Warn("No valid classification result found in OpenAI response.");
            return string.Empty;
        }
        catch (Exception ex)
        {
            log.Error("Exception during message classification.", ex);
            throw;
        }
    }
}
