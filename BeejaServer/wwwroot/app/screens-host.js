import { assets, finalTeams, gameInfo, roundLeaderboards } from "./data.js";
import { currentCaptain, currentQuestion, currentRound } from "./state-machine.js";
import { button, esc, listRows, metricGrid, phoneShell, questionMedia, stickyAction } from "./ui.js";

function hostMetrics(state) {
  const answered = state.live.answeredTeams;
  return metricGrid([
    { value: answered, label: "ответили", attribute: "answer-count" },
    { value: gameInfo.teams, label: "команд" },
    { value: gameInfo.players, label: "участников" },
    { value: "1", label: "главная кнопка" }
  ]).replace("<div class=\"metric-grid\">", `<div class="metric-grid" data-answer-count="${answered}">`);
}

function hostRows(rows) {
  return `
    <div class="host-stack">
      ${rows.map((row) => `
        <div class="host-line">
          <div><strong>${esc(row.title)}</strong><span>${esc(row.detail)}</span></div>
          <b>${esc(row.value)}</b>
        </div>
      `).join("")}
    </div>
  `;
}

function preflight(state) {
  return `
    <section class="surface solid" data-testid="host-preflight">
      <div class="panel-title"><strong>Всё готово к началу</strong><span>${esc(gameInfo.roomCode)}</span></div>
      ${hostRows([
        { title: "10 из 10 команд в комнате", detail: "56 участников", value: "готово" },
        { title: `Кандидат в капитаны — ${currentCaptain(state).name}`, detail: "закрепится после запуска", value: "голос" },
        { title: "Общий экран подключён", detail: "код комнаты готов к показу", value: "эфир" },
        { title: "После запуска", detail: "команду и капитана сменить нельзя", value: "замок" }
      ])}
    </section>
    ${stickyAction(
      state.team.voteSubmitted ? "10/10" : "9/10",
      state.team.voteSubmitted ? "Начать игру" : "Ждём голос команды",
      "host-next",
      { tone: state.team.voteSubmitted ? "signal" : "", wide: true, disabled: !state.team.voteSubmitted, testId: "host-primary" }
    )}
  `;
}

function roundReady(state) {
  const round = currentRound(state);
  const question = currentQuestion(state);
  return `
    <section class="surface solid" data-testid="host-round-ready">
      <div class="panel-title">
        <strong>Раунд ${state.live.roundIndex + 1} · ${esc(round.title)}</strong>
        <span>вопрос ${state.live.questionIndex + 1} из 3</span>
      </div>
      <div class="surface cloud">
        <strong>${esc(question.title)}</strong>
        ${questionMedia(question, { className: "compact" })}
        <p class="hero-text" style="color: rgba(16,17,22,.62)">На всех экранах вопрос появится одновременно. Отсчёт начнётся после нажатия.</p>
      </div>
      ${hostRows([
        { title: "Тип ответа", detail: question.label, value: "готово" },
        { title: "Время", detail: "единый синхронный отсчёт", value: `${question.duration}с` },
        { title: "Правильный ответ", detail: "скрыт до разбора раунда", value: "скрыт" }
      ])}
    </section>
    ${stickyAction(`${question.duration} секунд`, "Показать вопрос всем", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function questionOpen(state) {
  const live = state.live;
  const timer = live.timer;
  const answered = live.answeredTeams;
  const question = currentQuestion(state);
  return `
    <section class="surface solid" data-testid="host-question-open" data-answer-count="${answered}">
      <div class="panel-title">
        <strong>${timer.paused ? "Отсчёт приостановлен" : "Идёт отсчёт"}</strong>
        <span data-live-answer-copy>Ответили ${answered} из 10 команд</span>
      </div>
      <div class="surface cloud">
        <div class="panel-title"><strong>${esc(question.title)}</strong><span>${esc(question.code)}</span></div>
        ${questionMedia(question, { className: "compact" })}
      </div>
      <div class="surface cloud timer-surface">
        <div class="panel-title"><strong>${String(timer.remaining).padStart(2, "0")} секунд</strong><span>${timer.paused ? "пауза" : "осталось"}</span></div>
        <div class="score-number" data-testid="shared-timer">${String(timer.remaining).padStart(2, "0")}</div>
        ${timer.paused ? "<span>Ответы временно заблокированы ведущим.</span>" : ""}
      </div>
      ${hostRows([
        { title: "Вопрос на всех экранах", detail: "игроки обсуждают, капитаны отвечают", value: "эфир" },
        { title: "Ответили", detail: `${answered} из 10 команд уже отправили вариант`, value: `${answered}/10` },
        { title: "Правильные ответы", detail: "скрыты до завершения трёх вопросов", value: "скрыты" }
      ])}
      ${button(timer.paused ? "resume-timer" : "pause-timer", timer.paused ? "Продолжить отсчёт" : "Поставить на паузу", { className: "secondary-button", testId: "timer-toggle" })}
    </section>
    ${stickyAction(`${answered} из 10`, "Закрыть приём ответов", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function questionClosed(state) {
  const isLast = state.live.questionIndex === 2;
  const answered = state.live.answeredTeams;
  return `
    <section class="surface solid" data-testid="host-question-closed" data-answer-count="${answered}">
      <div class="panel-title"><strong>Приём ответов закрыт</strong><span>${state.live.closedBy === "time" ? "время вышло" : "закрыто вручную"}</span></div>
      ${hostRows([
        { title: `Ответили ${answered} из 10 команд`, detail: "полученные варианты неизменяемы", value: `${answered}/10` },
        { title: "Правильный ответ", detail: isLast ? "можно открыть очередь разбора" : "останется скрытым до третьего вопроса", value: "скрыт" },
        { title: "Следующий шаг", detail: isLast ? "три ответа по очереди" : `вопрос ${state.live.questionIndex + 2} без промежуточного разбора`, value: isLast ? "разбор" : "вопрос" }
      ])}
    </section>
    ${stickyAction(isLast ? "3 из 3" : `${state.live.questionIndex + 1} из 3`, isLast ? "Начать разбор раунда" : "Подготовить следующий вопрос", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function revealQueue(state) {
  const round = currentRound(state);
  const question = round.questions[state.live.revealIndex];
  const correct = Array.isArray(question.correct) ? question.correct.join(", ") : question.correct;
  const isLast = state.live.revealIndex === 2;
  return `
    <section class="surface solid" data-testid="host-reveal">
      <div class="panel-title"><strong>Разбор ${state.live.revealIndex + 1} из 3</strong><span>${esc(question.code)}</span></div>
      <div class="surface cloud">
        <strong>${esc(correct)}</strong>
        ${questionMedia(question, { className: "compact" })}
        <p class="hero-text" style="color: rgba(16,17,22,.62)">${esc(question.explanation)}</p>
      </div>
      ${hostRows([
        { title: "Правильный ответ открыт", detail: "видят игроки и экран зала", value: "эфир" },
        { title: "Баллы", detail: "начислены после закрытого раунда", value: `+${question.points}` }
      ])}
    </section>
    ${stickyAction(`${state.live.revealIndex + 1} из 3`, isLast ? "Показать таблицу команд" : "Открыть следующий ответ", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function leaderboard(state) {
  const board = roundLeaderboards[state.live.roundIndex];
  return `
    <section class="surface solid" data-testid="host-leaderboard">
      <div class="panel-title"><strong>${esc(board.title)}</strong><span>все 10 команд</span></div>
      ${listRows(board.teams.map((team, index) => ({
        rank: team.rank,
        title: team.name,
        detail: `${team.correct} · ${team.time}`,
        value: team.score,
        tone: index === 0 ? "gold" : ""
      })))}
    </section>
    ${stickyAction("таблица", state.live.roundIndex === 2 ? "Перейти к финалу" : "Подготовить следующий раунд", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function finalScreen() {
  return `
    <section class="surface solid" data-testid="host-final">
      <div class="panel-title"><strong>Победитель определён</strong><span>финал</span></div>
      <div class="surface cloud"><strong>«Северный кадр» — 80 баллов</strong><p class="hero-text" style="color: rgba(16,17,22,.62)">Лучшее время среди трёх лидеров с 80 баллами.</p></div>
      ${listRows(finalTeams.slice(0, 5).map((team) => ({
        rank: team.rank,
        title: team.name,
        detail: `${team.correct} · ${team.time}`,
        value: team.score,
        tone: team.rank === 1 ? "gold" : ""
      })))}
    </section>
    ${stickyAction("80", "Завершить игру и открыть отчёт", "host-next", { tone: "signal", testId: "host-primary" })}
  `;
}

function report(state) {
  const roundValues = state.feedback.roundRatings.map((value, index) => value || (state.feedback.roundSkipped[index] ? "пропущено" : "нет оценки"));
  const overall = state.feedback.overallRating || (state.feedback.overallSkipped ? "пропущено" : "нет оценки");
  return `
    <section class="surface solid" data-testid="host-report">
      <div class="panel-title"><strong>Отчёт ведущего</strong><span>после игры</span></div>
      ${hostRows([
        { title: "Правильные ответы", detail: "63 из 90 по всем командам", value: "70%" },
        { title: "Оценки раундов", detail: roundValues.join(" · "), value: "3" },
        { title: "Итоговая оценка вечера", detail: "единая оценка игры, ведущего, организации и площадки", value: overall }
      ])}
    </section>
    ${stickyAction("отчёт", "Открыть аналитику организатора", "open-organizer-analytics", { tone: "cloud", testId: "host-primary" })}
  `;
}

export function renderHost(state) {
  const live = state.live;
  const answered = live.answeredTeams;
  let title = "Всё готово к началу";
  let kicker = gameInfo.title;
  let text = "После запуска капитан и команда закрепятся до конца игры.";
  let timer = "QR";
  let content = preflight(state);

  if (live.gameStarted && (live.phase === "roundIntro" || live.phase === "questionReady")) {
    title = live.phase === "roundIntro" ? `Раунд ${live.roundIndex + 1} готов` : `Вопрос ${live.questionIndex + 1} готов`;
    kicker = currentRound(state).title;
    text = "Следующий вопрос запускает только ведущий.";
    timer = currentQuestion(state).duration;
    content = roundReady(state);
  } else if (live.phase === "questionOpen") {
    title = `Ответили ${answered} из 10 команд`;
    kicker = live.timer.paused ? "Отсчёт на паузе" : "Идёт отсчёт";
    text = "Один синхронный таймер работает на всех ролях.";
    timer = String(live.timer.remaining).padStart(2, "0");
    content = questionOpen(state);
  } else if (live.phase === "questionClosed") {
    title = "Приём ответов закрыт";
    kicker = live.closedBy === "time" ? "Время вышло" : "Закрыто ведущим";
    text = "Верные ответы остаются скрыты до конца третьего вопроса.";
    timer = "0";
    content = questionClosed(state);
  } else if (live.phase === "revealQueue") {
    title = "Разбираем ответы раунда";
    kicker = `Ответ ${live.revealIndex + 1} из 3`;
    text = "Только после трёх вопросов открываются правильные ответы.";
    timer = `${live.revealIndex + 1}/3`;
    content = revealQueue(state);
  } else if (live.phase === "leaderboard") {
    title = "Таблица команд на экранах";
    kicker = `После раунда ${live.roundIndex + 1}`;
    text = "Все 10 команд видят общий порядок одновременно.";
    timer = "10";
    content = leaderboard(state);
  } else if (live.phase === "final") {
    title = "Завершить игру красиво";
    kicker = "Финал";
    text = "Победителя определило время правильных ответов.";
    timer = "80";
    content = finalScreen();
  } else if (live.phase === "report") {
    title = "Киновечер в цифрах";
    kicker = "Отчёт";
    text = "Оценки берутся из фактических действий в прототипе.";
    timer = "4,7";
    content = report(state);
  }

  return phoneShell({
    image: assets.host,
    room: "Ведущий",
    timer,
    title,
    kicker,
    text,
    metrics: hostMetrics(state),
    compact: live.gameStarted,
    content
  });
}
