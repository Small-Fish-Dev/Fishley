namespace Fishley;

public partial class Fishley
{
	public class AssetPartyScraper : WebsiteScraper
	{
		public override int SecondsCooldown => 60 * 1 + 5;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<(string, Embed, string)> Fetch()
		{
			var query = await AssetParty.AssetParty.QueryAsync(sortType: QuerySort.Newest);

			if (query == null) return (null, null, null);

			var newestItem = query.Packages.First();
			var embed = await newestItem.ToEmbed();

			return (newestItem.FullUrl, embed.Item1, embed.Item2);
		}
	}
}