namespace Fishley;

public partial class Fishley
{
	public class SboxUgcNewsScraper : WebsiteScraper
	{
		public override string Url => "https://services.facepunch.com/sbox/news";
		public override int SecondsCooldown => 60 * 10;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<ScrapingResult> Fetch()
		{
			using var client = new HttpClient();

			try
			{
				var json = await client.GetStringAsync(Url);
				var data = JsonConvert.DeserializeObject<dynamic[]>(json);

				var latestPost = data.First();
				string path = latestPost.Url;
				string image = latestPost.Media;
				string title = latestPost.Title;
				var author = latestPost.Author;
				var ident = latestPost.Package;

				var embedBuilder = new EmbedBuilder();
				embedBuilder.WithUrl($"https://sbox.game${path}");
				embedBuilder.WithAuthor(author.Name as string, author.Avatar as string);
				embedBuilder.WithTitle(title);

				var packageJson = await client.GetStringAsync($"https://services.facepunch.com/sbox/package/find?q=${ident as string}");
				var packageData = JsonConvert.DeserializeObject<dynamic>(packageJson);
				
				embedBuilder.WithDescription(packageData.Packages[0].Title as string);
				if (image is not null && image.Trim() != "")
					embedBuilder.WithImageUrl(image);
				
				return new ScrapingResult($"<https://sbox.game${path}>", embedBuilder.Build());
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return new ScrapingResult(null, null);
			}
		}
	}
}