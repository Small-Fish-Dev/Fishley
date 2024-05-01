namespace Fishley;

public partial class Fishley
{
	public class AssetPartyScraper : WebsiteScraper
	{
		public override int SecondsCooldown => 60 * 1 + 5;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<(string, Embed)> Fetch()
		{
			var query = await AssetParty.AssetParty.QueryAsync(sortType: QuerySort.Newest);

			if (query == null) return (null, null);

			var newestItem = query.Packages.First();
			var embed = newestItem.ToEmbed();
			var finalUrl = newestItem.FullUrl;

			if (newestItem.Screenshots != null && newestItem.Screenshots.Count() >= 1)
			{
				foreach (var screenshot in newestItem.Screenshots)
				{
					finalUrl = $"{finalUrl}\n{screenshot.Url}";
				}
			}
			return (finalUrl, embed);
		}
	}
}