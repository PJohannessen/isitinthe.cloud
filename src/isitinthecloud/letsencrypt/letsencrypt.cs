using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

// ReSharper disable once CheckNamespace
namespace isitinthecloud
{
    // ReSharper disable once InconsistentNaming
    [Disable]
    public static class letsencrypt
    {
        [FunctionName("letsencrypt")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, string challengeId, TraceWriter log)
        {
            var responsePage = File.ReadAllText(Path.Combine(GetScriptPath(), @".well-known\acme-challenge\" + challengeId));
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(responsePage, Encoding.UTF8, "text/html");
            return response;
        }

        private static string GetScriptPath()
            => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        private static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
