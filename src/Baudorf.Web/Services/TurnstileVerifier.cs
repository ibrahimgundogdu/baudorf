using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Baudorf.Web.Models;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Services;

/// <summary>Server-seitige Verifizierung des Cloudflare-Turnstile-Tokens.</summary>
public interface ITurnstileVerifier
{
    bool Enabled { get; }
    Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
}

public class TurnstileVerifier(
    HttpClient http,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileVerifier> logger) : ITurnstileVerifier
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public bool Enabled => options.Value.Enabled;

    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
    {
        var opt = options.Value;
        if (!opt.Enabled) return true; // Keine Keys → Prüfung übersprungen.
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var fields = new Dictionary<string, string>
            {
                ["secret"] = opt.SecretKey,
                ["response"] = token
            };
            if (!string.IsNullOrWhiteSpace(remoteIp)) fields["remoteip"] = remoteIp;

            using var resp = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(fields), ct);
            var result = await resp.Content.ReadFromJsonAsync<TurnstileResult>(cancellationToken: ct);
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Turnstile-Verifizierung fehlgeschlagen.");
            return false;
        }
    }

    private sealed class TurnstileResult
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
    }
}
