namespace Fishley;

public partial class Fishley
{
	public static Dictionary<string, DiscordSlashCommand> Commands = new()
	{
		{ "fish", new RandomFishCommand() },
		{ "speak", new SpeakCommand() },
		{ "balance", new BalanceCommand() },
		{ "transfer", new TransferCommand() },
		{ "invoice", new InvoiceCommand() },
		{ "leaderboards", new LeaderboardsCommand() },
		{ "finebutton", new FineButton() },
		{ "shutdown", new ShutdownCommand() },
		{ "restart", new RestartCommand() },
		{ "emergency", new EmergencyCommand() },
		{ "recap", new RecapCommand() },
		{ "prompt", new PromptCommand() },
		{ "yapping", new YappingCommand() },
		{ "givemoney", new GiveMoneyCommand() },
		{ "ban", new BanCommand() },
		{ "grug", new GrugCommand() },
		{ "search", new SearchCommand() },
		{ "verifyall", new VerifyAllCommand() },
		{ "thread", new ThreadCommand() }
	};

	private static async Task SlashCommandHandler(SocketSlashCommand command)
	{
		if (!Running) return;

		var name = command.Data.Name;
		var channel = command.Channel;

		if (Commands.ContainsKey(name))
		{
			var commandClass = Commands[name];

			if (commandClass.SpamOnly && channel != SpamChannel)
			{
				await command.RespondAsync($"You can only use this command in the <#{SpamChannel}> channel.", ephemeral: true);
				return;
			}

			await Commands[name].Function.Invoke(command);
		}
		else
			await command.RespondAsync("That command is unavailable. Bug off now!", ephemeral: true);
	}

	private static async Task ButtonHandler(SocketMessageComponent component)
	{
		if (!Running) return;

		var componentId = component.Data.CustomId;
		if (componentId == null) return; // Unpossible

		foreach (var command in Commands)
		{
			if (command.Value.Components == null || command.Value.Components.Count() == 0) continue;
			var componentFound = command.Value.Components.FirstOrDefault(x => componentId.Contains(x.Key));

			if (componentFound.Equals(default(KeyValuePair<string, Func<SocketMessageComponent, Task>>))) continue; // Not found

			await componentFound.Value.Invoke(component);
			return;
		}

		await component.RespondAsync($"Nothing happened...", ephemeral: true);
	}

	public abstract class DiscordSlashCommand
	{
		public virtual SlashCommandBuilder Builder { get; private set; }
		public virtual Func<SocketSlashCommand, Task> Function { get; private set; }
		public virtual bool SpamOnly { get; private set; } = true;
		public virtual Dictionary<string, Func<SocketMessageComponent, Task>> Components { get; private set; }
	}
}