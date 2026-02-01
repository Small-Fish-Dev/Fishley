namespace Fishley;

public partial class Fishley
{
	public class YoutubeScraper : WebsiteScraper
	{
		public virtual string SearchTerm => "s&box";
		public virtual List<string> Blacklist { get; } = new()
		{
			"ragdoll",
			"pixelcat",
			"ekaj",
			"\"S&box Lab\""
		};

		public override string Url
		{
			get
			{
				string blacklistQuery = string.Join(" ", Blacklist.Select(b => "-" + b));
				string query = $"{SearchTerm} {blacklistQuery}".Trim();
				string encoded = Uri.EscapeDataString(query);

				return $"https://www.youtube.com/results?search_query={encoded}&sp=EgQIAhAB";
			}
		}
		public override int SecondsCooldown => 60 + 7;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<ScrapingResult> Fetch()
		{
			DebugSay( Url );
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

				HttpResponseMessage response = await client.GetAsync(Url);
				response.EnsureSuccessStatusCode();
				string htmlContent = await response.Content.ReadAsStringAsync();

				if (string.IsNullOrEmpty(htmlContent)) return new ScrapingResult(null, null, null);

				List<string> links = new List<string>();
				Regex videoLinkRegex = new Regex(@"\/watch\?v=([a-zA-Z0-9_-]{11})");

				MatchCollection matches = videoLinkRegex.Matches(htmlContent);

				foreach (Match match in matches)
				{
					string link = "https://www.youtube.com" + match.Value;

					if (!links.Contains(link))
						links.Add(link);
				}

				if ( links.Count() == 0 )
					return new ScrapingResult();
				return new ScrapingResult(links.First(), null, null);
			}
		}
	}
}