using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeejaServer.Models;
using BeejaServer.Data;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BeejaServer.Controllers
{
    [Route("api/teams")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TeamController(AppDbContext context)
        {
            _context = context;
        }

        // Универсальный метод получения ID текущего пользователя (JWT Claims + сессия)
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("UserId");
            if (claim != null && int.TryParse(claim.Value, out int idFromToken))
            {
                return idFromToken;
            }

            var userIdFromSession = HttpContext.Session.GetInt32("UserId");
            if (userIdFromSession.HasValue)
            {
                return userIdFromSession.Value;
            }

            throw new UnauthorizedAccessException("Пользователь не авторизован");
        }

        // 1. Создать команду
        [HttpPost("create")]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Name))
            {
                return BadRequest(new { message = "Название команды не может быть пустым" });
            }

            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var team = new Team
            {
                Name = request.Name,
                CreatorId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Создатель сразу становится участником со статусом 'accepted'
            var member = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Status = "accepted",
                CreatedAt = DateTime.Now
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Команда успешно создана", teamId = team.Id });
        }

        // 2. Получить список команд пользователя (для личного кабинета)
        [HttpGet("my-teams")]
        public async Task<IActionResult> GetMyTeams()
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var memberships = await _context.TeamMembers
                .Where(tm => tm.UserId == userId)
                .ToListAsync();

            var teamIds = memberships.Select(m => m.TeamId).ToList();
            var teams = await _context.Teams.Where(t => teamIds.Contains(t.Id)).ToListAsync();

            var result = memberships.Select(m => {
                var team = teams.FirstOrDefault(t => t.Id == m.TeamId);
                return new
                {
                    TeamId = m.TeamId,
                    TeamName = team?.Name,
                    Status = m.Status,
                    IsCreator = team?.CreatorId == userId
                };
            });

            return Ok(result);
        }

        // 3. Пригласить игрока в команду
        [HttpPost("invite")]
        public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId);
            if (team == null)
            {
                return NotFound(new { message = "Команда не найдена" });
            }

            if (team.CreatorId != userId)
            {
                return BadRequest(new { message = "Только создатель команды может отправлять приглашения" });
            }

            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == request.TeamId && tm.UserId == request.TargetUserId);

            if (existingMember != null)
            {
                return BadRequest(new { message = "Игрок уже состоит в команде или имеет приглашение" });
            }

            var newMember = new TeamMember
            {
                TeamId = request.TeamId,
                UserId = request.TargetUserId,
                Status = "pending",
                CreatedAt = DateTime.Now
            };

            _context.TeamMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Приглашение успешно отправлено" });
        }

        // 4. Подтвердить или отклонить приглашение / заявку
        [HttpPost("respond-invite")]
        public async Task<IActionResult> RespondInvite([FromBody] RespondInviteRequest request)
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == request.TeamId && tm.UserId == userId);

            if (membership == null)
            {
                return NotFound(new { message = "Приглашение не найдено" });
            }

            if (request.Accept)
            {
                membership.Status = "accepted";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Вы успешно присоединились к команде!" });
            }
            else
            {
                _context.TeamMembers.Remove(membership);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Приглашение отклонено" });
            }
        }

        // 5. Удалить команду
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteTeam([FromBody] DeleteTeamRequest request)
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId);

            if (team == null)
            {
                return NotFound(new { message = "Команда не найдена" });
            }

            if (team.CreatorId != userId)
            {
                return BadRequest(new { message = "Только создатель может удалить команду" });
            }

            var members = await _context.TeamMembers.Where(tm => tm.TeamId == request.TeamId).ToListAsync();
            _context.TeamMembers.RemoveRange(members);

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Команда успешно удалена" });
        }

        // 6. Получить список участников конкретной команды
        [HttpGet("{teamId}/members")]
        public async Task<IActionResult> GetTeamMembers(int teamId)
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var userMembership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.Status == "accepted");

            if (userMembership == null)
            {
                return BadRequest(new { message = "У вас нет доступа к этой команде" });
            }

            var members = await _context.TeamMembers
                .Where(tm => tm.TeamId == teamId && tm.Status == "accepted")
                .ToListAsync();

            var userIds = members.Select(m => m.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);

            var result = members.Select(m => {
                var user = users.FirstOrDefault(u => u.UserId == m.UserId);
                return new
                {
                    UserId = m.UserId,
                    Username = user?.Username ?? "Игрок",
                    IsCreator = team?.CreatorId == m.UserId
                };
            });

            return Ok(result);
        }

        // 7. Удалить участника из команды (доступно только создателю)
        [HttpPost("remove-member")]
        public async Task<IActionResult> RemoveMember([FromBody] RemoveMemberRequest request)
        {
            int userId;
            try { userId = GetCurrentUserId(); }
            catch { return Unauthorized(new { message = "Требуется авторизация" }); }

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId);
            if (team == null)
            {
                return NotFound(new { message = "Команда не найдена" });
            }

            if (team.CreatorId != userId)
            {
                return BadRequest(new { message = "Только создатель может удалять участников" });
            }

            if (team.CreatorId == request.UserId)
            {
                return BadRequest(new { message = "Нельзя удалить создателя из команды" });
            }

            var membership = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == request.TeamId && tm.UserId == request.UserId);

            if (membership == null)
            {
                return NotFound(new { message = "Участник не найден в команде" });
            }

            _context.TeamMembers.Remove(membership);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Участник успешно удален" });
        }
    }

    // --- Классы запросов ---

    public class CreateTeamRequest
    {
        public required string Name { get; set; }
    }

    public class InviteMemberRequest
    {
        public int TeamId { get; set; }
        public int TargetUserId { get; set; }
    }

    public class RespondInviteRequest
    {
        public int TeamId { get; set; }
        public bool Accept { get; set; }
    }

    public class DeleteTeamRequest
    {
        public int TeamId { get; set; }
    }

    public class RemoveMemberRequest
    {
        public int TeamId { get; set; }
        public int UserId { get; set; }
    }
}