namespace Fishley;

public partial class Fishley
{
	public class BalanceCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName( "balance" )
		.WithDescription( "How much money do you have" );

		public override Func<SocketSlashCommand, Task> Function => Balance;

		public override bool SpamOnly => false;

		public async Task Balance(SocketSlashCommand command)
		{
			var user = await GetOrCreateUser( command.User.Id );
				
			await command.RespondAsync( $"You have ${user.Money}" );
		}
	}
}