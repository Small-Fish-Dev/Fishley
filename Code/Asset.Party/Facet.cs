namespace SboxGame;

public struct FacetEntry
{
	public string Name { get; set; }
	public string Icon { get; set; }
	public string Title { get; set; }
	public int Count { get; set; }

	public FacetEntry() { }
}

public struct Facet
{
	public string Name { get; set; }
	public string Title { get; set; }
	public int Order { get; set; }
	public List<FacetEntry> Entries { get; set; }

	public Facet() { }
}