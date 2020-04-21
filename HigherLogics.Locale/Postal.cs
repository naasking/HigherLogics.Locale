using System;
using System.Linq;
using System.Collections.Generic;

namespace HigherLogics.Locale
{
    public static partial class Locales
    {
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
        /// <param name="stateName"></param>
        /// <returns></returns>
        public static string State(this Country country, string stateName) =>
            provinces[country][stateName];

        /// <summary>
        /// Look up the country the official currency.
        /// </summary>
        /// <param name="currency"></param>
        /// <returns></returns>
        public static IEnumerable<Country> Countries(this Currency currency) =>
            currency2Country[(int)currency];

        /// <summary>
        /// Lookup the official currencies used by a country.
        /// </summary>
        /// <param name="country"></param>
        /// <returns></returns>
        public static Currency? Currency(this Country country) =>
            country2Currency.TryGetValue(country, out var c)
            ? c
            : new Currency?();
    }
}