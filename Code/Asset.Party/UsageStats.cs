namespace AssetParty;

public struct UsageStat
{
	public int Users { get; set; }
	public int Seconds { get; set; }

	public UsageStat() { }
}

public struct UsageStats
{
	public UsageStat Total { get; set; }
	public UsageStat Month { get; set; }
	public UsageStat Week { get; set; }
	public UsageStat Day { get; set; }
	public int UsersNow { get; set; }
	public List<int> DailyUsers { get; set; }
	public List<int> DailySeconds { get; set; }
	public double Trend { get; set; } // Example: -0.70576673746109
	public UsageStats() { }
}