using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
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

        #region Yandex OAuth

        [HttpGet("yandex-login")]
        public IActionResult YandexLogin()
        {
            var clientId = _configuration["Yandex:ClientId"];
            var redirectUri = Uri.EscapeDataString(_configuration["Yandex:RedirectUri"]!);

            string yandexAuthUrl = $"https://oauth.yandex.ru/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}";

            return Redirect(yandexAuthUrl);
        }

        [HttpGet("yandex-callback")]
        public async Task<IActionResult> YandexCallback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { message = "Код авторизации не получен" });
            }

            try
            {
                using var httpClient = new HttpClient();

                // 1. Обмениваем временный code на access_token от Яндекса
                var tokenRequestParams = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "client_id", _configuration["Yandex:ClientId"]! },
                    { "client_secret", _configuration["Yandex:ClientSecret"]! }
                };

                var tokenResponse = await httpClient.PostAsync("https://oauth.yandex.ru/token", new FormUrlEncodedContent(tokenRequestParams));
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Ошибка получения токена от Яндекс" });
                }

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<YandexTokenResponse>();
                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    return BadRequest(new { message = "Не удалось распарсить токен Яндекс" });
                }

                // 2. С помощью access_token запрашиваем данные пользователя из Яндекс ID
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", tokenData.AccessToken);
                var userInfoResponse = await httpClient.GetAsync("https://login.yandex.ru/info?format=json");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Не удалось получить профиль пользователя Яндекс" });
                }

                var yandexUser = await userInfoResponse.Content.ReadFromJsonAsync<YandexUserInfo>();
                if (yandexUser == null || string.IsNullOrEmpty(yandexUser.DefaultEmail))
                {
                    return BadRequest(new { message = "Email не предоставлен сервисом Яндекс" });
                }

                // 3. Ищем пользователя в базе или создаем нового
                var normalizedEmail = yandexUser.DefaultEmail.Trim().ToLowerInvariant();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (user == null)
                {
                    user = new User
                    {
                        Username = !string.IsNullOrEmpty(yandexUser.DisplayLogin) ? yandexUser.DisplayLogin : yandexUser.DefaultEmail.Split('@')[0],
                        Email = normalizedEmail,
                        PasswordHash = string.Empty, // При входе через OAuth пароль не нужен
                        IsEmailConfirmed = true,     // Почта Яндекса проверена
                        TotalPoints = 0,
                        Level = 1,
                        CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (!user.IsEmailConfirmed)
                    {
                        user.IsEmailConfirmed = true;
                        await _context.SaveChangesAsync();
                    }
                }

                // 4. Генерируем собственный JWT-токен
                string jwtToken = GenerateJwtToken(user);

                var response = new AuthResponseDto
                {
                    Token = jwtToken,
                    User = new UserResponseDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        Email = user.Email,
                        IsEmailConfirmed = user.IsEmailConfirmed,
                        TotalPoints = user.TotalPoints,
                        Level = user.Level,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка OAuth Яндекс: {ex}");
                return StatusCode(500, new { message = "Ошибка авторизации через Яндекс", error = ex.Message });
            }
        }

        #endregion

        #region Standard Auth

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
                    IsEmailConfirmed = false,
                    TotalPoints = 0,
                    Level = 1,
                    CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var response = new UserResponseDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    IsEmailConfirmed = user.IsEmailConfirmed,
                    TotalPoints = user.TotalPoints,
                    Level = user.Level,
                    CreatedAt = user.CreatedAt
                };

                bool emailSent = await SendConfirmationEmailAsync(user.Email, user.Username);

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
                    message = "Успешная регистрация. Ссылка отправлена в консоль!",
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
                return StatusCode(500, new { message = "Ошибка при регистрации пользователя на сервере", error = ex.Message });
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
                        IsEmailConfirmed = user.IsEmailConfirmed,
                        TotalPoints = user.TotalPoints,
                        Level = user.Level,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка входа: {ex}");
                return StatusCode(500, new { message = "Ошибка при авторизации на сервере", error = ex.Message });
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
                    IsEmailConfirmed = user.IsEmailConfirmed,
                    TotalPoints = user.TotalPoints,
                    Level = user.Level,
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

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            user.IsEmailConfirmed = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Email {email} успешно подтверждён!" });
        }

        #endregion

        #region Helpers

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

        private async Task<bool> SendConfirmationEmailAsync(string toEmail, string username)
        {
            string confirmationLink = $"http://localhost:5234/api/v1/User/confirm-email?email={Uri.EscapeDataString(toEmail)}";

            Console.WriteLine($"\n==================================================");
            Console.WriteLine($"[EMAIL MOCK] Ссылка подтверждения для {username} ({toEmail}):");
            Console.WriteLine(confirmationLink);
            Console.WriteLine($"==================================================\n");

            await Task.CompletedTask;
            return true;
        }

        #endregion
    }

    #region Yandex DTOs

    public class YandexTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    public class YandexUserInfo
    {
        [JsonPropertyName("default_email")]
        public string DefaultEmail { get; set; } = string.Empty;

        [JsonPropertyName("display_login")]
        public string DisplayLogin { get; set; } = string.Empty;
    }

    #endregion
}