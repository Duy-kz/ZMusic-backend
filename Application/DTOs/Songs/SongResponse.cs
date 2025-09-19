namespace zmusic_backend.Application.DTOs.Songs
{
    public class SongResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? CoverImagePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
    }
}