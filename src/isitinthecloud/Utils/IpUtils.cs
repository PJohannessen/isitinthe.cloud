using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;

namespace isitinthecloud
{
    public static class IpUtils
    {
        private static readonly int MaxIpV4AsDecimalLength = BigInteger.Pow(2, 32).ToString().Length;
        private static readonly int MaxIpV6AsDecimalLength = BigInteger.Pow(2, 128).ToString().Length;

        public static string IpToDecimal(string ip, out AddressFamily addressFamily)
        {
            bool ipIsValid = IPAddress.TryParse(ip, out var ipAddress);
            if (!ipIsValid) throw new ArgumentOutOfRangeException(nameof(ip));
            addressFamily = ipAddress.AddressFamily;
            byte[] bytes = ipAddress.GetAddressBytes();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            BigInteger bigInt = new BigInteger(bytes, true);
            return bigInt.ToString();
        }

        public static string IpToPaddedDecimal(string ip)
        {
            return IpToDecimal(ip, out var addressFamily).PadLeft(addressFamily == AddressFamily.InterNetwork ? MaxIpV4AsDecimalLength : MaxIpV6AsDecimalLength, '0');
        }

        public static async Task<(IPAddress ip, string host)> IpFromUnknownInput(string input)
        {
            if (IPAddress.TryParse(input, out var ip)) return (ip, null);

            var lookupValue = input;
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                lookupValue = uri.Host;
            }

            IPHostEntry hostEntry;
            try
            {
                hostEntry = await Dns.GetHostEntryAsync(lookupValue);
                ip = hostEntry.AddressList[0];
            }
            catch (Exception e)
            {
                throw new ArgumentException("Invalid input", nameof(input), e);
            }

            return (ip, hostEntry.HostName);
        }
    }
}
