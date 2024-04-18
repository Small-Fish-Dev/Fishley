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
			var randomFish = AllFishes.Query().ToList()[rnd.Next(AllFishes.Count())];

			var embed = new EmbedBuilder()
				.WithColor(Color.Blue)
				.WithTitle($"You caught: {randomFish.CommonName}!")
				.WithDescription($"{randomFish.WikiPage}")
				.WithImageUrl( randomFish.ImageLink )
				.WithThumbnailUrl( FishRarities[GetFishRarity( randomFish.MonthlyViews )] )
				.WithCurrentTimestamp()
				.Build();
			
			await command.RespondAsync( embed: embed );
		}
	}
}