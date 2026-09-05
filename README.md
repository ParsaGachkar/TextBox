# TextBox

Simple SMS mocking API + dashboard - C# .NET + Blazor. Ship as a Docker image for local/dev testing and consume via a NuGet SDK.

> **Status: single Blazor project** - `src/TextBox/TextBox.csproj` hosts both the SMS mock API (`/api/messages`) and the dashboard (`/`). `dotnet sln migrate` already run - use `TextBox.slnx`.

## What it does

- **Mock SMS API** - send/list/clear messages without a real provider. LiteDB file persistence (`MessageStore:Path`, default `Data/textbox.db`) with an in-memory read-through cache for tests and local dev.
- **Dashboard** - phone-style Blazor UI (Tailwind CSS + daisyUI): conversations grouped by recipient at `/`, chat view with message-detail popup at `/conversation/{number}`.
- **API docs** - interactive Scalar reference at `/scalar` (OpenAPI at `/openapi/v1.json`).
- **Docker image** - one-command `docker run` for CI or local dev.
- **NuGet SDK** - typed client to integrate tests/apps (`TextBox.Sdk`).

## Tech Stack

- .NET SDK `10.0.302` (`net10.0` - check `.csproj` `TargetFramework` after scaffolding)
- ASP.NET Core (API) + Blazor (dashboard)
- Tailwind CSS v4 + daisyUI 5 (compiled from `Styles/app.css` on build - needs Node.js; `npm run watch:css` in `src/TextBox` for live rebuilds)
- Scalar API reference (`Scalar.AspNetCore` + `Microsoft.AspNetCore.OpenApi`)
- Rider / Visual Studio

## Prerequisites

- [.NET SDK 10.0.302+](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for the Tailwind CLI invoked by the build; one-time `npm install` in `src/TextBox`)
- Docker (optional, for image)

Verify: `dotnet --version` -> `10.0.302`

## Getting Started

Single project - build and run immediately:

```powershell
# Build & run (API + dashboard on the same host)
dotnet build TextBox.slnx
dotnet run --project src/TextBox/TextBox.csproj
```

Then open `/` for the phone dashboard, `/scalar` for the interactive API reference, and use `/api/messages` for the mock SMS API:

```powershell
# Send
curl -X POST http://localhost:5000/api/messages `
  -H "Content-Type: application/json" `
  -d '{"to":"+123","body":"hello"}'
# List (optional ?to= filter)
curl http://localhost:5000/api/messages
# Clear
curl -X DELETE http://localhost:5000/api/messages
```

## API key auth (send endpoint)

`POST /api/messages` requires an API key when one is configured (reads stay
open). Set it in `appsettings.json` — empty means open:

```json
"ApiKey": {
  "Key": "secret-1"
}
```

```powershell
curl -X POST http://localhost:5000/api/messages `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer secret-1" `
  -d '{"to":"+123","body":"hello"}'
```

Without a valid key the API returns `401 {"error":"Missing or invalid API key."}`
(enforced through the ASP.NET Core authentication pipeline). The `/scalar`
reference documents the Bearer scheme, and the dashboard shows the
configured key in the side panel, with a copy button.

## Live updates

The dashboard updates in realtime over SignalR (`/hubs/messages`):
`MessageReceived` after each send, `MessagesCleared` after each clear.
No client authentication is required on the hub (local dev mock).

## Tests

xUnit + NSubstitute (unit) + bUnit (Blazor UI) in `tests/TextBox.Tests`:

```powershell
# All tests
dotnet test TextBox.slnx
# Single test
dotnet test TextBox.slnx --filter <TestName>
```

## Docker

```powershell
# Build
docker build -t textbox .
# Run (dashboard at http://localhost:8080, API docs at /scalar)
docker run --rm -p 8080:8080 textbox
```

The image runs as non-root and listens on `8080`. Persist the LiteDB
file across restarts with a volume (it lives at `/app/Data` by default):

```powershell
docker run --rm -p 8080:8080 -v textbox-data:/app/Data textbox
```

Configure via environment (same keys as `appsettings.json`):

```powershell
docker run --rm -p 8080:8080 `
  -e ApiKey__Key=secret-1 `
  -e MessageStore__Path=/app/Data/textbox.db `
  textbox
```

## CI/CD

`.github/workflows/ci.yml` runs on pushes/PRs to `master`: build, tests,
then a Docker build. Pushes to `master` also push the image to
`ghcr.io/<owner>/textbox` (`:latest` + `:sha-<commit>`).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Commit messages and PR titles must
follow Conventional Commits (enforced on PRs by CI).

## NuGet SDK (planned)

```powershell
dotnet pack src/TextBox.Sdk/TextBox.Sdk.csproj -c Release
# or push to feed
dotnet nuget push src/TextBox.Sdk/bin/Release/*.nupkg --source <feed>
```

Consume:

```csharp
var client = new TextBoxClient("http://localhost:8080");
await client.SendAsync(new SmsMessage { To = "+123", Body = "hello" });
```

## Project Structure (target)

```
TextBox.slnx  # use .slnx (migrated from .sln via `dotnet sln migrate`)
├── src/
│   └── TextBox/          # Blazor dashboard (/) + SMS mock API (/api/messages)
│       ├── Components/   # Blazor UI (Pages/Home.razor = inbox)
│       ├── Endpoints/    # Minimal API (MessageEndpoints)
│       ├── Models/       # SmsMessage / SendSmsRequest
│       └── Services/     # IMessageStore (LiteDB), options, validators
└── tests/
    └── TextBox.Tests/    # xUnit/NSubstitute unit + bUnit UI tests
```

`TextBox.Sdk` NuGet client planned - not scaffolded yet.

## Commands

| Task | Command |
|------|---------|
| Verify solution | `dotnet sln TextBox.slnx list` |
| Build all | `dotnet build TextBox.slnx` |
| Run app (API + dashboard) | `dotnet run --project src/TextBox/TextBox.csproj` |
| Run tests | `dotnet test TextBox.slnx` |

PowerShell 5.1 on Windows - use `workdir` param in tooling instead of `cd`.

## License

TBD
