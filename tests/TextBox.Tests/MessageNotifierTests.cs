using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using TextBox.Hubs;
using TextBox.Models;

namespace TextBox.Tests;

public class MessageNotifierTests
{
    private readonly IHubClients _clients = Substitute.For<IHubClients>();
    private readonly ISingleClientProxy _all = Substitute.For<ISingleClientProxy>();
    private readonly MessageNotifier _notifier;

    public MessageNotifierTests()
    {
        _clients.All.Returns(_all);
        var hub = Substitute.For<IHubContext<MessageHub>>();
        hub.Clients.Returns(_clients);
        _notifier = new MessageNotifier(hub);
    }

    [Fact]
    public async Task NotifyReceivedAsync_BroadcastsMessage()
    {
        var message = new SmsMessage { To = "+123", Body = "hi" };

        await _notifier.NotifyReceivedAsync(message);

        await _all.Received(1).SendCoreAsync(
            MessageHubEvents.MessageReceived,
            Arg.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], message)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyClearedAsync_BroadcastsCleared()
    {
        await _notifier.NotifyClearedAsync();

        await _all.Received(1).SendCoreAsync(
            MessageHubEvents.MessagesCleared,
            Arg.Is<object?[]>(args => args.Length == 0),
            Arg.Any<CancellationToken>());
    }
}
