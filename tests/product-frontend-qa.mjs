import fs from "node:fs";

const playwrightModule = process.env.BEERJA_PLAYWRIGHT_MODULE || "playwright";
const { chromium } = await import(playwrightModule);
const baseUrl = process.env.BEERJA_BASE_URL || "http://127.0.0.1:5234";
const outputPath = process.env.BEERJA_QA_OUTPUT || "/private/tmp/beerja-product-qa.json";
const executablePath = process.env.BEERJA_CHROME_PATH || undefined;

const proof = {
  generatedAt: new Date().toISOString(),
  status: "RUNNING",
  checks: [],
  responsive: [],
  consoleErrors: [],
  pageErrors: [],
  failed: []
};

function assert(condition, message, details = null) {
  if (!condition) {
    const error = new Error(message);
    error.details = details;
    throw error;
  }
}

function pass(name, details = {}) {
  proof.checks.push({ name, status: "PASS", details });
}

function attachDiagnostics(page, label, options = {}) {
  page.on("console", (message) => {
    if (message.type() === "error" && !options.allowHttpErrors) proof.consoleErrors.push({ label, text: message.text() });
  });
  page.on("pageerror", (error) => proof.pageErrors.push({ label, text: error.message }));
}

async function clearProduct(page) {
  await page.goto(`${baseUrl}/app/`, { waitUntil: "networkidle" });
  await page.evaluate(() => localStorage.removeItem("beerja.product.v1"));
  await page.reload({ waitUntil: "networkidle" });
}

const browser = await chromium.launch({ headless: true, ...(executablePath ? { executablePath } : {}) });

try {
  const routes = [
    "/app/",
    "/app/player/join",
    "/app/player/account",
    "/app/player/account/empty",
    "/app/player/games/game-001",
    "/app/player/settings",
    "/app/host",
    "/app/host/account",
    "/app/host/account/empty",
    "/app/host/analytics/game-archive-1",
    "/app/mechanics",
    "/app/mechanics/audience-voice",
    "/app/order/cinema-3x3"
  ];
  const viewports = [
    { width: 360, height: 800 },
    { width: 390, height: 844 },
    { width: 430, height: 932 },
    { width: 768, height: 1024 },
    { width: 1024, height: 768 },
    { width: 1440, height: 900 }
  ];

  for (const viewport of viewports) {
    const context = await browser.newContext({ viewport });
    const page = await context.newPage();
    attachDiagnostics(page, `responsive-${viewport.width}`);
    for (const route of routes) {
      const response = await page.goto(`${baseUrl}${route}`, { waitUntil: "networkidle" });
      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      const productRoot = await page.locator(".product-root").count();
      assert(response?.ok(), `Route ${route} returned ${response?.status()}`, { viewport });
      assert(productRoot === 1, `Route ${route} did not render product root`, { viewport });
      assert(overflow <= 1, `Horizontal overflow on ${route}`, { viewport, overflow });
      proof.responsive.push({ route, viewport, overflow, status: "PASS" });
    }
    await context.close();
  }
  pass("13 production routes × 6 viewports", { checks: proof.responsive.length });

  const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
  const page = await context.newPage();
  attachDiagnostics(page, "product-flows");
  await clearProduct(page);

  const internalLinks = await page.locator("a[data-route]").evaluateAll((links) => links.map((link) => link.getAttribute("href")));
  assert(internalLinks.every((href) => href?.startsWith("/app/")), "Product links must have working /app fallback href", internalLinks);
  pass("Progressive-enhancement hrefs", { count: internalLinks.length });

  await page.goto(`${baseUrl}/app/player/join`, { waitUntil: "networkidle" });
  await page.locator('[data-testid="join-room-submit"]').click();
  assert((await page.locator(".field-error").innerText()).includes("Введите код"), "Empty room code must show validation");
  await page.locator("#room-code").fill("QR-2048");
  await page.locator('[data-testid="join-room-submit"]').click();
  await page.waitForURL("**/app/player/room");
  assert((await page.locator(".page-intro h1").innerText()).includes("Киновечер"), "Room confirmation must show selected game");
  pass("Room code → confirmation");

  await page.locator('[data-route="/auth/player"]').click();
  await page.locator('[data-testid="auth-submit"]').click();
  assert(await page.locator(".field-error").count() === 2, "Login must validate login and password");
  await page.locator("#auth-login").fill("maya");
  await page.locator("#auth-password").fill("password1");
  await page.locator('[data-testid="auth-submit"]').click();
  await page.waitForURL("**/app/player/account");
  assert(await page.locator(".account-game").count() === 3, "Player account must show upcoming and completed games");
  assert(await page.locator(".achievement-grid article").count() === 4, "Player account must show achievements");
  pass("Player auth → account");

  await page.locator('.account-game:not(.upcoming) [data-route^="/player/games/"]').first().click();
  await page.waitForURL("**/app/player/games/game-001");
  assert(await page.locator(".result-metrics article").count() === 4, "Player result must show four metrics");
  assert(await page.locator(".round-breakdown article").count() === 3, "Player result must show three rounds");
  await page.goBack({ waitUntil: "networkidle" });
  assert(await page.locator(".account-page").count() === 1, "Browser back must restore player account");
  pass("Player game detail + browser back");

  await page.goto(`${baseUrl}/app/host/account`, { waitUntil: "networkidle" });
  assert(await page.locator(".host-game-card").count() === 3, "Host account must show ready, draft and completed games");
  await page.locator('[data-route^="/host/analytics/"]').click();
  assert(await page.locator(".analytics-metrics article").count() === 4, "Host analytics must show four metrics");
  assert(await page.locator(".round-bars article").count() === 3, "Host analytics must show three rounds");
  pass("Host account → analytics");

  await page.goto(`${baseUrl}/app/mechanics/audience-voice`, { waitUntil: "networkidle" });
  await page.locator('[data-product-action="preview-select"]').first().click();
  await page.locator('[data-product-action="preview-submit"]').click();
  assert((await page.locator(".preview-result strong").innerText()) === "68%", "Audience preview must render result");
  pass("Audience mechanic preview");

  await page.evaluate(() => localStorage.removeItem("beerja.product.v1"));
  await page.goto(`${baseUrl}/app/order/cinema-3x3`, { waitUntil: "networkidle" });
  await page.locator('[data-testid="order-primary"]').click();
  assert((await page.locator(".field-error").innerText()).includes("тип события"), "Order must validate first step");
  await page.locator('[data-field="eventType"]').first().click();
  await page.locator('[data-field="audience"]').nth(1).click();
  await page.locator('[data-testid="order-primary"]').click();
  await page.locator('[data-testid="order-primary"]').click();
  await page.locator("#order-date").fill("2026-08-24");
  await page.locator("#order-city").fill("Москва");
  await page.locator('[data-field="budget"]').nth(1).click();
  await page.locator('[data-testid="order-primary"]').click();
  await page.locator("#order-name").fill("Анна");
  await page.locator("#order-contact").fill("@anna_event");
  await page.locator("#order-email").fill("anna@example.ru");
  await page.locator('[data-testid="order-primary"]').click();
  assert(await page.locator(".order-summary").count() === 1, "Order summary must render before submit");
  await page.locator('[data-testid="order-primary"]').click();
  await page.waitForSelector(".order-success");
  assert((await page.locator(".order-success h1").innerText()) === "Приняли задачу.", "Order must finish in submitted state");
  pass("Five-step order flow");

  await page.goto(`${baseUrl}/app/`, { waitUntil: "networkidle" });
  let keyboardActivations = 0;
  for (let index = 0; index < 18; index += 1) {
    await page.keyboard.press("Tab");
    const tag = await page.evaluate(() => document.activeElement?.tagName || "");
    if (["A", "BUTTON", "INPUT"].includes(tag)) keyboardActivations += 1;
  }
  assert(keyboardActivations >= 14, "Main product navigation must be keyboard reachable", { keyboardActivations });
  pass("Keyboard reachability", { keyboardActivations });

  const legacyPage = await context.newPage();
  attachDiagnostics(legacyPage, "legacy-tour");
  await legacyPage.goto(`${baseUrl}/app/?tour=1&clear=1`, { waitUntil: "networkidle" });
  assert(await legacyPage.locator(".phone-shell").count() === 1, "Guided tour must remain available");
  assert(await legacyPage.locator(".product-root").count() === 0, "Guided tour must stay isolated from product shell");
  pass("Legacy guided tour isolation");

  const soloPlayerPage = await context.newPage();
  attachDiagnostics(soloPlayerPage, "solo-player-autostart");
  await soloPlayerPage.goto(`${baseUrl}/app/?demo=0&role=player&player=lobby&clear=1`, { waitUntil: "networkidle" });
  await soloPlayerPage.locator('[data-testid="captain-vote-submit"]').click();
  await soloPlayerPage.waitForSelector('[data-testid="question-surface"]');
  assert(await soloPlayerPage.locator('[data-testid="answer-submit"]').count() === 1, "Selected local captain must reach the first answer automatically");
  assert((await soloPlayerPage.locator(".hero-title").innerText()).includes("Выберите ответ"), "Solo player flow must promote the selected local captain");
  pass("Captain vote → automatic first question in mock preview");

  const apiPlayerPage = await context.newPage();
  attachDiagnostics(apiPlayerPage, "api-player-host-boundary");
  await apiPlayerPage.goto(`${baseUrl}/app/?data=api&role=player&player=lobby&clear=1`, { waitUntil: "networkidle" });
  await apiPlayerPage.locator('[data-testid="captain-vote-submit"]').click();
  await apiPlayerPage.waitForTimeout(1600);
  assert(await apiPlayerPage.locator('[data-testid="question-surface"]').count() === 0, "API mode must keep game start under host/backend control");
  pass("API mode preserves host-controlled game start");

  const livePage = await context.newPage();
  attachDiagnostics(livePage, "live-exit");
  await livePage.goto(`${baseUrl}/app/?role=host&host=question&clear=1`, { waitUntil: "networkidle" });
  await livePage.locator('[data-action="request-product-exit"]').click();
  assert(await livePage.locator('[aria-label="Выйти из активной игры"]').count() === 1, "Live exit must require confirmation");
  await livePage.locator('[data-action="cancel-product-exit"]').click();
  assert(await livePage.locator('[aria-label="Выйти из активной игры"]').count() === 0, "Player must be able to stay in the live game");
  await livePage.locator('[data-action="request-product-exit"]').click();
  await livePage.locator('[data-action="confirm-product-exit"]').click();
  await livePage.waitForURL(`${baseUrl}/app/`);
  assert(await livePage.locator(".product-root").count() === 1, "Confirmed live exit must return to the product home");
  pass("Confirmed exit from active live game");

  const apiPage = await context.newPage();
  attachDiagnostics(apiPage, "api-mode", { allowHttpErrors: true });
  await apiPage.goto(`${baseUrl}/app/player/join?data=api`, { waitUntil: "networkidle" });
  await apiPage.locator("#room-code").fill("QR-2048");
  await apiPage.locator('[data-testid="join-room-submit"]').click();
  await apiPage.waitForSelector(".status-panel.error");
  assert(apiPage.url().includes("data=api"), "API mode must remain in URL after failure");
  assert(!apiPage.url().includes("/player/room"), "API failure must not become mock room success");
  pass("API mode has no mock-success fallback");

  await context.close();

  assert(proof.consoleErrors.length === 0, "Unexpected console errors", proof.consoleErrors);
  assert(proof.pageErrors.length === 0, "Unexpected page errors", proof.pageErrors);
  proof.status = "PASS";
} catch (error) {
  proof.status = "FAIL";
  proof.failed.push({ message: error.message, details: error.details || null, stack: error.stack });
} finally {
  proof.finishedAt = new Date().toISOString();
  fs.writeFileSync(outputPath, `${JSON.stringify(proof, null, 2)}\n`);
  await browser.close();
}

console.log(JSON.stringify({
  status: proof.status,
  output: outputPath,
  checks: proof.checks.length,
  responsive: proof.responsive.length,
  consoleErrors: proof.consoleErrors.length,
  pageErrors: proof.pageErrors.length,
  failed: proof.failed
}, null, 2));

if (proof.status !== "PASS") process.exitCode = 1;
