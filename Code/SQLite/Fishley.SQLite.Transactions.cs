namespace Fishley;

public partial class Fishley
{
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
		[Key]
		public int TransactionId { get; set; }
		public ulong CreatorId { get; set; }
		public ulong TargetId { get; set; }
		[NotMapped]
		public SocketUser Creator => SmallFishServer.GetUser(CreatorId);
		[NotMapped]
		public SocketUser Target => SmallFishServer.GetUser(TargetId);
		public float Amount { get; set; }
		public string Reason { get; set; }
		public TransactionType Type { get; set; }
		public TransactionState State { get; set; }
		public DateTime Expiration { get; set; }
		public DateTime CreationDate { get; set; }
		[NotMapped]
		public bool Expires => Expiration != DateTime.MinValue;
		[NotMapped]
		public bool Expired => Expired && Expiration <= DateTime.UtcNow;
		[NotMapped]
		public string ExpirationEmbed => Expired ? "Expired!" : $"<t:{((DateTimeOffset)Expiration).ToUnixTimeSeconds()}:R>";
		[NotMapped]
		public Color TransactionColor => State switch
		{
			TransactionState.Pending => Color.DarkGrey,
			TransactionState.Accepted => Color.DarkGreen,
			TransactionState.Rejected => Color.Red,
			TransactionState.Cancelled => Color.DarkGrey,
			TransactionState.Expired => Color.DarkGrey,
			_ => Color.DarkGrey
		};

		public Transaction(int transactionId)
		{
			TransactionId = transactionId;
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
					.WithButton("Accepted", style: ButtonStyle.Success, disabled: true);
					break;
				case TransactionState.Rejected:
					componentBuilder = componentBuilder
					.WithButton("Rejected", style: ButtonStyle.Danger, disabled: true);
					break;
				case TransactionState.Expired:
					componentBuilder = componentBuilder
					.WithButton("Expired", style: ButtonStyle.Secondary, disabled: true);
					break;
				case TransactionState.Cancelled:
					componentBuilder = componentBuilder
					.WithButton("Cancelled", style: ButtonStyle.Secondary, disabled: true);
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
			hash.Add(State);
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