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

		public Fish(int pageId)
		{
			PageId = pageId;
		}

		public Fish(FishData fish)
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

		public void Copy(FishData fish)
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

		public FishData(int pageId)
		{
			PageId = pageId;
		}

		public FishData(Fish fish)
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

	/// <summary>
	/// Get a random fish of that rarity, null if you're fine with any fish (Automatically updates the last seen)
	/// </summary>
	/// <param name="rarity"></param>
	/// <returns></returns>
	public static async Task<FishData> GetRandomFishFromRarity(string rarity = null)
	{
		using (var db = new FishleyDbContext())
		{
			var fishes = db.Fishes;

			var foundFishes = fishes.AsAsyncEnumerable();

			if (rarity != null && FishRarities.ContainsKey(rarity))
				foundFishes = foundFishes.Where(x => x.Rarity == rarity);

			int count = await foundFishes.CountAsync();

			if (count == 0)
				return null;

			int index = new Random((int)DateTime.UtcNow.Ticks).Next(count);
			var randomFish = await foundFishes.OrderBy(x => x.PageId)
				.Skip(index)
				.FirstOrDefaultAsync();

			return new FishData(randomFish);
		}
	}

	/// <summary>
	/// Get a fish by id
	/// </summary>
	/// <param name="fishId"></param>
	/// <returns></returns>
	public static async Task<FishData> GetFish(int fishId)
	{
		using (var db = new FishleyDbContext())
		{
			var fish = await db.Fishes.FindAsync(fishId);

			if (fish == null) return null;

			return new FishData(fish);
		}
	}

	/// <summary>
	/// Remove a fish by id
	/// </summary>
	/// <param name="fishId"></param>
	/// <returns></returns>
	public static async Task<bool> RemoveFish(int fishId)
	{
		using (var db = new FishleyDbContext())
		{
			var fish = await db.Fishes.FindAsync(fishId);

			if (fish == null) return false;

			db.Fishes.Remove(fish);

			await db.SaveChangesAsync();
			return true;
		}
	}

	public static async Task UpdateOrCreateFish(FishData fish)
	{
		using (var db = new FishleyDbContext())
		{
			var foundFish = await db.Fishes.FindAsync(fish.PageId);

			if (foundFish == null)
				db.Fishes.Add(new Fish(fish));
			else
				foundFish.Copy(fish);

			await db.SaveChangesAsync();
		}
	}
}