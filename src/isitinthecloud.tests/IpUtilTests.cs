using System.Net.Sockets;
using Xunit;

namespace isitinthecloud.tests
{
    public class IpUtilTests
    {
        [Theory]
        [InlineData("8.8.8.8", "134744072", AddressFamily.InterNetwork)]
        [InlineData("127.0.0.1", "2130706433", AddressFamily.InterNetwork)]
        [InlineData("185.143.16.1", "3113160705", AddressFamily.InterNetwork)]
        [InlineData("2001:4860:4860::8888", "42541956123769884636017138956568135816", AddressFamily.InterNetworkV6)]
        public void IpToDecimal(string ip, string expectedDecimalIp, AddressFamily expectedAddressFamily)
        {
            var result = IpUtils.IpToDecimal(ip, out var addressFamily);
            Assert.Equal(result, expectedDecimalIp);
            Assert.Equal(addressFamily, expectedAddressFamily);
        }

        [Theory]
        [InlineData("8.8.8.8", "0134744072")]
        [InlineData("127.0.0.1", "2130706433")]
        [InlineData("185.143.16.1", "3113160705")]
        [InlineData("2001:4860:4860::8888", "042541956123769884636017138956568135816")]
        public void IpToPaddedDecimal(string ip, string expectedDecimalIp)
        {
            var result = IpUtils.IpToPaddedDecimal(ip);
            Assert.Equal(result, expectedDecimalIp);
        }
    }
}
