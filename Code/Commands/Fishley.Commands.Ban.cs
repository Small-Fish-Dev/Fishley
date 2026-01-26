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
			var userOption   = command.Data.Options.FirstOrDefault(o => o.Name == "user");
			var reasonOption = command.Data.Options.FirstOrDefault(o => o.Name == "reason");
			var daysOption   = command.Data.Options.FirstOrDefault(o => o.Name == "days");

			var foundUser = userOption?.Value as SocketUser;
			var reason    = reasonOption?.Value as string;

			int days = 0;
			if (daysOption?.Value is long daysLong)
				days = (int)daysLong;

			if (foundUser is null)
			{
				await command.RespondAsync("You must specify a user to ban.", ephemeral: true);
				return;
			}

			var targetUser = foundUser as SocketGuildUser;
			if (targetUser != null)
			{
				if (IsSmallFish(targetUser))
				{
					await command.RespondAsync("You can't ban Small Fish, ask an admin for that.", ephemeral: true);
					return;
				}

				if (targetUser.Id == command.User.Id)
				{
					await command.RespondAsync("You can't ban yourself!", ephemeral: true);
					return;
				}
			}

			if (string.IsNullOrWhiteSpace(reason))
			{
				await command.RespondAsync("You need to give a reason.", ephemeral: true);
				return;
			}

			if (days < 1)
			{
				await command.RespondAsync("Minimum duration is 1 day.", ephemeral: true);
				return;
			}

			if (days > 3650)
			{
				await command.RespondAsync("Maximum duration is 10 years.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(foundUser.Id);
			target.Banned = true;

			var unbanDate = DateTime.UtcNow.AddDays(days);
			target.UnbanDate = unbanDate;

			await UpdateOrCreateUser(target);

			var unbanUnixSeconds = new DateTimeOffset(unbanDate, TimeSpan.Zero).ToUnixTimeSeconds();
			var unbanRelative = $"<t:{unbanUnixSeconds}:R>";

			await command.RespondAsync($"<@{foundUser.Id}> has been banned.", ephemeral: true);

			try
			{
				if (targetUser != null)
				{
					await MessageUser(
						targetUser,
						$"You have been banned from Small Fish for `{days}` day(s)\n" +
						$"Reason: `{reason}`\n" +
						$"Your unban date is {unbanRelative}"
					);
				}
			}
			catch
			{
			}

			var logMsg = $"<@{command.User.Id}> banned <@{foundUser.Id}> for `{days}` day(s)\n" +
				$"Reason: `{reason}`\n" +
				$"The unban date is {unbanRelative}";
			DebugSay(logMsg);
			await ModeratorLog(logMsg);

			await SmallFishServer.AddBanAsync(foundUser, pruneDays: 0, reason: $"{reason} ({days} days)");
		}
	}
}