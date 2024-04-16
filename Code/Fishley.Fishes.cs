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
	public static Dictionary<string, string> FishRarityThumbnails { get; set; } = new() // Will make it better
	{
		{ "F-", "https://i.imgur.com/DueCNx8.png" },
		{ "F", "https://i.imgur.com/eaasV91.png" },
		{ "F+", "https://i.imgur.com/F6B8QQb.png" },
		{ "E-", "https://i.imgur.com/epUwAHr.png" },
		{ "E", "https://i.imgur.com/DhFsRcr.png" },
		{ "E+", "https://i.imgur.com/Y6plinP.png" },
		{ "D-", "https://i.imgur.com/JB0DNWj.png" },
		{ "D", "https://i.imgur.com/uKhojQo.png" },
		{ "D+", "https://i.imgur.com/zF8mXIt.png" },
		{ "C-", "https://i.imgur.com/uKiJt8V.png" },
		{ "C", "https://i.imgur.com/56qj9pX.png" },
		{ "C+", "https://i.imgur.com/KLsqazN.png" },
		{ "B-", "https://i.imgur.com/SwzHOrR.png" },
		{ "B", "https://i.imgur.com/EZKH8Fn.png" },
		{ "B+", "https://i.imgur.com/vV27igO.png" },
		{ "A-", "https://i.imgur.com/VkqngLL.png" },
		{ "A", "https://i.imgur.com/Cpnz0jC.png" },
		{ "A+", "https://i.imgur.com/mhhf5e9.png" },
		{ "S-", "https://i.imgur.com/UKMV4gu.png" },
		{ "S", "https://i.imgur.com/QWdejAx.png" },
		{ "S+", "https://i.imgur.com/Gb8dN1p.png" },
	};
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