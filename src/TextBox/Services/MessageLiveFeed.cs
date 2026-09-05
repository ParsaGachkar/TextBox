using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using TextBox.Hubs;
using TextBox.Models;

namespace TextBox.Services;

/// <summary>
/// Per-circuit SignalR client for inbox change notifications. Pages subscribe
/// to the events, call <see cref="EnsureStartedAsync"/> once interactive, and
/// unsubscribe on dispose. A failed start degrades gracefully (pages keep
/// working via manual refresh); the next call retries.
/// </summary>
public sealed class MessageLiveFeed(NavigationManager navigationManager) : IAsyncDisposable, IDisposable
{
    private HubConnection? _connection;
    private bool _disposed;

    public event Func<SmsMessage, Task>? MessageReceived;
    public event Func<Task>? MessagesCleared;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task EnsureStartedAsync()
    {
        if (_connection is not null || _disposed)
            return;

        var connection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/hubs/messages"))
            .WithAutomaticReconnect()
            .Build();
        connection.On<SmsMessage>(MessageHubEvents.MessageReceived, ReceiveAsync);
        connection.On(MessageHubEvents.MessagesCleared, ClearedAsync);

        try
        {
            await connection.StartAsync();
            _connection = connection;
        }
        catch (Exception)
        {
            // No hub reachable (e.g. prerendered output under test): stay
            // disconnected; pages keep working and the next call retries.
            await connection.DisposeAsync();
        }
    }

    /// <summary>Raises <see cref="MessageReceived"/> (hub handler + tests).</summary>
    public async Task ReceiveAsync(SmsMessage message)
    {
        if (MessageReceived is null)
            return;
        foreach (var handler in MessageReceived.GetInvocationList().Cast<Func<SmsMessage, Task>>())
            await handler(message);
    }

    /// <summary>Raises <see cref="MessagesCleared"/> (hub handler + tests).</summary>
    public async Task ClearedAsync()
    {
        if (MessagesCleared is null)
            return;
        foreach (var handler in MessagesCleared.GetInvocationList().Cast<Func<Task>>())
            await handler();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public void Dispose()
    {
        // Sync path exists for containers without async disposal (e.g. bUnit's
        // sync teardown in tests). Production DI prefers DisposeAsync above.
        _disposed = true;
        var connection = Interlocked.Exchange(ref _connection, null);
        if (connection is not null)
            _ = connection.DisposeAsync();
    }
}
