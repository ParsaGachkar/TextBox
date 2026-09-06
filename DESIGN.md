# DESIGN.md — TextBox UI contracts

Phone-style dashboard (Tailwind CSS v4 + daisyUI 5) + Scalar API docs.
This file is the contract for UI work: class names, theme tokens, layout
structure, state flow, and the gotchas that are easy to break. Follow it
when adding or changing any `.razor`/CSS.

## 1. Stack

| Piece | Version / location | Notes |
|---|---|---|
| Tailwind CSS | 4.3.3 (`@tailwindcss/cli`) | CSS-first config, no `tailwind.config.js` |
| daisyUI | 5.7.28 | Loaded as a Tailwind plugin (see `Styles/app.css`) |
| CSS source | `src/TextBox/Styles/app.css` | **Only** place for hand-written CSS |
| CSS output | `src/TextBox/wwwroot/css/app.css` | Generated, gitignored; rebuilt on every `dotnet build` via the `BuildTailwindCss` target in `TextBox.csproj` |
| Live rebuild | `npm run watch:css` in `src/TextBox` | Run alongside `dotnet watch` during UI dev |
| Icons | Inline Lucide SVGs | No icon library/JS; `stroke="currentColor"`, 20×20 (see §7) |

## 2. Theme contract

- Themes enabled: `light --default` and `dark` (`@plugin "daisyui"` block in `Styles/app.css`).
- Both themes override **only** `--color-primary: #512bd4` and `--color-primary-content: #fff`; every other token is inherited from the daisyUI built-in (per daisyUI partial-override docs).
- The active theme lives on `<html data-theme="...">` (`Components/App.razor`, SSR default `light`).
- Persistence key: `localStorage["textbox-theme"]` (`"light"` fallback).
- No-flash snippet: an inline `<script>` in `App.razor <head>` applies the stored theme before first paint. Do not remove it.
- Toggle: `Components/Layout/ThemeToggle.razor` (navbar, sun/moon swap). State syncs via `window.textBoxTheme.get()/set()` in `wwwroot/js/theme.js`, loaded in `App.razor` **before** `blazor.web.js`.
- Rule: pages/components must use daisyUI semantic tokens (`bg-base-100`, `text-base-content`, `bg-primary`, `text-error`, `border-base-200`, …) — never hard-code light-mode colors — or dark mode breaks.

## 3. Layout contracts

Two shells share the navbar and error UI via reusable components — do not
duplicate that markup, extend the shared ones:

- `Components/Layout/TopNavbar.razor` — brand + `ThemeToggle` + SDK + API-docs
  links (both `target _blank`: `/sdk` internal guide, `/scalar` external docs).
  Static, no `@code`, no services.
- `Components/Layout/BlazorErrorUi.razor` — the `#blazor-error-ui` div.
  Render once per layout (its styles live in `Styles/app.css`).

### 3a. `MainLayout.razor` (phone shell — default for `/`, `/conversation/…`)

Static shell, identical on every phone page: no `@code`, no injected
services, no route checks. Do not add any.

```
TopNavbar
└── content row (flex-col on mobile, lg:flex-row on desktop, phone first)
    ├── .mockup-phone
    │   ├── .mockup-phone-camera
    │   └── .mockup-phone-display (.flex.flex-col.relative + pt-[15%], bg-base-100)
    │       └── @Body (each page renders its own bar + scroll content)
    ├── .divider.lg:divider-horizontal (daisyUI divider splitter)
    └── <HomeAside /> (API key + quick-guide + SDK cards)
```

Pages own everything inside the display: `Home.razor` renders the search
bar + list, `Conversation.razor` the back bar + chat + modal.

### 3b. `DocsLayout.razor` (prose shell — opt in via `@layout DocsLayout`)

Full-width docs shell, no phone mockup, no aside:

```
TopNavbar
└── main (max-w-3xl, centered)
    └── article.card > .card-body > @Body
```

Docs pages own everything inside the card body (`Sdk.razor`).

- There is no sidebar/`NavMenu` — do not reintroduce one.
- Notch clearance: `pt-[15%]` on the display. The camera pill bottom sits at ~6.7% of phone height ≈ 14% of display width (fixed 462:978 aspect ratio), and `%` padding is width-relative, so this clears the notch at any size. If daisyUI changes the mockup geometry, recompute.
- `<PhoneFrame>` takes only `ChildContent` and has no code-behind logic; page-specific bars live in the pages, never in the layout.

## 4. Routes & pages

| Route | File | Contract |
|---|---|---|
| `/` | `Components/Pages/Home.razor` (`InteractiveServer`) | Group messages by `To` (newest conversation first); daisyUI `ul.list` rows — count avatar (`bg-primary` circle), number + last-body snippet (truncate), timestamp; row click → `conversation/{url-escaped number}` via `NavigationManager`. Filter by a local `search` field (number substring, ordinal-ignore-case, `@bind:event="oninput"` + `@bind:after`). Keep Refresh / Clear-all ghost buttons. Empty state mentions `POST /api/messages`. Renders `<HomeAside />` beside the phone. |
| `/conversation/{PhoneNumber}` | `Components/Pages/Conversation.razor` (`InteractiveServer`) | Exact `To == PhoneNumber` match, oldest-first `chat chat-start` bubbles; header = `From ?? "Unknown"` + local timestamp. Click bubble → detail modal. Reload data in `OnParametersSetAsync` (route-param navigation reuses the component). |
| `/sdk` | `Components/Pages/Sdk.razor` (`@layout DocsLayout`, `InteractiveServer`) | Full-width SDK guide (install, manual + DI usage, key behavior, methods table, `/scalar` link). Prose inside the docs card — headings + `pre` blocks with `overflow-x-auto`. |
| `/scalar` | Scalar middleware (`Program.cs`) | External docs page, linked from navbar. Mapped in **all** environments (Docker runs as Production). Scalar JS comes from CDN — needs browser internet. |

## 5. Component contracts

- **Search field** (top of `Home.razor`, inside the phone): standalone Lucide search SVG + `label.floating-label > span + input.input.input-sm.input-bordered`, bound to a local field (`@bind:event="oninput"` + `@bind:after="RefreshAsync"`). Do NOT nest the icon inside the floating label — that combination is undocumented and breaks layout in the narrow bar.
- **Modal** (conversation page): `<div class="modal modal-open phone-modal">` + `.modal-box` (title, `dl` details, raw-JSON `pre`, `.modal-action` Close button) + `.modal-backdrop` div with `@onclick` close. Blazor-controlled (no `showModal()` JS). `.phone-modal` (`Styles/app.css`) constrains it to the phone frame — see §6.
- **Chat**: `chat-start` only (single-recipient mock inbox); body text via `@m.Body` (Razor-encoded, no raw HTML).
- **Buttons**: navbar `btn-ghost btn-sm` (+ `btn-square` for icon-only); list actions `btn-ghost btn-xs`, destructive `text-error`.

## 6. CSS override rules (`Styles/app.css` only)

- `.phone-modal { position: absolute; inset: 0; }` — daisyUI emits `.modal` as `position: fixed` inside **nested** cascade layers (`utilities.daisyui.*`), which beat Tailwind's `absolute` utility. Plain unlayered CSS wins over any layered CSS, hence the hand-written class. Requires `relative` on `.mockup-phone-display` (present in `PhoneFrame`). Do not "simplify" to `absolute`.
- `#blazor-error-ui` styles live here (ported from the old Bootstrap `app.css`). Keep in sync with the `#blazor-error-ui` div in `MainLayout.razor`.
- Never edit `wwwroot/css/app.css` by hand — it is overwritten on build.

## 7. Icons

Inline Lucide SVGs, `fill="none" stroke="currentColor" stroke-width="2" stroke-linecap/linejoin="round"`:
search = circle + `m21 21-4.3-4.3`; chevron-left = `m15 18-6-6 6-6`; moon = `M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z`; sun = circle + rays. To add an icon, paste the Lucide path data into this shell.

## 8. UI state

- There is no shared UI-state service. Filter/search text lives in local
  component fields (e.g. `search` in `Home.razor`).
- `ThemeToggle` keeps its own `isDark` bool, hydrated from JS on first interactive render; static prerender keeps the light default (JS interop is try/caught).

## 9. Render-mode contract (read this before touching interactivity)

- Interactivity is enabled **once, globally**: `Components/App.razor` renders
  `<Routes @rendermode="InteractiveServer" />`. Everything below it (Router,
  layout, pages) is interactive; prerendering is preserved.
- Render modes inherit **top-down only**. A page's `@rendermode` never makes
  its layout interactive — when the layout was static, every `@onclick`/`@bind`
  in it (theme toggle, search box) rendered as dead markup with zero errors.
- NEVER put `@rendermode` on `MainLayout` (its `Body` RenderFragment cannot
  cross a static→interactive boundary — throws `InvalidOperationException`)
  nor on `Router` in `Routes.razor` (its `Found`/`NotFound` templates cannot
  cross either). `Routes` itself takes no parameters, which is why the
  boundary lives there. Per-page `@rendermode` directives are redundant but
  harmless — keep them as documentation.
- Debugging rule: if clicks/inputs silently do nothing (no console errors,
  websocket connected), the element is almost certainly outside the
  interactive boundary — check where its render mode comes from, not the
  handler code.

## 10. Realtime updates (SignalR)

- Hub at `/hubs/messages` (`Hubs/MessageHub.cs`): server → client only, no
  client-callable methods, no auth. Events: `MessageReceived` (SmsMessage),
  `MessagesCleared` (no args) — names live in `MessageHubEvents`.
- Server fanout goes through `Hubs/MessageNotifier.cs` (keeps endpoints thin
  and unit-testable); endpoints call it after the store write.
- Pages consume via scoped `Services/MessageLiveFeed.cs` (one HubConnection
  per circuit, `WithAutomaticReconnect`). Rules:
  - Subscribe in `OnInitializedAsync`, unsubscribe in `Dispose`.
  - Only `await EnsureStartedAsync()` when interactive: guard with
    `[CascadingParameter] HttpContext?` (non-null during prerender → skip).
    Start failures degrade gracefully to manual refresh.
  - Hub callbacks arrive off-thread: marshal with `InvokeAsync` + explicit
    `StateHasChanged()` — calling either directly throws.
  - `Conversation`: on cleared, reload and `NavigateTo("/")` when its number
    is gone.

## 11. Gotchas (learned the hard way)

- Blazor SSR HTML-encodes `+` as `&#x2B;` — phone numbers look "wrong" in View Source but render fine. Test with rendered text, not raw HTML.
- `MapStaticAssets` fingerprints CSS (`css/app.<hash>.css`) — assert on `app.` + `.css`, never the exact filename.
- `data-theme` values are exactly `light`/`dark` (customized built-ins, not a custom theme name).
- `FocusOnNavigate Selector="h1"` in `Routes.razor`: pages should keep an `h1`.
- `Error`/`NotFound` render inside the phone too; they inherit global interactivity like everything else.
