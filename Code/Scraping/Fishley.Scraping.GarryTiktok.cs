namespace Fishley;

public partial class Fishley
{
	public class GarryTiktokScraper : WebsiteScraper
	{
		public override string Url => "https://urlebird.com/user/garrynewman/";
		public override int SecondsCooldown => 60 * 20 + 2; // 20 Minutes
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<ScrapingResult> Fetch()
		{
			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

				HttpResponseMessage response = await client.GetAsync(Url);
				response.EnsureSuccessStatusCode();
				string htmlContent = await response.Content.ReadAsStringAsync();

				if (string.IsNullOrEmpty(htmlContent)) return new ScrapingResult(null, null, null);

				List<string> links = new List<string>();
				Regex videoLinkRegex = new Regex(@"https:\/\/urlebird\.com\/video\/([^\/]+)\/");

				MatchCollection matches = videoLinkRegex.Matches(htmlContent);

				foreach (Match match in matches)
				{
					if (!links.Contains(match.Value))
						links.Add(match.Value);
				}

				return new ScrapingResult(links.First(), null, null);
			}
		}
	}
}