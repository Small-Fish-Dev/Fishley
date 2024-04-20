public partial class Fishley
{
	public struct Fish
	{
		[BsonId]
		public int Id { get; set; } = 0;
		public string CommonName { get; set; } = "Fish";
		public string PageName { get; set; } = "Fish";
		public string WikiPage { get; set; } = "https://smallfi.sh";
		public string WikiInfoPage { get; set; } = "https://smallfi.sh";
		public int MonthlyViews { get; set; } = 0;
		public string ImageLink { get; set; } = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png";
		public long LastSeen { get; set; } = 0;
		public string Rarity { get; set; } = "F-";

		public Fish() {}
	}

	public static Dictionary<string, (string, Color, decimal)> FishRarities { get; set; } = new() // Will make it better
	{
		{ "F-", ("https://i.imgur.com/DueCNx8.png", new Color(6,84,228), 0.005m) },
		{ "F", ("https://i.imgur.com/eaasV91.png", new Color(6,84,228), 0.007m) },
		{ "F+", ("https://i.imgur.com/F6B8QQb.png", new Color(6,84,228), 0.01m) },
		{ "E-", ("https://i.imgur.com/epUwAHr.png", new Color(36,228,212), 0.015m) },
		{ "E", ("https://i.imgur.com/DhFsRcr.png", new Color(36,228,212), 0.02m) },
		{ "E+", ("https://i.imgur.com/Y6plinP.png", new Color(36,228,212), 0.03m) },
		{ "D-", ("https://i.imgur.com/JB0DNWj.png", new Color(42,228,127), 0.05m) },
		{ "D", ("https://i.imgur.com/uKhojQo.png", new Color(42,228,127), 0.07m) },
		{ "D+", ("https://i.imgur.com/zF8mXIt.png", new Color(42,228,127), 0.1m) },
		{ "C-", ("https://i.imgur.com/uKiJt8V.png", new Color(50,229,43), 0.15m) },
		{ "C", ("https://i.imgur.com/56qj9pX.png", new Color(50,229,43), 0.2m) },
		{ "C+", ("https://i.imgur.com/KLsqazN.png", new Color(50,229,43), 0.3m) },
		{ "B-", ("https://i.imgur.com/SwzHOrR.png", new Color(223,230,75), 0.5m) },
		{ "B", ("https://i.imgur.com/EZKH8Fn.png", new Color(223,230,75), 0.7m) },
		{ "B+", ("https://i.imgur.com/vV27igO.png", new Color(223,230,75), 1m) },
		{ "A-", ("https://i.imgur.com/VkqngLL.png", new Color(226,146,54), 1.5m) },
		{ "A", ("https://i.imgur.com/Cpnz0jC.png", new Color(226,146,54), 2m) },
		{ "A+", ("https://i.imgur.com/mhhf5e9.png", new Color(226,146,54), 3m) },
		{ "S-", ("https://i.imgur.com/UKMV4gu.png", new Color(228,58,43), 5m) },
		{ "S", ("https://i.imgur.com/QWdejAx.png", new Color(228,58,43), 7m) },
		{ "S+", ("https://i.imgur.com/Gb8dN1p.png", new Color(228,58,43), 10m) },
	};

	public static string FishDatabasePath => @"/home/ubre/Desktop/Fishley/fishes.db";
	public static LiteDatabase FishDatabase => new ( FishDatabasePath );
	public static ILiteCollection<Fish> AllFishes => FishDatabase.GetCollection<Fish>( "fishes" );

	public static void FishUpdate( Fish fish ) 
	{
		using (var database = FishDatabase )
		{
			database.BeginTrans();
			var fishes = AllFishes;
	 		var added = fishes.Upsert( fish );
			database.Commit();

			Console.WriteLine( $"{(added ? "Added" : "Updated")} {fish.CommonName} {fish.PageName} {fish.WikiPage} {fish.WikiInfoPage} {fish.MonthlyViews} {fish.ImageLink}" );
		}
	}

	public static void LoadFishes()
	{
		InitializeFishRarities( 100f / FishRarities.Count() );
	}

	private static List<int> _fishPercentileGroups { get; set; } = new();

	private static void InitializeFishRarities( float percentageSize )
	{
		var totalFishes = AllFishes.Count();
		var groupSize = totalFishes * (percentageSize / 100f);

		int effectiveGroupSize = (int)Math.Ceiling(groupSize);

		var fishGroups = AllFishes.Query().ToList().OrderBy(x => x.MonthlyViews)
			.Select((fish, index) => new { Fish = fish, Index = index })
			.GroupBy(x => x.Index / effectiveGroupSize, x => x.Fish);

		_fishPercentileGroups?.Clear();

		foreach (var fishGroup in fishGroups)
			_fishPercentileGroups.Add(fishGroup.Max(x => x.MonthlyViews));
	}

	public static string GetFishRarity( float monthlyViews )
	{
		if ( _fishPercentileGroups == null || _fishPercentileGroups.Count() == 0)
			InitializeFishRarities( 100f / FishRarities.Count() );

		var currentGroup = 0;

		foreach ( var group in _fishPercentileGroups )
		{
			if ( monthlyViews <= group )
				return FishRarities.ToArray()[currentGroup].Key;

			currentGroup++;
		}

		return FishRarities.ToArray()[_fishPercentileGroups.Count()].Key;
	}

	private static bool IsFish( string text )
	{
		if ( text.Contains( "Agnatha", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains( "Chondrichthyes", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains( "Osteichthyes", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains( "Actinopterygii", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains( "Sarcopterygii", StringComparison.OrdinalIgnoreCase ) ) return true;

		return false;
	}

	private static bool IsIndexedPage( string text )
	{
		if ( text.Contains("page lists  articles associated with the title", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("This page is an index of articles", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("This disambiguation page lists articles associated", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("Index of animals with the same common name", StringComparison.OrdinalIgnoreCase ) ) return true;

		return false;
	}

	public static async Task ScrapeWikipediaLol()
	{
		var url = "https://en.wikipedia.org/wiki/List_of_fish_common_names";
        var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        var html = await httpClient.GetStringAsync(url);
		var fishData = new List<Fish>();
		var initialLinksFound = new List<(string,string)>(); // href, common name

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

		// Get every hyperlink in lists that's valid
        var linkNodes = htmlDoc.DocumentNode.SelectNodes("//ul/li/a[contains(@href, '/wiki/') and not(contains(@href, ':'))]");

        if ( linkNodes != null )
        {
			foreach (var node in linkNodes)
			{
				var href = node.Attributes["href"].Value;
				var commonName = node.InnerText;
				Console.WriteLine($"Found fish: {commonName}, Link: https://en.wikipedia.org{href}");
				
				initialLinksFound.Add( (href, commonName) );
            }
        }

		var redirectsFounds = new List<(string, string)>(); // In case we find redirects instead of fishes

		foreach ( var fishLink in initialLinksFound )
		{
			await Task.Delay( 1000 ); // Wait 1 second, don't wanna get blocked by wikipedia

			var fishUrl = $"https://en.wikipedia.org{fishLink.Item1}";
			var linkHtml = await httpClient.GetStringAsync(fishUrl);
			var linkDoc = new HtmlDocument();
			linkDoc.LoadHtml(linkHtml);
			var docText = linkDoc.DocumentNode.InnerText;
			Console.WriteLine( $"Checking for {fishLink.Item2}" );

			if ( IsIndexedPage( docText ) )
			{
				// Get every hyperlink in lists that's valid
				var initialLinkNodes = linkDoc.DocumentNode.SelectNodes("//ul/li/a[contains(@href, '/wiki/') and not(contains(@href, ':'))]");
				if (initialLinkNodes != null)
				{
					for (int i = 0; i < initialLinkNodes.Count; i++)
					{
						var node = initialLinkNodes[i];
                        var href = node.Attributes["href"].Value;
                        var commonName = node.InnerText;
                        Console.WriteLine($"Found fish: {commonName}, Link: https://en.wikipedia.org{href}");

						redirectsFounds.Add( (href, commonName) );
					}
				}
			}
			else
			{
				if ( !IsFish( docText ) ) continue;

				var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png"; // Placeholder
				var pageImage = linkDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

				if ( pageImage != null ) // TODO If no image found, get the family image
				{
					imageUrl = pageImage.GetAttributeValue("content", string.Empty);
				}
				else
				{
					// If no og:image found, try to get the image from the scientific classification box
					var scientificClassificationImage = linkDoc.DocumentNode.SelectSingleNode("//table[@class='infobox biota']/following-sibling::div[@class='thumb']/descendant::img");

					if (scientificClassificationImage != null)
					{
						imageUrl = scientificClassificationImage.GetAttributeValue("src", string.Empty);
						// Add the protocol if it's missing
						if (!imageUrl.StartsWith("http"))
						{
							imageUrl = "https:" + imageUrl;
						}
					}
				}

				var title = "Fish";
				var titleNode = linkDoc.DocumentNode.SelectSingleNode("//title");

				if ( titleNode != null )
					title = titleNode.InnerText.Replace(" - Wikipedia", "");

				var pageName = fishLink.Item1.Split("/wiki/").Last();
				var infoPageLink = $"https://en.wikipedia.org/w/index.php?title={pageName}&action=info";

				await Task.Delay( 1000 );

				var infoResponse = await httpClient.GetAsync(infoPageLink);
				var infoHtml = await infoResponse.Content.ReadAsStringAsync();
				var infoDocument = new HtmlDocument();
				infoDocument.LoadHtml(infoHtml);

				var viewNode = infoDocument.DocumentNode.SelectSingleNode("//tr[@id='mw-pvi-month-count']/td/div/a");
				var monthlyViews = viewNode?.InnerText.Trim().Replace( ",", "" ).Replace( ".", "" ) ?? "0";
				var realMonthlyViews = 0;

				if ( int.TryParse( monthlyViews, out var parsedMonthlyViews ) )
					realMonthlyViews = parsedMonthlyViews;
				
				var newFish = new Fish()
				{
					CommonName = fishLink.Item2,
					PageName = title,
					WikiPage = $"https://en.wikipedia.org/wiki/{fishLink.Item2}",
					WikiInfoPage = infoPageLink,
					MonthlyViews = realMonthlyViews,
					ImageLink = imageUrl
				};

				FishUpdate( newFish );
			}
		}

		foreach ( var fishLink in redirectsFounds ) // TODO: Reuse code
		{
			await Task.Delay( 1000 ); // Wait 1 second, don't wanna get blocked by wikipedia

			var fishUrl = $"https://en.wikipedia.org{fishLink.Item1}";
			var linkHtml = await httpClient.GetStringAsync(fishUrl);
			var linkDoc = new HtmlDocument();
			linkDoc.LoadHtml(linkHtml);
			var docText = linkDoc.DocumentNode.InnerText;
			Console.WriteLine( $"Checking for {fishLink.Item2}" );

			if ( !IsFish( docText ) ) continue;

			var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png"; // Placeholder
			var pageImage = linkDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

			if ( pageImage != null ) // TODO If no image found, get the family image
				imageUrl = pageImage.GetAttributeValue("content", string.Empty);

			var title = "Fish";
			var titleNode = linkDoc.DocumentNode.SelectSingleNode("//title");

			if ( titleNode != null )
				title = titleNode.InnerText.Replace(" - Wikipedia", "");

			var pageName = fishLink.Item1.Split("/wiki/").Last();
			var infoPageLink = $"https://en.wikipedia.org/w/index.php?title={pageName}&action=info";

			await Task.Delay( 1000 );

			var infoResponse = await httpClient.GetAsync(infoPageLink);
			var infoHtml = await infoResponse.Content.ReadAsStringAsync();
			var infoDocument = new HtmlDocument();
			infoDocument.LoadHtml(infoHtml);

			var viewNode = infoDocument.DocumentNode.SelectSingleNode("//tr[@id='mw-pvi-month-count']/td/div/a");
			var monthlyViews = viewNode?.InnerText.Trim().Replace( ",", "" ).Replace( ".", "" ) ?? "0";
			var realMonthlyViews = 0;

			if ( int.TryParse( monthlyViews, out var parsedMonthlyViews ) )
				realMonthlyViews = parsedMonthlyViews;
				
			var newFish = new Fish()
			{
				CommonName = fishLink.Item2,
				PageName = title,
				WikiPage = $"https://en.wikipedia.org/wiki/{fishLink.Item2}",
				WikiInfoPage = infoPageLink,
				MonthlyViews = realMonthlyViews,
				ImageLink = imageUrl
			};

			FishUpdate( newFish );
		}
	}
}