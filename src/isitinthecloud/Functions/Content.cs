using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace isitinthecloud
{
    public  class Content
    {
        private const string Index =
            @"<!doctype html><html lang=""en""><head> <meta charset=""utf-8""> <meta name=""viewport"" content=""width=device-width, initial-scale=1""> <title>Is It in the Cloud?</title> <style>body{display: flex; justify-content: center;}footer{position: absolute; bottom: 0;}input{width: 200px;}</style></head><body> <div> <h1>Is It in the Cloud?</h1> <div id=""placeholder""></div><br/> <form action="""" method=""GET""> <input type=""text"" id=""lookup"" name=""lookup"" placeholder=""Enter a URL, hostname or IP.""/> </form> <footer> <p>See <a href=""https://lonesomecrowdedweb.com/blog/site-on-azure-functions/"">here</a> for more details.</p><p><em>Azure is a trademark of Microsoft Corporation.<br/>This site is not affiliated with Microsoft.</em></p></footer> </div></body></html>";

        [FunctionName("Content")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log,
            [Table("IpAddresses")] CloudTable table)
        {
            string responsePage = Index;
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            string lookup = req.Query["lookup"];

            if (!string.IsNullOrWhiteSpace(lookup))
            {
                try
                {
                    var isItInTheCloud = await IsItInTheCloud(lookup, table);
                    if (isItInTheCloud.Match != null)
                    {
                        if (!string.IsNullOrWhiteSpace(isItInTheCloud.HostName))
                        {
                            responsePage = responsePage.Replace(
                                "<div id=\"placeholder\"></div>",
                                $"<div id=\"match\">Yes, it looks like {WebUtility.HtmlEncode(isItInTheCloud.HostName)} ({isItInTheCloud.Ip}) is hosted on {isItInTheCloud.Match.Platform}!<br />Region: {isItInTheCloud.Match.Region}<br />CIDR: {isItInTheCloud.Match.CIDR}<br />Service: {isItInTheCloud.Match.Service}</div>");
                        }
                        else
                        {
                            responsePage = responsePage.Replace(
                                "<div id=\"placeholder\"></div>",
                                $"<div id=\"match\">Yes, it looks like {WebUtility.HtmlEncode(isItInTheCloud.Ip)} is hosted on {isItInTheCloud.Match.Platform}!<br />Region: {isItInTheCloud.Match.Region}<br />CIDR: {isItInTheCloud.Match.CIDR}<br />Service: {isItInTheCloud.Match.Service}</div>");
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(isItInTheCloud.HostName))
                        {
                            responsePage = responsePage.Replace(
                                "<div id=\"placeholder\"></div>",
                                $"<div id=\"nomatch\">No, it looks like {WebUtility.HtmlEncode(isItInTheCloud.HostName)} ({isItInTheCloud.Ip}) is not hosted in the cloud.</div>");
                        }
                        else
                        {
                            responsePage = responsePage.Replace(
                                "<div id=\"placeholder\"></div>",
                                $"<div id=\"nomatch\">No, it looks like {WebUtility.HtmlEncode(isItInTheCloud.HostName)} is not hosted in the cloud.</div>");
                        }
                    }
                }
                catch (Exception)
                {
                    responsePage = responsePage.Replace(
                        "<div id=\"placeholder\"></div>",
                        "<div id=\"error\">An error occurred. Please ensure a valid URL, hostname or IP was provided.</div>");
                }
            }

            response.Content = new StringContent(responsePage, Encoding.UTF8, "text/html");
            return response;
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
    }
}
