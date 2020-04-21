using System;
using Xunit;
using HigherLogics.Locale;

namespace HigherLogics.Locale
{
    public class Tests
    {
        [Theory]
        [InlineData(@"MCC Fire Equipment
961 Meadow Lane
Cavon, ON L0A 1C0

Attn: Mike Corriveau")]
        [InlineData(@"Trimen Food Service Equipment Inc.
Autorente 20 and Tourane St
Boucherville, QC")]
        [InlineData(@"Carmel Heights Senior Residence
1720 Sherwood Forest Circle
Mississauga, ON, Canada

Sister Veronical or Sister Barbara 905-822-5298")]
        static void NoPostalCodes(string addr)
        {
            var post = Locales.ParseAddress(addr);
            Assert.True(Enum.IsDefined(typeof(Country), post.Country));
            Assert.NotEmpty(post.State);
            Assert.NotEmpty(post.Municipality);
            Assert.NotEmpty(post.StreetAddress);
        }

        [Theory]
        [InlineData(@"CLassic Fire Protection
645 Garyray Drive
North York, ON M9L 1P9

Attn: Angelo Giannone 416-740-3000")]
        [InlineData(@"SimplexGrinnell LP
1701 W. Sequoia Ave.
Orange, CA 92868-1015
USA

Attn: Bob Finnerty 714-712-3629")]
        [InlineData(@"Airsys Eng. Ltd. (A.S.E)
Commercial Heating
C/O Empire Theateers
349 Lahaug St.
Bridgewater, NS, CA
B4V 2T6
Contact: Ken Malone
Tel: 902-476-5187")]
        [InlineData(@"Fire Systems of Michigan, Inc
26109 Grand River Avenue
Redford, MI 48240-1480

Attn: Dave Nagy 313-255-0053")]
        static void FullPostalAddresses(string addr)
        {
            var post = Locales.ParseAddress(addr);
            Assert.True(Enum.IsDefined(typeof(Country), post.Country));
            Assert.NotEmpty(post.State);
            Assert.NotEmpty(post.PostalCode);
            Assert.NotEmpty(post.Municipality);
            Assert.NotEmpty(post.StreetAddress);
        }
    }
}
