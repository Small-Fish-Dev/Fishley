namespace Fishley;

public partial class Fishley
{
	public class FineButton : DiscordSlashCommand
	{
		public override Dictionary<string, Func<SocketMessageComponent, Task>> Components => new()
		{
			{ "fine_paid-", PayFine }
		};

		public static async Task PayFine(SocketMessageComponent component)
		{
			var data = component.Data.CustomId.Split("-");
			var amount = (decimal)int.Parse(data[1]);
			var targetId = ulong.Parse(data[2]);
			var warnCount = int.Parse(data[3]);
			var targetIsPayee = component.User.Id == targetId;

			if (component.User.Id != targetId && !IsSmallFish((SocketGuildUser)component.User))
			{
				await component.RespondAsync("You can't pay someone else's fine.", ephemeral: true);
				return;
			}

			var payer = await GetOrCreateUser(component.User.Id);

			if (payer.Money < amount && targetIsPayee)
			{
				await component.RespondAsync("You don't have enough money to pay this fine.", ephemeral: true);
				return;
			}

			var target = await GetOrCreateUser(targetId);
			var targetDiscord = SmallFishServer.GetUser(targetId);
			var targetPronouns = IsSmallFish((SocketGuildUser)component.User) ? "They" : "You";

			if (target.Warnings <= 0 || !targetDiscord.Roles.Any(x => x == Warning1Role || x == Warning2Role || x == Warning3Role))
			{
				await component.RespondAsync($"{targetPronouns} have no warnings left.", ephemeral: true);
				return;
			}

			if (targetIsPayee)
			{
				payer.Money -= amount;
				await UpdateOrCreateUser(payer);
			}

			await RemoveWarn(targetDiscord, warnCount);

			if (targetIsPayee)
				await component.RespondAsync($"<@{component.User.Id}> paid their fine of {NiceMoney((float)amount)} and removed the warn!");
			else
				await component.Message.DeleteAsync();
		}
	}
}