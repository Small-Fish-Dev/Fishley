namespace Fishley;

public partial class Fishley
{
	public class FishleyDbContext : DbContext
	{
		public DbSet<DiscordUser> Users { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=discord_users.db");
		}
	}

	public class DiscordUser
	{
		[Key]
		public ulong UserId { get; set; }
		public int Warnings { get; set; }
		public DateTime LastWarn { get; set; }
		public decimal Money { get; set; }
		public DateTime LastFish { get; set; }

		public DiscordUser( ulong userId )
		{
			UserId = userId;
		}

		public void Copy( User user )
		{
			Warnings = user.Warnings;
			LastWarn = user.LastWarn;
			Money = user.Money;
			LastFish = user.LastFish;
		}
	}

	public class User
	{
		public ulong UserId { get; set; }
		public int Warnings { get; set; }
		public DateTime LastWarn { get; set; }
		public decimal Money { get; set; }
		public DateTime LastFish { get; set; }

		public User( ulong userId )
		{
			UserId = userId;
		}

		public User( DiscordUser user )
		{
			UserId = user.UserId;
			Warnings = user.Warnings;
			LastWarn = user.LastWarn;
			Money = user.Money;
			LastFish = user.LastFish;
		}
	}
	
	public static async Task<User> GetOrCreateUser( ulong userId )
	{
		using (var db = new FishleyDbContext())
		{
			var user = await db.Users.FindAsync(userId);

			if (user == null)
			{
				user = new DiscordUser(userId);
				db.Users.Add(user);
				await db.SaveChangesAsync();
			}

			return new User( user );
		}
	}

	public static async Task UpdateUser( User user )
	{
		using (var db = new FishleyDbContext())
		{
			var foundUser = await db.Users.FindAsync( user.UserId );

			if ( foundUser == null )
				db.Users.Add( new DiscordUser( user.UserId ) );
			else
				foundUser.Copy( user );

			await db.SaveChangesAsync();
		}
	}
}