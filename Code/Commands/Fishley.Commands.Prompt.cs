namespace Fishley;

public partial class Fishley
{
	public class PromptCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("prompt")
		.WithDescription("Save a custom prompt for Fishley to remember whenever he responds to you. (Reset if not set)")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("prompt")
			.WithDescription("Prompt appended to Fishley responses to you. (Costs $5)")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => SetPrompt;

		public override bool SpamOnly => false;

		public async Task SetPrompt(SocketSlashCommand command)
		{
			var storedUser = await GetOrCreateUser(command.User.Id);

			if (command.Data.Options.Count() == 0)
			{
				storedUser.CustomFishleyPrompt = null;
				await UpdateOrCreateUser(storedUser);
				await command.RespondAsync($"Your prompt has been reset.", ephemeral: true);
				return;
			}
			var prompt = (string)command.Data.Options.First().Value;

			if (prompt.Length > 2000)
			{
				await command.RespondAsync("Max prompt is 2000 characters.", ephemeral: true);
				return;
			}

			if (prompt.Length < 5)
			{
				await command.RespondAsync("Prompt too short.", ephemeral: true);
				return;
			}

			prompt = prompt.Replace("'", "").Replace("DROP", "I'm an idiot").Replace("<script>", "Im an idiot");

			var price = 5;

			if (storedUser.Money < price)
			{
				await command.RespondAsync($"You don't have enough money. ($5)", ephemeral: true);
				return;
			}

			if (await IsTextBreakingRules(prompt))
			{
				await command.RespondAsync($"The prompt is too offensive or breaks our rules.", ephemeral: true);
				return;
			}

			storedUser.Money -= price;
			storedUser.CustomFishleyPrompt = prompt;
			await UpdateOrCreateUser(storedUser);

			await command.RespondAsync($"Custom prompt has been set!");
		}
	}
}