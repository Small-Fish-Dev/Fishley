namespace AssetParty;

public struct Organization
{
	public string Ident { get; set; }
	public string Title { get; set; }
	public string Description { get; set; }
	public string Thumb { get; set; }
	public string Twitter { get; set; }
	public string WebUrl { get; set; }
	public string Url => $"{AssetParty.AssetPartyUrl}{Ident}";

	public Organization() { }
}