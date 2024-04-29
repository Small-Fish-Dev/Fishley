namespace Animals;

public class Wikipedia
{
	/// <summary>
	/// Try to load a page and catalogue it
	/// </summary>
	/// <param name="client"></param>
	/// <param name="pageUrl"></param>
	/// <param name="waitBetweenCalls"></param>
	/// <returns></returns>
	public static async Task<AnimalEntry> CataloguePage(HttpClient client, string pageUrl, int waitBetweenCalls = 1000)
	{
		if (!IsWikiPage(pageUrl))
		{
			Console.WriteLine($"{pageUrl} is not a wikipedia page.");
			return null;
		}

		pageUrl = GetCleanWikiPage(pageUrl); // Clean any references etc...

		var htmlDocument = await LoadPage(client, pageUrl, waitBetweenCalls);
		if (htmlDocument == null)
		{
			Console.WriteLine($"Found nothing in {pageUrl}.");
			return null;
		}

		var biota = await LoadBiota(htmlDocument, client, waitBetweenCalls);
		if (biota == null)
		{
			Console.WriteLine($"{pageUrl} biota not found.");
			return null;
		}

		var wikiPageIdentifier = GetPageIdentifier(pageUrl);
		var wikiPageUrl = GetWikiPage(wikiPageIdentifier);
		var wikiInfoPageUrl = GetWikiInfoPage(wikiPageIdentifier);

		var wikiInfoPage = await LoadPage(client, wikiInfoPageUrl, waitBetweenCalls);
		if (wikiInfoPage == null)
		{
			Console.WriteLine($"{wikiInfoPageUrl} couldn't load info page.");
			return null;
		}
		var wikiPageId = IsolatePageId(wikiInfoPage);
		var wikiMonthlyViews = IsolateMonthlyViews(wikiInfoPage);

		if (wikiPageId <= -1 || wikiMonthlyViews <= -1)
		{
			Console.WriteLine($"{wikiInfoPageUrl} invalid id or views! PageId: {wikiPageId} - Monthly Views: {wikiMonthlyViews}");
			return null;
		}

		return new AnimalEntry(wikiPageId)
		{
			CommonName = biota.CommonName,
			BinomialName = biota.BinomialName,
			TrinomialName = biota.TrinomialName,
			Domain = biota.Domain?.Name ?? "None",
			Kingdom = biota.Kingdom?.Name ?? "None",
			Phylum = biota.Domain?.Name ?? "None",
			Class = biota.Class?.Name ?? "None",
			Order = biota.Order?.Name ?? "None",
			Family = biota.Family?.Name ?? "None",
			Genus = biota.Genus?.Name ?? "None",
			Species = biota.Species?.Name ?? "None",
			Subspecies = biota.Subspecies?.Name ?? "None",
			ConservationStatus = biota.ConservationStatus,
			WikiIdentifier = wikiPageIdentifier,
			MonthlyViews = wikiMonthlyViews
		};
	}

	/// <summary>
	/// Try to load a page, following redirects and all
	/// </summary>
	/// <param name="client"></param>
	/// <param name="pageUrl"></param>
	/// <param name="waitBetweenCalls"></param>
	/// <returns></returns>
	public static async Task<HtmlDocument> LoadPage(HttpClient client, string pageUrl, int waitBetweenCalls = 1000)
	{
		Console.WriteLine($"Loading {pageUrl}.");
		var pageResponse = await client.GetAsync(pageUrl);

		// Unsuccessful status code
		if ((int)pageResponse.StatusCode < 200 || (int)pageResponse.StatusCode >= 400)
		{
			Console.WriteLine($"{pageUrl} Exited: Status Code {pageResponse.StatusCode}.");
			return null;
		}

		// Redirection status codes
		if ((int)pageResponse.StatusCode >= 300)
		{
			var redirectedUrl = pageResponse.Headers.Location?.ToString() ?? null;

			if (redirectedUrl != null)
			{
				Console.WriteLine($"{pageUrl} was redirected to {redirectedUrl}.");
				await Task.Delay(waitBetweenCalls);
				return await LoadPage(client, redirectedUrl, waitBetweenCalls); // Load the redirected page instead
			}
		}

		var pageContent = await pageResponse.Content.ReadAsStringAsync();
		var htmlDocument = new HtmlDocument();
		htmlDocument.LoadHtml(pageContent);

		return htmlDocument;
	}

	/// <summary>
	/// Is the url provided that of a wikipedia page
	/// </summary>
	/// <param name="pageUrl"></param>
	/// <returns></returns>
	public static bool IsWikiPage(string pageUrl) => pageUrl.Contains("wikipedia.org/wiki/");

	/// <summary>
	/// Remove annoying references
	/// </summary>
	/// <param name="pageUrl"></param>
	/// <returns></returns>
	private static string GetCleanWikiPage(string pageUrl)
	{
		if (pageUrl.Contains("#"))
			return pageUrl.Split("#").First();
		else
			return pageUrl;
	}

	/// <summary>
	/// Return the page identifier of the
	/// </summary>
	/// <param name="pageUrl"></param>
	/// <returns></returns>
	private static string GetPageIdentifier(string pageUrl) => pageUrl.Split("/wiki/").Last();

	/// <summary>
	/// Returns the absolute wikipedia path from an href (Example: "/wiki/Fish" -> "https://en.wikipedia.org/wiki/Fish")
	/// </summary>
	/// <param name="href"></param>
	/// <returns></returns>
	public static string WikipediaAbsolutePath(string href) => $"https://en.wikipedia.org{href}";

	/// <summary>
	/// Get the full wikipedia page from an identifier
	/// </summary>
	/// <param name="pageIdentifier"></param>
	/// <returns></returns>
	public static string GetWikiPage(string pageIdentifier) => WikipediaAbsolutePath($"/wiki/{pageIdentifier}");

	/// <summary>
	/// Get the wiki informations page from the identifier
	/// </summary>
	/// <param name="pageUrl"></param>
	/// <returns></returns>
	public static string GetWikiInfoPage(string pageIdentifier) => WikipediaAbsolutePath($"/w/index.php?title={pageIdentifier}&action=info");

	/// <summary>
	/// Isolate and return the infobox biota as an HtmlNode
	/// </summary>
	/// <param name="htmlDocument"></param>
	/// <returns></returns>
	private static HtmlNode IsolateBiota(HtmlDocument htmlDocument) => htmlDocument.DocumentNode.SelectSingleNode("//table[contains(@class, 'infobox biota')]");

	/// <summary>
	/// Load all available information from the html document onto a Biota class
	/// </summary>
	/// <param name="biotaNode"></param>
	/// <returns></returns>
	private static async Task<Biota> LoadBiota(HtmlDocument htmlDocument, HttpClient client, int waitBetweenCalls = 1000)
	{
		var infoboxBiota = IsolateBiota(htmlDocument);

		if (infoboxBiota == null)
		{
			Console.WriteLine("No infobox biota found.");
			return null;
		}

		var subspeciesTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Subspecies");
		var speciesTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Species");

		if (subspeciesTaxonomy == null && speciesTaxonomy == null)
		{
			Console.WriteLine("Too generalized");
			return null;
		}

		var genusTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Genus");
		var familyTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Family");
		var orderTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Order");
		var classTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Class");
		var phylumTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Phylum");
		var kingdomTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Kingdom");
		var domainTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Domain");

		var commonName = IsolateName(infoboxBiota);
		var imageUrl = IsolateImage(infoboxBiota);

		if (imageUrl == null)
		{
			Console.WriteLine("No image found, searching for the parent taxonomic group.");

			if (subspeciesTaxonomy != null) // This is a subspecies
				imageUrl = await TryFetchImage(client, speciesTaxonomy.Url, waitBetweenCalls); // So we try getting the species image
			else
				imageUrl = await TryFetchImage(client, genusTaxonomy?.Url ?? null, waitBetweenCalls); // If it's a species we try getting the genus image

			// Well then, let's try getting the family image
			if (imageUrl == null)
				imageUrl = await TryFetchImage(client, familyTaxonomy?.Url ?? null, waitBetweenCalls);

			// Wikipedia is really lacking these days, let's try the order image
			if (imageUrl == null)
				imageUrl = await TryFetchImage(client, orderTaxonomy?.Url ?? null, waitBetweenCalls);

			// No point in trying with the class since it's too generalized
			if (imageUrl == null)
				imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png";
		}

		var conservationStatus = AnimalEntry.ParseStatus(IsolateConservationStatus(infoboxBiota));
		var binomialName = IsolateClass(infoboxBiota, "binomial");
		var trinomialName = IsolateClass(infoboxBiota, "trinomial");

		return new Biota()
		{
			CommonName = commonName,
			ConservationStatus = conservationStatus,
			BinomialName = binomialName,
			TrinomialName = trinomialName,
			Domain = domainTaxonomy,
			Kingdom = kingdomTaxonomy,
			Phylum = phylumTaxonomy,
			Class = classTaxonomy,
			Order = orderTaxonomy,
			Family = familyTaxonomy,
			Genus = genusTaxonomy,
			Species = speciesTaxonomy,
			Subspecies = subspeciesTaxonomy,
			ImageUrl = imageUrl
		};
	}

	/// <summary>
	/// Find and return a specific taxonomic group inside of an infobox biota node
	/// </summary>
	/// <param name="biota"></param>
	/// <param name="group"></param>
	/// <returns></returns>
	private static TaxonomicGroup IsolateTaxonomicGroup(HtmlNode biota, string group)
	{
		var groupNode = biota.SelectSingleNode($".//tr[td[contains(text(),'{group}')]]/td[2]/a");
		if (groupNode == null) return null;

		var groupName = groupNode.InnerText.Trim();
		var groupUrl = groupNode.GetAttributeValue("href", null);

		// Ignore Incertae Sedis taxonomic group
		if (groupName.Contains("incertae", StringComparison.OrdinalIgnoreCase)) return null;

		return new TaxonomicGroup(groupName, groupUrl is null ? null : WikipediaAbsolutePath(groupUrl));
	}

	/// <summary>
	/// Find and return the first image displayed in the infobox biota node
	/// </summary>
	/// <param name="biota"></param>
	/// <returns></returns>
	private static string IsolateImage(HtmlNode biota)
	{
		var imageNodes = biota.SelectNodes(".//img");
		if (imageNodes == null || imageNodes.Count() == 0) return null;

		foreach (var imageNode in imageNodes)
		{
			var fileName = imageNode.GetAttributeValue("src", null);
			if (fileName == null) continue;

			// Ignore any map images
			if (fileName.Contains("map.png", StringComparison.OrdinalIgnoreCase)) continue;
			// Ignore range images
			if (fileName.Contains("range.png", StringComparison.OrdinalIgnoreCase)) continue;
			// Ignore conservation status images
			if (fileName.Contains("status_", StringComparison.OrdinalIgnoreCase)) continue;

			return fileName; // Return the first valid animal image found
		}

		return null;
	}

	/// <summary>
	/// Try and fetch the biota image from a page
	/// </summary>
	/// <param name="url"></param>
	/// <param name="client"></param>
	/// <param name="waitBetweenCalls"></param>
	/// <returns></returns>
	private static async Task<string> TryFetchImage(HttpClient client, string url, int waitBetweenCalls)
	{
		if (url == null) return null;

		await Task.Delay(waitBetweenCalls);
		var loadedPage = await LoadPage(client, url, waitBetweenCalls);
		if (loadedPage == null) return null;

		var biota = IsolateBiota(loadedPage);
		return IsolateImage(biota);
	}

	/// <summary>
	/// Isolate a single class node from the biota
	/// </summary>
	/// <param name="biota"></param>
	/// <param name="className"></param>
	/// <returns></returns>
	private static string IsolateClass(HtmlNode biota, string className)
	{
		var classNode = biota.SelectSingleNode($".//span[@class='{className}']");
		if (classNode == null) return null;

		return classNode.InnerText.Trim();
	}

	/// <summary>
	/// Isolate the name at the top of the biota
	/// </summary>
	/// <param name="biota"></param>
	/// <returns></returns>
	private static string IsolateName(HtmlNode biota)
	{
		var nameNode = biota.SelectSingleNode(".//th[@colspan]");
		if (nameNode == null) return null;

		return nameNode.InnerText.Trim();
	}

	/// <summary>
	/// Isolate the conservation status inside of the biota
	/// </summary>
	/// <param name="biota"></param>
	/// <returns></returns>
	private static string IsolateConservationStatus(HtmlNode biota)
	{
		var statusNode = biota.SelectSingleNode(".//tr[td[contains(text(),'Conservation status')]]/following-sibling::tr[1]/td");
		if (statusNode == null) return "Not Evaluated";

		return statusNode.InnerText.Trim();
	}

	/// <summary>
	///  Isolate the page id from a wiki informations page
	/// </summary>
	/// <param name="infoPage"></param>
	/// <returns></returns>
	private static int IsolatePageId(HtmlDocument infoPage)
	{
		var pageIdNode = infoPage.DocumentNode.SelectSingleNode("//tr[@id='mw-pageinfo-article-id']/td[2]");
		if (pageIdNode == null) return -1;

		var pageIdData = pageIdNode.InnerText.Trim()
			.Replace(",", "")
			.Replace(".", ""); // Clean string to parse

		if (int.TryParse(pageIdData, out var pageId)) return pageId;

		return -1;
	}

	/// <summary>
	/// Isolate the monthly views from a wiki informations page
	/// </summary>
	/// <param name="infoPage"></param>
	/// <returns></returns>
	private static int IsolateMonthlyViews(HtmlDocument infoPage)
	{
		var monthlyViewsNode = infoPage.DocumentNode.SelectSingleNode("//tr[@id='mw-pvi-month-count']/td/div/a");
		if (monthlyViewsNode == null) return -1;

		var monthlyViewsData = monthlyViewsNode.InnerText.Trim()
			.Replace(",", "")
			.Replace(".", ""); // Clean string to parse

		if (int.TryParse(monthlyViewsData, out var monthlyViews)) return monthlyViews;

		return -1;
	}
}