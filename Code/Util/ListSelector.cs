namespace Fishley;

// Thank you ChatGPT!
public class ListSelector
{
    private Random _random = new Random( (int)DateTime.UtcNow.Ticks );

	/// <summary>
	/// Selects an item from the specified collection using a normally distributed random index.
	/// This method implements the Box-Muller transform to generate a normally distributed index based on the specified mean and standard deviation,
	/// allowing for controlled randomness in the selection from the collection. The selection can be biased towards the middle or any part of the collection
	/// by adjusting the mean and standard deviation. Note that this method is best used with collections that allow efficient indexing.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the collection.</typeparam>
	/// <param name="items">The collection of items from which to select. Should ideally support efficient random access.</param>
	/// <param name="mean">The mean of the normal distribution, which determines the index around which selections are centered.</param>
	/// <param name="stdDev">The standard deviation of the normal distribution, which affects the spread of the index selection.
	/// A smaller standard deviation results in a higher likelihood of selecting items near the mean, whereas a larger standard deviation
	/// increases the chances of selecting items further from the mean.</param>
	/// <returns>A randomly selected item from the collection, chosen according to the specified normal distribution.</returns>
    public T SelectItem<T>(IEnumerable<T> items, double mean, double stdDev)
    {
        // Convert enumerable to list for indexing
        List<T> itemList = items.ToList();
        int itemCount = itemList.Count;
        int index = 0;

        do
        {
            // Generate two uniformly distributed random numbers
            double u1 = 1.0 - _random.NextDouble(); // Subtractive to avoid zero
            double u2 = 1.0 - _random.NextDouble();
            
            // Box-Muller transform to get one standard normal variable
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            
            // Scale and shift using mean and standard deviation input
            double randNormal = mean + stdDev * randStdNormal;

            // Round to nearest index and ensure it's within bounds
            index = (int)Math.Round(randNormal);
        } while (index < 0 || index >= itemCount); // Retry if the index is out of bounds

        return itemList[index];
    }
}