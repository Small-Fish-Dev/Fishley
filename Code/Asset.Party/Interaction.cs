namespace AssetParty;

public struct Interaction
{
	public bool Favourite { get; set; }
	public string FavouriteCreated { get; set; } // Always null? Maybe not even string?
	public string Rating { get; set; } // Always null? Maybe not even string?
	public string RatingCreated { get; set; } // Always null? Maybe not even string?
	public bool Used { get; set; }
	public string FirstUsed { get; set; } // Always null? Maybe not even string?
	public string LastUsed { get; set; } // Always null? Maybe not even string?
	public int Sessions { get; set; }
	public int Seconds { get; set; }
	public Interaction() { }
}