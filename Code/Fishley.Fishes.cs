namespace Fishley;

public partial class Fishley
{
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

	public static void LoadFishes()
	{
		//InitializeFishRarities( 100f / FishRarities.Count() );
	}

	private static List<int> _fishPercentileGroups { get; set; } = new();

	private static void InitializeFishRarities( float percentageSize )
	{
		/*
		var totalFishes = AllFishes.Count();
		var groupSize = totalFishes * (percentageSize / 100f);

		int effectiveGroupSize = (int)Math.Ceiling(groupSize);

		var fishGroups = AllFishes.Query().ToList().OrderBy(x => x.MonthlyViews)
			.Select((fish, index) => new { Fish = fish, Index = index })
			.GroupBy(x => x.Index / effectiveGroupSize, x => x.Fish);

		_fishPercentileGroups?.Clear();

		foreach (var fishGroup in fishGroups)
			_fishPercentileGroups.Add(fishGroup.Max(x => x.MonthlyViews));*/
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

	private static bool IsFish( HtmlDocument document )
	{
		var taxonomyNode = document.DocumentNode.SelectSingleNode( "//tr[td[contains(., 'Class:') or contains(., 'Clade:') or contains(., 'Superclass:')]]/td[2]/a" );

		if (taxonomyNode == null)
			taxonomyNode = document.DocumentNode.SelectSingleNode( "//tr[td[contains(., 'Class:') or contains(., 'Clade:') or contains(., 'Superclass:')]]/td[2]" ); // No link found let's try plain text

        if ( taxonomyNode != null )
        {
			var animalType = taxonomyNode.InnerText.Trim();

			if ( animalType.Contains( "Agnatha", StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( animalType.Contains( "Chondrichthyes", StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( animalType.Contains( "Osteichthyes", StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( animalType.Contains( "Actinopterygii", StringComparison.OrdinalIgnoreCase ) ) return true;
			if ( animalType.Contains( "Sarcopterygii", StringComparison.OrdinalIgnoreCase ) ) return true;
		}

		return false;
	}

	private static bool IsIndexedPage( HtmlDocument document )
	{
		var text = document.DocumentNode.InnerText;

		if ( text.Contains("page lists  articles associated with the title", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("This page is an index of articles", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("This disambiguation page lists articles associated", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("Index of animals with the same common name", StringComparison.OrdinalIgnoreCase ) ) return true;
		if ( text.Contains("is a common name", StringComparison.OrdinalIgnoreCase ) ) return true;

		return false;
	}

	public static async Task ExplorePage( HttpClient client, string url, string commonName = null, bool startingPage = false, int waitBetweenCalls = 1000, List<string> visitedUrls = null, int currentRecursion = 0 )
	{
		if ( startingPage )
			visitedUrls = new();

		if ( visitedUrls.Contains( url ) )
		{
			DebugSay( $"Visited {url} already, skipping.." );
			return;
		}

		if ( currentRecursion >= 3 )
		{
			DebugSay( $"We are looking too deep man, skipping..." );
			return;
		}

		DebugSay( $"Exploring {url}" );
		var response = await client.GetAsync( url, HttpCompletionOption.ResponseHeadersRead );

		if ( !response.IsSuccessStatusCode )
		{
			DebugSay( $"Unable to fetch {url}. Status code: {response.StatusCode}" );
			return;
		}

		visitedUrls.Add( url );

		var finalUrl = response.RequestMessage.RequestUri.AbsoluteUri;
		if ( finalUrl != url )
		{
			DebugSay( $"{url} redirected to {finalUrl}" ); // This never works lol
			url = finalUrl;
			visitedUrls.Add( url );
		}

		var loadedPage = await response.Content.ReadAsStringAsync();
		var htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml( loadedPage );

		await Task.Delay( waitBetweenCalls ); // Wait before doing anything else, we don't want to overload Wikipedia (Or get blocked!)

		// Is this page a collection of links?
		if ( startingPage || IsIndexedPage( htmlDocument ) )
		{
			DebugSay( $"{url} is an indexed page" );
			// Get every valid hyperlink that is inside of a list
			var outgoingLinks = htmlDocument.DocumentNode.SelectNodes("//ul/li/a[contains(@href, '/wiki/') and not(contains(@href, ':'))]");

			if ( outgoingLinks != null )
			{
				foreach ( var linkNode in outgoingLinks )
				{
					var reference = linkNode.Attributes["href"].Value;
					var innerText = linkNode.InnerText;

					// Recursively go through all links, let's find all the fish!
					await ExplorePage( client, $"https://en.wikipedia.org{reference}", innerText, false, waitBetweenCalls, visitedUrls, currentRecursion + 1 );
				}
			}
		}
		else
		{
			// Is this page a fish or is it Mark Twain again?
			if ( IsFish( htmlDocument ) )
				await AddFishPage( client, htmlDocument, url, commonName, waitBetweenCalls );
		}
	}

	public static async Task AddFishPage( HttpClient client, HtmlDocument document, string url, string commonName, int waitBetweenCalls = 1000 )
	{
		DebugSay( $"Adding fish: {url}" );
		var documentNode = document.DocumentNode;

		var pageIdentifier = url.Split("/wiki/").Last();
		var infoPageLink = $"https://en.wikipedia.org/w/index.php?title={pageIdentifier}&action=info";

		#region Setting the title and image
		var imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png"; // Placeholder image
		var pageImage = documentNode.SelectSingleNode( "//meta[@property='og:image']" ); // Fetch the embed image

		if ( pageImage != null )
		{
			imageUrl = pageImage.GetAttributeValue( "content", imageUrl );
		}
		else
		{
			// Find the taxonomy image if no embed image is used
			var taxonomyImageNode = documentNode.SelectSingleNode( "//table[contains(@class, 'infobox biota')]//img" );

			if ( taxonomyImageNode != null)
			{
				imageUrl = taxonomyImageNode.GetAttributeValue("src", string.Empty);

				if ( !imageUrl.StartsWith( "http:", StringComparison.OrdinalIgnoreCase ) && !imageUrl.StartsWith( "https:", StringComparison.OrdinalIgnoreCase ) ) // Make sure it's using a full url
					imageUrl = "https:" + imageUrl;
			}
		}

		var pageName = commonName;
		var titleNode = documentNode.SelectSingleNode( "//title" );

		if ( titleNode != null )
			pageName = titleNode.InnerText.Replace( " - Wikipedia", "" );
		else
		{
			if ( pageName == null ) // If we didn't even have a common name to go off of
				pageName = pageIdentifier.Replace( "_", " " ); // Beautify the identifier and use it as the title
		}
		#endregion

		await Task.Delay( waitBetweenCalls ); // We're about to make another request so wait

		#region Setting the metadata
		var infoResponse = await client.GetStringAsync( infoPageLink );
		var infoHtmlDocument = new HtmlDocument();
		infoHtmlDocument.LoadHtml( infoResponse );
		var infoDocumentNode = infoHtmlDocument.DocumentNode;

		var pageIdNode = infoDocumentNode.SelectSingleNode( "//tr[@id='mw-pageinfo-article-id']/td[2]" );
		var pageIdData = pageIdNode?.InnerText.Trim().Replace( ",", "" ).Replace( ".", "" ) ?? "0"; // Clean string to parse
		int.TryParse( pageIdData, out var pageId );

		if ( pageId == 0 )
			pageId = new Random( (int)DateTime.UtcNow.Ticks ).Next(); // Avoid multiple pages with the same Id at all costs

		var monthlyViewsNode = infoDocumentNode.SelectSingleNode( "//tr[@id='mw-pvi-month-count']/td/div/a" );
		var monthlyViewsData = monthlyViewsNode?.InnerText.Trim().Replace( ",", "" ).Replace( ".", "" ) ?? "0"; // Clean string to parse
		int.TryParse( monthlyViewsData, out var monthlyViews );
		#endregion

		var newFish = new FishData( pageId )
		{
			CommonName = commonName,
			PageName = pageName,
			WikiPage = url,
			WikiInfoPage = infoPageLink,
			MonthlyViews = monthlyViews,
			ImageLink = imageUrl
		};

		DebugSay( "Found a new fish:" );
		Console.WriteLine( $"PageId: {pageId}" );
		Console.WriteLine( $"CommonName: {commonName}" );
		Console.WriteLine( $"PageName: {pageName}" );
		Console.WriteLine( $"WikiPage: {url}" );
		Console.WriteLine( $"WikiInfoPage: {infoPageLink}" );
		Console.WriteLine( $"MonthlyViews: {monthlyViews}" );
		Console.WriteLine( $"ImageLink: {imageUrl}" );

		await UpdateOrCreateFish( newFish );
	}

	public static async Task ScrapeWikipediaLol()
	{
		var startingUrl = "https://en.wikipedia.org/wiki/List_of_fish_common_names";
        var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });

		await ExplorePage( httpClient, startingUrl, null, true, 1000 );
	}
}