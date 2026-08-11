import { hostGames, hostProfile } from "./product-data.js";
import { esc, routeButton } from "./product-ui.js";

function hostGameCard(game) {
  const action = game.status === "ready"
    ? `<a class="product-button signal" href="/app/?role=host&host=preflight&demo=0">Открыть пульт</a>`
    : game.status === "draft"
      ? `<a class="product-button cloud" href="/app/?role=organizer&organizer=editor&demo=0">Продолжить настройку</a>`
      : routeButton(`/host/analytics/${game.id}`, "Открыть отчёт", { tone: "ghost" });
  const statusLabel = game.status === "ready" ? "Готова к запуску" : game.status === "draft" ? `Готовность ${game.readiness}%` : "Завершена";
  return `
    <article class="host-game-card ${esc(game.status)}">
      <div class="host-game-status"><span>${esc(statusLabel)}</span><b>${esc(game.roomCode)}</b></div>
      <div class="host-game-copy"><span>${esc(game.date)} · ${esc(game.place)}</span><h3>${esc(game.title)}</h3><p>${esc(game.mechanic)}</p></div>
      <div class="host-game-facts">
        ${game.status === "draft"
          ? `<div class="mini-progress"><i style="width:${game.readiness}%"></i></div><span>Проверьте вопросы и правила комнаты</span>`
          : `<div><strong>${game.teams || 0}</strong><span>команд</span></div><div><strong>${game.players || 0}</strong><span>участников</span></div>`}
      </div>
      ${action}
    </article>
  `;
}

export function renderHostLanding() {
  return `
    <section class="host-landing compact-page">
      <div class="host-landing-copy"><span class="eyebrow">Для ведущих и event-команд</span><h1>Сценарий, зал и результаты держатся в одном ритме.</h1><p>Выберите готовую механику или закажите формат под площадку. BeerJa соберёт участников по QR, синхронизирует экраны и сохранит аналитику.</p><div class="page-actions">${routeButton("/mechanics", "Выбрать механику", { tone: "signal" })}${routeButton("/auth/host", "Войти в кабинет", { tone: "cloud" })}</div></div>
      <div class="host-landing-media"><img src="/assets/generated/quiz-host-control-v1-web.jpg" alt="Ведущий управляет вопросом и таймером" width="1000" height="1200" /><div><span>Пульт ведущего</span><strong>10 команд в эфире</strong><b>09 сек</b></div></div>
    </section>
  `;
}

export function renderHostAccount(options = {}) {
  const empty = options.empty;
  const activeGames = empty ? [] : hostGames.filter((game) => game.status !== "completed");
  const archive = empty ? [] : hostGames.filter((game) => game.status === "completed");
  return `
    <section class="host-account compact-page">
      <header class="host-account-hero">
        <div class="host-avatar">${esc(hostProfile.initials)}</div>
        <div><span class="eyebrow">Кабинет ведущего</span><h1>${esc(hostProfile.name)}</h1><p>${esc(hostProfile.email)}</p></div>
        ${routeButton("/order", "Новая игра", { tone: "signal" })}
      </header>
      <div class="host-account-metrics"><article><strong>${empty ? 0 : hostProfile.completedGames}</strong><span>игр проведено</span></article><article><strong>${empty ? 0 : hostProfile.participants.toLocaleString("ru-RU")}</strong><span>участников</span></article><article><strong>${empty ? "—" : hostProfile.rating}</strong><span>средняя оценка</span></article></div>
      <section class="account-section"><div class="account-heading"><div><span class="eyebrow">В работе</span><h2>${activeGames.length ? "Следующие игры" : "Пока нет активных игр"}</h2></div><span>${activeGames.length}</span></div>${activeGames.length ? `<div class="host-games-grid">${activeGames.map(hostGameCard).join("")}</div>` : `<div class="account-empty"><strong>Создайте первую игру</strong><p>Выберите механику или оставьте заявку на авторский сценарий.</p>${routeButton("/mechanics", "Открыть каталог", { tone: "signal" })}</div>`}</section>
      <section class="account-section"><div class="account-heading"><div><span class="eyebrow">Архив</span><h2>Проведённые события</h2></div><span>${archive.length}</span></div>${archive.length ? `<div class="host-games-grid archive">${archive.map(hostGameCard).join("")}</div>` : `<div class="account-empty"><strong>Архив пуст</strong><p>Завершённые игры и отчёты появятся здесь.</p></div>`}</section>
    </section>
  `;
}
