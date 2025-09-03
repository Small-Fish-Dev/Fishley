namespace Fishley;

public partial class Fishley
{
	public static DateTime LastGrug { get; set; } = DateTime.UnixEpoch;
	public static int SecondsSinceLastGrug => (int)(DateTime.UtcNow - LastGrug).TotalSeconds;

	public class GrugCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("grug")
		.WithDescription("Put grug in a situation")
		.WithDefaultMemberPermissions( GuildPermission.BanMembers )
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("prompt")
			.WithDescription("The situation, be specific and mention characters and settings")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => GrugImage;

		public override bool SpamOnly => false;

		public async Task GrugImage(SocketSlashCommand command)
		{
			await command.RespondAsync( "Grugging it up...", ephemeral: true );
			LastGrug = DateTime.Now;
			var prompt = (string)command.Data.Options.First().Value;
			GrugMessage( prompt, (SocketTextChannel)command.Channel );
		}
	}
}