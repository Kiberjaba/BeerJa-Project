using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeejaServer.Data;
using BeejaServer.DTOs;
using BeejaServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
                var normalizedUsername = dto.Username.Trim();

                if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var input = dto.LoginOrEmail.Trim().ToLowerInvariant();

                var user = await _context.Users.FirstOrDefaultAsync(u => 
                    u.Email == input || u.Username.ToLower() == input);

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    return BadRequest(new { message = "Неверный логин/email или пароль" });
                }

                string token = GenerateJwtToken(user);

                var response = new AuthResponseDto
                {
                    Token = token,
                    User = new UserResponseDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex}");
                return StatusCode(500, new { message = "Ошибка при авторизации на сервере" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var response = new UserResponseDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения профиля: {ex}");
                return StatusCode(500, new { message = "Ошибка сервера при получении профиля" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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