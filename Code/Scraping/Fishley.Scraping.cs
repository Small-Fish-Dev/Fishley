namespace Fishley;

public partial class Fishley
{
	private static string _scrapedSites => @"/home/ubre/Desktop/Fishley/scraped_sites.json";

	public static Dictionary<string, WebsiteScraper> WebsitesToCheck = new()
	{
		{ "r_sandbox", new SubredditScraper() },
		{ "youtube", new YoutubeScraper() },
		{ "garry", new GarryScraper() },
		{ "asset.party", new AssetPartyScraper() },
		{ "latentplaces", new LatentPlacesScrapper() },
		{ "garrytiktok", new GarryTiktokScraper() },
		{ "mindfunk", new MindfunkScraper() }
	};

	public static async Task ComputeScrapers()
	{
		var scrapedWebsites = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(await File.ReadAllTextAsync(_scrapedSites));
		foreach (var scraper in WebsitesToCheck)
		{
			var secondsPassed = (DateTime.UtcNow - scraper.Value.LastFetched).TotalSeconds;

			if (secondsPassed >= scraper.Value.SecondsCooldown)
			{
				DebugSay($"Scraping {scraper.Key}");
				scraper.Value.LastFetched = DateTime.UtcNow;

				List<string> currentUrls;
				scrapedWebsites.TryGetValue(scraper.Key, out currentUrls);

				var fetched = await scraper.Value.Fetch();

				if (fetched.Item1 == null) continue;

				if (currentUrls == null || !currentUrls.Contains(fetched.Item1))
				{
					if (scrapedWebsites.ContainsKey(scraper.Key))
						scrapedWebsites[scraper.Key].Add(fetched.Item1);
					else
						scrapedWebsites.Add(scraper.Key, new List<string>() { fetched.Item1 });

					await SendMessage((SocketTextChannel)scraper.Value.ChannelToPost, $"{fetched.Item1}", embed: fetched.Item2, pathToUpload: fetched.Item3);
				}
			}
		}

		var maxCount = 20;

		foreach (var links in scrapedWebsites.Values)
		{
			if (links.Count() > maxCount)
			{
				links.RemoveAt(0);
			}
		}

		await File.WriteAllTextAsync(_scrapedSites, System.Text.Json.JsonSerializer.Serialize(scrapedWebsites));
	}

	public abstract class WebsiteScraper
	{
		public virtual string Url { get; private set; }
		public virtual int SecondsCooldown { get; private set; } = 60 * 5; // Every 5 minutes
		public virtual SocketGuildChannel ChannelToPost { get; private set; }
		public DateTime LastFetched { get; set; }

		public virtual async Task<(string, Embed, string)> Fetch()
		{
			await Task.CompletedTask;
			return (null, null, null);
		}
	}
}