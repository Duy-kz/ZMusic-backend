using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using zmusic_backend.Application.DTOs.Songs;
using zmusic_backend.Application.Interfaces;
using zmusic_backend.Domain.Entities;

namespace zmusic_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class UploadController : ControllerBase
    {
        private readonly ISongService _songService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;

        // Allowed audio file extensions
        private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".flac", ".m4a", ".aac" };
        // Allowed image file extensions  
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        // Maximum file sizes (in bytes)
        private const long MaxAudioFileSize = 50 * 1024 * 1024; // 50MB
        private const long MaxImageFileSize = 5 * 1024 * 1024;  // 5MB

        public UploadController(ISongService songService, IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _songService = songService;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("songs")]
        [Authorize(Roles = "Admin")] // Only Admin can upload songs
        public async Task<IActionResult> UploadSong([FromForm] UploadSongRequest request)
        {
            try
            {
                // Validate audio file
                var audioValidation = ValidateAudioFile(request.AudioFile);
                if (!audioValidation.IsValid)
                {
                    return BadRequest(new { success = false, message = audioValidation.ErrorMessage });
                }

                // Validate cover file if provided
                if (request.CoverFile != null)
                {
                    var coverValidation = ValidateCoverFile(request.CoverFile);
                    if (!coverValidation.IsValid)
                    {
                        return BadRequest(new { success = false, message = coverValidation.ErrorMessage });
                    }
                }

                // Create directories if they don't exist
                var musicDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "music");
                var coversDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "covers");

                Directory.CreateDirectory(musicDir);
                Directory.CreateDirectory(coversDir);

                // Save audio file
                var audioFileName = await SaveAudioFile(request.AudioFile, musicDir);
                
                // Save cover file if exists
                string? coverFileName = null;
                if (request.CoverFile != null)
                {
                    coverFileName = await SaveCoverFile(request.CoverFile, coversDir);
                }

                // Get current user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { success = false, message = "Invalid user token" });
                }

                // Create song entity
                var song = new Song
                {
                    Title = request.Title.Trim(),
                    Artist = request.Artist.Trim(),
                    Album = request.Album?.Trim() ?? string.Empty,
                    Duration = request.Duration,
                    FilePath = $"/music/{audioFileName}",
                    CoverImagePath = coverFileName != null ? $"/covers/{coverFileName}" : null,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to database
                var createdSong = await _songService.CreateSongAsync(song);

                _logger.LogInformation("Song uploaded successfully: {Title} by {Artist} (ID: {Id})", 
                    createdSong.Title, createdSong.Artist, createdSong.Id);

                // Return success response
                var response = new SongResponse
                {
                    Id = createdSong.Id,
                    Title = createdSong.Title,
                    Artist = createdSong.Artist,
                    Album = createdSong.Album,
                    Duration = createdSong.Duration,
                    FilePath = createdSong.FilePath,
                    CoverImagePath = createdSong.CoverImagePath,
                    CreatedAt = createdSong.CreatedAt,
                    CreatedByUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown"
                };

                return Ok(new { success = true, message = "Song uploaded successfully", song = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading song");
                return StatusCode(500, new { success = false, message = "An error occurred while uploading the song" });
            }
        }

        private (bool IsValid, string? ErrorMessage) ValidateAudioFile(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return (false, "Audio file is required");
            }

            if (audioFile.Length > MaxAudioFileSize)
            {
                return (false, $"Audio file size cannot exceed {MaxAudioFileSize / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(audioFile.FileName).ToLowerInvariant();
            if (!_allowedAudioExtensions.Contains(extension))
            {
                return (false, $"Audio file must be one of: {string.Join(", ", _allowedAudioExtensions)}");
            }

            return (true, null);
        }

        private (bool IsValid, string? ErrorMessage) ValidateCoverFile(IFormFile coverFile)
        {
            if (coverFile.Length > MaxImageFileSize)
            {
                return (false, $"Cover image size cannot exceed {MaxImageFileSize / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(coverFile.FileName).ToLowerInvariant();
            if (!_allowedImageExtensions.Contains(extension))
            {
                return (false, $"Cover image must be one of: {string.Join(", ", _allowedImageExtensions)}");
            }

            return (true, null);
        }

        private async Task<string> SaveAudioFile(IFormFile audioFile, string musicDir)
        {
            var fileName = $"{DateTime.Now.Ticks}_{SanitizeFileName(audioFile.FileName)}";
            var filePath = Path.Combine(musicDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await audioFile.CopyToAsync(stream);

            return fileName;
        }

        private async Task<string> SaveCoverFile(IFormFile coverFile, string coversDir)
        {
            var fileName = $"{DateTime.Now.Ticks}_{SanitizeFileName(coverFile.FileName)}";
            var filePath = Path.Combine(coversDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await coverFile.CopyToAsync(stream);

            return fileName;
        }

        private static string SanitizeFileName(string fileName)
        {
            // Remove invalid characters from file name
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized;
        }
    }
}
