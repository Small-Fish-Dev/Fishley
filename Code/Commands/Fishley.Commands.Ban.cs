namespace Fishley;

public partial class Fishley
{
	public class BanCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("ban")
		.WithDescription("Ban someone from the server")
		.WithDefaultMemberPermissions( GuildPermission.BanMembers )
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to ban")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("reason")
			.WithDescription("Reason for the ban")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("days")
			.WithDescription("How many days to ban for")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.Integer));


		public override Func<SocketSlashCommand, Task> Function => Ban;

		public override bool SpamOnly => false;

		public async Task Ban(SocketSlashCommand command)
		{
			var targetUser = (SocketGuildUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;
			var reason = (string)command.Data.Options.FirstOrDefault(x => x.Name == "reason")?.Value ?? null;
			var days = (int)(long)(command.Data.Options.FirstOrDefault(x => x.Name == "days")?.Value ?? 0L);

			if (IsSmallFish( targetUser ))
			{
				await command.RespondAsync("You can't ban Small Fish, ask an admin for that", ephemeral: true);
				return;
			}
			if (targetUser.Id == command.User.Id)
			{
				await command.RespondAsync("You can't ban yourself!", ephemeral: true);
				return;
			}
			if ( string.IsNullOrWhiteSpace( reason ) )
			{
				await command.RespondAsync("You need to give a reason", ephemeral: true);
				return;
			}
			if ( days < 1 )
			{
				await command.RespondAsync("Minimum duration is 1 day", ephemeral: true);
				return;
			}
			if ( days > 3650 )
			{
				await command.RespondAsync("Maximum duration is 10 years", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(targetUser.Id);

			target.Banned = true;
			var unbanDate = DateTime.UtcNow.AddDays( days );
			target.UnbanDate = unbanDate;

			await UpdateOrCreateUser(target);
			await SmallFishServer.AddBanAsync( targetUser, 0, $"{reason} ({days.ToString()} days)" );
			await command.RespondAsync($"<@{targetUser.Id}> has been banned.", ephemeral: true);
			var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			var sinceEpoch = unbanDate - unixEpoch;
			var unbanRelative = $"<t:{(int)sinceEpoch.TotalSeconds}:R>";
			await MessageUser( targetUser, $"You have been banned from Small Fish for `{days.ToString()} days`\nReason: `{reason}`\nYour unban date is {unbanRelative}" );
			await ModeratorLog( $"<@{command.User.Id}> banned <@{targetUser.Id}> for `{days} days`\nReason: `{reason}`\nThe unban date is {unbanRelative}" );
		}
	}
}