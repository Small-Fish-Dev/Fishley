namespace Fishley;

public partial class Fishley
{
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
		
		public DiscordUser( DiscordUserData user )
		{
			UserId = user.UserId;
			Warnings = user.Warnings;
			LastWarn = user.LastWarn;
			Money = user.Money;
			LastFish = user.LastFish;
		}

		public void Copy( DiscordUserData user )
		{
			Warnings = user.Warnings;
			LastWarn = user.LastWarn;
			Money = user.Money;
			LastFish = user.LastFish;
		}
	}

	public class DiscordUserData
	{
		public ulong UserId { get; set; }
		public int Warnings { get; set; }
		public DateTime LastWarn { get; set; }
		public decimal Money { get; set; }
		public DateTime LastFish { get; set; }

		public DiscordUserData( ulong userId )
		{
			UserId = userId;
		}

		public DiscordUserData( DiscordUser user )
		{
			UserId = user.UserId;
			Warnings = user.Warnings;
			LastWarn = user.LastWarn;
			Money = user.Money;
			LastFish = user.LastFish;
		}
	}
	
	public static async Task<DiscordUserData> GetOrCreateUser( ulong userId )
	{
		using (var db = new FishleyDbContext())
		{
			var user = await db.Users.FindAsync(userId);

			if (user == null)
			{
				user = new DiscordUser( userId );
				db.Users.Add(user);
				await db.SaveChangesAsync();
			}

			return new DiscordUserData( user );
		}
	}

	public static async Task UpdateUser( DiscordUserData user )
	{
		using (var db = new FishleyDbContext())
		{
			var foundUser = await db.Users.FindAsync( user.UserId );

			if ( foundUser == null )
				db.Users.Add( new DiscordUser( user ) );
			else
				foundUser.Copy( user );

			await db.SaveChangesAsync();
		}
	}
}