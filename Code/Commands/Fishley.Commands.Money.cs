namespace Fishley;

public partial class Fishley
{
	public class BalanceCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("balance")
		.WithDescription("How much money do you have")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to check")
			.WithRequired(false)
			.WithType(ApplicationCommandOptionType.User));

		public override Func<SocketSlashCommand, Task> Function => Balance;

		public override bool SpamOnly => false;

		public async Task Balance(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;

			if (targetUser != null)
			{
				var user = await GetOrCreateUser(targetUser.Id);
				await command.RespondAsync($"{targetUser.GetUsername()} has {NiceMoney((float)user.Money)}");
			}
			else
			{
				var user = await GetOrCreateUser(command.User.Id);
				await command.RespondAsync($"You have {NiceMoney((float)user.Money)}");
			}
		}
	}

	public class LeaderboardsCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("leaderboards")
		.WithDescription("Top 10 users with most money");

		public override Func<SocketSlashCommand, Task> Function => GetLeaderboards;

		public override bool SpamOnly => false;

		public async Task GetLeaderboards(SocketSlashCommand command)
		{

			using (var db = new FishleyDbContext())
			{
				var users = db.Users;
				var foundUsers = users.AsAsyncEnumerable();
				var mostMoney = foundUsers.OrderByDescending(x => x.Money);
				var top10 = await mostMoney.Take(10).ToListAsync();

				var embedBuilder = new EmbedBuilder().WithTitle("Users with most money")
					.WithColor(Color.DarkGreen);

				var fieldString = "";
				var spot = 1;
				foreach (var user in top10)
				{
					if (user.Money <= 0) break;
					fieldString = $"{fieldString}#{spot} <@{user.UserId}>: {NiceMoney((float)user.Money)}\n";
					spot++;
				}

				embedBuilder = embedBuilder.AddField("Leaderboard", fieldString);

				await command.RespondAsync(embed: embedBuilder.Build());
			}
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

		public override bool SpamOnly => false;

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

	public class GiveMoneyCommand : DiscordSlashCommand
	{
		public override SlashCommandBuilder Builder => new SlashCommandBuilder()
		.WithName("givemoney")
		.WithDescription("Give money to someone, will be announced")
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("user")
			.WithDescription("Who to give money to")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.User))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("amount")
			.WithDescription("How much to give")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.AddOption(new SlashCommandOptionBuilder()
			.WithName("reason")
			.WithDescription("The reason that will be printed out")
			.WithRequired(true)
			.WithType(ApplicationCommandOptionType.String))
		.WithDefaultMemberPermissions(GuildPermission.BanMembers);

		public override Func<SocketSlashCommand, Task> Function => GiveMoney;

		public override bool SpamOnly => false;

		public async Task GiveMoney(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;
			var amountString = (string)command.Data.Options.FirstOrDefault(x => x.Name == "amount")?.Value ?? null;
			var reason = (string)command.Data.Options.FirstOrDefault(x => x.Name == "reason")?.Value ?? null;

			if (!IsSmallFish((SocketGuildUser)command.User))
			{
				await command.RespondAsync("Not small fish, bug off.", ephemeral: true);
				await ModeratorLog($"<@{command.User.Id}> attempted to use /givemoney but is not a moderator");
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

			amount = MathF.Round(amount, 2, MidpointRounding.AwayFromZero); // Round to two digits
			var toGive = (decimal)amount;


			var receiver = await GetOrCreateUser(targetUser.Id);
			receiver.Money += toGive;
			await UpdateOrCreateUser(receiver);
			await command.RespondAsync($"<@{command.User.Id}> gave {NiceMoney( (float)toGive )} to <@{targetUser.Id}>\n**Reason:** {reason}");
			await ModeratorLog($"<@{command.User.Id}> gave {NiceMoney( (float)toGive )} to <@{targetUser.Id}>\n**Reason:** {reason}\nNew balance: ${Math.Round(receiver.Money, 2)}");
		}
	}
}