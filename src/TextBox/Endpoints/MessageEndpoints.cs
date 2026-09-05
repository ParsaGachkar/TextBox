using TextBox.Hubs;
using TextBox.Models;
using TextBox.Services;

namespace TextBox.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageApi(this WebApplication app, bool requireApiKey)
    {
        var group = app.MapGroup("/api/messages").WithTags("Messages");

        group.MapGet("/", async (string? to, IMessageStore store, CancellationToken ct) =>
            Results.Ok(await store.ListAsync(to, ct)))
            .Produces<List<SmsMessage>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, IMessageStore store, CancellationToken ct) =>
            await store.GetAsync(id, ct) is { } message ? Results.Ok(message) : Results.NotFound())
            .Produces<SmsMessage>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        var send = group.MapPost("/", async (
            SendSmsRequest request,
            IMessageStore store,
            MessageNotifier notifications,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Body))
                return Results.BadRequest(new { error = "'to' and 'body' are required." });

            var message = await store.AddAsync(request.To.Trim(), request.From?.Trim(), request.Body, ct);
            await notifications.NotifyReceivedAsync(message, ct);
            return Results.Created($"/api/messages/{message.Id}", message);
        })
        .Produces<SmsMessage>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        if (requireApiKey)
            send.RequireAuthorization();

        group.MapDelete("/", async (
            IMessageStore store,
            MessageNotifier notifications,
            CancellationToken ct) =>
        {
            var cleared = await store.ClearAsync(ct);
            await notifications.NotifyClearedAsync(ct);
            return Results.Ok(new { cleared });
        })
        .Produces(StatusCodes.Status200OK);
    }
}
