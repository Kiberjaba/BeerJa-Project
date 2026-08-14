namespace BeejaServer.DTOs
{
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        // Добавлено поле Photo:
        public string? Photo { get; set; }

        public bool IsEmailConfirmed { get; set; }
        public int TotalPoints { get; set; }
        public int Level { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}