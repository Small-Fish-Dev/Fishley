namespace Fishley;

public partial class Fishley
{
	public static int WarnRole1DecaySeconds => 60 * 10; // 10 minutes
	public static int WarnRole2DecaySeconds => 60 * 60; // 1 hour
	public static int WarnRole3DecaySeconds => 60 * 60 * 6; // 6 hours
	public static int TimeoutDuration => 60 * 10; // 10 minutes

	public static int WarnDecayCheckTimer => 60; // 1 minute
	public static DateTime LastWarnDecayCheck;
	public static int WarnDecaySecondsPassed => (int)(DateTime.UtcNow - LastWarnDecayCheck).TotalSeconds;

	/// <summary>
	/// Add a warning to the user with the option to send a message in chat
	/// </summary>
	/// <param name="user"></param>
	/// <param name="socketMessage"></param>
	/// <param name="message"></param>
	/// <param name="includeWarnCount"></param>
	/// <param name="reply"></param>
	/// <param name="warnEmoteAlreadyThere"></param>
	/// <returns></returns>
	private static async Task AddWarn(SocketGuildUser user, SocketMessage socketMessage = null, string message = null, bool includeWarnCount = true, bool reply = true, bool warnEmoteAlreadyThere = false)
	{
		var storedUser = await GetOrCreateUser(user.Id);

		if (socketMessage.Channel is not SocketTextChannel channel) return;
		if (channel == null || message == null || socketMessage == null) return;
		if (socketMessage.Reactions.FirstOrDefault(x => x.Key.Equals(WarnEmoji)).Value.ReactionCount >= (warnEmoteAlreadyThere ? 2 : 1)) return; // Don't warn if this message led to a warn already

		if (IsAdmin(user))
		{
			DebugSay($"Attempted to give warning to {user.GlobalName}({user.Id})");
			await SendMessage(channel, $"{message} I can't warn you so please don't do it again.", reply ? socketMessage : null, 5f);
			return;
		}

		var timedOut = false;

		switch (storedUser.Warnings)
		{
			case <= 0:
				await user.AddRoleAsync(Warning1Role);
				break;
			case 1:
				await user.AddRoleAsync(Warning2Role);
				break;
			case 2:
				await user.AddRoleAsync(Warning3Role);
				break;
			case >= 3:
				await user.SetTimeOutAsync(TimeSpan.FromSeconds(TimeoutDuration));
				timedOut = true;
				break;
		}

		storedUser.Warnings = Math.Min(storedUser.Warnings + 1, 3);
		storedUser.LastWarn = DateTime.UtcNow;

		var warnPrice = (int)Math.Max((float)storedUser.Money / 20f, 10f);

		await UpdateOrCreateUser(storedUser);

		DebugSay($"Given warning to {user.GlobalName}({user.Id})");
		var component = new ComponentBuilder()
			.WithButton($"Remove Warn ({NiceMoney((float)warnPrice)})", $"fine_paid-{warnPrice}-{user.Id}", ButtonStyle.Danger)
			.Build();

		await SendMessage(channel, $"{message}{(includeWarnCount ? $" ({(timedOut ? "Timed Out" : $"Warning {storedUser.Warnings}/3")})" : "")}", reply ? socketMessage : null, component: component);

		await socketMessage.AddReactionAsync(WarnEmoji);
	}

	/// <summary>
	/// Remove a warning from the user
	/// </summary>
	/// <param name="user"></param>
	/// <returns></returns>
	private static async Task RemoveWarn(SocketGuildUser user)
	{
		var storedUser = await GetOrCreateUser(user.Id);

		switch (storedUser.Warnings)
		{
			case 1:
				await user.RemoveRoleAsync(Warning1Role);
				break;
			case 2:
				await user.RemoveRoleAsync(Warning2Role);
				break;
			case 3:
				await user.RemoveRoleAsync(Warning3Role);
				break;
		}

		DebugSay($"Removed warning from {user.GlobalName}({user.Id})");

		storedUser.Warnings = Math.Max(storedUser.Warnings - 1, 0);
		await UpdateOrCreateUser(storedUser);
	}

	private static async Task WarnsDecayCheck()
	{
		if (WarnDecaySecondsPassed >= WarnDecayCheckTimer)
		{
			LastWarnDecayCheck = DateTime.UtcNow;

			using (var context = new FishleyDbContext())
			{
				var allWarnedUsers = await context.Users.AsAsyncEnumerable()
				.Where(x => x.Warnings > 0)
				.ToListAsync();

				foreach (var warnedUser in allWarnedUsers)
				{
					var secondsPassed = (DateTime.UtcNow - warnedUser.LastWarn).TotalSeconds;
					var secondsToPass = warnedUser.Warnings == 1 ? WarnRole1DecaySeconds : (warnedUser.Warnings == 2 ? WarnRole2DecaySeconds : WarnRole3DecaySeconds);

					if (secondsPassed >= secondsToPass)
					{
						var user = SmallFishServer.GetUser(warnedUser.UserId);

						if (user != null)
							await RemoveWarn(user);
					}
				}
			}
		}
	}
}