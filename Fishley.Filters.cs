public partial class Fishley
{
	private static async Task HandleFilters( SocketUserMessage message )
	{
		if ( !await HandleSimpleFilter( userMessage ) )
			if ( !await HandleComplicatedFilter( userMessage ) )
				await HandleConfusingFilter( userMessage );
	}

	/// <summary>
	/// Does this message contain "simple", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleSimpleFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "SimpleRegex", @"[sS][iI][mM][pP][lL][eE]" );
		
		return await HandleFilter( message, pattern, "shrimple" );
	}

	/// <summary>
	/// Does this message contain "complicated", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleComplicatedFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "ComplicatedRegex", @"[cC][oO][mM][pP][lL][iI][cC][aA][tT][eE][dD]" );
		
		return await HandleFilter( message, pattern, "clamplicated" );
	}

	/// <summary>
	/// Does this message contain "confusing", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleConfusingFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "ConfusingRegex", @"[cC][oO][nN][fF][uU][sS][iI][nN][gG]" );
		
		return await HandleFilter( message, pattern, "conchfusing" );
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
		if ( FindAndReplace( message.Content, regexPattern, correctWord, out string correctedMessage ))
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;

			await AddWarn( user, channel, $"Hey <@{user.Id}>, perhaps you meant to say: \n> {correctedMessage} \n" );
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
	private static bool FindAndReplace( string messageToCheck, string regexPattern, string correctWord, out string correctedMessage, int lettersAroundIncluded = 10, bool excludeLinks = true )
	{
		correctedMessage = "";
		var regex = new Regex( regexPattern, RegexOptions.IgnoreCase );
		var match = regex.Match( messageToCheck );

		if ( match.Success )
		{
			var matchingIndex = match.Index;
			var matchingWord = match.Value;
			
			if ( excludeLinks )
			{
				var linkRegex = new Regex( @"(?:https?://)?(?:www\.)?\S+\.\S+", RegexOptions.IgnoreCase );
				var linkMatch = linkRegex.Match( messageToCheck );
				var linkStart = linkMatch.Index;
				var linkEnd = linkStart + linkMatch.Value.Length;

				if ( matchingIndex >= linkStart && matchingIndex + matchingWord.Length <= linkEnd ) // The match is inside a link
					return false;
			}

            var startIndex = Math.Max(0, matchingIndex - lettersAroundIncluded);
            var endIndex = Math.Min( messageToCheck.Length, matchingIndex + matchingWord.Length + lettersAroundIncluded );
			var length = endIndex - startIndex;
			var isStarting = startIndex == 0;
			var isEnding = endIndex == messageToCheck.Length;

            var phrase = messageToCheck.Substring( startIndex, length );
			var replacedPhrase = phrase.Replace( matchingWord, $"~~{matchingWord}~~ **{correctWord}**");

			correctedMessage = $"{(isStarting ? "" : "...")}{replacedPhrase}{(isEnding ? "" : "...")}";
			return true;
		}

		return false;
	}
}