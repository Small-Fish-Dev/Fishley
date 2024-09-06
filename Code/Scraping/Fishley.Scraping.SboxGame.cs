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

			return new ScrapingResult(newestItem.FullUrl, embed.Item1, embed.Item2);
		}
	}
}