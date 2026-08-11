import { gallery, mechanics, productBrand, upcomingEvents } from "./product-data.js";
import { actionButton, esc, field, routeButton, statusPanel } from "./product-ui.js";

function renderMechanicCard(item, index) {
  return `
    <article class="mechanic-card reveal-on-scroll" style="--card-index:${index}">
      <a class="mechanic-media" href="/app/mechanics/${esc(item.slug)}" data-route="/mechanics/${esc(item.slug)}" aria-label="Открыть механику ${esc(item.title)}">
        <img src="${esc(item.image)}" alt="" width="800" height="600" />
        <span>${esc(item.kind)}</span>
      </a>
      <div class="mechanic-copy">
        <div><span>${esc(item.complexity)}</span><strong>${esc(item.price)}</strong></div>
        <h3>${esc(item.title)}</h3>
        <p>${esc(item.description)}</p>
        ${routeButton(`/mechanics/${item.slug}`, "Посмотреть механику", { className: "text-link" })}
      </div>
    </article>
  `;
}

export function renderHome(state) {
  const roomError = state.room.error || "";
  const roomStatus = state.room.status === "loading"
    ? statusPanel("Проверяем комнату", "Сверяем код и статус события.")
    : state.room.status === "not-found"
      ? statusPanel("Комната не найдена", "Проверьте код из приглашения или попросите ведущего показать QR ещё раз.", { error: true, tone: "error" })
      : "";

  return `
    <section class="product-hero">
      <div class="hero-copy reveal-first">
        <span class="eyebrow">${esc(productBrand.eyebrow)}</span>
        <h1>Зал больше не смотрит. <em>Он играет.</em></h1>
        <p>${esc(productBrand.description)}</p>
        <div class="hero-actions">
          ${routeButton("/player/join", "Я игрок", { tone: "signal", testId: "hero-player" })}
          ${routeButton("/host", "Я ведущий", { tone: "cloud", testId: "hero-host" })}
        </div>
      </div>
      <div class="hero-visual reveal-first">
        <img src="/assets/generated/quiz-lobby-night-v1-web.jpg" alt="Команды подключаются к интерактивной игре в зале" width="1200" height="1500" />
        <div class="hero-room-card">
          <span>Сейчас в эфире</span>
          <strong>Киновечер</strong>
          <b>56 участников</b>
        </div>
      </div>
      <form class="room-entry reveal-first" data-product-form="room" novalidate>
        <div>
          <span class="eyebrow">Уже на площадке?</span>
          <h2>Введите код комнаты</h2>
        </div>
        ${field({ id: "room-code", label: "Код из приглашения", value: state.room.code, placeholder: "QR-2048", inputMode: "text", error: roomError, autocomplete: "off" })}
        ${actionButton("join-room", state.room.status === "loading" ? "Проверяем…" : "Найти игру", { tone: "signal", disabled: state.room.status === "loading", testId: "room-submit" })}
        ${roomStatus}
      </form>
    </section>

    <section class="product-section event-section reveal-on-scroll" aria-labelledby="events-title">
      <div class="section-heading">
        <span class="eyebrow">Ближайшие события</span>
        <h2 id="events-title">Выберите вечер, который хочется запомнить.</h2>
      </div>
      <div class="event-stack">
        ${upcomingEvents.map((event, index) => `
          <article class="event-row">
            <div class="event-number">${String(index + 1).padStart(2, "0")}</div>
            <div><span>${esc(event.date)} · ${esc(event.time)}</span><h3>${esc(event.title)}</h3><p>${esc(event.place)}</p></div>
            <div><span>${esc(event.status)}</span>${routeButton("/player/join", "Присоединиться", { className: "text-link" })}</div>
          </article>
        `).join("")}
      </div>
    </section>

    <section class="product-section mechanics-section" aria-labelledby="mechanics-title">
      <div class="section-heading sticky-heading">
        <span class="eyebrow">Готовые механики</span>
        <h2 id="mechanics-title">От короткого голосования до вечера под ключ.</h2>
        <p>Начните с готового формата. Сценарий, визуал и правила можно адаптировать под площадку.</p>
      </div>
      <div class="mechanics-list">
        ${mechanics.map(renderMechanicCard).join("")}
      </div>
    </section>

    <section class="product-section proof-section" aria-labelledby="proof-title">
      <div class="proof-copy reveal-on-scroll">
        <span class="eyebrow">Один ритм на всех экранах</span>
        <h2 id="proof-title">Ведущий задаёт темп. Команды отвечают. Результат появляется у всего зала.</h2>
        ${routeButton("/mechanics/cinema-3x3", "Посмотреть игровой пример", { tone: "cloud" })}
      </div>
      <div class="gallery-track">
        ${gallery.map((item) => `
          <figure class="gallery-frame reveal-on-scroll">
            <img src="${esc(item.src)}" alt="${esc(item.alt)}" width="900" height="1200" />
            <figcaption>${esc(item.caption)}</figcaption>
          </figure>
        `).join("")}
      </div>
    </section>

    <section class="product-cta reveal-on-scroll">
      <div><span class="eyebrow">Для ведущих и event-команд</span><h2>Соберём механику под ваш следующий зал.</h2></div>
      ${routeButton("/order", "Обсудить событие", { tone: "signal" })}
    </section>
  `;
}

export function renderPlayerJoin(state) {
  return `
    <section class="compact-page join-page">
      <div class="page-intro">
        <span class="eyebrow">Вход игрока</span>
        <h1>Найдите свою комнату.</h1>
        <p>Введите код с экрана ведущего или откройте ссылку из QR. До старта команду можно будет поменять.</p>
      </div>
      <form class="focus-form" data-product-form="room" novalidate>
        ${field({ id: "room-code", label: "Код комнаты", value: state.room.code, placeholder: "QR-2048", error: state.room.error, autocomplete: "off" })}
        ${actionButton("join-room", state.room.status === "loading" ? "Проверяем…" : "Продолжить", { tone: "signal", disabled: state.room.status === "loading", testId: "join-room-submit" })}
        <p class="form-hint">Для демонстрации используйте код <button class="inline-code" data-product-action="use-demo-room" type="button">QR-2048</button>.</p>
        ${state.room.status === "not-found" ? statusPanel("Такой комнаты нет", "Проверьте символы и попробуйте ещё раз.", { error: true, tone: "error" }) : ""}
      </form>
      <div class="qr-hint">
        <img src="/assets/generated/quiz-entry-qr-v1.svg" alt="Демонстрационный QR-код комнаты QR-2048" width="240" height="240" />
        <div><strong>Есть QR?</strong><span>Откройте камеру телефона — ссылка сразу приведёт на подтверждение комнаты.</span></div>
      </div>
    </section>
  `;
}

export function renderRoomConfirmation(room) {
  return `
    <section class="compact-page room-confirm-page">
      <div class="room-confirm-media"><img src="/assets/generated/quiz-lobby-night-v1-web.jpg" alt="Зал перед началом игры" width="1200" height="900" /></div>
      <div class="page-intro">
        <span class="eyebrow">Комната найдена</span>
        <h1>${esc(room.title)}</h1>
        <p>${esc(room.startsAt)} · ${esc(room.place)}</p>
      </div>
      <div class="confirm-facts">
        <div><span>Код</span><strong>QR-2048</strong></div>
        <div><span>Статус</span><strong>${room.status === "open" ? "Вход открыт" : "Ожидает ведущего"}</strong></div>
      </div>
      <div class="page-actions">
        ${routeButton("/auth/player", "Войти и присоединиться", { tone: "signal" })}
        ${routeButton("/player/join", "Другой код", { tone: "ghost" })}
      </div>
    </section>
  `;
}
