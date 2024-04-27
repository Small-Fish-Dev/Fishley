namespace AssetParty;

public struct Query
{
	public int TotalCount { get; set; }
	public string QueryString { get; set; }
	public List<Package> Packages { get; set; }
	public float Milliseconds { get; set; }
	public Query() { }
}