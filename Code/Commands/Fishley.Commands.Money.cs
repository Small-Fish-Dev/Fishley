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
			.WithType(ApplicationCommandOptionType.String));

		public override Dictionary<string, Func<SocketMessageComponent, Task>> Components => new()
		{
			{ "invoice_paid-", InvoicePaid }
		};

		public override Func<SocketSlashCommand, Task> Function => SendInvoice;

		public override bool SpamOnly => false;

		public async Task SendInvoice(SocketSlashCommand command)
		{
			var targetUser = (SocketUser)command.Data.Options.First().Value;
			var amountString = (string)command.Data.Options.FirstOrDefault(x => x.Name == "amount")?.Value ?? null;
			var reason = (string)command.Data.Options.Last().Value;

			if (targetUser.IsBot)
			{
				await command.RespondAsync($"Bots don't have a wallet.", ephemeral: true);
				return;
			}
			if (targetUser.Id == command.User.Id)
			{
				await command.RespondAsync($"You can't send yourself an invoide!", ephemeral: true);
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

			var button = new ComponentBuilder()
				.WithButton("Accept", $"invoice_paid-{targetUser.Id}-{command.User.Id}-{toPay}", ButtonStyle.Success)
				.Build();

			DebugSay($"invoice_paid-{command.User.Id}-{toPay}");
			var embed = new EmbedBuilder().WithTitle($"Invoice - Global Bank of Small Fish")
				.WithAuthor(command.User)
				.WithColor(Color.DarkGreen)
				.AddField("From:", command.User.GlobalName, true)
				.AddField("To:", targetUser.GlobalName, true)
				.AddField("Amount to pay:", NiceMoney(amount))
				.AddField("Reason:", $"\"{reason}\"")
				.WithCurrentTimestamp()
				.Build();

			await command.RespondAsync($"<@{command.User.Id}> sent <@{targetUser.Id}> an invoice.", embed: embed, components: button);
		}


		public async Task InvoicePaid(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Replace("invoice_paid-", "").Split("-");
			var targetId = ulong.Parse(data[0]);
			var creatorId = ulong.Parse(data[1]);
			var amountToPay = decimal.Parse(data[2]);

			if (component.User.Id != targetId)
			{
				await component.RespondAsync("You're not the receipient of this invoice.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(component.User.Id);

			if (target.Money < amountToPay)
			{
				await component.RespondAsync("You don't have enough money to pay this invoice.", ephemeral: true);
				return;
			}

			var creator = await GetOrCreateUser(component.User.Id);
			target.Money -= amountToPay;
			creator.Money += amountToPay;

			await UpdateUser(target);
			await UpdateUser(creator);

			var disabledButton = new ComponentBuilder()
				.WithButton("Paid", "im_nothing_bro", style: ButtonStyle.Success, disabled: true)
				.Build();

			await component.UpdateAsync(x => x.Components = disabledButton);
			await component.FollowupAsync($"<@{targetId}> paid an invoice of {NiceMoney((float)amountToPay)} to <@{creatorId}>!");
		}
	}
}