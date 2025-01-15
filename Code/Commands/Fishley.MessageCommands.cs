namespace Fishley;

public partial class Fishley
{
	public static Dictionary<string, DiscordMessageCommand> MessageCommands = new()
	{
		{ "Warn", new WarnMessageCommand() },
	};

	private static async Task MessageCommandHandler(SocketMessageCommand  command)
	{
		if (!Running) return;

		var name = command.Data.Name;
		var channel = command.Channel;
		if (MessageCommands.ContainsKey(name))
		{
			var commandClass = MessageCommands[name];

			if (commandClass.SpamOnly && channel != SpamChannel)
			{
				await command.RespondAsync($"You can only use this command in the <#{SpamChannel}> channel.", ephemeral: true);
				return;
			}

			await MessageCommands[name].Function.Invoke(command);
		}
		else
			await command.RespondAsync("That command is unavailable. Bug off now!", ephemeral: true);
	}

	public abstract class DiscordMessageCommand
	{
		public virtual MessageCommandBuilder Builder { get; private set; }
		public virtual Func<SocketMessageCommand, Task> Function { get; private set; }
		public virtual bool SpamOnly { get; private set; } = true;
	}
}