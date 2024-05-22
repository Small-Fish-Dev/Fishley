namespace Fishley;

public partial class Fishley
{
	public class RecapCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("recap")
		.WithDescription("Recap the last X messages")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("message_amount")
			.WithDescription("How many messages to recap from")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.Integer))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("question")
			.WithDescription("Do you have a question to ask")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => RecapMessages;

		public override bool SpamOnly => false;

		public async Task RecapMessages(SocketSlashCommand command)
		{
			using (var typing = command.Channel.EnterTypingState())
			{
				var amountToRecap = Convert.ToInt32((long)command.Data.Options.First().Value);
				var question = command.Data.Options.Count() == 2 ? (string)command.Data.Options.Last().Value : null;
				var channel = command.Channel;

				if (amountToRecap < 5)
				{
					await command.RespondAsync("Minimum message amount is 5", ephemeral: true);
					return;
				}
				else if (amountToRecap > 500)
				{
					await command.RespondAsync("Maximum message amount is 500", ephemeral: true);
					return;
				}

				if (question != null && question.Last() != '?')
				{
					await command.RespondAsync("The question has to end with a question mark", ephemeral: true);
					return;
				}

				await command.RespondAsync($"Recapping the last {amountToRecap} messages...");

				var messages = await channel.GetMessagesAsync(amountToRecap).FlattenAsync();
				var recapString = "";

				foreach (var message in messages)
				{
					if (message.Author is not SocketWebhookUser)
						recapString += $"[{message.Timestamp}]{((SocketGuildUser)message.Author).GetUsername()}: {message.CleanContent}{(message.Embeds != null && message.Embeds.Count() > 0 ? "[MESSAGE HAS AN EMBED]" : "")}\n";
				}

				var context = new List<string>();

				if (question != null)
				{
					context.Add($"[You will be provided with a list of messages from this chat, you are tasked with memorizing the discussions that were had and answering to this question: {question}, just answer the question, give some context if necessary, and nothing else.]");
				}
				else
				{
					context.Add("[You will be provided with a list of messages from this chat, you are tasked with giving a summary of the discussions that were had, make sure not to go over 2000 characters, just say the recap and nothing else.]");
				}

				var recap = await OpenAIChat(recapString, context, false, false);
				await command.ModifyOriginalResponseAsync(x => x.Content = recap);
			}
		}
	}
}