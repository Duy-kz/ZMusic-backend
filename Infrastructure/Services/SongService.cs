using Microsoft.EntityFrameworkCore;
using zmusic_backend.Application.Interfaces;
using zmusic_backend.Domain.Entities;
using zmusic_backend.Infrastructure.Data;

namespace zmusic_backend.Infrastructure.Services
{
    public class SongService : ISongService
    {
        private readonly ZMusicDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SongService(ZMusicDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<Song>> GetAllSongsAsync()
        {
            var songs = await _context.Songs
                .Include(s => s.CreatedBy)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            // Convert relative paths to full URLs
            return songs.Select(s => ConvertToFullUrls(s));
        }

        public async Task<Song?> GetSongByIdAsync(int id)
        {
            var song = await _context.Songs
                .Include(s => s.CreatedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            return song != null ? ConvertToFullUrls(song) : null;
        }

        public async Task<Song> CreateSongAsync(Song song)
        {
            _context.Songs.Add(song);
            await _context.SaveChangesAsync();
            return ConvertToFullUrls(song);
        }

        public async Task<bool> DeleteSongAsync(int id)
        {
            var song = await _context.Songs.FindAsync(id);
            if (song == null)
                return false;

            _context.Songs.Remove(song);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Song>> SearchSongsAsync(string searchTerm)
        {
            var songs = await _context.Songs
                .Include(s => s.CreatedBy)
                .Where(s => s.Title.Contains(searchTerm) || 
                           s.Artist.Contains(searchTerm) || 
                           s.Album.Contains(searchTerm))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return songs.Select(s => ConvertToFullUrls(s));
        }

        private Song ConvertToFullUrls(Song song)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}";
                
                // Convert relative paths to full URLs
                if (!string.IsNullOrEmpty(song.FilePath) && song.FilePath.StartsWith("/"))
                {
                    song.FilePath = $"{baseUrl}{song.FilePath}";
                }
                
                if (!string.IsNullOrEmpty(song.CoverImagePath) && song.CoverImagePath.StartsWith("/"))
                {
                    song.CoverImagePath = $"{baseUrl}{song.CoverImagePath}";
                }
            }
            
            return song;
        }
    }
}