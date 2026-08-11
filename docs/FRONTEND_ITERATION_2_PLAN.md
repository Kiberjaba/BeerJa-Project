# BeerJa frontend iteration 2

Статус: реализовано, финальная проверка пройдена
Источник требований: Telegram export от 9–11 августа 2026 года
Основа: интегрированный Live Signal в `BeejaServer/wwwroot/app/`

## Цель итерации

Превратить текущий качественный demo игрового процесса в связанный frontend-продукт, который backend-команда сможет подключать по частям:

1. человек попадает на публичную главную;
2. выбирает роль игрока или ведущего;
3. игрок входит, присоединяется к комнате по QR/коду и открывает кабинет;
4. ведущий входит, выбирает механику, оформляет заказ или открывает свою игру;
5. обе роли видят историю и аналитику;
6. существующий live-game flow остаётся рабочим и получает явные API-boundary.

Финальный frontend этой итерации остаётся mock-first, но каждая бизнес-операция должна иметь понятный action, payload и ожидаемый backend endpoint. Реальные DB, scoring, payment processing и realtime не входят во frontend-итерацию.

## 1. Аудит требований

| Требование из переписки | Текущее состояние | Что сделать |
| --- | --- | --- |
| Общая главная «игрок / ведущий» | Есть только старый `glav.html`; root открывает tour | Собрать новую canonical landing внутри `/app/`, сделать её root entry |
| Нормальная работа на разных экранах | Live Signal проверен на 360/390/430; старые страницы отдельно | Новые страницы сделать mobile-first и проверить также 768/1024/1440 |
| Вход и регистрация игрока | Есть fake Yandex login, нет самостоятельной регистрации | Добавить auth choice, login/register states, Yandex action и ошибки |
| Вход по QR | QR есть на public screen | Добавить deep-link contract и экран подтверждения комнаты |
| Ввод номера комнаты с главной | Отсутствует | Добавить room-code form, validation, loading/not-found/success |
| Личный кабинет игрока | Есть профиль после финала | Сделать отдельный route: профиль, прогресс, ближайшая игра, история, достижения |
| Статистика игрока за игру | История есть без detail view | Добавить game result detail: место, ответы, очки игры, XP, команда |
| Вход/регистрация ведущего | Отсутствует как отдельный flow | Добавить role-aware auth entry для ведущего |
| Личный кабинет ведущего | Есть organizer flow и старый `lichved.html` | Собрать новый host dashboard: ближайшие/прошедшие/черновики, быстрые действия |
| Примеры игровых механик | Есть шаблоны организатора | Превратить в публичный каталог с detail pages и понятными use cases |
| Градация механик по сложности и цене | Отсутствует | Добавить `Базовая / Сценарная / Под ключ`, состав и draft prices |
| Заказать игру | Отсутствует | Добавить multi-step order request: событие, аудитория, механика, дата, контакты, summary |
| Запустить игру | Host live flow готов | Связать dashboard card с preflight конкретной комнаты |
| Аналитика ведущего | Есть report и organizer analytics | Сделать самостоятельный route из истории игр и расширить до полезного отчёта |
| Вернуться на главную везде | Нет общего navigation contract | Добавить app shell, back, home, active route и safe exit из live-game |
| Предстоящие и прошедшие игры/фото | Только в старой landing-болванке | Добавить в новую landing как реальные content sections |
| Опрос/сбор мнений/голосование | Есть feedback template, но нет отдельной механики | Добавить механику «Мнение зала» и runnable preview |
| Понятно разделить frontend-файлы | Live flow разделён по ролям; legacy HTML дублирует продукт | Ввести новые role-specific modules и оставить legacy pages только как redirects |
| Подготовить frontend к backend/БД/матмодели | Gameplay bridge и DTO есть | Расширить bridge на auth, rooms, profile, catalog, orders и analytics |
| Оплата | Нет | Сделать только UI handoff-state после заказа; реальный эквайринг отдельным этапом |

## 2. Целевая информационная архитектура

### Публичный слой

- `/app/home` — продукт, выбор роли, ввод комнаты, ближайшие события, фото, механики.
- `/app/mechanics` — каталог игровых механик.
- `/app/mechanics/{slug}` — описание, сложность, цена от, пример, CTA заказа.
- `/app/order` — заявка на проведение игры.
- `/app/auth` — вход/регистрация с выбранной ролью.

### Игрок

- `/app/player/join` — ввод кода или подтверждение QR deep link.
- `/app/player/account` — самостоятельный кабинет.
- `/app/player/games/{id}` — результат и статистика конкретной игры.
- `/app/player/live/{gameId}` — существующий player/captain flow.

### Ведущий

- `/app/host/account` — ближайшие игры, черновики, прошедшие игры.
- `/app/host/games/{id}` — карточка игры и preflight.
- `/app/host/live/{gameId}` — существующий пульт ведущего.
- `/app/host/analytics/{gameId}` — отчёт и статистика.

### Организатор

Существующий builder остаётся внутри host/organizer account и не становится отдельным публичным продуктом. Он открывается действием «Настроить игру».

### Demo и QA

- `/app/?tour=1` — сохранить без изменений.
- `/app/?demo=1&role=overview` — сохранить внутреннюю QA-панель.
- `/app/?publictour=1` — сохранить public-screen tour.

## 3. Новые frontend-модули

Работа остаётся в текущем vanilla JS/CSS stack.

- `router.js` — route parsing, history, refresh restore, back/home semantics.
- `app-shell.js` — header/navigation/account controls и safe live-game exit.
- `screens-public-site.js` — home, events, gallery, role entry.
- `screens-auth.js` — login/register/Yandex/error/loading states.
- `screens-user-account.js` — кабинет игрока и game result detail.
- `screens-host-account.js` — кабинет ведущего, game cards, drafts/history.
- `screens-mechanics.js` — каталог, detail, «Мнение зала» preview.
- `screens-order.js` — order steps, validation, summary, submitted state.
- `screens-analytics.js` — player/host analytics projections.
- `product-data.js` — mock fixtures публичного сайта, каталога и аккаунтов.
- `product-contracts.js` — frontend payload mappers для backend-команды.

Существующие `screens-player.js`, `screens-host.js`, `screens-organizer*.js`, `screens-public.js` и `state-machine.js` не переписывать. Подключать их через router и bridge.

## 4. Карта пользовательских потоков

### Поток игрока

`Главная → код комнаты/QR → проверка комнаты → вход/регистрация → команда → lobby → игра → финал → кабинет → статистика игры`

Обязательные состояния:

- пустой/невалидный код;
- комната не найдена;
- игра ещё не открыта;
- комната найдена;
- auth loading/error;
- пользователь без игр;
- пользователь с историей;
- результат загружается/не найден.

### Поток ведущего

`Главная → для ведущего → каталог механик → карточка механики → заявка → подтверждение → кабинет → карточка игры → preflight → live control → отчёт`

Альтернативный поток постоянного ведущего:

`Главная → вход → кабинет → ближайшая игра → запуск`

Обязательные состояния:

- новый ведущий без заказов;
- заявка отправлена;
- игра в подготовке;
- игра готова;
- игра завершена;
- статистика ещё не собрана;
- статистика готова.

### Поток «Мнение зала»

`Каталог → Мнение зала → preview вопроса → preview результатов → добавить в заказ`

Типы первой версии preview:

- один вариант;
- несколько вариантов;
- шкала 1–5;
- короткий текст.

## 5. Визуальная система

Новые экраны продолжают Live Signal, а не старые отдельные HTML-болванки:

- mobile-first 390×844;
- dark cinematic surfaces;
- Onest/rounded sans, tabular figures для статистики;
- Signal `#C9FF47` как единственный action accent;
- Pulse `#705CFF`, Gold и Coral только для смысловых состояний;
- крупные media-led блоки;
- sticky primary action на мобильном;
- desktop — editorial layout без стандартного dashboard sidebar;
- motion через transform/opacity, reduced-motion fallback;
- без `alert()`, мёртвых ссылок и декоративных кнопок.

Для публичной landing перед реализацией отдельно зафиксировать design plan: hero architecture, typography, component composition и motion. Существующий игровой UI при этом не редизайнить.

## 6. Backend bridge v2

Frontend должен использовать один API adapter. Предлагаемый контракт:

### Auth

- `POST /User/register`
- `POST /User/login`
- `GET /User/yandex-login`
- `GET /User/me`

### Rooms

- `GET /rooms/by-code/{code}`
- `POST /rooms/{roomId}/join`
- `GET /games/{gameId}/state`

### Player account

- `GET /users/me/profile`
- `GET /users/me/games`
- `GET /users/me/games/{gameId}`

### Host account

- `GET /hosts/me/games`
- `GET /hosts/me/games/{gameId}`
- `GET /hosts/me/games/{gameId}/analytics`

### Catalog and orders

- `GET /mechanics`
- `GET /mechanics/{slug}`
- `POST /orders`
- `GET /orders/{orderId}`

Endpoint names are frontend proposals, not implemented backend. До согласования backend owner должен либо принять их, либо вернуть согласованный mapping. В `mock`-режиме те же методы возвращают deterministic fixtures.

Каждый mutate action должен иметь loading, success, validation, unauthorized, not-found, conflict и generic-error states. В `api`-режиме нельзя молча откатываться на mock после HTTP error.

## 7. Этапы реализации

### Wave 1 — product shell и landing

Результат:

- root открывает новую landing, а не guided tour;
- работают role entry, room-code form, mechanics/events/gallery sections;
- app shell даёт home/back/account navigation;
- существующие demo routes сохранены.

### Wave 2 — auth и кабинет игрока

Результат:

- login/register/Yandex mock states;
- room-code/QR join flow;
- отдельный player account;
- clickable game history и result detail;
- empty/loading/error states.

### Wave 3 — кабинет ведущего, каталог и заказ

Результат:

- host account с ближайшими, черновиками и историей;
- mechanics catalog + detail + complexity/price;
- «Мнение зала» preview;
- multi-step order request;
- переход к существующему builder/preflight/live flow.

### Wave 4 — аналитика и contracts

Результат:

- самостоятельные player/host analytics routes;
- расширенный backend bridge;
- documented payloads и state ownership;
- legacy HTML заменены redirect pages или исключены из canonical navigation.

### Wave 5 — QA и передача

Результат:

- regression старого Live Signal;
- новая route/interaction/accessibility/responsive suite;
- backend handoff matrix;
- обновлённые README и PR description.

Реализация доставляется двумя цельными commits: продуктовый frontend и документация/QA. Они добавляются в текущую fork-ветку `codex/live-signal-integration`, поэтому PR #2 обновляется автоматически. Новый PR не нужен.

## 8. Acceptance criteria

### Навигация

- Root показывает продуктовую landing.
- Любой non-live экран имеет понятный путь домой и назад.
- Refresh сохраняет текущий route.
- Browser back/forward работают без потери состояния формы.
- Выход из live-game требует подтверждения.

### Функциональность

- Ни одной обязательной кнопки с `alert()`, `href="#"` или отсутствующим action.
- Room-code form реально валидирует и проходит success/error branches.
- Auth forms валидируются и показывают server-shaped errors.
- History rows открывают конкретную статистику.
- Host game card открывает preflight/live/report в соответствии со статусом.
- Order form проходит все шаги и формирует однозначный payload.
- «Мнение зала» имеет работающий preview.

### Responsive и accessibility

- Проверки: 360×800, 390×844, 430×932, 768×1024, 1024×768, 1440×900.
- Нет horizontal overflow.
- Все actions доступны с клавиатуры.
- Есть видимый focus, корректные labels/errors и focus management после route change.
- Reduced motion поддерживается.

### Regression

- Существующий tour: все 27 шагов.
- Functional live-game: 13/13.
- Keyboard suite: 48 исходных действий без регрессий.
- 47-state audit: 47/47.
- Public screen сохраняет 16:9 и read-only.
- JavaScript syntax, .NET Release build и xUnit проходят.

### Новая QA-матрица

1. Landing → room code → player auth → account.
2. Landing → QR deep link → room confirmation → live lobby.
3. Landing → host → mechanic → order submit → host account.
4. Host account → ready game → preflight → live → analytics.
5. Player account → history → result detail → back.
6. Empty/error/loading states для player и host accounts.
7. Direct URL + refresh для каждого production route.
8. API mode failure не подменяется mock-успехом.

## 9. Definition of done перед сообщением команде

Итерация считается готовой к backend handoff только когда:

1. все требования из таблицы имеют route и рабочее интерактивное состояние;
2. каждый экран использует mock data через тот же adapter, который будет заменён API;
3. существующая игровая state machine не сломана;
4. весь acceptance checklist подтверждён отчётом и артефактами;
5. PR #2 содержит понятный screen map, API matrix и инструкции запуска;
6. backend owner может выбрать любой endpoint из bridge v2 и подключить его без редизайна frontend flow.

## 10. Внешние решения, которые не блокируют frontend

До реализации backend команда позже должна подтвердить:

- окончательные роли `host` и `organizer`: одна роль или две;
- цены и валюту в каталоге;
- обязательные поля заказа;
- нужна ли email/password регистрация вместе с Yandex;
- provider и юридический сценарий оплаты;
- точный deep-link формат QR.

До ответа используются clearly marked demo fixtures и предложенные contracts. Это не должно останавливать frontend-сборку.
