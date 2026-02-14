using Core.Entities.Concrete;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework;

public class DevelopTurkeyContext : DbContext
{
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
        optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=DevelopTurkey;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;");
    }

	public DbSet<User> Users { get; set; }
	public DbSet<Topic> Topics { get; set; }
	public DbSet<Solution> Solutions { get; set; }
	public DbSet<Problem> Problems { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Log> Logs { get; set; }
	public DbSet<EmailVerification> EmailVerifications { get; set; }
	public DbSet<SolutionVote> SolutionVotes { get; set; }
}