namespace AssetParty;

public enum QuerySort
{
	All,
	Popular,
	Trending,
	Newest,
	Updated,
	Referenced,
	Favourites,
	Upvotes,
	Downvotes
}

public struct QueryOrder
{
	public string Name { get; set; }
	public string Title { get; set; }
	public string Icon { get; set; }

	public QueryOrder() { }
}