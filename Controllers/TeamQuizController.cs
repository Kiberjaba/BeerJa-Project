using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeejaServer.Data;
using BeejaServer.Models;

namespace BeejaServer.Controllers
{
    /// <summary>
    /// Логика КОМАНДНОГО КВИЗА.
    ///
    /// Подготовленная игра хранится в events.
    /// Конкретный запуск хранится в game_sessions.
    ///
    /// game_type:
    /// 1 = нишевая игра
    /// 2 = командный квиз
    /// 3 = индивидуальная игра
    ///
    /// Этот контроллер работает только с game_type = 2.
    ///
    /// Поддерживаются:
    /// /api/team-quiz/...
    /// /api/v1/games/...
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/team-quiz")]
    [Route("api/v1/games")]
    public class TeamQuizController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamQuizController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // AUTH
        // ============================================================

        private bool TryGetUserId(out int userId)
        {
            userId = 0;

            var claim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;

            return int.TryParse(claim, out userId);
        }

        private async Task<GameSession?> GetSessionAsync(int gameId)
        {
            return await _context.GameSessions
                .FirstOrDefaultAsync(x => x.Id == gameId);
        }

        private bool IsHost(GameSession session, int userId)
        {
            return session.OrganiserUserId == userId;
        }

        private IActionResult WrongGameType(GameSession session)
        {
            return BadRequest(new
            {
                message = "Эта игровая сессия не является командным квизом",
                gameId = session.Id,
                gameType = session.GameType
            });
        }

        // ============================================================
        // JOIN ROOM
        // ============================================================

        /// <summary>
        /// Игрок входит по коду комнаты и сразу указывает существующую
        /// команду.
        ///
        /// POST /api/team-quiz/join-room
        /// </summary>
        [HttpPost("join-room")]
        public async Task<IActionResult> JoinRoom(
            [FromBody] JoinTeamRoomDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Недействительный токен" });

            var roomCode = (dto.GameCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(roomCode))
                return BadRequest(new { message = "Не указан код комнаты" });

            if (dto.TeamId <= 0)
                return BadRequest(new { message = "Не выбрана команда" });

            var room = await _context.GameRoomCodes
                .Where(x => x.RoomCode == roomCode)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (room == null)
                return NotFound(new
                {
                    message = "Комната с таким кодом не найдена"
                });

            var session = await GetSessionAsync(room.GameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Игровая сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!session.IsActive)
                return BadRequest(new
                {
                    message = "Игра уже завершена"
                });

            if (session.CurrentStep != "lobby")
            {
                return BadRequest(new
                {
                    message =
                        "Нельзя подключиться к команде после начала игры"
                });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
                return Unauthorized(new
                {
                    message = "Пользователь не найден"
                });

            var team = await _context.Teams
                .FirstOrDefaultAsync(x => x.Id == dto.TeamId);

            if (team == null)
                return NotFound(new
                {
                    message = "Команда не найдена"
                });

            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(x =>
                    x.TeamId == dto.TeamId &&
                    x.UserId == userId &&
                    x.Status == "accepted");

            if (teamMember == null)
            {
                return BadRequest(new
                {
                    message = "Вы не состоите в этой команде"
                });
            }

            var livePlayer = await _context.LivePlayers
                .FirstOrDefaultAsync(x =>
                    x.GameId == session.Id &&
                    x.UserId == userId);

            if (livePlayer == null)
            {
                livePlayer = new LivePlayer
                {
                    GameId = session.Id,
                    UserId = userId,
                    Username = user.Username,
                    JoinedAt = DateTime.Now
                };

                _context.LivePlayers.Add(livePlayer);
            }

            await _context.SaveChangesAsync();

            await UpsertResponseAsync(
                session.Id,
                userId,
                "team_select",
                dto.TeamId.ToString());

            await DeleteResponseAsync(
                session.Id,
                userId,
                "captain_vote");

            await _context.SaveChangesAsync();

            var players = await GetGamePlayersAsync(session.Id);

            return Ok(new
            {
                gameId = session.Id,
                userId,
                playerId = livePlayer.Id,
                username = user.Username,
                roomCode,
                eventId = session.EventId,
                gameType = session.GameType,
                team = new
                {
                    id = team.Id,
                    name = team.Name
                },
                playersCount = players.Count
            });
        }

        // ============================================================
        // AVAILABLE TEAMS
        // ============================================================

        /// <summary>
        /// Команды текущего пользователя.
        ///
        /// GET /api/team-quiz/my-teams
        /// </summary>
        [HttpGet("my-teams")]
        public async Task<IActionResult> GetMyTeams()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var teams = await (
                from tm in _context.TeamMembers
                join t in _context.Teams
                    on tm.TeamId equals t.Id
                where tm.UserId == userId
                      && tm.Status == "accepted"
                orderby t.Name
                select new
                {
                    id = t.Id,
                    name = t.Name
                }
            ).ToListAsync();

            return Ok(teams);
        }

        // ============================================================
        // GAME STATE
        // ============================================================

        /// <summary>
        /// Состояние игры для игрока.
        ///
        /// GET /api/v1/games/{gameId}/state
        /// </summary>
        [HttpGet("{gameId:int}/state")]
        public async Task<IActionResult> GetGameState(int gameId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            var roomCode = await _context.GameRoomCodes
                .Where(x => x.GameId == gameId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.RoomCode)
                .FirstOrDefaultAsync();

            var player = await _context.LivePlayers
                .FirstOrDefaultAsync(x =>
                    x.GameId == gameId &&
                    x.UserId == userId);

            var selectedTeamId =
                await GetSelectedTeamIdAsync(
                    gameId,
                    userId);

            object? team = null;

            if (selectedTeamId.HasValue)
            {
                team = await _context.Teams
                    .Where(x => x.Id == selectedTeamId.Value)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name
                    })
                    .FirstOrDefaultAsync();
            }

            var captainInfo =
                await GetCaptainInfoAsync(
                    gameId,
                    selectedTeamId);

            var currentQuestion =
                await GetCurrentQuestionAsync(session);

            var teams =
                await GetGameTeamsAsync(gameId);

            var players =
                await GetGamePlayersDetailedAsync(gameId);

            var answeredTeams =
                await GetAnsweredTeamsCountAsync(
                    gameId,
                    session.CurrentStep);

            var phase =
                ResolvePhase(session.CurrentStep);

            return Ok(new
            {
                gameId = session.Id,
                eventId = session.EventId,
                gameType = session.GameType,
                roomCode,

                isActive = session.IsActive,
                currentStep = session.CurrentStep,
                phase,

                player = player == null
                    ? null
                    : new
                    {
                        id = player.UserId,
                        username = player.Username,
                        joined = true,
                        teamId = selectedTeamId,
                        isCaptain =
                            captainInfo?.CaptainUserId == userId
                    },

                team,

                captain = captainInfo == null
                    ? null
                    : new
                    {
                        id = captainInfo.CaptainUserId,
                        username = captainInfo.CaptainUsername,
                        teamId = captainInfo.TeamId
                    },

                teams,
                players,
                answeredTeams,

                question =
                    currentQuestion == null
                        ? null
                        : new
                        {
                            id = currentQuestion.Id,
                            eventId = currentQuestion.EventId,
                            questionText =
                                currentQuestion.QuestionText,
                            options =
                                ParseOptionsSafe(
                                    currentQuestion.OptionsJson),
                            stepOrder =
                                currentQuestion.StepOrder,
                            roundNumber =
                                currentQuestion.RoundNumber,
                            timerSeconds =
                                currentQuestion.TimerSeconds,
                            imageUrl =
                                currentQuestion.ImageUrl,
                            audioUrl =
                                currentQuestion.AudioUrl,
                            videoUrl =
                                currentQuestion.VideoUrl,
                            correctOption =
                                currentQuestion.CorrectOption
                        }
            });
        }

        // ============================================================
        // CHANGE TEAM
        // ============================================================

        /// <summary>
        /// POST /api/team-quiz/{gameId}/select-team
        /// </summary>
        [HttpPost("{gameId:int}/select-team")]
        public async Task<IActionResult> SelectTeam(
            int gameId,
            [FromBody] SelectTeamDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!session.IsActive)
                return BadRequest(new
                {
                    message = "Игра завершена"
                });

            if (session.CurrentStep != "lobby")
            {
                return BadRequest(new
                {
                    message = "После начала игры команду менять нельзя"
                });
            }

            var playerExists = await _context.LivePlayers
                .AnyAsync(x =>
                    x.GameId == gameId &&
                    x.UserId == userId);

            if (!playerExists)
            {
                return BadRequest(new
                {
                    message = "Сначала войдите в комнату"
                });
            }

            var teamExists = await _context.Teams
                .AnyAsync(x => x.Id == dto.TeamId);

            if (!teamExists)
                return NotFound(new
                {
                    message = "Команда не найдена"
                });

            var membership = await _context.TeamMembers
                .AnyAsync(x =>
                    x.TeamId == dto.TeamId &&
                    x.UserId == userId &&
                    x.Status == "accepted");

            if (!membership)
            {
                return BadRequest(new
                {
                    message =
                        "Вы не являетесь участником этой команды"
                });
            }

            await UpsertResponseAsync(
                gameId,
                userId,
                "team_select",
                dto.TeamId.ToString());

            await DeleteResponseAsync(
                gameId,
                userId,
                "captain_vote");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Команда изменена",
                teamId = dto.TeamId
            });
        }

        // ============================================================
        // CAPTAIN VOTE
        // ============================================================

        /// <summary>
        /// POST /api/v1/games/{gameId}/captain-votes
        /// </summary>
        [HttpPost("{gameId:int}/captain-votes")]
        public async Task<IActionResult> SubmitCaptainVote(
            int gameId,
            [FromBody] CaptainVoteDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!session.IsActive)
                return BadRequest(new
                {
                    message = "Игра завершена"
                });

            if (session.CurrentStep != "lobby")
            {
                return BadRequest(new
                {
                    message = "Голосование за капитана уже закрыто"
                });
            }

            var player = await _context.LivePlayers
                .FirstOrDefaultAsync(x =>
                    x.GameId == gameId &&
                    x.UserId == userId);

            if (player == null)
            {
                return BadRequest(new
                {
                    message = "Вы не находитесь в этой комнате"
                });
            }

            var myTeamId =
                await GetSelectedTeamIdAsync(
                    gameId,
                    userId);

            if (!myTeamId.HasValue)
            {
                return BadRequest(new
                {
                    message = "Сначала выберите команду"
                });
            }

            var candidateTeamId =
                await GetSelectedTeamIdAsync(
                    gameId,
                    dto.CaptainUserId);

            if (!candidateTeamId.HasValue ||
                candidateTeamId.Value != myTeamId.Value)
            {
                return BadRequest(new
                {
                    message =
                        "Капитан должен быть участником вашей команды"
                });
            }

            var candidateIsInGame =
                await _context.LivePlayers.AnyAsync(x =>
                    x.GameId == gameId &&
                    x.UserId == dto.CaptainUserId);

            if (!candidateIsInGame)
            {
                return BadRequest(new
                {
                    message =
                        "Этот игрок ещё не подключён к комнате"
                });
            }

            await UpsertResponseAsync(
                gameId,
                userId,
                "captain_vote",
                dto.CaptainUserId.ToString());

            await _context.SaveChangesAsync();

            var votes =
                await GetCaptainVotesAsync(
                    gameId,
                    myTeamId.Value);

            return Ok(new
            {
                message = "Голос за капитана учтён",
                teamId = myTeamId.Value,
                votes
            });
        }

        // ============================================================
        // COMMANDS
        // ============================================================

        /// <summary>
        /// Команды ведущего.
        ///
        /// POST /api/v1/games/{gameId}/commands
        /// </summary>
        [HttpPost("{gameId:int}/commands")]
        public async Task<IActionResult> AdvanceGame(
            int gameId,
            [FromBody] GameCommandDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!IsHost(session, userId))
                return Forbid();

            if (!session.IsActive &&
                !string.Equals(
                    dto.Action,
                    "finish",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Сессия уже завершена"
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.NewStep))
            {
                var validation =
                    await ValidateStepAsync(
                        session,
                        dto.NewStep);

                if (validation != null)
                    return validation;

                session.CurrentStep =
                    dto.NewStep.Trim();

                if (session.CurrentStep != "lobby")
                    await EnsureCaptainsExistAsync(gameId);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Игровой этап изменён",

                    gameId,
                    eventId = session.EventId,
                    gameType = session.GameType,
                    currentStep = session.CurrentStep,
                    isActive = session.IsActive
                });
            }

            var action =
                (dto.Action ?? "")
                    .Trim()
                    .ToLowerInvariant();

            switch (action)
            {
                case "lobby":
                    session.CurrentStep = "lobby";
                    break;

                case "start":
                {
                    await EnsureCaptainsExistAsync(gameId);

                    var firstQuestion =
                        await _context.GameQuestions
                            .Where(q =>
                                q.EventId == session.EventId)
                            .OrderBy(q => q.StepOrder)
                            .ThenBy(q => q.Id)
                            .FirstOrDefaultAsync();

                    if (firstQuestion == null)
                    {
                        return BadRequest(new
                        {
                            message = "У игры нет вопросов"
                        });
                    }

                    session.CurrentStep =
                        $"question_{firstQuestion.Id}";

                    break;
                }

                case "open_question":
                case "question":
                {
                    if (!string.IsNullOrWhiteSpace(
                        dto.QuestionId) &&
                        int.TryParse(
                            dto.QuestionId,
                            out var questionId))
                    {
                        var question =
                            await GetEventQuestionAsync(
                                session.EventId,
                                questionId);

                        if (question == null)
                        {
                            return NotFound(new
                            {
                                message =
                                    "Вопрос не найден в этой игре"
                            });
                        }

                        session.CurrentStep =
                            $"question_{question.Id}";
                    }
                    else
                    {
                        var nextQuestion =
                            await GetNextQuestionAsync(
                                session,
                                session.CurrentStep);

                        if (nextQuestion == null)
                        {
                            return BadRequest(new
                            {
                                message =
                                    "Следующего вопроса нет"
                            });
                        }

                        session.CurrentStep =
                            $"question_{nextQuestion.Id}";
                    }

                    break;
                }

                case "next_question":
                {
                    var nextQuestion =
                        await GetNextQuestionAsync(
                            session,
                            session.CurrentStep);

                    if (nextQuestion == null)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Следующего вопроса нет"
                        });
                    }

                    session.CurrentStep =
                        $"question_{nextQuestion.Id}";

                    break;
                }

                case "close_question":
                    if (!session.CurrentStep.StartsWith(
                        "question_"))
                    {
                        return BadRequest(new
                        {
                            message =
                                "Сейчас нет открытого вопроса"
                        });
                    }

                    session.CurrentStep =
                        "question_closed";
                    break;

                case "reveal":
                    session.CurrentStep = "reveal";
                    break;

                case "leaderboard":
                    session.CurrentStep = "leaderboard";
                    break;

                case "finish":
                    session.CurrentStep = "finished";
                    session.IsActive = false;
                    break;

                default:
                    return BadRequest(new
                    {
                        message =
                            "Неизвестная команда ведущего",

                        allowedActions = new[]
                        {
                            "lobby",
                            "start",
                            "open_question",
                            "close_question",
                            "reveal",
                            "leaderboard",
                            "next_question",
                            "finish"
                        }
                    });
            }

            if (session.CurrentStep != "lobby")
                await EnsureCaptainsExistAsync(gameId);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Команда выполнена",
                gameId,
                eventId = session.EventId,
                gameType = session.GameType,
                currentStep = session.CurrentStep,
                isActive = session.IsActive
            });
        }

        // ============================================================
        // ANSWER
        // ============================================================

        /// <summary>
        /// Ответ капитана команды.
        ///
        /// POST /api/v1/games/{gameId}/answers
        /// </summary>
        [HttpPost("{gameId:int}/answers")]
        public async Task<IActionResult> SubmitAnswer(
            int gameId,
            [FromBody] TeamAnswerDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!session.IsActive)
                return BadRequest(new
                {
                    message = "Игра завершена"
                });

            if (!session.CurrentStep.StartsWith(
                "question_"))
            {
                return BadRequest(new
                {
                    message = "Сейчас ответы закрыты"
                });
            }

            var captainTeam =
                await GetSelectedTeamIdAsync(
                    gameId,
                    userId);

            if (!captainTeam.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "Вы не состоите в команде этой игры"
                });
            }

            var captainInfo =
                await GetCaptainInfoAsync(
                    gameId,
                    captainTeam.Value);

            if (captainInfo == null ||
                captainInfo.CaptainUserId != userId)
            {
                return Forbid();
            }

            var questionIdText =
                session.CurrentStep.Replace(
                    "question_",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            if (!int.TryParse(
                questionIdText,
                out var questionId))
            {
                return BadRequest(new
                {
                    message =
                        "Некорректный идентификатор вопроса"
                });
            }

            var question =
                await GetEventQuestionAsync(
                    session.EventId,
                    questionId);

            if (question == null)
            {
                return NotFound(new
                {
                    message =
                        "Вопрос не найден для этой игры"
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Answer))
            {
                return BadRequest(new
                {
                    message = "Ответ не может быть пустым"
                });
            }

            var answer =
                dto.Answer.Trim();

            var isCorrect =
                CheckAnswer(
                    question,
                    answer);

            var response =
                await UpsertResponseAsync(
                    gameId,
                    userId,
                    session.CurrentStep,
                    answer);

            response.IsCorrect = isCorrect;

            await _context.SaveChangesAsync();

            var answeredTeams =
                await GetAnsweredTeamsCountAsync(
                    gameId,
                    session.CurrentStep);

            return Ok(new
            {
                message =
                    "Ответ команды зафиксирован",

                gameId,
                questionId,
                teamId = captainTeam.Value,
                answer,
                isCorrect,
                answeredTeams
            });
        }

        // ============================================================
        // QUESTION RESULTS
        // ============================================================

        /// <summary>
        /// GET /api/v1/games/{gameId}/results/{stepIdentifier}
        /// </summary>
        [HttpGet("{gameId:int}/results/{stepIdentifier}")]
        public async Task<IActionResult> GetQuestionResults(
            int gameId,
            string stepIdentifier)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (session.OrganiserUserId != userId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(stepIdentifier))
                return BadRequest(new
                {
                    message = "Не указан stepIdentifier"
                });

            var responses =
                await _context.LiveStepResponses
                    .Where(x =>
                        x.GameId == gameId &&
                        x.StepIdentifier == stepIdentifier)
                    .ToListAsync();

            var result =
                new List<QuestionResultInfo>();

            foreach (var response in responses)
            {
                var teamId =
                    await GetSelectedTeamIdAsync(
                        gameId,
                        response.UserId);

                if (!teamId.HasValue)
                    continue;

                var team =
                    await _context.Teams
                        .FirstOrDefaultAsync(x =>
                            x.Id == teamId.Value);

                if (team == null)
                    continue;

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(x =>
                            x.UserId == response.UserId);

                result.Add(new QuestionResultInfo
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    UserId = response.UserId,
                    Username = user?.Username,
                    Answer = response.AnswerData,
                    IsCorrect = response.IsCorrect,
                    CreatedAt = response.CreatedAt
                });
            }

            return Ok(new
            {
                gameId,
                eventId = session.EventId,
                gameType = session.GameType,
                stepIdentifier,
                count = result.Count,
                responses = result.Select(x => new
                {
                    teamId = x.TeamId,
                    teamName = x.TeamName,
                    userId = x.UserId,
                    username = x.Username,
                    answer = x.Answer,
                    isCorrect = x.IsCorrect,
                    createdAt = x.CreatedAt
                })
            });
        }

        // ============================================================
        // FINAL RESULTS
        // ============================================================

        /// <summary>
        /// GET /api/v1/games/{gameId}/final-results
        /// </summary>
        [HttpGet("{gameId:int}/final-results")]
        public async Task<IActionResult> GetFinalResults(
            int gameId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (session.OrganiserUserId != userId)
                return Forbid();

            var gamePlayers =
                await GetGamePlayersAsync(gameId);

            var groups =
                gamePlayers
                    .Where(x => x.TeamId.HasValue)
                    .GroupBy(x => x.TeamId!.Value)
                    .ToList();

            var rows =
                new List<TeamFinalResult>();

            foreach (var group in groups)
            {
                var teamId = group.Key;

                var team =
                    await _context.Teams
                        .FirstOrDefaultAsync(x =>
                            x.Id == teamId);

                if (team == null)
                    continue;

                var captain =
                    await GetCaptainInfoAsync(
                        gameId,
                        teamId);

                var score =
                    await CalculateTeamScoreAsync(
                        gameId,
                        teamId);

                var answeredQuestions = 0;

                if (captain != null)
                {
                    answeredQuestions =
                        await _context.LiveStepResponses
                            .CountAsync(x =>
                                x.GameId == gameId &&
                                x.UserId ==
                                    captain.CaptainUserId &&
                                x.StepIdentifier
                                    .StartsWith("question_"));
                }

                rows.Add(new TeamFinalResult
                {
                    TeamId = team.Id,
                    TeamName = team.Name,
                    CaptainUserId =
                        captain?.CaptainUserId,
                    CaptainUsername =
                        captain?.CaptainUsername,
                    Players = group.Count(),
                    AnsweredQuestions =
                        answeredQuestions,
                    Score = score
                });
            }

            var final =
                rows
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(
                        x => x.AnsweredQuestions)
                    .ThenBy(x => x.TeamName)
                    .Select((x, index) => new
                    {
                        rank = index + 1,
                        teamId = x.TeamId,
                        teamName = x.TeamName,
                        captainUserId =
                            x.CaptainUserId,
                        captainUsername =
                            x.CaptainUsername,
                        players = x.Players,
                        answeredQuestions =
                            x.AnsweredQuestions,
                        score = x.Score
                    })
                    .ToList();

            return Ok(new
            {
                gameId,
                eventId = session.EventId,
                gameType = session.GameType,
                isActive = session.IsActive,
                results = final
            });
        }

        // ============================================================
        // FEEDBACK
        // ============================================================

        /// <summary>
        /// POST /api/v1/games/{gameId}/feedback
        /// </summary>
        [HttpPost("{gameId:int}/feedback")]
        public async Task<IActionResult> SubmitFeedback(
            int gameId,
            [FromBody] TeamFeedbackDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            var playerExists =
                await _context.LivePlayers.AnyAsync(x =>
                    x.GameId == gameId &&
                    x.UserId == userId);

            if (!playerExists)
            {
                return BadRequest(new
                {
                    message =
                        "Вы не участник этой игры"
                });
            }

            if (dto.Rating < 1 ||
                dto.Rating > 5)
            {
                return BadRequest(new
                {
                    message =
                        "Оценка должна быть от 1 до 5"
                });
            }

            var step =
                !string.IsNullOrWhiteSpace(
                    dto.StepIdentifier)
                    ? $"feedback_{dto.StepIdentifier}"
                    : "feedback_overall";

            var feedbackJson =
                JsonSerializer.Serialize(new
                {
                    rating = dto.Rating,
                    text = dto.Text?.Trim() ?? ""
                });

            await UpsertResponseAsync(
                gameId,
                userId,
                step,
                feedbackJson);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Оценка сохранена"
            });
        }

        // ============================================================
        // HOST STATE
        // ============================================================

        /// <summary>
        /// GET /api/team-quiz/{gameId}/host-state
        /// </summary>
        [HttpGet("{gameId:int}/host-state")]
        public async Task<IActionResult> GetHostState(
            int gameId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });

            var session = await GetSessionAsync(gameId);

            if (session == null)
                return NotFound(new
                {
                    message = "Сессия не найдена"
                });

            if (session.GameType != 2)
                return WrongGameType(session);

            if (!IsHost(session, userId))
                return Forbid();

            var roomCode =
                await _context.GameRoomCodes
                    .Where(x =>
                        x.GameId == gameId)
                    .OrderByDescending(
                        x => x.CreatedAt)
                    .Select(
                        x => x.RoomCode)
                    .FirstOrDefaultAsync();

            var questions =
                await _context.GameQuestions
                    .Where(x =>
                        x.EventId ==
                        session.EventId)
                    .OrderBy(x => x.StepOrder)
                    .ThenBy(x => x.Id)
                    .Select(x => new
                    {
                        id = x.Id,
                        eventId = x.EventId,
                        questionText =
                            x.QuestionText,
                        options =
                            ParseOptionsSafe(
                                x.OptionsJson),
                        stepOrder =
                            x.StepOrder,
                        roundNumber =
                            x.RoundNumber,
                        timerSeconds =
                            x.TimerSeconds,
                        imageUrl =
                            x.ImageUrl,
                        audioUrl =
                            x.AudioUrl,
                        videoUrl =
                            x.VideoUrl,
                        correctOption =
                            x.CorrectOption
                    })
                    .ToListAsync();

            var teams =
                await GetGameTeamsAsync(gameId);

            var players =
                await GetGamePlayersDetailedAsync(gameId);

            var currentQuestion =
                await GetCurrentQuestionAsync(session);

            var captains =
                new List<object>();

            foreach (var team in teams)
            {
                if (team.Id <= 0)
                    continue;

                var captain =
                    await GetCaptainInfoAsync(
                        gameId,
                        team.Id);

                if (captain != null)
                {
                    captains.Add(new
                    {
                        teamId = team.Id,
                        teamName = team.Name,
                        captainUserId =
                            captain.CaptainUserId,
                        captainUsername =
                            captain.CaptainUsername
                    });
                }
            }

            return Ok(new
            {
                gameId,
                eventId = session.EventId,
                gameType = session.GameType,
                roomCode,

                currentStep =
                    session.CurrentStep,

                phase =
                    ResolvePhase(
                        session.CurrentStep),

                isActive =
                    session.IsActive,

                teams = teams.Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    connected = x.Connected,
                    capacity = x.Capacity,
                    captainId = x.CaptainId,
                    score = x.Score
                }),

                players,

                captains,

                question =
                    currentQuestion == null
                        ? null
                        : BuildQuestionMedia(currentQuestion),

                questions
            });
        }

        // ============================================================
        // INTERNAL HELPERS
        // ============================================================

        private async Task<string>
            GenerateUniqueRoomCodeAsync()
        {
            while (true)
            {
                var code =
                    Random.Shared
                        .Next(1000, 10000)
                        .ToString();

                var exists =
                    await _context.GameRoomCodes
                        .AnyAsync(x =>
                            x.RoomCode == code);

                if (!exists)
                    return code;
            }
        }

        private async Task<LiveStepResponse>
            UpsertResponseAsync(
                int gameId,
                int userId,
                string stepIdentifier,
                string answer)
        {
            var existing =
                await _context.LiveStepResponses
                    .FirstOrDefaultAsync(x =>
                        x.GameId == gameId &&
                        x.UserId == userId &&
                        x.StepIdentifier ==
                            stepIdentifier);

            if (existing != null)
            {
                existing.AnswerData = answer;
                existing.CreatedAt = DateTime.Now;

                return existing;
            }

            var response =
                new LiveStepResponse
                {
                    GameId = gameId,
                    UserId = userId,
                    StepIdentifier =
                        stepIdentifier,
                    AnswerData = answer,
                    CreatedAt = DateTime.Now
                };

            _context.LiveStepResponses.Add(
                response);

            return response;
        }

        private async Task DeleteResponseAsync(
            int gameId,
            int userId,
            string stepIdentifier)
        {
            var existing =
                await _context.LiveStepResponses
                    .FirstOrDefaultAsync(x =>
                        x.GameId == gameId &&
                        x.UserId == userId &&
                        x.StepIdentifier ==
                            stepIdentifier);

            if (existing != null)
                _context.LiveStepResponses.Remove(existing);
        }

        private async Task<int?>
            GetSelectedTeamIdAsync(
                int gameId,
                int userId)
        {
            var response =
                await _context.LiveStepResponses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.GameId == gameId &&
                        x.UserId == userId &&
                        x.StepIdentifier ==
                            "team_select");

            if (response == null)
                return null;

            return int.TryParse(
                response.AnswerData,
                out var teamId)
                ? teamId
                : null;
        }

        private async Task<List<GamePlayerInfo>>
            GetGamePlayersAsync(int gameId)
        {
            var players =
                await _context.LivePlayers
                    .AsNoTracking()
                    .Where(x =>
                        x.GameId == gameId)
                    .OrderBy(x => x.JoinedAt)
                    .ToListAsync();

            var result =
                new List<GamePlayerInfo>();

            foreach (var player in players)
            {
                result.Add(
                    new GamePlayerInfo
                    {
                        UserId = player.UserId,
                        Username = player.Username,
                        TeamId =
                            await GetSelectedTeamIdAsync(
                                gameId,
                                player.UserId)
                    });
            }

            return result;
        }

        private async Task<List<object>>
            GetGamePlayersDetailedAsync(
                int gameId)
        {
            var players =
                await GetGamePlayersAsync(
                    gameId);

            var result =
                new List<object>();

            foreach (var player in players)
            {
                string? teamName = null;

                if (player.TeamId.HasValue)
                {
                    teamName =
                        await _context.Teams
                            .Where(x =>
                                x.Id ==
                                player.TeamId.Value)
                            .Select(x =>
                                x.Name)
                            .FirstOrDefaultAsync();
                }

                var captain =
                    player.TeamId.HasValue
                        ? await GetCaptainInfoAsync(
                            gameId,
                            player.TeamId.Value)
                        : null;

                result.Add(new
                {
                    id = player.UserId,
                    username = player.Username,
                    teamId = player.TeamId,
                    teamName,
                    isCaptain =
                        captain?.CaptainUserId ==
                        player.UserId
                });
            }

            return result;
        }

        private async Task<List<GameTeamInfo>>
            GetGameTeamsAsync(int gameId)
        {
            var players =
                await GetGamePlayersAsync(
                    gameId);

            var teamIds =
                players
                    .Where(x =>
                        x.TeamId.HasValue)
                    .Select(x =>
                        x.TeamId!.Value)
                    .Distinct()
                    .ToList();

            if (teamIds.Count == 0)
                return new List<GameTeamInfo>();

            var teams =
                await _context.Teams
                    .Where(x =>
                        teamIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .ToListAsync();

            var result =
                new List<GameTeamInfo>();

            foreach (var team in teams)
            {
                var members =
                    players
                        .Where(x =>
                            x.TeamId ==
                            team.Id)
                        .ToList();

                var captain =
                    await GetCaptainInfoAsync(
                        gameId,
                        team.Id);

                var capacity =
                    await _context.TeamMembers
                        .CountAsync(x =>
                            x.TeamId == team.Id &&
                            x.Status == "accepted");

                var score =
                    await CalculateTeamScoreAsync(
                        gameId,
                        team.Id);

                result.Add(
                    new GameTeamInfo
                    {
                        Id = team.Id,
                        Name = team.Name,
                        Connected =
                            members.Count,
                        Capacity =
                            capacity,
                        CaptainId =
                            captain?.CaptainUserId,
                        Score = score
                    });
            }

            return result;
        }

        private async Task<CaptainInfo?>
            GetCaptainInfoAsync(
                int gameId,
                int? teamId)
        {
            if (!teamId.HasValue)
                return null;

            var players =
                await GetGamePlayersAsync(
                    gameId);

            var teamPlayers =
                players
                    .Where(x =>
                        x.TeamId ==
                        teamId.Value)
                    .ToList();

            if (teamPlayers.Count == 0)
                return null;

            var playerIds =
                teamPlayers
                    .Select(x => x.UserId)
                    .ToHashSet();

            var votes =
                await _context.LiveStepResponses
                    .AsNoTracking()
                    .Where(x =>
                        x.GameId == gameId &&
                        x.StepIdentifier ==
                            "captain_vote" &&
                        playerIds.Contains(
                            x.UserId))
                    .ToListAsync();

            var voteGroups =
                votes
                    .Select(x => new
                    {
                        CandidateId =
                            int.TryParse(
                                x.AnswerData,
                                out var candidateId)
                                ? candidateId
                                : 0
                    })
                    .Where(x =>
                        playerIds.Contains(
                            x.CandidateId))
                    .GroupBy(x =>
                        x.CandidateId)
                    .Select(g => new
                    {
                        CandidateId = g.Key,
                        Votes = g.Count()
                    })
                    .OrderByDescending(x =>
                        x.Votes)
                    .ThenBy(x =>
                        x.CandidateId)
                    .ToList();

            int captainUserId;

            if (votes.Count == 0)
            {
                captainUserId =
                    teamPlayers
                        .OrderBy(x =>
                            x.UserId)
                        .First()
                        .UserId;
            }
            else
            {
                captainUserId =
                    voteGroups
                        .FirstOrDefault()
                        ?.CandidateId
                    ?? teamPlayers
                        .OrderBy(x =>
                            x.UserId)
                        .First()
                        .UserId;
            }

            var captain =
                teamPlayers
                    .FirstOrDefault(
                        x =>
                            x.UserId ==
                            captainUserId);

            if (captain == null)
                return null;

            return new CaptainInfo
            {
                TeamId =
                    teamId.Value,

                CaptainUserId =
                    captain.UserId,

                CaptainUsername =
                    captain.Username
            };
        }

        private async Task<List<CaptainVoteInfo>>
            GetCaptainVotesAsync(
                int gameId,
                int teamId)
        {
            var players =
                await GetGamePlayersAsync(
                    gameId);

            var teamPlayers =
                players
                    .Where(x =>
                        x.TeamId ==
                        teamId)
                    .ToList();

            var playerIds =
                teamPlayers
                    .Select(x => x.UserId)
                    .ToHashSet();

            var votes =
                await _context.LiveStepResponses
                    .AsNoTracking()
                    .Where(x =>
                        x.GameId == gameId &&
                        x.StepIdentifier ==
                            "captain_vote" &&
                        playerIds.Contains(
                            x.UserId))
                    .ToListAsync();

            var result =
                new List<CaptainVoteInfo>();

            foreach (var player in teamPlayers)
            {
                var count =
                    votes.Count(x =>
                        int.TryParse(
                            x.AnswerData,
                            out var candidateId) &&
                        candidateId ==
                            player.UserId);

                result.Add(
                    new CaptainVoteInfo
                    {
                        UserId =
                            player.UserId,

                        Username =
                            player.Username,

                        Votes =
                            count
                    });
            }

            return result
                .OrderByDescending(x =>
                    x.Votes)
                .ThenBy(x =>
                    x.UserId)
                .ToList();
        }

        private async Task EnsureCaptainsExistAsync(
            int gameId)
        {
            var players =
                await GetGamePlayersAsync(
                    gameId);

            var teamIds =
                players
                    .Where(x =>
                        x.TeamId.HasValue)
                    .Select(x =>
                        x.TeamId!.Value)
                    .Distinct()
                    .ToList();

            foreach (var teamId in teamIds)
            {
                _ =
                    await GetCaptainInfoAsync(
                        gameId,
                        teamId);
            }
        }

        private async Task<GameQuestion?>
            GetEventQuestionAsync(
                int eventId,
                int questionId)
        {
            return await _context.GameQuestions
                .FirstOrDefaultAsync(q =>
                    q.Id == questionId &&
                    q.EventId == eventId);
        }

        private async Task<GameQuestion?>
            GetNextQuestionAsync(
                GameSession session,
                string currentStep)
        {
            var questions =
                await _context.GameQuestions
                    .Where(q =>
                        q.EventId ==
                        session.EventId)
                    .OrderBy(q =>
                        q.StepOrder)
                    .ThenBy(q =>
                        q.Id)
                    .ToListAsync();

            if (questions.Count == 0)
                return null;

            if (!currentStep.StartsWith(
                "question_"))
            {
                return questions.First();
            }

            var currentIdText =
                currentStep.Replace(
                    "question_",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            if (!int.TryParse(
                currentIdText,
                out var currentId))
            {
                return questions.First();
            }

            var index =
                questions.FindIndex(
                    x =>
                        x.Id ==
                        currentId);

            if (index < 0 ||
                index + 1 >=
                    questions.Count)
            {
                return null;
            }

            return questions[
                index + 1];
        }

        private async Task<IActionResult?>
            ValidateStepAsync(
                GameSession session,
                string step)
        {
            if (string.IsNullOrWhiteSpace(step))
            {
                return BadRequest(new
                {
                    message = "Пустой игровой этап"
                });
            }

            if (step.StartsWith(
                "question_"))
            {
                var idText =
                    step.Replace(
                        "question_",
                        "",
                        StringComparison.OrdinalIgnoreCase);

                if (!int.TryParse(
                    idText,
                    out var questionId))
                {
                    return BadRequest(new
                    {
                        message =
                            "Некорректный question id"
                    });
                }

                var question =
                    await GetEventQuestionAsync(
                        session.EventId,
                        questionId);

                if (question == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Этот вопрос не относится к текущей игре",

                        eventId =
                            session.EventId,

                        questionId
                    });
                }
            }

            return null;
        }

        private async Task<GameQuestion?>
            GetCurrentQuestionAsync(
                GameSession session)
        {
            if (!session.CurrentStep.StartsWith(
                "question_"))
            {
                return null;
            }

            var idText =
                session.CurrentStep.Replace(
                    "question_",
                    "",
                    StringComparison.OrdinalIgnoreCase);

            if (!int.TryParse(
                idText,
                out var questionId))
            {
                return null;
            }

            return await GetEventQuestionAsync(
                session.EventId,
                questionId);
        }

        private async Task<int>
            GetAnsweredTeamsCountAsync(
                int gameId,
                string stepIdentifier)
        {
            if (string.IsNullOrWhiteSpace(
                stepIdentifier))
            {
                return 0;
            }

            var responses =
                await _context.LiveStepResponses
                    .AsNoTracking()
                    .Where(x =>
                        x.GameId == gameId &&
                        x.StepIdentifier ==
                            stepIdentifier)
                    .ToListAsync();

            var teamIds =
                new HashSet<int>();

            foreach (var response in responses)
            {
                var teamId =
                    await GetSelectedTeamIdAsync(
                        gameId,
                        response.UserId);

                if (teamId.HasValue)
                    teamIds.Add(
                        teamId.Value);
            }

            return teamIds.Count;
        }

        // ============================================================
        // SCORE
        // ============================================================

        private async Task<int>
            CalculateTeamScoreAsync(
                int gameId,
                int teamId)
        {
            var captain =
                await GetCaptainInfoAsync(
                    gameId,
                    teamId);

            if (captain == null)
                return 0;

            return await _context.LiveStepResponses
                .Where(x =>
                    x.GameId == gameId &&
                    x.UserId ==
                        captain.CaptainUserId &&
                    x.StepIdentifier
                        .StartsWith("question_") &&
                    x.IsCorrect == true)
                .CountAsync();
        }

        private static bool CheckAnswer(
            GameQuestion question,
            string answer)
        {
            if (question.CorrectOption == null)
                return false;

            var options =
                ParseOptionsToStrings(
                    question.OptionsJson);

            var normalizedAnswer =
                NormalizeAnswer(answer);

            // --------------------------------------------------------
            // Вариант 1:
            // frontend отправляет индекс ответа:
            //
            // "0", "1", "2", "3"
            // --------------------------------------------------------

            if (int.TryParse(
                answer,
                out var selectedIndex))
            {
                return selectedIndex ==
                       question.CorrectOption.Value;
            }

            // --------------------------------------------------------
            // Вариант 2:
            // frontend отправляет текст варианта.
            // --------------------------------------------------------

            if (selectedIndexFromText(
                options,
                normalizedAnswer,
                out var textIndex))
            {
                return textIndex ==
                       question.CorrectOption.Value;
            }

            return false;
        }

        private static bool selectedIndexFromText(
            List<string> options,
            string normalizedAnswer,
            out int index)
        {
            index = -1;

            for (var i = 0;
                 i < options.Count;
                 i++)
            {
                if (NormalizeAnswer(
                        options[i]) ==
                    normalizedAnswer)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeAnswer(
            string? value)
        {
            return (value ?? "")
                .Trim()
                .ToLowerInvariant();
        }

        private static List<string>
            ParseOptionsToStrings(
                string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(
                optionsJson))
            {
                return new List<string>();
            }

            try
            {
                using var document =
                    JsonDocument.Parse(
                        optionsJson);

                if (document.RootElement.ValueKind ==
                    JsonValueKind.Array)
                {
                    var result =
                        new List<string>();

                    foreach (
                        var item
                        in document.RootElement.EnumerateArray())
                    {
                        result.Add(
                            item.ValueKind ==
                                JsonValueKind.String
                                ? item.GetString() ?? ""
                                : item.ToString());
                    }

                    return result;
                }
            }
            catch
            {
                // ниже fallback
            }

            return optionsJson
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(x =>
                    x.Trim(
                        '"',
                        '\'',
                        ' '))
                .ToList();
        }

        // ============================================================
        // QUESTION MEDIA
        // ============================================================

        private static object BuildQuestionMedia(
            GameQuestion question)
        {
            return new
            {
                id = question.Id,
                eventId = question.EventId,
                questionText = question.QuestionText,
                options = ParseOptionsSafe(question.OptionsJson),
                stepOrder = question.StepOrder,
                roundNumber = question.RoundNumber,
                timerSeconds = question.TimerSeconds,
                imageUrl = question.ImageUrl,
                audioUrl = question.AudioUrl,
                videoUrl = question.VideoUrl,
                correctOption = question.CorrectOption
            };
        }

        // ============================================================
        // PHASE
        // ============================================================

        private string ResolvePhase(
            string currentStep)
        {
            if (string.Equals(
                currentStep,
                "lobby",
                StringComparison.OrdinalIgnoreCase))
            {
                return "lobby";
            }

            if (string.Equals(
                currentStep,
                "reveal",
                StringComparison.OrdinalIgnoreCase))
            {
                return "revealQueue";
            }

            if (string.Equals(
                currentStep,
                "leaderboard",
                StringComparison.OrdinalIgnoreCase))
            {
                return "leaderboard";
            }

            if (string.Equals(
                currentStep,
                "finished",
                StringComparison.OrdinalIgnoreCase))
            {
                return "final";
            }

            if (string.Equals(
                currentStep,
                "question_closed",
                StringComparison.OrdinalIgnoreCase))
            {
                return "questionClosed";
            }

            if (currentStep.StartsWith(
                "question_"))
            {
                return "questionOpen";
            }

            return currentStep;
        }

        // ============================================================
        // OPTIONS JSON
        // ============================================================

        private static object
            ParseOptionsSafe(
                string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(
                optionsJson))
            {
                return Array.Empty<string>();
            }

            try
            {
                using var document =
                    JsonDocument.Parse(
                        optionsJson);

                return document.RootElement
                    .Clone();
            }
            catch
            {
                return optionsJson
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);
            }
        }
    }


    // ================================================================
    // DTO
    // ================================================================

    public class JoinTeamRoomDto
    {
        public string GameCode { get; set; }
            = string.Empty;

        public int TeamId { get; set; }
    }

    public class SelectTeamDto
    {
        public int TeamId { get; set; }
    }

    public class CaptainVoteDto
    {
        public int CaptainUserId { get; set; }
    }

    public class TeamAnswerDto
    {
        public string Answer { get; set; }
            = string.Empty;
    }

    public class GameCommandDto
    {
        public string? Action { get; set; }

        public string? NewStep { get; set; }

        public string? QuestionId { get; set; }
    }

    public class TeamFeedbackDto
    {
        public int Rating { get; set; }

        public string? Text { get; set; }

        public string? StepIdentifier { get; set; }
    }


    // ================================================================
    // INTERNAL TYPES
    // ================================================================

    internal class GamePlayerInfo
    {
        public int UserId { get; set; }

        public string Username { get; set; }
            = string.Empty;

        public int? TeamId { get; set; }
    }

    internal class GameTeamInfo
    {
        public int Id { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public int Connected { get; set; }

        public int Capacity { get; set; }

        public int? CaptainId { get; set; }

        public int Score { get; set; }
    }

    internal class CaptainInfo
    {
        public int TeamId { get; set; }

        public int CaptainUserId { get; set; }

        public string CaptainUsername { get; set; }
            = string.Empty;
    }

    internal class CaptainVoteInfo
    {
        public int UserId { get; set; }

        public string Username { get; set; }
            = string.Empty;

        public int Votes { get; set; }
    }

    internal class QuestionResultInfo
    {
        public int TeamId { get; set; }

        public string TeamName { get; set; }
            = string.Empty;

        public int UserId { get; set; }

        public string? Username { get; set; }

        public string Answer { get; set; }
            = string.Empty;

        public bool? IsCorrect { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    internal class TeamFinalResult
    {
        public int TeamId { get; set; }

        public string TeamName { get; set; }
            = string.Empty;

        public int? CaptainUserId { get; set; }

        public string? CaptainUsername { get; set; }

        public int Players { get; set; }

        public int AnsweredQuestions { get; set; }

        public int Score { get; set; }
    }
}