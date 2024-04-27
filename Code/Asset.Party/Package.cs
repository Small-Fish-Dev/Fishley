namespace AssetParty;

public struct Package
{
	public Organization Org { get; set; }
	public int AssetId { get; set; }
	public string Ident { get; set; }
	public string FullIdent { get; set; }
	public string Title { get; set; }
	public string Summary { get; set; }
	public string Description { get; set; }
	public string Thumb { get; set; }
	public string VideoThumb { get; set; }
	public int PackageType { get; set; } // 2 = Game, 5 = Model, 20 = Library
	public string Type { get; set; }
	public DateTime Updated { get; set; } // ISO 8601 Format
	public DateTime Created { get; set; } // ISO 8601 Format
	public UsageStats UsageStats { get; set; }



	public Package() { }
}