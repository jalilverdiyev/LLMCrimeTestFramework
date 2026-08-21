using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CrimeTestValidator.Configs;

namespace CrimeTestValidator.Inference;

public sealed class InferenceClient : IInferenceClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    private const int MaxAttempts = 3;

    private readonly ExperimentConfig _config;

    public InferenceClient(ExperimentConfig config) => _config = config;

    public async Task<InferenceResult> AskAsync(InferenceRequest request, CancellationToken ct)
    {
        var payload = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user",   content = request.UserPrompt }
            },
            stream = false,
            options = new
            {
                temperature = 0.0,   // without this a rerun is a different experiment
                seed = 42,
                top_p = 1.0,
                num_ctx = 8192,      // Ollama truncates past num_ctx silently, dropping the scenario
                num_predict = 512
            },
            keep_alive = "30m"       // stops the model unloading between calls
        };

        var json = JsonSerializer.Serialize(payload);
        var sw = Stopwatch.StartNew();
        string? lastError = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await Http.PostAsync(_config.ApiUrl, content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = $"HTTP {(int)response.StatusCode}: {Truncate(body, 300)}";
                    if (attempt < MaxAttempts) { await Task.Delay(1000 * attempt, ct); continue; }
                    break;
                }

                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.TryGetProperty("message", out var message)
                           && message.TryGetProperty("content", out var c)
                    ? c.GetString() ?? string.Empty
                    : string.Empty;

                sw.Stop();
                return new InferenceResult(text, true, null, (int)sw.ElapsedMilliseconds, attempt);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                if (attempt < MaxAttempts) await Task.Delay(1000 * attempt, ct);
            }
        }

        sw.Stop();
        return InferenceResult.Failed(lastError ?? "unknown error", (int)sw.ElapsedMilliseconds, MaxAttempts);
    }

    private static string Truncate(string s, int n) =>
        string.IsNullOrEmpty(s) || s.Length <= n ? s : s[..n];
}
