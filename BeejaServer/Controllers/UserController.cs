using BeejaServer.Data;
using BeejaServer.DTOs;
using BeejaServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
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
            try
            {
                var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
                var normalizedUsername = dto.Username.Trim();

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (existingUser != null)
                {
                    return Conflict(new { message = "Email уже зарегистрирован" });
                }

                if (await _context.Users.AnyAsync(u => u.Username == normalizedUsername))
                {
                    return Conflict(new { message = "Имя пользователя уже занято" });
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Username = normalizedUsername,
                    Email = normalizedEmail,
                    PasswordHash = passwordHash,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var response = new UserResponseDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };

                bool emailSent = await SendConfirmationEmailAsync(user.Email);

                if (!emailSent)
                {
                    return StatusCode(201, new 
                    { 
                        message = "Регистрация успешна, но email подтверждения не был отправлен",
                        user = response 
                    });
                }

                return StatusCode(201, new 
                { 
                    message = "Успешная регистрация. Письмо с подтверждением отправлено на ваш Email",
                    user = response 
                });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Пользователь с таким Email или именем уже существует" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка регистрации: {ex}");
                return StatusCode(500, new { message = "Ошибка при регистрации пользователя на сервере" });
            }
        }

        private async Task<bool> SendConfirmationEmailAsync(string email)
        {
            try
            {
                await Task.Delay(100);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}