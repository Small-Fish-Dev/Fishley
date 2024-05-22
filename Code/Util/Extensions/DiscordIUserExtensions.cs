namespace Fishley
{
	public static class DiscordIUserExtensions
	{
		public static string GetUsername(this IUser user)
		{
			if (user is SocketWebhookUser webhookUser)
				return webhookUser.Username;
			else
				return user.GlobalName != null ? user.GlobalName : user.Username;
		}
	}
}