using Microsoft.EntityFrameworkCore;
using Opilochka.Data;
using Opilochka.Data.Answers;
using Opilochka.Data.Lessons;
using Opilochka.Data.StudyGroups;
using Opilochka.Data.Tasks;
using Opilochka.Data.Users;
using Task = Opilochka.Data.Tasks.Task;

namespace Opilochka.API;

public class OpilochkaDbContext : DbContext
{
    private readonly IConfiguration _configuration;

    public OpilochkaDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<User> Users { get; set; }

    public DbSet<StudyGroup> StudyGroups { get; set; }

    public DbSet<Lesson> Lessons { get; set; }

    public DbSet<Task> Tasks { get; set; }

    public DbSet<Answer> Answers { get; set; }

    public DbSet<ResultCheck> ResultChecks { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));
    }
}