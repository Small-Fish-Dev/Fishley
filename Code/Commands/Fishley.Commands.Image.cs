namespace Fishley;

public partial class Fishley
{
	public class ImageCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("generate")
		.WithDescription("Generate an image")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("prompt")
			.WithDescription("What to generate")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => GenerateImage;

		public override bool SpamOnly => false;

		public async Task GenerateImage(SocketSlashCommand command)
		{
			using (var typing = command.Channel.EnterTypingState())
			{
				var prompt = (string)command.Data.Options.First().Value;

				await command.RespondAsync($"Generating an image depicting: '{prompt}'");

				var image = await OpenAIImage(prompt );

				if ( image == null ) return;

				var embed = new EmbedBuilder().WithImageUrl( image ).Build();
				await SendMessage((SocketTextChannel)command.Channel, $"Here's what <@{command.User.Id}> asked: '{prompt}'", embed: embed);
			}
		}
	}
}