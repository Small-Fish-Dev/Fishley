namespace Fishley;

public partial class Fishley
{
	public class SboxGameScraper : WebsiteScraper
	{
		public override int SecondsCooldown => 60 * 1 + 5;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<(string, Embed, string)> Fetch()
		{
			var query = await SboxGame.SboxGame.QueryAsync(sortType: QuerySort.Newest);

			if (query == null) return (null, null, null);

			var newestItem = query.Packages.First();
			var embed = await newestItem.ToEmbed();

			return (newestItem.FullUrl, embed.Item1, embed.Item2);
		}
	}
}