using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeejaServer.Data;
using BeejaServer.Models;

namespace BeejaServer.Controllers
{
    /// <summary>
    /// Универсальный контроллер игровых сущностей.
    /// Поддерживает как /api/games, так и /api/niche-game во избежание 404 ошибок.
    /// </summary>
    [ApiController]
    [Route("api/games")]
    [Route("api/niche-game")]
    public class GameController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GameController(AppDbContext context)
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

        // ============================================================
        // MY GAMES
        // ============================================================

        /// <summary>
        /// Список подготовленных игр текущего ведущего.
        /// Доступен по путям:
        /// GET /api/games/my-games
        /// GET /api/niche-game/my-games
        /// </summary>
        [HttpGet("my-games")]
        [Authorize]
        public async Task<IActionResult> GetMyGames()
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });
            }

            var games = await _context.Events
                .AsNoTracking()
                .Where(e => e.OrganiserUserId == userId)
                .OrderByDescending(e => e.EventId)
                .ToListAsync();

            var result = new List<object>();

            foreach (var game in games)
            {
                var questions = await _context.GameQuestions
                    .AsNoTracking()
                    .Where(q => q.EventId == game.EventId)
                    .ToListAsync();

                var questionsCount = questions.Count;

                var roundsCount = questions
                    .Select(q => q.RoundNumber)
                    .Distinct()
                    .Count();

                result.Add(new
                {
                    eventId = game.EventId,
                    title = game.Title,
                    description = game.Description,
                    gameType = game.GameType,
                    questionsCount,
                    roundsCount,
                    meta = roundsCount > 0
                        ? $"{questionsCount} вопросов • {roundsCount} раундов"
                        : $"{questionsCount} вопросов"
                });
            }

            return Ok(result);
        }

        // ============================================================
        // START SESSION
        // ============================================================

        [HttpPost("start-session/{eventId:int}")]
        [Authorize]
        public async Task<IActionResult> StartSession(int eventId)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });
            }

            var game = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EventId == eventId &&
                    e.OrganiserUserId == userId);

            if (game == null)
            {
                return NotFound(new
                {
                    message = "Игра не найдена или вы не являетесь её ведущим",
                    eventId
                });
            }

            var questionsCount = await _context.GameQuestions
                .CountAsync(q => q.EventId == eventId);

            if (questionsCount == 0)
            {
                return BadRequest(new
                {
                    message = "Для этой подготовленной игры ещё нет вопросов",
                    eventId
                });
            }

            var gameType = game.GameType;

            if (gameType <= 0)
            {
                return BadRequest(new
                {
                    message = "У подготовленной игры не указан корректный gameType",
                    eventId,
                    gameType
                });
            }

            var session = new GameSession
            {
                EventId = game.EventId,
                OrganiserUserId = userId,
                GameType = gameType,
                CurrentStep = "lobby",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.GameSessions.Add(session);
            await _context.SaveChangesAsync();

            var roomCode = await GenerateUniqueRoomCodeAsync();

            var room = new GameRoomCode
            {
                EventId = session.EventId,
                GameId = session.Id,
                RoomCode = roomCode,
                CreatedAt = DateTime.Now
            };

            _context.GameRoomCodes.Add(room);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                gameId = session.Id,
                eventId = session.EventId,
                gameType = session.GameType,
                title = game.Title,
                roomCode,
                currentStep = session.CurrentStep,
                isActive = session.IsActive,
                questionsCount
            });
        }

        // ============================================================
        // JOIN ROOM
        // ============================================================

// ============================================================
// JOIN ROOM
// ============================================================

        [HttpPost("join-room")]
        [Authorize]
        public async Task<IActionResult> JoinRoom([FromBody] JoinGameRoomDto dto)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized(new
                {
                    message = "Недействительный токен"
                });
            }

            var roomCode = (dto.GameCode ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                return BadRequest(new
                {
                    message = "Не указан код комнаты"
                });
            }

            var room = await _context.GameRoomCodes
                .AsNoTracking()
                .Where(x => x.RoomCode == roomCode)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                return NotFound(new
                {
                    message = "Комната с таким кодом не найдена"
                });
            }

            var session = await _context.GameSessions
                .FirstOrDefaultAsync(x => x.Id == room.GameId);

            if (session == null)
            {
                return NotFound(new
                {
                    message = "Игровая сессия не найдена",
                    gameId = room.GameId
                });
            }

            if (!session.IsActive)
            {
                return BadRequest(new
                {
                    message = "Эта игра уже завершена",
                    gameId = session.Id
                });
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Пользователь не найден"
                });
            }

            var gameType = session.GameType;

            if (gameType <= 0)
            {
                return BadRequest(new
                {
                    message = "У игровой сессии не указан тип игры",
                    gameId = session.Id,
                    eventId = session.EventId,
                    gameType
                });
            }

            // Регистрируем игрока в сессии, если он еще не подключен
            var existingPlayer = await _context.LivePlayers
                .FirstOrDefaultAsync(p => p.GameId == session.Id && p.UserId == user.UserId);

            if (existingPlayer == null)
            {
                _context.LivePlayers.Add(new LivePlayer
                {
                    GameId = session.Id,
                    UserId = user.UserId,
                    Username = user.Username,
                    JoinedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                gameId = session.Id,
                eventId = session.EventId,
               userId = user.UserId,
                username = user.Username,
                roomCode = room.RoomCode,
                gameType,
                currentStep = session.CurrentStep,
                isActive = session.IsActive
           });
        }   
        // ============================================================
        // GENERATE UNIQUE ROOM CODE
        // ============================================================

        private async Task<string> GenerateUniqueRoomCodeAsync()
        {
            while (true)
            {
                var code = Random.Shared.Next(1000, 10000).ToString();

                var exists = await _context.GameRoomCodes
                    .AnyAsync(x => x.RoomCode == code);

                if (!exists)
                {
                    return code;
                }
            }
        }
    }

    public class JoinGameRoomDto
    {
        public string GameCode { get; set; } = string.Empty;
    }
}