using Discord.WebSocket;
using Discord.Commands;
using Discord;

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
	[JsonIgnore]
	public string FullUrl => $"{AssetParty.AssetPartyUrl}{Org.Ident}/{Ident}";

	public Package() { }

	public async Task<(Embed, string)> ToEmbed()
	{
		var embedBuilder = new EmbedBuilder()
		.WithTitle($"{Type.ToUpper()} - {Title}")
		.WithDescription(string.IsNullOrEmpty(Summary) ? "No summary" : Summary)
		.WithUrl(FullUrl)
		.WithThumbnailUrl(Thumb)
		.WithAuthor(new EmbedAuthorBuilder()
			.WithIconUrl(Org.Thumb)
			.WithName(Org.Title)
			.WithUrl(Org.Url))
		.AddField("Description:", string.IsNullOrEmpty(Description) ? "No description" : Description)
		.AddField("Created:", $"<t:{((DateTimeOffset)Created).ToUnixTimeSeconds()}:R>", true)
		.AddField("Updated:", $"<t:{((DateTimeOffset)Updated).ToUnixTimeSeconds()}:R>", true);

		string path = null;

		if (Thumb.Contains(".mp4"))
		{
			path = await VideoToGif.FromUrl(VideoThumb);
			embedBuilder = embedBuilder.WithImageUrl($"attachment://{path}");
			Console.WriteLine("1");
		}
		else
		{
			if (Screenshots != null && Screenshots.First().Url.Contains(".mp4"))
			{
				path = await VideoToGif.FromUrl(Screenshots.First().Url);
				embedBuilder = embedBuilder.WithImageUrl($"attachment://{path}");
				Console.WriteLine("2");
			}
			else
			{
				embedBuilder = embedBuilder.WithImageUrl(Thumb);
				Console.WriteLine("3");
			}
		}

		Console.WriteLine(Thumb);
		Console.WriteLine(VideoThumb);
		Console.WriteLine(Screenshots.First());

		return (embedBuilder.Build(), path);
	}
}