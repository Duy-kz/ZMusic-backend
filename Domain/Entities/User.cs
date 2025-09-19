using System.ComponentModel.DataAnnotations;

namespace zmusic_backend.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public string Role { get; set; } = "User"; // User, Admin
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
    }
}