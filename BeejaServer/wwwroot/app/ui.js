export function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

export function button(action, label, options = {}) {
  const value = options.value !== undefined && options.value !== null ? ` data-value="${esc(options.value)}"` : "";
  const tone = options.tone ? ` ${options.tone}` : "";
  const disabled = options.disabled ? " disabled" : "";
  const ariaLabel = options.ariaLabel ? ` aria-label="${esc(options.ariaLabel)}"` : "";
  const testId = options.testId ? ` data-testid="${esc(options.testId)}"` : "";
  return `<button class="${options.className || "primary-button"}${tone}" data-action="${esc(action)}"${value}${ariaLabel}${testId}${disabled} type="button">${esc(label)}</button>`;
}

export function metricGrid(items) {
  return `
    <div class="metric-grid">
      ${items.map((item) => `
        <div class="metric-chip">
          <strong>${esc(item.value)}</strong>
          <span>${esc(item.label)}</span>
        </div>
      `).join("")}
    </div>
  `;
}

export function segment(items, active, action, extraClass = "") {
  const demoClass = action.startsWith("demo-") ? " demo-only" : "";
  return `
    <div class="segment ${extraClass}${demoClass}" role="tablist" aria-label="Переключатель раздела">
      ${items.map((item) => `
        <button
          class="segment-button ${item.id === active ? "active" : ""}"
          data-action="${esc(action)}"
          data-value="${esc(item.id)}"
          role="tab"
          aria-selected="${item.id === active ? "true" : "false"}"
          type="button"
        >${esc(item.label)}</button>
      `).join("")}
    </div>
  `;
}

export function rail(items, action, selectedId = "", size = "small", options = {}) {
  const selectable = options.selectable ?? ["select-captain", "select-template", "select-editor-round"].includes(action);
  return `
    <div class="rail"${selectable ? ' role="group" aria-label="Выбор варианта"' : ""}>
      ${items.map((item) => {
        const selected = item.id === selectedId || item.selected;
        const content = `
          <div class="mark">${selected ? "✓" : "+"}</div>
          <strong>${esc(item.title || item.name)}</strong>
          <span>${esc(item.detail || item.subtitle || item.role || item.state || "")}</span>
        `;
        if (!action || item.disabled) {
          return `
            <article class="rail-card ${size} ${selected ? "selected" : ""} ${item.disabled ? "disabled" : ""}" aria-disabled="${item.disabled ? "true" : "false"}">
              ${content}
            </article>
          `;
        }
        return `
          <button class="rail-card ${size} ${selected ? "selected" : ""}" data-action="${esc(action)}" data-value="${esc(item.id || item.title)}"${selectable ? ` aria-pressed="${selected ? "true" : "false"}"` : ""} type="button">
            ${content}
          </button>
        `;
      }).join("")}
    </div>
  `;
}

export function listRows(rows) {
  return `
    <div class="list">
      ${rows.map((row, index) => `
        <div class="list-row ${row.tone || ""}">
          <div>
            <strong>${row.rank ? `${esc(row.rank)}. ` : ""}${esc(row.title || row.name)}</strong>
            <span>${esc(row.detail || row.date || "")}</span>
          </div>
          <b>${esc(row.value ?? row.score ?? "")}</b>
        </div>
      `).join("")}
    </div>
  `;
}

export function stickyAction(meta, label, action, options = {}) {
  return `
    <div class="sticky-action ${options.wide ? "wide-lead" : ""}">
      <div class="sticky-meta">${esc(meta)}</div>
      ${button(action, label, {
        className: "primary-button",
        tone: options.tone || "",
        disabled: options.disabled,
        ariaLabel: options.ariaLabel,
        testId: options.testId
      })}
    </div>
  `;
}

export function stickyStatus(meta, label, options = {}) {
  const tone = options.tone ? ` ${options.tone}` : "";
  return `
    <div class="sticky-action status-only">
      <div class="sticky-meta">${esc(meta)}</div>
      <div class="sticky-status${tone}" role="status">${esc(label)}</div>
    </div>
  `;
}

export function inlineError(message) {
  return message ? `<p class="inline-error" role="alert">${esc(message)}</p>` : "";
}

export function field(label, value, bind, options = {}) {
  const type = options.type || "text";
  const min = options.min !== undefined ? ` min="${esc(options.min)}"` : "";
  const max = options.max !== undefined ? ` max="${esc(options.max)}"` : "";
  const placeholder = options.placeholder ? ` placeholder="${esc(options.placeholder)}"` : "";
  return `
    <label class="form-field">
      <span>${esc(label)}</span>
      <input type="${esc(type)}" value="${esc(value)}" data-bind="${esc(bind)}"${min}${max}${placeholder} />
      ${inlineError(options.error)}
    </label>
  `;
}

export function textArea(label, value, bind, options = {}) {
  const placeholder = options.placeholder ? ` placeholder="${esc(options.placeholder)}"` : "";
  return `
    <label class="form-field">
      <span>${esc(label)}</span>
      <textarea data-bind="${esc(bind)}"${placeholder}>${esc(value)}</textarea>
      ${inlineError(options.error)}
    </label>
  `;
}

export function selectField(label, value, bind, options, error = "") {
  return `
    <label class="form-field">
      <span>${esc(label)}</span>
      <select data-bind="${esc(bind)}">
        ${options.map((item) => `<option value="${esc(item.value)}"${item.value === value ? " selected" : ""}>${esc(item.label)}</option>`).join("")}
      </select>
      ${inlineError(error)}
    </label>
  `;
}

export function phoneShell({ image, room = "Комната QR-2048", timer = "02:14", title, kicker, text, compact = false, metrics = "", content = "", action = "" }) {
  return `
    <main class="phone-shell" style="--hero-image: url('${esc(image)}')" aria-label="${esc(title)}">
      <section class="phone-content">
        <div class="topbar">
          <div class="glass-chip"><span class="status-dot"></span><span>${esc(room)}</span></div>
          <div class="round-chip" data-testid="shell-timer">${esc(timer)}</div>
        </div>
        ${metrics}
        <section class="hero-block ${compact ? "compact" : ""}">
          <div class="kicker">${esc(kicker)}</div>
          <h1 class="hero-title" tabindex="-1">${esc(title)}</h1>
          ${text ? `<p class="hero-text">${esc(text)}</p>` : ""}
        </section>
        ${content}
      </section>
      ${action}
    </main>
  `;
}

export function choiceGrid(question, selected, action, readonly = false) {
  if (question.type === "text") {
    const value = typeof selected === "string" ? selected : "";
    return `
      <textarea class="text-answer" data-bind="textAnswer" ${readonly ? "readonly" : ""} placeholder="${esc(question.placeholder || question.prompt || "Введите ответ")}" aria-label="Короткий ответ">${esc(value)}</textarea>
    `;
  }

  return `
    <div class="choice-grid ${question.answers.length > 4 ? "many" : ""}" role="group" aria-label="Варианты ответа">
      ${question.answers.map((answer) => {
        const isSelected = Array.isArray(selected) ? selected.includes(answer) : selected === answer;
        const tag = readonly ? "div" : "button";
        const attrs = readonly
          ? ` aria-label="${esc(answer)}: ${isSelected ? "выбрано командой" : "не выбрано"}"`
          : ` data-action="${esc(action)}" data-value="${esc(answer)}" aria-pressed="${isSelected ? "true" : "false"}" type="button"`;
        return `
          <${tag} class="choice-card ${isSelected ? "selected" : ""} ${readonly ? "readonly" : ""}"${attrs}>
            <div class="mark">${isSelected ? "✓" : "+"}</div>
            <strong>${esc(answer)}</strong>
            <span>${isSelected ? "выбрано командой" : question.label}</span>
          </${tag}>
        `;
      }).join("")}
    </div>
  `;
}

export function questionMedia(question, options = {}) {
  if (!question?.image) return "";
  const extraClass = options.className ? ` ${esc(options.className)}` : "";
  return `
    <figure class="question-media${extraClass}" data-testid="question-media">
      <img src="${esc(question.image)}" alt="${esc(question.imageAlt || "Изображение к вопросу")}" />
    </figure>
  `;
}

export function rating(name, value, action) {
  const selected = Number(value || 0);
  return `
    <div class="surface solid">
      <div class="panel-title">
        <strong>${esc(name)}</strong>
        <span>${selected ? `${selected} из 5` : "не выбрано"}</span>
      </div>
      <div class="rating-row" aria-label="${esc(name)}">
        ${[1, 2, 3, 4, 5].map((star) => `
          <button class="star-button ${star <= selected ? "active" : ""}" data-action="${esc(action)}" data-value="${star}" type="button" aria-label="${star} из 5" aria-pressed="${star <= selected ? "true" : "false"}">★</button>
        `).join("")}
      </div>
    </div>
  `;
}
