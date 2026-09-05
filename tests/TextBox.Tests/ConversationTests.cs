using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TextBox.Components.Pages;
using TextBox.Models;
using TextBox.Services;

namespace TextBox.Tests;

public class ConversationTests : BunitContext
{
    private readonly IMessageStore _store = Substitute.For<IMessageStore>();

    public ConversationTests()
    {
        Services.AddSingleton(_store);
        Services.AddSingleton<MessageLiveFeed>(sp => new MessageLiveFeed(sp.GetRequiredService<NavigationManager>()));
    }

    private MessageLiveFeed Feed => Services.GetRequiredService<MessageLiveFeed>();

    private static SmsMessage Msg(string to, string body, DateTimeOffset? at = null, string? from = null) =>
        new() { Id = Guid.NewGuid(), To = to, From = from, Body = body, CreatedAt = at ?? DateTimeOffset.UtcNow };

    private void GivenMessages(params List<SmsMessage> messages) =>
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(messages);

    private IRenderedComponent<Conversation> RenderNumber(string number) =>
        Render<Conversation>(p => p.Add(x => x.PhoneNumber, number));

    [Fact]
    public void RendersOnlyMatchingBubbles_OldestFirst()
    {
        var t = DateTimeOffset.UtcNow;
        GivenMessages([
            Msg("+123", "second", t.AddMinutes(1), "Alice"),
            Msg("+456", "wrong-number", t),
            Msg("+123", "first", t, "Bob"),
        ]);

        var cut = RenderNumber("+123");
        var bubbles = cut.FindAll(".chat-bubble");

        Assert.Equal(2, bubbles.Count);
        Assert.Equal("first", bubbles[0].TextContent);
        Assert.Equal("second", bubbles[1].TextContent);
        Assert.DoesNotContain("wrong-number", cut.Markup);
    }

    [Fact]
    public void RendersEmptyState_WhenNoMessagesForNumber()
    {
        GivenMessages([Msg("+456", "hi", DateTimeOffset.UtcNow)]);

        var cut = RenderNumber("+123");

        Assert.Contains("No messages for +123 yet.", cut.Markup);
        Assert.Empty(cut.FindAll(".chat-bubble"));
    }

    [Fact]
    public void ClickBubble_OpensDetailsModal_AndCloseHidesIt()
    {
        var message = Msg("+123", "hello", DateTimeOffset.UtcNow, "Alice");
        GivenMessages([message]);

        var cut = RenderNumber("+123");
        Assert.Empty(cut.FindAll(".modal-open"));

        cut.Find(".chat-bubble").Click();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".modal-open")));
        var modal = cut.Find(".modal-box");
        Assert.Contains("Message details", modal.TextContent);
        Assert.Contains(message.Id.ToString(), modal.TextContent);
        Assert.Contains("hello", modal.TextContent);

        cut.Find(".modal-action button").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".modal-open")));
    }

    [Fact]
    public async Task LiveMessage_AppearsAsBubble()
    {
        var backing = new List<SmsMessage>();
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => (IReadOnlyList<SmsMessage>)backing.ToList());
        var cut = RenderNumber("+123");
        Assert.Contains("No messages for +123 yet.", cut.Markup);

        backing.Add(Msg("+123", "live bubble"));
        await Feed.ReceiveAsync(backing[0]);

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".chat-bubble")));
    }

    [Fact]
    public async Task ClearedConversation_NavigatesHome()
    {
        var backing = new List<SmsMessage> { Msg("+123", "hi") };
        _store.ListAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => (IReadOnlyList<SmsMessage>)backing.ToList());
        var cut = RenderNumber("+123");
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        Assert.Single(cut.FindAll(".chat-bubble"));

        backing.Clear();
        await Feed.ClearedAsync();

        cut.WaitForAssertion(() => Assert.Equal(nav.BaseUri, nav.Uri));
    }
}
