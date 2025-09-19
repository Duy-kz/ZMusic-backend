using zmusic_backend.Domain.Entities;

namespace zmusic_backend.Application.Interfaces
{
    public interface IPlaylistService
    {
        Task<IEnumerable<Playlist>> GetUserPlaylistsAsync(int userId);
        Task<Playlist?> GetPlaylistByIdAsync(int id);
        Task<Playlist> CreatePlaylistAsync(Playlist playlist);
        Task<bool> AddSongToPlaylistAsync(int playlistId, int songId);
        Task<bool> RemoveSongFromPlaylistAsync(int playlistId, int songId);
        Task<bool> DeletePlaylistAsync(int id);
    }
}