namespace Fishley;

public partial class Fishley
{
	public class ImageCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("generate")
		.WithDescription("Pay to generate an image from Fishley! ($50 for Dalle2 $100 for Dalle3)")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("prompt")
			.WithDescription("What to generate")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("dalle3")
			.WithDescription("Use Dalle3 instead of Dalle2, better result but will cost you more money!")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.Boolean));

		public override Func<SocketSlashCommand, Task> Function => GenerateImage;

		public override bool SpamOnly => false;

		public async Task GenerateImage(SocketSlashCommand command)
		{
			var prompt = (string)command.Data.Options.First().Value;
			var dalle3 = command.Data.Options.Count() > 1 ? (bool)command.Data.Options.Last().Value : false;

			var storedUser = await GetOrCreateUser(command.User.Id);
			var price = dalle3 ? 1f : 0.5f; // TODO CHANGE TO 100 AND 50

			if ((float)storedUser.Money < price)
			{
				await command.RespondAsync($"You don't have enough money to pay for that! ({NiceMoney(price)})", ephemeral: true);
				return;
			}

			if (await IsTextBreakingRules(prompt))
			{
				await command.RespondAsync($"I can't generate the prompt '{prompt}' as it breaks the rules.");
				return;
			}


			storedUser.Money -= (decimal)price;
			await UpdateOrCreateUser(storedUser);
			await command.RespondAsync($"Paid {NiceMoney(price)} to generate an image depicting: '{prompt}'");

			CreateImage(prompt, dalle3).ContinueWith(x => SendMessage((SocketTextChannel)command.Channel, $"Here's what <@{command.User.Id}> asked: '{prompt}'\n{x.Result}"));
		}
	}
}