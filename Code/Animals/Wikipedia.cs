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

		var infoboxBiota = LoadBiota(htmlDocument);
		if (infoboxBiota == null)
		{
			Console.WriteLine($"{pageUrl} infobox biota not found.");
			return null;
		}

		var wikiPageIdentifier = GetPageIdentifier(pageUrl);
		var wikiPageUrl = GetWikiPage(wikiPageIdentifier);
		var wikiInfoPageUrl = GetWikiInfoPage(wikiPageIdentifier);



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
	private static Biota LoadBiota(HtmlDocument htmlDocument)
	{
		var infoboxBiota = IsolateBiota(htmlDocument);

		if (infoboxBiota == null) return null;


		var statusNode = infoboxBiota.SelectSingleNode(".//tr[td[contains(text(),'Conservation status')]]/following-sibling::tr[1]/td");
		var conservationStatus = statusNode?.InnerText.Trim() ?? "Data Deficient";

		var domainTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Domain");
		var kingdomTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Kingdom");
		var phylumTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Phylum");
		var classTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Class");
		var orderTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Order");
		var familyTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Family");
		var genusTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Genus");
		var speciesTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Species");
		var subspeciesTaxonomy = IsolateTaxonomicGroup(infoboxBiota, "Subspecies");

		var imageNode = infoboxBiota.SelectSingleNode(".//img");
		var imageUrl = imageNode?.GetAttributeValue("src", "No image found");
		// TODO Go up the taxonomy groups and use that

		// Fetch Trinomial name if present
		var trinomialNode = infobox.SelectSingleNode(".//span[@class='trinomial']");
		string trinomialName = trinomialNode != null ? trinomialNode.InnerText.Trim() : "No trinomial name found";

		Console.WriteLine($"Image URL: {imageUrl}");
		Console.WriteLine($"Conservation Status: {conservationStatus}");
		Console.WriteLine($"Trinomial Name: {trinomialName}");
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

		if (groupNode != null)
		{
			var groupName = groupNode.InnerText.Trim();
			var groupUrl = groupNode.GetAttributeValue("href", null);

			// Ignore Incertae Sedis taxonomic group
			if (groupName.Contains("incertae", StringComparison.OrdinalIgnoreCase)) return null;

			return new TaxonomicGroup(groupName, groupUrl is null ? "No URL found" : WikipediaAbsolutePath(groupUrl));
		}

		return null;
	}

	/// <summary>
	/// Find and return the first image displayed in the infobox biota node
	/// </summary>
	/// <param name="biota"></param>
	/// <returns></returns>
	private static string IsolateImage(HtmlNode biota)
	{
		var imageNode = biota.SelectSingleNode(".//img");
		return imageNode?.GetAttributeValue("src", null);
		// TODO Go up the taxonomy groups and use that
		// TODO Ignore images that are not the animal
	}
}