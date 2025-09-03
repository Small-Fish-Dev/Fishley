namespace Fishley;

public partial class Fishley
{
	public class ShutdownCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("shutdown")
		.WithDescription("Emergency shutdown for Fishley")
		.WithDefaultMemberPermissions(GuildPermission.BanMembers);

		public override Func<SocketSlashCommand, Task> Function => Shutdown;

		public override bool SpamOnly => false;

		public async Task Shutdown(SocketSlashCommand command)
		{

			if (IsSmallFish((SocketGuildUser)command.User))
			{
				await command.RespondAsync($"Emergency shutdown activated, shutting down.");
				Running = false;
				DebugSay( command.User.GetUsername() + " used shutdown" );
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
		.WithDefaultMemberPermissions(GuildPermission.BanMembers);

		public override Func<SocketSlashCommand, Task> Function => Restart;

		public override bool SpamOnly => false;

		public async Task Restart(SocketSlashCommand command)
		{

			if (IsSmallFish((SocketGuildUser)command.User))
			{
				await command.RespondAsync($"Restarting protocol initiated. Restarting.");
				DebugSay( command.User.GetUsername() + " used restart" );
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
			.WithName("rule")
			.WithDescription("Describe which messages break the rules and Fishley should punish.")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("punishment")
			.WithDescription("What should Fishley do to messages that break the rule.")
			.WithRequired(false)
			.AddChoice("Warn", 0)
			.AddChoice("Timeout", 1)
			.AddChoice("Delete", 2)
			.AddChoice("Warn and Timeout", 3)
			.AddChoice("Delete and Timeout", 4)
			.AddChoice("Kick", 5)
			.WithType(ApplicationCommandOptionType.Integer))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("prompt")
			.WithDescription("Does Fishley need his prompt to understand the context of the rule?")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.Boolean))
		.WithDefaultMemberPermissions(GuildPermission.BanMembers);

		public override Func<SocketSlashCommand, Task> Function => Emergancy;

		public override bool SpamOnly => false;

		public async Task Emergancy(SocketSlashCommand command)
		{

			if (IsSmallFish((SocketGuildUser)command.User))
			{
				var enabled = !Emergency;
				DebugSay( command.User.GetUsername() + " used emergency mode" );

				if (enabled)
				{
					if (command.Data.Options.Count() < 2)
					{
						await command.RespondAsync($"To enable you have to define a rule and a punishment.", ephemeral: true);
						return;
					}

					var rule = (string)command.Data.Options.ToArray()[0].Value;
					var punishment = (long)command.Data.Options.ToArray()[1].Value;
					var usePrompt = command.Data.Options.Count() == 3 ? (bool)command.Data.Options.Last().Value : false;
					var punishmentName = punishment switch
					{
						0 => "Warn",
						1 => "Timeout",
						2 => "Delete",
						3 => "Warn and Timeout",
						4 => "Delete and Timeout",
						5 => "Kick",
						_ => "None"
					};

					await command.RespondAsync($"EMERGENCY PROTOCOL: **__ACTIVATED__**\nRule: `{rule}`\nPushiment: `{punishmentName}`\nFeed Prompt: `{usePrompt}`");
					Rule = rule;
					Punishment = punishment;
					UsePrompt = usePrompt;
				}
				else
				{
					await command.RespondAsync($"EMERGENCY PROTOCOL: **__DISACTIVATED__**");
				}

				Emergency = enabled;
			}
			else
				await command.RespondAsync($"You can't use this command?", ephemeral: true);
		}
	}
}