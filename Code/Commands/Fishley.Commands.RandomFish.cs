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
			var user = UserGet( command.User.Id );
			var now = DateTime.UtcNow;
			var passed = (now - DateTime.FromBinary( user.LastFish )).TotalSeconds;

			if ( passed <= 5 )
			{
				await command.RespondAsync( "You're fishing too much, wait 5 seconds.", ephemeral: true );
				return;
			}

			var rnd = new Random( (int)DateTime.UtcNow.Ticks );
			var randomFish = AllFishes.Query().ToList()[rnd.Next(AllFishes.Count())];

			var rarity = FishRarities[GetFishRarity( randomFish.MonthlyViews )];
			var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var sinceEpoch = DateTime.FromBinary( randomFish.LastSeen ) - unixEpoch;
			var lastSeen = $"<t:{sinceEpoch.TotalSeconds}:R>";

			if ( randomFish.LastSeen == 0 )
				lastSeen = "Never!";

			var embed = new EmbedBuilder()
				.WithColor( rarity.Item2 )
				.WithTitle($"{command.User.GlobalName} caught: {randomFish.CommonName}!")
				.AddField( "Common Name:", randomFish.CommonName )
				.AddField( "Scientific Name:", randomFish.PageName )
				.AddField( "Sell amount:", $"${rarity.Item3}" )
				.AddField( "Monthly Views:", randomFish.MonthlyViews.ToString( "N0" ) )
				.AddField( "Last Seen:", lastSeen )
				.WithDescription($"{randomFish.WikiPage}")
				.WithImageUrl( randomFish.ImageLink )
				.WithThumbnailUrl( rarity.Item1 )
				.WithAuthor( command.User )
				.WithCurrentTimestamp()
				.Build();

			randomFish.LastSeen = DateTime.UtcNow.Ticks;
			FishUpdate( randomFish );

			user.LastFish = DateTime.UtcNow.Ticks;
			user.Money += rarity.Item3;
			UserUpdate( user );

			Console.WriteLine( $"{command.User.GlobalName} caught: {randomFish.CommonName} - {randomFish.WikiPage} - {randomFish.PageName} - {randomFish.MonthlyViews} - {randomFish.ImageLink}" );
			
			await command.RespondAsync( embed: embed );
		}
	}
}