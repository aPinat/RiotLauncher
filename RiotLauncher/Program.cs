using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RiotLauncher
{
    internal static class Program
    {
        private const int Hide = 0;
        private const int Show = 5;
        private static readonly string? RiotClientPath = GetRiotClientPath();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static async Task Main(string[] args)
        {
            if (args.Length == 1) ShowWindow(GetConsoleWindow(), Hide);

            Console.WriteLine("RiotLauncher");
            Console.WriteLine("1. Start League of Legends");
            Console.WriteLine("2. Start League of Legends duplicate");
            Console.WriteLine("3. Start League of Legends custom config");
            Console.WriteLine("4. Start League of Legends custom config duplicate");
            Console.WriteLine("5. Start Legends of Runeterra");
            Console.WriteLine("6. Start Legends of Runeterra duplicate");
            Console.WriteLine("7. Start Legends of Runeterra custom config");
            Console.WriteLine("8. Start Legends of Runeterra custom config duplicate");
            Console.WriteLine("9. Start VALORANT");
            Console.WriteLine("10. Start VALORANT duplicate");
            Console.WriteLine("11. Start VALORANT custom config");
            Console.WriteLine("12. Start VALORANT custom config duplicate");

            Console.Write("Choose your option: ");
            int input;

            while (true)
            {
                if (args.Length == 1) input = int.Parse(args[0]);
                else int.TryParse(Console.ReadLine(), out input);
                if (input >= 1 && input <= 12) break;
                Console.Write("Invalid input... Try again: ");
            }

            if (RiotClientPath != null)
            {
                await RunSelection(input);
            }
            else
            {
                Console.WriteLine("No Riot Client found.");
                Console.ReadLine();
            }
        }

        private static async Task RunSelection(int input)
        {
            Process process;
            ConfigProxy proxyServer;
            switch (input)
            {
                case 1:
                    Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=live");
                    break;
                case 2:
                    Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=live --allow-multiple-clients");
                    break;
                case 3:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath,
                        "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                case 4:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath,
                        "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live --allow-multiple-clients");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                case 5:
                    Process.Start(RiotClientPath, "--launch-product=bacon --launch-patchline=live");
                    break;
                case 6:
                    Process.Start(RiotClientPath, "--launch-product=bacon --launch-patchline=live --allow-multiple-clients");
                    break;
                case 7:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath, "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=bacon --launch-patchline=live");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                case 8:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath,
                        "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=bacon --launch-patchline=live --allow-multiple-clients");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                case 9:
                    Process.Start(RiotClientPath, "--launch-product=valorant --launch-patchline=live");
                    break;
                case 10:
                    Process.Start(RiotClientPath, "--launch-product=valorant --launch-patchline=live --allow-multiple-clients");
                    break;
                case 11:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath,
                        "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=valorant --launch-patchline=live");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                case 12:
                    proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                    process = Process.Start(RiotClientPath,
                        "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=valorant --launch-patchline=live --allow-multiple-clients");
                    ShowWindow(GetConsoleWindow(), Hide);
                    await CheckRunningProcess(process);
                    break;
                default:
                    Console.WriteLine("Invalid input.");
                    Console.ReadLine();
                    break;
            }
        }

        private static async Task CheckRunningProcess(Process process)
        {
            process?.WaitForExit();
            while (true)
            {
                var processes = Process.GetProcessesByName("RiotClientServices");
                if (processes.Length == 0) return;
                await Task.Delay(5000);
            }
        }

        private static string? GetRiotClientPath()
        {
            // Find the RiotClientInstalls file.
            var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Riot Games/RiotClientInstalls.json");
            if (!File.Exists(installPath)) return null;

            var data = JObject.Parse(File.ReadAllText(installPath));
            var rcPaths = new List<string?> {data["rc_default"]?.ToString(), data["rc_live"]?.ToString(), data["rc_beta"]?.ToString()};
            
            return rcPaths.FirstOrDefault(File.Exists);
        }
    }
}