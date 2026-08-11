import { analytics, assets, finalTeams, gameInfo, templates } from "./data.js";
import { renderEditor, renderPreview, renderReview, renderRules } from "./screens-organizer-editor.js";
import { esc, listRows, metricGrid, phoneShell, rail, stickyAction } from "./ui.js";

function organizerMetrics(state) {
  const draft = state.organizer.draft;
  const questionCount = draft.rounds.reduce((sum, round) => sum + round.questions.length, 0);
  return metricGrid([
    { value: draft.rounds.length, label: "раунда" },
    { value: questionCount, label: "вопросов" },
    { value: `${draft.rules.timer}с`, label: "по умолчанию" },
    { value: gameInfo.players, label: "участников" }
  ]);
}

function home(state) {
  const templateItems = templates.map((template) => ({
    ...template,
    detail: template.subtitle,
    disabled: template.id !== "cinema"
  }));
  return `
    <section class="surface solid" data-testid="organizer-home">
      <div class="panel-title"><strong>Ближайшая игра</strong><span>сегодня в 19:00</span></div>
      <div class="surface cloud">
        <div class="panel-title"><strong>${esc(state.organizer.draft.title)}</strong><span>${esc(gameInfo.place)}</span></div>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">Начните с готового шаблона — все девять вопросов можно отредактировать.</p>
      </div>
      ${rail(templateItems, "select-template", state.organizer.templateId, "medium")}
      <p class="muted">Создание с нуля появится позже. Сейчас доступен цельный шаблон «Кино 3×3».</p>
    </section>
    ${stickyAction("шаблон", "Настроить «Кино 3×3»", "organizer-next", { tone: "signal" })}
  `;
}

function templatesScreen(state) {
  return `
    <section class="surface solid" data-testid="template-selected">
      <div class="panel-title"><strong>Шаблон выбран</strong><span>Кино 3×3</span></div>
      ${listRows([
        { title: "3 раунда", detail: state.organizer.draft.rounds.map((round) => round.title).join(" · "), value: "готово", tone: "signal" },
        { title: "9 вопросов", detail: "три типа ответа и два вопроса с изображением", value: "9" },
        { title: "Разбор", detail: "после трёх вопросов каждого раунда", value: "очередь" },
        { title: "Командный формат", detail: "отправляет только капитан", value: "замок" }
      ])}
    </section>
    ${stickyAction("Кино 3×3", "Редактировать игру", "organizer-next", { tone: "signal" })}
  `;
}

function session(state) {
  return `
    <section class="surface solid" data-testid="organizer-session">
      <div class="panel-title"><strong>Комната готова</strong><span>${esc(gameInfo.roomCode)}</span></div>
      <div class="surface cloud">
        <div class="panel-title"><strong>${esc(gameInfo.roomCode)}</strong><span>${esc(gameInfo.place)}</span></div>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">Покажите код на экране зала или назовите его участникам.</p>
      </div>
      ${listRows([
        { title: "Подключились все 10 команд", detail: "56 участников", value: "10/10", tone: "signal" },
        { title: "Кандидаты в капитаны выбраны", detail: "закрепятся после запуска ведущим", value: "10" },
        { title: "Общий экран", detail: "готов показать код комнаты", value: "готов" }
      ])}
      ${rail(finalTeams.map((team) => ({
        id: team.name,
        title: team.name,
        detail: `${team.players} участников`
      })), "open-team-detail", "", "small")}
    </section>
    ${stickyAction("10 команд", "Передать управление ведущему", "handoff-to-host", { tone: "signal" })}
  `;
}

function analyticsScreen(state) {
  const roundValues = state.feedback.roundRatings.map((value, index) => ({
    title: state.organizer.draft.rounds[index].title,
    detail: state.feedback.roundSubmitted[index] ? "оценка участника сохранена" : state.feedback.roundSkipped[index] ? "оценка пропущена" : "оценки ещё нет",
    value: value || "—"
  }));
  const overall = state.feedback.overallRating || "—";
  return `
    <section class="surface solid" data-testid="organizer-analytics">
      <div class="panel-title"><strong>Киновечер в цифрах</strong><span>после игры</span></div>
      ${listRows([
        { title: "10 команд", detail: "56 участников", value: "10" },
        { title: "Правильные ответы", detail: "63 из 90 по всем командам", value: "70%", tone: "gold" },
        { title: "Среднее время ответа", detail: "по девяти вопросам", value: "8,6 с" },
        { title: "Итоговая оценка вечера", detail: "единая оценка игры, ведущего, организации и площадки", value: overall, tone: "gold" }
      ])}
      <div class="surface cloud">
        <strong>Самый сложный вопрос</strong>
        <p class="hero-text" style="color: rgba(16,17,22,.62)">${esc(analytics.hardest)}. Полностью правильно ответили 4 из 10 команд.</p>
      </div>
      ${listRows(roundValues)}
    </section>
    ${stickyAction("аналитика", "Вернуться на главную", "organizer-home", { tone: "cloud" })}
  `;
}

export function renderOrganizer(state) {
  const step = state.organizer.step;
  const map = {
    home: {
      title: "Что запускаем сегодня?",
      kicker: `Добрый вечер, ${gameInfo.organizer.split(" ")[0]}`,
      text: `${state.organizer.draft.title} · сегодня в 19:00`,
      content: home(state)
    },
    templates: {
      title: "Основа игры выбрана",
      kicker: "Шаблон",
      text: "Дальше можно изменить названия раундов, вопросы, ответы, пояснения и правила.",
      content: templatesScreen(state)
    },
    editor: {
      title: state.organizer.draft.title,
      kicker: "Редактор игры",
      text: "Изменения сохраняются локально и сразу попадают в предпросмотр.",
      content: renderEditor(state)
    },
    rules: {
      title: "Как пройдёт игра",
      kicker: "Правила",
      text: "Капитан отвечает, фиксация неизменяема, разбор начинается после трёх вопросов.",
      content: renderRules(state)
    },
    preview: {
      title: "Посмотрите как игрок",
      kicker: "Предпросмотр",
      text: "Здесь используются реальные данные из редактора.",
      content: renderPreview(state)
    },
    review: {
      title: "Проверка перед запуском",
      kicker: "Обязательная валидация",
      text: "Комнату нельзя создать, пока в вопросах есть ошибки.",
      content: renderReview(state)
    },
    session: {
      title: gameInfo.roomCode,
      kicker: "Комната готова",
      text: "Команды подключены, ведущему можно передавать управление.",
      content: session(state)
    },
    analytics: {
      title: "Киновечер в цифрах",
      kicker: "Аналитика",
      text: "Отчёт использует оценки, отправленные в текущем прототипе.",
      content: analyticsScreen(state)
    }
  };
  const current = map[step] || map.home;
  return phoneShell({
    image: assets.organizer,
    room: "Организатор",
    timer: step === "analytics" ? "4,7" : "9",
    title: current.title,
    kicker: current.kicker,
    text: current.text,
    metrics: organizerMetrics(state),
    compact: step !== "home",
    content: current.content
  });
}
