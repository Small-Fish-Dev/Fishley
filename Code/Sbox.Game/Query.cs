namespace SboxGame;

public class Query
{
	public List<Package> Packages { get; set; }
	public List<Facet> Facets { get; set; }
	public Dictionary<string, int> Tags { get; set; }
	public List<QueryOrder> Orders { get; set; }
	public Query() { }
}