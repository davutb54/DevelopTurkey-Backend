using Core.Entities.Concrete;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete.EntityFramework;

public class DevelopTurkeyContext : DbContext
{
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSqlServer(@"Server=89.252.187.226\MSSQLSERVER2019;Database=turki121_developturkey;user=turki121_dbadmin;password=yM$y*eI51qRl7yoe;TrustServerCertificate=True;");
	}

	public DbSet<User> Users { get; set; }
	public DbSet<Topic> Topics { get; set; }
	public DbSet<Solution> Solutions { get; set; }
	public DbSet<Problem> Problems { get; set; }
	public DbSet<Comment> Comments { get; set; }
	public DbSet<Log> Logs { get; set; }
	public DbSet<EmailVerification> EmailVerifications { get; set; }
}