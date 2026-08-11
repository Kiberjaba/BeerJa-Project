import { renderAppShell } from "./app-shell.js";
import { mechanics, playerGames, playerProfile, roomFixtures } from "./product-data.js";
import { currentRoute, installRouter, navigate } from "./router.js";
import { renderHome, renderPlayerJoin, renderRoomConfirmation } from "./screens-public-site.js";
import { renderAuth, renderForgotPassword } from "./screens-auth.js";
import { renderPlayerAccount, renderPlayerGame, renderPlayerSettings } from "./screens-user-account.js";
import { renderHostAccount, renderHostLanding } from "./screens-host-account.js";
import { renderMechanicDetail, renderMechanicsCatalog } from "./screens-mechanics.js";
import { renderOrder } from "./screens-order.js";
import { analyticsSummary, renderHostAnalytics } from "./screens-analytics.js";
import { authPayload, orderPayload, productApi } from "./product-contracts.js";
import { esc, routeButton } from "./product-ui.js";

const STORAGE_KEY = "beerja.product.v1";
const initialState = {
  room: { code: "", status: "idle", error: "", selected: null },
  accountRole: "player",
  auth: {
    role: "player",
    mode: "login",
    status: "idle",
    login: "",
    name: "",
    email: "",
    password: "",
    remember: true,
    consent: false,
    errors: {}
  },
  profile: {
    name: playerProfile.name,
    email: playerProfile.email,
    tone: "signal",
    status: "idle",
    errors: {}
  },
  mechanic: {
    previewType: "choice",
    previewValue: "",
    previewText: "",
    previewSubmitted: false
  },
  order: {
    step: 0,
    status: "idle",
    id: "",
    eventType: "",
    audience: "",
    mechanic: "",
    date: "",
    city: "",
    notes: "",
    budget: "",
    name: "",
    contact: "",
    email: "",
    errors: {}
  },
  analytics: {
    copied: false
  }
};

function loadState() {
  try {
    const saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "{}");
    return {
      ...structuredClone(initialState),
      ...saved,
      room: { ...initialState.room, ...(saved.room || {}) },
      auth: { ...initialState.auth, ...(saved.auth || {}), errors: { ...(saved.auth?.errors || {}) } },
      profile: { ...initialState.profile, ...(saved.profile || {}), errors: { ...(saved.profile?.errors || {}) } },
      mechanic: { ...initialState.mechanic, ...(saved.mechanic || {}) },
      order: { ...initialState.order, ...(saved.order || {}), errors: { ...(saved.order?.errors || {}) } },
      analytics: { ...initialState.analytics, ...(saved.analytics || {}) }
    };
  } catch {
    return structuredClone(initialState);
  }
}

let state = loadState();
let lastRoute = "";

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function renderNotFound() {
  return `
    <section class="compact-page not-found-page">
      <span class="eyebrow">Страница не найдена</span>
      <h1>Здесь пока ничего нет.</h1>
      <p>Вернитесь на главную или откройте каталог игровых механик.</p>
      <div class="page-actions">
        ${routeButton("/home", "На главную", { tone: "signal" })}
        ${routeButton("/mechanics", "Механики", { tone: "cloud" })}
      </div>
    </section>
  `;
}

function routeView(route) {
  if (route === "/home") return { title: "BeerJa", content: renderHome(state) };
  if (route === "/player/join") return { title: "Вход игрока", content: renderPlayerJoin(state), backRoute: "/home" };
  if (route === "/player/room") {
    const room = roomFixtures[state.room.selected] || roomFixtures["QR-2048"];
    return { title: "Комната найдена", content: renderRoomConfirmation(room), backRoute: "/player/join" };
  }
  if (route === "/auth/player" || route === "/auth/host") {
    const role = route.endsWith("/host") ? "host" : "player";
    state.auth.role = role;
    state.accountRole = role;
    return { title: role === "host" ? "Вход ведущего" : "Вход игрока", content: renderAuth(state, role), backRoute: role === "host" ? "/host" : state.room.selected ? "/player/room" : "/home", accountRoute: role === "host" ? "/host/account" : "/player/account" };
  }
  if (route === "/auth/forgot") return { title: "Восстановление доступа", content: renderForgotPassword(state), backRoute: `/auth/${state.auth.role || "player"}` };
  if (route === "/player/account") return { title: "Кабинет игрока", content: renderPlayerAccount(state), backRoute: "/home" };
  if (route === "/player/account/empty") return { title: "Новый профиль", content: renderPlayerAccount(state, { empty: true }), backRoute: "/home" };
  if (route === "/player/settings") return { title: "Настройки профиля", content: renderPlayerSettings(state), backRoute: "/player/account" };
  if (route.startsWith("/player/games/")) {
    const gameId = route.split("/").pop();
    return { title: "Статистика игрока", content: renderPlayerGame(playerGames.find((game) => game.id === gameId)), backRoute: "/player/account" };
  }
  if (route === "/host") return { title: "Для ведущих", content: renderHostLanding(), backRoute: "/home", accountRoute: "/host/account" };
  if (route === "/host/account") return { title: "Кабинет ведущего", content: renderHostAccount(), backRoute: "/home", accountRoute: "/host/account" };
  if (route === "/host/account/empty") return { title: "Новый кабинет ведущего", content: renderHostAccount({ empty: true }), backRoute: "/home", accountRoute: "/host/account" };
  if (route.startsWith("/host/analytics/")) {
    const gameId = route.split("/").pop();
    return { title: "Аналитика ведущего", content: renderHostAnalytics(gameId, state), backRoute: "/host/account", accountRoute: "/host/account" };
  }
  if (route === "/mechanics") return { title: "Игровые механики", content: renderMechanicsCatalog(), backRoute: "/home", accountRoute: state.accountRole === "host" ? "/host/account" : "/player/account" };
  if (route.startsWith("/mechanics/")) {
    const slug = route.split("/").pop();
    return { title: mechanics.find((item) => item.slug === slug)?.title || "Механика", content: renderMechanicDetail(mechanics.find((item) => item.slug === slug), state), backRoute: "/mechanics", accountRoute: state.accountRole === "host" ? "/host/account" : "/player/account" };
  }
  if (route === "/order" || route.startsWith("/order/")) {
    const slug = route.split("/")[2];
    if (slug && mechanics.some((item) => item.slug === slug) && state.order.status !== "submitted") state.order.mechanic = slug;
    return { title: "Заказать игру", content: renderOrder(state), backRoute: slug ? `/mechanics/${slug}` : "/mechanics", accountRoute: "/host/account" };
  }
  return { title: "Страница не найдена", content: renderNotFound(), backRoute: "/home" };
}

function installRevealObserver() {
  const items = document.querySelectorAll(".reveal-on-scroll:not(.is-visible)");
  if (!items.length) return;
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches || !("IntersectionObserver" in window)) {
    items.forEach((item) => item.classList.add("is-visible"));
    return;
  }
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      entry.target.classList.add("is-visible");
      observer.unobserve(entry.target);
    });
  }, { threshold: 0.16 });
  items.forEach((item) => observer.observe(item));
}

function render(route = currentRoute()) {
  const view = routeView(route);
  document.title = view.title === "BeerJa" ? "BeerJa — интерактивные мероприятия" : `${view.title} — BeerJa`;
  document.getElementById("app").innerHTML = renderAppShell({ route, ...view });
  if (route !== lastRoute) {
    window.scrollTo(0, 0);
    const main = document.getElementById("product-main");
    window.requestAnimationFrame(() => main?.focus({ preventScroll: true }));
  }
  lastRoute = route;
  installRevealObserver();
}

function emailValid(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(value || "").trim());
}

function validateAuth() {
  const errors = {};
  if (state.auth.mode === "login") {
    if (!state.auth.login.trim()) errors.login = "Введите email или имя пользователя.";
  } else {
    if (state.auth.name.trim().length < 2) errors.name = "Введите имя длиной не менее двух символов.";
    if (!emailValid(state.auth.email)) errors.email = "Введите корректный email.";
    if (!state.auth.consent) errors.consent = "Нужно принять правила сервиса.";
  }
  if (state.auth.password.length < 8) errors.password = "Пароль должен содержать не менее 8 символов.";
  state.auth.errors = errors;
  return !Object.keys(errors).length;
}

function completeAuth() {
  state.auth.status = "success";
  state.auth.errors = {};
  state.auth.password = "";
  saveState();
  navigate(state.auth.role === "host" ? "/host/account" : "/player/account");
}

async function submitAuth() {
  syncAuthFields();
  if (!validateAuth()) {
    state.auth.status = "idle";
    saveState();
    render();
    document.querySelector('[aria-invalid="true"]')?.focus();
    return;
  }
  state.auth.status = "loading";
  saveState();
  render();
  try {
    await productApi[state.auth.mode === "register" ? "register" : "login"](authPayload(state.auth));
    completeAuth();
  } catch (error) {
    state.auth.status = "error";
    state.auth.errors = error?.status === 400 ? { login: "Проверьте логин и пароль." } : {};
    saveState();
    render();
  }
}

function syncAuthFields() {
  const values = {
    login: document.getElementById("auth-login")?.value,
    name: document.getElementById("auth-name")?.value,
    email: document.getElementById("auth-email")?.value,
    password: document.getElementById("auth-password")?.value
  };
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined) state.auth[key] = value;
  });
}

function submitForgot() {
  const email = document.getElementById("forgot-email")?.value || state.auth.email;
  state.auth.email = email;
  state.auth.errors = emailValid(email) ? {} : { email: "Введите корректный email." };
  if (state.auth.errors.email) {
    render();
    document.getElementById("forgot-email")?.focus();
    return;
  }
  state.auth.status = "loading";
  render();
  window.setTimeout(() => {
    state.auth.status = "sent";
    saveState();
    render();
  }, 420);
}

function saveProfile() {
  const name = document.getElementById("profile-name")?.value || "";
  const email = document.getElementById("profile-email")?.value || "";
  const errors = {};
  if (name.trim().length < 2) errors.name = "Введите имя длиной не менее двух символов.";
  if (!emailValid(email)) errors.email = "Введите корректный email.";
  state.profile = { ...state.profile, name, email, errors };
  if (Object.keys(errors).length) {
    render();
    document.querySelector('[aria-invalid="true"]')?.focus();
    return;
  }
  state.profile.status = "loading";
  render();
  window.setTimeout(() => {
    state.profile.status = "saved";
    saveState();
    render();
  }, 420);
}

function resetPreview(type = state.mechanic.previewType) {
  state.mechanic.previewType = type;
  state.mechanic.previewValue = "";
  state.mechanic.previewText = "";
  state.mechanic.previewSubmitted = false;
  saveState();
  render();
}

function syncOrderFields() {
  ["date", "city", "notes", "name", "contact", "email"].forEach((key) => {
    const input = document.getElementById(`order-${key}`);
    if (input) state.order[key] = input.value;
  });
}

function validateOrderStep(step) {
  const errors = {};
  if (step === 0 && (!state.order.eventType || !state.order.audience)) errors.basics = "Выберите тип события и размер аудитории.";
  if (step === 1 && !state.order.mechanic) errors.mechanic = "Выберите механику или вечер под ключ.";
  if (step === 2) {
    if (!state.order.date) errors.date = "Укажите ориентировочную дату.";
    if (state.order.city.trim().length < 2) errors.city = "Укажите город или площадку.";
    if (!state.order.budget) errors.details = "Выберите ориентир бюджета.";
  }
  if (step === 3) {
    if (state.order.name.trim().length < 2) errors.name = "Введите имя контактного лица.";
    if (state.order.contact.trim().length < 5) errors.contact = "Введите телефон или Telegram.";
    if (!emailValid(state.order.email)) errors.email = "Введите корректный email.";
  }
  state.order.errors = errors;
  return !Object.keys(errors).length;
}

function orderNext() {
  syncOrderFields();
  if (!validateOrderStep(state.order.step)) {
    saveState();
    render();
    document.querySelector('[aria-invalid="true"], .field-error')?.focus?.();
    return;
  }
  state.order.errors = {};
  state.order.step = Math.min(4, state.order.step + 1);
  saveState();
  render();
}

async function orderSubmit() {
  state.order.status = "loading";
  saveState();
  render();
  try {
    const response = await productApi.createOrder(orderPayload(state.order));
    state.order.status = "submitted";
    state.order.id = response.id;
    saveState();
    render();
  } catch {
    state.order.status = "error";
    saveState();
    render();
  }
}

function validateRoom(code) {
  const normalized = String(code || "").trim().toUpperCase().replace(/\s+/g, "-");
  if (!normalized) return { normalized, error: "Введите код комнаты." };
  if (!/^[A-ZА-Я0-9-]{4,16}$/u.test(normalized)) return { normalized, error: "Код содержит 4–16 букв, цифр или дефис." };
  return { normalized, error: "" };
}

async function joinRoom() {
  const input = document.getElementById("room-code");
  const { normalized, error } = validateRoom(input?.value || state.room.code);
  state.room.code = normalized;
  state.room.error = error;
  if (error) {
    state.room.status = "idle";
    saveState();
    render();
    document.getElementById("room-code")?.focus();
    return;
  }
  state.room.status = "loading";
  saveState();
  render();
  try {
    await productApi.getRoomByCode(normalized);
    state.room.status = "found";
    state.room.selected = normalized;
    state.room.error = "";
    saveState();
    navigate("/player/room");
  } catch (error) {
    state.room.status = "not-found";
    state.room.selected = null;
    state.room.error = error?.status === 404 ? "" : "Не удалось проверить комнату. Повторите попытку.";
    saveState();
    render();
    document.getElementById("room-code")?.focus();
  }
}

document.addEventListener("click", (event) => {
  const target = event.target.closest("[data-product-action]");
  if (!target) return;
  event.preventDefault();
  if (target.dataset.productAction === "join-room") joinRoom();
  if (target.dataset.productAction === "auth-mode") {
    state.auth.mode = target.dataset.value;
    state.auth.status = "idle";
    state.auth.errors = {};
    saveState();
    render();
  }
  if (target.dataset.productAction === "submit-auth") submitAuth();
  if (target.dataset.productAction === "yandex-auth") {
    state.auth.status = "loading";
    state.auth.errors = {};
    saveState();
    render();
    productApi.beginYandexLogin().then((response) => {
      if (response) completeAuth();
    }).catch(() => {
      state.auth.status = "error";
      saveState();
      render();
    });
  }
  if (target.dataset.productAction === "forgot-password") navigate("/auth/forgot");
  if (target.dataset.productAction === "submit-forgot") submitForgot();
  if (target.dataset.productAction === "save-profile") saveProfile();
  if (target.dataset.productAction === "preview-type") resetPreview(target.dataset.value);
  if (target.dataset.productAction === "preview-select") {
    state.mechanic.previewValue = target.dataset.value;
    state.mechanic.previewSubmitted = false;
    saveState();
    render();
  }
  if (target.dataset.productAction === "preview-submit") {
    if (state.mechanic.previewType === "text" ? state.mechanic.previewText.trim() : state.mechanic.previewValue) {
      state.mechanic.previewSubmitted = true;
      saveState();
      render();
    }
  }
  if (target.dataset.productAction === "preview-again") resetPreview();
  if (target.dataset.productAction === "order-select") {
    state.order[target.dataset.field] = target.dataset.value;
    state.order.errors = {};
    state.order.status = "idle";
    saveState();
    render();
  }
  if (target.dataset.productAction === "order-next") orderNext();
  if (target.dataset.productAction === "order-back") {
    syncOrderFields();
    state.order.step = Math.max(0, state.order.step - 1);
    state.order.errors = {};
    saveState();
    render();
  }
  if (target.dataset.productAction === "order-submit") orderSubmit();
  if (target.dataset.productAction === "copy-analytics") {
    const gameId = currentRoute().split("/").pop();
    const summary = analyticsSummary(gameId);
    Promise.resolve(navigator.clipboard?.writeText?.(summary)).catch(() => null).finally(() => {
      state.analytics.copied = true;
      saveState();
      render();
    });
  }
  if (target.dataset.productAction === "use-demo-room") {
    state.room.code = "QR-2048";
    state.room.error = "";
    saveState();
    render();
    document.getElementById("room-code")?.focus();
  }
});

document.addEventListener("input", (event) => {
  if (event.target.id === "room-code") {
    state.room.code = event.target.value.toUpperCase();
    state.room.error = "";
    state.room.status = "idle";
    saveState();
  }
  if (event.target.id?.startsWith("auth-")) {
    const key = event.target.id.replace("auth-", "");
    state.auth[key] = event.target.value;
    state.auth.errors[key] = "";
    saveState();
  }
  if (event.target.id === "forgot-email") state.auth.email = event.target.value;
  if (event.target.id === "profile-name") state.profile.name = event.target.value;
  if (event.target.id === "profile-email") state.profile.email = event.target.value;
  if (event.target.matches("[data-preview-text]")) {
    state.mechanic.previewText = event.target.value;
    const submit = document.querySelector('[data-product-action="preview-submit"]');
    if (submit) submit.disabled = !event.target.value.trim();
  }
  if (event.target.id?.startsWith("order-")) {
    const key = event.target.id.replace("order-", "");
    state.order[key] = event.target.value;
    state.order.errors[key] = "";
    state.order.status = "idle";
  }
  saveState();
});

document.addEventListener("change", (event) => {
  if (event.target.dataset.authBind === "remember") state.auth.remember = event.target.checked;
  if (event.target.dataset.authBind === "consent") {
    state.auth.consent = event.target.checked;
    state.auth.errors.consent = "";
  }
  if (event.target.dataset.profileBind === "tone") state.profile.tone = event.target.value;
  saveState();
});

document.addEventListener("submit", (event) => {
  if (!event.target.matches('[data-product-form="room"]')) return;
  event.preventDefault();
  joinRoom();
});

document.addEventListener("submit", (event) => {
  if (event.target.matches('[data-product-form="auth"]')) {
    event.preventDefault();
    submitAuth();
  }
  if (event.target.matches('[data-product-form="forgot"]')) {
    event.preventDefault();
    submitForgot();
  }
  if (event.target.matches('[data-product-form="profile"]')) {
    event.preventDefault();
    saveProfile();
  }
  if (event.target.matches('[data-product-form="order"]')) {
    event.preventDefault();
    if (state.order.step === 4) orderSubmit();
    else orderNext();
  }
});

installRouter(render);
