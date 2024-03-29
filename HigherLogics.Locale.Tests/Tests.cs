﻿using System;
using Xunit;
using HigherLogics.Locale;

namespace HigherLogics.Locale
{
    public class Tests
    {
        [Theory]
        [InlineData(Country.CA, "Quebec", "Boucherville", @"Trimen Food Service Equipment Inc.
Autorente 20 and Tourane St
Boucherville, QC")]
        [InlineData(Country.CA, "Quebec", "Boucherville", @"Trimen Food Service Equipment Inc.
Autorente 20 and Tourane St
Boucherville QC")]
        [InlineData(Country.CA, "Ontario", "Mississauga", @"Carmel Heights Senior Residence
1720 Sherwood Forest Circle
Mississauga ON Canada

Sister Veronical or Sister Barbara 905-822-5298")]
        [InlineData(Country.CA, "Ontario", "Mississauga", @"Carmel Heights Senior Residence
1720 Sherwood Forest Circle
Mississauga, ON, Canada

Sister Veronical or Sister Barbara 905-822-5298")]
        static void NoPostalCodes(Country country, string state, string city, string addr)
        {
            var post = Locales.ParseAddress(addr);
            Assert.True(Enum.IsDefined(typeof(Country), post.Country));
            Assert.NotEmpty(post.State);
            Assert.NotEmpty(post.Municipality);
            Assert.NotEmpty(post.StreetAddress);
            Assert.NotEmpty(post.AddressTo);
            Assert.Empty(post.PostalCode);

            Assert.Equal(country, post.Country);
            Assert.Equal(state, post.State);
            Assert.Equal(city, post.Municipality);
        }

        [Theory]
        [InlineData(Country.CA, "Ontario", "Burlington", "L7N3P1", @"Russell Hendrix Foodservice Equipment
c/oAssumption CSS
3230 Woodware Avenue
Burlington, ON, CA
L7N3P1")]
        [InlineData(Country.CA, "Ontario", "Cavon", "L0A 1C0", @"MCC Fire Equipment
961 Meadow Lane
Cavon, ON L0A 1C0

Attn: Mike Corriveau")]
        [InlineData(Country.CA, "Ontario", "Cavon", "L0A 1C0", @"MCC Fire Equipment
961 Meadow Lane
Cavon ON L0A 1C0

Attn: Mike Corriveau")]
        [InlineData(Country.CA, "Ontario", "North York", "M9L 1P9", @"Classic Fire Protection
645 Garyray Drive
North York, ON M9L 1P9

Attn: Angelo Giannone 416-740-3000")]
        [InlineData(Country.CA, "Ontario", "North York", "M9L 1P9", @"Classic Fire Protection
645 Garyray Drive
North York ON M9L 1P9

Attn: Angelo Giannone 416-740-3000")]
        [InlineData(Country.US, "California", "Orange", "92868-1015", @"SimplexGrinnell LP
1701 W. Sequoia Ave.
Orange, CA 92868-1015
USA

Attn: Bob Finnerty 714-712-3629")]
        [InlineData(Country.US, "California", "Orange", "92868-1015", @"SimplexGrinnell LP
1701 W. Sequoia Ave.
Orange CA 92868-1015
USA

Attn: Bob Finnerty 714-712-3629")]
        [InlineData(Country.CA, "Nova Scotia", "Bridgewater", "B4V 2T6", @"Airsys Eng. Ltd. (A.S.E)
Commercial Heating
C/O Empire Theateers
349 Lahaug St.
Bridgewater, NS, CA
B4V 2T6
Contact: Ken Malone
Tel: 902-476-5187")]
        [InlineData(Country.CA, "Nova Scotia", "Bridgewater", "B4V 2T6", @"Airsys Eng. Ltd. (A.S.E)
Commercial Heating
C/O Empire Theateers
349 Lahaug St.
Bridgewater NS CA
B4V 2T6
Contact: Ken Malone
Tel: 902-476-5187")]
        [InlineData(Country.US, "Michigan", "Redford", "48240-1480", @"Fire Systems of Michigan, Inc
26109 Grand River Avenue
Redford, MI 48240-1480

Attn: Dave Nagy 313-255-0053")]
        [InlineData(Country.US, "Michigan", "Redford", "48240-1480", @"Fire Systems of Michigan, Inc
26109 Grand River Avenue
Redford MI 48240-1480

Attn: Dave Nagy 313-255-0053")]
        [InlineData(Country.US, "Washington", "Bellvue", "98004", @"Tokyo Japanese Steakhouse
Suite 108
909 112th Avenue NE
Bellvue, WA, US
98004")]
        static void FullPostalAddresses(Country country, string state, string city, string postalCode, string addr)
        {
            var post = Locales.ParseAddress(addr);
            Assert.True(Enum.IsDefined(typeof(Country), post.Country));
            Assert.NotEmpty(post.State);
            Assert.NotEmpty(post.PostalCode);
            Assert.NotEmpty(post.Municipality);
            Assert.NotEmpty(post.StreetAddress);
            Assert.NotEmpty(post.AddressTo);

            Assert.Equal(country, post.Country);
            Assert.Equal(state, post.State);
            Assert.Equal(city, post.Municipality);
            Assert.Equal(postalCode, post.PostalCode);
        }

        [Theory]
        [InlineData(@"MCC Fire Equipment
961 Meadow Lane
Cavon")]
        [InlineData(@"MCC Fire Equipment
961 Meadow Lane
Cavon, M4M 4M4")]
        static void AddressFails(string addr)
        {
            Assert.ThrowsAny<Exception>(() => Locales.ParseAddress(addr));
        }
    }
}
