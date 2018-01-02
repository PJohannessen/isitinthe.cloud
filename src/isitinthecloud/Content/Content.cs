using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace isitinthecloud
{
    public static class Content
    {
        [FunctionName("Content")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string responsePage = File.ReadAllText(Path.Combine(GetScriptPath(), @"Content\index.html"));
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add("Strict-Transport-Security", "max-age=86400; includeSubDomains");

            string lookup = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "lookup", StringComparison.OrdinalIgnoreCase) == 0)
                .Value;

            if (!string.IsNullOrEmpty(lookup))
            {
                var azureMatch = false;
                uint uintAddress;

                try
                {
                    if (!IPAddress.TryParse(lookup, out var address))
                    {
                        string lookupValue = lookup;
                        if (Uri.TryCreate(lookup, UriKind.Absolute, out var uri))
                        {
                            lookupValue = uri.Host;
                        }
                        var hostEntry = Dns.GetHostEntry(lookupValue);
                        address = hostEntry.AddressList[0];
                    }

                    var ipAddress = address.MapToIPv4();
                    uintAddress = IpToInt(ipAddress.ToString());
                    string ipFilePath = Path.Combine(GetScriptPath(), @"Content\AzurePublicIPs.json");
                    var ranges = JsonConvert.DeserializeObject<List<IpRange>>(File.ReadAllText(ipFilePath));
                    var matchingRegion = ranges.FirstOrDefault(r => uintAddress >= r.Lower && uintAddress <= r.Upper);
                    if (matchingRegion != null) azureMatch = true;
                    if (azureMatch)
                    {
                        responsePage = responsePage.Replace(
                            "<div id=\"placeholder\"></div>",
                            $"<div id=\"match\">Yes, it looks like {WebUtility.HtmlEncode(lookup)} ({ipAddress}) is hosted on Azure!<br />Region: {matchingRegion.Region}<br />CIDR: {matchingRegion.Subnet}</div>");
                    }
                    else
                    {
                        responsePage = responsePage.Replace(
                            "<div id=\"placeholder\"></div>",
                            $"<div id=\"nomatch\">No, it looks like {WebUtility.HtmlEncode(lookup)} ({ipAddress}) is not hosted on Azure.</div>");
                    }
                }
                catch (Exception e)
                {
                    log.Info(e.Message);
                    responsePage = responsePage.Replace(
                        "<div id=\"placeholder\"></div>",
                        "<div id=\"error\">An error occured. Please ensure a valid URL, hostname or IP was provided.</div>");
                }
            }

            response.Content = new StringContent(responsePage, Encoding.UTF8, "text/html");
            return response;
        }

        private static string GetScriptPath()
            => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

        static uint IpToInt(string ip)
        {
            var address = IPAddress.Parse(ip);
            byte[] bytes = address.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            var intAddress = BitConverter.ToUInt32(bytes, 0);
            return intAddress;
        }

        public class IpRange
        {
            public string Region { get; set; }
            public string Subnet { get; set; }
            public uint Lower { get; set; }
            public uint Upper { get; set; }
        }
    }
}
