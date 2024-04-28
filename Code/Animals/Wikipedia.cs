namespace Animals;

public class Wikipedia
{
	public static async Task<AnimalEntry> CataloguePage(HttpClient client, string pageUrl, int waitBetweenCalls = 1000)
	{
		if (!pageUrl.Contains("wikipedia.org/wiki/"))
		{
			Console.WriteLine($"{pageUrl} is not a wikipedia page.");
			return null;
		}

		if (pageUrl.Contains("#"))
		{
			Console.WriteLine($"{pageUrl} contains a reference, removing...");
			pageUrl = pageUrl.Split("#").First();
		}

		var htmlDocument = await LoadPage(client, pageUrl, waitBetweenCalls);

		if (htmlDocument == null)
		{
			Console.WriteLine($"Found nothing in {pageUrl}.");
			return null;
		}

		var wikiIdentifier = pageUrl.Split("/wiki/").Last();
	}

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
}