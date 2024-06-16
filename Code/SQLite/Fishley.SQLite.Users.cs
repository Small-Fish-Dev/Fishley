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
		public string CustomFishleyPrompt { get; set; }

		public DiscordUser(ulong userId)
		{
			UserId = userId;
		}
	}
	public static async Task<DiscordUser> GetOrCreateUser(ulong userId)
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

			return user;
		}
	}

	public static async Task UpdateOrCreateUser(DiscordUser user)
	{
		using (var db = new FishleyDbContext())
		{
			var foundUser = await db.Users.FindAsync(user.UserId);

			if (foundUser == null)
				db.Users.Add(user);
			else
				db.Entry(foundUser).CurrentValues.SetValues(user);

			await db.SaveChangesAsync();
		}
	}
}