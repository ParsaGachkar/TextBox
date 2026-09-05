using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using TextBox.Auth;
using TextBox.Components;
using TextBox.Endpoints;
using TextBox.Hubs;
using TextBox.Services;

var builder = WebApplication.CreateBuilder(args);

// Single-project hosting: Blazor dashboard + SMS mock API share this host.
// LiteDB file persistence with an in-memory read-through cache.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IMessageStore, LiteDbMessageStore>();
builder.Services.AddSingleton<MessageNotifier>();
builder.Services.AddScoped<MessageLiveFeed>();

// API-key auth for sending SMS (ASP.NET Core authentication pipeline;
// options bound from the "ApiKey" section in appsettings.json).
builder.Services.AddAuthentication(ApiKeyAuthOptions.SchemeName)
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(
        ApiKeyAuthOptions.SchemeName,
        options => builder.Configuration.GetSection(ApiKeyAuthOptions.SectionName).Bind(options));
builder.Services.AddSingleton<ApiKeyService>();

// OpenAPI document for the Scalar API reference below.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<ApiKeyDocumentTransformer>());

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Report whether sending SMS requires an API key (single source of truth).
var apiKeyEnabled = app.Services.GetRequiredService<ApiKeyService>().IsEnabled;
app.Logger.LogInformation(
    "API key auth for sending SMS is {State}.",
    apiKeyEnabled ? "enabled" : "disabled");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHub<MessageHub>("/hubs/messages");
app.MapMessageApi(requireApiKey: apiKeyEnabled);

// Interactive API documentation. Mapped in all environments: this is a
// local/dev mock server (often run from Docker as Production), and the
// dashboard navbar links here. Note: Scalar's UI assets load from a CDN,
// so the page needs internet access in the browser.
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("TextBox API"));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
