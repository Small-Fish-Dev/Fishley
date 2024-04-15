public partial class Fishley
{
	public static List<Fish> AllFish { get; set; }
	public struct Fish
	{
		public string Name { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
		[JsonIgnore]
		public string Rarity => FishRarities[GetFishRarity( MonthlyViews )];
	}

	public static List<string> FishRarities { get; set; } = new() {"F-", "F", "F+", "E-", "E", "E+", "D-", "D", "D+", "C-", "C", "C+", "B-", "B", "B+", "A-", "A", "A+", "S-", "S", "S+" };
	private static List<int> _fishPercentileGroups { get; set; } = new();

	public static async Task LoadFishes()
	{
		var jsonFile = await File.ReadAllTextAsync( @"/home/ubre/Desktop/Fishley/fishes.json" );
		AllFish = System.Text.Json.JsonSerializer.Deserialize<List<Fish>>( jsonFile );
		InitializeFishRarities( 100f / FishRarities.Count() );
	}

	private static void InitializeFishRarities( float percentageSize )
	{
		var totalFishes = AllFish.Count;
		var groupSize = totalFishes * (percentageSize / 100f);

		int effectiveGroupSize = (int)Math.Ceiling(groupSize);

		var fishGroups = AllFish.OrderBy(x => x.MonthlyViews)
			.Select((fish, index) => new { Fish = fish, Index = index })
			.GroupBy(x => x.Index / effectiveGroupSize, x => x.Fish);

		_fishPercentileGroups.Clear();

		foreach (var fishGroup in fishGroups)
			_fishPercentileGroups.Add(fishGroup.Max(x => x.MonthlyViews));

		foreach (var fish in _fishPercentileGroups)
			Console.WriteLine(fish);
	}

	public static int GetFishRarity( float monthlyViews )
	{
		var currentGroup = 0;

		foreach ( var group in _fishPercentileGroups )
		{
			if ( monthlyViews <= group )
				return currentGroup;

			currentGroup++;
		}

		return _fishPercentileGroups.Count();
	}
}