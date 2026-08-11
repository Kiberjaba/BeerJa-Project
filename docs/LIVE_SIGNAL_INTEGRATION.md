# Live Signal integration

## Цель

Live Signal перенесён в BeerJa как полноценный статический frontend внутри ASP.NET Core, а не как ссылка на внешний прототип. Сервер продолжает владеть авторизацией и постепенно забирает владение игровой сессией, состоянием, таймером, начислением баллов и рейтингами.

## Точка входа

- Product source: `BeejaServer/wwwroot/app/`.
- ASP.NET Core раздаёт папку через `UseStaticFiles()`.
- Root `BeejaServer/wwwroot/index.html` переводит пользователя в `/app/?tour=1`.
- Все изображения находятся в `BeejaServer/wwwroot/assets/generated/`; внешняя Open Design папка для runtime не требуется.

## Что уже работает во frontend

- Игрок, капитан, организатор, ведущий, профиль и общий экран 16:9.
- Три раунда по три вопроса.
- Single, multiple, short text и image question.
- 15-секундный таймер, pause/resume/manual close/expiry.
- Captain-only submit и неизменяемый ответ после отправки.
- Разбор трёх ответов после раунда, лидерборд и финал.
- Линейный 27-шаговый tour на одном устройстве.
- Русский интерфейс, keyboard/focus/reduced-motion состояния.

## Временный mock boundary

`state-machine.js` — проверенная браузерная модель прототипа. Она остаётся источником состояния только в `mock`-режиме. Не переносите этот файл целиком в controller и не пытайтесь синхронизировать localStorage между реальными клиентами.

Для backend-интеграции добавлен `backend-bridge.js`:

- `window.BeerJaFrontend.api` — единая точка HTTP-вызовов.
- `beerja:ui-action` — событие до обработки пользовательского действия.
- `beerja:state-changed` — снимок состояния после изменения.
- `beerja:ready` — frontend готов к интеграции.
- `/app/?data=api` — явный интеграционный режим; обычный demo остаётся детерминированным.

События нужны для диагностики и постепенного подключения endpoint-ов. Они не являются серверным event log.

## Владение состоянием в production

| Область | Владелец |
| --- | --- |
| Пользователь, JWT, Yandex ID | Backend |
| Игра, раунды, вопросы и правила | Backend/database |
| Команда и голосование капитана | Backend |
| Текущая фаза игры | Backend, изменяет только ведущий |
| Дедлайн таймера | Backend UTC timestamp |
| Ответ команды и immutable lock | Backend transaction |
| Баллы, tie-break и leaderboard | Отдельный backend math/domain service |
| Локальные sheet/toast/focus состояния | Frontend |
| Guided tour и demo fixtures | Frontend only |

## Инварианты для backend и матмодели

1. Ведущий — единственный актор, меняющий глобальную фазу.
2. Капитан и команда блокируются при старте игры.
3. Только капитан может отправить ответ команды.
4. Для вопроса принимается максимум один финальный ответ команды.
5. После submit или закрытия вопроса ответ неизменяем.
6. Сервер хранит `EndsAt`, клиенты лишь отображают оставшееся время.
7. За раунд: три вопроса → три раскрытия → один leaderboard.
8. Game score и profile experience — разные величины.
9. Tie-break использует суммарное время правильных ответов.
10. Каждая команда видит свою проекцию, а public screen не принимает команды.

## Optimistic concurrency

`LiveGameStateDto.Version` должен увеличиваться после каждого серверного изменения. Команды клиента передают `ExpectedVersion`. При несовпадении backend возвращает `409 Conflict` и актуальный state. Это предотвращает двойной submit и конкурирующие команды ведущего.

## Порядок подключения backend

1. Реализовать read-only `GET /games/{gameId}/state`.
2. Заменить mock team/captain projection серверной.
3. Подключить `captain-votes` и серверный lock при старте.
4. Подключить host commands и серверный timer deadline.
5. Подключить immutable answer submission.
6. Вынести scoring/tie-break/leaderboard в domain service и покрыть unit-тестами.
7. Подключить feedback, analytics и profile XP.
8. После полного e2e удалить runtime-зависимость product mode от localStorage; tour оставить автономным.

## Правило изменений

Не редизайнить интерфейс параллельно с подключением backend. Сначала сохранить экранные контракты и заменить источник данных через bridge. Любое изменение state flow должно сопровождаться тестом инварианта на backend и проверкой соответствующего frontend-маршрута.
