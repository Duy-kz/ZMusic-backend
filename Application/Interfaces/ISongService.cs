using zmusic_backend.Domain.Entities;

namespace zmusic_backend.Application.Interfaces
{
    public interface ISongService
    {
        Task<IEnumerable<Song>> GetAllSongsAsync();
        Task<Song?> GetSongByIdAsync(int id);
        Task<Song> CreateSongAsync(Song song);
        Task<bool> DeleteSongAsync(int id);
        Task<IEnumerable<Song>> SearchSongsAsync(string searchTerm);
    }
}