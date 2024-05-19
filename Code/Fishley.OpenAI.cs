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

	public static async Task<string> OpenAIChat(string input, string context = null, bool gpt4 = false)
	{
		var chat = OpenAIClient.Chat.CreateConversation();
		chat.Model = gpt4 ? OpenAI_API.Models.Model.GPT4 : OpenAI_API.Models.Model.ChatGPTTurbo;
		chat.AppendSystemMessage(_fishleySystemPrompt);

		if (context != null)
			chat.AppendSystemMessage(context);

		chat.AppendUserInput(input);
		var response = await chat.GetResponseFromChatbotAsync();
	}

	public static async Task OpenAIRespond(SocketMessage message, bool gpt4 = false)
	{
		var messageAuthor = (SocketGuildUser)message.Author;
		var messageChannel = (SocketTextChannel)message.Channel;
		var storedUser = await GetOrCreateUser(messageAuthor.Id);

		var rolesString = "None";
		foreach (var role in messageAuthor.Roles)
			if (!role.IsEveryone)
				rolesString = $"{rolesString}, {role.Name}";

		var context = $"[This message is sent by the user: {message.Author.GlobalName}. The user has has {storedUser.Warnings}/3 warnings. The user is the following roles: {rolesString}. The message was sent at {DateTime.UtcNow.ToString()}UTC.]:";

		var response = await OpenAIChat(message.Content, context, gpt4);

		var hasWarning = response.Contains("[WARNING]");

		var clearedResponse = response.Replace("[WARNING]", "\n").Replace("@everyone", "@ everyone").Replace("@here", "@ here"); // Just to be safe...

		if (hasWarning)
			await AddWarn((SocketGuildUser)message.Author, message, clearedResponse);
		else
			await SendMessage((SocketTextChannel)message.Channel, clearedResponse, message);
	}

	/// <summary>
	/// Check if the message is problematic, returns true if a warning has been issued.
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> ModerateMessage(SocketMessage message)
	{
		var messageAuthor = (SocketGuildUser)message.Author;
		var messageChannel = (SocketTextChannel)message.Channel;
	}
}