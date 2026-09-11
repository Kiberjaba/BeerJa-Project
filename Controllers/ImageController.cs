using BeejaServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeejaServer.Controllers;

[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly YandexStorageService _storage;

    public ImagesController(YandexStorageService storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Файл не выбран"
            });
        }

        try
        {
            var url = await _storage.UploadAsync(
                file,
                "images",
                ct);

            return Ok(new
            {
                url
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}