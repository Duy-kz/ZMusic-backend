using System.ComponentModel.DataAnnotations;

namespace zmusic_backend.Domain.Entities
{
    public class Song
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Artist { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Album { get; set; } = string.Empty;
        
        public int Duration { get; set; } // seconds
        
        [Required]
        public string FilePath { get; set; } = string.Empty;
        
        public string? CoverImagePath { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int CreatedByUserId { get; set; }
        public virtual User CreatedBy { get; set; } = null!;
        
        // Navigation properties
        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
    }
}