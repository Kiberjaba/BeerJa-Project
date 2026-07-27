using System.ComponentModel.DataAnnotations;

namespace BeejaServer.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Укажите Email или имя пользователя")]
        public string LoginOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }
}