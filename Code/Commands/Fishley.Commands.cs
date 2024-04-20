namespace Fishley;

public partial class Fishley
{
	public static Dictionary<string, DiscordSlashCommand> Commands = new()
	{
		{ "fish", new RandomFishCommand() },
		{ "speak", new SpeakCommand() },
		{ "balance", new BalanceCommand() }
	};

	private static async Task SlashCommandHandler( SocketSlashCommand command )
	{
		var name = command.Data.Name;
		var channel = command.ChannelId;

		if ( Commands.ContainsKey( name ) )
		{
			var commandClass = Commands[name];

			if ( commandClass.SpamOnly && channel != SpamChannel )
			{
				await command.RespondAsync( $"You can only use this command in the <#{SpamChannel}> channel.", ephemeral: true );
				return;
			}

			await Commands[name].Function.Invoke( command );
		}
		else
			await command.RespondAsync( "That command is unavailable. Bug off now!", ephemeral: true );
	}

	public abstract class DiscordSlashCommand
	{
		public virtual SlashCommandBuilder Builder { get; private set; }
		public virtual Func<SocketSlashCommand, Task> Function { get; private set; }
		public virtual bool SpamOnly { get; private set; } = true;
	}
}