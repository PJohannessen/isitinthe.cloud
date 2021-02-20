using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace isitinthecloud
{
    public class FetchAzureIpFile
    {
        private const string AzureIPRangesUri = "https://www.microsoft.com/en-us/download/confirmation.aspx?id=56519";
        private readonly HttpClient _httpClient;

        public FetchAzureIpFile(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [FunctionName("FetchAzureIpFile")]
        public async Task Run(
            [TimerTrigger("0 0 0 * * Mon")] TimerInfo myTimer,
            ILogger log,
            Binder binder)
        {
            var web = new HtmlWeb();
            var htmlDoc = await web.LoadFromWebAsync(AzureIPRangesUri);
            // ReSharper disable once StringLiteralTypo
            var downloadNode = htmlDoc.DocumentNode.SelectSingleNode("//a[@data-bi-id='downloadretry']");
            string downloadLink = downloadNode.Attributes["href"].Value;
            var response = await _httpClient.GetAsync(downloadLink);

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<AzureIpRangesSummary>(content);
            string blobPath = $"azure-ip-files/{data.ChangeNumber}.json";
            var blobAttribute = new BlobAttribute(blobPath, FileAccess.Write);
            var blob = await binder.BindAsync<CloudBlockBlob>(blobAttribute);
            var exists = await blob.ExistsAsync();
            if (exists)
            {
                log.LogInformation("Azure IP file {name} already exists, skipping save", blobPath);
            }
            else
            {
                log.LogInformation("New Azure IP file found, saving {name}", blobPath);
                await blob.UploadTextAsync(content);
            }
        }

        public class AzureIpRangesSummary
        {
            [JsonPropertyName("changeNumber")]
            public int ChangeNumber { get; set; }
        }
    }
}
