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
	public static SocketGuildChannel ShadowBannedChannel => SmallFishServer.GetChannel(1466830851793752246);

	// Shadow banned threads (mirrors of main channels)
	public static SocketThreadChannel GeneralTalkThread => SmallFishServer.GetThreadChannel(1466832968277426318);
	public static SocketThreadChannel FunnyMemesThread => SmallFishServer.GetThreadChannel(1466833014498529372);
	public static SocketThreadChannel SboxFeedThread => SmallFishServer.GetThreadChannel(1466833091632042281);
	public static SocketThreadChannel WaywoThread => SmallFishServer.GetThreadChannel(1466833122044805242);
	public static SocketThreadChannel ZoologyThread => SmallFishServer.GetThreadChannel(1466833172263338188);
}