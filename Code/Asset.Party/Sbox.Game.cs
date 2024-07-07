global using System;
global using System.Threading.Tasks;
global using System.Threading;
global using System.Linq;
global using System.Text.RegularExpressions;
global using System.Collections.Generic;
global using Newtonsoft.Json;
global using System.IO;
global using System.Net.Http;
global using Newtonsoft.Json.Linq;
global using HtmlAgilityPack;
global using System.Text.Json;
global using System.ComponentModel.DataAnnotations;
global using System.ServiceModel.Syndication;
global using System.Xml;
global using System.Globalization;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Net;

namespace SboxGame;

public partial class SboxGame
{
	public static string ServicesUrl => "https://services.facepunch.com/sbox/package/find?q=";
	public static string SboxGameUrl => "https://sbox.game/";

	public static async Task<Query> QueryAsync(PackageType packageType = PackageType.All, QuerySort sortType = QuerySort.All) // TODO: Add tags, orders, and facets (They must all be strings unfortunately)
	{
		var finalUrl = ServicesUrl;

		if (packageType != PackageType.All)
			finalUrl = $@"{finalUrl}type:{packageType.ToString().ToLower()}%20";

		if (sortType != QuerySort.All)
			finalUrl = $@"{finalUrl}sort:{sortType.ToString().ToLower()}%20";

		using (HttpClient client = new HttpClient())
		{
			HttpResponseMessage response = await client.GetAsync(finalUrl);

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Response unsuccesful. Status code: {response.StatusCode}");
				return null;
			}

			string jsonContent = await response.Content.ReadAsStringAsync();

			if (string.IsNullOrEmpty(jsonContent))
			{
				Console.WriteLine("Sbox.Game query was empty.");
				return null;
			}

			var deserializerSettings = new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore
			};

			return JsonConvert.DeserializeObject<Query>(jsonContent, deserializerSettings);
		}
	}
}