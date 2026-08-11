import { assets, finalTeams, gameInfo, roundLeaderboards } from "./data.js";
import { currentQuestion, currentRound } from "./state-machine.js";
import { esc, listRows, questionMedia } from "./ui.js";

function publicLeaderboard(state) {
  const board = roundLeaderboards[state.live.roundIndex];
  return `
    <div class="public-leader-grid" data-testid="public-leaderboard">
      ${board.teams.map((team) => `
        <div class="public-team-row ${team.rank === 1 ? "gold" : ""}">
          <b>${team.rank}</b>
          <strong>${esc(team.name)}</strong>
          <span>${team.score}</span>
        </div>
      `).join("")}
    </div>
  `;
}

function publicStateContent(state) {
  const live = state.live;
  const round = currentRound(state);
  const question = currentQuestion(state);

  if (!live.gameStarted) {
    return {
      image: assets.public,
      kicker: `${gameInfo.title} · вход по коду`,
      title: "Сканируйте код и найдите свою команду.",
      side: `
        <div class="qr-box">
          <img class="qr-image" src="${esc(assets.entryQr)}" alt="QR-код для входа в игру" width="1024" height="1024" />
          <div class="qr-caption">
            <strong>${esc(gameInfo.roomCode)}</strong>
            <span>Наведите камеру телефона</span>
          </div>
        </div>
        ${listRows([
          { title: "Команд", detail: "в комнате", value: "10" },
          { title: "Участников", detail: "подключаются после покупки", value: "56" },
          { title: "До старта", detail: "ведущий готовит первый кадр", value: "02:14" }
        ])}
      `,
      detail: "Капитан и команда закрепятся только после запуска ведущим."
    };
  }

  if (live.phase === "roundIntro" || live.phase === "questionReady") {
    return {
      image: assets.lobby,
      kicker: `Раунд ${live.roundIndex + 1} · ${esc(round.title)}`,
      title: live.phase === "roundIntro" ? "Команды готовы. Начинаем новый раунд." : `Следующий — вопрос ${live.questionIndex + 1}.`,
      side: `
        <div class="surface cloud">
          <strong>${esc(question.title)}</strong>
          <p class="hero-text" style="color: rgba(16,17,22,.62)">Отсчёт начнётся одновременно на всех экранах.</p>
        </div>
      `,
      detail: "Правильные ответы откроются только после третьего вопроса."
    };
  }

  if (live.phase === "questionOpen" || live.phase === "questionClosed") {
    const closed = live.phase === "questionClosed";
    const paused = live.phase === "questionOpen" && live.timer.paused;
    return {
      image: assets.question,
      kicker: `Раунд ${live.roundIndex + 1} · вопрос ${live.questionIndex + 1} · ${closed ? "приём закрыт" : paused ? "отсчёт на паузе" : `${String(live.timer.remaining).padStart(2, "0")} секунд`}`,
      title: question.title,
      media: questionMedia(question, { className: "public-question-media" }),
      side: `
        <div class="choice-grid" data-testid="public-question">
          ${(question.answers || []).map((answer) => `
            <div class="choice-card readonly">
              <div class="mark">+</div>
              <strong>${esc(answer)}</strong>
              <span>${closed ? "ответы сохранены" : "вариант ответа"}</span>
            </div>
          `).join("") || `<div class="surface cloud"><strong>Короткий ответ</strong><p class="hero-text" style="color: rgba(16,17,22,.62)">Капитан вводит ответ на своём телефоне.</p></div>`}
        </div>
      `,
      detail: closed
        ? "Правильный ответ пока скрыт. Ведущий продолжит раунд."
        : paused
          ? "Ведущий приостановил отсчёт. Ответы временно заблокированы."
          : `Ответили ${live.answeredTeams} из 10 команд. Отправляет только капитан.`,
      timer: closed ? 0 : live.timer.remaining
    };
  }

  if (live.phase === "revealQueue") {
    const revealQuestion = round.questions[live.revealIndex];
    const correct = Array.isArray(revealQuestion.correct) ? revealQuestion.correct.join(", ") : revealQuestion.correct;
    return {
      image: assets.question,
      kicker: `Разбор ${live.revealIndex + 1} из 3`,
      title: correct,
      media: questionMedia(revealQuestion, { className: "public-question-media" }),
      side: `
        <div class="surface cloud" data-testid="public-reveal">
          <strong>${esc(revealQuestion.explanation)}</strong>
          <p class="hero-text" style="color: rgba(16,17,22,.62)">Ответы команд были зафиксированы до начала разбора.</p>
        </div>
      `,
      detail: "Три правильных ответа открываются по очереди после завершения раунда."
    };
  }

  if (live.phase === "leaderboard") {
    const board = roundLeaderboards[live.roundIndex];
    return {
      image: assets.final,
      kicker: `Таблица после раунда ${live.roundIndex + 1}`,
      title: board.title,
      side: publicLeaderboard(state),
      detail: "Все 10 команд видят один и тот же порядок."
    };
  }

  return {
    image: assets.final,
    kicker: "Финал",
    title: "«Северный кадр» побеждает по времени.",
    side: listRows(finalTeams.slice(0, 3).map((team) => ({
      rank: team.rank,
      title: team.name,
      detail: `${team.correct} · ${team.time}`,
      value: team.score,
      tone: team.rank === 1 ? "gold" : ""
    }))),
    detail: "У трёх команд по 80 баллов. Победителя определило лучшее время правильных ответов."
  };
}

export function renderPublic(state) {
  const data = publicStateContent(state);
  return `
    <section class="public-shell" style="--public-image: url('${esc(data.image)}')" aria-label="Экран зала">
      <div class="public-content">
        <div class="public-panel">
          <div class="kicker">${esc(data.kicker)}</div>
          <h1 class="public-title" tabindex="-1">${esc(data.title)}</h1>
          ${data.media || ""}
          <p class="hero-text" data-testid="public-detail">${esc(data.detail)}</p>
          ${data.timer !== undefined ? `<div class="public-timer" data-testid="shared-timer">${String(data.timer).padStart(2, "0")}</div>` : ""}
        </div>
        <div class="public-panel">${data.side}</div>
      </div>
    </section>
  `;
}
