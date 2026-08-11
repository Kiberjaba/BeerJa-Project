# BeerJa frontend → backend handoff v2

## Canonical entries

- Product root: `/app/`
- Player join: `/app/player/join`
- Player account: `/app/player/account`
- Host account: `/app/host/account`
- Mechanics catalog: `/app/mechanics`
- Order flow: `/app/order`
- Existing guided demo: `/app/?tour=1`
- Existing integration mode: append `?data=api` to a product route or demo entry.

Legacy pages `glav.html`, `ved.html`, `lichved.html` and `shabkviz.html` are no longer linked from canonical navigation. They remain only as historical references until PR review confirms removal.

## State ownership

| State | Owner |
| --- | --- |
| Current user, role and token | Backend |
| Room lookup and join permission | Backend |
| Player profile, history and results | Backend |
| Host games and analytics | Backend |
| Mechanics catalog and prices | Backend/content storage |
| Order status and payment eligibility | Backend |
| Product route, open panels, local form draft | Frontend |
| Guided tour fixtures | Frontend only |

## Adapter

All new product operations go through `product-contracts.js`, which delegates to `backend-bridge.js` in `api` mode and deterministic fixtures in `mock` mode.

There is no automatic fallback from API errors to mock success.

For solo product review, the legacy clean-player mock automatically advances from a confirmed captain vote to the first question. This convenience automation is disabled in `?data=api`; production game start remains owned by the host/backend.

### Auth

- `POST /api/v1/User/register`
- `POST /api/v1/User/login`
- `GET /api/v1/User/yandex-login`
- `GET /api/v1/User/me`

### Rooms

- `GET /api/v1/rooms/by-code/{code}`
- `POST /api/v1/rooms/{roomId}/join`

### Player

- `GET /api/v1/users/me/profile`
- `GET /api/v1/users/me/games`
- `GET /api/v1/users/me/games/{gameId}`

### Host

- `GET /api/v1/hosts/me/games`
- `GET /api/v1/hosts/me/games/{gameId}`
- `GET /api/v1/hosts/me/games/{gameId}/analytics`

### Catalog and order

- `GET /api/v1/mechanics`
- `GET /api/v1/mechanics/{slug}`
- `POST /api/v1/orders`
- `GET /api/v1/orders/{orderId}`

## Required server-shaped errors

- `400` malformed payload;
- `401` login required;
- `403` role/room access denied;
- `404` room, game, mechanic or order not found;
- `409` room/game version conflict or duplicate action;
- `422` valid JSON with invalid business fields;
- `500` generic failure with safe `message`.

Frontend preserves entered data after validation or network errors. In API mode it shows an error and does not report mock success.

## First backend connection order

1. Login/register/current user.
2. Room lookup and join.
3. Player profile and game history.
4. Host game list and game detail.
5. Existing gameplay state/commands/answers.
6. Analytics.
7. Mechanics catalog and orders.
8. Payment only after an order is approved.

## Acceptance handoff

Backend owner can take one adapter method at a time. The UI route and mock fixture remain stable while the adapter implementation changes from mock to HTTP. Any payload change must update this document, `product-contracts.js`, and the relevant C# contract together.
