namespace Fishley;

public partial class Fishley
{
	private static string _scrapedSites => @"/home/ubre/Desktop/Fishley/scraped_sites.json";

	public static Dictionary<string, WebsiteScraper> WebsitesToCheck = new()
	{
		{ "r_sandbox", new SubredditScraper() }
	};

	public static async Task ComputeScrapers()
	{
		var scrapedWebsites = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(_scrapedSites));
		foreach (var scraper in WebsitesToCheck)
		{
			var secondsPassed = (DateTime.UtcNow - scraper.Value.LastFetched).TotalSeconds;

			if (secondsPassed >= scraper.Value.SecondsCooldown)
			{
				DebugSay($"Scraping {scraper.Key}");
				scraper.Value.LastFetched = DateTime.UtcNow;

				string currentUrl;
				scrapedWebsites.TryGetValue(scraper.Key, out currentUrl);

				var fetched = await scraper.Value.Fetch();

				if (fetched == null) continue;

				if (currentUrl == null || currentUrl != fetched)
				{
					scrapedWebsites[scraper.Key] = fetched;
					await SendMessage((SocketTextChannel)scraper.Value.ChannelToPost, $"New r/sandbox post!\n{fetched}");
				}
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

		public virtual async Task<string> Fetch()
		{
			await Task.CompletedTask;
			return string.Empty;
		}
	}
}