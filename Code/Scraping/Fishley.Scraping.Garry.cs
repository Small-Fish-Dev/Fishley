namespace Fishley;

public partial class Fishley
{
	public class GarryScraper : WebsiteScraper
	{
		public override string Url => "https://garry.net/posts/rss";
		public override int SecondsCooldown => 60 * 5 + 7;
		public override SocketGuildChannel ChannelToPost => SboxFeedChannel;

		public override async Task<(string, Embed)> Fetch()
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
						return (null, null);
					}

					// Load the RSS content into an XmlReader
					using (XmlReader reader = XmlReader.Create(new System.IO.StringReader(rssContent)))
					{
						// Parse the RSS content
						SyndicationFeed feed = SyndicationFeed.Load(reader);

						if (feed == null)
						{
							Console.WriteLine("Failed to load SyndicationFeed from content.");
							return (null, null);
						}

						// Check if the feed has any items
						if (feed.Items == null)
						{
							Console.WriteLine("No items found in the feed.");
							return (null, null);
						}

						// Iterate through the posts in the feed
						foreach (SyndicationItem item in feed.Items)
						{
							Uri link = item.Links[0]?.Uri;

							return (link.ToString(), null); // Return the linkie
						}

						return (null, null);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error fetching or parsing feed: " + ex.Message);
					return (null, null);
				}
			}
		}
	}
}