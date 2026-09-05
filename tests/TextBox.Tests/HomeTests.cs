using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TextBox.Components.Pages;
using TextBox.Models;
using TextBox.Services;

namespace TextBox.Tests;

public class HomeTests : BunitContext
{
    private readonly IMessageStore _store = Substitute.For<IMessageStore>();

    public HomeTests()
    {
        Services.AddSingleton(_store);
        Services.AddSingleton<MessageLiveFeed>(sp => new MessageLiveFeed(sp.GetRequiredService<NavigationManager>()));
    }

    private MessageLiveFeed Feed => Services.GetRequiredService<MessageLiveFeed>();

    private static SmsMessage Msg(string to, string body, DateTimeOffset? at = null) =>
        new() { Id = Guid.NewGuid(), To = to, Body = body, CreatedAt = at ?? DateTimeOffset.UtcNow };

    private void GivenMessages(params List<SmsMessage> messages) =>
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(messages);

    [Fact]
    public void RendersEmptyState_WhenNoMessages()
    {
        GivenMessages([]);

        var cut = Render<Home>();

        Assert.Contains("No messages yet", cut.Markup);
        Assert.Empty(cut.FindAll("li.list-row"));
    }

    [Fact]
    public void GroupsByRecipient_ShowingLastMessage()
    {
        var t = DateTimeOffset.UtcNow;
        GivenMessages([
            Msg("+123", "old", t.AddMinutes(-2)),
            Msg("+123", "newest", t),
            Msg("+456", "other", t.AddMinutes(-1)),
        ]);

        var cut = Render<Home>();
        var rows = cut.FindAll("li.list-row");

        Assert.Equal(2, rows.Count);
        Assert.Contains("+123", rows[0].TextContent);
        Assert.Contains("newest", rows[0].TextContent);
        Assert.DoesNotContain("old", rows[0].TextContent);
        Assert.Contains("+456", rows[1].TextContent);
    }

    [Fact]
    public async Task Search_FiltersConversations()
    {
        GivenMessages([Msg("+123", "hi", DateTimeOffset.UtcNow), Msg("+456", "yo", DateTimeOffset.UtcNow)]);
        var cut = Render<Home>();
        Assert.Equal(2, cut.FindAll("li.list-row").Count);

        await cut.InvokeAsync(() => cut.Find("input").Input("+123"));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("li.list-row")));
        Assert.Contains("+123", cut.Find("li.list-row").TextContent);
    }

    [Fact]
    public void RowClick_NavigatesToConversation()
    {
        GivenMessages([Msg("+123", "hi", DateTimeOffset.UtcNow)]);
        var cut = Render<Home>();
        var nav = Services.GetRequiredService<BunitNavigationManager>();

        cut.Find("li.list-row").Click();

        Assert.EndsWith("/conversation/%2B123", nav.Uri);
    }

    [Fact]
    public void HidesApiKey_WhenNoneConfigured()
    {
        GivenMessages([]);

        var cut = Render<Home>();

        Assert.DoesNotContain("API key", cut.Markup);
    }

    [Fact]
    public async Task LiveMessage_AppearsInList()
    {
        var backing = new List<SmsMessage>();
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => (IReadOnlyList<SmsMessage>)backing.ToList());
        var cut = Render<Home>();
        Assert.Contains("No messages yet", cut.Markup);

        backing.Add(Msg("+123", "live hello"));
        await Feed.ReceiveAsync(backing[0]);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("li.list-row")));
        Assert.Contains("live hello", cut.Find("li.list-row").TextContent);
    }

    [Fact]
    public void Clear_EmptiesInbox()
    {
        GivenMessages([Msg("+123", "hi", DateTimeOffset.UtcNow)]);
        _store.ClearAsync(Arg.Any<CancellationToken>()).Returns(1);
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns([]);
        var cut = Render<Home>();

        cut.FindAll("button").Single(b => b.TextContent.Contains("Clear")).Click();

        cut.WaitForAssertion(() => Assert.Contains("No messages yet", cut.Markup));
    }
}
