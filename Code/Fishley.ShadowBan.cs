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

	private static readonly Dictionary<ulong, ulong> ThreadToChannelMap = new()
	{
		{ 1466832968277426318, 1005596274004852739 }, // General Talk Thread -> General Talk
		{ 1466833014498529372, 1020718603298930728 }, // Funny Memes Thread -> Funny Memes
		{ 1466833091632042281, 1141117812430078022 }, // Sbox Feed Thread -> Sbox Feed
		{ 1466833122044805242, 1263929413792301058 }, // WAYWO Thread -> WAYWO
		{ 1466833172263338188, 1005604067520823296 }  // Zoology Thread -> Zoology
	};

	private static readonly Dictionary<ulong, string> ThreadToWebhookConfigMap = new()
	{
		{ 1466832968277426318, "GeneralTalkShadowBot" },
		{ 1466833014498529372, "FunnyMemesShadowBot" },
		{ 1466833091632042281, "SboxFeedShadowBot" },
		{ 1466833122044805242, "WaywoShadowBot" },
		{ 1466833172263338188, "ZoologyShadowBot" }
	};

	private static string ApplyShadowDimensionFilter(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		var random = new Random();
		var chars = text.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			// Only replace letters (not spaces, punctuation, etc.)
			if (char.IsLetter(chars[i]) && random.Next(0, 100) < 40) // 40% chance to replace
			{
				chars[i] = '.';
				// Add second dot
				if (i + 1 < chars.Length)
				{
					chars[i + 1] = '.';
					i++; // Skip next character since we just replaced it
				}
			}
		}

		return new string(chars);
	}

	private static async Task MirrorMessageFromShadowToNormal(SocketMessage message)
	{
		try
		{
			// Check if this message is in one of the shadow threads
			if (!ThreadToChannelMap.TryGetValue(message.Channel.Id, out var channelId))
				return;

			// Get the webhook URL for this thread
			if (!ThreadToWebhookConfigMap.TryGetValue(message.Channel.Id, out var webhookConfigKey))
				return;

			var webhookUrl = ConfigGet<string>(webhookConfigKey);
			if (string.IsNullOrEmpty(webhookUrl))
			{
				DebugSay($"{webhookConfigKey} not found in config!");
				return;
			}

			// Apply shadow dimension filter to the message
			var filteredContent = ApplyShadowDimensionFilter(message.Content);

			// Get user info and add "Echoes of " prefix
			string username = $"Echoes of {message.Author.GetUsername()}";
			string avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl();

			// Apply grayscale filter to avatar using TheImageView.app
			string grayscaleAvatarUrl = $"https://theimageview.app/placeholder?url={Uri.EscapeDataString(avatarUrl)}&grayscale=1";

			// Prepare webhook payload with allowed_mentions to only allow user pings
			var payload = new
			{
				content = filteredContent,
				username = username,
				avatar_url = grayscaleAvatarUrl,
				allowed_mentions = new
				{
					parse = new[] { "users" } // Only allow user pings, not @everyone or @here
				}
			};

			var json = System.Text.Json.JsonSerializer.Serialize(payload);
			var content = new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

			using var httpClient = new HttpClient();
			await httpClient.PostAsync(webhookUrl, content);
		}
		catch (Exception ex)
		{
			DebugSay($"Error mirroring message from shadow to normal: {ex.Message}");
		}
	}

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
			string username = message.Author.GetUsername();
			string avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl();

			// For webhooks, we can get the original webhook name and avatar
			if (message is SocketUserMessage userMessage && userMessage.Author.IsWebhook)
			{
				// Webhook messages already have the correct username and avatar
				username = userMessage.Author.GetUsername();
				avatarUrl = userMessage.Author.GetAvatarUrl() ?? avatarUrl;

				// Don't mirror messages that came from shadow realm (prevent infinite loop)
				if (username.StartsWith("Echoes of "))
					return;
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
				embeds = embeds.Count > 0 ? embeds : null,
				allowed_mentions = new
				{
					parse = new[] { "users" } // Only allow user pings, not @everyone or @here
				}
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
