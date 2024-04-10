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

public partial class Fishley
{
	private static DiscordSocketClient _client;
	public static SocketGuild SmallFishServer;
	private static string _configPath => @"/home/ubre/Desktop/Fishley/config.json";
	public static Dictionary<string, string> Config { get; private set; }
	public static ulong SmallFishRole => ConfigGet<ulong>( "SmallFishRole", 1005599675530870824 );
	public static ulong AdminRole => ConfigGet<ulong>( "AdminRole", 1197217122183544862 );

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
        _client = new DiscordSocketClient(_config);

        InitializeDatabase();

		_client.Log += Log;
        _client.MessageUpdated += MessageUpdated;
		_client.MessageReceived += MessageReceived;
		_client.ReactionAdded += ReactionAdded;

		var token = ConfigGet( "Token", "ERROR" );

		if ( token == "ERROR" )
			DebugSay( "Token not found!" );

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();

		Thread.Sleep( 6000 );

		SmallFishServer = _client.GetGuild( ConfigGet<ulong>( "SmallFish", 1005596271907717140 ) );
		await CacheMessages();

		while ( true )
		{
			await OnUpdate();
			Thread.Sleep( 1000 );
		}
	}

	public static async Task OnUpdate()
	{
		if ( WarnDecaySecondsPassed >= WarnDecayCheckTimer )
		{	
			await WarnsDecayCheck();
			LastWarnDecayCheck = DateTime.UtcNow;
		}
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

	private static async Task CacheMessages()
	{
		foreach (var channel in SmallFishServer.TextChannels)
		{
			if ( SmallFishServer.CurrentUser.GetPermissions( channel ).ReadMessageHistory )
			{
				var messagesToDownload = 20; // Screw it just do 20 lol
				var messages = await channel.GetMessagesAsync(limit: messagesToDownload).FlattenAsync();
			}
		}

		DebugSay( "Messages cached" );
	}

	private static async Task ReactionAdded(Cacheable<IUserMessage, ulong> cacheableMessage, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
	{
		if ( reaction.Emote.Name == WarnEmoji )
		{
			if ( reaction.Channel is SocketGuildChannel guildChannel )
			{
				var giver = guildChannel.GetUser(reaction.UserId);
				
				if (giver != null)
				{
					if ( CanModerate( giver ) )
					{
						var message = await cacheableMessage.GetOrDownloadAsync();

						if (message != null)
						{
							var user = guildChannel.GetUser(message.Author.Id);
							
							if (user != null)
							{
								if (guildChannel is SocketTextChannel textChannel)
								{
									if ( user.Id == _client.CurrentUser.Id )
									{
										await textChannel.SendMessageAsync( $"<@{giver.Id}> attempted to warn... me!? What did I do???" );
									}
									else
									{
										if ( CanModerate( user ) )
											await textChannel.SendMessageAsync( $"<@{giver.Id}> attempted to warn <@{user.Id}> but I'm not powerful enough to do it." );
										else
											await AddWarn( user, textChannel, $"<@{giver.Id}> warned <@{user.Id}>" );
									}
								}
							}
						}
					}
				}
			}
		}
	}


	private static async Task MessageReceived(SocketMessage message)
    {
        if ( message is not SocketUserMessage userMessage )
            return;
		if ( userMessage.Author.IsBot )
			return;
			
		if ( userMessage.Content.Contains( "emergency" ) )
		{
			// emergency detected
			await message.Channel.SendMessageAsync( "emergency! emergency! exiting!" );
			Environment.Exit( 127 );
		}
		
		await HandleFilters( userMessage );
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