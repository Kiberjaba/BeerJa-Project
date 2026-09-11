using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using BeejaServer.Data;
using BeejaServer.Models;

namespace BeejaServer.Controllers
{
    [Route("api/niche-game")]
    [ApiController]
    public class NicheGameController : ControllerBase
    {
        private readonly AppDbContext _context;

        private const int NicheMaxAchievementId = 4;
        private const int NicheMinAchievementId = 5;

        public NicheGameController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =========================================================

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claim, out int userId))
                return null;

            return userId;
        }

        private static string[] ParseOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
                return Array.Empty<string>();

            try
            {
                return JsonSerializer.Deserialize<string[]>(optionsJson) ?? Array.Empty<string>();
            }
            catch
            {
                return optionsJson
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }
        }

        private static bool TryParseQuestionStep(string step, out int questionId)
        {
            questionId = 0;

            if (string.IsNullOrWhiteSpace(step))
                return false;

            string idText;

            if (step.StartsWith("question_"))
            {
                idText = step["question_".Length..];
            }
            else if (step.StartsWith("reveal_"))
            {
                idText = step["reveal_".Length..];
            }
            else
            {
                return false;
            }

            return int.TryParse(idText, out questionId);
        }

        // =========================================================
        // РАСЧЁТ ИТОГОВЫХ БАЛЛОВ ИГРЫ
        // =========================================================

        private async Task<Dictionary<int, int>> CalculateGameScores(int gameId, int eventId)
        {
            var players = await _context.LivePlayers
                .Where(p => p.GameId == gameId)
                .ToListAsync();

            var scores = new Dictionary<int, int>();

            foreach (var player in players)
            {
                if (!scores.ContainsKey(player.UserId))
                {
                    scores[player.UserId] = 0;
                }
            }

            var questions = await _context.GameQuestions
                .Where(q => q.EventId == eventId)
                .OrderBy(q => q.Id)
                .ToListAsync();

            var responses = await _context.LiveStepResponses
                .Where(r => r.GameId == gameId)
                .ToListAsync();

            foreach (var question in questions)
            {
                var step = $"question_{question.Id}";

                var questionResponses = responses
                    .Where(r => r.StepIdentifier == step)
                    .ToList();

                if (!questionResponses.Any())
                    continue;

                var options = ParseOptions(question.OptionsJson);

                if (!options.Any())
                    continue;

                var counts = options
                    .Select(option => new
                    {
                        Option = option,
                        Count = questionResponses.Count(r => string.Equals(r.AnswerData, option, StringComparison.Ordinal))
                    })
                    .ToList();

                if (!counts.Any())
                    continue;

                var minCount = counts.Min(x => x.Count);

                var nicheOptions = counts
                    .Where(x => x.Count == minCount)
                    .Select(x => x.Option)
                    .ToHashSet();

                foreach (var response in questionResponses)
                {
                    if (!nicheOptions.Contains(response.AnswerData))
                    {
                        continue;
                    }

                    if (!scores.ContainsKey(response.UserId))
                    {
                        scores[response.UserId] = 0;
                    }

                    scores[response.UserId]++;
                }
            }

            return scores;
        }

        // =========================================================
        // ВЫДАЧА АЧИВКИ
        // =========================================================

        private async Task AwardAchievementAsync(User user, int achievementId)
        {
            var alreadyEarned = await _context.UserAchievements
                .AnyAsync(ua => ua.UserId == user.UserId && ua.AchievementId == achievementId);

            if (alreadyEarned)
                return;

            var achievement = await _context.Achievements
                .FirstOrDefaultAsync(a => a.AchievementId == achievementId);

            if (achievement == null)
                return;

            _context.UserAchievements.Add(new UserAchievement
            {
                UserId = user.UserId,
                AchievementId = achievement.AchievementId,
                EarnedAt = DateTime.Now
            });

            user.TotalPoints += achievement.PointsReward;
        }

        // =========================================================
        // ФИНАЛИЗАЦИЯ ИГРЫ
        // =========================================================

        private async Task FinalizeGameAsync(GameSession session)
        {
            if (!session.IsActive)
                return;

            var scores = await CalculateGameScores(session.Id, session.EventId);

            var players = await _context.LivePlayers
                .Where(p => p.GameId == session.Id)
                .ToListAsync();

            if (!players.Any())
            {
                session.IsActive = false;
                session.CurrentStep = "finished";
                await _context.SaveChangesAsync();
                return;
            }

            foreach (var player in players)
            {
                if (!scores.ContainsKey(player.UserId))
                {
                    scores[player.UserId] = 0;
                }
            }

            var maxScore = scores.Values.Max();
            var minScore = scores.Values.Min();

            var maxPlayers = players
                .Where(player => scores.TryGetValue(player.UserId, out var score) && score == maxScore)
                .ToList();

            var minPlayers = players
                .Where(player => scores.TryGetValue(player.UserId, out var score) && score == minScore)
                .ToList();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var player in players)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == player.UserId);

                    if (user == null)
                        continue;

                    user.TotalPoints += 50;
                    user.GamesPlayed += 1;

                    var alreadyCheckedIn = await _context.EventCheckins
                        .AnyAsync(c => c.UserId == player.UserId && c.EventId == session.EventId);

                    if (!alreadyCheckedIn)
                    {
                        _context.EventCheckins.Add(new EventCheckin
                        {
                            UserId = player.UserId,
                            EventId = session.EventId,
                            CheckedInAt = DateTime.Now,
                            PointsAwarded = 50
                        });
                    }

                    if (maxPlayers.Any(p => p.UserId == player.UserId))
                    {
                        await AwardAchievementAsync(user, NicheMaxAchievementId);
                    }

                    if (minPlayers.Any(p => p.UserId == player.UserId))
                    {
                        await AwardAchievementAsync(user, NicheMinAchievementId);
                    }
                }

                session.IsActive = false;
                session.CurrentStep = "finished";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // =========================================================
        // ТЕКУЩИЙ ПОЛЬЗОВАТЕЛЬ
        // GET /api/niche-game/me
        // =========================================================

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            return Ok(new
            {
                userId = user.UserId,
                username = user.Username,
                photo = user.Photo,
                totalPoints = user.TotalPoints,
                gamesPlayed = user.GamesPlayed
            });
        }

        // =========================================================
        // ЛОББИ
        // GET /api/niche-game/lobby/{gameId}
        // =========================================================

        [HttpGet("lobby/{gameId}")]
        [Authorize]
        public async Task<IActionResult> GetLobby(int gameId)
        {
            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            var playersCount = await _context.LivePlayers
                .CountAsync(x => x.GameId == gameId);

            var roomCode = await _context.GameRoomCodes
                .Where(x => x.GameId == gameId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.RoomCode)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                gameId,
                playersCount,
                currentStep = session.CurrentStep,
                isActive = session.IsActive,
                roomCode
            });
        }

        // =========================================================
        // СОСТОЯНИЕ ДЛЯ ИГРОКА
        // GET /api/niche-game/room-status/{gameId}
        // =========================================================

        [HttpGet("room-status/{gameId}")]
        [Authorize]
        public async Task<IActionResult> GetRoomStatus(int gameId)
        {
            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            var step = session.CurrentStep ?? "lobby";

            string questionText = string.Empty;
            string[] options = Array.Empty<string>();

            if (step.StartsWith("question_") || step.StartsWith("reveal_"))
            {
                if (TryParseQuestionStep(step, out int questionId))
                {
                    var question = await _context.GameQuestions
                        .FirstOrDefaultAsync(q => q.Id == questionId && q.EventId == session.EventId);

                    if (question != null)
                    {
                        questionText = question.QuestionText;
                        options = ParseOptions(question.OptionsJson);
                    }
                }
            }

            var roomCode = await _context.GameRoomCodes
                .Where(x => x.GameId == gameId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.RoomCode)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                gameId,
                eventId = session.EventId,
                currentStep = step,
                questionText,
                options,
                roomCode,
                isActive = session.IsActive
            });
        }

        // =========================================================
        // СОСТОЯНИЕ ДЛЯ ВЕДУЩЕГО
        // GET /api/niche-game/state/{gameId}
        // =========================================================

        [HttpGet("state/{gameId}")]
        [Authorize]
        public async Task<IActionResult> GetState(int gameId)
        {
            var hostUserId = GetCurrentUserId();

            if (hostUserId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            if (session.OrganiserUserId != hostUserId.Value)
            {
                return Unauthorized(new { message = "Вы не ведущий этой игры" });
            }

            var step = session.CurrentStep ?? "lobby";

            string questionText = string.Empty;
            string[] options = Array.Empty<string>();

            if (step.StartsWith("question_") || step.StartsWith("reveal_"))
            {
                if (TryParseQuestionStep(step, out int questionId))
                {
                    var question = await _context.GameQuestions
                        .FirstOrDefaultAsync(q => q.Id == questionId && q.EventId == session.EventId);

                    if (question != null)
                    {
                        questionText = question.QuestionText;
                        options = ParseOptions(question.OptionsJson);
                    }
                }
            }

            var roomCode = await _context.GameRoomCodes
                .Where(x => x.GameId == gameId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.RoomCode)
                .FirstOrDefaultAsync();

            var playersCount = await _context.LivePlayers
                .CountAsync(x => x.GameId == gameId);

            return Ok(new
            {
                gameId = session.Id,
                eventId = session.EventId,
                currentStep = step,
                questionText,
                options,
                roomCode,
                playersCount,
                isActive = session.IsActive
            });
        }

        // =========================================================
        // ПЕРЕКЛЮЧЕНИЕ ЭТАПА
        // POST /api/niche-game/advance-step
        // =========================================================

        [HttpPost("advance-step")]
        [Authorize]
        public async Task<IActionResult> AdvanceStep([FromBody] NicheAdvanceStepDto dto)
        {
            var hostUserId = GetCurrentUserId();

            if (hostUserId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            if (string.IsNullOrWhiteSpace(dto.NewStep))
            {
                return BadRequest(new { message = "Не указан новый шаг игры" });
            }

            var session = await _context.GameSessions.FindAsync(dto.GameId);

            if (session == null)
            {
                return NotFound(new { message = "Игровая сессия не найдена" });
            }

            if (session.OrganiserUserId != hostUserId.Value)
            {
                return Unauthorized(new { message = "Вы не ведущий этой игры" });
            }

            var newStep = dto.NewStep.Trim();

            if (newStep != "lobby" && newStep != "finished" &&
                !newStep.StartsWith("question_") && !newStep.StartsWith("reveal_"))
            {
                return BadRequest(new { message = "Недопустимый шаг игры" });
            }

            if (newStep.StartsWith("question_") || newStep.StartsWith("reveal_"))
            {
                if (!TryParseQuestionStep(newStep, out int questionId))
                {
                    return BadRequest(new { message = "Некорректный ID вопроса" });
                }

                var question = await _context.GameQuestions
                    .FirstOrDefaultAsync(q => q.Id == questionId && q.EventId == session.EventId);

                if (question == null)
                {
                    return BadRequest(new { message = "Этот вопрос не принадлежит данной игре" });
                }
            }

            if (newStep == "finished")
            {
                if (!session.IsActive)
                {
                    return Ok(new
                    {
                        message = "Игра уже завершена",
                        currentStep = session.CurrentStep,
                        isActive = session.IsActive
                    });
                }

                try
                {
                    await FinalizeGameAsync(session);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new
                    {
                        message = "Не удалось завершить игру и начислить награды",
                        details = ex.Message
                    });
                }
            }
            else
            {
                session.CurrentStep = newStep;
                session.IsActive = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Шаг изменён",
                currentStep = session.CurrentStep,
                isActive = session.IsActive
            });
        }

        // =========================================================
        // ОТВЕТ ИГРОКА
        // POST /api/niche-game/submit-answer
        // =========================================================

        [HttpPost("submit-answer")]
        [Authorize]
        public async Task<IActionResult> SubmitAnswer([FromBody] NicheSubmitAnswerDto dto)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var session = await _context.GameSessions.FindAsync(dto.GameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            if (!session.IsActive)
            {
                return BadRequest(new { message = "Игра завершена" });
            }

            if (string.IsNullOrWhiteSpace(session.CurrentStep) ||
                !session.CurrentStep.StartsWith("question_"))
            {
                return BadRequest(new { message = "Сейчас нет активного вопроса" });
            }

            var player = await _context.LivePlayers
                .FirstOrDefaultAsync(p => p.GameId == dto.GameId && p.UserId == userId.Value);

            if (player == null)
            {
                return BadRequest(new { message = "Вы не подключены к этой игре" });
            }

            if (string.IsNullOrWhiteSpace(dto.AnswerChoice))
            {
                return BadRequest(new { message = "Выберите вариант ответа" });
            }

            if (!TryParseQuestionStep(session.CurrentStep, out int questionId))
            {
                return BadRequest(new { message = "Некорректный текущий вопрос" });
            }

            var question = await _context.GameQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId && q.EventId == session.EventId);

            if (question == null)
            {
                return BadRequest(new { message = "Вопрос не найден" });
            }

            var options = ParseOptions(question.OptionsJson);

            if (!options.Contains(dto.AnswerChoice))
            {
                return BadRequest(new { message = "Такого варианта ответа нет" });
            }

            var existingAnswer = await _context.LiveStepResponses
                .FirstOrDefaultAsync(x => x.GameId == dto.GameId &&
                                          x.UserId == userId.Value &&
                                          x.StepIdentifier == session.CurrentStep);

            if (existingAnswer != null)
            {
                existingAnswer.AnswerData = dto.AnswerChoice;
            }
            else
            {
                _context.LiveStepResponses.Add(new LiveStepResponse
                {
                    GameId = dto.GameId,
                    UserId = userId.Value,
                    StepIdentifier = session.CurrentStep,
                    AnswerData = dto.AnswerChoice,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Ответ принят",
                choice = dto.AnswerChoice
            });
        }

        // =========================================================
        // РЕЗУЛЬТАТЫ ОДНОГО ВОПРОСА
        // GET /api/niche-game/calculate-results/{gameId}/{stepIdentifier}
        // =========================================================

        [HttpGet("calculate-results/{gameId}/{stepIdentifier}")]
        [Authorize]
        public async Task<IActionResult> CalculateResults(int gameId, string stepIdentifier)
        {
            if (string.IsNullOrWhiteSpace(stepIdentifier) || !stepIdentifier.StartsWith("question_"))
            {
                return BadRequest(new { message = "Нужно передать question_ID" });
            }

            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            if (!TryParseQuestionStep(stepIdentifier, out int questionId))
            {
                return BadRequest(new { message = "Некорректный ID вопроса" });
            }

            var question = await _context.GameQuestions
                .FirstOrDefaultAsync(q => q.Id == questionId && q.EventId == session.EventId);

            if (question == null)
            {
                return NotFound(new { message = "Вопрос не найден" });
            }

            var options = ParseOptions(question.OptionsJson);

            var responses = await _context.LiveStepResponses
                .Where(x => x.GameId == gameId && x.StepIdentifier == stepIdentifier)
                .ToListAsync();

            var totalAnswers = responses.Count;

            var stats = options.Select((option, index) =>
            {
                var count = responses.Count(r => r.AnswerData == option);
                var percentage = totalAnswers == 0 ? 0 : Math.Round(((double)count / totalAnswers) * 100, 1);

                return new
                {
                    answerChoice = option,
                    answerIndex = index,
                    count,
                    percentage
                };
            }).ToList();

            var nicheAnswers = new List<string>();
            var nicheWinners = new List<int>();

            if (totalAnswers > 0 && stats.Any())
            {
                var minCount = stats.Min(x => x.count);

                nicheAnswers = stats
                    .Where(x => x.count == minCount)
                    .Select(x => x.answerChoice)
                    .ToList();

                nicheWinners = responses
                    .Where(r => nicheAnswers.Contains(r.AnswerData))
                    .Select(r => r.UserId)
                    .Distinct()
                    .ToList();
            }

            var totalPlayers = await _context.LivePlayers.CountAsync(x => x.GameId == gameId);

            return Ok(new
            {
                gameId,
                questionId,
                totalAnswers,
                totalPlayers,
                breakdown = stats,
                nicheAnswers,
                nicheWinners
            });
        }

        // =========================================================
        // СПИСОК ВОПРОСОВ
        // GET /api/niche-game/questions/{gameId}
        // =========================================================

        [HttpGet("questions/{gameId}")]
        [Authorize]
        public async Task<IActionResult> GetGameQuestions(int gameId)
        {
            var hostUserId = GetCurrentUserId();

            if (hostUserId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игровая сессия не найдена" });
            }

            if (session.OrganiserUserId != hostUserId.Value)
            {
                return Unauthorized(new { message = "Вы не ведущий этой игры" });
            }

            var questions = await _context.GameQuestions
                .Where(q => q.EventId == session.EventId)
                .OrderBy(q => q.Id)
                .Select(q => new
                {
                    id = q.Id,
                    questionText = q.QuestionText,
                    options = q.OptionsJson
                })
                .ToListAsync();

            return Ok(questions);
        }

        // =========================================================
        // ФИНАЛЬНАЯ ТАБЛИЦА
        // GET /api/niche-game/final-results/{gameId}
        // =========================================================

        [HttpGet("final-results/{gameId}")]
        [Authorize]
        public async Task<IActionResult> GetFinalResults(int gameId)
        {
            var hostUserId = GetCurrentUserId();

            if (hostUserId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            if (session.OrganiserUserId != hostUserId.Value)
            {
                return Unauthorized(new { message = "Вы не ведущий этой игры" });
            }

            var scores = await CalculateGameScores(gameId, session.EventId);

            var players = await _context.LivePlayers
                .Where(p => p.GameId == gameId)
                .ToListAsync();

            if (!players.Any())
            {
                return Ok(Array.Empty<object>());
            }

            var userIds = players.Select(p => p.UserId).Distinct().ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Username);

            var result = players
                .GroupBy(p => p.UserId)
                .Select(group =>
                {
                    var userId = group.Key;
                    var username = users.ContainsKey(userId) ? users[userId] : group.First().Username;
                    var score = scores.TryGetValue(userId, out var value) ? value : 0;

                    return new { userId, username, score };
                })
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.username)
                .Select((x, index) => new
                {
                    place = index + 1,
                    x.userId,
                    x.username,
                    x.score
                })
                .ToList();

            return Ok(result);
        }

        // =========================================================
        // ЗАВЕРШЕНИЕ ИГРЫ
        // POST /api/niche-game/finish/{gameId}
        // =========================================================

        [HttpPost("finish/{gameId}")]
        [Authorize]
        public async Task<IActionResult> FinishGame(int gameId)
        {
            var hostUserId = GetCurrentUserId();

            if (hostUserId == null)
            {
                return Unauthorized(new { message = "Недействительный токен" });
            }

            var session = await _context.GameSessions.FindAsync(gameId);

            if (session == null)
            {
                return NotFound(new { message = "Игра не найдена" });
            }

            if (session.OrganiserUserId != hostUserId.Value)
            {
                return Unauthorized(new { message = "Вы не ведущий этой игры" });
            }

            if (!session.IsActive)
            {
                return Ok(new
                {
                    message = "Игра уже завершена",
                    currentStep = session.CurrentStep,
                    isActive = session.IsActive
                });
            }

            try
            {
                await FinalizeGameAsync(session);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Не удалось завершить игру и начислить награды",
                    details = ex.Message
                });
            }

            return Ok(new
            {
                message = "Игра завершена",
                currentStep = session.CurrentStep,
                isActive = session.IsActive
            });
        }
    }

    // =========================================================
    // DTO
    // =========================================================

    public class NicheAdvanceStepDto
    {
        public int GameId { get; set; }
        public string NewStep { get; set; } = string.Empty;
    }

    public class NicheSubmitAnswerDto
    {
        public int GameId { get; set; }
        public string AnswerChoice { get; set; } = string.Empty;
    }
}