public partial class Fishley
{
	public class RandomFishCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName( "fish" )
		.WithDescription( "Get a random fish" );

		public override Func<SocketSlashCommand, Task> Function => GetRandomFish;

		public async Task GetRandomFish(SocketSlashCommand command)
		{
			using (var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
			{
				var response = await httpClient.GetAsync("https://en.wikipedia.org/wiki/List_of_fish_common_names");
				var html = await response.Content.ReadAsStringAsync();
				
				if (!response.IsSuccessStatusCode)
				{
					await command.RespondAsync("Sorry, could not retrieve fish list...");
					return;
				}

				var htmlDocument = new HtmlDocument();
				htmlDocument.LoadHtml(html);

				var links = htmlDocument.DocumentNode.SelectNodes("//a[@href]")
							.Select(node => node.GetAttributeValue("href", string.Empty))
							.Where(href => href.StartsWith("/wiki/") && !href.Contains(":") && !href.Contains("#") && !href.Contains("?"))
							.Distinct()
							.ToList();

				if (links.Count <= 165)
				{
					await command.RespondAsync("Sorry, no fish right now...");
					return;
				}

				links.RemoveRange(0, 7); // Remove wikipedia links
				links.RemoveRange(links.Count - 158, 158); // Remove "See Also" links

				var random = new Random();
				var randomLink = links[random.Next(links.Count)];

				var pageResponse = await httpClient.GetAsync($"https://en.wikipedia.org{randomLink}");
				var finalUrl = pageResponse.RequestMessage.RequestUri.ToString();

				var pageName = finalUrl.Split("/wiki/").Last();
				var infoPageLink = $"https://en.wikipedia.org/w/index.php?title={pageName}&action=info";

				var infoResponse = await httpClient.GetAsync(infoPageLink);
				var infoHtml = await infoResponse.Content.ReadAsStringAsync();
				var infoDocument = new HtmlDocument();
				infoDocument.LoadHtml(infoHtml);

				var viewNode = infoDocument.DocumentNode.SelectSingleNode("//tr[@id='mw-pvi-month-count']/td/div/a");
				var monthlyViews = int.Parse( viewNode?.InnerText.Trim().Replace( ",", "" ) );

				await command.RespondAsync($"Here's a random fish: {finalUrl} (Information: {infoPageLink}, Monthly Views: {monthlyViews})");
			}
		}
	}
}