namespace Fishley;

public struct ScrapingResult
{
	public string Url { get; set; }
	public Embed Embed { get; set; }
	public string Video { get; set; }
	public string ExtraUrl { get; set; }

	public ScrapingResult(string url, Embed embed, string video = null, string extraUrl = null)
	{
		Url = url;
		Embed = embed;
		Video = video;
		ExtraUrl = extraUrl;
	}
}

public partial class Fishley
{
	private static string _scrapedSites => @"/home/ubre/Desktop/Fishley/scraped_sites.json";

	public static Dictionary<string, WebsiteScraper> WebsitesToCheck = new()
	{
		{ "r_sandbox", new SubredditScraper() },
		{ "twitch", new TwitchScraper() },
		{ "youtube", new YoutubeScraper() },
		{ "garry", new GarryScraper() },
		//{ "sbox.game", new SboxGameScraper() },
		//{ "latentplaces", new LatentPlacesScrapper() },
		{ "garrytiktok", new GarryTiktokScraper() },
		{ "fish_of_the_week", new FishOfTheWeekTikTok() }
		//{ "mindfunk", new MindfunkScraper() }
	};

	public static async Task ComputeScrapers()
	{
		if ( !Running ) return;

		var file = await File.ReadAllTextAsync(_scrapedSites);

		var scrapedWebsites = new Dictionary<string, List<string>>();

		if (file != null && file != string.Empty)
			scrapedWebsites = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(file);

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

				if (fetched.Url == null) continue;

				if (currentUrls == null || !currentUrls.Contains(fetched.Url))
				{
					if (scrapedWebsites.ContainsKey(scraper.Key))
						scrapedWebsites[scraper.Key].Add(fetched.Url);
					else
						scrapedWebsites.Add(scraper.Key, new List<string>() { fetched.Url });

					if (fetched.ExtraUrl != null)
						await SendMessage((SocketTextChannel)scraper.Value.ChannelToPost, fetched.ExtraUrl);

					await SendMessage((SocketTextChannel)scraper.Value.ChannelToPost, $"{fetched.Url}", embed: fetched.Embed, pathToUpload: fetched.Video);
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

		public virtual async Task<ScrapingResult> Fetch()
		{
			await Task.CompletedTask;
			return new ScrapingResult(null, null);
		}
	}
}