namespace Fishley;

public partial class Fishley
{
	public class FishleyDbContext : DbContext
	{
		public DbSet<DiscordUser> Users { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=database.db");
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<DiscordUser>()
				.Property(u => u.Banned)
				.HasDefaultValue(false);
		}
	}
}