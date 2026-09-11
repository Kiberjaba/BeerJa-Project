using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BeejaServer.Data;
using BeejaServer.Models;

[Route("api/code")]
[ApiController]
public class CodeGeneratedController : ControllerBase
{
    private readonly AppDbContext _context;

    public CodeGeneratedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("generate")]
    [Authorize]
    public async Task<IActionResult> GenerateCode([FromBody] GenerateCodeDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int hostUserId))
        {
            return Unauthorized(new { message = "Недействительный токен" });
        }

    // 1. Создаем саму игровую сессию
        var session = new GameSession
        {
            EventId = dto.EventId,
            OrganiserUserId = hostUserId,
            CurrentStep = "lobby",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.GameSessions.Add(session);
        await _context.SaveChangesAsync(); // Получаем session.Id

    // 2. Генерируем уникальный 4-значный код комнаты
        string roomCode;
        var random = new Random();
        do
        {
            roomCode = random.Next(1000, 9999).ToString();
        } 
        while (await _context.GameRoomCodes.AnyAsync(r => r.RoomCode == roomCode));

    // 3. Сохраняем связку в новую таблицу game_room_codes
        var roomCodeRecord = new GameRoomCode
        {
            EventId = dto.EventId,
            GameId = session.Id,
            RoomCode = roomCode,
            CreatedAt = DateTime.Now
        };

        _context.GameRoomCodes.Add(roomCodeRecord);
        await _context.SaveChangesAsync();

    // 4. Отдаем на фронт
        return Ok(new 
        { 
            message = "Код успешно сгенерирован", 
            roomCode = roomCode, 
            gameId = session.Id 
        });
    }

    [HttpPost("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateCode([FromBody] ValidateCodeDto dto)
    {
        var roomCodeRecord = await _context.GameRoomCodes
            .FirstOrDefaultAsync(r => r.RoomCode == dto.GameCode);

        if (roomCodeRecord == null)
        {
            return BadRequest(new { message = "Неверный код комнаты" });
        } 

        var session = await _context.GameSessions
            .FirstOrDefaultAsync(s => s.Id == roomCodeRecord.GameId && s.IsActive);

        if (session == null)
        {
            return BadRequest(new { message = "Сессия игры не активна" });
        }

        return Ok(new 
        { 
            gameId = session.Id, 
            eventId = session.EventId,
            currentStep = session.CurrentStep 
        });
    }
}

public class GenerateCodeDto
{
    public int EventId { get; set; }
}

public class ValidateCodeDto
{
    public string GameCode { get; set; } = string.Empty;
}