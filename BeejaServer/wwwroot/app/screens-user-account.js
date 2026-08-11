import { playerAchievements, playerGames, playerProfile } from "./product-data.js";
import { actionButton, esc, field, routeButton, statusPanel } from "./product-ui.js";

function gameCard(game) {
  const upcoming = game.status === "upcoming";
  return `
    <article class="account-game ${upcoming ? "upcoming" : ""}">
      <div class="game-date"><span>${upcoming ? "Ближайшая" : "Завершена"}</span><strong>${esc(game.date)}</strong></div>
      <div class="game-summary"><h3>${esc(game.title)}</h3><p>${esc(game.place)} · команда «${esc(game.team)}»</p></div>
      <div class="game-result">${upcoming ? `<span>Комната</span><strong>${esc(game.roomCode)}</strong>` : `<span>${game.rank} место из ${game.teams}</span><strong>${game.score} баллов</strong>`}</div>
      ${upcoming
        ? `<a class="product-button signal" href="/app/?role=player&player=team&demo=0">Открыть комнату</a>`
        : routeButton(`/player/games/${game.id}`, "Статистика", { tone: "ghost" })}
    </article>
  `;
}

export function renderPlayerAccount(state, options = {}) {
  const empty = options.empty;
  const completed = empty ? [] : playerGames.filter((game) => game.status === "completed");
  const upcoming = empty ? [] : playerGames.find((game) => game.status === "upcoming");
  return `
    <section class="account-page compact-page">
      <header class="account-hero">
        <div class="profile-avatar">${esc(playerProfile.initials)}<span></span></div>
        <div><span class="eyebrow">Профиль игрока</span><h1>${esc(playerProfile.name)}</h1><p>Команда «${esc(playerProfile.team)}» · ${esc(playerProfile.email)}</p></div>
        ${routeButton("/player/settings", "Настроить профиль", { tone: "ghost" })}
      </header>

      <section class="level-panel">
        <div><span>Уровень</span><strong>${playerProfile.level}</strong></div>
        <div class="level-progress"><div><strong>${playerProfile.xp} / ${playerProfile.nextLevelXp} XP</strong><span>До следующего уровня — ${playerProfile.xpToNext}</span></div><div class="progress-track"><i style="width:${Math.round(playerProfile.xp / playerProfile.nextLevelXp * 100)}%"></i></div></div>
        <div class="profile-stat"><strong>${empty ? 0 : playerProfile.sessions}</strong><span>игр</span></div>
        <div class="profile-stat"><strong>${empty ? 0 : playerProfile.totalScore.toLocaleString("ru-RU")}</strong><span>баллов</span></div>
      </section>

      ${upcoming ? `<section class="account-section"><div class="account-heading"><div><span class="eyebrow">Следующая игра</span><h2>Комната уже ждёт.</h2></div></div>${gameCard(upcoming)}</section>` : ""}

      <section class="account-section">
        <div class="account-heading"><div><span class="eyebrow">История</span><h2>${completed.length ? "Ваши игры" : "Здесь появятся результаты"}</h2></div><span>${completed.length} сессии</span></div>
        ${completed.length
          ? `<div class="account-games">${completed.map(gameCard).join("")}</div>`
          : `<div class="account-empty"><strong>Вы ещё не завершили ни одной игры</strong><p>Введите код комнаты или выберите ближайшее открытое событие.</p>${routeButton("/player/join", "Найти комнату", { tone: "signal" })}</div>`}
      </section>

      <section class="account-section">
        <div class="account-heading"><div><span class="eyebrow">Достижения</span><h2>Собираются по ходу игр.</h2></div><span>${empty ? 0 : playerAchievements.length}</span></div>
        ${empty
          ? `<div class="account-empty"><strong>Первое достижение впереди</strong><p>Оно появится после завершённой игры.</p></div>`
          : `<div class="achievement-grid">${playerAchievements.map((item) => `<article><span>${esc(item.state)}</span><h3>${esc(item.title)}</h3><p>${esc(item.detail)}</p></article>`).join("")}</div>`}
      </section>
    </section>
  `;
}

export function renderPlayerGame(game) {
  if (!game || game.status !== "completed") {
    return `<section class="compact-page narrow-page">${statusPanel("Результат не найден", "Игра ещё не завершена или недоступна.", { error: true, tone: "error" })}${routeButton("/player/account", "Вернуться в кабинет", { tone: "signal" })}</section>`;
  }
  return `
    <section class="game-detail compact-page">
      <header class="result-hero">
        <div><span class="eyebrow">${esc(game.date)} · ${esc(game.place)}</span><h1>${esc(game.title)}</h1><p>Команда «${esc(game.team)}» · комната ${esc(game.roomCode)}</p></div>
        <div class="result-place"><strong>${game.rank}</strong><span>место из ${game.teams}</span></div>
      </header>
      <div class="result-metrics">
        <article><span>Счёт игры</span><strong>${game.score}</strong><small>баллов</small></article>
        <article><span>Профиль</span><strong>+${game.xp}</strong><small>XP</small></article>
        <article><span>Точность</span><strong>${game.accuracy}%</strong><small>${game.correct} из ${game.questions}</small></article>
        <article><span>Среднее время</span><strong>${esc(game.averageTime)}</strong><small>на верный ответ</small></article>
      </div>
      <section class="round-breakdown"><div class="account-heading"><div><span class="eyebrow">По раундам</span><h2>Где команда взяла очки.</h2></div></div><div>${game.answers.map((answer, index) => `<article><span>${index + 1}</span><div><strong>${esc(answer.round)}</strong><small>${esc(answer.result)}</small></div><b class="${esc(answer.tone)}">${answer.score}</b></article>`).join("")}</div></section>
      <section class="result-note"><div><span class="eyebrow">Что дальше</span><h2>${game.rank === 1 ? "Победа сохранена в профиле." : "Следующая игра уже в расписании."}</h2><p>Баллы игры и опыт профиля считаются отдельно. Подробные ответы откроются после публикации ведущим.</p></div>${routeButton("/player/account", "В кабинет", { tone: "signal" })}</section>
    </section>
  `;
}

export function renderPlayerSettings(state) {
  return `
    <section class="compact-page settings-page">
      <div class="page-intro"><span class="eyebrow">Настройки профиля</span><h1>Как вас видит команда.</h1><p>Имя используется в комнате и голосовании за капитана.</p></div>
      <form class="focus-form" data-product-form="profile" novalidate>
        ${field({ id: "profile-name", label: "Имя", value: state.profile.name, placeholder: "Майя Волкова", autocomplete: "name", error: state.profile.errors.name })}
        ${field({ id: "profile-email", label: "Email", value: state.profile.email, placeholder: "name@example.ru", type: "email", autocomplete: "email", error: state.profile.errors.email })}
        <fieldset class="avatar-options"><legend>Цвет аватара</legend>${["signal", "pulse", "gold"].map((tone) => `<label><input type="radio" name="avatar-tone" value="${tone}" data-profile-bind="tone" ${state.profile.tone === tone ? "checked" : ""} /><span class="avatar-swatch ${tone}">М</span></label>`).join("")}</fieldset>
        ${actionButton("save-profile", state.profile.status === "loading" ? "Сохраняем…" : "Сохранить изменения", { tone: "signal", disabled: state.profile.status === "loading" })}
        ${state.profile.status === "saved" ? statusPanel("Профиль сохранён", "Новые данные появятся в следующей комнате.") : ""}
      </form>
    </section>
  `;
}
