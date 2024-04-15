public partial class Fishley
{
	public static Dictionary<string, DiscordSlashCommand> Commands = new()
	{
		{ "fish", new RandomFishCommand() }
	};

	private static async Task SlashCommandHandler( SocketSlashCommand command )
	{
		var name = command.Data.Name;

		if ( Commands.ContainsKey( name ) )
			await Commands[name].Function.Invoke( command );
		else
			await command.RespondAsync( "That command is unavailable. Bug off now!" );
	}

	public abstract class DiscordSlashCommand
	{
		public virtual SlashCommandBuilder Builder { get; private set; }
		public virtual Func<SocketSlashCommand, Task> Function { get; private set; }
	}
}