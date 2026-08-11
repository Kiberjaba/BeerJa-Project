import {
  achievements,
  finalTeams,
  hostStates,
  organizerStates,
  playerStates,
  publicStates,
  roles
} from "./data.js";
import { renderHost } from "./screens-host.js";
import { renderOrganizer } from "./screens-organizer.js";
import { validateCurrentEditorQuestion } from "./screens-organizer-editor.js";
import { renderPlayer } from "./screens-player.js";
import { renderPublic } from "./screens-public.js";
import {
  advanceHost,
  answerIsValid,
  closeQuestion,
  createDraft,
  createInitialState,
  currentAnswer,
  currentCaptain,
  currentQuestion,
  currentTeamMembers,
  ensureAnswer,
  loadState,
  pauseTimer,
  reconcileTimer,
  resetTimer,
  resumeTimer,
  saveState,
  validateDraft,
  validateRules
} from "./state-machine.js";
import {
  renderTourBar,
  tourAutomation,
  tourAutomationKey,
  tourMode
} from "./tour.js";
import { button, esc } from "./ui.js";
import {
  beginIntegration,
  emitStateChanged,
  emitUiAction
} from "./backend-bridge.js";

let state = loadState();
let toastTimer = null;
let tourAutomationTimer = null;
let scheduledTourKey = "";
let lastRenderedTourIdentity = null;
let lastRenderedFocusIdentity = null;
applyRouteParams();
reconcileTimer(state);
if (tourMode()) {
  if ("scrollRestoration" in window.history) window.history.scrollRestoration = "manual";
  saveState(state);
}

function setToast(message) {
  state.toast = message;
  window.clearTimeout(toastTimer);
  toastTimer = window.setTimeout(() => {
    state.toast = "";
    saveState(state);
    document.querySelectorAll(".toast-message").forEach((element) => element.remove());
  }, 1800);
}

function livePhaseFromStep(step) {
  const map = {
    preflight: "lobby",
    round: "roundIntro",
    question: "questionOpen",
    locked: "questionClosed",
    reveal: "revealQueue",
    leaderboard: "leaderboard",
    final: "final",
    report: "report",
    welcome: "lobby"
  };
  return map[step] || "lobby";
}

function publicTourMode() {
  return new URLSearchParams(window.location.search).get("publictour") === "1";
}

function livePhaseFromPublicStep(step) {
  const map = {
    welcome: "lobby",
    lobby: "roundIntro",
    question: "questionOpen",
    reveal: "revealQueue",
    leaderboard: "leaderboard",
    final: "final"
  };
  return map[step] || "lobby";
}

function activateLiveState(phase) {
  state.live.gameStarted = phase !== "lobby";
  state.team.captainLocked = phase !== "lobby";
  state.player.step = phase === "lobby" ? "lobby" : "live";
  state.live.phase = phase;
  if (phase === "questionOpen") {
    resetTimer(state);
    state.live.timer.remaining = 9;
    state.live.timer.running = false;
    state.live.timer.paused = true;
  } else {
    state.live.timer.running = false;
    state.live.timer.endsAt = null;
  }
}

function activatePublicDemoState(step, illustrative = false) {
  activateLiveState(livePhaseFromPublicStep(step));
  if (illustrative && step === "question") {
    state.live.timer.remaining = 15;
    state.live.timer.running = false;
    state.live.timer.paused = false;
    state.live.timer.endsAt = null;
    state.live.answeredTeams = 6;
  }
}

function applyRouteParams() {
  const params = new URLSearchParams(window.location.search);
  if (params.get("clear") === "1") state = createInitialState();
  const guidedPublic = publicTourMode();

  const role = params.get("role");
  if (role && roles.some((item) => item.id === role)) state.activeRole = role;
  if (guidedPublic) state.activeRole = "public";

  const round = Number(params.get("round"));
  const question = Number(params.get("q") || params.get("question"));
  if (round >= 1 && round <= 3) {
    state.live.roundIndex = round - 1;
    state.organizer.roundIndex = round - 1;
  }
  if (question >= 1 && question <= 3) {
    state.live.questionIndex = question - 1;
    state.organizer.questionIndex = question - 1;
  }

  const organizerStep = params.get("organizer");
  if (organizerStep && organizerStates.some((item) => item.id === organizerStep)) {
    state.organizer.step = organizerStep;
  }

  const playerStep = params.get("player");
  if (playerStep) {
    if (["auth", "team", "lobby", "profile"].includes(playerStep)) {
      state.player.step = playerStep;
    } else {
      const map = {
        question: "questionOpen",
        submitted: "questionOpen",
        reveal: "revealQueue",
        roundResults: "leaderboard",
        final: "final"
      };
      if (map[playerStep]) activateLiveState(map[playerStep]);
    }
  }

  const hostStep = params.get("host");
  const publicStep = params.get("public");
  if (hostStep) activateLiveState(livePhaseFromStep(hostStep));
  if (publicStep) activatePublicDemoState(publicStep, guidedPublic || params.get("demo") === "1");

  if (params.get("locked") === "1") {
    state.team.captainLocked = true;
    state.team.voteSubmitted = true;
  }

  const answered = Number(params.get("answered"));
  if (answered >= 0 && answered <= 10) state.live.answeredTeams = answered;

  if (params.get("answer") === "correct") {
    const questionData = currentQuestion(state);
    const answer = ensureAnswer(state);
    answer.value = Array.isArray(questionData.correct) ? [...questionData.correct] : questionData.correct;
  }

  if (params.get("submitted") === "1" || playerStep === "submitted") {
    const answer = ensureAnswer(state);
    answer.submitted = true;
    state.live.answeredTeams = Math.max(8, state.live.answeredTeams);
  }

  if (params.get("tour") === "1") {
    state.tour.active = true;
    state.tour.startedAt ||= new Date().toISOString();
    state.activeRole = state.live.gameStarted ? "captain" : "player";
  }

  if ((params.get("tour") === "1" || guidedPublic) && params.get("clear") === "1") {
    params.delete("clear");
    if (guidedPublic) params.delete("public");
    const query = params.toString();
    window.history.replaceState(null, "", `${window.location.pathname}${query ? `?${query}` : ""}`);
  }
}

function setDemoStep(value) {
  if (state.activeRole === "organizer") {
    state.organizer.step = value;
    return;
  }

  if (state.activeRole === "host") {
    activateLiveState(livePhaseFromStep(value));
    return;
  }

  if (state.activeRole === "public") {
    activatePublicDemoState(value, true);
    return;
  }

  if (["auth", "team", "lobby", "profile"].includes(value)) {
    state.player.step = value;
    if (value !== "profile") {
      state.live.gameStarted = false;
      state.live.phase = "lobby";
    }
    return;
  }

  const map = {
    question: "questionOpen",
    submitted: "questionOpen",
    reveal: "revealQueue",
    roundResults: "leaderboard",
    final: "final"
  };
  activateLiveState(map[value] || "lobby");
  if (value === "submitted") ensureAnswer(state).submitted = true;
}

function demoBar() {
  const stateSets = {
    player: playerStates,
    captain: playerStates,
    organizer: organizerStates,
    host: hostStates,
    public: publicStates,
    overview: []
  };
  const activeSet = stateSets[state.activeRole] || [];
  const activeStep = state.activeRole === "organizer"
    ? state.organizer.step
    : state.activeRole === "host"
      ? hostStepFromPhase()
      : state.activeRole === "public"
        ? publicStepFromPhase()
        : state.player.step;

  return `
    <section class="demo-bar" aria-label="Навигация демо">
      <div class="demo-row" role="tablist" aria-label="Роль в демо">
        ${roles.map((role) => `
          <button class="demo-button ${state.activeRole === role.id ? "active" : ""}" data-action="set-role" data-value="${esc(role.id)}" role="tab" aria-selected="${state.activeRole === role.id ? "true" : "false"}" type="button">${esc(role.label)}</button>
        `).join("")}
      </div>
      <div class="demo-row" role="tablist" aria-label="Состояние роли">
        ${activeSet.map((item) => `
          <button class="demo-button ${activeStep === item.id ? "active" : ""}" data-action="set-demo-step" data-value="${esc(item.id)}" role="tab" aria-selected="${activeStep === item.id ? "true" : "false"}" type="button">${esc(item.label)}</button>
        `).join("")}
      </div>
      <div class="demo-row">
        ${button("happy-path", "Следующий шаг", { className: "demo-button" })}
        ${button("demo-auth-error", "Ошибка входа", { className: "demo-button" })}
        ${button("demo-team-error", "Команда не найдена", { className: "demo-button" })}
        ${button("demo-empty-profile", "Пустой профиль", { className: "demo-button" })}
        ${button("reset-demo", "Сброс", { className: "demo-button" })}
      </div>
    </section>
  `;
}

function publicTourBar() {
  const activeStep = publicStepFromPhase();
  const activeIndex = Math.max(0, publicStates.findIndex((item) => item.id === activeStep));
  const nextLabel = activeIndex === publicStates.length - 1 ? "Начать заново" : "Следующий экран";
  return `
    <section class="demo-bar public-tour-bar" data-testid="public-tour-bar" aria-label="Навигация флоу общего экрана">
      <div class="public-tour-heading">
        <span>Общий экран · 16:9</span>
        <strong>Шаг ${activeIndex + 1} из ${publicStates.length}</strong>
      </div>
      <div class="demo-row public-tour-steps" role="tablist" aria-label="Состояние общего экрана">
        ${publicStates.map((item) => `
          <button class="demo-button ${activeStep === item.id ? "active" : ""}" data-action="set-demo-step" data-value="${esc(item.id)}" role="tab" aria-selected="${activeStep === item.id ? "true" : "false"}" type="button">${esc(item.label)}</button>
        `).join("")}
      </div>
      ${button("happy-path", nextLabel, { className: "demo-button public-tour-next", testId: "public-tour-next" })}
    </section>
  `;
}

function advancePublicTour() {
  const activeStep = publicStepFromPhase();
  const activeIndex = Math.max(0, publicStates.findIndex((item) => item.id === activeStep));
  const nextIndex = (activeIndex + 1) % publicStates.length;
  const next = publicStates[nextIndex];
  setDemoStep(next.id);
  return nextIndex === 0 ? "Флоу общего экрана начат заново" : `Открыт экран «${next.label}»`;
}

function hostStepFromPhase() {
  const map = {
    lobby: "preflight",
    roundIntro: "round",
    questionReady: "round",
    questionOpen: "question",
    questionClosed: "locked",
    revealQueue: "reveal",
    leaderboard: "leaderboard",
    final: "final",
    report: "report"
  };
  return map[state.live.phase] || "preflight";
}

function publicStepFromPhase() {
  const map = {
    lobby: "welcome",
    roundIntro: "lobby",
    questionReady: "lobby",
    questionOpen: "question",
    questionClosed: "question",
    revealQueue: "reveal",
    leaderboard: "leaderboard",
    final: "final",
    report: "final"
  };
  return map[state.live.phase] || "welcome";
}

function renderOverview() {
  const playerState = structuredClone(state);
  playerState.activeRole = "player";
  activateCloneLive(playerState, "questionOpen", 6);

  const captainState = structuredClone(state);
  captainState.activeRole = "captain";
  activateCloneLive(captainState, "questionOpen", 8);
  const captainAnswer = ensureAnswer(captainState);
  captainAnswer.value = currentQuestion(captainState).type === "multiple"
    ? [...currentQuestion(captainState).correct]
    : currentQuestion(captainState).correct;
  captainAnswer.submitted = true;

  const organizerState = structuredClone(state);
  organizerState.organizer.step = "editor";

  const hostState = structuredClone(state);
  activateCloneLive(hostState, "questionOpen", 7);

  const profileState = structuredClone(state);
  profileState.player.step = "profile";

  const publicState = structuredClone(state);
  activateCloneLive(publicState, "leaderboard", 10);

  return `
    <section class="overview-grid" aria-label="Обзор всех ролей">
      <div class="overview-phone">${renderPlayer(playerState, "player")}</div>
      <div class="overview-phone">${renderPlayer(captainState, "captain")}</div>
      <div class="overview-phone">${renderOrganizer(organizerState)}</div>
      <div class="overview-phone">${renderHost(hostState)}</div>
      <div class="overview-phone">${renderPlayer(profileState, "player")}</div>
      <div class="overview-public">${renderPublic(publicState)}</div>
    </section>
  `;
}

function activateCloneLive(target, phase, answered) {
  target.live.gameStarted = true;
  target.live.phase = phase;
  target.live.answeredTeams = answered;
  target.live.timer.remaining = 9;
  target.live.timer.running = false;
  target.live.timer.paused = true;
  target.team.captainLocked = true;
  target.player.step = "live";
}

function renderStage() {
  if (tourMode()) return renderPlayer(state, state.live.gameStarted ? "captain" : "player", { tour: true });
  if (state.activeRole === "player") return renderPlayer(state, "player");
  if (state.activeRole === "captain") return renderPlayer(state, "captain");
  if (state.activeRole === "organizer") return renderOrganizer(state);
  if (state.activeRole === "host") return renderHost(state);
  if (state.activeRole === "public") return renderPublic(state);
  return renderOverview();
}

function renderSheet() {
  const sheet = state.ui.sheet;
  if (!sheet) return "";

  if (sheet.type === "achievement") {
    const item = achievements.find((achievement) => achievement.title === sheet.id);
    if (!item) return "";
    return `
      <div class="sheet-backdrop" data-action="close-sheet">
        <section class="detail-sheet" role="dialog" aria-modal="true" aria-label="Достижение">
          <div class="mark">✓</div>
          <h2>${esc(item.title)}</h2>
          <p>${esc(item.detail)}</p>
          <span>${esc(item.status)}</span>
          ${button("close-sheet", "Понятно", { className: "primary-button signal" })}
        </section>
      </div>
    `;
  }

  const team = finalTeams.find((item) => item.name === sheet.id);
  if (!team) return "";
  return `
    <div class="sheet-backdrop" data-action="close-sheet">
      <section class="detail-sheet" role="dialog" aria-modal="true" aria-label="Команда">
        <h2>${esc(team.name)}</h2>
        <p>${team.players} участников · ${esc(team.correct)} · ${team.score} баллов</p>
        <span>Суммарное время правильных ответов: ${esc(team.time)}</span>
        ${button("close-sheet", "Закрыть", { className: "primary-button signal" })}
      </section>
    </div>
  `;
}

function renderResetConfirm() {
  if (!state.ui.resetConfirm) return "";
  return `
    <div class="sheet-backdrop">
      <section class="confirm-dialog" role="alertdialog" aria-modal="true" aria-label="Сбросить демонстрацию">
        <h2>Сбросить демонстрацию?</h2>
        <p>Локальный прогресс, ответы и изменения редактора вернутся к исходным данным.</p>
        <div class="inline-actions">
          ${button("cancel-reset", "Отмена", { className: "secondary-button" })}
          ${button("confirm-reset", "Сбросить", { className: "primary-button signal" })}
        </div>
      </section>
    </div>
  `;
}

function tourViewIdentity() {
  return [
    state.player.step,
    state.live.phase,
    state.live.roundIndex,
    state.live.questionIndex,
    state.live.revealIndex,
    state.team.pickerOpen ? "picker" : "team",
    state.team.teamId
  ].join(":");
}

function focusViewIdentity() {
  return [
    tourMode() ? "tour" : state.activeRole,
    state.player.step,
    state.organizer.step,
    state.live.phase,
    state.live.roundIndex,
    state.live.questionIndex,
    state.live.revealIndex,
    state.ui.sheet?.type || "",
    state.ui.resetConfirm ? "reset" : ""
  ].join(":");
}

function focusDescriptor(element) {
  if (!element || element === document.body || element === document.documentElement) return null;
  if (!document.getElementById("app")?.contains(element)) return null;

  const descriptor = {
    tag: element.tagName,
    action: element.dataset.action || "",
    value: element.dataset.value || "",
    bind: element.dataset.bind || "",
    testId: element.dataset.testid || "",
    id: element.id || "",
    ariaLabel: element.getAttribute("aria-label") || ""
  };
  const peers = [...document.querySelectorAll(element.tagName)].filter((candidate) =>
    (!descriptor.action || candidate.dataset.action === descriptor.action)
    && (!descriptor.value || candidate.dataset.value === descriptor.value)
    && (!descriptor.bind || candidate.dataset.bind === descriptor.bind)
    && (!descriptor.testId || candidate.dataset.testid === descriptor.testId)
    && (!descriptor.id || candidate.id === descriptor.id)
    && (!descriptor.ariaLabel || candidate.getAttribute("aria-label") === descriptor.ariaLabel)
  );
  descriptor.index = Math.max(0, peers.indexOf(element));
  if (typeof element.selectionStart === "number") {
    descriptor.selectionStart = element.selectionStart;
    descriptor.selectionEnd = element.selectionEnd;
    descriptor.selectionDirection = element.selectionDirection;
  }
  return descriptor;
}

function findFocusTarget(descriptor) {
  if (!descriptor) return null;
  const peers = [...document.querySelectorAll(descriptor.tag)].filter((candidate) =>
    (!descriptor.action || candidate.dataset.action === descriptor.action)
    && (!descriptor.value || candidate.dataset.value === descriptor.value)
    && (!descriptor.bind || candidate.dataset.bind === descriptor.bind)
    && (!descriptor.testId || candidate.dataset.testid === descriptor.testId)
    && (!descriptor.id || candidate.id === descriptor.id)
    && (!descriptor.ariaLabel || candidate.getAttribute("aria-label") === descriptor.ariaLabel)
  );
  return peers[descriptor.index] || peers[0] || null;
}

function restoreFocus(descriptor) {
  const target = findFocusTarget(descriptor);
  if (!target || target.disabled || target.getAttribute("aria-disabled") === "true") return false;
  target.focus({ preventScroll: true });
  if (typeof descriptor.selectionStart === "number" && typeof target.setSelectionRange === "function") {
    target.setSelectionRange(
      Math.min(descriptor.selectionStart, target.value.length),
      Math.min(descriptor.selectionEnd, target.value.length),
      descriptor.selectionDirection || "none"
    );
  }
  return document.activeElement === target;
}

function focusViewContext() {
  const target = document.querySelector('[role="dialog"], [role="alertdialog"]')
    || document.querySelector(".stage .hero-title")
    || document.querySelector(".stage .public-title")
    || document.querySelector(".stage h1")
    || document.querySelector(".stage main");
  if (!target) return;
  if (!target.hasAttribute("tabindex")) target.setAttribute("tabindex", "-1");
  target.focus({ preventScroll: true });
}

function render() {
  const guidedTour = tourMode();
  const guidedPublic = publicTourMode();
  const showDemo = new URLSearchParams(window.location.search).get("demo") === "1" && !guidedTour && !guidedPublic;
  const previousFocus = focusDescriptor(document.activeElement);
  const nextTourIdentity = guidedTour ? tourViewIdentity() : null;
  const nextFocusIdentity = focusViewIdentity();
  const tourIdentityChanged = guidedTour && nextTourIdentity !== lastRenderedTourIdentity;
  const focusIdentityChanged = nextFocusIdentity !== lastRenderedFocusIdentity;
  document.getElementById("app").innerHTML = `
    <main class="demo-root ${showDemo || guidedPublic ? "" : "demo-clean"} ${guidedTour ? "tour-root" : ""} ${guidedPublic ? "public-tour-root" : ""}">
      ${guidedTour ? renderTourBar(state) : guidedPublic ? publicTourBar() : showDemo ? demoBar() : ""}
      <section class="stage">${renderStage()}</section>
      ${state.toast ? `<div class="toast-message" role="status">${esc(state.toast)}</div>` : ""}
      ${renderSheet()}
      ${renderResetConfirm()}
    </main>
  `;
  if (tourIdentityChanged) window.scrollTo(0, 0);
  if (focusIdentityChanged) {
    focusViewContext();
  } else if (previousFocus && !restoreFocus(previousFocus)) {
    focusViewContext();
  }
  if (tourIdentityChanged) {
    window.scrollTo(0, 0);
    window.requestAnimationFrame(() => window.scrollTo(0, 0));
  }
  lastRenderedTourIdentity = nextTourIdentity;
  lastRenderedFocusIdentity = nextFocusIdentity;
  scheduleTourAutomation();
}

function updateLiveTick() {
  const remaining = String(state.live.timer.remaining).padStart(2, "0");
  const answered = state.live.answeredTeams;
  const activeLivePhone = tourMode() || ["player", "captain", "host"].includes(state.activeRole);

  if (activeLivePhone) {
    const shellTimer = document.querySelector('[data-testid="shell-timer"]');
    if (shellTimer) shellTimer.textContent = remaining;
  }

  document.querySelectorAll('[data-testid="shared-timer"]').forEach((element) => {
    element.textContent = remaining;
  });

  document.querySelectorAll("[data-answer-count]").forEach((element) => {
    element.dataset.answerCount = String(answered);
    if (element.tagName === "SPAN" && element.textContent.trim().startsWith("Ответили")) {
      element.textContent = `Ответили ${answered} из 10 команд`;
    }
  });

  if (document.querySelector('[data-testid="question-surface"]')) {
    const stickyMeta = document.querySelector(".phone-shell .sticky-action .sticky-meta");
    if (stickyMeta && /\d+\s+из\s+10/.test(stickyMeta.textContent)) {
      stickyMeta.textContent = `${answered} из 10`;
    }
  }

  const hostSurface = document.querySelector('[data-testid="host-question-open"]');
  if (hostSurface) {
    const heroTitle = document.querySelector(".phone-shell .hero-title");
    if (heroTitle) heroTitle.textContent = `Ответили ${answered} из 10 команд`;
    const panelCopy = hostSurface.querySelector("[data-live-answer-copy]");
    if (panelCopy) panelCopy.textContent = `Ответили ${answered} из 10 команд`;
    const timerTitle = hostSurface.querySelector(".timer-surface .panel-title strong");
    if (timerTitle) timerTitle.textContent = `${remaining} секунд`;
    const answerRow = [...hostSurface.querySelectorAll(".host-line")]
      .find((row) => row.querySelector("strong")?.textContent.trim() === "Ответили");
    if (answerRow) {
      const detail = answerRow.querySelector("span");
      const value = answerRow.querySelector("b");
      if (detail) detail.textContent = `${answered} из 10 команд уже отправили вариант`;
      if (value) value.textContent = `${answered}/10`;
    }
    const stickyMeta = document.querySelector(".phone-shell .sticky-action .sticky-meta");
    if (stickyMeta) stickyMeta.textContent = `${answered} из 10`;
    const metric = document.querySelector('.metric-grid[data-answer-count] .metric-chip strong');
    if (metric) metric.textContent = String(answered);
  }

  const publicShell = document.querySelector(".public-shell");
  if (publicShell) {
    const kicker = publicShell.querySelector(".kicker");
    if (kicker) kicker.textContent = `Раунд ${state.live.roundIndex + 1} · вопрос ${state.live.questionIndex + 1} · ${remaining} секунд`;
    const detail = publicShell.querySelector('[data-testid="public-detail"]');
    if (detail) detail.textContent = `Ответили ${answered} из 10 команд. Отправляет только капитан.`;
  }
}

function runTourAutomation(action) {
  if (!tourMode() || !state.tour.active) return;
  let message = "";
  if (action === "start-game") {
    message = advanceHost(state);
    state.activeRole = "captain";
  } else if (action === "start-question") {
    message = advanceHost(state);
  } else if (action === "close-question") {
    if (closeQuestion(state, "host")) message = "Ведущий принял ответ команды.";
  } else if (action === "advance-after-close") {
    message = advanceHost(state);
  }
  if (message) setToast(message);
  saveState(state);
  render();
  emitStateChanged(state, `tour:${action}`);
}

function scheduleTourAutomation() {
  if (!tourMode()) {
    window.clearTimeout(tourAutomationTimer);
    tourAutomationTimer = null;
    scheduledTourKey = "";
    return;
  }

  const action = tourAutomation(state);
  if (!action) {
    window.clearTimeout(tourAutomationTimer);
    tourAutomationTimer = null;
    scheduledTourKey = "";
    return;
  }

  const key = tourAutomationKey(state, action);
  if (tourAutomationTimer && scheduledTourKey === key) return;
  window.clearTimeout(tourAutomationTimer);
  scheduledTourKey = key;
  const delay = action === "start-game" ? 900 : action === "close-question" ? 750 : 650;
  tourAutomationTimer = window.setTimeout(() => {
    tourAutomationTimer = null;
    scheduledTourKey = "";
    runTourAutomation(action);
  }, delay);
}

function selectAnswer(value) {
  if (state.activeRole !== "captain" || state.live.phase !== "questionOpen" || state.live.timer.paused) return;
  const question = currentQuestion(state);
  const answer = ensureAnswer(state);
  if (answer.submitted) return;

  if (question.type === "multiple") {
    const current = Array.isArray(answer.value) ? answer.value : [];
    const limit = Number(question.required || 2);
    answer.value = current.includes(value)
      ? current.filter((item) => item !== value)
      : current.length < limit
        ? [...current, value]
        : [...current.slice(1), value];
  } else {
    answer.value = value;
  }
}

function nextOrganizer() {
  const order = ["home", "templates", "editor", "rules", "preview", "review", "session", "analytics"];
  const currentIndex = order.indexOf(state.organizer.step);
  if (state.organizer.step === "editor") {
    const errors = validateCurrentEditorQuestion(state);
    state.organizer.validation = errors;
    if (Object.keys(errors).length) {
      setToast("Исправьте вопрос перед переходом к правилам");
      return false;
    }
  }
  state.organizer.step = order[Math.min(order.length - 1, currentIndex + 1)];
  return true;
}

function updateEditorBinding(target) {
  const bind = target.dataset.bind;
  const round = state.organizer.draft.rounds[state.organizer.roundIndex];
  const question = round.questions[state.organizer.questionIndex];
  const value = target.type === "number" ? Number(target.value) : target.value;

  if (bind === "draft-round-title") round.title = target.value;
  else if (bind === "draft-question-title") question.title = target.value;
  else if (bind === "draft-question-prompt") question.prompt = target.value;
  else if (bind === "draft-question-explanation") question.explanation = target.value;
  else if (bind === "draft-question-duration") question.duration = value;
  else if (bind === "draft-image-alt") question.imageAlt = target.value;
  else if (bind?.startsWith("draft-answer-")) {
    const index = Number(bind.split("-").pop());
    while (question.answers.length <= index) question.answers.push("");
    const oldValue = question.answers[index];
    question.answers[index] = target.value;
    if (question.type === "single" && question.correct === oldValue) question.correct = target.value;
    if (question.type === "multiple" && Array.isArray(question.correct)) {
      question.correct = question.correct.map((item) => item === oldValue ? target.value : item);
    }
  } else if (bind === "draft-correct-text" || bind === "draft-correct-single") question.correct = target.value;
  else if (bind === "draft-question-type") {
    question.type = target.value;
    if (question.type === "text") {
      question.answers = [];
      question.correct = "";
      delete question.required;
    } else {
      const defaults = ["Вариант 1", "Вариант 2", "Вариант 3", "Вариант 4"];
      question.answers = question.answers?.length ? question.answers : defaults;
      if (question.type === "multiple") {
        question.required = 2;
        question.correct = question.answers.slice(0, 2);
      } else {
        delete question.required;
        question.correct = question.answers[0];
      }
    }
  } else if (bind === "draft-has-image") {
    question.hasImage = target.checked;
    if (target.checked && !question.imageAlt) question.imageAlt = "Кино-кадр для вопроса.";
  } else if (bind === "draft-correct-option") {
    const answerValue = target.dataset.value;
    const current = Array.isArray(question.correct) ? question.correct : [];
    question.correct = target.checked ? [...current, answerValue] : current.filter((item) => item !== answerValue);
  } else if (bind === "draft-team-min") state.organizer.draft.rules.teamMin = value;
  else if (bind === "draft-team-max") state.organizer.draft.rules.teamMax = value;
  else if (bind === "draft-default-timer") state.organizer.draft.rules.timer = value;
  else if (bind === "draft-points") state.organizer.draft.rules.points = value;
  saveState(state);
}

function handleAction(action, value) {
  emitUiAction(action, value, state);
  if (action === "tour-restart") {
    state = createInitialState();
    state.tour.active = true;
    state.tour.startedAt = new Date().toISOString();
    state.activeRole = "player";
    saveState(state);
    render();
    return;
  } else if (action === "tour-next") {
    const roundRated = state.feedback.roundSubmitted[state.live.roundIndex] || state.feedback.roundSkipped[state.live.roundIndex];
    if (state.live.phase === "revealQueue" || (state.live.phase === "leaderboard" && roundRated)) {
      setToast(advanceHost(state));
    }
  } else if (action === "set-role") state.activeRole = value;
  else if (action === "set-demo-step") setDemoStep(value);
  else if (action === "reset-demo") state.ui.resetConfirm = true;
  else if (action === "cancel-reset") state.ui.resetConfirm = false;
  else if (action === "confirm-reset") {
    const demoVisible = new URLSearchParams(window.location.search).get("demo") === "1";
    state = createInitialState();
    state.activeRole = demoVisible ? "player" : state.activeRole;
    setToast("Демонстрация сброшена");
  } else if (action === "happy-path") {
    if (state.activeRole === "host") setToast(advanceHost(state));
    else if (state.activeRole === "organizer") nextOrganizer();
    else if (state.activeRole === "public") setToast(advancePublicTour());
    else if (state.player.step === "auth") handleAction("fake-auth");
    else if (state.player.step === "team") state.player.step = "lobby";
    else setToast("Следующий глобальный этап запускает ведущий");
  } else if (action === "demo-auth-error") {
    state.activeRole = "player";
    state.player.step = "auth";
    state.auth.status = "error";
  } else if (action === "demo-team-error") {
    state.activeRole = "player";
    state.player.step = "team";
    state.ui.teamScenario = "not-found";
  } else if (action === "demo-empty-profile") {
    state.activeRole = "player";
    state.player.step = "profile";
    state.ui.emptyHistory = true;
    state.ui.emptyAchievements = true;
  } else if (action === "fake-auth") {
    state.auth.status = "loading";
    saveState(state);
    render();
    window.setTimeout(() => {
      if (state.ui.authScenario === "error") {
        state.auth.status = "error";
        state.ui.authScenario = "success";
      } else {
        state.auth.status = "success";
        state.player.step = "team";
      }
      saveState(state);
      render();
      emitStateChanged(state, "action:fake-auth:complete");
    }, 650);
    return;
  } else if (action === "retry-team-lookup") {
    state.ui.teamScenario = "found";
    setToast("Команда найдена");
  } else if (action === "open-team-picker") {
    if (state.team.captainLocked) setToast("После старта команду сменить нельзя");
    else state.team.pickerOpen = true;
  } else if (action === "close-team-picker") state.team.pickerOpen = false;
  else if (action === "change-team") {
    if (!state.team.captainLocked) {
      state.team.teamId = value;
      state.team.captainId = currentTeamMembers(state)[0].id;
      state.team.voteSubmitted = false;
      state.team.pickerOpen = false;
      state.ui.teamScenario = "found";
      setToast("Команда изменена");
    }
  } else if (action === "team-found") state.player.step = "lobby";
  else if (action === "select-captain") {
    if (!state.team.captainLocked) {
      state.team.captainId = value;
      state.team.voteSubmitted = false;
    }
  } else if (action === "submit-captain-vote") {
    if (!state.team.captainLocked) {
      state.team.voteSubmitted = true;
      setToast("Голос за капитана учтён");
    }
  } else if (action === "edit-captain-vote") state.team.voteSubmitted = false;
  else if (action === "host-next") setToast(advanceHost(state));
  else if (action === "pause-timer") {
    if (pauseTimer(state)) setToast("Отсчёт приостановлен");
  } else if (action === "resume-timer") {
    if (resumeTimer(state)) setToast("Отсчёт продолжен");
  } else if (action === "select-answer") selectAnswer(value);
  else if (action === "submit-answer") {
    const question = currentQuestion(state);
    const answer = currentAnswer(state);
    if (state.activeRole === "captain" && state.live.phase === "questionOpen" && !state.live.timer.paused && !answer.submitted && answerIsValid(question, answer.value)) {
      answer.submitted = true;
      answer.unanswered = false;
      state.live.answeredTeams = Math.max(state.live.answeredTeams, 8);
      setToast("Ответ команды зафиксирован");
    }
  } else if (action === "rate-current-round") state.feedback.roundRatings[state.live.roundIndex] = Number(value);
  else if (action === "submit-round-rating") {
    const index = state.live.roundIndex;
    if (state.feedback.roundRatings[index]) state.feedback.roundSubmitted[index] = true;
  } else if (action === "skip-round-rating") state.feedback.roundSkipped[state.live.roundIndex] = true;
  else if (action === "rate-overall") state.feedback.overallRating = Number(value);
  else if (action === "submit-overall-rating") {
    if (state.feedback.overallRating) state.feedback.overallSubmitted = true;
  } else if (action === "skip-overall-rating") state.feedback.overallSkipped = true;
  else if (action === "open-profile") state.player.step = "profile";
  else if (action === "back-to-final") state.player.step = "live";
  else if (action === "open-achievement") state.ui.sheet = { type: "achievement", id: value };
  else if (action === "open-team-detail") state.ui.sheet = { type: "team", id: value };
  else if (action === "close-sheet") state.ui.sheet = null;
  else if (action === "restore-profile-data") {
    state.ui.emptyHistory = false;
    state.ui.emptyAchievements = false;
  } else if (action === "select-template") {
    if (value === "cinema") {
      state.organizer.templateId = value;
      state.organizer.draft = createDraft();
      setToast("Шаблон выбран");
    }
  } else if (action === "organizer-next") nextOrganizer();
  else if (action === "organizer-home") state.organizer.step = "home";
  else if (action === "select-editor-round") {
    state.organizer.roundIndex = Number(value);
    state.organizer.questionIndex = 0;
    state.organizer.validation = {};
  } else if (action === "previous-editor-question") {
    state.organizer.questionIndex = Math.max(0, state.organizer.questionIndex - 1);
    state.organizer.validation = {};
  } else if (action === "next-editor-question") {
    state.organizer.questionIndex = Math.min(2, state.organizer.questionIndex + 1);
    state.organizer.validation = {};
  } else if (action === "save-editor-question") {
    const errors = validateCurrentEditorQuestion(state);
    state.organizer.validation = errors;
    if (Object.keys(errors).length) setToast("Исправьте поля вопроса");
    else {
      state.organizer.savedAt = new Date().toISOString();
      setToast("Вопрос сохранён");
    }
  } else if (action === "save-rules") {
    const errors = validateRules(state.organizer.draft.rules);
    if (Object.keys(errors).length) {
      state.organizer.validation = errors;
      setToast("Исправьте правила");
    } else {
      state.organizer.validation = {};
      state.organizer.step = "preview";
      setToast("Правила сохранены");
    }
  } else if (action === "create-session") {
    const errors = validateDraft(state.organizer.draft);
    if (errors.length) {
      state.organizer.step = "review";
      state.organizer.validation = { review: errors[0] };
      setToast("Исправьте игру перед созданием комнаты");
    } else {
      state.organizer.creating = true;
      saveState(state);
      render();
      window.setTimeout(() => {
        state.organizer.creating = false;
        state.organizer.step = "session";
        saveState(state);
        render();
        emitStateChanged(state, "action:create-session:complete");
      }, 700);
      return;
    }
  } else if (action === "handoff-to-host") state.activeRole = "host";
  else if (action === "open-organizer-analytics") {
    state.activeRole = "organizer";
    state.organizer.step = "analytics";
  }
  saveState(state);
  render();
  emitStateChanged(state, `action:${action}`);
}

document.addEventListener("click", (event) => {
  const target = event.target.closest("[data-action]");
  if (!target) return;
  event.preventDefault();
  handleAction(target.dataset.action, target.dataset.value);
});

document.addEventListener("input", (event) => {
  const target = event.target;
  if (target.dataset.bind === "textAnswer") {
    if (state.activeRole !== "captain" || state.live.phase !== "questionOpen" || state.live.timer.paused) return;
    const answer = ensureAnswer(state);
    if (!answer.submitted) {
      answer.value = target.value;
      const submit = document.querySelector('[data-action="submit-answer"]');
      const ready = answerIsValid(currentQuestion(state), answer.value);
      if (submit) {
        submit.disabled = !ready;
        submit.classList.toggle("signal", ready);
        submit.textContent = ready ? "Зафиксировать ответ" : "Введите ответ";
      }
    }
    saveState(state);
    return;
  }
  if (target.dataset.bind?.startsWith("draft-")) updateEditorBinding(target);
});

document.addEventListener("change", (event) => {
  const target = event.target;
  if (target.dataset.bind?.startsWith("draft-")) {
    updateEditorBinding(target);
    if (target.tagName === "SELECT" || target.type === "checkbox") render();
  }
});

window.setInterval(() => {
  const previousPhase = state.live.phase;
  if (reconcileTimer(state)) {
    const expired = previousPhase === "questionOpen"
      && state.live.phase === "questionClosed"
      && state.live.closedBy === "time";
    if (expired && state.activeRole === "host") saveState(state);
    if (expired) render();
    else updateLiveTick();
  }
}, 250);

window.addEventListener("storage", (event) => {
  if (!event.key || !event.key.startsWith("live-signal-full-prototype-v2")) return;
  const localRole = state.activeRole;
  state = loadState();
  state.activeRole = localRole;
  reconcileTimer(state);
  render();
  emitStateChanged(state, "storage");
});

render();
beginIntegration(() => state);
emitStateChanged(state, "bootstrap");
