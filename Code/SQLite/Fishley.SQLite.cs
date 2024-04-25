namespace Fishley;

public partial class Fishley
{
	public class FishleyDbContext : DbContext
	{
		public DbSet<DiscordUser> Users { get; set; }
		public DbSet<Fish> Fishes { get; set; }
		public DbSet<Transaction> Transactions { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=database.db");
		}
	}
}