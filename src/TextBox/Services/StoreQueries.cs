using TextBox.Models;

namespace TextBox.Services;

internal static class StoreQueries
{
    public static List<SmsMessage> SortedFiltered(this IEnumerable<SmsMessage> source, string? toFilter)
    {
        IEnumerable<SmsMessage> query = source.OrderByDescending(m => m.CreatedAt);
        if (!string.IsNullOrWhiteSpace(toFilter))
            query = query.Where(m => m.To.Contains(toFilter, StringComparison.OrdinalIgnoreCase));
        return query.ToList();
    }
}