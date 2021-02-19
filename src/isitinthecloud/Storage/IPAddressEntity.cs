using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.WindowsAzure.Storage.Table;

namespace isitinthecloud
{
    public class IPAddressEntity : TableEntity
    {
        // ReSharper disable once InconsistentNaming
        private const string IPv4PartitionKey = "ipv4";
        // ReSharper disable once InconsistentNaming
        private const string IPv6PartitionKey = "ipv6";

        private const bool SupportIPv6 = false;

        public IPAddressEntity()
        {
        }

        public IPAddressEntity(string platform, string region, string service, string cidr)
        {
            var ip = IPNetwork.Parse(cidr);
            var lowerRange = IpUtils.IpToPaddedDecimal(ip.FirstUsable.ToString());
            var upperRange = IpUtils.IpToPaddedDecimal(ip.LastUsable.ToString());

            var partitionKey = ip.AddressFamily == AddressFamily.InterNetwork ? IPv4PartitionKey : IPv6PartitionKey;

            PartitionKey = partitionKey;
            RowKey = upperRange;
            LowerRange = lowerRange;
            Platform = platform;
            Region = region;
            Service = service;
            CIDR = cidr;
        }

        public string LowerRange { get; set; }
        public string Platform { get; set; }
        public string Service { get; set; }
        public string Region { get; set; }
        public string CIDR { get; set; }

        public static IEnumerable<ICollection<IPAddressEntity>> Chunk(ICollection<IPAddressEntity> entities)
        {
            const int batchSize = 100;
            var skip = 0;
            var batch = entities.Where(e => e.PartitionKey == IPv4PartitionKey).Skip(skip).Take(batchSize).ToList();
            while (batch.Any())
            {
                yield return batch;
                skip += batchSize;
                batch = entities.Where(e => e.PartitionKey == IPv4PartitionKey).Skip(skip).Take(batchSize).ToList();
            }

            if (SupportIPv6)
            {
                skip = 0;
                batch = entities.Where(e => e.PartitionKey == IPv6PartitionKey).Skip(skip).Take(batchSize).ToList();
                while (batch.Any())
                {
                    yield return batch;
                    skip += batchSize;
                    batch = entities.Where(e => e.PartitionKey == IPv6PartitionKey).Skip(skip).Take(batchSize).ToList();
                }
            }
        }
    }
}