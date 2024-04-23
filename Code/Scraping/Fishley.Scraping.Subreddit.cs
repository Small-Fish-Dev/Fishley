namespace Fishley;

public partial class Fishley
{
	public class SubredditScraper : WebsiteScraper
	{
		public override string Url => "https://old.reddit.com/r/sandbox/new/";
		public override int SecondsCooldown => 60; // Every minute
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<string> Fetch()
		{
			await Task.CompletedTask;
			return string.Empty;
		}
	}
}