namespace Fishley;

public partial class Fishley
{
	public static Dictionary<string, WebsiteScraper> WebsitesToCheck = new()
	{
		{ "r/sandbox", new SubredditScraper() }
	};
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