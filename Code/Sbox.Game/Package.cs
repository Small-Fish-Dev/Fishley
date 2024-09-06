using Discord.WebSocket;
using Discord.Commands;
using Discord;

namespace SboxGame;

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
	public string Ident { get; set; }
	public string FullIdent { get; set; }
	public string Title { get; set; }
	public string Summary { get; set; }
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
	public int Favourited { get; set; }
	public int VotesUp { get; set; }
	public int VotesDown { get; set; }
	public bool Public { get; set; }
	[JsonIgnore]
	public string FullUrl => $"{SboxGame.SboxGameUrl}{Org.Ident}/{Ident}";

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
		.AddField("Created:", $"<t:{((DateTimeOffset)Created).ToUnixTimeSeconds()}:R>", true)
		.AddField("Updated:", $"<t:{((DateTimeOffset)Updated).ToUnixTimeSeconds()}:R>", true);

		string path = null;

		if (Thumb.Contains(".mp4"))
		{
			path = await VideoToGif.FromUrl(VideoThumb);
			embedBuilder = embedBuilder.WithImageUrl($"attachment://{path}");
		}
		else
		{
			embedBuilder = embedBuilder.WithImageUrl(Thumb);
		}

		return (embedBuilder.Build(), path);
	}
}