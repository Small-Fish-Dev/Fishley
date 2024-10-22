namespace Fishley;
public partial class Fishley
{
	public static SocketRole SmallFishRole => SmallFishServer.GetRole(1005599675530870824);
	public static SocketRole AdminRole => SmallFishServer.GetRole(1197217122183544862);
	public static SocketRole FishOfTheDayRole => SmallFishServer.GetRole(1146188313867329656);
	public static SocketRole ClambassadorRole => SmallFishServer.GetRole(1063497806519730216);
	public static SocketRole ConchtributorRole => SmallFishServer.GetRole(1142444426065625211);
	public static SocketRole Warning1Role => SmallFishServer.GetRole(1063893887564914869);
	public static SocketRole Warning2Role => SmallFishServer.GetRole(1063894349617823766);
	public static SocketRole Warning3Role => SmallFishServer.GetRole(1227004898802143252);
	public static SocketRole DramaDolphinRole => SmallFishServer.GetRole(1128639097603358840);
	public static SocketRole NewsNewtRole => SmallFishServer.GetRole(1128639400838963300);
	public static SocketRole PlaytestPenguinRole => SmallFishServer.GetRole(1197211499911991467);


	public static bool IsSmallFish(SocketGuildUser user) => user.Roles.Contains(SmallFishRole);
	public static bool IsAdmin(SocketGuildUser user) => user.Roles.Contains(AdminRole);
	public static bool IsClambassador(SocketGuildUser user) => user.Roles.Contains(ClambassadorRole);
	public static bool IsConchtributor(SocketGuildUser user) => user.Roles.Contains(ConchtributorRole);
	public static bool IsFishOfTheDay(SocketGuildUser user) => user.Roles.Contains(FishOfTheDayRole);
	public static bool CanModerate(SocketGuildUser user) => IsAdmin(user) || IsSmallFish(user);
}