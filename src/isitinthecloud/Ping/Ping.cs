using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace isitinthecloud.Ping
{
    public static class Ping
    {
        [FunctionName("Ping")]
        public static void Run([TimerTrigger("0 */4 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
