namespace Fishley;

public partial class Fishley
{
	public class ShadowBanCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("shadowban")
		.WithDescription("Banish someone to the shadow realm")
		.WithDefaultMemberPermissions( GuildPermission.BanMembers )
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to shadowban")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("reason")
			.WithDescription("Reason for the shadowban")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("hours")
			.WithDescription("How many hours to shadowban for")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.Integer));


		public override Func<SocketSlashCommand, Task> Function => ShadowBan;

		public override bool SpamOnly => false;

		public async Task ShadowBan(SocketSlashCommand command)
		{
			var userOption   = command.Data.Options.FirstOrDefault(o => o.Name == "user");
			var reasonOption = command.Data.Options.FirstOrDefault(o => o.Name == "reason");
			var hoursOption   = command.Data.Options.FirstOrDefault(o => o.Name == "hours");

			var foundUser = userOption?.Value as SocketUser;
			var reason    = reasonOption?.Value as string;

			int hours = 0;
			if (hoursOption?.Value is long hoursLong)
				hours = (int)hoursLong;

			if (foundUser is null)
			{
				await command.RespondAsync("You must specify a user to shadowban.", ephemeral: true);
				return;
			}

			var targetUser = foundUser as SocketGuildUser;
			if (targetUser != null)
			{
				if (IsSmallFish(targetUser))
				{
					await command.RespondAsync("You can't shadowban Small Fish, ask an admin for that.", ephemeral: true);
					return;
				}

				if (targetUser.Id == command.User.Id)
				{
					await command.RespondAsync("You can't shadowban yourself!", ephemeral: true);
					return;
				}
			}

			if (string.IsNullOrWhiteSpace(reason))
			{
				await command.RespondAsync("You need to give a reason.", ephemeral: true);
				return;
			}

			if (hours < 1)
			{
				await command.RespondAsync("Minimum duration is 1 hour.", ephemeral: true);
				return;
			}

			if (hours > 87600)
			{
				await command.RespondAsync("Maximum duration is 10 years.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(foundUser.Id);
			target.ShadowBanned = true;

			var unbanDate = DateTime.UtcNow.AddHours(hours);
			target.ShadowUnbanDate = unbanDate;

			await UpdateOrCreateUser(target);

			var unbanUnixSeconds = new DateTimeOffset(unbanDate, TimeSpan.Zero).ToUnixTimeSeconds();
			var unbanRelative = $"<t:{unbanUnixSeconds}:R>";

			// Add Banished role if user is still in the server
			if (targetUser != null)
			{
				try
				{
					await targetUser.AddRoleAsync(BanishedRole);
				}
				catch (Exception ex)
				{
					DebugSay($"Failed to add Banished role to {targetUser.GetUsername()}: {ex.Message}");
				}
			}

			// Respond publicly (not ephemeral)
			await command.RespondAsync($"<@{foundUser.Id}> has been banished to the shadow realm for {hours} hour(s)!\nReason: `{reason}`\nThey will return {unbanRelative}");

			try
			{
				if (targetUser != null)
				{
					await MessageUser(
						targetUser,
						$"You have been banished to the shadow realm for `{hours}` hour(s)\n" +
						$"Reason: `{reason}`\n" +
						$"Your return date is {unbanRelative}"
					);
				}
			}
			catch
			{
			}

			var logMsg = $"<@{command.User.Id}> shadowbanned <@{foundUser.Id}> for `{hours}` hour(s)\n" +
				$"Reason: `{reason}`\n" +
				$"The unshadowban date is {unbanRelative}";
			DebugSay(logMsg);
			await ModeratorLog(logMsg);
		}
	}
}
