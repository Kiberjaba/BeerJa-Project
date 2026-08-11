namespace BeejaServer.Contracts;

public enum GamePhase
{
    Lobby,
    RoundIntro,
    QuestionOpen,
    QuestionClosed,
    RevealQueue,
    Leaderboard,
    Final,
    Report
}

public enum QuestionKind
{
    Single,
    Multiple,
    Text,
    Image
}

public sealed record PlayerSummaryDto(
    int UserId,
    string DisplayName,
    int Level,
    int ProfileExperience);

public sealed record TeamMemberDto(
    int UserId,
    string DisplayName,
    bool IsConnected,
    bool IsCaptain);

public sealed record TeamSummaryDto(
    int TeamId,
    string Name,
    int ConnectedPlayers,
    int Capacity,
    int? CaptainUserId,
    IReadOnlyList<TeamMemberDto> Members);

public sealed record QuestionDto(
    int QuestionId,
    string Code,
    QuestionKind Kind,
    string Title,
    string Prompt,
    string? ImageUrl,
    string? ImageAlt,
    IReadOnlyList<string> Options,
    int RequiredAnswers,
    int DurationSeconds,
    int Points);

public sealed record LeaderboardEntryDto(
    int Rank,
    int TeamId,
    string TeamName,
    int CorrectAnswers,
    int Score,
    long CorrectAnswerTimeMilliseconds);

public sealed record LiveGameStateDto(
    int GameId,
    string RoomCode,
    string Title,
    GamePhase Phase,
    int RoundIndex,
    int QuestionIndex,
    int RevealIndex,
    DateTimeOffset? EndsAt,
    bool IsPaused,
    int AnsweredTeams,
    PlayerSummaryDto CurrentUser,
    TeamSummaryDto CurrentTeam,
    QuestionDto? CurrentQuestion,
    IReadOnlyList<LeaderboardEntryDto> Leaderboard,
    long Version);

public sealed record CaptainVoteRequest(int CandidateUserId, long ExpectedVersion);

public sealed record AnswerSubmissionRequest(
    int QuestionId,
    IReadOnlyList<string> Values,
    long ExpectedVersion);

public sealed record HostCommandRequest(
    string Command,
    long ExpectedVersion);

public sealed record FeedbackRequest(
    string Scope,
    int? RoundIndex,
    int? Rating,
    bool Skipped);
