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
			{ "transfer_paid-", TransferPaid },
			{ "transfer_unpaid-", TransferUnpaid }
		};

		public override bool SpamOnly => true;

		public async Task Pay(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.FirstOrDefault(x => x.Name == "user")?.Value ?? null;
			var amountString = (string)command.Data.Options.FirstOrDefault(x => x.Name == "amount")?.Value ?? null;
			var reason = (string)command.Data.Options.FirstOrDefault(x => x.Name == "reason")?.Value ?? null;
			var expiration = (int)(long)(command.Data.Options.FirstOrDefault(x => x.Name == "expiration")?.Value ?? 0);

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
			amount = MathF.Round(amount, 2, MidpointRounding.AwayFromZero); // Round to two digits
			var toPay = (decimal)amount;

			var payer = await GetOrCreateUser(command.User.Id);

			if (payer.Money < toPay)
			{
				await command.RespondAsync($"You don't have enough money!", ephemeral: true);
				return;
			}

			var components = new ComponentBuilder()
				.WithButton("Accept", $"transfer_paid-{targetUser.Id}-{command.User.Id}-{toPay}", ButtonStyle.Success)
				.WithButton("Reject", $"transfer_unpaid-{targetUser.Id}-{command.User.Id}-{toPay}", ButtonStyle.Danger)
				.Build();

			var embed = new EmbedBuilder().WithTitle($"Transfer - Global Bank of Small Fish")
				.WithAuthor(command.User)
				.WithColor(Color.DarkGreen)
				.AddField("From:", command.User.GlobalName, true)
				.AddField("To:", targetUser.GlobalName, true)
				.AddField("Amount to receive:", NiceMoney(amount))
				.AddField("Reason:", $"\"{reason}\"")
				.WithCurrentTimestamp()
				.Build();

			await command.RespondAsync($"<@{command.User.Id}> sent <@{targetUser.Id}> a transfer.", embed: embed, components: components);
		}

		public async Task TransferPaid(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Replace("transfer_paid-", "").Split("-");
			var targetId = ulong.Parse(data[0]);
			var creatorId = ulong.Parse(data[1]);
			var amountToPay = decimal.Parse(data[2]);

			if (component.User.Id != targetId)
			{
				await component.RespondAsync("You're not the receipient of this transfer.", ephemeral: true);
				return;
			}

			var creator = await GetOrCreateUser(creatorId);

			if (creator.Money < amountToPay)
			{
				await component.RespondAsync("The sender doesn't have enough money to pay for this transfer.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(targetId);
			target.Money += amountToPay;
			creator.Money -= amountToPay;

			await UpdateUser(target);
			await UpdateUser(creator);

			var disabledButton = new ComponentBuilder()
				.WithButton("Accepted", "im_nothing_bro", style: ButtonStyle.Success, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);
			await component.FollowupAsync($"<@{targetId}> accepted a transfer of {NiceMoney((float)amountToPay)} from <@{creatorId}>!");
		}

		public async Task TransferUnpaid(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Replace("transfer_unpaid-", "").Split("-");
			var targetId = ulong.Parse(data[0]);
			var creatorId = ulong.Parse(data[1]);
			var amountToPay = decimal.Parse(data[2]);

			if (component.User.Id != targetId)
			{
				await component.RespondAsync("You're not the recipient of this transfer.", ephemeral: true);
				return;
			}

			var disabledButton = new ComponentBuilder()
				.WithButton("Rejected", "im_nothing_bro", style: ButtonStyle.Danger, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);
			await component.FollowupAsync($"<@{targetId}> rejected a transfer of {NiceMoney((float)amountToPay)} from <@{creatorId}>!");
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
			{ "invoice_paid-", InvoicePaid },
			{ "invoice_unpaid-", InvoiceUnpaid }
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

			var components = new ComponentBuilder()
				.WithButton("Accept", $"invoice_paid-{targetUser.Id}-{command.User.Id}-{toPay}", ButtonStyle.Success)
				.WithButton("Reject", $"invoice_unpaid-{targetUser.Id}-{command.User.Id}-{toPay}", ButtonStyle.Danger)
				.Build();

			var embedBuilder = new EmbedBuilder().WithTitle($"Invoice - Global Bank of Small Fish")
				.WithAuthor(command.User)
				.WithColor(Color.DarkGreen)
				.AddField("From:", command.User.GlobalName, true)
				.AddField("To:", targetUser.GlobalName, true)
				.AddField("Amount to pay:", NiceMoney(amount))
				.AddField("Reason:", $"\"{reason}\"")
				.WithCurrentTimestamp();

			if (expiration > 0)
			{
				var futureTime = DateTime.UtcNow.AddSeconds((double)expiration);
				var unixTimestamp = ((DateTimeOffset)futureTime).ToUnixTimeSeconds();
				embedBuilder = embedBuilder.AddField("Expiration:", $"<t:{unixTimestamp}:R>");
			}

			await command.RespondAsync($"<@{command.User.Id}> sent <@{targetUser.Id}> an invoice.", embed: embedBuilder.Build(), components: components);
		}

		public async Task InvoicePaid(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Replace("invoice_paid-", "").Split("-");
			var targetId = ulong.Parse(data[0]);
			var creatorId = ulong.Parse(data[1]);
			var amountToPay = decimal.Parse(data[2]);

			if (component.User.Id != targetId)
			{
				await component.RespondAsync("You're not the recipient of this invoice.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(targetId);

			if (target.Money < amountToPay)
			{
				await component.RespondAsync("You don't have enough money to pay this invoice.", ephemeral: true);
				return;
			}

			var creator = await GetOrCreateUser(creatorId);
			target.Money -= amountToPay;
			creator.Money += amountToPay;

			await UpdateUser(target);
			await UpdateUser(creator);

			var disabledButton = new ComponentBuilder()
				.WithButton("Accepted", "im_nothing_bro", style: ButtonStyle.Success, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);
			await component.FollowupAsync($"<@{targetId}> accepted an invoice of {NiceMoney((float)amountToPay)} from <@{creatorId}>!");
		}

		public async Task InvoiceUnpaid(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Replace("invoice_unpaid-", "").Split("-");
			var targetId = ulong.Parse(data[0]);
			var creatorId = ulong.Parse(data[1]);
			var amountToPay = decimal.Parse(data[2]);

			if (component.User.Id != targetId)
			{
				await component.RespondAsync("You're not the receipient of this invoice.", ephemeral: true);
				return;
			}

			var disabledButton = new ComponentBuilder()
				.WithButton("Rejected", "im_nothing_bro", style: ButtonStyle.Danger, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);
			await component.FollowupAsync($"<@{targetId}> rejected an invoice of {NiceMoney((float)amountToPay)} from <@{creatorId}>!");
		}
	}
}