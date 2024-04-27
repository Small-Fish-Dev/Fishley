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
	public ReviewStats ReviewStats { get; set; }
	public List<string> Tags { get; set; }
	public int CategoryId { get; set; }
	public int SubCategoryId { get; set; }
	public int Favourited { get; set; }
	public int Collections { get; set; }
	public int Referencing { get; set; }
	public int Referenced { get; set; }
	public int VotesUp { get; set; }
	public int VotesDown { get; set; }
	public string Source { get; set; }
	public bool Public { get; set; }
	public int ApiVersion { get; set; }
	public List<Screenshot> Screenshots { get; set; }
	public List<string> PackageReferences { get; set; }
	public List<string> EditorReferences { get; set; }
	public Interaction Interaction { get; set; } // This doesn't seem to be filled out

	public Package() { }
}