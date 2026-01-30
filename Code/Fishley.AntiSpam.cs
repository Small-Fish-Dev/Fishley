namespace Fishley;

public partial class Fishley
{
	private class SpamTracker
	{
		public float SpamLevel { get; set; }
		public string LastMessage { get; set; }
	}

	private static readonly Dictionary<ulong, SpamTracker> _spamTrackers = new();
	private static DateTime _lastSpamDecay = DateTime.UtcNow;
	private static readonly int SpamDecayInterval = 60; // seconds
	private static readonly float SpamDecayAmount = 10f;
	private static readonly float SpamThreshold = 10f;

	private static async Task CheckSpam(SocketUserMessage message, bool wasWarned = false)
	{
		var userId = message.Author.Id;

		// Get or create spam tracker for this user
		if (!_spamTrackers.ContainsKey(userId))
		{
			_spamTrackers[userId] = new SpamTracker
			{
				SpamLevel = 0,
				LastMessage = ""
			};
		}

		var tracker = _spamTrackers[userId];
		float spamToAdd = 1f; // Base spam for sending a message

		// +2 if message is more than 200 characters
		if (message.Content.Length > 200)
			spamToAdd += 2f;

		// +2 if message has a URL
		if (message.Content.Contains("http://") || message.Content.Contains("https://"))
			spamToAdd += 2f;

		// +2 if message got warned
		if (wasWarned)
			spamToAdd += 2f;

		// +2 if message is only emoji(s)
		if (IsOnlyEmojis(message.Content))
			spamToAdd += 2f;

		// +3 if message is the same as last message
		if (!string.IsNullOrEmpty(tracker.LastMessage) && tracker.LastMessage == message.Content)
			spamToAdd += 3f;

		// Update last message
		tracker.LastMessage = message.Content;

		// Add spam level
		tracker.SpamLevel += spamToAdd;

		// Check if they hit the threshold
		if (tracker.SpamLevel >= SpamThreshold)
		{
			tracker.SpamLevel -= 5f; // Reduce by 5 when warned
			await AddWarn(message.Author as SocketGuildUser, message, "Spam detected - slow down!", warnCount: 1);
			DebugSay($"Warned {message.Author.GetUsername()} for spam. New spam level: {tracker.SpamLevel}");
		}
	}

	private static bool IsOnlyEmojis(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return false;

		// Remove whitespace
		text = text.Replace(" ", "").Replace("\n", "").Replace("\r", "");

		if (text.Length == 0)
			return false;

		// Check if all characters are emoji/emoticon characters
		foreach (char c in text)
		{
			// Unicode emoji ranges and variation selectors
			if (char.IsLetterOrDigit(c) || char.IsPunctuation(c))
				return false;
		}

		return true;
	}

	private static async Task DecaySpamLevels()
	{
		var secondsPassed = (DateTime.UtcNow - _lastSpamDecay).TotalSeconds;

		if (secondsPassed >= SpamDecayInterval)
		{
			_lastSpamDecay = DateTime.UtcNow;

			// Reduce spam level for all users
			foreach (var tracker in _spamTrackers.Values)
			{
				tracker.SpamLevel = Math.Max(0, tracker.SpamLevel - SpamDecayAmount);
			}

			// Clean up users with 0 spam
			var usersToRemove = _spamTrackers.Where(x => x.Value.SpamLevel <= 0).Select(x => x.Key).ToList();
			foreach (var userId in usersToRemove)
			{
				_spamTrackers.Remove(userId);
			}

			if (_spamTrackers.Count > 0)
				DebugSay($"Decayed spam levels for {_spamTrackers.Count} users. Removed {usersToRemove.Count} users with 0 spam.");
		}
	}
}
