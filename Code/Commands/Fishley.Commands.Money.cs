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

	public class TransferCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("transfer")
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
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("reason")
			.WithDescription("The reason for the transfer")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("expiration")
			.WithDescription("How many seconds before this transfer expires")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.Integer));

		public override Func<SocketSlashCommand, Task> Function => Pay;

		public override Dictionary<string, Func<SocketMessageComponent, Task>> Components => new()
		{
			{ "transaction_accepted|", HandleTransactionResponse },
			{ "transaction_rejected|", HandleTransactionResponse },
			{ "transaction_cancelled|", HandleTransactionResponse }
		};

		public override bool SpamOnly => true;

		public async Task Pay(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;
			var amountString = (string)command.Data.Options.FirstOrDefault(x => x.Name == "amount")?.Value ?? null;
			var reason = (string)command.Data.Options.FirstOrDefault(x => x.Name == "reason")?.Value ?? null;
			var expiration = (int)(long)(command.Data.Options.FirstOrDefault(x => x.Name == "expiration")?.Value ?? 0L);

			if (targetUser.IsBot)
			{
				await command.RespondAsync($"Bots don't have a wallet.", ephemeral: true);
				return;
			}
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
			if (expiration < 0)
			{
				await command.RespondAsync($"Can't have negative expiration time.", ephemeral: true);
				return;
			}

			amount = MathF.Round(amount, 2, MidpointRounding.AwayFromZero); // Round to two digits
			var toPay = (decimal)amount;

			var payer = await GetOrCreateUser(command.User.Id);

			if (payer.Money < toPay)
			{
				await command.RespondAsync($"You don't have enough money!", ephemeral: true);
				return;
			}

			var transaction = new Transaction()
			{
				CreatorId = command.User.Id,
				TargetId = targetUser.Id,
				Type = TransactionType.Transfer,
				State = TransactionState.Pending,
				Amount = (float)toPay,
				Reason = reason,
				CreationDate = DateTime.UtcNow,
				OriginalCommand = command
			};

			if (expiration > 0)
				transaction.Expiration = DateTime.UtcNow.AddSeconds((double)expiration);

			AddActiveTransaction(transaction);

			await command.RespondAsync($"<@{command.User.Id}> sent <@{targetUser.Id}> a transfer.", embed: transaction.BuildEmbed(), components: transaction.BuildButtons());
		}
	}

	public class InvoiceCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("invoice")
		.WithDescription("Send someone an invoice")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to send the invoice to")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("amount")
			.WithDescription("How much to ask for")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("reason")
			.WithDescription("The reason for the invoice")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("expiration")
			.WithDescription("How many seconds before this invoice expires")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.Integer));

		public override Dictionary<string, Func<SocketMessageComponent, Task>> Components => new()
		{
			{ "transaction_accepted|", HandleTransactionResponse },
			{ "transaction_rejected|", HandleTransactionResponse },
			{ "transaction_cancelled|", HandleTransactionResponse }
		};

		public override Func<SocketSlashCommand, Task> Function => SendInvoice;

		public override bool SpamOnly => false;

		public async Task SendInvoice(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;
			var amountString = (string)command.Data.Options.FirstOrDefault(x => x.Name == "amount")?.Value ?? null;
			var reason = (string)command.Data.Options.FirstOrDefault(x => x.Name == "reason")?.Value ?? null;
			var expiration = (int)(long)(command.Data.Options.FirstOrDefault(x => x.Name == "expiration")?.Value ?? 0L);

			if (targetUser.IsBot)
			{
				await command.RespondAsync($"Bots don't have a wallet.", ephemeral: true);
				return;
			}
			if (targetUser.Id == command.User.Id)
			{
				await command.RespondAsync($"You can't send yourself an invoice!", ephemeral: true);
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
			if (expiration < 0)
			{
				await command.RespondAsync($"Can't have negative expiration time.", ephemeral: true);
				return;
			}

			amount = MathF.Round(amount, 2, MidpointRounding.AwayFromZero); // Round to two digits
			var toPay = (decimal)amount;

			var transaction = new Transaction()
			{
				CreatorId = command.User.Id,
				TargetId = targetUser.Id,
				Type = TransactionType.Invoice,
				State = TransactionState.Pending,
				Amount = (float)toPay,
				Reason = reason,
				CreationDate = DateTime.UtcNow,
				OriginalCommand = command
			};

			if (expiration > 0)
				transaction.Expiration = DateTime.UtcNow.AddSeconds((double)expiration);

			AddActiveTransaction(transaction);

			await command.RespondAsync($"<@{command.User.Id}> sent <@{targetUser.Id}> an invoice.", embed: transaction.BuildEmbed(), components: transaction.BuildButtons());
		}
	}
}