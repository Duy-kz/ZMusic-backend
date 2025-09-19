using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using zmusic_backend.Application.Interfaces;
using zmusic_backend.Domain.Entities;
using System.Security.Claims;

namespace zmusic_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SongsController : ControllerBase
    {
        private readonly ISongService _songService;

        public SongsController(ISongService songService)
        {
            _songService = songService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Song>>> GetAllSongs()
        {
            var songs = await _songService.GetAllSongsAsync();
            return Ok(songs);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Song>> GetSong(int id)
        {
            var song = await _songService.GetSongByIdAsync(id);
            if (song == null)
            {
                return NotFound();
            }
            return Ok(song);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Song>> CreateSong([FromBody] CreateSongRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var song = new Song
            {
                Title = request.Title,
                Artist = request.Artist,
                Album = request.Album,
                Duration = request.Duration,
                FilePath = request.FilePath,
                CoverImagePath = request.CoverImagePath,
                CreatedByUserId = userId
            };

            var createdSong = await _songService.CreateSongAsync(song);
            return CreatedAtAction(nameof(GetSong), new { id = createdSong.Id }, createdSong);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSong(int id)
        {
            var result = await _songService.DeleteSongAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Song>>> SearchSongs([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest("Search term is required");
            }

            var songs = await _songService.SearchSongsAsync(q);
            return Ok(songs);
        }
    }

    public class CreateSongRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
    }
}