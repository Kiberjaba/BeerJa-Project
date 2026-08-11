# BeerJa gameplay API contract v1

Base URL: `/api/v1`

Существующий `UserController` остаётся источником авторизации. Ниже — контракт следующего слоя; endpoint-ы перечислены до реализации, чтобы frontend, backend и math work могли идти параллельно.

## Existing user endpoints

- `GET /User/yandex-login`
- `GET /User/yandex-callback?code=...`
- `POST /User/register`
- `POST /User/login`
- `GET /User/me`

## Game state

### `GET /games/{gameId}/state`

Возвращает `LiveGameStateDto` из `BeejaServer/Contracts/GameplayContracts.cs`.

Требования:

- Ответ является role-aware projection для текущего JWT user.
- Правильный ответ не попадает в payload до reveal phase.
- Public projection не содержит приватные ответы команд.
- `EndsAt` передаётся в UTC.
- `Version` обязателен.

## Captain vote

### `POST /games/{gameId}/captain-votes`

```json
{
  "candidateUserId": 42,
  "expectedVersion": 17
}
```

Ответ: актуальный `LiveGameStateDto`.

Ошибки: `403` не участник команды; `409` игра уже началась или версия устарела; `422` кандидат не состоит в команде.

## Team answer

### `POST /games/{gameId}/answers`

```json
{
  "questionId": 301,
  "values": ["Сталкер"],
  "expectedVersion": 25
}
```

Сервер проверяет роль капитана, фазу, дедлайн, тип вопроса, число вариантов и отсутствие предыдущего финального ответа. Повторный submit возвращает `409` и не меняет данные.

## Host commands

### `POST /games/{gameId}/commands`

```json
{
  "command": "start-question",
  "expectedVersion": 31
}
```

Допустимые команды первой версии:

- `start-game`
- `start-round`
- `start-question`
- `pause-question`
- `resume-question`
- `close-question`
- `start-reveal`
- `next-reveal`
- `show-leaderboard`
- `finish-game`

Только ведущий/организатор с нужным permission может выполнять команды.

## Feedback

### `POST /games/{gameId}/feedback`

```json
{
  "scope": "round",
  "roundIndex": 0,
  "rating": 5,
  "skipped": false
}
```

`scope`: `round` или `overall`. Для skip `rating` может быть `null`.

## Realtime

После стабилизации HTTP-команд добавить SignalR hub `/hubs/games/{gameId}`. Событие `game-state-changed` передаёт либо целый role-aware state, либо его version + причину для повторного `GET`. HTTP остаётся авторитетным fallback.

## Math service boundary

Рекомендуемый отдельный интерфейс:

```csharp
public interface IGameScoringService
{
    ScoreResult Score(QuestionDto question, IReadOnlyList<string> submittedValues);
    IReadOnlyList<LeaderboardEntryDto> BuildLeaderboard(GameSnapshot snapshot);
}
```

Сервис должен быть детерминированным, не обращаться к HTTP/DB и иметь unit-тесты на single, multiple, text normalization, unanswered, tie-break и равенство очков.
