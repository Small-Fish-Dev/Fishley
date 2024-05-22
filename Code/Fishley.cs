global using Discord.WebSocket;
global using Discord.Commands;
global using System;
global using System.Threading.Tasks;
global using System.Threading;
global using System.Linq;
global using Discord;
global using System.Text.RegularExpressions;
global using System.Collections.Generic;
global using Newtonsoft.Json;
global using System.IO;
global using System.Net.Http;
global using Newtonsoft.Json.Linq;
global using HtmlAgilityPack;
global using System.Text.Json;
global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using System.ServiceModel.Syndication;
global using System.Xml;
global using System.Globalization;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Net;
global using AssetParty;
global using Animals;
global using OpenAI_API;

namespace Fishley;

public partial class Fishley
{
	public static DiscordSocketClient Client;
	public static ulong FishleyId => Client.CurrentUser.Id;
	public static SocketGuild SmallFishServer;
	private static string _configPath => @"/home/ubre/Desktop/Fishley/config.json";
	public static Dictionary<string, string> Config { get; private set; }
	public static bool Running { get; set; } = false;
	public static bool Emergency { get; set; } = false;
	public static DateTime LastMessage { get; set; } = DateTime.UtcNow;
	public static int SecondsSinceLastMessage => (int)(DateTime.UtcNow - LastMessage).TotalSeconds;
	public static HttpClient HttpClient { get; set; } = new HttpClient();
	public static Random Random { get; set; } = new Random((int)DateTime.UtcNow.Ticks);

	public static async Task Main()
	{
		if (!File.Exists(_configPath))
		{
			DebugSay("Config file not found!");
			return;
		}

		var configFile = File.ReadAllText(_configPath);
		Config = JsonConvert.DeserializeObject<Dictionary<string, string>>(configFile);

		var _config = new DiscordSocketConfig
		{
			MessageCacheSize = ConfigGet<int>("MessageCacheSize"),
			LogLevel = LogSeverity.Verbose,
			GatewayIntents = GatewayIntents.AllUnprivileged |
			GatewayIntents.MessageContent |
			GatewayIntents.GuildMembers |
			GatewayIntents.GuildMessages |
			GatewayIntents.DirectMessages |
			GatewayIntents.MessageContent |
			GatewayIntents.Guilds |
			GatewayIntents.DirectMessages |
			GatewayIntents.GuildMessageReactions
		};
		Client = new DiscordSocketClient(_config);

		Client.Log += Log;
		Client.MessageUpdated += MessageUpdated;
		Client.MessageReceived += MessageReceived;
		Client.ReactionAdded += ReactionAdded;
		Client.Ready += OnReady;
		Client.SlashCommandExecuted += SlashCommandHandler;
		Client.ButtonExecuted += ButtonHandler;

		var token = ConfigGet<string>("Token");

		if (token == null)
			DebugSay("Token not found!");

		await InitializeRarityGroups();
		InitiateOpenAI();

		await Client.LoginAsync(TokenType.Bot, token);
		await Client.StartAsync();

		while (true)
		{
			if (Running)
			{
				await OnUpdate();
				await Task.Delay(1000);
			}
		}
	}

	private static async Task OnReady()
	{
		var guildId = ConfigGet<ulong>("SmallFish");
		SmallFishServer = Client.GetGuild(guildId);
		await SmallFishServer.DownloadUsersAsync();

		//await SmallFishServer.DeleteApplicationCommandsAsync(); // Use only if you wanna remove a command

		var existingCommands = await SmallFishServer.GetApplicationCommandsAsync();

		foreach (var command in Commands.Values)
		{
			if (existingCommands.Any(x => command.Builder != null && x.Name == command.Builder.Name)) continue;
			if (command.Builder == null) continue;

			await SmallFishServer.CreateApplicationCommandAsync(command.Builder.Build());
			await Task.Delay(300); // Wait a bit in between
		} // Add any new commands

		var allCommandsUpdated = Commands.Values.Where(x => x.Builder != null)
		.Select(x => x.Builder.Build())
			.ToArray();
		await SmallFishServer.BulkOverwriteApplicationCommandAsync(allCommandsUpdated); // Update commands if they were modified

		Running = true;
		await Task.CompletedTask;
	}

	private static async Task OnUpdate()
	{
		if (!Running) return;

		await WarnsDecayCheck();
		await ComputeScrapers();
		await HandleTransactionExpiration();
	}

	/// <summary>
	/// Send a message in a channel, can reply to someone and can delete message after a while
	/// </summary>
	/// <param name="channel"></param>
	/// <param name="message"></param>
	/// <param name="messageToReply"></param>
	/// <param name="deleteAfterSeconds"></param>
	/// <param name="embed"></param>
	/// <param name="pathToUpload"></param>
	/// <returns></returns>
	public static async Task<bool> SendMessage(SocketTextChannel channel, string message, SocketMessage messageToReply = null, float deleteAfterSeconds = 0, Embed embed = null, string pathToUpload = null, MessageComponent component = null)
	{
		if (channel is null) return false;
		if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(message)) return false;

		MessageReference replyTo = null;

		if (messageToReply != null)
			replyTo = new MessageReference(messageToReply.Id);

		Discord.Rest.RestUserMessage sentMessage = null;

		if (pathToUpload != null)
			await channel.SendFileAsync(pathToUpload, message, messageReference: replyTo, embed: embed);
		else
		{
			await channel.SendMessageAsync(message, messageReference: replyTo, components: component, embed: embed);
		}

		if (sentMessage != null)
		{
			LastMessage = DateTime.UtcNow;

			if (deleteAfterSeconds > 0f)
				DeleteMessageAsync(sentMessage, deleteAfterSeconds);

			return true;
		}
		else
		{
			return false;
		}
	}

	private static async void DeleteMessageAsync(Discord.Rest.RestUserMessage message, float seconds)
	{
		if (message is null) return;

		await Task.Delay((int)(seconds * 1000));

		if (message is null) return;

		await message.DeleteAsync();
	}

	/// <summary>
	/// Get a value from the config file, else return a default value
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="key"></param>
	/// <returns></returns>
	public static T ConfigGet<T>(string key)
	{
		if (Config.TryGetValue(key, out var configValue))
		{
			try
			{
				T parsedValue = (T)Convert.ChangeType(configValue, typeof(T));
				return parsedValue;
			}
			catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
			{
				DebugSay($"Couldn't get {key} so I'm returning the default.");
				return default;
			}
		}

		DebugSay($"Couldn't get {key} so I'm returning the default.");
		return default;
	}

	/// <summary>
	/// Make Fishley say something in console!
	/// </summary>
	/// <param name="phrase"></param>
	internal static void DebugSay(string phrase)
	{
		Console.WriteLine($"Fishley: {phrase}");
	}

	private static Task Log(LogMessage msg)
	{
		DebugSay(msg.ToString());
		return Task.CompletedTask;
	}

	private static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
	{
		if (!Running) return;

		if (reaction.Channel is not SocketGuildChannel guildChannel) return;
		var giver = guildChannel.GetUser(reaction.UserId);
		if (giver is null) return;
		if (giver.IsBot) return;
		if (!reaction.Emote.Equals(WarnEmoji)) return;

		var message = await cacheableMessage.GetOrDownloadAsync();
		if (message is null) return;

		if (!CanModerate(giver))
		{
			await message.RemoveReactionAsync(reaction.Emote, reaction.UserId); // Remove any non moderator warning
			return;
		}

		var user = guildChannel.GetUser(message.Author.Id);
		if (user is null) return;

		if (guildChannel is not SocketTextChannel textChannel) return;
		if (message is not SocketMessage textMessage) return;
		if (textMessage.Reactions.FirstOrDefault(x => x.Key.Equals(WarnEmoji)).Value.ReactionCount >= 2) return; // Don't warn if this message led to a warn already

		if (user.Id == Client.CurrentUser.Id)
		{
			await SendMessage(textChannel, $"<@{giver.Id}> attempted to warn... me!? What did I do???", deleteAfterSeconds: 5f);
		}
		else
		{
			if (IsAdmin(user))
				await SendMessage(textChannel, $"<@{giver.Id}> attempted to warn <@{user.Id}> but I'm not powerful enough to do it.", deleteAfterSeconds: 5f);
			else
			{
				if (IsSmallFish(user) && !IsAdmin(user) && !IsAdmin(giver))
					await SendMessage(textChannel, $"<@{giver.Id}> attempted to warn <@{user.Id}> but they're not powerful enough to do it.", deleteAfterSeconds: 5f);
				else
					await AddWarn(user, textMessage, $"<@{giver.Id}> warned <@{user.Id}>", warnEmoteAlreadyThere: true);
			}
		}
	}

	private static async Task MessageReceived(SocketMessage message)
	{
		if (message is not SocketUserMessage userMessage)
			return;
		if (userMessage.Author.IsBot)
			return;

		if (!Running) return;

		if (!CanModerate((SocketGuildUser)message.Author))
		{
			if (await HandleFilters(userMessage))
				return;

			if (await ModerateMessage(message))
				return;
		}

		var mentioned = message.MentionedUsers.Any(user => user.Id == FishleyId);

		if (mentioned)
		{
			OpenAIRespond(message, CanModerate((SocketGuildUser)message.Author)); // Let's try not awaiting it
			return;
		}
	}

	private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
	{
		if (!Running) return;
		if (after is not SocketUserMessage userMessage)
			return;
		if (userMessage.Author.IsBot)
			return;

		await HandleFilters(userMessage);
	}
}