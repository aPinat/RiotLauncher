using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RiotLauncher;

public static class Program
{
    private const int Hide = 0;
    private const int Show = 5;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static async Task Main(string[] args)
    {
        ShowWindow(GetConsoleWindow(), args.Length == 1 ? Hide : Show);
        Console.WriteLine("RiotLauncher");
        Console.WriteLine("0. Kill all Riot Client processes");
        Console.WriteLine("1. Start Riot Client");
        Console.WriteLine("2. Start Riot Client duplicate");
        Console.WriteLine("3. Start Riot Client custom config");
        Console.WriteLine("4. Start Riot Client custom config duplicate");
        Console.WriteLine("5. Start League of Legends");
        Console.WriteLine("6. Start League of Legends duplicate");
        Console.WriteLine("7. Start League of Legends custom config");
        Console.WriteLine("8. Start League of Legends custom config duplicate");
        Console.WriteLine("9. Start Legends of Runeterra");
        Console.WriteLine("10. Start Legends of Runeterra duplicate");
        Console.WriteLine("11. Start Legends of Runeterra custom config");
        Console.WriteLine("12. Start Legends of Runeterra custom config duplicate");
        Console.WriteLine("13. Start VALORANT");
        Console.WriteLine("14. Start VALORANT duplicate");
        Console.WriteLine("15. Start VALORANT custom config");
        Console.WriteLine("16. Start VALORANT custom config duplicate");
        Console.WriteLine("17. Start League of Legends custom config with PBE client on Live");
        Console.WriteLine();

        Console.Write("Choose your option: ");
        int input;

        while (true)
        {
            if (args.Length == 1)
            {
                input = int.Parse(args[0]);
                break;
            }

            if (int.TryParse(Console.ReadLine(), out input) && input is >= 0 and <= 17)
                break;

            Console.Write("Invalid input... Try again: ");
        }

        var riotClientPath = GetRiotClientPath();
        if (riotClientPath != null)
        {
            ShowWindow(GetConsoleWindow(), Hide);
            if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().All(endpoint => endpoint.Port != 2099))
            {
                var _ = new TcpProxy("prod.euw1.lol.riotgames.com", 2099);
            }

            if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().All(endpoint => endpoint.Port != 5223))
            {
                var __ = new ChatProxy("euw1.chat.si.riotgames.com", 5223);
            }

            await RunSelectionAsync(input, riotClientPath);
        }
        else
        {
            Console.WriteLine("No Riot Client found.");
            Console.ReadLine();
        }
    }

    private static async Task RunSelectionAsync(int input, string riotClientPath)
    {
        Process process;
        ConfigProxy proxyServer;
        switch (input)
        {
            case 0:
                foreach (var p in Process.GetProcessesByName("RiotClientServices"))
                    p.Kill(true);
                break;
            case 1:
                Process.Start(riotClientPath, "");
                break;
            case 2:
                Process.Start(riotClientPath, "--allow-multiple-clients");
                break;
            case 3:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\"");
                await CheckRunningProcessAsync(process);
                break;
            case 4:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --allow-multiple-clients");
                await CheckRunningProcessAsync(process);
                break;
            case 5:
                Process.Start(riotClientPath, "--launch-product=league_of_legends --launch-patchline=live");
                break;
            case 6:
                Process.Start(riotClientPath, "--launch-product=league_of_legends --launch-patchline=live --allow-multiple-clients");
                break;
            case 7:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live");
                await CheckRunningProcessAsync(process);
                break;
            case 8:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live --allow-multiple-clients");
                await CheckRunningProcessAsync(process);
                break;
            case 9:
                Process.Start(riotClientPath, "--launch-product=bacon --launch-patchline=live");
                break;
            case 10:
                Process.Start(riotClientPath, "--launch-product=bacon --launch-patchline=live --allow-multiple-clients");
                break;
            case 11:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath, "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=bacon --launch-patchline=live");
                await CheckRunningProcessAsync(process);
                break;
            case 12:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=bacon --launch-patchline=live --allow-multiple-clients");
                await CheckRunningProcessAsync(process);
                break;
            case 13:
                Process.Start(riotClientPath, "--launch-product=valorant --launch-patchline=live");
                break;
            case 14:
                Process.Start(riotClientPath, "--launch-product=valorant --launch-patchline=live --allow-multiple-clients");
                break;
            case 15:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=valorant --launch-patchline=live");
                await CheckRunningProcessAsync(process);
                break;
            case 16:
                proxyServer = new ConfigProxy();
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=valorant --launch-patchline=live --allow-multiple-clients");
                await CheckRunningProcessAsync(process);
                break;
            case 17:
                proxyServer = new ConfigProxy(input);
                process = Process.Start(riotClientPath,
                    "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live");
                await CheckRunningProcessAsync(process);
                break;
            default:
                Console.WriteLine("Invalid input.");
                Console.ReadLine();
                break;
        }
    }

    private static async Task CheckRunningProcessAsync(Process process)
    {
        await process.WaitForExitAsync();
        while (true)
        {
            var processes = Process.GetProcessesByName("RiotClientServices");
            if (processes.Length == 0)
                return;
            await Task.Delay(5000);
        }
    }

    private static string? GetRiotClientPath()
    {
        var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Riot Games/RiotClientInstalls.json");
        if (!File.Exists(installPath))
            return null;

        var data = JsonSerializer.Deserialize<JsonNode>(File.ReadAllText(installPath));
        var rcPaths = new List<string?> { data?["rc_default"]?.ToString(), data?["rc_live"]?.ToString(), data?["rc_beta"]?.ToString() };

        return rcPaths.FirstOrDefault(File.Exists);
    }
}
