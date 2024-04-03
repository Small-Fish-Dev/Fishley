global using Discord.WebSocket;
global using Discord.Commands;
global using System;
global using System.Threading.Tasks;
global using System.Linq;
global using Discord;
global using System.Text.RegularExpressions;
global using System.Collections.Generic;
global using Newtonsoft.Json;
global using System.IO;

public partial class Fishley
{
	private static DiscordSocketClient _client;
	private static string _configPath => "config.json";
	public static Dictionary<string, string> Config { get; private set; }

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
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
		};
        _client = new DiscordSocketClient(_config);

		_client.Log += Log;
        _client.MessageUpdated += MessageUpdated;
		_client.MessageReceived += MessageReceived;

		var token = ConfigGet( "Token", "ERROR" );

		if ( token == "ERROR" )
			DebugSay( "Token not found!" );

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();

		// Block this task until the program is closed.
		await Task.Delay(-1);
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

	private static async Task MessageReceived(SocketMessage message)
    {
        if ( message is not SocketUserMessage userMessage )
            return;
		if ( userMessage.Author.IsBot )
			return;

		if ( !await HandleSimpleFilter( userMessage ) )
			if ( !await HandleComplicatedFilter( userMessage ) )
				await HandleConfusingFilter( userMessage );
    }

    private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message} -> {after}");
    }
}