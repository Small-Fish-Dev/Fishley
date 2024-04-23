namespace Fishley;

public partial class Fishley
{
	public static GuildEmote WarnEmoji => SmallFishServer.Emotes.FirstOrDefault(x => x.Name == "warn");
}