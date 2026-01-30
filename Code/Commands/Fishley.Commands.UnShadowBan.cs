namespace Fishley;

public partial class Fishley
{
	public class UnShadowBanCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("unshadowban")
		.WithDescription("Release someone from the shadow realm")
		.WithDefaultMemberPermissions( GuildPermission.BanMembers )
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to unshadowban")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User));

		public override Func<SocketSlashCommand, Task> Function => UnShadowBan;

		public override bool SpamOnly => false;

		public async Task UnShadowBan(SocketSlashCommand command)
		{
			var userOption = command.Data.Options.FirstOrDefault(o => o.Name == "user");
			var foundUser = userOption?.Value as SocketUser;

			if (foundUser is null)
			{
				await command.RespondAsync("You must specify a user to unshadowban.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(foundUser.Id);

			if (!target.ShadowBanned)
			{
				await command.RespondAsync($"<@{foundUser.Id}> is not shadow banned.", ephemeral: true);
				return;
			}

			// Remove shadow ban from database
			target.ShadowBanned = false;
			target.ShadowUnbanDate = DateTime.MinValue;
			await UpdateOrCreateUser(target);

			// Remove Banished role if user is in the server
			var targetUser = foundUser as SocketGuildUser;
			if (targetUser != null)
			{
				try
				{
					await targetUser.RemoveRoleAsync(BanishedRole);
				}
				catch (Exception ex)
				{
					DebugSay($"Failed to remove Banished role from {targetUser.GetUsername()}: {ex.Message}");
				}
			}

			// Respond publicly
			await command.RespondAsync($"<@{foundUser.Id}> has been released from the shadow realm!");

			// Send DM to user
			try
			{
				if (targetUser != null)
				{
					await MessageUser(
						targetUser,
						"You have been released from the shadow realm! Welcome back to the light."
					);
				}
			}
			catch
			{
			}

			var logMsg = $"<@{command.User.Id}> unshadowbanned <@{foundUser.Id}>";
			DebugSay(logMsg);
			await ModeratorLog(logMsg);
		}
	}
}
