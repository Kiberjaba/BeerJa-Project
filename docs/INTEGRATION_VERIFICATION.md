# Integration verification

Updated: 2026-08-11
Branch: `codex/live-signal-integration`

## Result

Frontend integration status: PASS.

## Checks completed against BeerJa paths

- JavaScript syntax: every module in `BeejaServer/wwwroot/app/*.js` passed `node --check`.
- Product browser suite: 14/14 end-to-end checks passed.
- Responsive matrix: 13 production routes × 6 viewports = 78/78 checks.
- Root opens the canonical product landing at `/app/`.
- Live Signal renders from BeerJa-owned static files; no dependency on the old Open Design workspace.
- Backend bridge is exposed as `window.BeerJaFrontend`.
- `mock` is the default mode; `?data=api` selects integration mode.
- Clean player route hides the QA navigator.
- Demo overview renders five mobile role frames and one public screen.
- Public screen keeps a 16:9 ratio.
- Active live-game exit requires explicit confirmation and returns to `/app/` only after approval.
- In mock preview, captain vote automatically starts the game and opens the first question; `?data=api` preserves host/backend-controlled start.
- Runtime console errors: 0.
- Runtime page errors: 0.
- Failed asset requests: 0.

## Regression suite reused from Live Signal

- Guided tour: PASS; 9 questions, 12 screenshots, 0 console/page errors.
- Functional: PASS; 13/13 stages, 0 console/page errors.
- Keyboard/focus: PASS; 48 keyboard activations, 45 transitions, 0 mouse clicks, 0 console/page errors.
- Visual: PASS; 8 route/viewport reports, 0 failed checks.
- Route audit: PASS; 47/47 player, captain, organizer, host, public, overview, error, empty and responsive states; 0 failures.

## Server build

The repository was verified with a temporary official Microsoft .NET SDK 10.0.302 installation; no system-wide SDK or project configuration was changed.

- `dotnet restore Beerja.slnx`: PASS.
- `dotnet build Beerja.slnx --configuration Release --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test Beerja.slnx --configuration Release`: PASS, 4/4 tests.

The upstream GitHub Actions run still requires first-time contributor approval from `Kiberjaba` because the pull request comes from a fork.

## GitHub permission state

GitHub reports `viewerPermission: READ` for upstream `Kiberjaba/BeerJa-Project`. Delivery therefore uses fork branch `Temik812/BeerJa-Project:codex/live-signal-integration`; upstream PR #2 is the review and merge boundary.
