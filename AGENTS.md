# AGENTS.md

## Repo state — single Blazor project (API + dashboard merged)
- Solution `TextBox.slnx` contains one project: `src/TextBox/TextBox.csproj` (Blazor Web App, `net10.0`, Server interactivity). Phone-style dashboard at `/` (`Components/Pages/Home.razor` = conversation list + `HomeAside` side panel, `Conversation.razor` = chat + detail modal; `MainLayout.razor` is a static shell — same phone mockup + daisyUI divider + aside on every page, pages render only inner content), SMS mock API at `/api/messages` (`Endpoints/MessageEndpoints.cs`, LiteDB-backed async store in `Services/LiteDbMessageStore.cs` with `IMemoryCache` read-through, contracts in `Services/IMessageStore.cs`, models in `Models/SmsMessage.cs`). DB path via `MessageStore:Path` (default `Data/textbox.db`, gitignored). `POST /api/messages` optionally requires an API key (ASP.NET Core auth pipeline in `Auth/`: `ApiKeyAuthOptions.cs` scheme options + `ApiKeyAuthHandler.cs`, enforced via `RequireAuthorization()` when a key is configured, `ApiKey` section in `appsettings.json`, open by default; `Auth/ApiKeyService.cs` is the single source of truth for the key — never read `IOptions<>` directly, the scheme binds the named instance). Message timestamps are monotonic per store instance (LiteDB persists ms precision, so back-to-back sends would tie — do not revert to raw `UtcNow`). Realtime fanout over SignalR (`Hubs/MessageHub.cs` at `/hubs/messages`, `Hubs/MessageNotifier.cs`, per-circuit `Services/MessageLiveFeed.cs` client). Scalar API docs at `/scalar` (OpenAPI at `/openapi/v1.json`, mapped in all environments).
- Legacy `TextBox.sln` still present but unused — always specify `TextBox.slnx` explicitly (bare `dotnet sln list` / `dotnet build` fails with "Found more than one solution file").
- No `TextBox.Sdk` committed yet — verify before assuming it exists.
- Docker: `Dockerfile` at repo root (SDK + Node build stage, non-root `aspnet` runtime, port `8080`); DB lives at `/app/Data` — mount `-v textbox-data:/app/Data` to persist. CI: `.github/workflows/ci.yml` (build + test + docker build on push/PR to `master`, pushes image to GHCR on `master`).
- Contributions: follow `CONTRIBUTING.md` — Conventional Commits (`type(scope): subject`), PR titles linted in CI.
- Git is pre-initial-commit on `master`, no remote, no commits. `bin/`, `obj/`, `packages/` are gitignored.

## Stack & toolchain
- .NET SDK `10.0.302` available (`dotnet --version`). Single project targets `net10.0` — see `src/TextBox/TextBox.csproj` `TargetFramework`.
- Node.js + npm required: the build runs the Tailwind v4 CLI (`node ./node_modules/@tailwindcss/cli/dist/index.mjs -i ./Styles/app.css -o ./wwwroot/css/app.css`) via a `.csproj` target; daisyUI theme (`#512bd4` primary) lives in `Styles/app.css`. For live CSS rebuilds run `npm run watch:css` in `src/TextBox`.
- IDE is Rider (JetBrains). `.idea/` is nested at `.idea/.idea.TextBox/.idea/` — do not commit; root `.gitignore` already ignores `bin/`/`obj/` but not `.idea/` at this path, so avoid staging it.

## Commands (PowerShell 5.1, win32)
- Build all: `dotnet build TextBox.slnx`
- Run API/dashboard (single host): `dotnet run --project src/TextBox/TextBox.csproj`
- Test (xUnit + NSubstitute + bUnit in `tests/TextBox.Tests`): `dotnet test TextBox.slnx` / single test: `dotnet test TextBox.slnx --filter <TestName>`
- Add project to solution: `dotnet new <template> -n <Name> -o <Dir>` then `dotnet sln TextBox.slnx add <Dir>/<Name>.csproj`
- Verify solution state: `dotnet sln TextBox.slnx list` (bare `dotnet sln list` fails — two solution files)

## Gotchas
- `TextBox.sln` is legacy/empty — ignore it, always build `TextBox.slnx`.
- Use `workdir` param for tool calls instead of `Set-Location`/`cd` in PowerShell.
