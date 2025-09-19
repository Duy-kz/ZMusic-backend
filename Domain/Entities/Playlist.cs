using System.ComponentModel.DataAnnotations;

namespace zmusic_backend.Domain.Entities
{
    public class Playlist
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        
        // Navigation properties
        public virtual ICollection<PlaylistSong> PlaylistSongs { get; set; } = new List<PlaylistSong>();
    }
}