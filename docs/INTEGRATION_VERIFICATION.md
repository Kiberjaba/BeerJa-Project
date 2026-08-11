# Integration verification

Generated: 2026-08-11 11:15 MSK
Branch: `codex/live-signal-integration`

## Result

Frontend integration status: PASS.

## Checks completed against BeerJa paths

- JavaScript syntax: every module in `BeejaServer/wwwroot/app/*.js` passed `node --check`.
- Browser smoke: 18/18 checks passed.
- Root opens `/app/?tour=1`.
- Live Signal renders from BeerJa-owned static files; no dependency on the old Open Design workspace.
- Backend bridge is exposed as `window.BeerJaFrontend`.
- `mock` is the default mode; `?data=api` selects integration mode.
- Clean player route hides the QA navigator.
- Demo overview renders five mobile role frames and one public screen.
- Public screen keeps a 16:9 ratio.
- Runtime console errors: 0.
- Runtime page errors: 0.
- Failed asset requests: 0.

## Regression suite reused from Live Signal

- Guided tour: PASS; 27 visible steps, 9 questions, 36 view transitions, 0 console/page errors.
- Functional: PASS; 13/13 stages, 0 console/page errors.
- Keyboard/focus: PASS; 48 keyboard activations, 45 transitions, 0 mouse clicks, 0 console/page errors.
- Visual: PASS; 8 route/viewport reports, 0 failed checks.
- Route audit: PASS; 47/47 player, captain, organizer, host, public, overview, error, empty and responsive states; 0 failures.

## Server build limitation

The local Mac does not currently have a `dotnet` executable, so the .NET build and xUnit tests cannot be executed locally. The repository GitHub Actions workflow installs .NET SDK 10 and runs restore/build/test on pull requests to `main`; that CI result is the authoritative server compilation gate for this branch.

## GitHub permission state

The repository invitation was accepted successfully, but GitHub reports `viewerPermission: READ` even though the original invitation advertised `write`. The integration is prepared in a local branch. If direct push remains blocked, push the same commit from a fork and open a pull request, or ask `Kiberjaba` to grant Write access.
