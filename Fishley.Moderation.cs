public partial class Fishley
{
	public static ulong WarnRole1 => ConfigGet<ulong>( "WarnRole1", 1063893887564914869 );
	public static ulong WarnRole2 => ConfigGet<ulong>( "WarnRole2", 1063894349617823766 );

	/// <summary>
	/// Add a warning to the user with the option to send a message in chat
	/// </summary>
	/// <param name="user"></param>
	/// <param name="channel"></param>
	/// <param name="message"></param>
	/// <param name="includeWarnCount"></param>
	/// <returns></returns>
    private static async Task AddWarn(SocketGuildUser user, SocketTextChannel channel = null, string message = null, bool includeWarnCount = true )
    {
		var warnLevel = UserWarnLevel( user );

		if ( warnLevel == 0 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );
		else if ( warnLevel == 1 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );

			
		if ( channel != null && message != null )
			await channel.SendMessageAsync( $"{message}{(includeWarnCount ? $" (Warning {warnLevel + 1}/3)" : "")}" ); // Kinda hardcoded to max 3 warnings but we don't even have a punishment right now so it's fine
    }

	/// <summary>
	/// Remove a warning from the user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
    private static async Task RemoveWarn(SocketGuildUser user)
    {
		var warnLevel = UserWarnLevel( user );

		if ( warnLevel == 2 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );
		else if ( warnLevel == 1 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );
    }

	/// <summary>
	/// Return how many warnings the user has right now
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
	private static int UserWarnLevel(SocketGuildUser user)
	{
		if ( user.Roles.Any( x => x.Id == WarnRole2 ) )
			return 2;
		if ( user.Roles.Any( x => x.Id == WarnRole1 ) )
			return 1;
		
		return 0;
	}
}