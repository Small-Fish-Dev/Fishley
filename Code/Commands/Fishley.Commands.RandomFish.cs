
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
			{ "report_fish_issue-", ReportIssue },
			{ "disarm_mine", DisarmMine },
			{ "hi_killerfish", HelloKillerfish }
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

			var isSeaMine = passed <= 15 && new Random((int)DateTime.UtcNow.Ticks).Next(100) <= 10; // 10% chance of sea mine
			var isKillerFish = passed <= 20 && new Random((int)DateTime.UtcNow.Ticks).Next(100) <= 5;

			if (isKillerFish)
			{
				var embed = KillerfishEmbed(command.User, false);

				var button = new ComponentBuilder()
					.WithButton("Say HI to Killer Fish", $"hi_killerfish", ButtonStyle.Danger)
					.Build();

				await command.RespondAsync($"<@{command.User.Id}> CAUGHT KILLER FISH! SAY HI NOW AND HE MIGHT SPARE YOUR WALLET", embed: embed, components: button);

				_ = KillerFishEncounter(command);
			}
			else if (isSeaMine)
			{
				var embed = SeaMineEmbed(command.User, false, false);

				var button = new ComponentBuilder()
					.WithButton("DISARM", $"disarm_mine", ButtonStyle.Danger)
					.Build();

				await command.RespondAsync($"<@{command.User.Id}> You caught a naval mine! You better disarm it before it explodes!", embed: embed, components: button);

				_ = StartMine(command);
			}
			else
			{

				var luck = (int)(Math.Min((float)passed, 21600f) / 21600f); // 3 hours = 0.5 luck, 6 hours = 1.0 luck - 21600 = 6 hours
				var randomFish = await GetRandomFishFromRarity(new ListSelector().SelectItem(FishRarities, 3 + luck * 17, 6).Key);
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
		}

		public async Task ReportIssue(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Issue reported.", "im_nothing_bro", style: ButtonStyle.Secondary, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);

			var fishId = int.Parse(component.Data.CustomId.Split("-").Last());
			_ = await component.FollowupAsync($"Reported an issue with fish #{fishId}! Thank you!", ephemeral: true); // Gotta respond within 3 seconds so I'll handle the logic later

			var fishFound = await GetFish(fishId);
			fishFound.IssuesReported++;
			await UpdateOrCreateFish(fishFound);
		}

		public async Task DisarmMine(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Disarmed.", "im_nothing_bro", style: ButtonStyle.Danger, disabled: true)
				.Build();

			await component.UpdateAsync(x =>
			{
				x.Embed = SeaMineEmbed(component.User, false, true);
				x.Components = disabledButton;
			});

			_ = await component.FollowupAsync($"Sea mine disarmed.", ephemeral: true);
		}

		public async Task KillerFishEncounter(SocketSlashCommand command)
		{
			await Task.Delay(5000);

			var response = await command.GetOriginalResponseAsync();

			if (!response.Embeds.First().Fields.Any(x => x.Value.Contains("You are spared.")) && !response.Embeds.First().Fields.Any(x => x.Value.Contains("FURIOUS."))) // I have came up with this solution myself, I'm so good at this.
			{
				var disabledButton = new ComponentBuilder()
					.WithButton("It's too late...", "errm_what_the_scallop", style: ButtonStyle.Danger, disabled: true)
					.Build();

				await command.ModifyOriginalResponseAsync(x =>
				{
					x.Embed = KillerfishEmbed(command.User, false);
					x.Components = disabledButton;
				});

				var user = await GetOrCreateUser(command.User.Id);
				var moneyLost = user.Money / 8M;

				await command.FollowupAsync($"Too bad! <@{command.User.Id}> ignored Killer Fish and he is VERY ANGRY now! User has been ROBBED, they lost {NiceMoney((float)moneyLost)}! Better say Hi next time...");

				user.Money = user.Money - moneyLost;
				await UpdateUser(user);
			}
		}
		public async Task HelloKillerfish(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Killer Fish was greeted.", "fishy_business", style: ButtonStyle.Danger, disabled: true)
				.Build();

			if (Random.Shared.NextDouble() >= 0.6) // I'm not sure if it works, but if it does, this should make Killer Fish be more often merciful than be mean.
			{
				await component.UpdateAsync(x =>
				{
					x.Embed = KillerfishEmbed(component.User, true);
					x.Components = disabledButton;
				});

				_ = await component.FollowupAsync($"Killer Fish has been greeted by **{component.User.GlobalName}** and he seems to be happy. {component.User.GlobalName} is now safe.", ephemeral: false);
			}
			else // You might not get spared by Killer Fish even if you say Hi, but in this case he takes away less money.
			{
				await component.UpdateAsync(x =>
				{
					x.Embed = KillerfishEmbed(component.User, false, true);
					x.Components = disabledButton;
				});

				var user = await GetOrCreateUser(component.User.Id);
				var moneyLost = user.Money / 15M;

				_ = await component.FollowupAsync($"Oh no! **{component.User.GlobalName}** greeted Killer Fish, but Killer Fish is MERCILESS this time. <@{component.User.Id}> lost {NiceMoney((float)moneyLost)}...", ephemeral: false);

				user.Money = user.Money - moneyLost;
				await UpdateUser(user);
			}
		}

		public async Task StartMine(SocketSlashCommand command)
		{
			// TODO Disarm only if creator
			await Task.Delay(10000);

			var response = await command.GetOriginalResponseAsync();

			if (!response.Embeds.First().Fields.Any(x => x.Value.Contains("Disarmed."))) // Lazy way to check if it wasn't disarmed, it works!!
			{
				var disabledButton = new ComponentBuilder()
					.WithButton("Exploded.", "im_nothing_bro", style: ButtonStyle.Danger, disabled: true)
					.Build();

				await command.ModifyOriginalResponseAsync(x =>
				{
					x.Embed = SeaMineEmbed(command.User, true, false);
					x.Components = disabledButton;
				});

				var user = await GetOrCreateUser(command.User.Id);
				var moneyLost = user.Money / 10M; // 10% of the user's money

				await command.FollowupAsync($"<@{command.User.Id}> didn't disarm the Naval Mine and lost {NiceMoney((float)moneyLost)}!");

				user.Money = user.Money - moneyLost;
				await UpdateUser(user);
			}
		}

		public Embed SeaMineEmbed(SocketUser user, bool exploded, bool disarmed)
		{
			var embedBuilder = new EmbedBuilder()
				.WithTitle("YOU GOT A NAVAL MINE")
				.WithAuthor(user)
				.WithDescription("https://en.wikipedia.org/wiki/Naval_mine")
				.AddField("Common Name:", "Sea Mine")
				.AddField("Scientific Name:", "Moored contact mine")
				.WithColor(Color.Red)
				.AddField("What now?", "Disarm this within 10 second or else it will explode!")
				.AddField("Explodes:", exploded ? "Exploded." : (disarmed ? "Disarmed." : $"<t:{((DateTimeOffset)DateTime.UtcNow.AddSeconds(10)).ToUnixTimeSeconds()}:R>"))
				.WithImageUrl(exploded ? "https://upload.wikimedia.org/wikipedia/commons/0/09/Operation_Crossroads_Baker_Edit.jpg" : "https://upload.wikimedia.org/wikipedia/commons/thumb/0/03/Mine_%28AWM_304925%29.jpg/220px-Mine_%28AWM_304925%29.jpg");

			return embedBuilder.Build();
		}

		public Embed KillerfishEmbed(SocketUser user, bool isUserSpared, bool isRejected = false)
		{
			var embedBuilder = new EmbedBuilder()
				.WithTitle("KILLER FISH APPEARS")
				.WithAuthor(user)
				.WithDescription("https://www.youtube.com/watch?v=MesY1X9iYks")
				.AddField("Common Name:", "Killer Fish from San Diego")
				.AddField("Scientific Name:", "Interfectorem Pisces a San Diego")
				.WithColor(Color.Red)
				.AddField("What now?", "Killer Fish is serious, and nobody knows what's in his mind! Say Hi to Killer Fish, maybe he's in a good mood today.")
				.AddField("Killer Fish verdict:", isUserSpared ? "Killer Fish is feeling kind today. You are spared." : isRejected ? "Killer Fish is FURIOUS. No FORGIVENESS." : $"You have <t:{((DateTimeOffset)DateTime.UtcNow.AddSeconds(5)).ToUnixTimeSeconds()}:R> before catastrophic consequences.")
				.WithImageUrl("https://wheatleymf.net/killerfish1.jpg");

			return embedBuilder.Build();
		}
	}
}