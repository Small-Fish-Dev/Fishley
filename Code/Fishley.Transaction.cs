namespace Fishley;

public partial class Fishley
{
	public static Dictionary<int, Transaction> ActiveTransactions { get; set; } = new();

	/// <summary>
	/// Try to get a transaction from the active transactions, returns null if not found
	/// </summary>
	/// <param name="transactionId"></param>
	/// <returns></returns>
	public static Transaction GetActiveTransaction(int transactionId)
	{
		if (ActiveTransactions.ContainsKey(transactionId))
			return ActiveTransactions[transactionId];
		else
			return null;
	}

	/// <summary>
	///  Add a new transaction to the active transactions
	/// </summary>
	/// <param name="transaction"></param>
	/// <returns></returns>
	public static bool AddActiveTransaction(Transaction transaction)
	{
		if (ActiveTransactions.ContainsKey(transaction.TransactionId)) return false;

		ActiveTransactions.Add(transaction.TransactionId, transaction);
		return true;
	}

	public static async Task HandleTransactionExpiration()
	{
		foreach (var transaction in ActiveTransactions.Values)
		{
			if (transaction.State != TransactionState.Pending) return;
			if (!transaction.Expired) return;

			transaction.State = TransactionState.Expired;
			await transaction.Update();
		}
	}

	public static async Task HandleTransactionResponse(SocketMessageComponent component)
	{
		var data = component.Data.CustomId.Split("-");
		var transactionId = int.Parse(data.Last());
		var transaction = GetActiveTransaction(transactionId);

		if (transaction == null)
		{
			await component.RespondAsync("Transaction is not active anymore.", ephemeral: true);
			return;
		}

		if (transaction.State != TransactionState.Pending)
		{
			await component.RespondAsync("Transaction has been completed already.", ephemeral: true);
			return;
		}

		if (data.First().Contains("cancelled"))
		{
			if (component.User.Id != transaction.CreatorId)
			{
				await component.RespondAsync("You're not the creator of this transaction.", ephemeral: true);
				return;
			}

			transaction.State = TransactionState.Cancelled;
			await transaction.Update();
			await component.RespondAsync("Transaction has been cancelled.", ephemeral: true);
			return;
		}

		if (component.User.Id != transaction.TargetId)
		{
			await component.RespondAsync("You're not the target of this transaction.", ephemeral: true);
			return;
		}

		if (data.First().Contains("rejected"))
		{
			transaction.State = TransactionState.Rejected;
			await transaction.Update();
			await component.RespondAsync("Transaction has been rejected.", ephemeral: true);
			return;
		}

		var creator = await GetOrCreateUser(transaction.CreatorId);
		var target = await GetOrCreateUser(transaction.TargetId);

		if (data.First().Contains("accepted"))
		{
			if (transaction.Type == TransactionType.Invoice)
			{
				if (target.Money < (decimal)transaction.Amount)
				{
					await component.RespondAsync("You don't have enough money to pay for this invoice.", ephemeral: true);
					return;
				}

				target.Money -= (decimal)transaction.Amount;
				creator.Money += (decimal)transaction.Amount;
			}
			if (transaction.Type == TransactionType.Transfer)
			{
				if (creator.Money < (decimal)transaction.Amount)
				{
					await component.RespondAsync("The sender doesn't have enough money to pay for this transfer.", ephemeral: true);
					return;
				}

				target.Money += (decimal)transaction.Amount;
				creator.Money -= (decimal)transaction.Amount;
			}

			await UpdateUser(target);
			await UpdateUser(creator);

			transaction.State = TransactionState.Accepted;
			await transaction.Update();

			if (transaction.Type == TransactionType.Invoice)
				await component.RespondAsync($"<@{transaction.TargetId}> paid an invoice of {NiceMoney(transaction.Amount)} from <@{transaction.CreatorId}>!");
			if (transaction.Type == TransactionType.Transfer)
				await component.RespondAsync($"<@{transaction.TargetId}> accepted a transfer of {NiceMoney(transaction.Amount)} from <@{transaction.CreatorId}>!");
		}
	}

	public enum TransactionState
	{
		Pending,
		Accepted,
		Rejected,
		Cancelled,
		Expired
	}

	public enum TransactionType
	{
		Transfer,
		Invoice
	}

	public class Transaction
	{
		public int TransactionId => GetHashCode();
		public ulong CreatorId { get; set; }
		public ulong TargetId { get; set; }
		public SocketUser Creator => SmallFishServer.GetUser(CreatorId);
		public SocketUser Target => SmallFishServer.GetUser(TargetId);
		public SocketSlashCommand OriginalCommand { get; set; }
		public float Amount { get; set; }
		public string Reason { get; set; }
		public TransactionType Type { get; set; }
		public TransactionState State { get; set; }
		public DateTime Expiration { get; set; }
		public DateTime CreationDate { get; set; }
		public bool Expires => Expiration != DateTime.MinValue;
		public bool Expired => Expired && Expiration <= DateTime.UtcNow;
		public string ExpirationEmbed => Expired ? "Expired!" : $"<t:{((DateTimeOffset)Expiration).ToUnixTimeSeconds()}:R>";
		public Color TransactionColor => State switch
		{
			TransactionState.Pending => Color.DarkGrey,
			TransactionState.Accepted => Color.DarkGreen,
			TransactionState.Rejected => Color.Red,
			TransactionState.Cancelled => Color.DarkGrey,
			TransactionState.Expired => Color.DarkGrey,
			_ => Color.DarkGrey
		};

		public async Task Update()
		{
			await OriginalCommand.ModifyOriginalResponseAsync(response =>
			{
				response.Components = BuildButtons();
				response.Embed = BuildEmbed();
			});
		}

		public Embed BuildEmbed()
		{
			var embedBuilder = new EmbedBuilder().WithTitle($"{Type.ToString()} - Global Bank of Small Fish")
				.WithAuthor(Creator)
				.WithColor(TransactionColor)
				.AddField("From:", Creator.GlobalName, true)
				.AddField("To:", Target.GlobalName, true)
				.AddField("Amount:", NiceMoney(Amount))
				.AddField("Reason:", $"\"{Reason}\"")
				.WithCurrentTimestamp();

			if (Expires)
				embedBuilder = embedBuilder.AddField("Expiration:", ExpirationEmbed);

			return embedBuilder.Build();
		}

		public MessageComponent BuildButtons()
		{
			var componentBuilder = new ComponentBuilder();

			switch (State)
			{
				case TransactionState.Pending:
					componentBuilder = componentBuilder
					.WithButton("Accept", $"transaction_accepted-{TransactionId}", ButtonStyle.Success)
					.WithButton("Reject", $"transaction_rejected-{TransactionId}", ButtonStyle.Danger)
					.WithButton("Cancel", $"transaction_cancelled-{TransactionId}", ButtonStyle.Secondary);
					break;
				case TransactionState.Accepted:
					componentBuilder = componentBuilder
					.WithButton("Accepted", "im_nothing_bro", style: ButtonStyle.Success, disabled: true);
					break;
				case TransactionState.Rejected:
					componentBuilder = componentBuilder
					.WithButton("Rejected", "im_nothing_bro", style: ButtonStyle.Danger, disabled: true);
					break;
				case TransactionState.Expired:
					componentBuilder = componentBuilder
					.WithButton("Expired", "im_nothing_bro", style: ButtonStyle.Secondary, disabled: true);
					break;
				case TransactionState.Cancelled:
					componentBuilder = componentBuilder
					.WithButton("Cancelled", "im_nothing_bro", style: ButtonStyle.Secondary, disabled: true);
					break;
			}

			return componentBuilder.Build();
		}

		public override bool Equals(object obj)
		{
			if (obj is Transaction transaction)
			{
				return Creator == transaction.Creator &&
					   Target == transaction.Target &&
					   Amount == transaction.Amount &&
					   Reason == transaction.Reason &&
					   Type == transaction.Type &&
					   State == transaction.State &&
					   Expiration == transaction.Expiration &&
					   CreationDate == transaction.CreationDate;
			}
			return false;
		}

		public override int GetHashCode()
		{
			HashCode hash = new HashCode();
			hash.Add(Creator);
			hash.Add(Target);
			hash.Add(Amount);
			hash.Add(Reason);
			hash.Add(Type);
			hash.Add(Expiration);
			hash.Add(CreationDate);
			return hash.ToHashCode();
		}

		public static bool operator ==(Transaction left, Transaction right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Transaction left, Transaction right)
		{
			return !Equals(left, right);
		}
	}
}