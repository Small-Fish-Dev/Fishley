namespace Fishley;

public partial class Fishley
{
	public class ThreadCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("thread")
		.WithDescription("Create a new thread in this channel (moderator only)")
		.AddOption("name", ApplicationCommandOptionType.String, "The name of the thread", isRequired: true)
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => CreateThread;

		public override bool SpamOnly => false;

		public async Task CreateThread(SocketSlashCommand command)
		{
			var user = (SocketGuildUser)command.User;

			// Check if user is a moderator
			if (!CanModerate(user))
			{
				await command.RespondAsync("You don't have permission to use this command.", ephemeral: true);
				return;
			}

			var threadName = command.Data.Options.FirstOrDefault(x => x.Name == "name")?.Value.ToString();

			if (string.IsNullOrWhiteSpace(threadName))
			{
				await command.RespondAsync("Please provide a thread name.", ephemeral: true);
				return;
			}

			try
			{
				// Respond first to acknowledge the command
				await command.DeferAsync(ephemeral: true);

				var channel = command.Channel as SocketTextChannel;
				if (channel == null)
				{
					await command.FollowupAsync("This command can only be used in text channels.", ephemeral: true);
					return;
				}

				// Create a thread from the channel
				var thread = await channel.CreateThreadAsync(threadName, ThreadType.PublicThread, ThreadArchiveDuration.OneWeek);

				DebugSay($"<@{user.Id}> created thread '{threadName}' in <#{channel.Id}>");

				// Delete the ephemeral response silently
				await command.DeleteOriginalResponseAsync();
			}
			catch (Exception ex)
			{
				DebugSay($"Thread creation failed: {ex.Message}");
				await command.FollowupAsync($"Failed to create thread: {ex.Message}", ephemeral: true);
			}
		}
	}
}
