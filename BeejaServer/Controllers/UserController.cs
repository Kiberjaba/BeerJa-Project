using BeejaServer.Data;
using BeejaServer.DTOs;
using BeejaServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Проверяем, существует ли пользователь с таким email или username
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { message = "Пользователь с таким Email уже существует" });
            }

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            {
                return BadRequest(new { message = "Имя пользователя уже занято" });
            }

            // 2. Хэшируем пароль
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Создаем сущность пользователя
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Сохраняем в БД
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Формируем ответ (без пароля!)
            var response = new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return Ok(response);
        }
    }
}