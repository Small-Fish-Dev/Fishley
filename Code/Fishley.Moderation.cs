namespace Fishley;

public partial class Fishley
{
	public static ulong WarnRole1 => ConfigGet<ulong>( "WarnRole1" );
	public static int WarnRole1DecaySeconds => ConfigGet<int>( "WarnRole1DecaySeconds" );
	public static ulong WarnRole2 => ConfigGet<ulong>( "WarnRole2" );
	public static int WarnRole2DecaySeconds => ConfigGet<int>( "WarnRole2DecaySeconds" );
	public static ulong WarnRole3 => ConfigGet<ulong>( "WarnRole3" );
	public static int WarnRole3DecaySeconds => ConfigGet<int>( "WarnRole3DecaySeconds" );
	public static string WarnEmoji => ConfigGet<string>( "WarnEmoji" );
	public static int TimeoutDuration => ConfigGet<int>( "WarnTimeoutSeconds" );

	public static int WarnDecayCheckTimer => ConfigGet<int>( "WarnDecayCheckTimer" );
	public static DateTime LastWarnDecayCheck;
	public static int WarnDecaySecondsPassed => (int)( DateTime.UtcNow - LastWarnDecayCheck ).TotalSeconds;

	/// <summary>
	/// Add a warning to the user with the option to send a message in chat
	/// </summary>
	/// <param name="user"></param>
	/// <param name="socketMessage"></param>
	/// <param name="message"></param>
	/// <param name="includeWarnCount"></param>
	/// <param name="reply"></param>
	/// <returns></returns>
    private static async Task AddWarn(SocketGuildUser user, SocketMessage socketMessage = null, string message = null, bool includeWarnCount = true, bool reply = true )
    {
		var storedUser = await GetOrCreateUser( user.Id );

		if ( socketMessage.Channel is not SocketTextChannel channel ) return;
		if ( channel == null || message == null || socketMessage == null ) return;
		if ( socketMessage.Reactions.Count( x => x.Key.Name == WarnEmoji ) >= 1 ) return; // Don't warn if this message led to a warn already

		if ( CanModerate( user ) )
		{
			DebugSay( $"Attempted to give warning to {user.GlobalName}({user.Id})" );
			await SendMessage( channel, $"{message} I can't warn you so please don't do it again.", reply ? socketMessage : null, 5f );
			return;
		}

		var timedOut = false;

		switch ( storedUser.Warnings )
		{
			case <= 0:
				await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );
				break;
			case 1:
				await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );
				break;
			case 2:
				await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole3 ) );
				break;
			case >= 3:
				await user.SetTimeOutAsync( TimeSpan.FromSeconds( TimeoutDuration ) );
				timedOut = true;
				break;
		}

		storedUser.Warnings = Math.Min( storedUser.Warnings + 1, 3 );
		storedUser.LastWarn = DateTime.UtcNow;
		await UpdateUser( storedUser );

		DebugSay( $"Given warning to {user.GlobalName}({user.Id})" );
		await SendMessage( channel, $"{message}{(includeWarnCount ? $" ({(timedOut ? "Timed Out" : $"Warning {storedUser.Warnings}/3")})" : "")}", reply ? socketMessage : null );

		var warnEmoji = SmallFishServer.Emotes.FirstOrDefault( x => x.Name == WarnEmoji ); // TODO Better way to get emojies
		if ( warnEmoji != null )
			await socketMessage.AddReactionAsync( warnEmoji );
    }

	/// <summary>
	/// Remove a warning from the user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
    private static async Task RemoveWarn(SocketGuildUser user)
    {
		var storedUser = await GetOrCreateUser( user.Id );

		if ( storedUser.Warnings == 3 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole3 ) );
		else if ( storedUser.Warnings == 2 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole2 ) );
		else if ( storedUser.Warnings == 1 )
			await user.RemoveRoleAsync(user.Guild.Roles.FirstOrDefault( x => x.Id == WarnRole1 ) );

		DebugSay( $"Removed warning from {user.GlobalName}({user.Id})" );

		storedUser.Warnings = Math.Max( storedUser.Warnings - 1, 0 );
		await UpdateUser( storedUser );
    }

	private static async Task WarnsDecayCheck()
	{
		using ( var context = new FishleyDbContext() )
		{
			var allWarnedUsers = await context.Users.AsAsyncEnumerable()
			.Where( x => x.Warnings > 0 )
			.ToListAsync();

			foreach ( var warnedUser in allWarnedUsers )
			{
				var secondsPassed = ( DateTime.UtcNow - warnedUser.LastWarn ).TotalSeconds;
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
}