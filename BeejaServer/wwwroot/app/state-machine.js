import { availableTeams, gameInfo, rounds, teamRosters } from "./data.js";

export const storageKey = "live-signal-full-prototype-v2";

function clone(value) {
  return structuredClone(value);
}

function normalizeQuestion(question) {
  return {
    ...clone(question),
    duration: Number(question.duration || gameInfo.timer),
    hasImage: Boolean(question.imageAlt),
    answers: [...(question.answers || [])],
    correct: Array.isArray(question.correct) ? [...question.correct] : question.correct
  };
}

export function createDraft() {
  return {
    title: gameInfo.title,
    rules: {
      teamMin: 5,
      teamMax: 6,
      timer: gameInfo.timer,
      points: 10,
      captainOnly: true,
      immutableAnswers: true,
      revealAfterRound: true
    },
    rounds: rounds.map((round) => ({
      ...clone(round),
      questions: round.questions.map(normalizeQuestion)
    }))
  };
}

export function createInitialState() {
  return {
    activeRole: "player",
    toast: "",
    tour: {
      active: false,
      startedAt: null
    },
    auth: {
      status: "idle"
    },
    team: {
      teamId: "north",
      pickerOpen: false,
      captainId: "maya",
      voteSubmitted: false,
      captainLocked: false
    },
    player: {
      step: "auth"
    },
    live: {
      gameStarted: false,
      phase: "lobby",
      roundIndex: 0,
      questionIndex: 0,
      revealIndex: 0,
      answeredTeams: 0,
      answers: {},
      closedBy: "",
      timer: {
        duration: gameInfo.timer,
        remaining: gameInfo.timer,
        running: false,
        paused: false,
        endsAt: null
      }
    },
    organizer: {
      step: "home",
      templateId: "cinema",
      roundIndex: 0,
      questionIndex: 0,
      draft: createDraft(),
      validation: {},
      creating: false,
      savedAt: null
    },
    feedback: {
      roundRatings: [null, null, null],
      roundSubmitted: [false, false, false],
      roundSkipped: [false, false, false],
      overallRating: null,
      overallSubmitted: false,
      overallSkipped: false
    },
    ui: {
      sheet: null,
      resetConfirm: false,
      exitConfirm: false,
      authScenario: "success",
      teamScenario: "found",
      emptyHistory: false,
      emptyAchievements: false
    }
  };
}

function mergeDeep(base, saved) {
  if (Array.isArray(base)) return Array.isArray(saved) ? clone(saved) : clone(base);
  if (!base || typeof base !== "object") return saved === undefined ? base : saved;

  const result = clone(base);
  for (const [key, value] of Object.entries(saved || {})) {
    if (value && typeof value === "object" && !Array.isArray(value) && result[key] && typeof result[key] === "object" && !Array.isArray(result[key])) {
      result[key] = mergeDeep(result[key], value);
    } else {
      result[key] = clone(value);
    }
  }
  return result;
}

export function loadState() {
  const initial = createInitialState();
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey));
    const hydrated = saved ? mergeDeep(initial, saved) : initial;
    const templateQuestions = new Map(
      initial.organizer.draft.rounds.flatMap((round) => round.questions).map((question) => [question.code, question])
    );
    hydrated.organizer.draft.rounds.forEach((round) => {
      round.questions.forEach((question) => {
        const template = templateQuestions.get(question.code);
        if (!question.image && template?.image) question.image = template.image;
        if (!question.imageAlt && template?.imageAlt) question.imageAlt = template.imageAlt;
      });
    });
    return hydrated;
  } catch {
    return initial;
  }
}

export function saveState(state) {
  const persistent = clone(state);
  delete persistent.activeRole;
  persistent.toast = "";
  persistent.ui.sheet = null;
  persistent.ui.resetConfirm = false;
  persistent.ui.exitConfirm = false;
  localStorage.setItem(storageKey, JSON.stringify(persistent));
}

export function currentTeam(state) {
  return availableTeams.find((team) => team.id === state.team.teamId) || availableTeams[0];
}

export function currentTeamMembers(state) {
  return teamRosters[state.team.teamId] || teamRosters.north;
}

export function currentCaptain(state) {
  const members = currentTeamMembers(state);
  return members.find((member) => member.id === state.team.captainId) || members[0];
}

export function currentRound(state) {
  return state.organizer.draft.rounds[state.live.roundIndex] || state.organizer.draft.rounds[0];
}

export function currentQuestion(state) {
  const round = currentRound(state);
  return round.questions[state.live.questionIndex] || round.questions[0];
}

export function questionKey(state, roundIndex = state.live.roundIndex, questionIndex = state.live.questionIndex) {
  return `${roundIndex + 1}-${questionIndex + 1}`;
}

export function currentAnswer(state) {
  return state.live.answers[questionKey(state)] || {
    value: currentQuestion(state).type === "multiple" ? [] : "",
    submitted: false,
    unanswered: false
  };
}

export function answerIsValid(question, value) {
  if (question.type === "multiple") {
    return Array.isArray(value) && value.length === Number(question.required || 2);
  }
  return String(value || "").trim().length > 0;
}

export function ensureAnswer(state) {
  const key = questionKey(state);
  if (!state.live.answers[key]) {
    state.live.answers[key] = {
      value: currentQuestion(state).type === "multiple" ? [] : "",
      submitted: false,
      unanswered: false
    };
  }
  return state.live.answers[key];
}

export function startGame(state) {
  state.team.captainLocked = true;
  state.team.voteSubmitted = true;
  state.team.pickerOpen = false;
  state.player.step = "live";
  state.live.gameStarted = true;
  state.live.phase = "roundIntro";
  state.live.roundIndex = 0;
  state.live.questionIndex = 0;
  state.live.revealIndex = 0;
  state.live.answeredTeams = 0;
  resetTimer(state);
}

export function resetTimer(state) {
  const duration = Number(currentQuestion(state).duration || state.organizer.draft.rules.timer || gameInfo.timer);
  state.live.timer = {
    duration,
    remaining: duration,
    running: false,
    paused: false,
    endsAt: null
  };
}

export function startQuestion(state, now = Date.now()) {
  resetTimer(state);
  const timer = state.live.timer;
  timer.running = true;
  timer.endsAt = now + timer.remaining * 1000;
  state.live.phase = "questionOpen";
  state.live.closedBy = "";
  state.live.answeredTeams = 0;
  ensureAnswer(state);
}

export function closeQuestion(state, reason = "host") {
  if (state.live.phase !== "questionOpen") return false;
  const answer = ensureAnswer(state);
  if (!answer.submitted) answer.unanswered = true;
  state.live.timer.running = false;
  state.live.timer.paused = false;
  state.live.timer.endsAt = null;
  state.live.phase = "questionClosed";
  state.live.closedBy = reason;
  if (reason === "time") state.live.timer.remaining = 0;
  return true;
}

export function reconcileTimer(state, now = Date.now()) {
  const timer = state.live.timer;
  if (state.live.phase !== "questionOpen" || !timer.running || !timer.endsAt) return false;

  const nextRemaining = Math.max(0, Math.ceil((timer.endsAt - now) / 1000));
  let changed = nextRemaining !== timer.remaining;
  timer.remaining = nextRemaining;

  const elapsed = timer.duration - timer.remaining;
  const mockAnswered = Math.min(9, Math.max(state.live.answeredTeams, Math.floor(elapsed / 2) + 2));
  if (mockAnswered !== state.live.answeredTeams) {
    state.live.answeredTeams = mockAnswered;
    changed = true;
  }

  if (nextRemaining <= 0) {
    closeQuestion(state, "time");
    changed = true;
  }
  return changed;
}

export function pauseTimer(state, now = Date.now()) {
  if (state.live.phase !== "questionOpen" || !state.live.timer.running) return false;
  reconcileTimer(state, now);
  if (state.live.phase !== "questionOpen") return false;
  state.live.timer.running = false;
  state.live.timer.paused = true;
  state.live.timer.endsAt = null;
  return true;
}

export function resumeTimer(state, now = Date.now()) {
  const timer = state.live.timer;
  if (state.live.phase !== "questionOpen" || !timer.paused || timer.remaining <= 0) return false;
  timer.running = true;
  timer.paused = false;
  timer.endsAt = now + timer.remaining * 1000;
  return true;
}

export function advanceHost(state) {
  const live = state.live;
  if (!live.gameStarted || live.phase === "lobby") {
    if (!state.team.voteSubmitted) return "Сначала команда должна подтвердить голос за капитана.";
    startGame(state);
    return "Игра началась. Капитаны закреплены.";
  }

  if (live.phase === "roundIntro" || live.phase === "questionReady") {
    startQuestion(state);
    return `Вопрос ${live.questionIndex + 1} открыт на всех экранах.`;
  }

  if (live.phase === "questionOpen") {
    closeQuestion(state, "host");
    return "Приём ответов закрыт.";
  }

  if (live.phase === "questionClosed") {
    if (live.questionIndex < 2) {
      live.questionIndex += 1;
      live.phase = "questionReady";
      live.answeredTeams = 0;
      resetTimer(state);
      return `Следующий вопрос подготовлен. Ответы раунда пока скрыты.`;
    }
    live.phase = "revealQueue";
    live.revealIndex = 0;
    return "Начался разбор трёх вопросов раунда.";
  }

  if (live.phase === "revealQueue") {
    if (live.revealIndex < 2) {
      live.revealIndex += 1;
      return `Открыт ответ ${live.revealIndex + 1} из 3.`;
    }
    live.phase = "leaderboard";
    return "Таблица раунда открыта на всех экранах.";
  }

  if (live.phase === "leaderboard") {
    if (live.roundIndex < 2) {
      live.roundIndex += 1;
      live.questionIndex = 0;
      live.revealIndex = 0;
      live.phase = "roundIntro";
      live.answeredTeams = 0;
      resetTimer(state);
      return `Раунд ${live.roundIndex + 1} готов к запуску.`;
    }
    live.phase = "final";
    return "Финал открыт на всех экранах.";
  }

  if (live.phase === "final") {
    live.phase = "report";
    return "Игра завершена, отчёт готов.";
  }

  return "Текущий этап уже завершён.";
}

export function validateQuestion(question) {
  const errors = {};
  if (!String(question.title || "").trim()) errors.title = "Введите текст вопроса.";
  if (!String(question.prompt || "").trim()) errors.prompt = "Добавьте короткую подсказку для игроков.";
  if (!String(question.explanation || "").trim()) errors.explanation = "Добавьте пояснение для разбора.";
  if (!Number.isFinite(Number(question.duration)) || Number(question.duration) < 5) errors.duration = "Время должно быть не меньше 5 секунд.";
  if (question.hasImage && !String(question.imageAlt || "").trim()) errors.imageAlt = "Опишите изображение для доступности.";

  const answers = (question.answers || []).map((answer) => String(answer || "").trim()).filter(Boolean);
  if (question.type === "single") {
    if (answers.length < 2) errors.answers = "Добавьте минимум два варианта.";
    if (!answers.includes(String(question.correct || "").trim())) errors.correct = "Выберите правильный вариант из списка.";
  } else if (question.type === "multiple") {
    const correct = Array.isArray(question.correct) ? question.correct : [];
    if (answers.length < 3) errors.answers = "Добавьте минимум три варианта.";
    if (correct.length !== Number(question.required || 2) || correct.some((answer) => !answers.includes(answer))) {
      errors.correct = `Отметьте ровно ${Number(question.required || 2)} правильных варианта.`;
    }
  } else if (question.type === "text") {
    if (!String(question.correct || "").trim()) errors.correct = "Введите правильный короткий ответ.";
  } else {
    errors.type = "Выберите тип вопроса.";
  }
  return errors;
}

export function validateRules(rules) {
  const errors = {};
  const values = {
    teamMin: Number(rules?.teamMin),
    teamMax: Number(rules?.teamMax),
    timer: Number(rules?.timer),
    points: Number(rules?.points)
  };

  if (!Number.isInteger(values.teamMin) || values.teamMin < 2 || values.teamMin > 10) {
    errors.teamMin = "Минимум игроков должен быть целым числом от 2 до 10.";
  }
  if (!Number.isInteger(values.teamMax) || values.teamMax < 2 || values.teamMax > 10) {
    errors.teamMax = "Максимум игроков должен быть целым числом от 2 до 10.";
  }
  if (!errors.teamMin && !errors.teamMax && values.teamMin > values.teamMax) {
    errors.rules = "Минимальный состав не может быть больше максимального.";
  }
  if (!Number.isInteger(values.timer) || values.timer < 5 || values.timer > 60) {
    errors.timer = "Время ответа должно быть целым числом от 5 до 60 секунд.";
  }
  if (!Number.isInteger(values.points) || values.points < 1 || values.points > 100) {
    errors.points = "Баллы за ответ должны быть целым числом от 1 до 100.";
  }
  return errors;
}

export function validateDraft(draft) {
  const errors = [];
  if (!String(draft.title || "").trim()) errors.push("Введите название игры.");
  Object.values(validateRules(draft.rules)).forEach((error) => errors.push(error));
  if (!draft.rules?.captainOnly || !draft.rules?.immutableAnswers || !draft.rules?.revealAfterRound) {
    errors.push("Обязательные правила капитана, фиксации и разбора после раунда должны быть включены.");
  }

  draft.rounds.forEach((round, roundIndex) => {
    if (!String(round.title || "").trim()) errors.push(`Раунд ${roundIndex + 1}: введите название.`);
    if (round.questions.length !== 3) errors.push(`Раунд ${roundIndex + 1}: должно быть три вопроса.`);
    round.questions.forEach((question, questionIndex) => {
      const questionErrors = validateQuestion(question);
      if (Object.keys(questionErrors).length) {
        errors.push(`Раунд ${roundIndex + 1}, вопрос ${questionIndex + 1}: ${Object.values(questionErrors)[0]}`);
      }
    });
  });
  return errors;
}
