public partial class Fishley
{
	// TODO Put these in a single method and use that method for each of these

	/// <summary>
	/// Does this message contain "simple", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleSimpleFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "SimpleRegex", @"[sS][iI][mM][pP][lL][eE]" );
		var regex = new Regex( pattern );
		var match = regex.Match( message.Content );

		if ( match.Success )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;

			var matchingIndex = match.Index;
			var matchingWord = match.Value;
            var startIndex = Math.Max(0, matchingIndex - 6);
            var endIndex = Math.Min( message.Content.Length, matchingIndex + matchingWord.Length + 6 );
			var length = endIndex - startIndex;

            var phrase = message.Content.Substring( startIndex, length );
			var replacedPhrase = phrase.Replace( matchingWord, $"~~{matchingWord}~~ **shrimple**");

			await AddWarn( user, channel, $"Hey {user.DisplayName}, perhaps you meant to say: {replacedPhrase}" );
			return true;
		}
		return false;
	}

	/// <summary>
	/// Does this message contain "complicated", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleComplicatedFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "ComplicatedRegex", @"[cC][oO][mM][pP][lL][iI][cC][aA][tT][eE][dD]" );
		var regex = new Regex( pattern );
		var match = regex.Match( message.Content );

		if ( match.Success )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;

			var matchingIndex = match.Index;
			var matchingWord = match.Value;
            var startIndex = Math.Max(0, matchingIndex - 6);
            var endIndex = Math.Min( message.Content.Length, matchingIndex + matchingWord.Length + 6 );
			var length = endIndex - startIndex;

            var phrase = message.Content.Substring( startIndex, length );
			var replacedPhrase = phrase.Replace( matchingWord, $"~~{matchingWord}~~ **clamplicated**");

			await AddWarn( user, channel, $"Hey {user.DisplayName}, perhaps you meant to say: {replacedPhrase}" );
			return true;
		}
		return false;
	}

	/// <summary>
	/// Does this message contain "confusing", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleConfusingFilter(SocketUserMessage message)
    {
		string pattern = ConfigGet( "ConfusingRegex", @"[cC][oO][nN][fF][uU][sS][iI][nN][gG]" );
		var regex = new Regex( pattern );
		var match = regex.Match( message.Content );

		if ( match.Success )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;

			var matchingIndex = match.Index;
			var matchingWord = match.Value;
            var startIndex = Math.Max(0, matchingIndex - 6);
            var endIndex = Math.Min( message.Content.Length, matchingIndex + matchingWord.Length + 6 );
			var length = endIndex - startIndex;

            var phrase = message.Content.Substring( startIndex, length );
			var replacedPhrase = phrase.Replace( matchingWord, $"~~{matchingWord}~~ **conchfusing**");

			await AddWarn( user, channel, $"Hey {user.DisplayName}, perhaps you meant to say: {replacedPhrase}" );
			return true;
		}
		return false;
	}
}