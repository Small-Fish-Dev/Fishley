namespace Fishley;

public partial class Fishley
{
	private static string _openAIKey;
	private static string _fishleySystemPrompt;
	public static OpenAIAPI OpenAIClient { get; private set; }

	private static void InitiateOpenAI()
	{
		_openAIKey = ConfigGet<string>("ChatGPTKey");
		_fishleySystemPrompt = ConfigGet<string>("FishleyPrompt");
		OpenAIClient = new OpenAI_API.OpenAIAPI(_openAIKey);
	}

	/// <summary>
	/// Generate an image using dalle
	/// </summary>
	/// <param name="input"></param>
	/// <param name="dalle3"></param>
	/// <returns></returns>
	public static async Task<string> CreateImage(string input, bool dalle3 = false)
	{
		var response = await OpenAIClient.ImageGenerations.CreateImageAsync(input, dalle3 ? OpenAI_API.Models.Model.DALLE3 : OpenAI_API.Models.Model.DALLE2);

		DebugSay($"RESPONSE WAS: {response}");

		return response.ToString();
	}

	/// <summary>
	/// Get a response out of Fishley through ChatGPT
	/// </summary>
	/// <param name="input"></param>
	/// <param name="context"></param>
	/// <param name="gpt4"></param>
	/// <returns></returns>
	public static async Task<string> OpenAIChat(string input, List<string> context = null, bool gpt4 = false)
	{
		var chat = OpenAIClient.Chat.CreateConversation();
		chat.Model = gpt4 ? new OpenAI_API.Models.Model("gpt-4o") : OpenAI_API.Models.Model.ChatGPTTurbo;
		chat.AppendSystemMessage(_fishleySystemPrompt);

		if (context != null)
			foreach (var ctx in context)
				if (ctx != null && ctx != string.Empty)
					chat.AppendSystemMessage(ctx);

		chat.AppendUserInput(input);
		return await chat.GetResponseFromChatbotAsync();
	}

	/// <summary>
	/// Let Fishley repond to a message through ChatGPT
	/// </summary>
	/// <param name="message"></param>
	/// <param name="gpt4"></param>
	/// <returns></returns>
	public static async Task OpenAIRespond(SocketMessage message, bool gpt4 = false)
	{
		var messageAuthor = (SocketGuildUser)message.Author;
		var messageChannel = (SocketTextChannel)message.Channel;
		var storedUser = await GetOrCreateUser(messageAuthor.Id);
		var rolesString = "None";
		foreach (var role in messageAuthor.Roles)
			if (!role.IsEveryone)
				rolesString = $"{rolesString}, {role.Name}";

		var context = new List<string>();

		context.Add($"[This message is sent by the user: {message.Author.GlobalName}. The user has has {storedUser.Warnings}/3 warnings. The user is the following roles: {rolesString}. The message was sent at {DateTime.UtcNow}UTC.]:");

		var reference = message.Reference;
		SocketMessage reply = null;

		if (reference != null)
		{
			if (reference.MessageId.IsSpecified)
			{
				var foundMessage = await messageChannel.GetMessageAsync(reference.MessageId.Value);

				if (foundMessage != null)
					reply = (SocketMessage)foundMessage;
			}
		}


		if (reply != null)
		{
			context.Add($"[The message you're replying to is a reply to the following message sent by {((SocketGuildUser)reply.Author).DisplayName} that says '{reply.Content}']");
			DebugSay($"[The message you're replying to is a reply to the following message sent by {((SocketGuildUser)reply.Author).DisplayName} that says '{reply.Content}']");
		}

		var response = await OpenAIChat(message.CleanContent, context, gpt4);

		var hasWarning = response.Contains("[WARNING]");

		var clearedResponse = response.Replace("[WARNING]", "\n").Replace("@everyone", "@ everyone").Replace("@here", "@ here"); // Just to be safe...

		if (hasWarning)
			await AddWarn(messageAuthor, message, clearedResponse);
		else
			await SendMessage(messageChannel, clearedResponse, message);
	}

	/// <summary>
	/// Check if the message is problematic, returns true if a warning has been issued.
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public static async Task<bool> ModerateMessage(SocketMessage message)
	{
		var messageAuthor = (SocketGuildUser)message.Author;
		var messageChannel = (SocketTextChannel)message.Channel;

		var moderation = await OpenAIClient.Moderation.CallModerationAsync(message.CleanContent);
		var minimumModeration = 50f; // 50% will make this editable later TODO: Some roles have higher percentages required

		if (moderation.Results.Any(x => x.HighestFlagScore * 100f >= minimumModeration))
		{
			var allCategories = moderation.Results.SelectMany(x => x.CategoryScores)
				.Where(x => x.Value * 100f >= minimumModeration)
				.OrderByDescending(x => x.Value);

			var categoriesString = "";
			foreach (var category in allCategories)
				categoriesString = $"{categoriesString}**{category.Key}:** {Math.Round(category.Value * 100f, 1)}%;\n";

			var responseString = $"I find that your message breaks one of our rules, perhaps I'll warn you, please don't do it again!\nThese are the categories your message fall into:\n{categoriesString}";

			await AddWarn((SocketGuildUser)message.Author, message, responseString);
			return true;
		}
		else
			return false;

	}


	/// <summary>
	/// Check if the message is problematic, returns true if a warning has been issued.
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	public static async Task<bool> IsTextBreakingRules(string message)
	{
		var moderation = await OpenAIClient.Moderation.CallModerationAsync(message);
		var minimumModeration = 10f; // 50% will make this editable later TODO: Some roles have higher percentages required

		if (moderation.Results.Any(x => x.HighestFlagScore * 100f >= minimumModeration))
			return true;
		else
			return false;
	}
}