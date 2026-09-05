using Microsoft.AspNetCore.SignalR;
using TextBox.Models;

namespace TextBox.Hubs;

/// <summary>
/// Client event names for <see cref="MessageHub"/> (server → client only;
/// the hub exposes no client-callable methods).
/// </summary>
public static class MessageHubEvents
{
    public const string MessageReceived = "MessageReceived";
    public const string MessagesCleared = "MessagesCleared";
}

/// <summary>
/// Pushes inbox changes to connected dashboards.
/// </summary>
public sealed class MessageHub : Hub
{
}

/// <summary>
/// Server-side fanout for inbox changes, kept separate so endpoints stay thin
/// and the fanout contract is unit-testable without a test server.
/// </summary>
public sealed class MessageNotifier(IHubContext<MessageHub> hub)
{
    public Task NotifyReceivedAsync(SmsMessage message, CancellationToken ct = default) =>
        hub.Clients.All.SendAsync(MessageHubEvents.MessageReceived, message, ct);

    public Task NotifyClearedAsync(CancellationToken ct = default) =>
        hub.Clients.All.SendAsync(MessageHubEvents.MessagesCleared, ct);
}
