using Microsoft.EntityFrameworkCore;
using zmusic_backend.Domain.Entities;

namespace zmusic_backend.Infrastructure.Data
{
    public class ZMusicDbContext : DbContext
    {
        public ZMusicDbContext(DbContextOptions<ZMusicDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Song> Songs { get; set; } = null!;
        public DbSet<Playlist> Playlists { get; set; } = null!;
        public DbSet<PlaylistSong> PlaylistSongs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Song configuration
            modelBuilder.Entity<Song>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.CreatedBy)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Playlist configuration
            modelBuilder.Entity<Playlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany(e => e.Playlists)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PlaylistSong configuration (Many-to-Many)
            modelBuilder.Entity<PlaylistSong>(entity =>
            {
                entity.HasKey(e => new { e.PlaylistId, e.SongId });
                
                entity.HasOne(e => e.Playlist)
                      .WithMany(e => e.PlaylistSongs)
                      .HasForeignKey(e => e.PlaylistId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasOne(e => e.Song)
                      .WithMany(e => e.PlaylistSongs)
                      .HasForeignKey(e => e.SongId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}