namespace Fishley;

public partial class Fishley
{
	public class SboxGameScraper : WebsiteScraper
	{
		public override int SecondsCooldown => 60 * 1 + 5;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<ScrapingResult> Fetch()
		{
			var query = await SboxGame.SboxGame.QueryAsync(sortType: QuerySort.Newest);

			if (query == null) return new ScrapingResult(null, null);

			var newestItem = query.Packages.First();
			var embed = await newestItem.ToEmbed();
			string extraVideo = null;

			if (newestItem.Screenshots != null && newestItem.Screenshots.Count() >= 1)
				if (newestItem.Screenshots.First().IsVideo)
					extraVideo = newestItem.Screenshots.First().Url;

			return new ScrapingResult(newestItem.FullUrl, embed.Item1, embed.Item2, extraVideo);
		}
	}
}