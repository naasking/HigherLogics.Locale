using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using C = HigherLogics.Locale.Country;

namespace HigherLogics.Locale
{
    /// <summary>
    /// Local-specific information.
    /// </summary>
    public static partial class Locales
    {
        #region Useful lookups
        /// <summary>
        /// Look up a country's states.
        /// </summary>
        /// <param name="country">The country.</param>
        /// <returns>The list of states for the given country.</returns>
        public static IEnumerable<string> States(this Country country) =>
            provinces[country].Values;

        /// <summary>
        /// Lookup the official state's name.
        /// </summary>
        /// <param name="country">The country to which this state belongs.</param>
        /// <param name="stateName">The possibly unofficial state name.</param>
        /// <returns>The official state name.</returns>
        public static string State(this Country country, string stateName) =>
            provinces[country][stateName];

        /// <summary>
        /// Look up the country the official currency.
        /// </summary>
        /// <param name="currency">The currency.</param>
        /// <returns>The list of countries that use the given currency.</returns>
        public static IEnumerable<Country> Countries(this Currency currency) =>
            currency2Country[(int)currency];

        /// <summary>
        /// Lookup the official currencies used by a country.
        /// </summary>
        /// <param name="country">The country.</param>
        /// <returns>The currency for the given country, if available.</returns>
        public static Currency? Currency(this Country country) =>
            country2Currency.TryGetValue(country, out var c)
            ? c
            : new Currency?();
        #endregion

        #region PostalAddress parsing
        public static PostalAddress ParseAddress(string address)
        {
            // 1. split the address into lines, and then split each line into words
            // 2. check the words against the database of countries and states and return the set of all
            //    matches as a parse forest
            // 3. given each entry, try to extract the city and postal code
            // 4. score each 
            var words = NewLines(address.Split(nl, StringSplitOptions.RemoveEmptyEntries))
                          .SelectMany(x => x.Split(sp, StringSplitOptions.RemoveEmptyEntries))
                          .ToList();
            // country is optional, so also search by state first and return all countries with states
            // that match, and merge them all into a set of options
            var countries = ParseCountries(words);
            var shipTo = countries.SelectMany(x => ParseProvincesGivenCountry(x))
                                  .Concat(ParseProvinces(words))
                                  .ToList();
            var match = BestMatch(shipTo);
            var lines = match.AddressTo.Split(nl, StringSplitOptions.RemoveEmptyEntries);
            return new PostalAddress
            {
                StreetAddress = string.Join("\r\n", lines.Skip(1)),
                AddressTo = lines[0].Trim(),
                Country = match.Country.Value,
                State = match.State,
                Municipality = match.City.Trim(),
                PostalCode = match.PostalCode,
            };
        }

        //FIXME: I should improve scoring:
        // 1. Case sensitivity is important, ie. "OF" should be given a higher score than "of".
        //    Perhaps case insensitivity is a fallback if no matches, and everything should be
        //    case sensitive by default.
        // 2. Extend the postal code matching to other countries.
        //FIXME: this is a very suboptimal parser that performs a lot of allocation. I can
        //definitely improve this using a proper parser combinator framework.
        //FIXME: 

        static char[] nl = new[] { '\r', '\n' };
        static char[] sp = new[] { ' ', ',' };
        static string[] countryNames = Enum.GetNames(typeof(Country)).OrderBy(x => x).ToArray();

        static IEnumerable<string> NewLines(string[] lines)
        {
            for (int i = 0; i < lines.Length; ++i)
            {
                yield return lines[i];
                yield return "\r\n";
            }
        }

        class PartialAddress : IComparable<PartialAddress>
        {
            public string AddressTo;
            public string City;
            public string State;
            public Country? Country;
            public string PostalCode;
            public int LastIndex;
            public string CountryCode;
            public IEnumerable<string> remainder;

            public int CompareTo(PartialAddress other) =>
                Score().CompareTo(other.Score());

            public int Score()
            {
                return Score(AddressTo) + Score(City) + Score(State) + Score(Country)
                     + Score(CountryCode) + Score(PostalCode) + remainder.Count(x => x == "\r\n")
                     + (Country == C.CA && IsPostalCode(PostalCode.AsSpan()) ? 0 : 1)
                     + (Country == C.US && IsZipCode(PostalCode.AsSpan()) ? 0 : 1)
                     + (Country == C.CA || Country == C.US ? 0 : 1);
            }

            static int Score(Country? x) =>
                x == null ? 1 : 0;

            static int Score(string x) =>
                string.IsNullOrEmpty(x) ? 1 : 0;
        }

        static PartialAddress BestMatch(List<PartialAddress> shipTo)
        {
            if (shipTo.Count == 1)
                return shipTo[0];
            shipTo.Sort();
            if (shipTo.Count == 0 || shipTo[0].Score() == shipTo[1].Score())
                throw new Exception("No best pick!");
            return shipTo[0];
        }

        static bool TryCountry(string x, out Country c)
        {
            var i = Array.BinarySearch(countryNames, x, StringComparer.OrdinalIgnoreCase);
            if (i >= 0)
            {
                c = (Country)Enum.Parse(typeof(Country), countryNames[i]);
                return true;
            }
            else
            {
                c = default;
                return false;
            }
        }

        static IEnumerable<PartialAddress> ParseCountries(IEnumerable<string> words)
        {
            int i = 0;
            foreach (var x in words)
            {
                if (x != "\r\n" && TryCountry(x, out Country country))
                    yield return new PartialAddress
                    {
                        Country = country,
                        CountryCode = x,
                        LastIndex = i,
                        remainder = words,
                    };
                ++i;
            }
        }

        static IEnumerable<PartialAddress> ParseProvincesGivenCountry(PartialAddress shipTo)
        {
            if (shipTo.Country == null || !provinces.TryGetValue(shipTo.Country.Value, out var states))
                yield break;
            int i = 0;
            foreach (var x in shipTo.remainder)
            {
                if (x != "\r\n" && states.TryGetValue(x, out var prov)
                    && (!string.IsNullOrEmpty(shipTo.CountryCode) || !Enum.TryParse(x, out Country c) || c != shipTo.Country))
                    yield return FindCity(x, new PartialAddress
                    {
                        Country = shipTo.Country,
                        CountryCode = shipTo.CountryCode,
                        State = prov,
                        LastIndex = i,
                        remainder = shipTo.remainder,
                    });
                ++i;
            }
        }

        static IEnumerable<PartialAddress> ParseProvinces(IEnumerable<string> words)
        {
            int i = 0;
            foreach (var x in words)
            {
                if (x != "\r\n")
                {
                    foreach (var state in provinces)
                    {
                        if (state.Value.TryGetValue(x, out var prov))
                            yield return FindCity(x, new PartialAddress
                            {
                                Country = state.Key,
                                State = prov,
                                LastIndex = i,
                                remainder = words,
                            });
                    }
                }
                ++i;
            }
        }

        static PartialAddress FindCity(string provinceCode, PartialAddress shipTo)
        {
            int lastNewLine = 0;
            var postalCode = new Stack<string>();
            shipTo.City = FindCity(shipTo.Country, shipTo.remainder.GetEnumerator(), 0, shipTo.LastIndex, ref lastNewLine, postalCode);
            shipTo.PostalCode = string.Join(" ", postalCode);
            var skip = postalCode.Concat(shipTo.City?.Split(sp) ?? Enumerable.Empty<string>()).Concat(shipTo.State.Split(sp));
            var exclude = new HashSet<string>(skip)
            {
                provinceCode, shipTo.State, shipTo.CountryCode,
            };
            shipTo.remainder = shipTo.remainder.Where((x, i) => i < shipTo.LastIndex || !exclude.Contains(x));
            shipTo.AddressTo = shipTo.remainder.Aggregate(new StringBuilder(),
                (acc, x) => x != "\r\n" ? acc.Append(x).Append(' '):
                            acc[acc.Length - 1] == '\n' ? acc:
                                                          acc.AppendLine()).ToString();
            return shipTo;
        }

        static string FindCity(Country? country, IEnumerator<string> ie, int i, int end, ref int lastNewLine, Stack<string> postalCode)
        {
            if (!ie.MoveNext())
                return null;
            else if (end <= i)
            {
                var part = ie.Current;
                FindCity(country, ie, i + 1, end, ref lastNewLine, postalCode);
                if (i != end && IsPostalCode(country, part.AsSpan()))
                    postalCode.Push(part);
                return null;
            }
            else if (i < end)
            {
                var part = ie.Current;
                if (part == "\r\n")
                    lastNewLine = i;
                var rest = FindCity(country, ie, i + 1, end, ref lastNewLine, postalCode);
                return end <= i || i <= lastNewLine ? rest:
                       rest == null                 ? part:
                                                      part + ' ' + rest;
            }
            else
            {
                return FindCity(country, ie, i + 1, end, ref lastNewLine, postalCode);
            }
        }

        static bool IsPostalCode(Country? country, ReadOnlySpan<char> zip) =>
            country == C.CA && IsPostalCode(zip)
            || country == C.US && IsZipCode(zip);

        static bool IsZipCode(ReadOnlySpan<char> zip)
        {
            var i = zip.IndexOf('-');
            return i < 0 && int.TryParse(zip.ToString(), out var j)
                || i >= 0 && int.TryParse(zip.Slice(0, i).ToString(), out j) && int.TryParse(zip.Slice(i + 1, zip.Length - i - 1).ToString(), out j);
        }

        static bool IsPostalCode(ReadOnlySpan<char> postalCode) =>
            postalCode.Length == 3 && char.IsLetter(postalCode[0]) && char.IsDigit(postalCode[1]) && char.IsLetter(postalCode[2])
            || postalCode.Length == 3 && char.IsDigit(postalCode[0]) && char.IsLetter(postalCode[1]) && char.IsDigit(postalCode[2])
            || postalCode.Length == 6 && IsPostalCode(postalCode.Slice(0, 3)) && IsPostalCode(postalCode.Slice(4, 3));
        #endregion
    }
}