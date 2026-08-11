import { mechanics, orderOptions } from "./product-data.js";
import { actionButton, esc, field, routeButton, statusPanel } from "./product-ui.js";

function optionGrid(name, options, selected) {
  return `<div class="order-options" role="group" aria-label="${esc(name)}">${options.map((option) => `<button class="${selected === option ? "selected" : ""}" data-product-action="order-select" data-field="${esc(name)}" data-value="${esc(option)}" aria-pressed="${selected === option}" type="button"><span>${selected === option ? "✓" : "+"}</span><strong>${esc(option)}</strong></button>`).join("")}</div>`;
}

function stepBasics(state) {
  return `<div class="order-step"><span class="eyebrow">Событие</span><h2>Что вы готовите?</h2><p>Эти данные помогут подобрать механику и состав команды.</p>${optionGrid("eventType", orderOptions.eventTypes, state.order.eventType)}${optionGrid("audience", orderOptions.audiences, state.order.audience)}${state.order.errors.basics ? `<small class="field-error" role="alert">${esc(state.order.errors.basics)}</small>` : ""}</div>`;
}

function stepMechanic(state) {
  return `<div class="order-step"><span class="eyebrow">Формат</span><h2>Выберите отправную точку.</h2><p>После брифа механику можно изменить или объединить с другой.</p><div class="order-mechanics">${mechanics.map((item) => `<button class="${state.order.mechanic === item.slug ? "selected" : ""}" data-product-action="order-select" data-field="mechanic" data-value="${esc(item.slug)}" type="button"><img src="${esc(item.image)}" alt="" width="600" height="400" /><span>${esc(item.complexity)}</span><strong>${esc(item.title)}</strong><small>${esc(item.price)}</small></button>`).join("")}</div>${state.order.errors.mechanic ? `<small class="field-error" role="alert">${esc(state.order.errors.mechanic)}</small>` : ""}</div>`;
}

function stepDetails(state) {
  return `<div class="order-step"><span class="eyebrow">Детали</span><h2>Когда и где всё произойдёт?</h2><p>Если дата ещё не определена, укажите ориентир в комментарии.</p><div class="order-fields">${field({ id: "order-date", label: "Дата", value: state.order.date, type: "date", error: state.order.errors.date })}${field({ id: "order-city", label: "Город или площадка", value: state.order.city, placeholder: "Москва, площадка уточняется", error: state.order.errors.city })}</div><label class="product-field"><span>Что важно учесть</span><textarea id="order-notes" rows="5" placeholder="Задача события, тема, ограничения">${esc(state.order.notes)}</textarea></label>${optionGrid("budget", orderOptions.budgets, state.order.budget)}${state.order.errors.details ? `<small class="field-error" role="alert">${esc(state.order.errors.details)}</small>` : ""}</div>`;
}

function stepContact(state) {
  return `<div class="order-step"><span class="eyebrow">Контакт</span><h2>Кому отправить предложение?</h2><p>Свяжемся только по этой заявке. Оплата появится после согласования сметы.</p><div class="order-fields">${field({ id: "order-name", label: "Имя", value: state.order.name, placeholder: "Анна", autocomplete: "name", error: state.order.errors.name })}${field({ id: "order-contact", label: "Телефон или Telegram", value: state.order.contact, placeholder: "+7 999 123-45-67", autocomplete: "tel", error: state.order.errors.contact })}${field({ id: "order-email", label: "Email", value: state.order.email, placeholder: "name@company.ru", type: "email", autocomplete: "email", error: state.order.errors.email })}</div></div>`;
}

function stepSummary(state) {
  const mechanic = mechanics.find((item) => item.slug === state.order.mechanic);
  const rows = [["Событие", state.order.eventType], ["Аудитория", state.order.audience], ["Механика", mechanic?.title], ["Дата", state.order.date], ["Площадка", state.order.city], ["Бюджет", state.order.budget], ["Контакт", `${state.order.name} · ${state.order.contact}`]];
  return `<div class="order-step order-summary"><span class="eyebrow">Проверка</span><h2>Заявка готова.</h2><p>После отправки мы уточним сценарий, сроки и финальную стоимость.</p><div>${rows.map(([label, value]) => `<div><span>${esc(label)}</span><strong>${esc(value || "не указано")}</strong></div>`).join("")}</div><div class="payment-note"><strong>Без оплаты сейчас</strong><span>Ссылка на оплату появится только после согласования предложения.</span></div></div>`;
}

export function renderOrder(state) {
  if (state.order.status === "submitted") {
    return `<section class="order-success compact-page"><div class="success-mark">✓</div><span class="eyebrow">Заявка ${esc(state.order.id)}</span><h1>Приняли задачу.</h1><p>Вернёмся с уточнениями и предложением по сценарию. Черновик уже сохранён в кабинете ведущего.</p><div class="page-actions">${routeButton("/host/account", "В кабинет ведущего", { tone: "signal" })}${routeButton("/mechanics", "Вернуться в каталог", { tone: "cloud" })}</div></section>`;
  }
  const steps = [stepBasics, stepMechanic, stepDetails, stepContact, stepSummary];
  return `
    <section class="order-page compact-page">
      <aside class="order-sidebar"><span class="eyebrow">Заявка на игру</span><h1>Соберём событие по шагам.</h1><div class="order-progress">${["Событие", "Механика", "Детали", "Контакт", "Проверка"].map((label, index) => `<div class="${index === state.order.step ? "active" : index < state.order.step ? "done" : ""}"><span>${index < state.order.step ? "✓" : index + 1}</span><strong>${label}</strong></div>`).join("")}</div></aside>
      <form class="order-form" data-product-form="order" novalidate>
        ${steps[state.order.step](state)}
        <div class="order-actions">${state.order.step > 0 ? actionButton("order-back", "Назад", { tone: "ghost" }) : routeButton("/mechanics", "Отмена", { tone: "ghost" })}${actionButton(state.order.step === steps.length - 1 ? "order-submit" : "order-next", state.order.status === "loading" ? "Отправляем…" : state.order.step === steps.length - 1 ? "Отправить заявку" : "Продолжить", { tone: "signal", disabled: state.order.status === "loading", testId: "order-primary" })}</div>
        ${state.order.status === "error" ? statusPanel("Не удалось отправить заявку", "Данные сохранены. Попробуйте ещё раз.", { error: true, tone: "error" }) : ""}
      </form>
    </section>
  `;
}
