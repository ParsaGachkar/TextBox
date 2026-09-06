using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using TextBox.Sdk;

namespace TextBox.Tests;

public sealed class SdkClientTests
{
    private const string JsonMessage =
        """{"id":"11111111-1111-1111-1111-111111111111","to":"+123","from":"","body":"hello","createdAt":"2026-09-06T00:00:00+00:00"}""";

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Json(string content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    [Fact]
    public void StringCtor_NormalizesBaseUrl()
    {
        var client = new TextBoxClient("http://localhost:8080");

        Assert.Equal("http://localhost:8080/", client.BaseUrl);
        Assert.Null(client.ApiKey);
    }

    [Fact]
    public async Task SendAsync_SendsBearerHeader_WhenApiKeySet()
    {
        var handler = new FakeHandler(_ => Json(JsonMessage, HttpStatusCode.Created));
        var client = new TextBoxClient(new TextBoxOptions
        {
            BaseAddress = "http://localhost:8080",
            ApiKey = "secret-1",
        }, handler);

        var sent = await client.SendAsync(SendSmsRequest.Create("+123", "hello"));

        Assert.Equal("+123", sent.To);
        Assert.Equal("hello", sent.Body);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("http://localhost:8080/api/messages", handler.LastRequest.RequestUri!.GetLeftPart(UriPartial.Path));
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("secret-1", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_OmitsAuthHeader_WhenNoApiKey()
    {
        var handler = new FakeHandler(_ => Json(JsonMessage, HttpStatusCode.Created));
        var client = new TextBoxClient(new TextBoxOptions { BaseAddress = "http://localhost:8080" }, handler);

        await client.SendAsync("+123", "hello");

        Assert.NotNull(handler.LastRequest);
        Assert.Null(handler.LastRequest.Headers.Authorization);
    }

    [Fact]
    public async Task ListAsync_DeserializesInbox()
    {
        var handler = new FakeHandler(_ => Json("[" + JsonMessage + "]"));
        var client = new TextBoxClient(new TextBoxOptions { BaseAddress = "http://localhost:8080" }, handler);

        var inbox = await client.ListAsync();

        Assert.Single(inbox);
        Assert.Equal("+123", inbox.First().To);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:8080/api/messages", handler.LastRequest.RequestUri!.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task GetAsync_HitsItemUrl()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var handler = new FakeHandler(_ => Json(JsonMessage));
        var client = new TextBoxClient(new TextBoxOptions { BaseAddress = "http://localhost:8080" }, handler);

        var message = await client.GetAsync(id);

        Assert.Equal(id, message.Id);
        Assert.Equal($"http://localhost:8080/api/messages/{id}", handler.LastRequest!.RequestUri!.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task ClearAsync_IssuesDelete()
    {
        var handler = new FakeHandler(_ => Json("""{"cleared":2}"""));
        var client = new TextBoxClient(new TextBoxOptions { BaseAddress = "http://localhost:8080" }, handler);

        await client.ClearAsync();

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("http://localhost:8080/api/messages", handler.LastRequest.RequestUri!.GetLeftPart(UriPartial.Path));
    }

    [Fact]
    public async Task SendAsync_ThrowsApiException_OnUnauthorized()
    {
        var handler = new FakeHandler(_ => Json("""{"error":"Missing or invalid API key."}""", HttpStatusCode.Unauthorized));
        var client = new TextBoxClient(new TextBoxOptions { BaseAddress = "http://localhost:8080" }, handler);

        var ex = await Assert.ThrowsAsync<TextBoxApiException>(() => client.SendAsync("+123", "hello"));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public void AddTextBoxClient_ResolvesTypedClient()
    {
        var services = new ServiceCollection();
        services.AddTextBoxClient("http://localhost:8080", "secret-1");

        var client = services.BuildServiceProvider().GetRequiredService<ITextBoxClient>();

        var typed = Assert.IsType<TextBoxClient>(client);
        Assert.Equal("http://localhost:8080/", typed.BaseUrl);
        Assert.Equal("secret-1", typed.ApiKey);
    }
}
