public partial class Fishley
{
	public static List<Fish> AllFish { get; set; }
	public struct Fish
	{
		public string Name { get; set; }
		public string WikiPage { get; set; }
		public string WikiInfoPage { get; set; }
		public int MonthlyViews { get; set; }
	}

	public static async Task LoadFishes()
	{
		var jsonFile = await File.ReadAllTextAsync( @"/home/ubre/Desktop/Fishley/fishes.json" );
		AllFish = System.Text.Json.JsonSerializer.Deserialize<List<Fish>>( jsonFile );
	}
}