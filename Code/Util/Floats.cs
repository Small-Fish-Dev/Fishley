namespace Fishley;

// Thank you ChatGPT!
public partial class Fishley
{
	public static bool ParseFloat(string input, out float output)
	{
		string cleanInput = input.Replace(",", "");

		if (input.Contains(",") && !input.Contains("."))
		{
			cleanInput = input.Replace(",", ".");
		}

		if (float.TryParse(cleanInput, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
		{
			output = result;
			return true;
		}

		output = 0f;
		return false;
	}

	public static string NiceMoney(float number)
	{
		CultureInfo usCulture = CultureInfo.GetCultureInfo("en-US");
		return number.ToString("C2", usCulture);
	}
}