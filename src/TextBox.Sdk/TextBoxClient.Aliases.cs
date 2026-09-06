namespace TextBox.Sdk;

/// <summary>
/// Friendly aliases over the generated operation-style names
/// (<c>MessagesPOSTAsync</c> etc.). Thin wrappers, same behavior.
/// </summary>
public partial class TextBoxClient
{
    /// <summary>Sends an SMS (<c>POST /api/messages</c>).</summary>
    public System.Threading.Tasks.Task<SmsMessage> SendAsync(
        SendSmsRequest request,
        System.Threading.CancellationToken cancellationToken = default) =>
        MessagesPOSTAsync(request, cancellationToken);

    /// <summary>Sends an SMS (<c>POST /api/messages</c>).</summary>
    public System.Threading.Tasks.Task<SmsMessage> SendAsync(
        string to,
        string body,
        string from = "",
        System.Threading.CancellationToken cancellationToken = default) =>
        SendAsync(SendSmsRequest.Create(to, body, from), cancellationToken);

    /// <summary>Lists messages (<c>GET /api/messages</c>), optionally filtered by recipient.</summary>
    public System.Threading.Tasks.Task<System.Collections.Generic.ICollection<SmsMessage>> ListAsync(
        string? to = null,
        System.Threading.CancellationToken cancellationToken = default) =>
        MessagesAllAsync(to!, cancellationToken);

    /// <summary>Gets one message (<c>GET /api/messages/{id}</c>).</summary>
    public System.Threading.Tasks.Task<SmsMessage> GetAsync(
        System.Guid id,
        System.Threading.CancellationToken cancellationToken = default) =>
        MessagesGETAsync(id, cancellationToken);

    /// <summary>Clears all messages (<c>DELETE /api/messages</c>).</summary>
    public System.Threading.Tasks.Task ClearAsync(
        System.Threading.CancellationToken cancellationToken = default) =>
        MessagesDELETEAsync(cancellationToken);
}
