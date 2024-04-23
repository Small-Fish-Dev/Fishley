namespace Fishley;

public partial class Fishley
{
	public class RandomFishCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("fish")
		.WithDescription("Get a random fish");

		public override Func<SocketSlashCommand, Task> Function => GetRandomFish;

		public override bool SpamOnly => true;

		public async Task GetRandomFish(SocketSlashCommand command)
		{
			var user = await GetOrCreateUser(command.User.Id);
			var now = DateTime.UtcNow;
			var passed = (now - user.LastFish).TotalSeconds;

			if (passed <= 5)
			{
				await command.RespondAsync("You're fishing too much, wait 5 seconds.", ephemeral: true);
				return;
			}

			var randomFish = await GetRandomFishFromRarity(new ListSelector().SelectItem(FishRarities, 5, 5).Key);
			var embedTitle = $"{command.User.GlobalName} caught: {randomFish.CommonName}!";

			var embed = new FishEmbedBuilder(randomFish, embedTitle, command.User)
			{
				WikiInfoPage = false,
				CommonName = false,
				MonthlyViews = false
			}.Build();

			var rarity = FishRarities[randomFish.Rarity];
			user.LastFish = DateTime.UtcNow;
			user.Money += rarity.Item3;

			await UpdateUser(user);

			randomFish.LastSeen = DateTime.UtcNow;
			await UpdateOrCreateFish(randomFish);

			Console.WriteLine($"{command.User.GlobalName} caught: {randomFish.PageId} {randomFish.CommonName} - {randomFish.WikiPage} - {randomFish.PageName} - {randomFish.MonthlyViews} - {randomFish.ImageLink}");
			await command.RespondAsync($"<@{command.User.Id}>", embed: embed);
		}
	}
}