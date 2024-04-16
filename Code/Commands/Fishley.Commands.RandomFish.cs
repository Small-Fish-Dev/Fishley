public partial class Fishley
{
	public class RandomFishCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName( "fish" )
		.WithDescription( "Get a random fish" );

		public override Func<SocketSlashCommand, Task> Function => GetRandomFish;

		public override bool SpamOnly => true;

		public async Task GetRandomFish(SocketSlashCommand command)
		{
			var rnd = new Random( (int)DateTime.UtcNow.Ticks );
			var randomFish = AllFish[rnd.Next(AllFish.Count())];

            HttpResponseMessage response = await HttpClient.GetAsync(randomFish.WikiPage);
            response.EnsureSuccessStatusCode();
            string htmlContent = await response.Content.ReadAsStringAsync();

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

			string imageUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/6/65/No-Image-Placeholder.svg/832px-No-Image-Placeholder.svg.png"; // Placeholder
            var pageImage = htmlDoc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

            if (pageImage != null)
                imageUrl = pageImage.GetAttributeValue("content", string.Empty);
			
			string title = "";
			// Extracting the title of the page
            var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
            {
                title = titleNode.InnerText;
                // Optional: Remove the " - Wikipedia" suffix
                title = title.Replace(" - Wikipedia", "");
                Console.WriteLine("Page Title: " + title);
            }
            else
            {
                Console.WriteLine("No title found.");
            }

			var embed = new EmbedBuilder()
				.WithColor(Color.Blue)
				.WithTitle($"You caught: {title}!")
				.WithDescription($"{randomFish.WikiPage}")
				.WithImageUrl( imageUrl )
				.WithThumbnailUrl( FishRarityThumbnails[randomFish.Rarity] )
				.WithCurrentTimestamp()
				.Build();
			
			await command.RespondAsync( embed: embed );
		}
	}
}