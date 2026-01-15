namespace Fishley;

public partial class Fishley
{
	public class SearchCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("search")
		.WithDescription("Ask Fishley a question and he'll search the web for the answer.")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("question")
			.WithDescription("What would you like to know?")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => SearchWeb;

		public override bool SpamOnly => false;

		public async Task SearchWeb(SocketSlashCommand command)
		{
			await command.DeferAsync();

			var question = (string)command.Data.Options.First().Value;
			var user = (SocketGuildUser)command.User;

			try
			{
				var context = new List<string>();
				context.Add($"[The user {user.GetUsername()} has asked you a question. You should use the web search results provided to answer their question in detail and with depth. Make sure to cite sources when relevant and provide a comprehensive answer while maintaining your personality.]");

				var response = await OpenAIChat(question, context, GPTModel.GPT4o, useSystemPrompt: true, enableWebSearch: true);

				await command.FollowupAsync(response);
			}
			catch (Exception ex)
			{
				DebugSay($"Search command failed: {ex.Message}");
				await command.FollowupAsync("Sorry, I encountered an error while searching for that information.");
			}
		}
	}
}
