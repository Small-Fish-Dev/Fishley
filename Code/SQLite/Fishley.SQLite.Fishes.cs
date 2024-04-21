namespace Fishley;

public partial class Fishley
{
	public class Fish
	{
		[Key]
		public int PageId { get; set; }
		public string CommonName { get; set; }
		public string PageName { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
		public string ImageLink { get; set; }
		public DateTime LastSeen { get; set; }
		public string Rarity { get; set; }

		public Fish( int pageId )
		{
			PageId = pageId;
		}
		
		public Fish( FishData fish )
		{
			PageId = fish.PageId;
			CommonName = fish.CommonName;
			PageName = fish.PageName;
			WikiPage = fish.WikiPage;
			WikiInfoPage = fish.WikiInfoPage;
			MonthlyViews = fish.MonthlyViews;
			ImageLink = fish.ImageLink;
			LastSeen = fish.LastSeen;
			Rarity = fish.Rarity;
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
		public int PageId { get; set; }
		public string CommonName { get; set; }
		public string PageName { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
		public string ImageLink { get; set; }
		public DateTime LastSeen { get; set; }
		public string Rarity { get; set; }

		public FishData( int pageId )
		{
			PageId = pageId;
		}

		public FishData( Fish fish )
		{
			PageId = fish.PageId;
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

	public static async Task UpdateOrCreateFish( FishData fish )
	{
		using (var db = new FishleyDbContext())
		{
			var foundFish = await db.Fishes.FindAsync( fish.PageId );

			if ( foundFish == null )
				db.Fishes.Add( new Fish( fish ) );
			else
				foundFish.Copy( fish );

			await db.SaveChangesAsync();
		}
	}
}