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
using MimeKit;
using MailKit.Net.Smtp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BeejaServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(
            AppDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================================================
        // YANDEX OAUTH
        // ============================================================

        [HttpGet("yandex-login")]
        public IActionResult YandexLogin(
            [FromQuery] string returnUrl = "/yandex_user.html")
        {
            var clientId = _configuration["Yandex:ClientId"];

            var redirectUri = Uri.EscapeDataString(
                _configuration["Yandex:RedirectUri"]!
            );

            string state = Uri.EscapeDataString(returnUrl);

            string yandexAuthUrl =
                $"https://oauth.yandex.ru/authorize" +
                $"?response_type=code" +
                $"&client_id={clientId}" +
                $"&redirect_uri={redirectUri}" +
                $"&state={state}";

            return Redirect(yandexAuthUrl);
        }

        [HttpGet("yandex-callback")]
        public async Task<IActionResult> YandexCallback(
            [FromQuery] string code,
            [FromQuery] string? state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new
                {
                    message = "Код авторизации не получен"
                });
            }

            try
            {
                using var httpClient = new HttpClient();

                var tokenRequestParams =
                    new Dictionary<string, string>
                    {
                        {
                            "grant_type",
                            "authorization_code"
                        },
                        {
                            "code",
                            code
                        },
                        {
                            "client_id",
                            _configuration["Yandex:ClientId"]!
                        },
                        {
                            "client_secret",
                            _configuration["Yandex:ClientSecret"]!
                        },
                        {
                            "redirect_uri",
                            _configuration["Yandex:RedirectUri"]!
                        }
                    };

                var tokenResponse =
                    await httpClient.PostAsync(
                        "https://oauth.yandex.ru/token",
                        new FormUrlEncodedContent(
                            tokenRequestParams
                        )
                    );

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        message =
                            "Ошибка получения токена от Яндекс"
                    });
                }

                var tokenData =
                    await tokenResponse.Content
                        .ReadFromJsonAsync<YandexTokenResponse>();

                if (tokenData == null ||
                    string.IsNullOrEmpty(
                        tokenData.AccessToken))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не удалось получить токен Яндекс"
                    });
                }

                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers
                        .AuthenticationHeaderValue(
                            "OAuth",
                            tokenData.AccessToken
                        );

                var userInfoResponse =
                    await httpClient.GetAsync(
                        "https://login.yandex.ru/info?format=json"
                    );

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        message =
                            "Не удалось получить профиль пользователя Яндекс"
                    });
                }

                var yandexUser =
                    await userInfoResponse.Content
                        .ReadFromJsonAsync<YandexUserInfo>();

                if (yandexUser == null ||
                    string.IsNullOrEmpty(
                        yandexUser.DefaultEmail))
                {
                    return BadRequest(new
                    {
                        message =
                            "Email не предоставлен Яндекс"
                    });
                }

                var normalizedEmail =
                    yandexUser.DefaultEmail
                        .Trim()
                        .ToLowerInvariant();

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u => u.Email == normalizedEmail
                        );

                if (user == null)
                {
                    user = new User
                    {
                        Username =
                            !string.IsNullOrEmpty(
                                yandexUser.DisplayLogin)
                                ? yandexUser.DisplayLogin
                                : normalizedEmail.Split('@')[0],

                        Email = normalizedEmail,

                        PasswordHash = string.Empty,

                        IsEmailConfirmed = true,

                        EmailVerificationToken = null,

                        EmailVerificationTokenExpiresAt = null,

                        TotalPoints = 0,

                        Level = 1,

                        CreatedAtUnix =
                            DateTimeOffset.UtcNow
                                .ToUnixTimeSeconds()
                    };

                    _context.Users.Add(user);

                    await _context.SaveChangesAsync();
                }
                else if (!user.IsEmailConfirmed)
                {
                    user.IsEmailConfirmed = true;
                    user.EmailVerificationToken = null;
                    user.EmailVerificationTokenExpiresAt = null;

                    await _context.SaveChangesAsync();
                }

                string jwtToken =
                    GenerateJwtToken(user);

                string targetPage =
                    !string.IsNullOrEmpty(state)
                        ? Uri.UnescapeDataString(state)
                        : "/yandex_user.html";

                string frontendBaseUrl =
                    "https://beerjaproject.ru";

                return Redirect(
                    $"{frontendBaseUrl}{targetPage}" +
                    $"?token={Uri.EscapeDataString(jwtToken)}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка OAuth Яндекс: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка авторизации через Яндекс",
                    error = ex.Message
                });
            }
        }


        // ============================================================
        // USERNAME
        // ============================================================

        [HttpPut("update-username")]
        [Authorize]
        public async Task<IActionResult> UpdateUsername(
            [FromBody] UpdateUsernameDto dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.Username))
                {
                    return BadRequest(new
                    {
                        message =
                            "Укажите новое имя пользователя"
                    });
                }

                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(
                        userIdClaim,
                        out int userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Недействительный токен"
                    });
                }

                var user =
                    await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Пользователь не найден"
                    });
                }

                var newUsername =
                    dto.Username.Trim();

                if (await _context.Users.AnyAsync(
                    u => u.Username == newUsername &&
                         u.UserId != userId))
                {
                    return Conflict(new
                    {
                        message =
                            "Имя пользователя уже занято"
                    });
                }

                user.Username = newUsername;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Имя пользователя успешно обновлено",
                    username = user.Username
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка обновления имени: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка при обновлении имени пользователя"
                });
            }
        }


        // ============================================================
        // AVATAR
        // ============================================================

        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(
            IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Файл не выбран"
                    });
                }

                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(
                        userIdClaim,
                        out int userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Недействительный токен"
                    });
                }

                var user =
                    await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Пользователь не найден"
                    });
                }

                var fileName =
                    $"user_{userId}_{Guid.NewGuid()}.jpg";

                var folderPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "photos"
                    );

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath =
                    Path.Combine(
                        folderPath,
                        fileName
                    );

                using (var stream =
                       file.OpenReadStream())
                using (var image =
                       await Image.LoadAsync(stream))
                {
                    int maxDimension = 1024;

                    if (image.Width > maxDimension ||
                        image.Height > maxDimension)
                    {
                        image.Mutate(x =>
                            x.Resize(
                                new ResizeOptions
                                {
                                    Size =
                                        new Size(
                                            maxDimension,
                                            maxDimension
                                        ),
                                    Mode =
                                        ResizeMode.Max
                                }
                            )
                        );
                    }

                    var encoder =
                        new JpegEncoder
                        {
                            Quality = 75
                        };

                    await image.SaveAsync(
                        filePath,
                        encoder
                    );
                }

                var photoUrl =
                    $"/photos/{fileName}";

                user.Photo = photoUrl;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Аватар успешно загружен и сжат",
                    photo = photoUrl
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка загрузки аватара: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка при обработке изображения"
                });
            }
        }


        // ============================================================
        // DELETE ACCOUNT (Удаление аккаунта)
        // ============================================================

        [HttpDelete("delete-account")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                var teamMemberships = await _context.TeamMembers.Where(tm => tm.UserId == userId).ToListAsync();
                _context.TeamMembers.RemoveRange(teamMemberships);

                var checkins = await _context.EventCheckins.Where(c => c.UserId == userId).ToListAsync();
                _context.EventCheckins.RemoveRange(checkins);

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Аккаунт успешно удален" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении аккаунта: {ex}");
                return StatusCode(500, new { message = "Ошибка при удалении аккаунта" });
            }
        }


        // ============================================================
        // REGISTER
        // ============================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Username) ||
                    string.IsNullOrWhiteSpace(dto.Password))
                {
                    return BadRequest(new
                    {
                        message =
                            "Заполните все поля"
                    });
                }

                var normalizedEmail =
                    dto.Email
                        .Trim()
                        .ToLowerInvariant();

                var normalizedUsername =
                    dto.Username.Trim();

                if (await _context.Users.AnyAsync(
                    u => u.Email == normalizedEmail))
                {
                    return Conflict(new
                    {
                        message =
                            "Email уже зарегистрирован"
                    });
                }

                if (await _context.Users.AnyAsync(
                    u => u.Username == normalizedUsername))
                {
                    return Conflict(new
                    {
                        message =
                            "Имя пользователя уже занято"
                    });
                }

                string passwordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        dto.Password
                    );

                string verificationToken =
                    Guid.NewGuid().ToString("N");

                var user = new User
                {
                    Username =
                        normalizedUsername,

                    Email =
                        normalizedEmail,

                    PasswordHash =
                        passwordHash,

                    IsEmailConfirmed =
                        false,

                    EmailVerificationToken =
                        verificationToken,

                    EmailVerificationTokenExpiresAt =
                        DateTime.UtcNow.AddHours(24),

                    TotalPoints = 0,

                    Level = 1,

                    CreatedAtUnix =
                        DateTimeOffset.UtcNow
                            .ToUnixTimeSeconds()
                };

                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                var response =
                    new UserResponseDto
                    {
                        UserId =
                            user.UserId,

                        Username =
                            user.Username,

                        Email =
                            user.Email,

                        Photo =
                            user.Photo,

                        IsEmailConfirmed =
                            user.IsEmailConfirmed,

                        TotalPoints =
                            user.TotalPoints,

                        Level =
                            user.Level,

                        CreatedAt =
                            user.CreatedAt
                    };

                bool emailSent =
                    await SendConfirmationEmailAsync(
                        user.Email,
                        user.Username,
                        verificationToken
                    );

                if (!emailSent)
                {
                    return StatusCode(201, new
                    {
                        message =
                            "Регистрация успешна, но письмо подтверждения не отправлено",
                        user = response
                    });
                }

                return StatusCode(201, new
                {
                    message =
                        "Регистрация успешна. Подтвердите email.",
                    user = response
                });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "Пользователь с таким Email или именем уже существует"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка регистрации: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка при регистрации пользователя"
                });
            }
        }


        // ============================================================
        // LOGIN
        // ============================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            try
            {
                if (dto == null ||
                    string.IsNullOrWhiteSpace(
                        dto.LoginOrEmail) ||
                    string.IsNullOrWhiteSpace(
                        dto.Password))
                {
                    return BadRequest(new
                    {
                        message =
                            "Заполните все поля"
                    });
                }

                var input =
                    dto.LoginOrEmail
                        .Trim()
                        .ToLowerInvariant();

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.Email.ToLower() ==
                                    input ||
                                u.Username.ToLower() ==
                                    input
                        );

                if (user == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Неверный логин/email или пароль"
                    });
                }

                if (!user.IsEmailConfirmed)
                {
                    var createdAtUtc = DateTimeOffset.FromUnixTimeSeconds(user.CreatedAtUnix).UtcDateTime;
                    if (createdAtUtc < DateTime.UtcNow.AddHours(-24))
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();

                        return BadRequest(new
                        {
                            message = "Срок подтверждения почты истёк (24 часа). Аккаунт удален, зарегистрируйтесь заново."
                        });
                    }
                }

                if (string.IsNullOrEmpty(
                        user.PasswordHash))
                {
                    return BadRequest(new
                    {
                        message =
                            "Для этого аккаунта доступен только вход через Яндекс"
                    });
                }

                bool isValidPassword =
                    BCrypt.Net.BCrypt.Verify(
                        dto.Password,
                        user.PasswordHash
                    );

                if (!isValidPassword)
                {
                    return BadRequest(new
                    {
                        message =
                            "Неверный логин/email или пароль"
                    });
                }

                string token =
                    GenerateJwtToken(user);

                return Ok(
                    new AuthResponseDto
                    {
                        Token = token,

                        User =
                            new UserResponseDto
                            {
                                UserId =
                                    user.UserId,

                                Username =
                                    user.Username,

                                Email =
                                    user.Email,

                                Photo =
                                    user.Photo,

                                IsEmailConfirmed =
                                    user.IsEmailConfirmed,

                                TotalPoints =
                                    user.TotalPoints,

                                Level =
                                    user.Level,

                                CreatedAt =
                                    user.CreatedAt
                            }
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка входа: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка авторизации на сервере"
                });
            }
        }


        // ============================================================
        // ME
        // ============================================================

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(
                        userIdClaim,
                        out int userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Недействительный токен"
                    });
                }

                var user =
                    await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Пользователь не найден"
                    });
                }

                int calculatedLevel =
                    LevelService.CalculateLevel(
                        user.TotalPoints
                    );

                if (user.Level != calculatedLevel)
                {
                    user.Level =
                        calculatedLevel;

                    await _context.SaveChangesAsync();
                }

                return Ok(
                    new UserResponseDto
                    {
                        UserId =
                            user.UserId,

                        Username =
                            user.Username,

                        Email =
                            user.Email,

                        Photo =
                            user.Photo,

                        IsEmailConfirmed =
                            user.IsEmailConfirmed,

                        TotalPoints =
                            user.TotalPoints,

                        Level =
                            user.Level,

                        CreatedAt =
                            user.CreatedAt
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка получения профиля: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка сервера"
                });
            }
        }


        // ============================================================
        // PROFILE DATA
        // ============================================================

        [HttpGet("profile-data")]
        [Authorize]
        public async Task<IActionResult> GetProfileData()
        {
            try
            {
                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(
                        userIdClaim,
                        out int userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Недействительный токен"
                    });
                }

                var user =
                    await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Пользователь не найден"
                    });
                }

                int currentLevel =
                    LevelService.CalculateLevel(
                        user.TotalPoints
                    );

                if (user.Level != currentLevel)
                {
                    user.Level =
                        currentLevel;

                    await _context.SaveChangesAsync();
                }

                int currentLevelBase =
                    LevelService
                        .GetRequiredPointsForLevel(
                            currentLevel
                        );

                int nextLevelBase =
                    LevelService
                        .GetRequiredPointsForLevel(
                            currentLevel + 1
                        );

                int pointsInCurrentLevel =
                    user.TotalPoints -
                    currentLevelBase;

                int pointsRequiredForNext =
                    nextLevelBase -
                    currentLevelBase;

                int pointsRemaining =
                    nextLevelBase -
                    user.TotalPoints;

                double progressPercentage =
                    LevelService
                        .CalculateProgressPercentage(
                            user.TotalPoints
                        );

                int realSessionsCount =
                    await _context.EventCheckins
                        .CountAsync(
                            c => c.UserId == userId
                        );

                var recentCheckins =
                    await _context.EventCheckins
                        .Where(
                            c => c.UserId == userId
                        )
                        .OrderByDescending(
                            c => c.CheckedInAt
                        )
                        .Take(10)
                        .Select(
                            c =>
                                new UserCheckinHistoryDto
                                {
                                    EventTitle =
                                        c.Event != null
                                            ? c.Event.Title
                                            : "Мероприятие",

                                    CheckedInAt =
                                        c.CheckedInAt,

                                    PointsAwarded =
                                        c.PointsAwarded
                                }
                        )
                        .ToListAsync();

                return Ok(
                    new UserProfileUiDto
                    {
                        Username =
                            user.Username,

                        Photo =
                            user.Photo,

                        Level =
                            currentLevel,

                        TotalPoints =
                            user.TotalPoints,

                        PointsInCurrentLevel =
                            pointsInCurrentLevel,

                        PointsRequiredForNext =
                            pointsRequiredForNext,

                        PointsRemaining =
                            pointsRemaining,

                        ProgressPercentage =
                            progressPercentage,

                        SessionsCount =
                            realSessionsCount,

                        RecentCheckins =
                            recentCheckins
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка загрузки профиля: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка сервера"
                });
            }
        }


        // ============================================================
        // ADD POINTS BY USERNAME
        // ============================================================

        [HttpPost("add-points-by-username")]
        public async Task<IActionResult> AddPointsByUsername(
            [FromBody] AddPointsByUsernameDto dto)
        {
            if (string.IsNullOrWhiteSpace(
                    dto.Username))
            {
                return BadRequest(new
                {
                    message =
                        "Укажите имя пользователя"
                });
            }

            if (dto.Points <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Количество очков должно быть больше нуля"
                });
            }

            try
            {
                var normalizedUsername =
                    dto.Username
                        .Trim()
                        .ToLowerInvariant();

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.Username.ToLower() ==
                                normalizedUsername
                        );

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            $"Пользователь с ником '{dto.Username}' не найден"
                    });
                }

                int oldLevel =
                    user.Level;

                user.TotalPoints +=
                    dto.Points;

                int newLevel =
                    LevelService.CalculateLevel(
                        user.TotalPoints
                    );

                bool leveledUp =
                    newLevel > oldLevel;

                user.Level =
                    newLevel;

                await _context.SaveChangesAsync();

                return Ok(
                    new AddPointsResponseDto
                    {
                        Message =
                            leveledUp
                                ? $"Пользователь {user.Username} достиг {newLevel} уровня!"
                                : $"Пользователю {user.Username} начислено +{dto.Points} очков!",

                        AddedPoints =
                            dto.Points,

                        TotalPoints =
                            user.TotalPoints,

                        Level =
                            user.Level,

                        LeveledUp =
                            leveledUp,

                        NextLevelPoints =
                            LevelService
                                .GetRequiredPointsForLevel(
                                    user.Level + 1
                                ),

                        ProgressPercentage =
                            LevelService
                                .CalculateProgressPercentage(
                                    user.TotalPoints
                                )
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка начисления очков: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка при начислении очков"
                });
            }
        }


        // ============================================================
        // CONFIRM EMAIL
        // ============================================================

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new
                    {
                        message =
                            "Недействительная ссылка подтверждения"
                    });
                }

                var user =
                    await _context.Users
                        .FirstOrDefaultAsync(
                            u =>
                                u.EmailVerificationToken ==
                                token
                        );

                if (user == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Недействительная ссылка подтверждения"
                    });
                }

                if (user.IsEmailConfirmed)
                {
                    return Ok(new
                    {
                        message =
                            "Email уже был подтверждён"
                    });
                }

                if (!user.EmailVerificationTokenExpiresAt
                        .HasValue ||
                    user.EmailVerificationTokenExpiresAt.Value
                        < DateTime.UtcNow)
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();

                    return BadRequest(new
                    {
                        message =
                            "Срок действия ссылки истек. Аккаунт удален, пройдите регистрацию заново."
                    });
                }

                user.IsEmailConfirmed =
                    true;

                user.EmailVerificationToken =
                    null;

                user.EmailVerificationTokenExpiresAt =
                    null;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message =
                        "Email успешно подтверждён!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка подтверждения email: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка сервера при подтверждении email"
                });
            }
        }

        // ============================================================
        // FORGOT PASSWORD
        // ============================================================

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new { message = "Укажите email" });
                }

                var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

                if (user != null)
                {
                    string resetToken = Guid.NewGuid().ToString("N");
                    user.PasswordResetToken = resetToken;
                    user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
                    await _context.SaveChangesAsync();

                    await SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);
                }

                return Ok(new { message = "Если такой email зарегистрирован, инструкции по сбросу отправлены." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка forgot-password: {ex}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    return BadRequest(new { message = "Некорректные данные" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

                if (user == null || !user.PasswordResetTokenExpiresAt.HasValue || user.PasswordResetTokenExpiresAt.Value < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Срок действия ссылки истек или она недействительна." });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiresAt = null;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Пароль успешно изменен! Теперь вы можете войти." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка reset-password: {ex}");
                return StatusCode(500, new { message = "Ошибка сервера" });
            }
        }

        private async Task<bool> SendPasswordResetEmailAsync(string toEmail, string username, string token)
        {
            try
            {
                string resetLink = $"https://beerjaproject.ru/user/reset-password.html?token={Uri.EscapeDataString(token)}";

                var smtpServer = _configuration["SmtpSettings:Server"] ?? string.Empty;
                var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "465");
                var senderName = _configuration["SmtpSettings:SenderName"] ?? string.Empty;
                var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? string.Empty;
                var smtpPassword = _configuration["SmtpSettings:Password"] ?? string.Empty;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(username, toEmail));
                message.Subject = "Восстановление пароля в Beeja Project";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2>Привет, {username}!</h2>
                            <p>Был получен запрос на восстановление пароля для вашего аккаунта.</p>
                            <p><a href='{resetLink}' style='background-color: #705cff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Сбросить пароль</a></p>
                            <p>Или скопируйте эту ссылку в браузер:</p>
                            <p><a href='{resetLink}'>{resetLink}</a></p>
                            <p><small>Ссылка действительна 1 час. Если вы не запрашивали смену пароля, просто проигнорируйте это письмо.</small></p>
                        </div>"
                };

                using var client = new SmtpClient();
                bool useSsl = smtpPort == 465;

                await client.ConnectAsync(smtpServer, smtpPort, useSsl);
                await client.AuthenticateAsync(senderEmail, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки письма сброса: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // ADD POINTS
        // ============================================================

        [HttpPost("add-points")]
        [Authorize]
        public async Task<IActionResult> AddPoints(
            [FromBody] AddPointsDto dto)
        {
            if (dto.Points <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Количество очков должно быть больше нуля"
                });
            }

            try
            {
                var userIdClaim =
                    User.FindFirst(
                        ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(
                        userIdClaim,
                        out int userId))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Недействительный токен"
                    });
                }

                var user =
                    await _context.Users.FindAsync(
                        userId
                    );

                if (user == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Пользователь не найден"
                    });
                }

                int oldLevel =
                    user.Level;

                user.TotalPoints +=
                    dto.Points;

                int newLevel =
                    LevelService.CalculateLevel(
                        user.TotalPoints
                    );

                bool leveledUp =
                    newLevel > oldLevel;

                user.Level =
                    newLevel;

                await _context.SaveChangesAsync();

                return Ok(
                    new AddPointsResponseDto
                    {
                        Message =
                            leveledUp
                                ? $"Поздравляем! Вы достигли {newLevel} уровня!"
                                : $"Начислено +{dto.Points} очков!",

                        AddedPoints =
                            dto.Points,

                        TotalPoints =
                            user.TotalPoints,

                        Level =
                            user.Level,

                        LeveledUp =
                            leveledUp,

                        NextLevelPoints =
                            LevelService
                                .GetRequiredPointsForLevel(
                                    user.Level + 1
                                ),

                        ProgressPercentage =
                            LevelService
                                .CalculateProgressPercentage(
                                    user.TotalPoints
                                )
                    }
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка начисления очков: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Ошибка при начислении очков"
                });
            }
        } 

[HttpGet("/api/v1/users/search-players")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string? filter = null)
        {
            var query = _context.Users.AsQueryable();

            if (filter == "looking")
            {
                query = query.Where(u => u.IsLookingForTeam);
            }

            var players = await query
                .OrderByDescending(u => u.Level)
                .Select(u => new {
                    userId = u.UserId,
                    username = u.Username,
                    gamesPlayed = u.TotalPoints, 
                    level = u.Level,
                    isLookingForTeam = u.IsLookingForTeam,
                    photo = u.Photo
                })
                .ToListAsync();

            return Ok(players);
        }

        // ============================================================
        // UPDATE LOOKING STATUS
        // ============================================================

        [HttpPost("update-looking-status")]
        [Authorize]
        public async Task<IActionResult> UpdateLookingStatus(
            [FromBody] UpdateLookingStatusDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Недействительный токен" });
                }

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Пользователь не найден" });
                }

                user.IsLookingForTeam = dto.IsLookingForTeam;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Статус успешно обновлен", isLookingForTeam = user.IsLookingForTeam });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления статуса: {ex}");
                return StatusCode(500, new { message = "Ошибка при обновлении статуса" });
            }
        }

        // ============================================================
        // JWT
        // ============================================================

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.Username
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                )
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["Jwt:Key"]!
                    )
                );

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

            var token =
                new JwtSecurityToken(
                    _configuration["Jwt:Issuer"],
                    _configuration["Jwt:Audience"],
                    claims,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddDays(7),
                    creds
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }


        // ============================================================
        // EMAIL (REAL SMTP VIA MAILKIT)
        // ============================================================

        private async Task<bool> SendConfirmationEmailAsync(
            string toEmail,
            string username,
            string verificationToken)
        {
            try
            {
                string confirmationLink =
                    $"https://beerjaproject.ru/user/confim-email.html" +
                    $"?token={Uri.EscapeDataString(verificationToken)}";

                var smtpServer = _configuration["SmtpSettings:Server"] ?? string.Empty;
                var smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "465");
                var senderName = _configuration["SmtpSettings:SenderName"] ?? string.Empty;
                var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? string.Empty;
                var smtpPassword = _configuration["SmtpSettings:Password"] ?? string.Empty;
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(username, toEmail));
                message.Subject = "Подтверждение регистрации в Beeja Project";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2>Привет, {username}!</h2>
                            <p>Спасибо за регистрацию в Beeja Project. Для подтверждения адреса электронной почты перейдите по ссылке ниже:</p>
                            <p><a href='{confirmationLink}' style='background-color: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Подтвердить Email</a></p>
                            <p>Или скопируйте эту ссылку в браузер:</p>
                            <p><a href='{confirmationLink}'>{confirmationLink}</a></p>
                            <p><small>Ссылка действительна 24 часа.</small></p>
                        </div>"
                };

                using var client = new SmtpClient();
                bool useSsl = smtpPort == 465;

                await client.ConnectAsync(smtpServer, smtpPort, useSsl);
                await client.AuthenticateAsync(senderEmail, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка отправки реального email: {ex.Message}");
                return false;
            }
        }
    }

    // ================================================================
    // DTO
    // ================================================================

    public class UpdateUsernameDto
    {
        public string Username { get; set; }
            = string.Empty;
    }

    public class AddPointsByUsernameDto
    {
        public string Username { get; set; }
            = string.Empty;

        public int Points { get; set; }
    }

    public class YandexTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
            = string.Empty;
    }

    public class YandexUserInfo
    {
        [JsonPropertyName("default_email")]
        public string DefaultEmail { get; set; }
            = string.Empty;

        [JsonPropertyName("display_login")]
        public string DisplayLogin { get; set; }
            = string.Empty;
    }

    public class AddPointsDto
    {
        public int Points { get; set; }
    }

    public class AddPointsResponseDto
    {
        public string Message { get; set; }
            = string.Empty;

        public int AddedPoints { get; set; }

        public int TotalPoints { get; set; }

        public int Level { get; set; }

        public bool LeveledUp { get; set; }

        public int NextLevelPoints { get; set; }

        public double ProgressPercentage { get; set; }
    }

    public class UserProfileUiDto
    {
        public string Username { get; set; }
            = string.Empty;

        public string? Photo { get; set; }

        public int Level { get; set; }

        public int TotalPoints { get; set; }

        public int PointsInCurrentLevel { get; set; }

        public int PointsRequiredForNext { get; set; }

        public int PointsRemaining { get; set; }

        public double ProgressPercentage { get; set; }

        public int SessionsCount { get; set; }

        public List<UserCheckinHistoryDto> RecentCheckins { get; set; }
            = new();
    }

    public class UserCheckinHistoryDto
    {
        public string EventTitle { get; set; }
            = string.Empty;

        public DateTime CheckedInAt { get; set; }

        public int PointsAwarded { get; set; }
    }

    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty; // Исправлено с `:` на `=`
    }

    public class UpdateLookingStatusDto
    {
        public bool IsLookingForTeam { get; set; }
    }
}