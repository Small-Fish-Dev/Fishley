namespace Fishley;

public partial class Fishley
{
	public class ShutdownCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("shutdown")
		.WithDescription("Emergency shutdown for Fishley")
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => Shutdown;

		public override bool SpamOnly => false;

		public async Task Shutdown(SocketSlashCommand command)
		{

			if (IsAdmin((SocketGuildUser)command.User))
			{
				await command.RespondAsync($"Emergency shutdown activated, shutting down.");
				Running = false;
			}
			else
				await command.RespondAsync($"You can't use this command?", ephemeral: true);
		}
	}

	public class RestartCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("restart")
		.WithDescription("Restart Fishley")
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => Restart;

		public override bool SpamOnly => false;

		public async Task Restart(SocketSlashCommand command)
		{

			if (IsAdmin((SocketGuildUser)command.User))
			{
				await command.RespondAsync($"Restarting protocol initiated. Restarting.");
				Environment.Exit(127);
			}
			else
				await command.RespondAsync($"You can't use this command?", ephemeral: true);
		}
	}

	public class EmergencyCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("emergency")
		.WithDescription("Set Fishley to emergency mode")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("enabled")
			.WithDescription("Enabled or Disabled")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.Boolean))
		.WithDefaultMemberPermissions(GuildPermission.Administrator);

		public override Func<SocketSlashCommand, Task> Function => Emergancy;

		public override bool SpamOnly => false;

		public async Task Emergancy(SocketSlashCommand command)
		{
			var enabled = (bool)command.Data.Options.First().Value;

			if (IsAdmin((SocketGuildUser)command.User))
			{
				await command.RespondAsync($"EMERGENCY PROTOCOL {(enabled ? "ACTIVATED" : "DISACTIVATED")}");
				Emergency = enabled;
			}
			else
				await command.RespondAsync($"You can't use this command?", ephemeral: true);
		}
	}
}