using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("game_sessions")]
public class GameSession
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("organiser_user_id")]
    public int OrganiserUserId { get; set; }

    [Column("current_step")]
    [MaxLength(50)]
    public string CurrentStep { get; set; } = "lobby";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("game_type")]
    public int GameType { get; set; } = 2;
}

[Table("live_step_responses")]
public class LiveStepResponse
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("game_id")]
    public int GameId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("step_identifier")]
    [Required]
    [MaxLength(50)]
    public string StepIdentifier { get; set; } = string.Empty;

    [Column("answer_data")]
    [Required]
    public string AnswerData { get; set; } = string.Empty;

    [Column("is_correct")]
    public bool? IsCorrect { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[Table("game_questions")]
public class GameQuestion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("question_text")]
    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Column("options")]
    [Required]
    public string OptionsJson { get; set; } = string.Empty;

    [Column("correct_option")]
    public int? CorrectOption { get; set; }

    [Column("step_order")]
    public int StepOrder { get; set; }

    [Column("round_number")]
    public int RoundNumber { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("timer_seconds")]
    public int TimerSeconds { get; set; } = 15;

    [Column("audio_url")]
    public string? AudioUrl { get; set; }

    [Column("video_url")]
    public string? VideoUrl { get; set; }
}