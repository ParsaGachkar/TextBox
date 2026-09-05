using TextBox.Models;

namespace TextBox.Services;

public interface IMessageStore
{
    Task<SmsMessage> AddAsync(string to, string? from, string body, CancellationToken ct = default);
    Task<IReadOnlyList<SmsMessage>> ListAsync(string? toFilter = null, CancellationToken ct = default);
    Task<SmsMessage?> GetAsync(Guid id, CancellationToken ct = default);
    Task<int> ClearAsync(CancellationToken ct = default);
}