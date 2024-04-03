public partial class Fishley
{
	/// <summary>
	/// Does this message contain "simple", returns true if yes
	/// </summary>
	/// <param name="message"></param>
	/// <returns></returns>
	private static async Task<bool> HandleSimpleFilter(SocketUserMessage message)
    {
		string simplePattern = ConfigGet( "SimpleRegex", @"[sS][iI][mM][pP][lL][eE]" );

		if ( Regex.IsMatch(message.Content, simplePattern) )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;
			await AddWarn( user, channel, $"{user.DisplayName}, Rule #3" );
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
		string simplePattern = ConfigGet( "ComplicatedRegex", @"[cC][oO][mM][pP][lL][iI][cC][aA][tT][eE][dD]" );

		if ( Regex.IsMatch(message.Content, simplePattern) )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;
			await AddWarn( user, channel, $"{user.DisplayName}, Rule #3" );
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
		string simplePattern = ConfigGet( "ConfusingRegex", @"[cC][oO][nN][fF][uU][sS][iI][nN][gG]" );

		if ( Regex.IsMatch(message.Content, simplePattern) )
		{
			var channel = message.Channel as SocketTextChannel;
			var user = message.Author as SocketGuildUser;
			await AddWarn( user, channel, $"{user.DisplayName}, Rule #3" );
			return true;
		}
		return false;
	}
}