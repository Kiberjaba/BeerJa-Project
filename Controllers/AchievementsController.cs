using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BeejaServer.Models;
using BeejaServer.Data;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/v1/User")]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AchievementsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("achievements")]
        public async Task<IActionResult> GetAchievements()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var unlockedIds = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.AchievementId)
                .ToListAsync();

            var achievements = await _context.Achievements
                .OrderBy(a => a.AchievementId)
                .Select(a => new
                {
                    title = a.Title,
                    description = a.Description,
                    pointsReward = a.PointsReward,
                    isUnlocked = unlockedIds.Contains(a.AchievementId)
                })
                .ToListAsync();

            return Ok(achievements);
        }
    }
}