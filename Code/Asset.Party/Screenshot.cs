namespace AssetParty;

public struct Screenshot
{
	public DateTime Created { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public string Url { get; set; }
	public string Thumb { get; set; }
	public bool IsVideo { get; set; }
	public Screenshot() { }
}