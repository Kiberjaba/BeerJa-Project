# BeerJa

Информационная система управления интерактивными мероприятиями.

## Что находится в репозитории

- `BeejaServer/` — ASP.NET Core backend, авторизация и статический frontend.
- `BeejaServer/wwwroot/app/` — интегрированный mobile-first интерфейс Live Signal.
- `BeejaServer/Contracts/GameplayContracts.cs` — DTO-контракт между frontend, backend и игровой математикой.
- `docs/LIVE_SIGNAL_INTEGRATION.md` — архитектура интеграции и порядок дальнейшей разработки.
- `docs/API_CONTRACT_V1.md` — ожидаемый HTTP-контракт игровых сценариев.

## Запуск

Требуется .NET SDK 10.

```bash
dotnet restore Beerja.slnx
dotnet run --project BeejaServer/BeejaServer.csproj
```

После запуска корень приложения открывает guided tour. Основные маршруты:

- `/app/?tour=1` — линейный сценарий на одном устройстве.
- `/app/?demo=0&role=player` — чистый интерфейс игрока.
- `/app/?demo=0&role=host` — чистый интерфейс ведущего.
- `/app/?publictour=1` — общий экран зала 16:9.
- `/app/?demo=1&role=overview` — внутренняя QA-панель всех ролей.

Frontend по умолчанию работает на локальных mock-данных. Режим `/app/?data=api` включает интеграционный режим для постепенного подключения endpoint-ов из `docs/API_CONTRACT_V1.md`.

## Проверка

```bash
dotnet test Beerja.slnx
```

Перед изменением игрового flow прочитайте `docs/LIVE_SIGNAL_INTEGRATION.md`.
