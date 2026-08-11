const APP_BASE = "/app";

function normalize(pathname) {
  const raw = String(pathname || "").split("?")[0].split("#")[0];
  const withoutBase = raw.startsWith(APP_BASE) ? raw.slice(APP_BASE.length) : raw;
  const clean = `/${withoutBase}`.replace(/\/{2,}/g, "/").replace(/\/$/, "");
  return clean === "" || clean === "/index.html" ? "/home" : clean;
}

export function currentRoute() {
  return normalize(window.location.pathname);
}

export function productUrl(route) {
  const normalized = normalize(route);
  const pathname = normalized === "/home" ? `${APP_BASE}/` : `${APP_BASE}${normalized}`;
  const dataMode = new URLSearchParams(window.location.search).get("data");
  return dataMode === "api" ? `${pathname}?data=api` : pathname;
}

export function navigate(route, options = {}) {
  const href = productUrl(route);
  const method = options.replace ? "replaceState" : "pushState";
  window.history[method](options.state || null, "", href);
  window.dispatchEvent(new CustomEvent("beerja:route-changed", { detail: { route: currentRoute() } }));
}

export function isRoute(route, prefix) {
  return route === prefix || route.startsWith(`${prefix}/`);
}

export function installRouter(onRoute) {
  const handle = () => onRoute(currentRoute());
  window.addEventListener("popstate", handle);
  window.addEventListener("beerja:route-changed", handle);
  document.addEventListener("click", (event) => {
    const target = event.target.closest("[data-route]");
    if (!target) return;
    event.preventDefault();
    navigate(target.dataset.route);
  });
  handle();
}
