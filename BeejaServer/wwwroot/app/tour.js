import { currentAnswer } from "./state-machine.js";
import { button, esc } from "./ui.js";

export const TOUR_TOTAL_STEPS = 27;

export function tourMode() {
  return new URLSearchParams(window.location.search).get("tour") === "1";
}

function roundBase(state) {
  return 5 + state.live.roundIndex * 7;
}

export function tourView(state) {
  if (state.player.step === "auth") {
    return {
      step: 1,
      simulation: state.auth.status === "loading"
        ? "Яндекс ID подтверждает тестовый профиль Майи."
        : "Вход через тестовый Яндекс ID без передачи данных."
    };
  }

  if (state.player.step === "team") {
    return {
      step: 2,
      simulation: "Команда подтягивается из покупки; до старта её можно сменить."
    };
  }

  if (!state.live.gameStarted) {
    return {
      step: state.team.voteSubmitted ? 4 : 3,
      simulation: state.team.voteSubmitted
        ? "Ведущий фиксирует выбранного капитана и запускает игру."
        : "Команда выбирает капитана; один голос сам игру не запускает."
    };
  }

  if (state.player.step === "profile") {
    return {
      step: 27,
      simulation: "Профиль показывает опыт платформы, достижения и историю отдельно от счёта.",
      action: "tour-exit", // <-- Изменили экшен
      actionLabel: "Вернуться на сайт" // <-- Изменили текст кнопки
    };
  }

  if (state.live.phase === "final" || state.live.phase === "report") {
    return {
      step: 26,
      simulation: "Экран зала показывает финал; отзыв остаётся необязательным."
    };
  }

  const base = roundBase(state);
  if (state.live.phase === "revealQueue") {
    return {
      step: base + 3 + state.live.revealIndex,
      simulation: `Ведущий открывает правильный ответ ${state.live.revealIndex + 1} из 3 на всех экранах.`,
      action: "tour-next",
      actionLabel: state.live.revealIndex === 2 ? "Показать таблицу" : "Следующий ответ"
    };
  }

  if (state.live.phase === "leaderboard") {
    const index = state.live.roundIndex;
    const rated = state.feedback.roundSubmitted[index] || state.feedback.roundSkipped[index];
    return {
      step: base + 6,
      simulation: rated
        ? "Ведущий получил оценку раунда и готовит следующий этап."
        : "Все 10 команд видят одну таблицу; оценка раунда необязательна.",
      action: rated ? "tour-next" : "",
      actionLabel: rated
        ? state.live.roundIndex === 2 ? "Перейти к финалу" : "Следующий раунд"
        : ""
    };
  }

  const answer = currentAnswer(state);
  const questionNumber = state.live.questionIndex + 1;
  let simulation = `Ведущий и экран зала синхронно показывают вопрос ${questionNumber}; отвечает капитан.`;
  if (state.live.phase === "roundIntro" || state.live.phase === "questionReady") {
    simulation = `Ведущий готовит вопрос ${questionNumber} и запускает общий 15-секундный отсчёт.`;
  } else if (state.live.phase === "questionClosed") {
    simulation = answer.submitted
      ? "Ведущий закрыл приём; ответ команды уже нельзя изменить."
      : "Время вышло; вопрос сохранён без ответа и тур идёт дальше.";
  } else if (answer.submitted) {
    simulation = "Ответ зафиксирован; ведущий закрывает вопрос без раннего раскрытия.";
  }

  return {
    step: base + state.live.questionIndex,
    simulation
  };
}

export function renderTourBar(state) {
  const view = tourView(state);
  return `
    <aside class="tour-bar" data-testid="tour-bar" aria-label="Служебная навигация демо-тура" aria-live="polite" aria-atomic="true">
      <div class="tour-heading">
        <span>Служебный режим</span>
        <strong>Демо-тур · шаг ${view.step} из ${TOUR_TOTAL_STEPS}</strong>
      </div>
      <p>Сейчас симулируется: ${esc(view.simulation)}</p>
      <div style="display: flex; gap: 10px; align-items: center;">
        ${view.action
          ? button(view.action, view.actionLabel, {
              className: "tour-next-button",
              testId: view.action === "tour-restart" ? "tour-restart" : "tour-next"
            })
          : ""}
        <button onclick="window.location.href='../ved.html'" class="tour-next-button" style="opacity: 0.85;">
          Выйти из демо-тура
        </button>
      </div>
    </aside>
  `;
}

export function tourAutomation(state) {
  if (!state.tour?.active || state.player.step === "profile") return "";
  if (state.team.voteSubmitted && !state.live.gameStarted) return "start-game";
  if (state.live.phase === "roundIntro" || state.live.phase === "questionReady") return "start-question";
  if (state.live.phase === "questionOpen" && currentAnswer(state).submitted) return "close-question";
  if (state.live.phase === "questionClosed") return "advance-after-close";
  return "";
}

export function tourAutomationKey(state, action) {
  const answer = currentAnswer(state);
  return [
    action,
    state.live.roundIndex,
    state.live.questionIndex,
    state.live.revealIndex,
    answer.submitted ? 1 : 0,
    state.live.closedBy || ""
  ].join(":");
}
