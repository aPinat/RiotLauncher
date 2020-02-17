using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RiotLauncher
{
    internal static class Program
    {
        private static readonly string RiotClientPath = LcuTools.GetRiotClientPath();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int Hide = 0;
        private const int Show = 5;

        public static void Main(string[] args)
        {
            if (args.Length == 1) ShowWindow(GetConsoleWindow(), Hide);
            
            Console.WriteLine("RiotLauncher");
            Console.WriteLine("1. Start League Live Client");
            Console.WriteLine("2. Start League PBE Client");
            Console.WriteLine("3. Start League Live Client duplicate");
            Console.WriteLine("4. Start League PBE Client duplicate");
            Console.WriteLine("5. Start Legends of Runeterra");
            Console.WriteLine("6. Start Legends of Runeterra duplicate");
            Console.WriteLine("7. Start Legends Live Client custom config");
            Console.WriteLine("8. Start Legends PBE Client custom config");
            Console.WriteLine("9. Start Legends of Runeterra custom config");
            Console.Write("Choose your option: ");
            int input;
            
            while (true)
            {
                if (args.Length == 1) input = int.Parse(args[0]);
                else int.TryParse(Console.ReadLine(), out input);
                if (input >= 1 && input <= 9) break;
                Console.Write("Invalid input... Try again: ");
            }
            
            RunSelection(input);
        }

        private static void RunSelection(int input)
        {
            switch (input)
            {
                case 1:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=live");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 2:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=pbe");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 3:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=live --allow-multiple-clients");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 4:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=league_of_legends --launch-patchline=pbe --allow-multiple-clients");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 5:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=bacon --launch-patchline=live");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 6:
                    if (RiotClientPath != null)
                    {
                        Process.Start(RiotClientPath, "--launch-product=bacon --launch-patchline=live --allow-multiple-clients");
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 7:
                    if (RiotClientPath != null)
                    {
                        var proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                        var process = Process.Start(RiotClientPath, "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=live");
                        ShowWindow(GetConsoleWindow(), Hide);
                        process?.WaitForExit();
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 8:
                    if (RiotClientPath != null)
                    {
                        var proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                        var process = Process.Start(RiotClientPath, "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=league_of_legends --launch-patchline=pbe");
                        ShowWindow(GetConsoleWindow(), Hide);
                        process?.WaitForExit();
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                case 9:
                    if (RiotClientPath != null)
                    {
                        var proxyServer = new ConfigProxy("https://clientconfig.rpg.riotgames.com");
                        var process = Process.Start(RiotClientPath, "--client-config-url=\"http://localhost:" + proxyServer.ConfigPort + "\" --launch-product=bacon --launch-patchline=live");
                        ShowWindow(GetConsoleWindow(), Hide);
                        process?.WaitForExit();
                    }
                    else
                    {
                        Console.WriteLine("No Riot Client found.");
                        Console.ReadLine();
                    }

                    break;
                default:
                    Console.WriteLine("Invalid input.");
                    Console.ReadLine();
                    break;
            }
        }
    }
}