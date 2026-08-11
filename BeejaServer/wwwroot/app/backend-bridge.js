const params = new URLSearchParams(window.location.search);

export const integrationMode = params.get("data") === "api" ? "api" : "mock";
export const apiBase = document.querySelector('meta[name="beerja-api-base"]')?.content || "/api/v1";

function clone(value) {
  return typeof structuredClone === "function"
    ? structuredClone(value)
    : JSON.parse(JSON.stringify(value));
}

function publish(name, detail) {
  window.dispatchEvent(new CustomEvent(name, { detail: clone(detail) }));
}

async function request(path, options = {}) {
  const response = await fetch(`${apiBase}${path}`, {
    credentials: "same-origin",
    headers: {
      Accept: "application/json",
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...options.headers
    },
    ...options
  });

  const payload = response.status === 204
    ? null
    : await response.json().catch(() => null);

  if (!response.ok) {
    const error = new Error(payload?.message || `BeerJa API вернул ${response.status}`);
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}

export const beerJaApi = Object.freeze({
  beginYandexLogin() {
    window.location.assign(`${apiBase}/User/yandex-login`);
  },
  getCurrentUser(token) {
    return request("/User/me", {
      headers: token ? { Authorization: `Bearer ${token}` } : {}
    });
  },
  getGameState(gameId) {
    return request(`/games/${encodeURIComponent(gameId)}/state`);
  },
  submitCaptainVote(gameId, payload) {
    return request(`/games/${encodeURIComponent(gameId)}/captain-votes`, {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  submitAnswer(gameId, payload) {
    return request(`/games/${encodeURIComponent(gameId)}/answers`, {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  advanceGame(gameId, payload) {
    return request(`/games/${encodeURIComponent(gameId)}/commands`, {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  submitFeedback(gameId, payload) {
    return request(`/games/${encodeURIComponent(gameId)}/feedback`, {
      method: "POST",
      body: JSON.stringify(payload)
    });
  },
  register(payload) {
    return request("/User/register", { method: "POST", body: JSON.stringify(payload) });
  },
  login(payload) {
    return request("/User/login", { method: "POST", body: JSON.stringify(payload) });
  },
  getRoomByCode(code) {
    return request(`/rooms/by-code/${encodeURIComponent(code)}`);
  },
  joinRoom(roomId, payload = {}) {
    return request(`/rooms/${encodeURIComponent(roomId)}/join`, { method: "POST", body: JSON.stringify(payload) });
  },
  getPlayerProfile() {
    return request("/users/me/profile");
  },
  getPlayerGames() {
    return request("/users/me/games");
  },
  getPlayerGame(gameId) {
    return request(`/users/me/games/${encodeURIComponent(gameId)}`);
  },
  getHostGames() {
    return request("/hosts/me/games");
  },
  getHostGame(gameId) {
    return request(`/hosts/me/games/${encodeURIComponent(gameId)}`);
  },
  getHostAnalytics(gameId) {
    return request(`/hosts/me/games/${encodeURIComponent(gameId)}/analytics`);
  },
  getMechanics() {
    return request("/mechanics");
  },
  getMechanic(slug) {
    return request(`/mechanics/${encodeURIComponent(slug)}`);
  },
  createOrder(payload) {
    return request("/orders", { method: "POST", body: JSON.stringify(payload) });
  },
  getOrder(orderId) {
    return request(`/orders/${encodeURIComponent(orderId)}`);
  }
});

export function emitUiAction(action, value, state) {
  publish("beerja:ui-action", {
    action,
    value: value ?? null,
    mode: integrationMode,
    state
  });
}

export function emitStateChanged(state, source = "render") {
  publish("beerja:state-changed", {
    source,
    mode: integrationMode,
    state
  });
}

export function beginIntegration(getState) {
  window.BeerJaFrontend = Object.freeze({
    version: "1.0.0",
    mode: integrationMode,
    apiBase,
    api: beerJaApi,
    getState: () => clone(getState())
  });

  publish("beerja:ready", {
    version: window.BeerJaFrontend.version,
    mode: integrationMode,
    apiBase
  });
}
