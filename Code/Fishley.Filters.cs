namespace Fishley;

public partial class Fishley
{
	private static async Task<bool> HandleFilters(SocketUserMessage message)
	{
		if (await HandleSimpleFilter(message)) return true;
		if (await HandleComplicatedFilter(message)) return true;
		if (await HandleConfusingFilter(message)) return true;
		if (await HandleDiscordInviteFilter(message)) return true;

		return false;
	}

	private static async Task<bool> HandleDiscordInviteFilter(SocketUserMessage message)
	{
		var regex = new Regex($@"(https?:\/\/|http?:\/\/)?(www.)?(discord.(gg|io|me|li)|discordapp.com\/invite|discord.com\/invite)\/[^\s\/]+?(?=\b)"); // Idk I can't put it in json so I'll just keep it here
		if (regex.Match(message.Content).Success)
		{
			var user = message.Author as SocketGuildUser;

			await AddWarn(user, message, $"Hey <@{user.Id}>, don't go crazy with the invite links!");
			return true;
		}
		return false;
	}

	/// <summary>
	/// Does this message contain "simple", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleSimpleFilter(SocketUserMessage message)
	{
		string pattern = ConfigGet<string>("SimpleRegex");

		return await HandleFilter(message, pattern, "shrimple");
	}

	/// <summary>
	/// Does this message contain "complicated", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleComplicatedFilter(SocketUserMessage message)
	{
		string pattern = ConfigGet<string>("ComplicatedRegex");

		return await HandleFilter(message, pattern, "clamplicated");
	}

	/// <summary>
	/// Does this message contain "confusing", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleConfusingFilter(SocketUserMessage message)
	{
		string pattern = ConfigGet<string>("ConfusingRegex");

		return await HandleFilter(message, pattern, "conchfusing");
	}

	/// <summary>
	/// Handle a shrimple find and replace filter
	/// </summary>
	/// <param name="message"></param>
	/// <param name="regexPattern"></param>
	/// <param name="correctWord"></param>
	/// <returns></returns>
	private static async Task<bool> HandleFilter(SocketUserMessage message, string regexPattern, string correctWord)
	{
		if (FindAndReplace(message.Content, regexPattern, correctWord, out string correctedMessage))
		{
			var user = message.Author as SocketGuildUser;

			await AddWarn(user, message, $"Hey <@{user.Id}>, perhaps you meant to say: \n> {correctedMessage} \n");
			return true;
		}
		return false;
	}

	/// <summary>
	/// Replace the offending word in a message
	/// </summary>
	/// <param name="messageToCheck"></param>
	/// <param name="regexPattern"></param>
	/// <param name="correctWord"></param>
	/// <param name="correctedMessage"></param>
	/// <param name="lettersAround"></param>
	/// <param name="excludeLinks"></param>
	/// <returns></returns>
	private static bool FindAndReplace(string messageToCheck, string regexPattern, string correctWord, out string correctedMessage, int lettersAroundIncluded = 10, bool excludeLinks = true)
	{
		correctedMessage = "";
		var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
		var match = regex.Match(messageToCheck);

		if (match.Success)
		{
			var matchingIndex = match.Index;
			var matchingWord = match.Value;

			if (excludeLinks)
			{
				var linkRegex = new Regex(@"(?:https?://)?(?:www\.)?\S+\.\S+", RegexOptions.IgnoreCase);
				var linkMatch = linkRegex.Match(messageToCheck);
				var linkStart = linkMatch.Index;
				var linkEnd = linkStart + linkMatch.Value.Length;

				if (matchingIndex >= linkStart && matchingIndex + matchingWord.Length <= linkEnd) // The match is inside a link
					return false;
			}

			var startIndex = Math.Max(0, matchingIndex - lettersAroundIncluded);
			var endIndex = Math.Min(messageToCheck.Length, matchingIndex + matchingWord.Length + lettersAroundIncluded);
			var length = endIndex - startIndex;
			var isStarting = startIndex == 0;
			var isEnding = endIndex == messageToCheck.Length;

			var phrase = messageToCheck.Substring(startIndex, length);
			var replacedPhrase = phrase.Replace(matchingWord, $"~~{matchingWord}~~ **{correctWord}**");

			correctedMessage = $"{(isStarting ? "" : "...")}{replacedPhrase}{(isEnding ? "" : "...")}";
			return true;
		}

		return false;
	}
}