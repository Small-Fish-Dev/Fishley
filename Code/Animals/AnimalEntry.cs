global using Discord.WebSocket;
global using Discord.Commands;
global using System;
global using System.Threading.Tasks;
global using System.Threading;
global using System.Linq;
global using Discord;
global using System.Text.RegularExpressions;
global using System.Collections.Generic;
global using Newtonsoft.Json;
global using System.IO;
global using System.Net.Http;
global using Newtonsoft.Json.Linq;
global using HtmlAgilityPack;
global using System.Text.Json;
global using Microsoft.EntityFrameworkCore;
global using System.ComponentModel.DataAnnotations;
global using System.ServiceModel.Syndication;
global using System.Xml;
global using System.Globalization;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Net;

namespace Animals;

/// <summary>
/// IUCN Red List
/// </summary>
public enum ConservationStatus
{
	Not_Evaluated,
	Data_Deficient,
	Least_Concern,
	Conservation_Dependent,
	Near_Threatened,
	Vulnerable,
	Endangered,
	Critically_Endangered,
	Extinct_In_The_Wild,
	Extinct
}

public class TaxonomicGroup
{
	public string Name { get; set; }
	public string Url { get; set; }

	public TaxonomicGroup(string name, string url)
	{
		Name = name;
		Url = url;
	}
}

public class Biota
{
	public string CommonName { get; set; }
	public ConservationStatus ConservationStatus { get; set; }
	public string BinomialName { get; set; }
	public string TrinomialName { get; set; }
	public TaxonomicGroup Domain { get; set; }
	public TaxonomicGroup Kingdom { get; set; }
	public TaxonomicGroup Phylum { get; set; }
	public TaxonomicGroup Class { get; set; }
	public TaxonomicGroup Order { get; set; }
	public TaxonomicGroup Family { get; set; }
	public TaxonomicGroup Genus { get; set; }
	public TaxonomicGroup Species { get; set; }
	public TaxonomicGroup Subspecies { get; set; }
	public string ImageUrl { get; set; }
}

public class AnimalEntry
{
	[Key]
	public int Id { get; set; }
	public string CommonName { get; set; }
	public string BinomialName { get; set; }
	public string TrinomialName { get; set; }
	[NotMapped]
	public string ScientificName => TrinomialName == null ? BinomialName : TrinomialName;
	public string Domain { get; set; }
	public string Kingdom { get; set; }
	public string Phylum { get; set; }
	public string Class { get; set; }
	public string Order { get; set; }
	public string Family { get; set; }
	public string Genus { get; set; }
	public string Species { get; set; }
	public string Subspecies { get; set; }
	public ConservationStatus ConservationStatus { get; set; }
	public int TimesCaught { get; set; } = 0;
	public DateTime LastCaught { get; set; } = DateTime.MinValue;
	public string WikiIdentifier { get; set; }
	[NotMapped]
	public string WikiPage => Wikipedia.GetWikiPage(WikiIdentifier);
	[NotMapped]
	public string WikiInfoPage => Wikipedia.GetWikiInfoPage(WikiIdentifier);
	public int MonthlyViews { get; set; }
	public string ImageUrl { get; set; }

	public AnimalEntry(int id)
	{
		Id = id;
	}

	public static ConservationStatus ParseStatus(string name)
	{
		var cleanName = name.ToLower() // To be extra sure
			.Replace("_", "")
			.Replace(" ", "")
			.Replace(".", "")
			.Replace("-", "")
			.Trim();

		if (cleanName.Contains("evaluated")) return ConservationStatus.Not_Evaluated;
		if (cleanName.Contains("data")) return ConservationStatus.Data_Deficient;
		if (cleanName.Contains("concern")) return ConservationStatus.Least_Concern;
		if (cleanName.Contains("dependent")) return ConservationStatus.Conservation_Dependent;
		if (cleanName.Contains("threat")) return ConservationStatus.Near_Threatened;
		if (cleanName.Contains("vulnerable")) return ConservationStatus.Vulnerable;
		if (cleanName.Contains("critically")) return ConservationStatus.Critically_Endangered;
		if (cleanName.Contains("endangered")) return ConservationStatus.Endangered;
		if (cleanName.Contains("wild")) return ConservationStatus.Extinct_In_The_Wild;
		if (cleanName.Contains("extinct")) return ConservationStatus.Extinct;

		return ConservationStatus.Not_Evaluated;
	}


	/// <summary>
	/// Get an animal entry by id
	/// </summary>
	/// <param name="entryId"></param>
	/// <returns></returns>
	public static async Task<AnimalEntry> GetEntry(int entryId)
	{
		using (var db = new Wikipedia.AnimalsDatabase())
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
		using (var db = new Wikipedia.AnimalsDatabase())
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
		using (var db = new Wikipedia.AnimalsDatabase())
		{
			var foundAnimal = await db.Animals.FindAsync(entry.Id);

			if (foundAnimal == null)
				db.Animals.Add(entry);
			else
				db.Entry(foundAnimal).CurrentValues.SetValues(entry);

			await db.SaveChangesAsync();
		}
	}
}