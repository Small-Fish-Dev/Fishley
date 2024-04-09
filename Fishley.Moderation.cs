public partial class Fishley
{
	public static ulong WarnRole1 => ConfigGet<ulong>( "WarnRole1", 1063893887564914869 );
	public static int WarnRole1DecaySeconds => ConfigGet<int>( "WarnRole1DecaySeconds", 900 );
	public static ulong WarnRole2 => ConfigGet<ulong>( "WarnRole2", 1063894349617823766 );
	public static int WarnRole2DecaySeconds => ConfigGet<int>( "WarnRole2DecaySeconds", 7200 );
	public static ulong WarnRole3 => ConfigGet<ulong>( "WarnRole3", 1227004898802143252 );
	public static int WarnRole3DecaySeconds => ConfigGet<int>( "WarnRole3DecaySeconds", 86400 );
	public static string WarnEmoji => ConfigGet( "WarnEmoji", "warn" );

	public static int WarnDecayCheckTimer => ConfigGet<int>( "WarnDecayCheckTimer", 60 );
	public static DateTime LastWarnDecayCheck;
	public static int WarnDecaySecondsPassed => (int)( DateTime.UtcNow - LastWarnDecayCheck ).TotalSeconds;

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
		var storedUser = UserGet( user.Id );

		if ( storedUser.Warnings == 0 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );
		else if ( storedUser.Warnings == 1 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );
		else if ( storedUser.Warnings == 2 )
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole3 ) );

		DebugSay( $"Given warning to {user.GlobalName}({user.Id})" );

		storedUser.Warnings = Math.Min( storedUser.Warnings + 1, 3 );
		storedUser.LastWarn = DateTime.Now.Ticks;
		UserUpdate( storedUser );

		if ( channel != null && message != null )
			await channel.SendMessageAsync( $"{message}{(includeWarnCount ? $" (Warning {storedUser.Warnings}/3)" : "")}" );
    }

	/// <summary>
	/// Remove a warning from the user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
    private static async Task RemoveWarn(SocketGuildUser user)
    {
		var storedUser = UserGet( user.Id );

		if ( storedUser.Warnings == 3 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole3 ) );
		else if ( storedUser.Warnings == 2 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );
		else if ( storedUser.Warnings == 1 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );

		DebugSay( $"Removed warning from {user.GlobalName}({user.Id})" );

		storedUser.Warnings = Math.Max( storedUser.Warnings - 1, 0 );
		UserUpdate( storedUser );
    }

	private static async Task WarnsDecayCheck()
	{
		var allWarnedUsers = Users.Query()
		.Where( x => x.Warnings > 0 )
		.ToList();

		foreach ( var warnedUser in allWarnedUsers )
		{
			var lastWarningTime = DateTime.FromBinary( warnedUser.LastWarn );
			var nowTime = DateTime.UtcNow;
			var secondsPassed = (nowTime - lastWarningTime).TotalSeconds;
			var secondsToPass = warnedUser.Warnings == 1 ? WarnRole1DecaySeconds : ( warnedUser.Warnings == 2 ? WarnRole2DecaySeconds : WarnRole3DecaySeconds );

			if ( secondsPassed >= secondsToPass )
			{
				var user = SmallFishServer.GetUser( warnedUser.UserId );

				if ( user != null )
					await RemoveWarn( user );
			}
		}
	}
}