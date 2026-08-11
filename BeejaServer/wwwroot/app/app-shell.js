import { esc, routeButton } from "./product-ui.js";

function active(route, path) {
  return route === path || route.startsWith(`${path}/`) ? " active" : "";
}

export function renderAppShell({ route, content, title = "BeerJa", backRoute = "", accountRoute = "/player/account", live = false }) {
  const back = backRoute
    ? `<a class="shell-icon" href="/app${esc(backRoute)}" data-route="${esc(backRoute)}" aria-label="Назад">←</a>`
    : `<a class="brand-lockup" href="/app/" data-route="/home" aria-label="BeerJa — главная"><span class="brand-mark">B</span><strong>BeerJa</strong></a>`;
  return `
    <div class="product-root${live ? " live-product" : ""}">
      <a class="skip-link" href="#product-main">К содержанию</a>
      <header class="product-nav">
        ${back}
        <nav aria-label="Основная навигация">
          <a class="nav-link${active(route, "/mechanics")}" href="/app/mechanics" data-route="/mechanics">Механики</a>
          <a class="nav-link${active(route, "/order")}" href="/app/order" data-route="/order">Заказать</a>
        </nav>
        <a class="account-link" href="/app${esc(accountRoute)}" data-route="${esc(accountRoute)}" aria-label="Открыть кабинет"><span>Кабинет</span><b>М</b></a>
      </header>
      <main id="product-main" tabindex="-1" aria-label="${esc(title)}">
        ${content}
      </main>
      <footer class="product-footer">
        <div><strong>BeerJa</strong><span>Интерактивные события без ручной склейки сервисов.</span></div>
        <div class="footer-links">
          ${routeButton("/mechanics", "Механики", { className: "footer-link" })}
          ${routeButton("/order", "Оставить заявку", { className: "footer-link" })}
          <a class="footer-link" href="mailto:hello@beerja.ru">hello@beerja.ru</a>
        </div>
      </footer>
    </div>
  `;
}
