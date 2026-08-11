import { hostGames } from "./product-data.js";
import { actionButton, esc, routeButton, statusPanel } from "./product-ui.js";

export function renderHostAnalytics(gameId, state) {
  const game = hostGames.find((item) => item.id === gameId && item.status === "completed");
  if (!game) {
    return `<section class="compact-page narrow-page">${statusPanel("Отчёт ещё не готов", "Аналитика появляется после завершения игры и подсчёта результатов.", { error: true, tone: "error" })}${routeButton("/host/account", "Вернуться в кабинет", { tone: "signal" })}</section>`;
  }
  const rounds = [
    { title: "Интро", accuracy: 76, rating: "4,6", answers: 128 },
    { title: "Припев", accuracy: 61, rating: "4,5", answers: 119 },
    { title: "Финал", accuracy: 67, rating: "4,9", answers: 109 }
  ];
  return `
    <section class="analytics-page compact-page">
      <header class="analytics-hero"><div><span class="eyebrow">Отчёт ведущего · ${esc(game.date)}</span><h1>${esc(game.title)}</h1><p>${esc(game.place)} · комната ${esc(game.roomCode)}</p></div><div class="analytics-rating"><strong>${esc(game.rating)}</strong><span>оценка вечера</span></div></header>
      <div class="analytics-metrics"><article><span>Участники</span><strong>${game.players}</strong><small>${game.teams} команд</small></article><article><span>Получено ответов</span><strong>${game.answers}</strong><small>из 432 возможных</small></article><article><span>Точность</span><strong>${game.accuracy}%</strong><small>по всем командам</small></article><article><span>Оценили событие</span><strong>42</strong><small>из ${game.players} участников</small></article></div>
      <section class="analytics-split"><div class="analytics-story"><span class="eyebrow">Главный сигнал</span><h2>Сложный второй раунд не выбил зал из игры.</h2><p>Точность снизилась до 61%, но финальный раунд получил максимальную оценку. Темп ведущего сохранил вовлечение до конца.</p><div class="hardest-question"><span>Самый сложный вопрос</span><strong>${esc(game.hardest)}</strong><b>38% верных ответов</b></div></div><div class="round-bars">${rounds.map((round, index) => `<article><div><span>Раунд ${index + 1}</span><strong>${esc(round.title)}</strong></div><div class="bar-track"><i style="width:${round.accuracy}%"></i></div><div><b>${round.accuracy}%</b><span>${round.answers} ответов · оценка ${esc(round.rating)}</span></div></article>`).join("")}</div></section>
      <section class="analytics-feedback"><div><span class="eyebrow">Обратная связь</span><h2>Что участники сказали после игры.</h2></div><div class="feedback-quotes"><blockquote>«Финал был ровно такой сложности, чтобы спорить всей командой»<cite>Команда «Северный кадр»</cite></blockquote><blockquote>«Добавьте ещё один музыкальный раунд»<cite>Команда «Пятый стол»</cite></blockquote><blockquote>«QR открылся сразу, объяснять вход никому не пришлось»<cite>Организатор площадки</cite></blockquote></div></section>
      <section class="analytics-actions"><div><span class="eyebrow">Передача результата</span><h2>Сводка готова для организатора.</h2><p>JSON/CSV экспорт подключит backend. Сейчас можно скопировать текстовую сводку из mock-отчёта.</p></div>${actionButton("copy-analytics", state.analytics.copied ? "Сводка скопирована" : "Скопировать сводку", { tone: "signal" })}${state.analytics.copied ? statusPanel("Готово", "Основные показатели сохранены в буфер обмена.") : ""}</section>
    </section>
  `;
}

export function analyticsSummary(gameId) {
  const game = hostGames.find((item) => item.id === gameId);
  return game ? `${game.title}: ${game.players} участников, ${game.teams} команд, ${game.answers} ответов, точность ${game.accuracy}%, оценка ${game.rating}.` : "";
}
