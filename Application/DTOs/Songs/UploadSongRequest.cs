using System.ComponentModel.DataAnnotations;

namespace zmusic_backend.Application.DTOs.Songs
{
    public class UploadSongRequest
    {
        [Required]
        public IFormFile AudioFile { get; set; } = null!;
        
        public IFormFile? CoverFile { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Artist { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Album { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than 0")]
        public int Duration { get; set; }
    }
}