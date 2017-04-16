using System.Net;
using System.Text;

public static HttpResponseMessage Run(HttpRequestMessage req, string challengeId, TraceWriter log)
{
    string responsePage = File.ReadAllText(Path.Combine(GetScriptPath(), @".well-known\acme-challenge\" + challengeId));
    var response = req.CreateResponse(HttpStatusCode.OK);
    response.Content = new StringContent(responsePage, Encoding.UTF8, "text/html");
    return response;
}

private static string GetScriptPath()
    => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

private static string GetEnvironmentVariable(string name)
    => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);