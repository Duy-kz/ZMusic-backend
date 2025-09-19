namespace zmusic_backend.Domain.Entities
{
    public class PlaylistSong
    {
        public int PlaylistId { get; set; }
        public virtual Playlist Playlist { get; set; } = null!;
        
        public int SongId { get; set; }
        public virtual Song Song { get; set; } = null!;
        
        public int Order { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}