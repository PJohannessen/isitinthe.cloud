using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace isitinthecloud
{
    public class Lookup
    {
        [FunctionName("Lookup")]
        public static async Task<ObjectResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            [Table("IpAddresses")] CloudTable table)
        {
            string lookup = req.Query["lookup"];
            LookupResult result = new LookupResult();

            if (!string.IsNullOrWhiteSpace(lookup))
            {
                try
                {
                    var isItInTheCloud = await IsItInTheCloud(lookup, table);
                    result.Match = isItInTheCloud.Match != null;
                    result.HostName = WebUtility.HtmlEncode(isItInTheCloud.HostName);
                    result.Ip = WebUtility.HtmlEncode(isItInTheCloud.Ip);
                    if (isItInTheCloud.Match != null)
                    {
                        result.Platform = isItInTheCloud.Match.Platform ?? "Unknown";
                        result.Region = isItInTheCloud.Match.Region ?? "Unknown";
                        result.CIDR = isItInTheCloud.Match.CIDR ?? "Unknown";
                        result.Service = isItInTheCloud.Match.Service ?? "Unknown";
                    }

                    result.Success = true;
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }

            return new OkObjectResult(result);
        }


        private static async Task<IsItInTheCloudResult> IsItInTheCloud(string lookup, CloudTable table)
        {
            var (ip, host) = await IpUtils.IpFromUnknownInput(lookup);
            string partitionKey = ip.AddressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6";
            var paddedIp = IpUtils.IpToPaddedDecimal(ip.ToString());
            var pkFilter = TableQuery.GenerateFilterCondition(nameof(IPAddressEntity.PartitionKey), QueryComparisons.Equal, partitionKey);
            var rkFilter = TableQuery.GenerateFilterCondition(nameof(IPAddressEntity.RowKey), QueryComparisons.GreaterThanOrEqual, paddedIp);
            var combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, rkFilter);

            var query = new TableQuery<IPAddressEntity>
            {
                TakeCount = 1,
                FilterString = combinedFilter
            };

            var querySegment = await table.ExecuteQuerySegmentedAsync(query, null);
            var entity = querySegment.SingleOrDefault();
            return new IsItInTheCloudResult
            {
                HostName = host,
                Ip = ip.ToString(),
                Match = (entity == null || string.Compare(entity.LowerRange, paddedIp, StringComparison.Ordinal) > 0)
                    ? null
                    : entity
            };
        }

        public class IsItInTheCloudResult
        {
            public string HostName { get; set; }
            public string Ip { get; set; }
            public IPAddressEntity Match { get; set; }
        }

        public class LookupResult
        {
            public bool Success { get; set; }
            public bool Match { get; set; }
            public string Ip { get; set; }
            public string HostName { get; set; }
            public string Platform { get; set; }
            public string Service { get; set; }
            public string Region { get; set; }
            public string CIDR { get; set; }
        }
    }
}
