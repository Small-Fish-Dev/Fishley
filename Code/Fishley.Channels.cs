namespace Fishley;

public partial class Fishley
{
	public static SocketGuildChannel GeneralTalkChannel => SmallFishServer.GetChannel(1005596274004852739);
	public static SocketGuildChannel FunnyMemesChannel => SmallFishServer.GetChannel(1020718603298930728);
	public static SocketGuildChannel SboxFeedChannel => SmallFishServer.GetChannel(1141117812430078022);
	public static SocketGuildChannel WaywoChannel => SmallFishServer.GetChannel(1263929413792301058);
	public static SocketGuildChannel ZoologyChannel => SmallFishServer.GetChannel(1005604067520823296);
	public static SocketGuildChannel CryptoZoologyChannel => SmallFishServer.GetChannel(1007362218247077938);
	public static SocketGuildChannel SpamChannel => SmallFishServer.GetChannel(1031998162212229120);
	public static SocketGuildChannel FishOfTheDayChannel => SmallFishServer.GetChannel(1146189225876783245);
	public static SocketGuildChannel ModeratorLogChannel => SmallFishServer.GetChannel(1197209153278574643);

	// Shadow banned channels
	public static SocketGuildChannel GeneralTalkShadow => SmallFishServer.GetThreadChannel(1466892641135366246);
	public static SocketGuildChannel FunnyMemesShadow => SmallFishServer.GetThreadChannel(1466893111199268895);
	public static SocketGuildChannel SboxFeedShadow => SmallFishServer.GetThreadChannel(1466893200856715397);
	public static SocketGuildChannel WaywoShadow => SmallFishServer.GetThreadChannel(1466893264144568482);
	public static SocketGuildChannel ZoologyShadow => SmallFishServer.GetThreadChannel(1466893439441305875);
}