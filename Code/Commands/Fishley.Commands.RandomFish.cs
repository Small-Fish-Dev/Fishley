
namespace Fishley;

public partial class Fishley
{
	public class RandomFishCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("fish")
		.WithDescription("Get a random fish");

		public override Func<SocketSlashCommand, Task> Function => GetRandomFish;
		public override Dictionary<string, Func<SocketMessageComponent, Task>> Components => new()
		{
			{ "report_fish_issue-", ReportIssue }
		};

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

			var luck = (int)(Math.Min((float)passed, 5f) / 5f); // 3 hours = 0.5 luck, 6 hours = 1.0 luck - 21600 = 6 hours
			var randomFish = await GetRandomFishFromRarity(new ListSelector().SelectItem(FishRarities, 5 + luck * 15, 5).Key);
			var embedTitle = $"{command.User.GlobalName} caught: {randomFish.CommonName}!";

			var embed = new FishEmbedBuilder(randomFish, embedTitle, command.User)
			{
				WikiInfoPage = false,
				CommonName = false,
				MonthlyViews = false,
				IssuesReported = false
			}.Build();

			var rarity = FishRarities[randomFish.Rarity];
			user.LastFish = DateTime.UtcNow;
			user.Money += rarity.Item3;

			await UpdateUser(user);

			randomFish.LastSeen = DateTime.UtcNow;
			await UpdateOrCreateFish(randomFish);

			var button = new ComponentBuilder()
				.WithButton("Report issue.", $"report_fish_issue-{randomFish.PageId}", ButtonStyle.Secondary)
				.Build();

			Console.WriteLine($"{command.User.GlobalName} caught: {randomFish.PageId} {randomFish.CommonName} - {randomFish.WikiPage} - {randomFish.PageName} - {randomFish.MonthlyViews} - {randomFish.ImageLink}");
			await command.RespondAsync($"<@{command.User.Id}> If you see anything wrong or missing with this fish please report the issue with the button below!", embed: embed, components: button);
		}

		public async Task ReportIssue(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Issue reported.", "im_nothing_bro", style: ButtonStyle.Secondary, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);

			var fishId = int.Parse(component.Data.CustomId.Split("-").Last());
			await component.FollowupAsync($"Reported an issue with fish #{fishId}! Thank you!", ephemeral: true); // Gotta respond within 3 seconds so I'll handle the logic later

			var fishFound = await GetFish(fishId);
			fishFound.IssuesReported++;
			await UpdateOrCreateFish(fishFound);
		}
	}
}