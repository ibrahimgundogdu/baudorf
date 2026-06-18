namespace Baudorf.Web.Services;

/// <summary>
/// Uygulama e-postaları (lead bildirimi, iletişim formu) için soyutlama.
/// Identity'nin kendi IEmailSender'ından ayrı; SMTP/Resend impl'i buraya bağlanır.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Geliştirme implementasyonu: e-postayı log'a yazar (gerçek gönderim yok).</summary>
public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        logger.LogInformation("[DEV-MAIL] To={To} | Subject={Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
