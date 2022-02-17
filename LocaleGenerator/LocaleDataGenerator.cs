using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;

namespace HigherLogics.Locale
{
	[Generator]
	public class LocaleDataGenerator : ISourceGenerator
	{
		//FIXME: need to find some way to cache the results so I don't fetch on every build? Initial idea is
		//to store file locally as "[etag].[file].csv", search for "[file].csv" and take the last one saved
		//then perform a fetch with If-Modified-Since set. However, source generators don't seem 

		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
            var source = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace HigherLogics.Locale
{{
    /// <summary>
	/// The set of continents.
	/// </summary>
    public enum Continents : byte
    {{
{ElaborateContinents()}
    }}
    /// <summary>
	/// The set of countries.
	/// </summary>
	public enum Country : byte
    {{
{ElaborateCountries()}
    }}

    /// <summary>
	/// The set of currencies.
	/// </summary>
	public enum Currency : byte
    {{
{ElaborateCurrencies(out var cc)}
    }}

	public static partial class Locales
    {{
  	    /// <summary>The map of currencies to the set of countries that use them.</summary>
        static readonly Country[][] currency2Country = new[]
        {{
{ElaborateCurrency2Country(cc)}
        }};

	    /// <summary>The map of countries to official currencies.</summary>
        static readonly Dictionary<Country, Currency> country2Currency = new Dictionary<Country, Currency>
		{{
{ElaborateCountry2Currency(cc)}
		}};

	    /// <summary>The map of territories/states/provinces</summary>
	    static readonly Dictionary<Country, Dictionary<string, string>> provinces = new Dictionary<Country, Dictionary<string, string>>
		{{
{ElaborateTerritories()}
		}};
	}}
}}";
			var locales = context.Compilation.GetTypeByMetadataName("HigherLogics.Locale.Locales");
			context.AddSource($"{locales.Name}.g.cs", source);
		}

		string EnumName(string name)
		{
			return name.Replace(' ', '_').Replace('-', '_').Replace(".", "").Replace(",_", "_");
		}

		static IEnumerable<string> Load(Assembly asm, string resourceName)
		{
			var buf = new StringBuilder();
			using (var stream = new StreamReader(asm.GetManifestResourceStream(resourceName)))
			{
				do
				{
					var x = stream.ReadLine();
					if (x == null)
						break;
					yield return x;
				} while (true);
			}
		}

		StringBuilder ElaborateContinents()
		{
			var buf = new StringBuilder();
			var continents = Continents().Reverse();
			foreach (var x in continents)
			{
				if (x.Key != x.Value)
				{
					buf.Append("            ").Append(x.Key).Append(" = ").Append(EnumName(x.Value)).AppendLine(",");
				}
				else
				{
					if (x.Value != EnumName(x.Value))
					{
						buf.Append("            ").Append("[Display(Name = \"").Append(x.Value).AppendLine("\")]");
						//buf.Append($@"[Name(""{x.Value}"")]");
					}
					buf.Append("            ").Append(EnumName(x.Value)).AppendLine(",");
				}
			}
			return buf;
		}

		StringBuilder ElaborateCountries()
		{
			var buf = new StringBuilder();
			var abbrev = Countries(out var countries);
			foreach (var x in countries)
			{
				if (x != EnumName(x))
				{
					buf.Append("            ").Append("[Display(Name = \"").Append(x).AppendLine("\")]");
				}
				buf.Append("            ").Append(EnumName(x)).AppendLine(",");
			}
			foreach (var x in abbrev)
			{
				buf.Append("            ").Append(x.Key).Append(" = ").Append(EnumName(x.Value)).AppendLine(",");
			}
			return buf;
		}

		StringBuilder ElaborateCurrencies(out SortedDictionary<string, List<string>> currencyMap)
		{
			var buf = new StringBuilder();
			currencyMap = Currencies(out var currencies);
			foreach (var x in currencies)
			{
				buf.Append("            ").Append(x).AppendLine(",");
			}
			return buf;
		}

		StringBuilder ElaborateCurrency2Country(SortedDictionary<string, List<string>> cc)
		{
			var buf = new StringBuilder();
			foreach (var x in cc)
			{
				if (x.Key.Length == 3)
				{
					buf.Append("        new[] //Currency.").AppendLine(x.Key)
					   .AppendLine("        {");
					foreach (var y in x.Value)
					{
						buf.Append("            Country.").Append(y).AppendLine(",");
					}
					buf.AppendLine("        },");
				}
			}
			return buf;
		}

		StringBuilder ElaborateCountry2Currency(SortedDictionary<string, List<string>> cc)
		{
			var buf = new StringBuilder();
			foreach (var x in cc)
			{
				if (x.Key.Length == 2)
				{
					buf.AppendLine("{ Country.").Append(x.Key).Append(", Locale.Currency.").Append(x.Value.Single()).AppendLine(" },");
				}
			}
			return buf;
		}

		StringBuilder ElaborateTerritories()
		{
			var buf = new StringBuilder();
			foreach (var country in Provinces())
			{
				buf.Append("        { Country.").Append(EnumName(country.Key)).AppendLine(", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)")
				   .AppendLine("            {");
				foreach (var province in country.Value)
				{
					buf.Append("                { \"").Append(province.Key).Append("\", \"").Append(province.Value).AppendLine("\" },");
				}
				buf.AppendLine("            }");
				buf.AppendLine("        },");
			}
			return buf;
		}

		string Fetch(string url)
		{
			//FIXME: these URLs return etags, so include the etag in the file name somehow? Or maybe as the last line in the file?
			//Or maybe in the generated source code as some kind of attribute?

			using (var web = new WebClient())
			{
				return web.DownloadString(new Uri(url));
			}
		}

		string Load(string file, string url)
		{
			//FIXME: use compilation target path
			var root = ""; // this.Host.ResolvePath(".");
			//// load all files with 'file' wildcard
			//var files = Directory.EnumerateFiles(root, "*" + file);
			//foreach(var x in files)
			var path = Path.Combine(root, file);
			if (File.Exists(path))
				return File.ReadAllText(path);
			var data = Fetch(url);
			//File.WriteAllText(path, data);
			return data;
		}

		string[] Split(string x) =>
			x.Split(new[] { '\r', '\n', ':', '"', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);

		Dictionary<string, string> Countries(out List<string> names)
		{
			var countries = Split(Load("countries.json", "http://country.io/names.json"));
			var iso = Split(Load("iso2_3.json", "http://country.io/iso3.json"));
			var map = new Dictionary<string, string>();
			names = new List<string>();
			// map ISO2 codes and case insensitive country name to normalized country name
			for (int i = 0; i < countries.Length; i += 4)
			{
				names.Add(countries[i + 2].Trim());
				map[countries[i]] = countries[i + 2].Trim();
			}
			// map ISO3 codes to normalized country name
			for (int i = 0; i < iso.Length; i += 4)
			{
				if (map.TryGetValue(iso[i], out var x))
					map[iso[i + 2]] = x;
			}
			names.Sort();
			return map;
		}

		Dictionary<string, Dictionary<string, string>> Provinces()
		{
			//from: https://www.cbp.gov/document/guidance/international-stateprovince-codes
			//var provinces = File.ReadAllLines(this.Host.ResolvePath("provinces.csv"))
			//FIXME: this is manually converted to csv at the moment, automate using XLSX nuget package.

			//FIXME: use compilation target path
			var provinces = Load(typeof(LocaleDataGenerator).Assembly, "LocaleGenerator.provinces.csv")
								.Select(x => x.Split(new[] { ',' }, StringSplitOptions.None));
			var countries = Countries(out var names);
			var states = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var province in provinces)
			{
				var name = countries[province[0]];
				if (!states.TryGetValue(name, out var country))
					country = states[name] = new Dictionary<string, string>();
				if (country.ContainsKey(province[1]))
					throw new Exception($"{province[0]} has a duplicate province {province[1]}, {province[2]}.");
				country.Add(province[1], province[2]);
				if (!country.ContainsKey(province[2]))
					country.Add(province[2], province[2]);
			}
			return states;
		}

		SortedDictionary<string, List<string>> Currencies(out SortedSet<string> currencies)
		{
			var countries = Split(Load("currencies.json", "http://country.io/currency.json"));
			currencies = new SortedSet<string>();
			var list = new SortedDictionary<string, List<string>>();
			for (int i = 0; i < countries.Length - 2; i += 4)
			{
				var c = countries[i + 2].Trim();
				if (string.IsNullOrEmpty(c) || c == ",")
					continue;
				currencies.Add(c);
				if (!list.TryGetValue(c, out var d2c))
					list[c] = d2c = new List<string>();
				d2c.Add(countries[i].Trim());
				if (!list.TryGetValue(countries[i], out var c2d))
					list[countries[i]] = c2d = new List<string>();
				c2d.Add(c);
			}
			return list;
		}

		SortedDictionary<string, string> Continents()
		{
			var continents = Load("continents.csv", "https://datahub.io/core/continent-codes/r/continent-codes.csv")
				.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var map = new SortedDictionary<string, string>();
			for (int i = 0; i < continents.Length; ++i)
			{
				var line = continents[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				map.Add(line[0], line[1]);
				map.Add(line[1], line[1]);
			}
			return map;
		}
	}
}
