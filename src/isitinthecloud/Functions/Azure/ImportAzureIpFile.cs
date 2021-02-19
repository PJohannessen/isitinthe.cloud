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
    public static class ImportAzureIpFile
    {
        [FunctionName("ImportAzureIpFile")]
        public static async Task Run(
            [BlobTrigger("azure-ip-files/{name}.json")]Stream awsIpFile,
            ILogger log,
            [Table("IpAddresses")] CloudTable table)
        {
            try
            {
                var ipData = await JsonSerializer.DeserializeAsync<AzureIpFile>(awsIpFile);
                var entities = ipData.Values.SelectMany(value =>
                {
                    return value.Properties.AddressPrefixes.Select(prefix => new IPAddressEntity("Azure", value.Properties.Region, value.Properties.SystemService, prefix));

                }).ToList();

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

        public class AzureIpFile
        {
            [JsonPropertyName("changeNumber")]
            public int ChangeNumber { get; set; }
            [JsonPropertyName("cloud")]
            public string Cloud { get; set; }
            [JsonPropertyName("values")]
            public AzureIpValue[] Values { get; set; }
        }

        public class AzureIpValue
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("properties")]
            public AzureIpProperty Properties { get; set; }
        }

        public class AzureIpProperty
        {
            [JsonPropertyName("region")]
            public string Region { get; set; }
            [JsonPropertyName("systemService")]
            public string SystemService { get; set; }
            [JsonPropertyName("addressPrefixes")]
            public string[] AddressPrefixes { get; set; }
        }
    }
}
