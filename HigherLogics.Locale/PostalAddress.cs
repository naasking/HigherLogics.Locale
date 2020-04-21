using System;
using System.Collections.Generic;
using System.Text;

namespace HigherLogics.Locale
{
    /// <summary>
    /// A type representing a mailing/postal address.
    /// </summary>
    public class PostalAddress
    {
        /// <summary>
        /// The person to whom the parcel is addressed.
        /// </summary>
        public string AddressTo { get; set; }

        /// <summary>
        /// The street address.
        /// </summary>
        public string StreetAddress { get; set; }

        /// <summary>
        /// The regional municipality.
        /// </summary>
        public string Municipality { get; set; }

        /// <summary>
        /// The state/province/territory.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The country.
        /// </summary>
        public Country Country { get; set; }

        /// <summary>
        /// The postal/zip code.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override string ToString() =>
            $@"{AddressTo}
{StreetAddress}
{Municipality}, {State}, {Country}
{PostalCode}";
    }
}
