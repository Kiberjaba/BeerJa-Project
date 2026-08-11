import { mechanics } from "./product-data.js";
import { actionButton, esc, routeButton } from "./product-ui.js";

function mechanicTile(item, index) {
  return `
    <article class="catalog-tile">
      <a class="catalog-image" href="/app/mechanics/${esc(item.slug)}" data-route="/mechanics/${esc(item.slug)}"><img src="${esc(item.image)}" alt="Визуальный пример механики ${esc(item.title)}" width="1000" height="700" /><span>${String(index + 1).padStart(2, "0")}</span></a>
      <div class="catalog-copy"><div><span>${esc(item.kind)}</span><b>${esc(item.complexity)}</b></div><h2>${esc(item.title)}</h2><p>${esc(item.description)}</p><div><strong>${esc(item.price)}</strong>${routeButton(`/mechanics/${item.slug}`, "Подробнее", { className: "text-link" })}</div></div>
    </article>
  `;
}

export function renderMechanicsCatalog() {
  return `
    <section class="catalog-page compact-page">
      <header class="catalog-hero"><span class="eyebrow">Каталог механик</span><h1>Формат под задачу, а не задача под шаблон.</h1><p>Цены — ориентир для первой оценки. Сценарий, количество экранов, контент и работа ведущего уточняются после заявки.</p></header>
      <div class="catalog-list">${mechanics.map(mechanicTile).join("")}</div>
      <section class="catalog-note"><div><span class="eyebrow">Не нашли точный формат?</span><h2>Соберём механику вокруг вашего события.</h2></div>${routeButton("/order", "Описать задачу", { tone: "signal" })}</section>
    </section>
  `;
}

function audiencePreview(state) {
  const type = state.mechanic.previewType;
  const submitted = state.mechanic.previewSubmitted;
  const options = type === "scale" ? ["1", "2", "3", "4", "5"] : ["Полностью согласен", "Скорее согласен", "Скорее не согласен", "Не согласен"];
  return `
    <section class="audience-preview">
      <div class="preview-tabs" role="tablist" aria-label="Тип вопроса">
        ${[["choice", "Выбор"], ["scale", "Шкала"], ["text", "Текст"]].map(([id, label]) => `<button class="${type === id ? "active" : ""}" data-product-action="preview-type" data-value="${id}" role="tab" aria-selected="${type === id}" type="button">${label}</button>`).join("")}
      </div>
      <div class="preview-phone">
        <span>Мнение зала · вопрос 1</span>
        <h3>${type === "text" ? "Что стоит изменить в следующей встрече?" : type === "scale" ? "Насколько полезной была эта сессия?" : "Продолжить обсуждение этой темы после события?"}</h3>
        ${submitted
          ? `<div class="preview-result"><strong>${type === "text" ? "42 ответа" : "68%"}</strong><span>${type === "text" ? "Самая частая тема — больше практики" : "выбрали положительный вариант"}</span>${actionButton("preview-again", "Ответить ещё раз", { tone: "ghost" })}</div>`
          : type === "text"
            ? `<label class="preview-text"><span>Короткий ответ</span><textarea data-preview-text rows="4" placeholder="Напишите одной фразой">${esc(state.mechanic.previewText)}</textarea></label>${actionButton("preview-submit", "Отправить мнение", { tone: "signal", disabled: !state.mechanic.previewText.trim() })}`
            : `<div class="preview-options ${type}">${options.map((option) => `<button class="${state.mechanic.previewValue === option ? "selected" : ""}" data-product-action="preview-select" data-value="${esc(option)}" type="button">${esc(option)}</button>`).join("")}</div>${actionButton("preview-submit", "Показать результат", { tone: "signal", disabled: !state.mechanic.previewValue })}`}
      </div>
    </section>
  `;
}

export function renderMechanicDetail(item, state) {
  if (!item) return `<section class="compact-page narrow-page"><span class="eyebrow">Механика не найдена</span><h1>Такого формата пока нет.</h1>${routeButton("/mechanics", "Вернуться в каталог", { tone: "signal" })}</section>`;
  const isAudience = item.slug === "audience-voice";
  const deliverables = isAudience
    ? ["Один или несколько вопросов", "QR-вход без установки", "Результаты на общем экране", "Выгрузка ответов после события"]
    : item.slug === "cinema-3x3"
      ? ["Три раунда и девять вопросов", "Командный режим и капитан", "Пульт ведущего и общий экран", "Итоговая таблица и аналитика"]
      : ["Авторский сценарий", "Визуальный пакет события", "Подготовка ведущего", "Техническое сопровождение и отчёт"];
  return `
    <section class="mechanic-detail compact-page">
      <header class="mechanic-detail-hero"><div><span class="eyebrow">${esc(item.kind)} · ${esc(item.complexity)}</span><h1>${esc(item.title)}</h1><p>${esc(item.description)}</p><div class="page-actions">${routeButton(`/order/${item.slug}`, "Добавить в заявку", { tone: "signal" })}${isAudience ? "" : `<a class="product-button cloud" href="/app/?tour=1&clear=1">Открыть demo</a>`}</div></div><div class="detail-price"><span>Стоимость</span><strong>${esc(item.price.replace("от ", ""))}</strong><small>Финальная оценка после брифа</small></div></header>
      <div class="mechanic-detail-media"><img src="${esc(item.image)}" alt="Пример оформления механики ${esc(item.title)}" width="1400" height="800" /></div>
      <section class="deliverables"><div><span class="eyebrow">Что входит</span><h2>Всё необходимое для первого запуска.</h2></div><ol>${deliverables.map((text, index) => `<li><span>${String(index + 1).padStart(2, "0")}</span><strong>${esc(text)}</strong></li>`).join("")}</ol></section>
      ${isAudience ? audiencePreview(state) : ""}
      <section class="result-note"><div><span class="eyebrow">Следующий шаг</span><h2>Зафиксируем площадку, аудиторию и задачу.</h2><p>Заявка не требует оплаты. Сначала согласуем сценарий и объём производства.</p></div>${routeButton(`/order/${item.slug}`, "Оставить заявку", { tone: "signal" })}</section>
    </section>
  `;
}
