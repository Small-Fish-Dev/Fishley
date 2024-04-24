namespace Fishley;

public partial class Fishley
{
	public static Dictionary<string, DiscordSlashCommand> Commands = new()
	{
		{ "fish", new RandomFishCommand() },
		{ "speak", new SpeakCommand() },
		{ "balance", new BalanceCommand() },
		{ "fish_database", new EditFish() },
		{ "pay", new PayCommand() }
	};

	private static async Task SlashCommandHandler(SocketSlashCommand command)
	{
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
		var componentId = component.Data.CustomId;
		if (componentId == null) return; // Unpossible

		foreach (var command in Commands)
		{
			var componentFound = command.Value.Components.FirstOrDefault(x => componentId.Contains(x.Key));

			if (componentFound.Equals(default(KeyValuePair<string, Func<SocketMessageComponent, Task>>))) return; // Not found

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