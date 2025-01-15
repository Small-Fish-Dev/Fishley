namespace Fishley;

public partial class Fishley
{
	public class WarnMessageCommand : DiscordMessageCommand
	{
		public override MessageCommandBuilder Builder => new MessageCommandBuilder()
		.WithName("Warn")
		.WithDefaultMemberPermissions(GuildPermission.KickMembers);

		public override Func<SocketMessageCommand, Task> Function => Warn;

		public override bool SpamOnly => false;

		public async Task Warn(SocketMessageCommand command)
		{
			var channel = (SocketTextChannel)command.Channel;
			var message = command.Data.Message;
			var target = (SocketGuildUser)message.Author;

			await HandleWarn( message, target, channel, (SocketGuildUser)command.User, (IUserMessage)message);
        	await command.RespondAsync("User warned", ephemeral: true );
		}
	}
}