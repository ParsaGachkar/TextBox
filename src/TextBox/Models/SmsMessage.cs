namespace TextBox.Models;

/// <summary>
/// Persisted SMS. Mutable record with a parameterless constructor so LiteDB
/// can materialize it; value equality is preserved.
/// </summary>
public record SmsMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string To { get; set; } = string.Empty;
    public string? From { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public record SendSmsRequest(string To, string Body, string? From);
