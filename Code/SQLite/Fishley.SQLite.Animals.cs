namespace Fishley;

public partial class Fishley
{
	public class AnimalsDatabase : DbContext
	{
		public DbSet<AnimalEntry> Animals { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=animals.db");
		}
	}

	/// <summary>
	/// Get an animal entry by id
	/// </summary>
	/// <param name="entryId"></param>
	/// <returns></returns>
	public static async Task<AnimalEntry> GetEntry(int entryId)
	{
		using (var db = new AnimalsDatabase())
		{
			var animal = await db.Animals.FindAsync(entryId);

			if (animal == null) return null;

			return animal;
		}
	}

	/// <summary>
	/// Remove an animal entry by id
	/// </summary>
	/// <param name="entryId"></param>
	/// <returns></returns>
	public static async Task<bool> RemoveEntry(int entryId)
	{
		using (var db = new AnimalsDatabase())
		{
			var animal = await db.Animals.FindAsync(entryId);

			if (animal == null) return false;

			db.Animals.Remove(animal);

			await db.SaveChangesAsync();
			return true;
		}
	}

	public static async Task UpdateOrCreateEntry(AnimalEntry entry)
	{
		using (var db = new AnimalsDatabase())
		{
			var foundAnimal = await db.Animals.FindAsync(entry.Id);

			if (foundAnimal == null)
				db.Animals.Add(entry);
			else
				db.Entry(foundAnimal).CurrentValues.SetValues(entry);

			await db.SaveChangesAsync();
		}
	}

	/// <summary>
	/// Go through every list item in this page and catalogue them into AnimalEntries if valid
	/// </summary>
	/// <param name="pageUrl"></param>
	/// <param name="waitBetweelCalls"></param>
	/// <param name="startingUrl"></param>
	/// <param name="endingUrl"></param>
	public static async Task CatalogueListedPage(string pageUrl, int waitBetweenCalls = 1000, string startingUrl = null, string endingUrl = null)
	{
		var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
		var loadedPage = await Wikipedia.LoadPage(httpClient, pageUrl, waitBetweenCalls);

		// Get every valid hyperlink that is inside of a list
		var outgoingLinks = loadedPage.DocumentNode.SelectNodes("//ul/li/a[contains(@href, '/wiki/') and not(contains(@href, ':'))]");

		if (outgoingLinks != null)
		{
			foreach (var linkNode in outgoingLinks)
			{
				var reference = linkNode.Attributes["href"]?.Value ?? null;
				var innerText = linkNode.InnerText;

				if (reference == null) continue;

				reference = Wikipedia.WikipediaAbsolutePath(reference);
				await Task.Delay(waitBetweenCalls);
				var animal = await Wikipedia.CataloguePage(httpClient, reference, waitBetweenCalls);

				if (animal != null)
				{
					var animalFound = await GetEntry(animal.Id);
					if (animalFound == null)
					{
						Console.WriteLine($"Added {animal.Id} {animal.WikiPage} {animal.ScientificName}");
						await UpdateOrCreateEntry(animal);
					}
					else
					{
						Console.WriteLine($"{animal.Id} already exists, updating");
						await UpdateOrCreateEntry(animal);
					}
				}
			}
		}
	}
}