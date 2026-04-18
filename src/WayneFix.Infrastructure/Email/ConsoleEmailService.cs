using WayneFix.Domain.Interfaces;

namespace WayneFix.Infrastructure.Email;

public class ConsoleEmailService : IEmailService
{
    public Task SendEmail(string title, string from, List<string> to, string body)
    {
        Console.WriteLine($"[EMAIL] To: {string.Join(", ", to)} | Subject: {title} | Body: {body}");
        return Task.CompletedTask;
    }
}
