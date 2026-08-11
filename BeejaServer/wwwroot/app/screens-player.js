import {
  achievements,
  assets,
  availableTeams,
  finalTeams,
  gameInfo,
  history,
  microcopy,
  roundLeaderboards,
  user
} from "./data.js";
import {
  answerIsValid,
  currentAnswer,
  currentCaptain,
  currentQuestion,
  currentTeam,
  currentTeamMembers,
  questionKey
} from "./state-machine.js";
import {
  button,
  choiceGrid,
  esc,
  listRows,
  metricGrid,
  phoneShell,
  questionMedia,
  rail,
  rating,
  stickyAction,
  stickyStatus
} from "./ui.js";

function firstName(name) {
  return String(name || "").split(" ")[0];
}

function accusativeFirstName(name) {
  const forms = {
    "Майя": "Майю",
    "Лев": "Льва",
    "Ника": "Нику",
    "Тимур": "Тимура",
    "Оля": "Олю",
    "Дима": "Диму",
    "Гриша": "Гришу",
    "Лада": "Ладу",
    "Роман": "Романа",
    "Инна": "Инну",
    "Соня": "Соню",
    "Глеб": "Глеба",
    "Ира": "Иру",
    "Антон": "Антона",
    "Вера": "Веру"
  };
  return forms[firstName(name)] || firstName(name);
}

function voteWord(count) {
  if (count % 10 === 1 && count % 100 !== 11) return "голос";
  if ([2, 3, 4].includes(count % 10) && ![12, 13, 14].includes(count % 100)) return "голоса";
  return "голосов";
}

function metrics(state) {
  const live = state.live;
  return metricGrid([
    { value: live.gameStarted ? live.roundIndex + 1 : gameInfo.rounds, label: live.gameStarted ? "текущий раунд" : "раунда" },
    { value: live.gameStarted ? `${live.questionIndex + 1}/3` : gameInfo.questions, label: live.gameStarted ? "вопрос" : "вопросов" },
    { value: gameInfo.teams, label: "команд" },
    { value: live.phase === "questionOpen" ? `${live.timer.remaining}с` : `${gameInfo.timer}с`, label: "на ответ" }
  ]);
}

function authContent(state) {
  if (state.auth.status === "loading") {
    return `
      <section class="surface solid" data-testid="auth-loading">
        <div class="loading-line"><span></span><strong>${esc(microcopy.authLoading)}</strong></div>
        <p class="hero-text">Проверяем тестовый профиль и билет. Данные остаются в этом браузере.</p>
      </section>
      ${stickyStatus("вход", "Подождите несколько секунд")}
    `;
  }

  if (state.auth.status === "error") {
    return `
      <section class="surface solid error-surface" data-testid="auth-error">
        <strong>Не получилось открыть Яндекс ID</strong>
        <p class="hero-text">Соединение прервалось. В демонстрации можно безопасно повторить вход.</p>
      </section>
      ${stickyAction("повтор", "Попробовать снова", "fake-auth", { tone: "signal" })}
    `;
  }

  return `
    <section class="surface solid">
      <div class="panel-title">
        <strong>Вход без настоящей авторизации</strong>
        <span>демонстрация</span>
      </div>
      <p class="hero-text">Профиль хранит уровень, достижения и историю игр. Сейчас используется тестовый аккаунт Майи.</p>
    </section>
    ${stickyAction("Яндекс", "Продолжить с Яндекс ID", "fake-auth", { tone: "signal", testId: "auth-submit" })}
  `;
}

function teamPicker(state) {
  return `
    <section class="surface solid" data-testid="team-picker">
      <div class="panel-title">
        <strong>Выберите свою команду</strong>
        <span>до начала игры</span>
      </div>
      <div class="choice-grid one-column" role="group" aria-label="Выбор команды">
        ${availableTeams.map((team) => `
          <button class="choice-card ${team.id === state.team.teamId ? "selected" : ""}" data-action="change-team" data-value="${esc(team.id)}" aria-pressed="${team.id === state.team.teamId ? "true" : "false"}" type="button">
            <div class="mark">${team.id === state.team.teamId ? "✓" : "+"}</div>
            <strong>${esc(team.name)}</strong>
            <span>${esc(team.detail)}</span>
          </button>
        `).join("")}
      </div>
      ${button("close-team-picker", "Оставить выбранную команду", { className: "secondary-button" })}
    </section>
    ${stickyStatus("до старта", "Команду можно менять")}
  `;
}

function teamContent(state) {
  if (state.ui.teamScenario === "not-found") {
    return `
      <section class="surface solid error-surface" data-testid="team-not-found">
        <strong>Команда по билету не найдена</strong>
        <p class="hero-text">Проверьте тестовый билет КИНО-7421 или выберите команду вручную.</p>
        ${button("open-team-picker", "Выбрать команду", { className: "secondary-button" })}
      </section>
      ${stickyAction("билет", "Повторить поиск", "retry-team-lookup", { tone: "signal" })}
    `;
  }

  if (state.team.pickerOpen) return teamPicker(state);
  const team = currentTeam(state);
  return `
    <section class="surface solid" data-testid="team-found">
      <div class="panel-title">
        <strong>Команда найдена</strong>
        <span>билет ${esc(team.ticket)}</span>
      </div>
      <div class="surface cloud">
        <strong>${esc(team.name)}</strong>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">${esc(team.detail)}. Команду можно сменить до начала игры.</p>
      </div>
      ${button("open-team-picker", "Сменить команду", { className: "secondary-button", testId: "change-team-open" })}
    </section>
    ${stickyAction(`${team.connected} из ${team.capacity}`, "Войти в комнату", "team-found", { tone: "signal" })}
  `;
}

function lobbyContent(state) {
  const locked = state.team.captainLocked;
  const captain = currentCaptain(state);
  const team = currentTeam(state);
  const submitted = state.team.voteSubmitted;
  const candidates = currentTeamMembers(state).map((member) => {
    const votes = member.votes + (submitted && member.id === captain.id ? 1 : 0);
    return {
      id: member.id,
      title: member.name,
      detail: `${votes} ${voteWord(votes)}`,
      selected: member.id === captain.id
    };
  });

  return `
    <section class="surface solid" data-testid="captain-vote">
      <div class="panel-title">
        <strong>Команда «${esc(team.name)}»</strong>
        <span>${locked ? "капитан закреплён" : `${team.connected} из ${team.capacity} участников подключились`}</span>
      </div>
      ${rail(candidates, locked ? "" : "select-captain", captain.id, "small")}
      <div class="surface cloud">
        <div class="panel-title">
          <strong>${locked ? `${esc(captain.name)} — капитан` : submitted ? "Голос учтён" : "Кто будет отвечать за всех?"}</strong>
          <span>${locked ? "до конца игры" : submitted ? `вы выбрали ${esc(firstName(captain.name))}` : `ваш выбор — ${esc(firstName(captain.name))}`}</span>
        </div>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">
          ${locked
            ? `Ответы команды отправляет ${esc(firstName(captain.name))}. Сменить капитана и команду теперь нельзя.`
            : submitted
              ? "До начала игры выбор можно изменить. Сам голос не запускает игру — ждём ведущего."
              : "Вы выбираете кандидата и отдельно подтверждаете голос. Игру запускает только ведущий."}
        </p>
        ${locked
          ? `<div class="lock-note">Команда закреплена после старта</div>`
          : submitted
            ? button("edit-captain-vote", "Изменить голос", { className: "secondary-button" })
            : button("open-team-picker", "Сменить команду", { className: "secondary-button" })}
      </div>
    </section>
    ${locked
      ? stickyStatus("замок", "Ждём вопрос ведущего", { tone: "signal" })
      : submitted
        ? stickyStatus("голос учтён", "Ждём остальных")
        : stickyAction("ваш голос", `Проголосовать за ${accusativeFirstName(captain.name)}`, "submit-captain-vote", { tone: "signal", testId: "captain-vote-submit" })}
  `;
}

function answerContent(state, mode) {
  const question = currentQuestion(state);
  const answer = currentAnswer(state);
  const isCaptain = mode === "captain";
  const phaseOpen = state.live.phase === "questionOpen";
  const open = phaseOpen && !state.live.timer.paused;
  const readonly = !isCaptain || !open || answer.submitted;
  const ready = answerIsValid(question, answer.value);
  const captain = currentCaptain(state);

  let status;
  if (phaseOpen && state.live.timer.paused) status = stickyStatus("пауза", "Ведущий остановил отсчёт");
  else if (!phaseOpen) status = stickyStatus("закрыто", answer.submitted ? "Ответ сохранён" : "Команда не успела");
  else if (!isCaptain) status = stickyStatus(`${state.live.answeredTeams} из 10`, `Ответ выбирает ${firstName(captain.name)}`);
  else if (answer.submitted) status = stickyStatus("замок", "Ответ команды зафиксирован", { tone: "signal" });
  else status = stickyAction(
    `${state.live.answeredTeams} из 10`,
    ready ? "Зафиксировать ответ" : question.type === "multiple" ? "Выберите два варианта" : question.type === "text" ? "Введите ответ" : "Выберите вариант",
    "submit-answer",
    { disabled: !ready, tone: ready ? "signal" : "", testId: "answer-submit" }
  );

  return `
    <section class="surface solid" data-testid="question-surface">
      <div class="panel-title">
        <strong>${esc(question.title)}</strong>
        <span data-answer-count="${state.live.answeredTeams}">Ответили ${state.live.answeredTeams} из 10 команд</span>
      </div>
      ${questionMedia(question)}
      <p class="hero-text">${esc(question.prompt)}</p>
      ${choiceGrid(question, answer.value, "select-answer", readonly)}
      <div class="surface ${answer.submitted ? "cloud" : "solid"}">
        <div class="panel-title">
          <strong>${answer.submitted ? "Ответ команды зафиксирован" : phaseOpen && state.live.timer.paused ? "Отсчёт на паузе" : open ? isCaptain ? "Выберите ответ команды" : `Решение принимает ${esc(firstName(captain.name))}` : "Приём ответов закрыт"}</strong>
          <span>${esc(question.label)}</span>
        </div>
        <p class="hero-text" style="${answer.submitted ? "color: rgba(16,17,22,.62)" : ""}">
          ${answer.submitted ? microcopy.submittedLong : phaseOpen && state.live.timer.paused ? "Выбор временно заблокирован. Он станет доступен после продолжения отсчёта." : !open ? "Верные ответы откроются только после трёх вопросов раунда." : isCaptain ? microcopy.lockWarning : microcopy.noCaptainAction}
        </p>
      </div>
    </section>
    ${status}
  `;
}

function waitingContent(state) {
  const round = state.organizer.draft.rounds[state.live.roundIndex];
  const question = round.questions[state.live.questionIndex];
  return `
    <section class="surface solid">
      <div class="panel-title">
        <strong>${state.live.phase === "roundIntro" ? `Раунд ${state.live.roundIndex + 1} готов` : `Вопрос ${state.live.questionIndex + 1} готов`}</strong>
        <span>${esc(round.title)}</span>
      </div>
      <div class="surface cloud">
        <strong>${esc(question.title)}</strong>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">Вопрос появится одновременно у команды, ведущего и на экране зала.</p>
      </div>
      <div class="lock-note">Команда «${esc(currentTeam(state).name)}» и капитан ${esc(currentCaptain(state).name)} закреплены до конца игры.</div>
    </section>
    ${stickyStatus("ведущий", "Ждём запуска вопроса")}
  `;
}

function revealContent(state) {
  const round = state.organizer.draft.rounds[state.live.roundIndex];
  const question = round.questions[state.live.revealIndex];
  const stored = state.live.answers[questionKey(state, state.live.roundIndex, state.live.revealIndex)];
  const teamAnswer = stored?.unanswered ? "Без ответа" : stored?.value || question.teamAnswer;
  const answerText = Array.isArray(teamAnswer) ? teamAnswer.join(", ") : teamAnswer;
  const correct = Array.isArray(question.correct) ? question.correct.join(", ") : question.correct;
  return `
    <section class="surface solid" data-testid="reveal-queue">
      <div class="panel-title">
        <strong>Разбор ${state.live.revealIndex + 1} из 3</strong>
        <span>после трёх вопросов</span>
      </div>
      ${questionMedia(question, { className: "compact" })}
      <div class="choice-grid">
        <div class="choice-card selected readonly"><div class="mark">✓</div><strong>${esc(correct)}</strong><span>верный ответ</span></div>
        <div class="choice-card readonly"><div class="mark">+</div><strong>${esc(answerText)}</strong><span>ответ команды</span></div>
      </div>
      <div class="surface cloud">
        <strong>${esc(question.explanation)}</strong>
      </div>
    </section>
    ${stickyStatus(`${state.live.revealIndex + 1} из 3`, "Разбор ведёт ведущий")}
  `;
}

function roundFeedback(state) {
  const index = state.live.roundIndex;
  if (state.feedback.roundSubmitted[index] || state.feedback.roundSkipped[index]) {
    return `<div class="surface cloud feedback-thanks"><strong>Спасибо за оценку раунда</strong><span>${state.feedback.roundSkipped[index] ? "Вы пропустили оценку" : `${state.feedback.roundRatings[index]} из 5`}</span></div>`;
  }
  return `
    ${rating(`Оцените раунд «${state.organizer.draft.rounds[index].title}»`, state.feedback.roundRatings[index], "rate-current-round")}
    <div class="inline-actions">
      ${button("submit-round-rating", "Отправить оценку", { className: "secondary-button", disabled: !state.feedback.roundRatings[index] })}
      ${button("skip-round-rating", "Пропустить", { className: "text-button" })}
    </div>
  `;
}

function leaderboardContent(state) {
  const board = roundLeaderboards[state.live.roundIndex];
  const team = currentTeam(state);
  const teamResult = board.teams.find((item) => item.name === team.name) || board.teams[0];
  return `
    <section class="surface solid" data-testid="round-leaderboard">
      <div class="panel-title"><strong>${teamResult.rank === 1 ? "Раунд за вами" : `После раунда — ${teamResult.rank} место`}</strong><span>Все 10 команд</span></div>
      ${listRows(board.teams.map((item) => ({
        rank: item.rank,
        title: item.name,
        detail: `${item.correct} · ${item.time}`,
        value: item.score,
        tone: item.name === team.name ? "gold" : ""
      })))}
      ${roundFeedback(state)}
    </section>
    ${stickyStatus("таблица", "Следующий этап запускает ведущий", { tone: "signal" })}
  `;
}

function finalContent(state) {
  const feedbackDone = state.feedback.overallSubmitted || state.feedback.overallSkipped;
  const team = currentTeam(state);
  const result = finalTeams.find((item) => item.name === team.name) || finalTeams[0];
  const headline = result.rank === 1 ? `«${team.name}» побеждает` : `«${team.name}» — ${result.rank} место`;
  return `
    <section class="surface solid" data-testid="final-feedback">
      <div class="panel-title"><strong>${esc(headline)}</strong><span>${result.score} баллов игры</span></div>
      <div class="surface cloud">
        <div class="score-number">+${user.finalXp}</div>
        <span>опыта профиля отдельно от баллов игры</span>
      </div>
      ${feedbackDone
        ? `<div class="surface cloud feedback-thanks"><strong>Спасибо за отзыв</strong><span>${state.feedback.overallSkipped ? "Оценка пропущена" : `${state.feedback.overallRating} из 5 за весь вечер`}</span></div>`
        : `
          ${rating("Оцените весь вечер целиком", state.feedback.overallRating, "rate-overall")}
          <p class="hero-text">Одна необязательная оценка включает игру, ведущего, организацию и площадку.</p>
          <div class="inline-actions">
            ${button("submit-overall-rating", "Отправить", { className: "secondary-button", disabled: !state.feedback.overallRating })}
            ${button("skip-overall-rating", "Пропустить", { className: "text-button" })}
          </div>
        `}
    </section>
    ${stickyAction(`${result.rank} место`, "Открыть профиль", "open-profile", { tone: "signal" })}
  `;
}

function profileContent(state, guidedTour = false) {
  const team = currentTeam(state);
  const result = finalTeams.find((item) => item.name === team.name) || finalTeams[0];
  const visibleAchievements = state.ui.emptyAchievements ? [] : achievements;
  const visibleHistory = state.ui.emptyHistory
    ? []
    : history.map((item, index) => index === 0
      ? { ...item, place: `${result.rank} из 10`, result: `${result.score} баллов · ${result.correct}` }
      : item);
  return `
    <section class="surface solid" data-testid="profile">
      <div class="panel-title"><strong>${esc(user.name)}</strong><span>Команда «${esc(team.name)}»</span></div>
      <div class="surface cloud">
        <div class="panel-title"><strong>Уровень ${user.level}</strong><span>${user.xp} из ${user.nextLevelXp} опыта</span></div>
        <div class="progress-track"><div style="width:80%"></div></div>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">До 8 уровня — ${user.xpToNext} опыта</p>
      </div>
      <div class="panel-title"><strong>Достижения</strong><span>${visibleAchievements.length}</span></div>
      ${visibleAchievements.length
        ? rail(visibleAchievements.map((item) => ({ id: item.title, title: item.title, detail: item.detail })), "open-achievement", "", "medium")
        : `<div class="empty-state"><strong>Достижений пока нет</strong><span>Первое появится после завершённой игры.</span>${button("restore-profile-data", "Показать примеры", { className: "secondary-button" })}</div>`}
      <div class="panel-title"><strong>История игр</strong><span>${visibleHistory.length}</span></div>
      ${visibleHistory.length
        ? listRows(visibleHistory.map((item) => ({ title: item.title, detail: `${item.date} · ${item.place} · ${item.result}`, value: item.xp })))
        : `<div class="empty-state"><strong>История пока пуста</strong><span>Завершённые игры появятся здесь.</span>${button("restore-profile-data", "Показать примеры", { className: "secondary-button" })}</div>`}
    </section>
    ${guidedTour
      ? stickyStatus("демо завершено", "Профиль открыт", { tone: "signal" })
      : stickyAction("профиль", "Вернуться к финалу", "back-to-final", { tone: "cloud" })}
  `;
}

export function renderPlayer(state, mode = "player", options = {}) {
  const team = currentTeam(state);
  let image = assets.lobby;
  let kicker = "Комната QR-2048";
  let title = `«${team.name}» почти в сборе`;
  let text = "До старта можно выбрать капитана и сменить команду.";
  let timer = "02:14";
  let content = "";
  let compact = false;

  if (state.player.step === "auth") {
    kicker = "Киновечер уже рядом";
    title = "Войдите — и команда подтянется сама";
    text = microcopy.demoNote;
    content = authContent(state);
  } else if (state.player.step === "team") {
    const team = currentTeam(state);
    kicker = state.ui.teamScenario === "not-found" ? "Нужна команда" : "Команда найдена";
    title = state.ui.teamScenario === "not-found" ? "Проверим билет ещё раз" : team.name;
    text = state.ui.teamScenario === "not-found" ? "До старта можно выбрать команду вручную." : team.detail;
    content = teamContent(state);
  } else if (state.player.step === "profile") {
    image = assets.profile;
    kicker = "Уровень и опыт профиля";
    title = "Профиль Майи Волковой";
    text = "Опыт профиля не смешивается с баллами игры.";
    timer = user.level;
    content = profileContent(state, Boolean(options.tour));
    compact = true;
  } else if (!state.live.gameStarted) {
    const captain = currentCaptain(state);
    if (state.team.pickerOpen && !state.team.captainLocked) {
      kicker = "До старта";
      title = "Выберите свою команду";
      text = "Состав и кандидаты в капитаны обновятся сразу после выбора.";
      timer = "↔";
      content = teamPicker(state);
    } else {
      kicker = "До старта";
      title = state.team.captainLocked ? "Команда готова, капитан закреплён" : `${currentTeam(state).name} почти в сборе`;
      text = state.team.captainLocked ? `${firstName(captain.name)} отвечает за команду. Ждём ведущего.` : "Один голос не запускает игру — сначала команда выбирает капитана.";
      timer = state.team.captainLocked ? "✓" : "02:14";
      content = lobbyContent(state);
    }
  } else {
    const live = state.live;
    compact = true;
    if (live.phase === "roundIntro" || live.phase === "questionReady") {
      kicker = `Раунд ${live.roundIndex + 1}`;
      title = live.phase === "roundIntro" ? state.organizer.draft.rounds[live.roundIndex].title : `Готов вопрос ${live.questionIndex + 1}`;
      text = "Вопрос откроется только после команды ведущего.";
      timer = "15";
      content = waitingContent(state);
    } else if (live.phase === "questionOpen" || live.phase === "questionClosed") {
      const answer = currentAnswer(state);
      image = assets.question;
      kicker = `Раунд ${live.roundIndex + 1} · вопрос ${live.questionIndex + 1} из 3`;
      title = live.phase === "questionClosed" ? "Приём ответов закрыт" : answer.submitted ? "Ответ команды зафиксирован" : mode === "captain" ? "Выберите ответ команды" : `Ответ выбирает ${firstName(currentCaptain(state).name)}`;
      text = live.phase === "questionClosed" ? "Верные ответы появятся только после третьего вопроса." : answer.submitted ? microcopy.submittedLong : mode === "captain" ? microcopy.lockWarning : "Обсуждайте ответ вслух. Отправляет только капитан.";
      timer = live.phase === "questionClosed" ? "0" : String(live.timer.remaining).padStart(2, "0");
      content = answerContent(state, mode);
    } else if (live.phase === "revealQueue") {
      image = assets.question;
      kicker = `Раунд ${live.roundIndex + 1} завершён`;
      title = "Разбираем три ответа";
      text = "Правильные ответы открывает ведущий по очереди.";
      timer = `${live.revealIndex + 1}/3`;
      content = revealContent(state);
    } else if (live.phase === "leaderboard") {
      const board = roundLeaderboards[live.roundIndex];
      const teamResult = board.teams.find((item) => item.name === team.name) || board.teams[0];
      image = assets.final;
      kicker = `После раунда ${live.roundIndex + 1}`;
      title = "Таблица команд открыта";
      text = "Все 10 команд видят один и тот же порядок.";
      timer = teamResult.score;
      content = leaderboardContent(state);
    } else {
      const result = finalTeams.find((item) => item.name === team.name) || finalTeams[0];
      image = assets.final;
      kicker = "Игра завершена";
      title = result.rank === 1 ? `«${team.name}» побеждает` : `«${team.name}» — ${result.rank} место`;
      text = result.rank === 1
        ? "У трёх команд по 80 баллов. Победителя определило время."
        : `${result.correct}, ${result.score} баллов. Итоговое место определило время правильных ответов.`;
      timer = String(result.rank);
      content = finalContent(state);
    }
  }

  return phoneShell({
    image,
    timer,
    title,
    kicker,
    text,
    metrics: state.player.step === "auth" || state.player.step === "team" ? "" : metrics(state),
    compact,
    content
  });
}
