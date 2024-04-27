namespace AssetParty;

public class Query
{
	public int TotalCount { get; set; }
	public string QueryString { get; set; }
	public List<Package> Packages { get; set; }
	public List<Facet> Facets { get; set; }
	public List<QueryTag> Tags { get; set; }
	public List<QueryOrder> Orders { get; set; }
	public float Milliseconds { get; set; }
	public Query() { }
}