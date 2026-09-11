using Microsoft.AspNetCore.Mvc;

namespace BeejaServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(IConfiguration configuration, ILogger<ContactsController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public class ContactMessageDto
        {
            public string Name { get; set; } = string.Empty;
            public string Contact { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ContactMessageDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
                {
                    return BadRequest(new { message = "Сообщение не может быть пустым." });
                }

                var botToken = _configuration["TelegramSettings:BotToken"];
                var chatId = _configuration["TelegramSettings:ChatId"];

                if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
                {
                    _logger.LogError("Telegram settings (BotToken or ChatId) are not configured.");
                    return StatusCode(500, new { message = "Бот не настроен на сервере." });
                }

                string text = "📩 **Новое сообщение с сайта (Контакты)!**\n\n" +
                              $"👤 **Имя:** {(string.IsNullOrEmpty(dto.Name) ? "Не указано" : dto.Name)}\n" +
                              $"📞 **Контакт:** {(string.IsNullOrEmpty(dto.Contact) ? "Не указан" : dto.Contact)}\n" +
                              $"💬 **Сообщение:**\n{dto.Message}";

                using var httpClient = new HttpClient();
                string url = $"https://api.telegram.org/bot{botToken}/sendMessage";

                var payload = new
                {
                    chat_id = chatId,
                    text = text,
                    parse_mode = "Markdown"
                };

                var response = await httpClient.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Telegram API error: {errorContent}");
                    return StatusCode(500, new { message = "Не удалось отправить сообщение в Telegram." });
                }

                return Ok(new { message = "Сообщение успешно отправлено!" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in SendMessage: {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return StatusCode(500, new { message = "Внутренняя ошибка сервера при отправке." });
            }
        }
    }
}