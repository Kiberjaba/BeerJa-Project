import { currentQuestion as liveQuestion, validateDraft, validateQuestion } from "./state-machine.js";
import {
  button,
  choiceGrid,
  esc,
  field,
  inlineError,
  listRows,
  rail,
  selectField,
  stickyAction,
  textArea
} from "./ui.js";

function editorQuestion(state) {
  return state.organizer.draft.rounds[state.organizer.roundIndex].questions[state.organizer.questionIndex];
}

function typeLabel(type) {
  if (type === "multiple") return "Несколько вариантов";
  if (type === "text") return "Короткий ответ";
  return "Один вариант";
}

function answerFields(question, errors) {
  if (question.type === "text") {
    return field("Правильный короткий ответ", question.correct || "", "draft-correct-text", { error: errors.correct });
  }

  const answers = [...(question.answers || [])];
  while (answers.length < 4) answers.push("");
  const answerInputs = answers.slice(0, 4).map((answer, index) =>
    field(`Вариант ${index + 1}`, answer, `draft-answer-${index}`)
  ).join("");

  if (question.type === "single") {
    const options = answers.filter(Boolean).map((answer) => ({ value: answer, label: answer }));
    return `
      <div class="form-grid">${answerInputs}</div>
      ${inlineError(errors.answers)}
      ${selectField("Правильный вариант", question.correct || "", "draft-correct-single", options.length ? options : [{ value: "", label: "Сначала заполните варианты" }], errors.correct)}
    `;
  }

  const correct = Array.isArray(question.correct) ? question.correct : [];
  return `
    <div class="form-grid">${answerInputs}</div>
    ${inlineError(errors.answers)}
    <fieldset class="check-field">
      <legend>Отметьте ровно ${Number(question.required || 2)} правильных варианта</legend>
      ${answers.filter(Boolean).map((answer) => `
        <label>
          <input type="checkbox" data-bind="draft-correct-option" data-value="${esc(answer)}"${correct.includes(answer) ? " checked" : ""} />
          <span>${esc(answer)}</span>
        </label>
      `).join("")}
      ${inlineError(errors.correct)}
    </fieldset>
  `;
}

export function renderEditor(state) {
  const draft = state.organizer.draft;
  const round = draft.rounds[state.organizer.roundIndex];
  const question = editorQuestion(state);
  const errors = state.organizer.validation || {};

  return `
    <section class="surface solid" data-testid="organizer-editor">
      <div class="panel-title"><strong>${esc(draft.title)}</strong><span>${state.organizer.savedAt ? "сохранено локально" : "есть несохранённые изменения"}</span></div>
      ${rail(draft.rounds.map((item, index) => ({
        id: String(index),
        title: `Раунд ${index + 1} · ${item.title || "Без названия"}`,
        detail: `${item.questions.length} вопроса`,
        selected: index === state.organizer.roundIndex
      })), "select-editor-round", String(state.organizer.roundIndex), "medium")}
      ${field("Название раунда", round.title, "draft-round-title", { error: errors.roundTitle })}
      <div class="question-nav">
        ${button("previous-editor-question", "Предыдущий", { className: "secondary-button", disabled: state.organizer.questionIndex === 0 })}
        <strong>Вопрос ${state.organizer.questionIndex + 1} из 3</strong>
        ${button("next-editor-question", "Следующий", { className: "secondary-button", disabled: state.organizer.questionIndex === 2 })}
      </div>
      ${selectField("Тип ответа", question.type, "draft-question-type", [
        { value: "single", label: "Один вариант" },
        { value: "multiple", label: "Несколько вариантов" },
        { value: "text", label: "Короткий ответ" }
      ], errors.type)}
      <label class="toggle-row">
        <span><strong>Вопрос с изображением</strong><small>Используется кино-сцена из медиатеки</small></span>
        <input type="checkbox" data-bind="draft-has-image"${question.hasImage ? " checked" : ""} />
      </label>
      ${textArea("Текст вопроса", question.title, "draft-question-title", { error: errors.title })}
      ${textArea("Подсказка игрокам", question.prompt, "draft-question-prompt", { error: errors.prompt })}
      ${answerFields(question, errors)}
      ${question.hasImage ? field("Описание изображения", question.imageAlt || "", "draft-image-alt", { error: errors.imageAlt }) : ""}
      ${textArea("Пояснение после раунда", question.explanation, "draft-question-explanation", { error: errors.explanation })}
      ${field("Секунд на ответ", question.duration, "draft-question-duration", { type: "number", min: 5, max: 60, error: errors.duration })}
      ${button("organizer-next", "Все вопросы готовы — к правилам", { className: "secondary-button" })}
    </section>
    ${stickyAction(`${typeLabel(question.type)} · ${question.duration}с`, "Сохранить вопрос", "save-editor-question", { tone: "signal", testId: "save-question" })}
  `;
}

export function renderRules(state) {
  const rules = state.organizer.draft.rules;
  const errors = state.organizer.validation || {};
  return `
    <section class="surface solid" data-testid="organizer-rules">
      <div class="panel-title"><strong>Как пройдёт игра</strong><span>обязательные правила</span></div>
      <div class="form-grid two-columns">
        ${field("Минимум игроков", rules.teamMin, "draft-team-min", { type: "number", min: 2, max: 10, error: errors.teamMin })}
        ${field("Максимум игроков", rules.teamMax, "draft-team-max", { type: "number", min: 2, max: 10, error: errors.teamMax })}
        ${field("Секунд по умолчанию", rules.timer, "draft-default-timer", { type: "number", min: 5, max: 60, error: errors.timer })}
        ${field("Баллов за ответ", rules.points, "draft-points", { type: "number", min: 1, max: 100, error: errors.points })}
      </div>
      ${inlineError(errors.rules)}
      <div class="rule-locks">
        <div><strong>Отвечает только капитан</strong><span>обязательно</span></div>
        <div><strong>Ответ после фиксации неизменяем</strong><span>обязательно</span></div>
        <div><strong>Разбор после трёх вопросов</strong><span>обязательно</span></div>
      </div>
    </section>
    ${stickyAction(`${rules.timer}с`, "Сохранить правила", "save-rules", { tone: "signal", testId: "save-rules" })}
  `;
}

export function renderPreview(state) {
  const question = editorQuestion(state);
  return `
    <section class="surface solid" data-testid="organizer-preview">
      <div class="panel-title"><strong>Предпросмотр игрока</strong><span>${esc(typeLabel(question.type))}</span></div>
      <div class="surface cloud">
        <strong>${esc(question.title)}</strong>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">${esc(question.prompt)}</p>
      </div>
      ${choiceGrid(question, question.type === "multiple" ? [] : "", "", true)}
      <div class="surface solid">
        <strong>Капитан фиксирует ответ</strong>
        <p class="hero-text">Игроки видят этот же вопрос без кнопки отправки.</p>
      </div>
    </section>
    ${stickyAction("просмотр", "Перейти к проверке", "organizer-next", { tone: "signal" })}
  `;
}

export function renderReview(state) {
  const draft = state.organizer.draft;
  const errors = validateDraft(draft);
  return `
    <section class="surface solid" data-testid="organizer-review">
      <div class="panel-title"><strong>${errors.length ? "Нужно исправить игру" : "Игра готова"}</strong><span>${errors.length ? `${errors.length} ошибок` : "все проверки пройдены"}</span></div>
      ${errors.length
        ? `<div class="validation-summary" role="alert">${errors.map((error) => `<p>${esc(error)}</p>`).join("")}</div>`
        : listRows([
          { title: "3 раунда заполнены", detail: draft.rounds.map((round) => round.title).join(" · "), value: "готово", tone: "signal" },
          { title: "9 вопросов готовы", detail: "один вариант, несколько вариантов, короткий ответ", value: "9" },
          { title: "Разбор после трёх вопросов", detail: "без подсказок между вопросами", value: "да" },
          { title: "Отсчёт", detail: "значение сохранено у каждого вопроса", value: `${draft.rules.timer}с` }
        ])}
    </section>
    ${stickyAction(errors.length ? "ошибки" : "QR-2048", state.organizer.creating ? "Готовим вашу игру…" : "Создать комнату", "create-session", {
      disabled: Boolean(errors.length) || state.organizer.creating,
      tone: errors.length ? "" : "signal",
      testId: "create-session"
    })}
  `;
}

export function validateCurrentEditorQuestion(state) {
  const round = state.organizer.draft.rounds[state.organizer.roundIndex];
  const question = editorQuestion(state);
  const errors = validateQuestion(question);
  if (!String(round.title || "").trim()) errors.roundTitle = "Введите название раунда.";
  return errors;
}

export function previewQuestion(state) {
  return liveQuestion(state);
}
