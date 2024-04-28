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

namespace Fishes;

public enum ConservationStatus
{
	Data_Deficient,
	Least_Concern,
	Near_Threatened,
	Vulnerable,
	Endangered,
	Critically_Endangered,
	Recently_Extinct,
	Extinct
}
public struct Taxonomy
{
	public string Domain { get; set; }
	public string Kingdom { get; set; }
	public string Phylum { get; set; }
	public string Class { get; set; }
	public string Order { get; set; }
	public string Family { get; set; }
	public string Genus { get; set; }
	public string Species { get; set; }
}

public class AnimalEntry
{

	private string Domain { get; set; }
	public string Kingdom { get; set; }
	public string Phylum { get; set; }
	public string Class { get; set; }
	public string Order { get; set; }
	public string Family { get; set; }
	public string Genus { get; set; }
	public string Species { get; set; }
	public ConservationStatus ConservationStatus { get; set; }
}