namespace Fishley
{
    public static class DiscordIUserExtensions
    {
        public static string GetUsername(this IUser user) => user.GlobalName != null ? user.GlobalName : user.Username;
    }
}