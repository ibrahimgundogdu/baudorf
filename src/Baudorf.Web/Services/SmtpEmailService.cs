using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Baudorf.Web.Services;

/// <summary>SMTP-Konfiguration (appsettings / Umgebungsvariablen — NICHT im Quellcode).</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;          // All-Inkl/KAS: 587 (STARTTLS)
    public string? User { get; set; }             // leer → From wird als Login genutzt
    public string? Password { get; set; }
    public string From { get; set; } = "web@baudorf.de";
    public string FromName { get; set; } = "Baudorf Immobilien";
    public bool EnableSsl { get; set; } = true;   // STARTTLS auf Port 587

    /// <summary>Empfänger interner Benachrichtigungen (Kontaktformular, Leads, Widerruf).
    /// Absender bleibt <see cref="From"/> (web@…), Benachrichtigungen gehen aber hierhin.</summary>
    public string NotifyTo { get; set; } = "andrea.krueger@baudorf.de";

    /// <summary>True, sobald ein SMTP-Host hinterlegt ist — sonst wird nicht real versendet.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>Produktions-Versand über SMTP (System.Net.Mail).</summary>
public class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _o = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!_o.IsConfigured)
        {
            logger.LogWarning("[MAIL] SMTP nicht konfiguriert — E-Mail an {To} NICHT gesendet: {Subject}", to, subject);
            return;
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_o.From, _o.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(to);

        using var client = new SmtpClient(_o.Host!, _o.Port)
        {
            EnableSsl = _o.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(
                string.IsNullOrWhiteSpace(_o.User) ? _o.From : _o.User, _o.Password)
        };

        await client.SendMailAsync(msg, ct);
        logger.LogInformation("[MAIL] gesendet an {To}: {Subject}", to, subject);
    }
}
