namespace SboxGame;

public struct Score
{
	public int Rating { get; set; }
	public int Total { get; set; }
	public List<int> PreviousDays { get; set; }
	public List<int> PreviousMonths { get; set; }

	public Score() { }
}

public struct ReviewStats
{
	public List<Score> Scores { get; set; }
	public ReviewStats() { }
}