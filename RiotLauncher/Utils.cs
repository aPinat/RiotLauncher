using System.Diagnostics;
using System.Management;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace RiotLauncher;

public static class Utils
{
    private static Regex TokenRegex { get; } = new("--remoting-auth-token=(.+?)[\"'\\s]");
    private static Regex PortRegex { get; } = new("--app-port=(.+?)[\"'\\s]");
    private static Regex RiotClientTokenRegex { get; } = new("--riotclient-auth-token=(.+?)[\"'\\s]");
    private static Regex RiotClientPortRegex { get; } = new("--riotclient-app-port=(.+?)[\"'\\s]");

    public static IEnumerable<Process> GetRiotClientUx() => Process.GetProcessesByName("RiotClientUx");

    public static IEnumerable<Process> GetLeagueClientUx() => Process.GetProcessesByName("LeagueClientUx");

    public static IEnumerable<Process> GetLeagueClient() => Process.GetProcessesByName("LeagueClient");

    public record RiotAuth(string Port, string Token);

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
                var currentProcessInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = fileName,
                    Verb = "runas"
                };

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
    
    public static async Task<string> SendApiRequest(RiotAuth? riotAuth, HttpMethod method, string endpointUrl, string body)
    {
        ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
        using var http = new HttpClient();
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:" + riotAuth?.Token));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        var requestMessage = new HttpRequestMessage(method, $"https://127.0.0.1:{riotAuth?.Port}{endpointUrl}") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        var responseMessage = await http.SendAsync(requestMessage);
        return await responseMessage.Content.ReadAsStringAsync();
    }
}