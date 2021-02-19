using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace isitinthecloud
{
    public static class ImportAWSIpFile
    {
        [FunctionName("ImportAWSIpFile")]
        public static async Task Run(
            [BlobTrigger("aws-ip-files/{name}.json")]Stream awsIpFile,
            ILogger log,
            [Table("IpAddresses")] CloudTable table)
        {
            try
            {
                var ipData = await JsonSerializer.DeserializeAsync<AwsIpFile>(awsIpFile);
                var entities = ipData.Prefixes
                    .Concat<AwsIpPrefix>(ipData.Ipv6Prefixes)
                    .GroupBy(prefix => prefix.IpPrefix)
                    .Select(prefix =>
                    {
                        var distinctRecord = prefix.First();
                        return new IPAddressEntity("AWS", distinctRecord.Region, distinctRecord.Service, distinctRecord.IpPrefix);
                    })
                    .ToList();

                foreach (var batch in IPAddressEntity.Chunk(entities))
                {
                    var batchOperation = new TableBatchOperation();
                    foreach (var operation in batch)
                    {
                        batchOperation.InsertOrReplace(operation);
                    }

                    await table.ExecuteBatchAsync(batchOperation);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "Failed to import IP ranges");
                throw;
            }
        }

        public class AwsIpFile
        {
            [JsonPropertyName("syncToken")]
            public string SyncToken { get; set; }
            [JsonPropertyName("createDate")]
            public string CreateDate { get; set; }
            [JsonPropertyName("prefixes")]
            public AwsIpV4Prefix[] Prefixes { get; set; }
            [JsonPropertyName("ipv6_prefixes")]
            public AwsIpV6Prefix[] Ipv6Prefixes { get; set; }
        }

        public class AwsIpV4Prefix : AwsIpPrefix
        {
            [JsonPropertyName("ip_prefix")]
            public override string IpPrefix { get; set; }
        }

        public class AwsIpV6Prefix : AwsIpPrefix
        {
            [JsonPropertyName("ipv6_prefix")]
            public override string IpPrefix { get; set; }
        }

        public abstract class AwsIpPrefix
        {
            public abstract string IpPrefix { get; set; }
            [JsonPropertyName("region")]
            public string Region { get; set; }
            [JsonPropertyName("network_border_group")]
            public string NetworkBorderGroup { get; set; }
            [JsonPropertyName("service")]
            public string Service { get; set; }
        }
    }
}
