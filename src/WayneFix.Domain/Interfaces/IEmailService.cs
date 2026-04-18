namespace WayneFix.Domain.Interfaces;

public interface IEmailService
{
    Task SendEmail(string title, string from, List<string> to, string body);
}
