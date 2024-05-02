
using System.Reactive.Subjects;

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
			{ "disarm_mine", DisarmMine },
			{ "hi_killerfish", HelloKillerfish },
			{ "release_killerfish", ReleaseKillerfish }
		};

		public override bool SpamOnly => true;

		public async Task GetRandomFish(SocketSlashCommand command)
		{
			var user = await GetOrCreateUser(command.User.Id);
			var now = DateTime.UtcNow;
			var passed = (now - user.LastFish).TotalSeconds;

			if (passed <= 3)
			{
				await command.RespondAsync("You're fishing too much, wait 3 seconds.", ephemeral: true);
				return;
			}

			var badLuck = passed <= 5;
			var isSeaMine = passed <= 5 && Random.Next(100) <= 6; // 6% chance of sea mine
			var isKillerFish = passed <= 10 && Random.Next(100) <= 2; // 2% chance of killer fish

			if (isSeaMine)
			{
				var embed = SeaMineEmbed(command.User, false, false);

				var button = new ComponentBuilder()
					.WithButton("DISARM", $"disarm_mine", ButtonStyle.Danger)
					.Build();

				await command.RespondAsync($"<@{command.User.Id}> You caught a naval mine! You better disarm it before it explodes!", embed: embed, components: button);

				_ = StartMine(command);
			}
			else if (isKillerFish)
			{
				var embed = KillerfishEmbed(command.User, false);

				var button = new ComponentBuilder()
					.WithButton("Hello, Killer Fish", $"hi_killerfish", ButtonStyle.Danger)
					.WithButton("Release him", $"release_killerfish", ButtonStyle.Secondary)
					.Build();

				await command.RespondAsync($"<@{command.User.Id}> caught **KILLER FISH**! Quick, do something, you've got only 5 seconds to think!", embed: embed, components: button);

				_ = KillerFishEncounter(command);
			}
			else
			{

				var maxLuckTime = 60f * 60f * 48f; // 2 days
				var luck = (int)(Math.Min((float)passed, maxLuckTime) / maxLuckTime) * 15 - (badLuck ? 1 : 0);
				var randomAnimal = await GetRandomAnimalFromRarity(new ListSelector().SelectItem(AnimalRarities, 3 + luck, 7));
				var embedTitle = $"{command.User.GlobalName} caught: {randomAnimal.CommonName}!";

				var embed = new AnimalEmbedBuilder(randomAnimal, embedTitle, command.User)
				{
					WikiInfoPage = false,
					CommonName = false,
					MonthlyViews = false
				}.Build();

				var rarity = GetRarity(randomAnimal.MonthlyViews);
				user.LastFish = DateTime.UtcNow;
				user.Money += rarity.Value;

				await UpdateOrCreateUser(user);

				randomAnimal.LastCaught = DateTime.UtcNow;
				await UpdateOrCreateEntry(randomAnimal);

				await command.RespondAsync($"<@{command.User.Id}>", embed: embed);
			}
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
			await Task.Delay(5500);

			var response = await command.GetOriginalResponseAsync();

			if (response.Embeds.First().Fields.Any(x => x.Value.Contains("Killer Fish is waiting"))) // I am not sure if this works, lets test it live LOL!
			{
				var disabledButton = new ComponentBuilder()
					.WithButton("It's too late...", "errm_what_the_scallop", style: ButtonStyle.Danger, disabled: true)
					.Build();

				await command.ModifyOriginalResponseAsync(x =>
				{
					x.Embed = KillerfishEmbed(command.User, false, false, false);
					x.Components = disabledButton;
				});

				var user = await GetOrCreateUser(command.User.Id);
				var moneyLost = user.Money / 8M;

				await command.FollowupAsync($"Too bad! <@{command.User.Id}> ignored Killer Fish and now he's angry, user lost {NiceMoney((float)moneyLost)}! Ignoring will result in losing higher amount of money than by any other interaction, be careful next time!");

				user.Money = user.Money - moneyLost;
				await UpdateOrCreateUser(user);
			}
		}
		public async Task HelloKillerfish(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Killer Fish was greeted", "fishy_business", style: ButtonStyle.Danger, disabled: true)
				.Build();

			// Should review the chance value later, depending on how often people will get blessed/robbed by Killer Fish.
			if (Random.Shared.NextDouble() >= 0.5)
			{
				await component.UpdateAsync(x =>
				{
					x.Embed = KillerfishEmbed(component.User, true, false, false);
					x.Components = disabledButton;
				});

				// If blessed, reward used with a random amount of money, ranging from 3 to 50 coins.
				var user = await GetOrCreateUser(component.User.Id);
				var MoneyToAdd = Random.Shared.Next(3, 80);

				_ = await component.FollowupAsync($"Killer Fish has been greeted by **{component.User.GlobalName}**, and he is feeling **kind** today! <@{component.User.Id}> receives {NiceMoney((float)MoneyToAdd)} as a blessing gift.", ephemeral: false);

				user.Money = user.Money + MoneyToAdd;
				await UpdateOrCreateUser(user);
			}
			else
			{
				await component.UpdateAsync(x =>
				{
					x.Embed = KillerfishEmbed(component.User, false, false, false);
					x.Components = disabledButton;
				});

				// Make amount of money robbed randomized as well.
				var user = await GetOrCreateUser(component.User.Id);
				var moneyLost = user.Money / Random.Shared.Next(10, 17);

				_ = await component.FollowupAsync($"Oh no! **{component.User.GlobalName}** greeted Killer Fish, but Killer Fish shows **NO FORGIVENESS**. <@{component.User.Id}> was robbed by Killer Fish and lost {NiceMoney((float)moneyLost)}...", ephemeral: false);

				user.Money = user.Money - moneyLost;
				await UpdateOrCreateUser(user);
			}
		}

		public async Task ReleaseKillerfish(SocketMessageComponent component)
		{
			var disabledButton = new ComponentBuilder()
				.WithButton("Killer Fish has been released", "fishy_business", style: ButtonStyle.Secondary, disabled: true)
				.Build();

			await component.UpdateAsync(x =>
			{
				x.Embed = KillerfishEmbed(component.User, false, true, false);
				x.Components = disabledButton;
			});

			_ = await component.FollowupAsync($"<@{component.User.Id}> released Killer Fish back into the ocean.", ephemeral: false);
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
				await UpdateOrCreateUser(user);
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

		public Embed KillerfishEmbed(SocketUser user, bool isUserSpared, bool isReleased = false, bool isAwaitingResponse = true)
		{
			var embedBuilder = new EmbedBuilder()
				.WithTitle("KILLER FISH APPEARS")
				.WithAuthor(user)
				.WithDescription("https://www.youtube.com/watch?v=MesY1X9iYks")
				.AddField("Common Name:", "Killer Fish from San Diego")
				.AddField("Scientific Name:", "Interfectorem Pisces a San Diego")
				.WithColor(Color.Red)
				.AddField("What now?", "You have **5 seconds** to safely release Killer Fish, or you can say 'Hello' and try get some extra money! However, Killer Fish can be angry and take some of your money instead, so do it at your own risk... Ignoring will result in losing a significant portion of your money.")
				.AddField("Verdict:", isAwaitingResponse ? $"Killer Fish is waiting... <t:{((DateTimeOffset)DateTime.UtcNow.AddSeconds(5)).ToUnixTimeSeconds()}:R> left!" : isReleased ? "Killer Fish has been released back into the ocean. Everyone is safe... until next time." : isUserSpared ? $"**{user.Username}** risked it all and got a blessing gift from Killer Fish!" : $"Killer Fish was angry and robbed **{user.Username}**...")
				.WithImageUrl("https://wheatleymf.net/killerfish1.jpg");

			return embedBuilder.Build();
		}
	}
}