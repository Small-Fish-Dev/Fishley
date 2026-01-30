using System.Text.Json;

namespace Fishley;

public partial class Fishley
{
	private static readonly Dictionary<ulong, ulong> ChannelToThreadMap = new()
	{
		{ 1005596274004852739, 1466832968277426318 }, // General Talk -> General Talk Thread
		{ 1020718603298930728, 1466833014498529372 }, // Funny Memes -> Funny Memes Thread
		{ 1141117812430078022, 1466833091632042281 }, // Sbox Feed -> Sbox Feed Thread
		{ 1263929413792301058, 1466833122044805242 }, // WAYWO -> WAYWO Thread
		{ 1005604067520823296, 1466833172263338188 }  // Zoology -> Zoology Thread
	};

	private static async Task MirrorMessageToShadowBan(SocketMessage message)
	{
		try
		{
			// Check if this message is in one of the monitored channels
			if (!ChannelToThreadMap.TryGetValue(message.Channel.Id, out var threadId))
				return;

			var shadowBotUrl = ConfigGet<string>("ShadowBotUrl");
			if (string.IsNullOrEmpty(shadowBotUrl))
			{
				DebugSay("ShadowBotUrl not found in config!");
				return;
			}

			// Get user info
			string username = message.Author.Username;
			string avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl();

			// For webhooks, we can get the original webhook name and avatar
			if (message is SocketUserMessage userMessage && userMessage.Author.IsWebhook)
			{
				// Webhook messages already have the correct username and avatar
				username = userMessage.Author.Username;
				avatarUrl = userMessage.Author.GetAvatarUrl() ?? avatarUrl;
			}

			// Collect attachments (images, videos, files)
			var embeds = new List<object>();
			if (message.Attachments.Any())
			{
				foreach (var attachment in message.Attachments)
				{
					// Create an embed for each attachment with the URL
					var embed = new
					{
						url = attachment.Url,
						image = new { url = attachment.Url }
					};
					embeds.Add(embed);
				}
			}

			// Prepare webhook payload (without thread_id in body)
			var payload = new
			{
				content = message.Content,
				username = username,
				avatar_url = avatarUrl,
				embeds = embeds.Count > 0 ? embeds : null
			};

			var json = System.Text.Json.JsonSerializer.Serialize(payload);
			var content = new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

			// Append thread_id as query parameter to the URL
			var webhookUrlWithThread = $"{shadowBotUrl}?thread_id={threadId}";

			using var httpClient = new HttpClient();
			await httpClient.PostAsync(webhookUrlWithThread, content);
		}
		catch (Exception ex)
		{
			DebugSay($"Error mirroring message to shadow ban: {ex.Message}");
		}
	}
}
