namespace Fishley;

public partial class Fishley
{
	public class VerifyAllCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("verifyall")
		.WithDescription("Give all users in the server the Certified Fish role (moderator only)")
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => VerifyAllUsers;

		public override bool SpamOnly => false;

		public async Task VerifyAllUsers(SocketSlashCommand command)
		{
			var user = (SocketGuildUser)command.User;

			// Check if user is a moderator
			if (!CanModerate(user))
			{
				await command.RespondAsync("You don't have permission to use this command.", ephemeral: true);
				return;
			}

			await command.DeferAsync();

			try
			{
				// Get all users in the server
				await SmallFishServer.DownloadUsersAsync();
				var allUsers = SmallFishServer.Users;

				int successCount = 0;
				int skippedCount = 0;
				int errorCount = 0;

				foreach (var member in allUsers)
				{
					// Skip bots
					if (member.IsBot)
					{
						skippedCount++;
						continue;
					}

					// Skip users who already have the role
					if (IsCertifiedFish(member))
					{
						skippedCount++;
						continue;
					}

					// Skip users who have Dirty Ape role
					if (IsDirtyApe(member))
					{
						skippedCount++;
						continue;
					}

					try
					{
						await member.AddRoleAsync(CertifiedFishRole);
						successCount++;

						// Add a small delay to avoid rate limiting
						await Task.Delay(100);
					}
					catch (Exception ex)
					{
						DebugSay($"Failed to add Fish role to {member.GetUsername()}: {ex.Message}");
						errorCount++;
					}
				}

				var responseMessage = $"**Verification Complete!**\n" +
					$"✅ Verified: {successCount} users\n" +
					$"⏭️ Skipped: {skippedCount} users (already verified, bots, or dirty apes)\n" +
					$"❌ Errors: {errorCount} users";

				await command.FollowupAsync(responseMessage);
				await ModeratorLog($"<@{user.Id}> used /verifyall command\n{responseMessage}");
			}
			catch (Exception ex)
			{
				DebugSay($"VerifyAll command failed: {ex.Message}");
				await command.FollowupAsync($"An error occurred while verifying users: {ex.Message}");
			}
		}
	}
}
