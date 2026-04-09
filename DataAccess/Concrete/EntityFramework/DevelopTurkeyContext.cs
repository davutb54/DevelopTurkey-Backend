using Core.Entities.Concrete;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Concrete.EntityFramework;

public class DevelopTurkeyContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{env}.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProblemTopic>()
            .HasKey(pt => new { pt.ProblemId, pt.TopicId });

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Solution> Solutions { get; set; }
    public DbSet<Problem> Problems { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<SolutionVote> SolutionVotes { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Institution> Institutions { get; set; }
    public DbSet<ProblemTopic> ProblemTopics { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
}