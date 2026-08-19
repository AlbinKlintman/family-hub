namespace WebApp.Services;

public class EmailSenderOptions
{
    public required string SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 465;
    public required string SenderAddress { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}
