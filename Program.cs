using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using System.Text.RegularExpressions;

class Program
{
	private static DiscordSocketClient _client;

	public static async Task Main()
	{
        var _config = new DiscordSocketConfig
		{
			MessageCacheSize = 100,
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		};
        _client = new DiscordSocketClient(_config);

		_client.Log += Log;
        _client.MessageUpdated += MessageUpdated;
		_client.MessageReceived += MessageReceived;

		//  You can assign your bot token to a string, and pass that in to connect.
		//  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
		var token = "MTIyNDc4Nzk2MjI3ODA1MTg3MA.Go9Fck.CB6jnRYl1oKnZmMvKAP5Msv7ysSTfCbBcHXoLg";

		// Some alternative options would be to keep your token in an Environment Variable or a standalone file.
		// var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
		// var token = File.ReadAllText("token.txt");
		// var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();

		// Block this task until the program is closed.
		await Task.Delay(-1);
	}
	private static Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}

	private static async Task MessageReceived(SocketMessage message)
    {
        // Ensure we don't process system messages
        if (message is not SocketUserMessage userMessage)
            return;
		
		string pattern = @"[sŠS5][^a-z]?[iíijI1l!|][^a-z]?[mMnN][^a-z]?[]pP[^a-z]?[lłiíijI1l!|][eE3aA4@][lłiíijI1l!|]?";

		if ( Regex.IsMatch(userMessage.Content, pattern) )
		{
			var channel = userMessage.Channel as SocketTextChannel;
			await WarnUser( userMessage.Author as SocketGuildUser );
			await channel.SendMessageAsync( "You can't say that! Get warned!" );
		}

        Console.WriteLine($"{userMessage.Author}: {userMessage.Content}");
    }

    private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        // If the message was not in the cache, downloading it will result in getting a copy of `after`.
        var message = await before.GetOrDownloadAsync();
        Console.WriteLine($"{message} -> {after}");
    }

	

    private static async Task WarnUser(SocketGuildUser user)
    {
		if ( user.Roles.Any( r => r.Name.Contains( "⅓" )))
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault(r => r.Name.Contains( "⅓" )));
		else
			await user.AddRoleAsync(user.Guild.Roles.FirstOrDefault(r => r.Name.Contains( "⅔" )));
		
		await user.SetTimeOutAsync( TimeSpan.FromSeconds( 60 ) );
    }
}