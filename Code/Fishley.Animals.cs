namespace Fishley;

public partial class Fishley
{
	public class Rarity
	{
		public string Index { get; set; }
		public string ImageUrl { get; set; }
		public Color Color { get; set; }
		public decimal Value { get; set; }
		public int PercentileValue { get; set; }

		public Rarity(string index, string imageUrl, Color color, decimal value)
		{
			Index = index;
			ImageUrl = imageUrl;
			Color = color;
			Value = value;
		}
	}
	public static List<Rarity> AnimalRarities { get; private set; } = new()
	{
		new Rarity( "F-", "https://i.imgur.com/DueCNx8.png", new Color(6, 84, 228), 0.01m),
		new Rarity("F","https://i.imgur.com/eaasV91.png", new Color(6, 84, 228), 0.02m),
		new Rarity("F+","https://i.imgur.com/F6B8QQb.png", new Color(6, 84, 228), 0.03m),
		new Rarity("E-","https://i.imgur.com/epUwAHr.png", new Color(36, 228, 212), 0.05m),
		new Rarity("E","https://i.imgur.com/DhFsRcr.png", new Color(36, 228, 212), 0.07m),
		new Rarity("E+","https://i.imgur.com/Y6plinP.png", new Color(36, 228, 212), 0.1m),
		new Rarity("D-","https://i.imgur.com/JB0DNWj.png", new Color(42, 228, 127), 0.15m),
		new Rarity("D","https://i.imgur.com/uKhojQo.png", new Color(42, 228, 127), 0.2m),
		new Rarity("D+","https://i.imgur.com/zF8mXIt.png", new Color(42, 228, 127), 0.3m),
		new Rarity("C-","https://i.imgur.com/uKiJt8V.png", new Color(50, 229, 43), 0.4m),
		new Rarity("C","https://i.imgur.com/56qj9pX.png", new Color(50, 229, 43), 0.6m),
		new Rarity("C+","https://i.imgur.com/KLsqazN.png", new Color(50, 229, 43), 0.8m),
		new Rarity("B-","https://i.imgur.com/SwzHOrR.png", new Color(223, 230, 75), 1m),
		new Rarity("B","https://i.imgur.com/EZKH8Fn.png", new Color(223, 230, 75), 1.5m),
		new Rarity("B+","https://i.imgur.com/vV27igO.png", new Color(223, 230, 75), 2m),
		new Rarity( "A-", "https://i.imgur.com/VkqngLL.png", new Color(226, 146, 54), 3m),
		new Rarity("A","https://i.imgur.com/Cpnz0jC.png", new Color(226, 146, 54), 4.5m),
		new Rarity("A+","https://i.imgur.com/mhhf5e9.png", new Color(226, 146, 54), 6m),
		new Rarity( "S-","https://i.imgur.com/UKMV4gu.png", new Color(228, 58, 43), 8m),
		new Rarity("S", "https://i.imgur.com/QWdejAx.png", new Color(228, 58, 43), 12m),
		new Rarity("S+","https://i.imgur.com/Gb8dN1p.png", new Color(228, 58, 43), 16m),
	};

	private static async Task InitializeRarityGroups()
	{
		using (var db = new AnimalsDatabase())
		{
			var animals = db.Animals;
			var totalFishes = await animals.CountAsync();
			var groupSize = totalFishes * (1f / AnimalRarities.Count());

			int effectiveGroupSize = (int)Math.Ceiling(groupSize);

			var animalGroups = await animals.AsAsyncEnumerable()
				.OrderBy(x => x.MonthlyViews)
				.Select((animal, index) => new { Animal = animal, Index = index })
				.GroupBy(x => x.Index / effectiveGroupSize, x => x.Animal)
				.ToListAsync();

			foreach (var rarityGroup in animalGroups)
			{
				var maxValue = await rarityGroup.MaxAsync(x => x.MonthlyViews);
				var index = animalGroups.IndexOf(rarityGroup);
				AnimalRarities[index].PercentileValue = maxValue;
			}
		}
	}

	public static Rarity GetRarity(float monthlyViews)
	{
		var currentGroup = 0;

		foreach (var rarity in AnimalRarities)
		{
			if (monthlyViews <= rarity.PercentileValue)
				return AnimalRarities[currentGroup];

			currentGroup++;
		}

		return AnimalRarities.Last();
	}

	public struct AnimalEmbedBuilder
	{
		public AnimalEntry AnimalEntry;
		public string Title;
		public SocketUser Author;
		public bool CommonName = true;
		public bool ScientificName = true;
		public bool ConservationStatus = true;
		public bool WikiPage = true;
		public bool WikiInfoPage = true;
		public bool MonthlyViews = true;
		public bool SellAmount = true;
		public bool LastSeen = true;
		public bool Image = true;
		public bool Rarity = true;
		public bool PageId = true;

		public AnimalEmbedBuilder(AnimalEntry animalEntry, string title = null, SocketUser author = null)
		{
			AnimalEntry = animalEntry;
			Title = title;
			Author = author;
		}

		public Embed Build()
		{
			var embedBuilder = new EmbedBuilder();
			var rarity = GetRarity(AnimalEntry.MonthlyViews);

			if (Title != null)
				embedBuilder = embedBuilder.WithTitle(Title);
			if (Author != null)
				embedBuilder = embedBuilder.WithAuthor(Author);
			if (CommonName)
				embedBuilder = embedBuilder.AddField("Common Name:", AnimalEntry.CommonName);
			if (ScientificName)
				embedBuilder = embedBuilder.AddField("Scientific Name:", AnimalEntry.ScientificName);
			if (ConservationStatus)
				embedBuilder = embedBuilder.AddField("Conservation Status:", AnimalEntry.ConservationStatus.ToString().Replace("_", " "));
			if (WikiPage)
				embedBuilder = embedBuilder.WithDescription(AnimalEntry.WikiPage);
			if (WikiInfoPage)
				embedBuilder = embedBuilder.AddField("Wikipedia Info:", AnimalEntry.WikiInfoPage);
			if (MonthlyViews)
				embedBuilder = embedBuilder.AddField("Monthly Views:", AnimalEntry.MonthlyViews);
			if (SellAmount)
				embedBuilder = embedBuilder.AddField("Sell Amount:", NiceMoney((float)rarity.Value));

			if (LastSeen)
			{
				var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				var sinceEpoch = AnimalEntry.LastCaught - unixEpoch;
				var lastSeen = $"<t:{(int)sinceEpoch.TotalSeconds}:R>";

				if (AnimalEntry.LastCaught == DateTime.MinValue)
					lastSeen = "Never!";

				embedBuilder = embedBuilder.AddField("Last Seen:", lastSeen);
			}

			if (Image)
				embedBuilder = embedBuilder.WithImageUrl(AnimalEntry.ImageUrl);

			if (Rarity)
			{
				embedBuilder = embedBuilder.WithColor(rarity.Color)
				.WithThumbnailUrl(rarity.ImageUrl);
			}

			if (PageId)
			{
				var data = AnimalEntry; // Why do I have to do this I don't get it
				embedBuilder = embedBuilder.WithFooter(x => x.Text = $"Identifier: {data.Id}");
			}

			return embedBuilder.Build();
		}
	}
}