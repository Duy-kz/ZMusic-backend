using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using zmusic_backend.Application.Interfaces;
using zmusic_backend.Domain.Entities;
using zmusic_backend.Infrastructure.Data;
using zmusic_backend.Infrastructure.Services;

namespace zmusic_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            
            // Add HttpContextAccessor for accessing request information
            builder.Services.AddHttpContextAccessor();
            
            // Configure Swagger/OpenAPI with JWT support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "ZMusic API", 
                    Version = "v1",
                    Description = "ZMusic - Music Streaming Platform API"
                });
                
                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });
            });

            // Database Configuration
            builder.Services.AddDbContext<ZMusicDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Application Services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISongService, SongService>();

            // JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                    };
                });

            // CORS Configuration - Update to allow all origins for development
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp",
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:5173", "https://localhost:3000", 
                                          "http://localhost:7800", "https://localhost:7800")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });

            var app = builder.Build();

            // Test Database Connection & Seed Initial Data
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ZMusicDbContext>();
                
                // Test connection
                if (dbContext.Database.CanConnect())
                {
                    Console.WriteLine("||||||||||||||||| Database connected successfully |||||||||||||||||");
                    
                    // Seed initial users if they don't exist
                    SeedInitialUsers(dbContext);
                    
                    // Seed sample songs for testing
                    SeedSampleSongs(dbContext);
                }
                else
                {
                    Console.WriteLine("XXXXXX Failed to connect to Database!");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ZMusic API v1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at apps root
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowReactApp");

            // Enable static files serving (for uploaded music and cover files)
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Add explicit endpoints for serving files with proper CORS headers
            app.MapGet("/music/{filename}", async (string filename, IWebHostEnvironment env) =>
            {
                var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "music", filename);
                if (!File.Exists(filePath))
                {
                    return Results.NotFound();
                }
                
                var contentType = GetContentType(filename);
                return Results.File(filePath, contentType);
            }).WithOpenApi();

            app.MapGet("/covers/{filename}", async (string filename, IWebHostEnvironment env) =>
            {
                var filePath = Path.Combine(env.WebRootPath ?? "wwwroot", "covers", filename);
                if (!File.Exists(filePath))
                {
                    return Results.NotFound();
                }
                
                var contentType = GetContentType(filename);
                return Results.File(filePath, contentType);
            }).WithOpenApi();

            app.Run();
        }

        private static void SeedInitialUsers(ZMusicDbContext context)
        {
            try
            {
                // Check if admin user exists
                if (!context.Users.Any(u => u.Email == "admin@zmusic.com"))
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@zmusic.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    context.Users.Add(adminUser);
                    Console.WriteLine("✅ Admin user created: admin@zmusic.com / admin123");
                }

                // Check if regular user exists
                if (!context.Users.Any(u => u.Email == "user1@zmusic.com"))
                {
                    var regularUser = new User
                    {
                        Username = "user1",
                        Email = "user1@zmusic.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
                        Role = "User",
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    context.Users.Add(regularUser);
                    Console.WriteLine("✅ Regular user created: user1@zmusic.com / user123");
                }

                context.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding users: {ex.Message}");
            }
        }

        private static void SeedSampleSongs(ZMusicDbContext context)
        {
            try
            {
                // Only seed if no songs exist
                if (!context.Songs.Any())
                {
                    var adminUser = context.Users.FirstOrDefault(u => u.Role == "Admin");
                    if (adminUser != null)
                    {
                        var sampleSongs = new List<Song>
                        {
                            new Song
                            {
                                Title = "Shape of You",
                                Artist = "Ed Sheeran",
                                Album = "÷ (Divide)",
                                Duration = 233,
                                FilePath = "/music/sample-song.mp3", // You can replace with actual files
                                CoverImagePath = "/covers/sample-cover.jpg",
                                CreatedByUserId = adminUser.Id,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Song
                            {
                                Title = "Blinding Lights",
                                Artist = "The Weeknd",
                                Album = "After Hours",
                                Duration = 200,
                                FilePath = "/music/sample-song.mp3",
                                CoverImagePath = "/covers/sample-cover.jpg",
                                CreatedByUserId = adminUser.Id,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Song
                            {
                                Title = "Bohemian Rhapsody",
                                Artist = "Queen",
                                Album = "A Night at the Opera",
                                Duration = 355,
                                FilePath = "/music/sample-song.mp3",
                                CoverImagePath = "/covers/sample-cover.jpg",
                                CreatedByUserId = adminUser.Id,
                                CreatedAt = DateTime.UtcNow
                            }
                        };

                        context.Songs.AddRange(sampleSongs);
                        context.SaveChanges();
                        Console.WriteLine($"✅ {sampleSongs.Count} sample songs created");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding sample songs: {ex.Message}");
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".flac" => "audio/flac",
                ".m4a" => "audio/mp4",
                ".aac" => "audio/aac",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }
}
