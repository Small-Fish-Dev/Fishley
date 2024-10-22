namespace Fishley;

using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Games;
using TwitchLib.Api.Helix.Models.Streams;

public partial class Fishley
{
	public class TwitchScraper : WebsiteScraper
	{
		public override string Url => "https://www.twitch.tv/directory/category/noita?sort=RECENT";
		public override int SecondsCooldown => 60 + 4;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;
		string _token;

		public override async Task<ScrapingResult> Fetch()
		{
			var clientId = ConfigGet<string>("TwitchClient");
			var clientSecret = ConfigGet<string>("TwitchSecret");

			if (string.IsNullOrEmpty(_token))
				_token = await GetAccessTokenAsync(clientId, clientSecret);

			var api = new TwitchAPI();
			api.Settings.ClientId = clientId;
			api.Settings.AccessToken = _token;

			// Get the game ID
			var games = await api.Helix.Games.GetGamesAsync(gameNames: new List<string> { "S&box" });
			if (games.Games.Length == 0)
				return new ScrapingResult(null, null, null);

			var gameId = games.Games[0].Id;

			// Get live streams for the game
			var streams = await api.Helix.Streams.GetStreamsAsync(first: 1, gameIds: new List<string> { gameId });

			if (streams == null || streams.Streams == null || streams.Streams.Count() == 0)
				return new ScrapingResult(null, null, null);

			return new ScrapingResult($"https://www.twitch.tv/{streams.Streams.First().UserLogin} is streaming S&box!", null, null);
		}

		public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret)
		{
			using var client = new HttpClient();

			var requestBody = new FormUrlEncodedContent(new[]
			{
			new KeyValuePair<string, string>("client_id", clientId),
			new KeyValuePair<string, string>("client_secret", clientSecret),
			new KeyValuePair<string, string>("grant_type", "client_credentials")
		});

			var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", requestBody);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Error fetching access token: {response.StatusCode}");
				Console.WriteLine(responseContent);
				return null;
			}

			dynamic json = JsonConvert.DeserializeObject(responseContent);
			string accessToken = json.access_token;
			return accessToken;
		}
	}
}