using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BeejaServer.Data;
using BeejaServer.DTOs;
using BeejaServer.Models;
using BeejaServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

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

                var normalizedEmail = yandexUser.DefaultEmail.Trim().ToLowerInvariant();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (user == null)
                {
                    user = new User
                    {
                        Username = !string.IsNullOrEmpty(yandexUser.DisplayLogin) ? yandexUser.DisplayLogin : yandexUser.DefaultEmail.Split('@')[0],
                        Email = normalizedEmail,
                        PasswordHash = string.Empty,
                        IsEmailConfirmed = true,
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

        #region Standard Auth & Points

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

                int calculatedLevel = LevelService.CalculateLevel(user.TotalPoints);
                if (user.Level != calculatedLevel)
                {
                    user.Level = calculatedLevel;
                    await _context.SaveChangesAsync();
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

        [HttpGet("profile-data")]
        [Authorize]
        public async Task<IActionResult> GetProfileData()
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

                // 1. Расчёт уровня и прогресса
                int currentLevel = LevelService.CalculateLevel(user.TotalPoints);
                if (user.Level != currentLevel)
                {
                    user.Level = currentLevel;
                    await _context.SaveChangesAsync();
                }

                int currentLevelBase = LevelService.GetRequiredPointsForLevel(currentLevel);
                int nextLevelBase = LevelService.GetRequiredPointsForLevel(currentLevel + 1);

                int pointsInCurrentLevel = user.TotalPoints - currentLevelBase;
                int pointsRequiredForNext = nextLevelBase - currentLevelBase;
                int pointsRemaining = nextLevelBase - user.TotalPoints;

                double progressPercentage = LevelService.CalculateProgressPercentage(user.TotalPoints);

                // 2. Считаем реальное количество пройденных сессий из event_checkins
                int realSessionsCount = await _context.EventCheckins
                    .CountAsync(c => c.UserId == userId);

                // 3. Загружаем последние 10 чекинов с именами игр/ивентов через JOIN с таблицей events
                var recentCheckins = await _context.EventCheckins
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CheckedInAt)
                    .Take(10)
                    .Select(c => new UserCheckinHistoryDto
                    {
                         // Берём название из связанной таблицы events
                        EventTitle = c.Event != null ? c.Event.Title : "Мероприятие",
                        CheckedInAt = c.CheckedInAt,
                        PointsAwarded = c.PointsAwarded
                    })
                    .ToListAsync();

        // 4. Формируем итоговый ответ
                var response = new UserProfileUiDto
                {
                    Username = user.Username, // если в модели поле называется UserName, замените на user.UserName
                    Level = currentLevel,
                    TotalPoints = user.TotalPoints,
                    PointsInCurrentLevel = pointsInCurrentLevel,
                    PointsRequiredForNext = pointsRequiredForNext,
                    PointsRemaining = pointsRemaining,
                    ProgressPercentage = progressPercentage,
                    SessionsCount = realSessionsCount,
                    RecentCheckins = recentCheckins
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных профиля: {ex}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        [HttpPost("add-points-by-username")]
        public async Task<IActionResult> AddPointsByUsername([FromBody] AddPointsByUsernameDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                return BadRequest(new { message = "Укажите имя пользователя" });
            }

            if (dto.Points <= 0)
            {
                return BadRequest(new { message = "Количество очков должно быть больше нуля" });
            }

            try
            {
                var normalizedUsername = dto.Username.Trim().ToLowerInvariant();

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);
                
                if (user == null)
                {
                    return NotFound(new { message = $"Пользователь с ником '{dto.Username}' не найден" });
                }

                int oldLevel = user.Level;

                user.TotalPoints += dto.Points;

                int newLevel = LevelService.CalculateLevel(user.TotalPoints);
                bool leveledUp = newLevel > oldLevel;

                user.Level = newLevel;
                await _context.SaveChangesAsync();

                var response = new AddPointsResponseDto
                {
                    Message = leveledUp ? $"Пользователь {user.Username} достиг {newLevel} уровня!" : $"Пользователю {user.Username} начислено +{dto.Points} очков!",
                    AddedPoints = dto.Points,
                    TotalPoints = user.TotalPoints,
                    Level = user.Level,
                    LeveledUp = leveledUp,
                    NextLevelPoints = LevelService.GetRequiredPointsForLevel(user.Level + 1),
                    ProgressPercentage = LevelService.CalculateProgressPercentage(user.TotalPoints)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка начисления очков по нику: {ex}");
                return StatusCode(500, new { message = "Ошибка при начислении очков" });
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

        [HttpPost("add-points")]
        [Authorize]
        public async Task<IActionResult> AddPoints([FromBody] AddPointsDto dto)
        {
            if (dto.Points <= 0)
            {
                return BadRequest(new { message = "Количество очков должно быть больше нуля" });
            }

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

                int oldLevel = user.Level;

                user.TotalPoints += dto.Points;
                int newLevel = LevelService.CalculateLevel(user.TotalPoints);
                bool leveledUp = newLevel > oldLevel;

                user.Level = newLevel;
                await _context.SaveChangesAsync();

                var response = new AddPointsResponseDto
                {
                    Message = leveledUp ? $"Поздравляем! Вы достигли {newLevel} уровня!" : $"Начислено +{dto.Points} очков!",
                    AddedPoints = dto.Points,
                    TotalPoints = user.TotalPoints,
                    Level = user.Level,
                    LeveledUp = leveledUp,
                    NextLevelPoints = LevelService.GetRequiredPointsForLevel(user.Level + 1),
                    ProgressPercentage = LevelService.CalculateProgressPercentage(user.TotalPoints)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка начисления очков: {ex}");
                return StatusCode(500, new { message = "Ошибка при начислении очков" });
            }
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

    #region DTOs

    public class AddPointsByUsernameDto
    {
        public string Username { get; set; } = string.Empty;
        public int Points { get; set; }
    }

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

    public class AddPointsDto
    {
        public int Points { get; set; }
    }

    public class AddPointsResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public int AddedPoints { get; set; }
        public int TotalPoints { get; set; }
        public int Level { get; set; }
        public bool LeveledUp { get; set; }
        public int NextLevelPoints { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class UserProfileUiDto
    {
        public string Username { get; set; } = string.Empty;
        public int Level { get; set; }
        public int TotalPoints { get; set; }
        public int PointsInCurrentLevel { get; set; }
        public int PointsRequiredForNext { get; set; }
        public int PointsRemaining { get; set; }
        public double ProgressPercentage { get; set; }
        public int SessionsCount { get; set; }


        public List<UserCheckinHistoryDto> RecentCheckins { get; set; } = new();
    }
    
    public class UserCheckinHistoryDto
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime CheckedInAt { get; set; }
        public int PointsAwarded { get; set; }
    }
    #endregion
}