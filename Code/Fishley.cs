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
global using LiteDB;
global using System.Net.Http;
global using Newtonsoft.Json.Linq;
global using HtmlAgilityPack;
global using System.Text.Json;

public partial class Fishley
{
	public static DiscordSocketClient Client;
	public static ulong FishleyId => Client.CurrentUser.Id;
	public static SocketGuild SmallFishServer;
	private static string _configPath => @"/home/ubre/Desktop/Fishley/config.json";
	public static Dictionary<string, string> Config { get; private set; }
	public static ulong SmallFishRole => ConfigGet<ulong>( "SmallFishRole", 1005599675530870824 );
	public static ulong AdminRole => ConfigGet<ulong>( "AdminRole", 1197217122183544862 );
	public static bool Running { get; set; } = false;
	public static DateTime LastMessage { get; set; } = DateTime.UtcNow;
	public static int SecondsSinceLastMessage => (int)( DateTime.UtcNow - LastMessage ).TotalSeconds;

	public static async Task Main()
	{		
		if ( !File.Exists( _configPath ) )
		{
			DebugSay( "Config file not found!" );
			return;
		}
		
		var configFile = File.ReadAllText( _configPath );
		Config = JsonConvert.DeserializeObject<Dictionary<string, string>>( configFile );

        var _config = new DiscordSocketConfig
		{
			MessageCacheSize = ConfigGet<int>( "MessageCacheSize", 100 ),
			GatewayIntents = GatewayIntents.AllUnprivileged |
			GatewayIntents.MessageContent |
			GatewayIntents.GuildMembers |
			GatewayIntents.GuildMessages |
			GatewayIntents.DirectMessages |
			GatewayIntents.MessageContent
		};
        Client = new DiscordSocketClient(_config);

        InitializeDatabase();
		await LoadFishes();

		Console.WriteLine( $"Found {AllFish.Count()} fishes" );

		var decileGroups = from fish in AllFish
                           orderby fish.MonthlyViews
                           group fish by GetDecile(AllFish, fish.MonthlyViews) into decileGroup
                           select new
                           {
                               Decile = decileGroup.Key,
                               AverageMonthlyViews = decileGroup.Average(x => x.MonthlyViews)
                           };

        // Output the results
        foreach (var group in decileGroups)
        {
            Console.WriteLine($"Decile {group.Decile}: Average Monthly Views = {group.AverageMonthlyViews:F2}");
        }
		
		
		static int GetDecile(List<Fish> fishes, int monthlyViews)
		{
			int index = fishes.OrderBy(f => f.MonthlyViews).ToList().FindIndex(f => f.MonthlyViews == monthlyViews);
			return index * 10 / fishes.Count;
		}

		Client.Log += Log;
        Client.MessageUpdated += MessageUpdated;
		Client.MessageReceived += MessageReceived;
		Client.ReactionAdded += ReactionAdded;
		Client.Ready += OnReady;
		Client.SlashCommandExecuted += SlashCommandHandler;

		var token = ConfigGet( "Token", "ERROR" );

		if ( token == "ERROR" )
			DebugSay( "Token not found!" );

		await Client.LoginAsync(TokenType.Bot, token);
		await Client.StartAsync();

		while ( true )
		{
			if ( Running )
			{
				await OnUpdate();
				await Task.Delay( 1000 );
			}
		}
	}

	private static async Task OnReady() 
	{
		var guildId = ConfigGet<ulong>( "SmallFish", 1005596271907717140 );
		SmallFishServer = Client.GetGuild( guildId );
		await SmallFishServer.DownloadUsersAsync();

		foreach ( var command in Commands.Values )
			await SmallFishServer.CreateApplicationCommandAsync( command.Builder.Build() );
		
		Running = true;
		await Task.CompletedTask;
	}

	private static async Task OnUpdate()
	{
		if ( WarnDecaySecondsPassed >= WarnDecayCheckTimer )
		{	
			await WarnsDecayCheck();
			LastWarnDecayCheck = DateTime.UtcNow;
		}
	}

	/// <summary>
	/// Send a message in a channel, can reply to someone and can delete message after a while
	/// </summary>
	/// <param name="channel"></param>
	/// <param name="message"></param>
	/// <param name="messageToReply"></param>
	/// <param name="deleteAfterSeconds"></param>
	/// <returns></returns>
	public static async Task<bool> SendMessage( SocketTextChannel channel, string message, SocketMessage messageToReply = null, float deleteAfterSeconds = 0 )
	{
		if ( channel is null ) return false;
		if ( string.IsNullOrWhiteSpace( message ) || string.IsNullOrEmpty( message ) ) return false;

		MessageReference replyTo = null;

		if ( messageToReply != null )
			replyTo = new MessageReference( messageToReply.Id );

		var sentMessage = await channel.SendMessageAsync( message, messageReference: replyTo );

		if ( sentMessage != null )
		{
			LastMessage = DateTime.UtcNow;

			if ( deleteAfterSeconds > 0f )
				DeleteMessageAsync( sentMessage, deleteAfterSeconds );

			return true;
		}
		else
		{
			return false;
		}
	}

	private static async void DeleteMessageAsync( Discord.Rest.RestUserMessage message, float seconds )
	{
		if ( message is null ) return;

		await Task.Delay( (int)(seconds * 1000) );

		if ( message is null ) return;

		await message.DeleteAsync();
	}

	/// <summary>
	/// Get a value from the config file, else return a default value
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static T ConfigGet<T>( string key, T defaultValue )
	{
		if ( Config.TryGetValue( key, out var configValue ) )
		{
			try
			{
				T parsedValue = (T)Convert.ChangeType(configValue, typeof(T));
				return parsedValue;
			}
			catch (FormatException)
			{
				return defaultValue;
			}
		}

		DebugSay( $"Couldn't get {key} so I'm using the default of {defaultValue}");
		return defaultValue;
	}

	/// <summary>
	/// Get a value from the config file, else return a default value
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static string ConfigGet( string key, string defaultValue )
	{
		if ( Config.TryGetValue( key, out var configValue ) )
			return configValue;

		DebugSay( $"Couldn't get {key} so I'm using the default of {defaultValue}");
		return defaultValue;
	}

	/// <summary>
	/// Make Fishley say something in console!
	/// </summary>
	/// <param name="phrase"></param>
	internal static void DebugSay( string phrase )
	{
		Console.WriteLine( $"Fishley: {phrase}" );
	}

	private static Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}

	private static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
	{
		if ( reaction.Emote.Name != WarnEmoji ) return;
		if ( reaction.Channel is not SocketGuildChannel guildChannel ) return;

		var giver = guildChannel.GetUser(reaction.UserId);
		if ( giver is null || !CanModerate( giver ) ) return;
		
		var message = await cacheableMessage.GetOrDownloadAsync();
		if ( message is null ) return;
		
		var user = guildChannel.GetUser(message.Author.Id);
		if ( user is null ) return;

		if ( guildChannel is not SocketTextChannel textChannel ) return;
		if (message is not SocketMessage textMessage) return;

		if ( user.Id == Client.CurrentUser.Id )
		{
			await SendMessage( textChannel, $"<@{giver.Id}> attempted to warn... me!? What did I do???", deleteAfterSeconds: 5f );
		}
		else
		{
			if ( CanModerate( user ) )
				await SendMessage( textChannel, $"<@{giver.Id}> attempted to warn <@{user.Id}> but I'm not powerful enough to do it.", deleteAfterSeconds: 5f );
			else
				await AddWarn( user, textMessage, $"<@{giver.Id}> warned <@{user.Id}>" );
		}
	}

	private static async Task MessageReceived(SocketMessage message)
    {
        if ( message is not SocketUserMessage userMessage )
            return;
		if ( userMessage.Author.IsBot )
			return;

		if ( userMessage.Author is SocketGuildUser sender )
		{
			if ( CanModerate( sender ) )
			{
				if ( userMessage.Content.Contains( "emergency", StringComparison.OrdinalIgnoreCase ) )
				{
					// emergancy detected
					//await message.Channel.SendMessageAsync( "Emergency protocol initiated. Shutting down." );
					//Environment.Exit( 127 );
				}
			}
		}
		
		if ( await HandleFilters( userMessage ) )
			return;
		
		var mentioned = message.MentionedUsers.Any(user => user.Id == FishleyId);

		if (mentioned)
		{
			List<string> phrases = new List<string>
			{
				"Fintastic day we're having!",
				"Let minnow if you need anything!",
				"Small fish, big dreams!",
				"For what porpoise you call me?"
			};

			Random random = new Random( (int)DateTime.UtcNow.Ticks );
			int randomIndex = random.Next(phrases.Count);
			string randomPhrase = phrases[randomIndex];

			var reference = new MessageReference( message.Id );
			await message.Channel.SendMessageAsync( randomPhrase, messageReference: reference );
		}
    }

    private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        if ( after is not SocketUserMessage userMessage )
            return;
		if ( userMessage.Author.IsBot )
			return;

		await HandleFilters( userMessage );
    }

	public static bool IsSmallFish( SocketGuildUser user ) => user.Roles.Any( x => x.Id == SmallFishRole );
	public static bool IsAdmin( SocketGuildUser user ) => user.Roles.Any( x => x.Id == AdminRole );
	public static bool CanModerate( SocketGuildUser user ) => IsAdmin( user ) || IsSmallFish( user );
}