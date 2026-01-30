using System.Text.Json;

namespace Fishley;

public partial class Fishley
{
	private static readonly Dictionary<ulong, ulong> ChannelToShadowMap = new()
	{
		{ 1005596274004852739, 1466892641135366246 }, // General Talk -> General Talk Shadow
		{ 1020718603298930728, 1466893111199268895 }, // Funny Memes -> Funny Memes Shadow
		{ 1141117812430078022, 1466893200856715397 }, // Sbox Feed -> Sbox Feed Shadow
		{ 1263929413792301058, 1466893264144568482 }, // WAYWO -> WAYWO Shadow
		{ 1005604067520823296, 1466893439441305875 }  // Zoology -> Zoology Shadow
	};

	private static readonly Dictionary<ulong, ulong> ShadowToChannelMap = new()
	{
		{ 1466892641135366246, 1005596274004852739 }, // General Talk Shadow -> General Talk
		{ 1466893111199268895, 1020718603298930728 }, // Funny Memes Shadow -> Funny Memes
		{ 1466893200856715397, 1141117812430078022 }, // Sbox Feed Shadow -> Sbox Feed
		{ 1466893264144568482, 1263929413792301058 }, // WAYWO Shadow -> WAYWO
		{ 1466893439441305875, 1005604067520823296 }  // Zoology Shadow -> Zoology
	};

	private static readonly Dictionary<ulong, string> ShadowToWebhookConfigMap = new()
	{
		{ 1466892641135366246, "GeneralTalkMirrorBot" },
		{ 1466893111199268895, "FunnyMemesMirrorBot" },
		{ 1466893200856715397, "SboxFeedMirrorBot" },
		{ 1466893264144568482, "WaywoMirrorBot" },
		{ 1466893439441305875, "ZoologyMirrorBot" }
	};

	private static readonly Dictionary<ulong, string> ChannelToWebhookConfigMap = new()
	{
		{ 1005596274004852739, "GeneralTalkMirrorBot" },
		{ 1020718603298930728, "FunnyMemesMirrorBot" },
		{ 1141117812430078022, "SboxFeedMirrorBot" },
		{ 1263929413792301058, "WaywoMirrorBot" },
		{ 1005604067520823296, "ZoologyMirrorBot" }
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
			// Check if this message is in one of the shadow channels
			if (!ShadowToChannelMap.TryGetValue(message.Channel.Id, out var channelId))
				return;

			// Get the webhook URL for this shadow channel
			if (!ShadowToWebhookConfigMap.TryGetValue(message.Channel.Id, out var webhookConfigKey))
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
			if (!ChannelToShadowMap.TryGetValue(message.Channel.Id, out var shadowChannelId))
				return;

			// Get the webhook URL for this channel
			if (!ChannelToWebhookConfigMap.TryGetValue(message.Channel.Id, out var webhookConfigKey))
				return;

			var webhookUrl = ConfigGet<string>(webhookConfigKey);
			if (string.IsNullOrEmpty(webhookUrl))
			{
				DebugSay($"{webhookConfigKey} not found in config!");
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

			// Prepare webhook payload
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

			using var httpClient = new HttpClient();
			await httpClient.PostAsync(webhookUrl, content);
		}
		catch (Exception ex)
		{
			DebugSay($"Error mirroring message to shadow ban: {ex.Message}");
		}
	}
}
