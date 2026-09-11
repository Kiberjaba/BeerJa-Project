using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BeejaServer.Models;
using BeejaServer.Data;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/v1/host")]
    public class VedController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VedController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Не удалось определить пользователя из токена" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            var events = await _context.Events
                .Where(e => e.OrganiserUserId == userId)
                .OrderByDescending(e => e.StartsAt)
                .ToListAsync();

            int hostedSessionsCount = events.Count(e => e.Status == "completed");
            int createdGamesCount = events.Count;

            var eventIds = events.Select(e => e.EventId).ToList();
            int totalPlayersCount = await _context.EventCheckins
                .Where(ec => eventIds.Contains(ec.EventId))
                .CountAsync();

            // room_code больше не хранится в Event — он принадлежит конкретной запущенной
            // сессии (GameSession + GameRoomCode). Берём код последней сессии по каждому event.
            var roomCodesRaw = await _context.GameSessions
                .Where(s => eventIds.Contains(s.EventId))
                .Join(_context.GameRoomCodes,
                    s => s.Id,
                    r => r.GameId,
                    (s, r) => new { s.EventId, r.RoomCode, r.CreatedAt })
                .ToListAsync();

            var roomCodeByEvent = roomCodesRaw
                .GroupBy(x => x.EventId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.CreatedAt).First().RoomCode
                );

            var response = new
            {
                username = user.Username,
                photo = user.Photo,
                hostedSessionsCount = hostedSessionsCount,
                createdGamesCount = createdGamesCount,
                totalPlayersCount = totalPlayersCount,
                games = events.Select(e => new {
                    eventId = e.EventId,
                    title = e.Title,
                    description = e.Description,
                    roomCode = roomCodeByEvent.TryGetValue(e.EventId, out var code) ? code : null,
                    status = e.Status,
                    progressPercentage = e.ProgressPercentage,
                    startsAt = e.StartsAt,
                    endsAt = e.EndsAt,
                    format = e.Format,
                    address = e.Address
                })
            };

            return Ok(response);
        }
    }
}