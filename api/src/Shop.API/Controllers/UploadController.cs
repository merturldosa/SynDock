using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileStorageService _storage;
    private readonly IImageProcessingService _imageProcessor;
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> ProcessableExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadController(IFileStorageService storage, IImageProcessingService imageProcessor)
    {
        _storage = storage;
        _imageProcessor = imageProcessor;
    }

    [HttpPost("image")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "general", CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Please select a file." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "File size must be 10MB or less." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = "Unsupported file format. Allowed: jpg, png, gif, webp." });

        var url = await ProcessAndUploadAsync(file, ext, folder);
        return Ok(new { url });
    }

    [HttpPost("images")]
    public async Task<IActionResult> UploadImages(List<IFormFile> files, [FromQuery] string folder = "general", CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Please select a file." });

        if (files.Count > 10)
            return BadRequest(new { error = "Maximum 10 files can be uploaded at once." });

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length > MaxFileSize) continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext)) continue;

            var url = await ProcessAndUploadAsync(file, ext, folder);
            urls.Add(url);
        }

        return Ok(new { urls });
    }

    private async Task<string> ProcessAndUploadAsync(IFormFile file, string ext, string folder)
    {
        if (ProcessableExtensions.Contains(ext))
        {
            using var inputStream = file.OpenReadStream();
            using var processedStream = await _imageProcessor.ResizeAndConvertAsync(inputStream);
            var webpFileName = Path.ChangeExtension(file.FileName, ".webp");
            return await _storage.UploadAsync(processedStream, webpFileName, folder);
        }

        // GIF: upload without processing (preserve animation)
        using var stream = file.OpenReadStream();
        return await _storage.UploadAsync(stream, file.FileName, folder);
    }
}
