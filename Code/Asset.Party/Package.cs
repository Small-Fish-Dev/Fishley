namespace AssetParty;

public enum PackageType
{
	All,
	Map,        // "map" - Type 1
	Game,       // "game" - Type 2
	Model,      // "model" - Type 5
	Material,   // "material" - Type 6
	Sound,      // "sound" - Type 7
	SoundScape, // "soundscape" - Type 8
	Shader,     // "shader" - Type 9
	Texture,    // "texture" - Type 12
	Library,    // "library" - Type 20
	Particle,   // "particle" - Type 1
}

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
	[JsonProperty("PackageType")]
	public int PackageTypeId { get; set; }
	[JsonProperty("Type")]
	private string _type { get; }
	[JsonIgnore]
	public PackageType Type => _type == null ? PackageType.All : (PackageType)Enum.Parse(typeof(PackageType), _type, true);
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