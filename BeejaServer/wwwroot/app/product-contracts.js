import { beerJaApi, integrationMode } from "./backend-bridge.js";
import { hostGames, mechanics, playerGames, playerProfile, roomFixtures } from "./product-data.js";

function wait(value, delay = 320) {
  return new Promise((resolve) => window.setTimeout(() => resolve(structuredClone(value)), delay));
}

function mockRoom(code) {
  const room = roomFixtures[code];
  if (!room) {
    const error = new Error("Комната не найдена");
    error.status = 404;
    throw error;
  }
  return { id: code, code, ...room };
}

function mockOrder(payload) {
  return { id: `BJ-${new Date().getFullYear()}-0811`, status: "received", ...payload };
}

export const productApi = Object.freeze({
  mode: integrationMode,
  beginYandexLogin() {
    if (integrationMode === "api") {
      beerJaApi.beginYandexLogin();
      return Promise.resolve(null);
    }
    return wait({ token: "mock-yandex-token", user: playerProfile });
  },
  getRoomByCode(code) {
    return integrationMode === "api" ? beerJaApi.getRoomByCode(code) : wait(mockRoom(code));
  },
  login(payload) {
    return integrationMode === "api" ? beerJaApi.login(payload) : wait({ token: "mock-token", user: playerProfile, role: payload.role });
  },
  register(payload) {
    return integrationMode === "api" ? beerJaApi.register(payload) : wait({ token: "mock-token", user: { ...playerProfile, name: payload.username }, role: payload.role });
  },
  getPlayerProfile() {
    return integrationMode === "api" ? beerJaApi.getPlayerProfile() : wait(playerProfile);
  },
  getPlayerGames() {
    return integrationMode === "api" ? beerJaApi.getPlayerGames() : wait(playerGames);
  },
  getHostGames() {
    return integrationMode === "api" ? beerJaApi.getHostGames() : wait(hostGames);
  },
  getHostAnalytics(gameId) {
    const game = hostGames.find((item) => item.id === gameId);
    return integrationMode === "api" ? beerJaApi.getHostAnalytics(gameId) : wait(game || null);
  },
  getMechanics() {
    return integrationMode === "api" ? beerJaApi.getMechanics() : wait(mechanics);
  },
  createOrder(payload) {
    return integrationMode === "api" ? beerJaApi.createOrder(payload) : wait(mockOrder(payload), 520);
  }
});

export function authPayload(auth) {
  return auth.mode === "register"
    ? { username: auth.name.trim(), email: auth.email.trim().toLowerCase(), password: auth.password, role: auth.role }
    : { loginOrEmail: auth.login.trim(), password: auth.password, role: auth.role, remember: auth.remember };
}

export function orderPayload(order) {
  return {
    eventType: order.eventType,
    audience: order.audience,
    mechanicSlug: order.mechanic,
    eventDate: order.date,
    cityOrVenue: order.city.trim(),
    notes: order.notes.trim() || null,
    budget: order.budget,
    contact: {
      name: order.name.trim(),
      phoneOrTelegram: order.contact.trim(),
      email: order.email.trim().toLowerCase()
    }
  };
}
