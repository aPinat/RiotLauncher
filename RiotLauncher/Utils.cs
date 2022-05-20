using System.Diagnostics;
using System.Management;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RiotLauncher;

public static class Utils
{
    private static readonly Regex TokenRegex = new("--remoting-auth-token=(.+?)[\"'\\s]");
    private static readonly Regex PortRegex = new("--app-port=(.+?)[\"'\\s]");
    private static readonly Regex RiotClientTokenRegex = new("--riotclient-auth-token=(.+?)[\"'\\s]");
    private static readonly Regex RiotClientPortRegex = new("--riotclient-app-port=(.+?)[\"'\\s]");

    public static IEnumerable<Process> GetRiotClientUx() => Process.GetProcessesByName("RiotClientUx");

    public static IEnumerable<Process> GetLeagueClientUx() => Process.GetProcessesByName("LeagueClientUx");

    public static IEnumerable<Process> GetLeagueClient() => Process.GetProcessesByName("LeagueClient");

    public static RiotAuth? GetRiotAuth(Process process, bool isRiotClient = false)
    {
        var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
        var objects = searcher.Get();
        var commandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"] as string;
        if (commandLine == null)
        {
            var fileName = Assembly.GetEntryAssembly()?.Location;
            if (fileName != null)
            {
                var currentProcessInfo = new ProcessStartInfo { UseShellExecute = true, WorkingDirectory = Environment.CurrentDirectory, FileName = fileName, Verb = "runas" };

                Process.Start(currentProcessInfo);
            }

            Environment.Exit(0);
        }

        try
        {
            string port, token;
            if (isRiotClient)
            {
                port = RiotClientPortRegex.Match(commandLine).Groups[1].Value;
                token = RiotClientTokenRegex.Match(commandLine).Groups[1].Value;
            }
            else
            {
                port = PortRegex.Match(commandLine).Groups[1].Value;
                token = TokenRegex.Match(commandLine).Groups[1].Value;
            }

            return new RiotAuth(port, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }

    public static async Task<HttpResponseMessage> SendApiRequestAsync(RiotAuth riotAuth, HttpMethod method, string endpointUrl, object? body = null)
    {
        using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true });
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:" + riotAuth.Token));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

        endpointUrl = $"https://127.0.0.1:{riotAuth.Port}{endpointUrl}";
        Console.WriteLine($"{method} {endpointUrl}");

        if (method == HttpMethod.Post)
            return await http.PostAsJsonAsync(endpointUrl, body);

        if (method == HttpMethod.Put)
            return await http.PutAsJsonAsync(endpointUrl, body);

        using var requestMessage = new HttpRequestMessage(method, endpointUrl);
        if (body is null)
            return await http.SendAsync(requestMessage);

        Console.WriteLine(JsonSerializer.Serialize(body));
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await http.SendAsync(requestMessage);
    }

    public record RiotAuth(string Port, string Token);
}
