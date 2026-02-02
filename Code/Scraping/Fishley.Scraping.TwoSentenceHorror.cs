namespace Fishley;

public partial class Fishley
{
	public class TwoSentenceHorrorScraper : WebsiteScraper
	{
		public override string Url => "https://www.reddit.com/r/TwoSentenceHorror/new/.rss";
		public override int SecondsCooldown => 60 * 10; // Every 10 minutes
		public override SocketGuildChannel ChannelToPost => null; // Not used, we use webhooks instead

		public override async Task<ScrapingResult> Fetch()
		{
			using (HttpClient client = new HttpClient())
			{
				// We are a browser ;-)
				client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

				try
				{
					// Fetch the RSS feed content
					HttpResponseMessage response = await client.GetAsync(Url);
					response.EnsureSuccessStatusCode();
					string rssContent = await response.Content.ReadAsStringAsync();

					if (string.IsNullOrEmpty(rssContent))
					{
						Console.WriteLine("RSS content is empty.");
						return new ScrapingResult(null, null, null);
					}

					// Load the RSS content into an XmlReader
					using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(rssContent)))
					{
						// Parse the RSS content
						SyndicationFeed feed = SyndicationFeed.Load(reader);

						if (feed == null)
						{
							Console.WriteLine("Failed to load SyndicationFeed from content.");
							return new ScrapingResult(null, null, null);
						}

						// Check if the feed has any items
						if (feed.Items == null)
						{
							Console.WriteLine("No items found in the feed.");
							return new ScrapingResult(null, null, null);
						}

						// Get the first (newest) post
						var latestPost = feed.Items.FirstOrDefault();
						if (latestPost == null)
						{
							return new ScrapingResult(null, null, null);
						}

						string title = latestPost.Title?.Text ?? "No title available";
						string summary = latestPost.Summary?.Text ?? "";
						Uri link = latestPost.Links[0]?.Uri;

						// Extract just the body text (the actual horror story)
						// The summary usually contains HTML, so we need to parse it
						string bodyText = ExtractBodyText(summary);

						if (string.IsNullOrEmpty(bodyText))
						{
							bodyText = title; // Fallback to title if body is empty
						}

						// Post to a random shadow channel via webhook
						await PostToRandomShadowChannel(bodyText);

						// Return the link so we track it as scraped
						return new ScrapingResult(link.ToString(), null, null);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error fetching or parsing TwoSentenceHorror feed: " + ex.Message);
					return new ScrapingResult(null, null, null);
				}
			}
		}

		private string ExtractBodyText(string htmlContent)
		{
			if (string.IsNullOrEmpty(htmlContent))
				return string.Empty;

			// Remove HTML tags
			var text = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<.*?>", string.Empty);

			// Decode HTML entities
			text = System.Net.WebUtility.HtmlDecode(text);

			// Clean up whitespace
			text = text.Trim();

			return text;
		}

		private async Task PostToRandomShadowChannel(string content)
		{
			// Get all shadow channel webhook config keys
			var shadowWebhookKeys = new[]
			{
				"GeneralTalkMirrorBot",
				"FunnyMemesMirrorBot",
				"SboxFeedMirrorBot",
				"WaywoMirrorBot",
				"ZoologyMirrorBot"
			};

			// Pick a random webhook
			var random = new Random();
			var randomWebhookKey = shadowWebhookKeys[random.Next(shadowWebhookKeys.Length)];

			var webhookUrl = ConfigGet<string>(randomWebhookKey);
			if (string.IsNullOrEmpty(webhookUrl))
			{
				DebugSay($"{randomWebhookKey} not found in config!");
				return;
			}

			// Get the Echoes of the Damned avatar URL from config
			var avatarUrl = ConfigGet<string>("EchoesOfTheDamnedAvatar") ?? "";

			// Prepare webhook payload
			var payload = new
			{
				content = content,
				username = "Echoes of the Damned",
				avatar_url = avatarUrl
			};

			var json = System.Text.Json.JsonSerializer.Serialize(payload);
			var httpContent = new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

			using var httpClient = new HttpClient();
			await httpClient.PostAsync(webhookUrl, httpContent);
		}
	}
}
