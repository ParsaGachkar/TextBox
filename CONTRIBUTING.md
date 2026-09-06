# Contributing to TextBox

## Prerequisites

- [.NET SDK 10.0.302+](https://dotnet.microsoft.com/download) (`dotnet --version`)
- [Node.js 22+](https://nodejs.org/) + npm (Tailwind CSS build)
- Docker Desktop (optional, for image work)

First-time setup:

```powershell
dotnet --version            # 10.0.302
npm install --prefix src/TextBox
dotnet build TextBox.slnx
```

## Workflow

```powershell
dotnet build TextBox.slnx              # build all (compiles CSS via Tailwind target)
dotnet test TextBox.slnx               # full suite (xUnit + NSubstitute + bUnit)
dotnet test TextBox.slnx --filter <TestName>
npm run watch:css --prefix src/TextBox # live CSS rebuilds while doing UI work
dotnet run --project src/TextBox/TextBox.csproj
docker build -f src/TextBox/Dockerfile -t textbox . # image (context is the repo root)
```

Always specify `TextBox.slnx` explicitly — a legacy `TextBox.sln` also exists
and bare `dotnet` commands fail with "Found more than one solution file".

## Conventional Commits (required)

Every commit message — and every PR title (linted in CI) — must follow
[Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

[optional body]
```

- `type`: `feat` | `fix` | `docs` | `style` | `refactor` | `perf` | `test` |
  `build` | `ci` | `chore` | `revert`
- `scope`: the area touched — `api`, `ui`, `auth`, `store`, `docker`, `ci`,
  `docs`, `tests`, `deps`. Omit only if truly repo-wide.
- `subject`: imperative mood, lowercase, no trailing period, max ~72 chars.

Examples:

```
feat(api): require API key on send endpoint
fix(ui): clear conversation selection after inbox clear
docs(readme): document GHCR image tags
ci(docker): push image to GHCR on master
chore(deps): bump LiteDB to 5.0.21
```

Breaking changes append `!` (`feat(api)!: ...`) and must explain the break
in the body.

## Pull requests

- Target `master`. Keep PRs focused; one concern per PR.
- CI must be green: build, tests, Docker build, and the PR-title lint.
- Use a conventional-commit message as the PR title (it becomes the squash
  commit) and link any related issues.
- Update `README.md` / `AGENTS.md` / `DESIGN.md` when behavior, commands,
  or UI contracts change.
