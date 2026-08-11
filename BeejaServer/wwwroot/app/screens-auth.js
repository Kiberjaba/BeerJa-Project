import { actionButton, esc, field, routeButton, statusPanel } from "./product-ui.js";

function loginForm(state, role) {
  return `
    <form class="auth-form" data-product-form="auth" novalidate>
      ${field({ id: "auth-login", label: "Email или имя пользователя", value: state.auth.login, placeholder: "maya.volkova", autocomplete: "username", error: state.auth.errors.login })}
      ${field({ id: "auth-password", label: "Пароль", value: state.auth.password, placeholder: "Не менее 8 символов", type: "password", autocomplete: "current-password", error: state.auth.errors.password })}
      <div class="form-between"><label class="check-line"><input type="checkbox" data-auth-bind="remember" ${state.auth.remember ? "checked" : ""} /><span>Запомнить меня</span></label><button class="text-action" data-product-action="forgot-password" type="button">Не помню пароль</button></div>
      ${actionButton("submit-auth", state.auth.status === "loading" ? "Входим…" : role === "host" ? "Войти как ведущий" : "Войти", { tone: "signal", disabled: state.auth.status === "loading", testId: "auth-submit" })}
    </form>
  `;
}

function registerForm(state, role) {
  return `
    <form class="auth-form" data-product-form="auth" novalidate>
      ${field({ id: "auth-name", label: role === "host" ? "Имя ведущего" : "Как к вам обращаться", value: state.auth.name, placeholder: role === "host" ? "Илья Соколов" : "Майя", autocomplete: "name", error: state.auth.errors.name })}
      ${field({ id: "auth-email", label: "Email", value: state.auth.email, placeholder: "name@example.ru", type: "email", autocomplete: "email", error: state.auth.errors.email })}
      ${field({ id: "auth-password", label: "Пароль", value: state.auth.password, placeholder: "Не менее 8 символов", type: "password", autocomplete: "new-password", error: state.auth.errors.password })}
      <label class="check-line consent-line"><input type="checkbox" data-auth-bind="consent" ${state.auth.consent ? "checked" : ""} /><span>Согласен с правилами сервиса и обработкой данных</span></label>
      ${state.auth.errors.consent ? `<small class="field-error" role="alert">${esc(state.auth.errors.consent)}</small>` : ""}
      ${actionButton("submit-auth", state.auth.status === "loading" ? "Создаём профиль…" : "Создать профиль", { tone: "signal", disabled: state.auth.status === "loading", testId: "auth-submit" })}
    </form>
  `;
}

export function renderAuth(state, role = "player") {
  const isHost = role === "host";
  const mode = state.auth.mode;
  return `
    <section class="auth-page compact-page">
      <div class="auth-story">
        <span class="eyebrow">${isHost ? "Кабинет ведущего" : "Профиль игрока"}</span>
        <h1>${isHost ? "Все игры и сценарии — в одном месте." : "Результаты остаются с вами после финала."}</h1>
        <p>${isHost ? "Готовьте комнаты, запускайте механику и возвращайтесь к аналитике после события." : "Команды, достижения, история и опыт платформы привязаны к одному профилю."}</p>
        <div class="auth-visual"><img src="${isHost ? "/assets/generated/quiz-host-control-v1-web.jpg" : "/assets/generated/quiz-profile-celebration-v1-web.jpg"}" alt="${isHost ? "Ведущий управляет интерактивной игрой" : "Участники празднуют финал игры"}" width="900" height="1100" /></div>
      </div>
      <div class="auth-panel">
        <div class="auth-tabs" role="tablist" aria-label="Вход или регистрация">
          <button class="auth-tab ${mode === "login" ? "active" : ""}" data-product-action="auth-mode" data-value="login" role="tab" aria-selected="${mode === "login"}" type="button">Вход</button>
          <button class="auth-tab ${mode === "register" ? "active" : ""}" data-product-action="auth-mode" data-value="register" role="tab" aria-selected="${mode === "register"}" type="button">Регистрация</button>
        </div>
        <div class="auth-heading"><h2>${mode === "login" ? "Продолжить" : "Новый профиль"}</h2><p>${isHost ? "Роль: ведущий" : "Роль: игрок"}</p></div>
        ${state.auth.status === "error" ? statusPanel("Не удалось войти", "Проверьте данные или повторите вход через Яндекс.", { error: true, tone: "error" }) : ""}
        ${mode === "login" ? loginForm(state, role) : registerForm(state, role)}
        <div class="auth-divider"><span>или</span></div>
        ${actionButton("yandex-auth", "Продолжить с Яндекс ID", { tone: "cloud", testId: "yandex-auth" })}
        <p class="auth-switch-role">${isHost ? "Хотите присоединиться к игре?" : "Проводите мероприятие?"} ${routeButton(isHost ? "/auth/player" : "/auth/host", isHost ? "Войти как игрок" : "Войти как ведущий", { className: "text-link" })}</p>
      </div>
    </section>
  `;
}

export function renderForgotPassword(state) {
  return `
    <section class="compact-page narrow-page">
      <span class="eyebrow">Восстановление доступа</span>
      <h1>Пришлём ссылку на почту.</h1>
      <p>Используйте email, с которым регистрировались в BeerJa.</p>
      <form class="focus-form single-form" data-product-form="forgot" novalidate>
        ${field({ id: "forgot-email", label: "Email", value: state.auth.email, placeholder: "name@example.ru", type: "email", autocomplete: "email", error: state.auth.errors.email })}
        ${actionButton("submit-forgot", state.auth.status === "loading" ? "Отправляем…" : "Получить ссылку", { tone: "signal", disabled: state.auth.status === "loading" })}
        ${state.auth.status === "sent" ? statusPanel("Письмо отправлено", "Если профиль существует, ссылка уже в почте.") : ""}
      </form>
    </section>
  `;
}
