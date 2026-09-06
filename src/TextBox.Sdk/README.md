# TextBox.Sdk

Typed .NET client for the [TextBox](https://github.com/ParsaGachkar/TextBox) SMS mock API.
Generated with [NSwag](https://github.com/RicoSuter/NSwag) from the server's OpenAPI document
(`src/TextBox.Sdk/openapi.json` snapshot); hand-written partials add auth + construction helpers.

> Not published to any feed yet. Pack locally (`dotnet pack src/TextBox.Sdk -c Release`)
> or consume the CI artifact until a feed is configured.

<!-- Logo note: nuget.org READMEs only render absolute image URLs and this repo
     has no remote yet. Once it does, point this at the raw logo URL:
     <p align="center"><img src="https://raw.githubusercontent.com/<owner>/TextBox/master/branding-assets/TextBoxLogo%20-%20256.png" width="96" alt="TextBox logo" /></p> -->

## Install

```powershell
dotnet add package TextBox.Sdk
```

## Use (no DI required)

```csharp
using TextBox.Sdk;

var client = new TextBoxClient(new TextBoxOptions
{
    BaseAddress = "http://localhost:8080",
    ApiKey = "secret-1", // optional; omit when the server has no key configured
});

var sent = await client.SendAsync(SendSmsRequest.Create("+123", "hello"));
var inbox = await client.ListAsync();
```

Convenience overloads: `new TextBoxClient("http://localhost:8080")`,
`new TextBoxClient("http://localhost:8080", apiKey: "secret-1")`.
Bring your own `HttpClient`/`HttpMessageHandler` (other containers, tests) via the
`TextBoxClient(HttpClient)` / `TextBoxClient(TextBoxOptions, HttpMessageHandler?)` overloads.
The client sends `Authorization: Bearer <key>` only when an API key is set,
so one build works against both open and secured servers.

## Use (Microsoft DI, optional)

```csharp
services.AddTextBoxClient("http://localhost:8080", apiKey: "secret-1");
```
