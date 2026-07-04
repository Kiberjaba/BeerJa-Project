using Microsoft.AspNetCore.Mvc;
using BeejaServer.Data;
using BeejaServer.Models;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public IActionResult Login(int gameId, int tableNumber, string username)
        {
            // Проверяем, есть ли уже такой юзер (чтобы не дублировать)
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Username == username && u.CurrentGameId == gameId);

            if (existingUser != null)
            {
                return Ok(new { userId = existingUser.UserId, message = "С возвращением!" });
            }

            // Если новый — создаем
            var newUser = new User
            {
                Username = username,
                TableNumber = tableNumber,
                CurrentGameId = gameId
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok(new { userId = newUser.UserId, message = "Добро пожаловать в игру!" });
        }
    }
}