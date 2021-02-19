using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace isitinthecloud
{
    public class FetchAWSIpFile
    {
        private const string AWSIpRangesUri = "https://ip-ranges.amazonaws.com/ip-ranges.json";
        private readonly HttpClient _httpClient;

        public FetchAWSIpFile(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [FunctionName("FetchAWSIpFile")]
        public async Task Run(
            [TimerTrigger("0 0 * * * *")] TimerInfo myTimer,
            ILogger log,
            Binder binder)
        {
            var response = await _httpClient.GetAsync(AWSIpRangesUri);
            var data = await response.Content.ReadAsAsync<AwsIpRangesSummary>();
            string blobPath = $"aws-ip-files/{data.SyncToken}.json";
            var blobAttribute = new BlobAttribute(blobPath, FileAccess.Write);
            var blob = await binder.BindAsync<CloudBlockBlob>(blobAttribute);
            var exists = await blob.ExistsAsync();
            if (exists)
            {
                log.LogInformation("AWS IP file {name} already exists, skipping save", blobPath);
            }
            else
            {
                log.LogInformation("New AWS IP file found, saving {name}", blobPath);
                await blob.UploadTextAsync(await response.Content.ReadAsStringAsync());
            }
        }

        public class AwsIpRangesSummary
        {
            public int SyncToken { get; set; }
        }
    }
}
