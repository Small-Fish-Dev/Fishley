namespace Fishley;

public partial class Fishley
{
	public class SpeakCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("speak")
		.WithDescription("Send a message as Fishley")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("channel")
			.WithDescription("Where to send the message")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.Channel))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("message")
			.WithDescription("What to say")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.WithDefaultMemberPermissions(GuildPermission.BanMembers);

		public override Func<SocketSlashCommand, Task> Function => Speak;

		public override bool SpamOnly => false;

		public async Task Speak(SocketSlashCommand command)
		{
			var channel = (SocketTextChannel)command.Data.Options.First().Value;
			var text = (string)command.Data.Options.Last().Value;

			var sent = false;

			if (IsSmallFish((SocketGuildUser)command.User))
			{
				sent = await SendMessage(channel, text);
				DebugSay( command.User.GetUsername() + " made me say " + text);
			}

			await command.RespondAsync($"Message {(sent ? "sent!" : "not sent...")}", ephemeral: true);
		}
	}
}