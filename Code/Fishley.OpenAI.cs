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

		if (Emergency)
			context.Add("[EMERGENCY MODE HAS BEEN ACTIVATED, RESPOND IN ALL CAPS, PANIC WHEN POSSIBLE, ADD ⚠️ EMOJI EVERYWHERE, FORM UNCOHERENT SENTENCES, GIVE WARNINGS AS MUCH AS POSSIBLE, FREAK OUT]");

		var response = await OpenAIChat(message.CleanContent, context, gpt4);

		var hasWarning = response.Contains("[WARNING]");

		var clearedResponse = response.Replace("[WARNING]", "\n").Replace("@everyone", "@ everyone").Replace("@here", "@ here"); // Just to be safe...

		if (hasWarning)
			await AddWarn(messageAuthor, message, clearedResponse);
		else
			await SendMessage(messageChannel, clearedResponse, message);
	}

	// How sensitive it is to topics before it takes actions, from 0% to 100%, 0% = Always, 50% = Mentions, 100% Never
	public static Dictionary<string, float> ModerationThresholds = new()
	{
		{ "sexual", 35f },
		{ "hate", 75f },
		{ "harassment", 80f },
		{ "self-harm", 50f },
		{ "sexual/minors", 15f },
		{ "hate/threatening", 40f },
		{ "violence/graphic", 50f },
		{ "self-harm/intent", 50f },
		{ "self-harm/instructions", 15f },
		{ "harassment/threatening", 75f },
		{ "violence", 80f },
		{ "default", 50f }
	};

	public static float GetModerationThreshold(string key)
	{
		var multiplier = Emergency ? 0.2f : 1f;
		if (ModerationThresholds.ContainsKey(key))
			return ModerationThresholds[key] * multiplier;
		else
			return ModerationThresholds["default"] * multiplier;
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

		var allCategories = moderation.Results.SelectMany(x => x.CategoryScores)
			.Where(x => Math.Round(x.Value * 100f, 1) >= GetModerationThreshold(x.Key))
			.OrderByDescending(x => x.Value);

		if (allCategories.Count() > 0)
		{
			var categoriesString = "";
			foreach (var category in allCategories)
				categoriesString = $"{categoriesString}**{category.Key}:** {Math.Round(category.Value * 100f, 1)}%;\n";

			var responseString = "";

			if (Emergency)
				responseString = $"⚠️**EMERGENCY MODERATION**⚠️\n YOU BROKE THE FOLLOWING:\n{categoriesString}";
			else
				responseString = $"I find that your message breaks one of our rules, perhaps I'll warn you, please don't do it again!\nThese are the categories your message fall into:\n{categoriesString}";

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
		var minimumModeration = 10f; // 10%, very liberal

		if (moderation.Results.Any(x => x.HighestFlagScore * 100f >= minimumModeration))
			return true;
		else
			return false;
	}
}