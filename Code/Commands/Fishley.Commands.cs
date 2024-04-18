public partial class Fishley
{
	public static Dictionary<string, DiscordSlashCommand> Commands = new()
	{
		{ "fish", new RandomFishCommand() },
		{ "speak", new SpeakCommand() }
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

	
	public class SpeakCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName( "speak" )
		.WithDescription( "Send a message as Fishley" )
		.AddOption( new SlashCommandOptionBuilder()
			.WithName( "channel" )
			.WithDescription( "Where to send the message" )
			.WithRequired( true )
			.WithType( ApplicationCommandOptionType.Channel ) )
		.AddOption( new SlashCommandOptionBuilder()
			.WithName( "message" )
			.WithDescription( "What to say" )
			.WithRequired( true )
			.WithType( ApplicationCommandOptionType.String ) )
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => Speak;

		public override bool SpamOnly => false;

		public async Task Speak(SocketSlashCommand command)
		{
			var channel = (SocketTextChannel)command.Data.Options.First().Value;
			var text = (string)command.Data.Options.Last().Value;

			var sent = false;
			
			if ( IsAdmin( (SocketGuildUser)command.User ) )
				sent = await SendMessage( channel, text );
				
			await command.RespondAsync( $"Message {(sent ? "sent!" : "not sent...")}", ephemeral: true );
		}
	}
}