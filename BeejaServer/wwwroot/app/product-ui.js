export function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

export function routeButton(route, label, options = {}) {
  const tone = options.tone ? ` ${options.tone}` : "";
  const className = options.className || "product-button";
  const testId = options.testId ? ` data-testid="${esc(options.testId)}"` : "";
  const href = route === "/home" ? "/app/" : `/app${route}`;
  return `<a class="${className}${tone}" href="${esc(href)}" data-route="${esc(route)}"${testId}>${esc(label)}</a>`;
}

export function actionButton(action, label, options = {}) {
  const tone = options.tone ? ` ${options.tone}` : "";
  const disabled = options.disabled ? " disabled" : "";
  const testId = options.testId ? ` data-testid="${esc(options.testId)}"` : "";
  const value = options.value === undefined ? "" : ` data-value="${esc(options.value)}"`;
  return `<button class="${options.className || "product-button"}${tone}" data-product-action="${esc(action)}"${value}${testId}${disabled} type="button">${esc(label)}</button>`;
}

export function field({ id, label, value = "", placeholder = "", type = "text", inputMode = "", error = "", autocomplete = "" }) {
  return `
    <label class="product-field" for="${esc(id)}">
      <span>${esc(label)}</span>
      <input id="${esc(id)}" name="${esc(id)}" type="${esc(type)}" value="${esc(value)}" placeholder="${esc(placeholder)}"${inputMode ? ` inputmode="${esc(inputMode)}"` : ""}${autocomplete ? ` autocomplete="${esc(autocomplete)}"` : ""} aria-invalid="${error ? "true" : "false"}"${error ? ` aria-describedby="${esc(id)}-error"` : ""} />
      ${error ? `<small id="${esc(id)}-error" class="field-error" role="alert">${esc(error)}</small>` : ""}
    </label>
  `;
}

export function statusPanel(title, text, options = {}) {
  return `
    <section class="status-panel ${options.tone || ""}" role="${options.error ? "alert" : "status"}">
      <strong>${esc(title)}</strong>
      <p>${esc(text)}</p>
    </section>
  `;
}
