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

namespace AssetParty;

public partial class AssetParty
{
	public static string ServicesUrl => "https://services.facepunch.com/sbox/package/";
	public static string AssetPartyUrl => "https://asset.party/";

	public static async Query QueryAsync(PackageType packageType = PackageType.All, QuerySort sortType = QuerySort.All)
	{

	}
}