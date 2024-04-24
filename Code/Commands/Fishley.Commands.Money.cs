namespace Fishley;

public partial class Fishley
{
	public class BalanceCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("balance")
		.WithDescription("How much money do you have");

		public override Func<SocketSlashCommand, Task> Function => Balance;

		public override bool SpamOnly => true;

		public async Task Balance(SocketSlashCommand command)
		{
			var user = await GetOrCreateUser(command.User.Id);

			await command.RespondAsync($"You have {NiceMoney((float)user.Money)}");
		}
	}

	public class PayCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("pay")
		.WithDescription("Pay someone")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to pay")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("amount")
			.WithDescription("How much to send over")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String));

		public override Func<SocketSlashCommand, Task> Function => Pay;

		public override bool SpamOnly => true;

		public async Task Pay(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.First().Value;
			var amountString = (string)command.Data.Options.Last().Value;

			if (targetUser.Id == command.User.Id)
			{
				await command.RespondAsync($"You can't send yourself money!", ephemeral: true);
				return;
			}
			if (!ParseFloat(amountString, out var amount))
			{
				await command.RespondAsync($"Please input a real number!", ephemeral: true);
				return;
			}
			if (amount < 0.01f)
			{
				await command.RespondAsync($"Minimum amount is 0.01!", ephemeral: true);
				return;
			}
			amount = MathF.Round(amount, 2, MidpointRounding.AwayFromZero); // Round to three digits
			var toPay = (decimal)amount;

			var payer = await GetOrCreateUser(command.User.Id);
			var payee = await GetOrCreateUser(targetUser.Id);

			if (payer.Money < toPay)
			{
				await command.RespondAsync($"You don't have enough money!", ephemeral: true);
				return;
			}

			payer.Money -= toPay;
			payee.Money += toPay;

			await UpdateUser(payer);
			await UpdateUser(payee);

			await command.RespondAsync($"<@{command.User.Id}> sent {NiceMoney(amount)} to <@{targetUser.Id}>");
		}
	}
}