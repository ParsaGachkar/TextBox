using TextBox.Models;
using TextBox.Services;

namespace TextBox.Tests;

public class StoreQueriesTests
{
    private static SmsMessage Msg(string to, string body, DateTimeOffset createdAt) =>
        new() { Id = Guid.NewGuid(), To = to, Body = body, CreatedAt = createdAt };

    [Fact]
    public void SortedFiltered_OrdersNewestFirst()
    {
        var older = Msg("+1", "old", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var newer = Msg("+1", "new", new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

        var result = new[] { older, newer }.SortedFiltered(null);

        Assert.Equal([newer, older], result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SortedFiltered_BlankFilter_ReturnsAll(string? filter)
    {
        var messages = new[] { Msg("+1", "a", DateTimeOffset.UtcNow), Msg("+2", "b", DateTimeOffset.UtcNow) };

        Assert.Equal(2, messages.SortedFiltered(filter).Count);
    }

    [Fact]
    public void SortedFiltered_FiltersByRecipientSubstring_CaseInsensitive()
    {
        var match = Msg("+123", "hi", DateTimeOffset.UtcNow);
        var other = Msg("+456", "hi", DateTimeOffset.UtcNow);

        var result = new[] { match, other }.SortedFiltered("+12");

        Assert.Equal([match], result);
    }

    [Fact]
    public void SortedFiltered_NoMatch_ReturnsEmpty()
    {
        var messages = new[] { Msg("+123", "hi", DateTimeOffset.UtcNow) };

        Assert.Empty(messages.SortedFiltered("+999"));
    }
}
