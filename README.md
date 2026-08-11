# BeerJa

Информационная система управления интерактивными мероприятиями.

## Что находится в репозитории

- `BeejaServer/` — ASP.NET Core backend, авторизация и статический frontend.
- `BeejaServer/wwwroot/app/` — интегрированный mobile-first интерфейс Live Signal.
- `BeejaServer/Contracts/GameplayContracts.cs` — DTO-контракт между frontend, backend и игровой математикой.
- `docs/LIVE_SIGNAL_INTEGRATION.md` — архитектура интеграции и порядок дальнейшей разработки.
- `docs/API_CONTRACT_V1.md` — ожидаемый HTTP-контракт игровых сценариев.
- `docs/FRONTEND_BACKEND_HANDOFF_V2.md` — карта продуктовых экранов и API-boundary для auth, rooms, кабинетов, каталога, заказов и аналитики.

## Запуск

Требуется .NET SDK 10.

```bash
dotnet restore Beerja.slnx
dotnet run --project BeejaServer/BeejaServer.csproj
```

После запуска корень приложения открывает продуктовую главную. Основные маршруты:

- `/app/` — публичная главная, выбор роли и вход в комнату.
- `/app/player/account` — кабинет игрока и история игр.
- `/app/host/account` — кабинет ведущего.
- `/app/mechanics` — каталог механик.
- `/app/order` — пошаговая заявка на игру.
- `/app/?tour=1` — линейный сценарий на одном устройстве.
- `/app/?demo=0&role=player` — чистый интерфейс игрока.
- `/app/?demo=0&role=host` — чистый интерфейс ведущего.
- `/app/?publictour=1` — общий экран зала 16:9.
- `/app/?demo=1&role=overview` — внутренняя QA-панель всех ролей.

Frontend по умолчанию работает на локальных mock-данных. Параметр `?data=api` на продуктовых маршрутах включает интеграционный режим без скрытого fallback на fixtures. Контракты описаны в `docs/FRONTEND_BACKEND_HANDOFF_V2.md` и `docs/API_CONTRACT_V1.md`.

## Проверка

```bash
dotnet test Beerja.slnx
```

Перед изменением игрового flow прочитайте `docs/LIVE_SIGNAL_INTEGRATION.md`.
