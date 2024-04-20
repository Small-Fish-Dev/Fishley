namespace Fishley;

public partial class Fishley
{
	public class Fish
	{
		[Key]
		public int Id { get; set; }
		public string CommonName { get; set; }
		public string PageName { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
		public string ImageLink { get; set; }
		public DateTime LastSeen { get; set; }
		public string Rarity { get; set; }

		public Fish( int id )
		{
			Id = id;
		}

		public void Copy( FishData fish )
		{
			CommonName = fish.CommonName;
			PageName = fish.PageName;
			WikiPage = fish.WikiPage;
			WikiInfoPage = fish.WikiInfoPage;
			MonthlyViews = fish.MonthlyViews;
			ImageLink = fish.ImageLink;
			LastSeen = fish.LastSeen;
			Rarity = fish.Rarity;
		}
	}

	public class FishData
	{
		public int Id { get; set; }
		public string CommonName { get; set; }
		public string PageName { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
		public string ImageLink { get; set; }
		public DateTime LastSeen { get; set; }
		public string Rarity { get; set; }

		public FishData( int id )
		{
			Id = id;
		}

		public FishData( Fish fish )
		{
			Id = fish.Id;
			CommonName = fish.CommonName;
			PageName = fish.PageName;
			WikiPage = fish.WikiPage;
			WikiInfoPage = fish.WikiInfoPage;
			MonthlyViews = fish.MonthlyViews;
			ImageLink = fish.ImageLink;
			LastSeen = fish.LastSeen;
			Rarity = fish.Rarity;
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