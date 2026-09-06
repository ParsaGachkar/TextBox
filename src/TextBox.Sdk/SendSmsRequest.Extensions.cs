namespace TextBox.Sdk;

/// <summary>
/// Convenience over the generated positional constructor (<c>body, from, to</c>),
/// which is easy to mis-order.
/// </summary>
public partial record SendSmsRequest
{
    /// <summary>Creates a send request in <c>(to, body, from)</c> order.</summary>
    public static SendSmsRequest Create(string to, string body, string from = "") =>
        new(body: body, from: from, to: to);
}
